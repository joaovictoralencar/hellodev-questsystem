using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Achievements
{
    /// <summary>
    /// Runtime representation of an achievement.
    /// Implements IObjective and IObjectiveGroup for unified objective system compatibility.
    /// </summary>
    public class AchievementRuntime : IObjective, IObjectiveGroup
    {
        #region Events

        /// <summary>
        /// Fired when this achievement is unlocked.
        /// </summary>
        public UnityEvent<AchievementRuntime> OnAchievementUnlocked = new();

        /// <summary>
        /// Fired when progress changes (for progressive achievements).
        /// </summary>
        public UnityEvent<AchievementRuntime> OnProgressUpdated = new();

        #endregion

        #region IObjective Backing Fields

        private event Action<IObjective> _onObjectiveStarted;
        private event Action<IObjective> _onObjectiveProgressChanged;
        private event Action<IObjective> _onObjectiveCompleted;
        private event Action<IObjective> _onObjectiveFailed;

        #endregion

        #region IObjectiveGroup Backing Fields

        private event Action<IObjectiveGroup> _onGroupStarted;
        private event Action<IObjectiveGroup> _onGroupProgressChanged;
        private event Action<IObjectiveGroup> _onGroupCompleted;
        private event Action<IObjectiveGroup> _onGroupFailed;
        private event Action<IObjectiveGroup, IObjective> _onObjectiveInGroupCompleted;

        /// <summary>
        /// Empty list for IObjectiveGroup.Objectives (achievements don't contain sub-objectives).
        /// </summary>
        private static readonly IReadOnlyList<IObjective> EmptyObjectives = Array.Empty<IObjective>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ScriptableObject data.
        /// </summary>
        public Achievement_SO Data { get; }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public Guid AchievementId => Data.AchievementId;

        /// <summary>
        /// Gets the developer name.
        /// </summary>
        public string DevName => Data.DevName;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        public ObjectiveState CurrentState { get; private set; }

        /// <summary>
        /// Gets the current progress value (for progressive achievements).
        /// </summary>
        public int CurrentValue { get; private set; }

        /// <summary>
        /// Gets the target value.
        /// </summary>
        public int TargetValue => Data.TargetValue;

        /// <summary>
        /// Gets whether this achievement is unlocked.
        /// </summary>
        public bool IsUnlocked => CurrentState == ObjectiveState.Completed;

        /// <summary>
        /// Gets the unlock timestamp (if unlocked).
        /// </summary>
        public DateTime? UnlockTime { get; private set; }

        /// <summary>
        /// Gets the progress as a float (0-1).
        /// </summary>
        public float Progress => Data.AchievementType == AchievementType.Progressive && TargetValue > 0
            ? Math.Min(1f, (float)CurrentValue / TargetValue)
            : IsUnlocked ? 1f : 0f;

        #endregion

        #region IObjective Implementation

        /// <inheritdoc />
        string IObjective.Id => AchievementId.ToString();

        /// <inheritdoc />
        ObjectiveState IObjective.State => CurrentState;

        /// <inheritdoc />
        float IObjective.Progress => Progress;

        /// <inheritdoc />
        bool IObjective.IsComplete => IsUnlocked;

        /// <inheritdoc />
        bool IObjective.IsFailed => CurrentState == ObjectiveState.Failed;

        /// <inheritdoc />
        void IObjective.Start() => StartTracking();

        /// <inheritdoc />
        void IObjective.Complete() => Unlock();

        /// <inheritdoc />
        void IObjective.Fail() { } // Achievements don't fail

        /// <inheritdoc />
        void IObjective.Reset() => ResetProgress();

        /// <inheritdoc />
        event Action<IObjective> IObjective.OnStarted
        {
            add => _onObjectiveStarted += value;
            remove => _onObjectiveStarted -= value;
        }

        /// <inheritdoc />
        event Action<IObjective> IObjective.OnProgressChanged
        {
            add => _onObjectiveProgressChanged += value;
            remove => _onObjectiveProgressChanged -= value;
        }

        /// <inheritdoc />
        event Action<IObjective> IObjective.OnCompleted
        {
            add => _onObjectiveCompleted += value;
            remove => _onObjectiveCompleted -= value;
        }

        /// <inheritdoc />
        event Action<IObjective> IObjective.OnFailed
        {
            add => _onObjectiveFailed += value;
            remove => _onObjectiveFailed -= value;
        }

        #endregion

        #region IObjectiveGroup Implementation

        /// <inheritdoc />
        string IObjectiveGroup.Id => AchievementId.ToString();

        /// <inheritdoc />
        ObjectiveState IObjectiveGroup.State => CurrentState;

        /// <inheritdoc />
        float IObjectiveGroup.Progress => Progress;

        /// <inheritdoc />
        IReadOnlyList<IObjective> IObjectiveGroup.Objectives => EmptyObjectives;

        /// <inheritdoc />
        ObjectiveExecutionMode IObjectiveGroup.ExecutionMode => ObjectiveExecutionMode.Sequential;

        /// <inheritdoc />
        int IObjectiveGroup.RequiredCount => 1;

        /// <inheritdoc />
        int IObjectiveGroup.CompletedCount => IsUnlocked ? 1 : 0;

        /// <inheritdoc />
        event Action<IObjectiveGroup> IObjectiveGroup.OnStarted
        {
            add => _onGroupStarted += value;
            remove => _onGroupStarted -= value;
        }

        /// <inheritdoc />
        event Action<IObjectiveGroup> IObjectiveGroup.OnProgressChanged
        {
            add => _onGroupProgressChanged += value;
            remove => _onGroupProgressChanged -= value;
        }

        /// <inheritdoc />
        event Action<IObjectiveGroup> IObjectiveGroup.OnCompleted
        {
            add => _onGroupCompleted += value;
            remove => _onGroupCompleted -= value;
        }

        /// <inheritdoc />
        event Action<IObjectiveGroup> IObjectiveGroup.OnFailed
        {
            add => _onGroupFailed += value;
            remove => _onGroupFailed -= value;
        }

        /// <inheritdoc />
        event Action<IObjectiveGroup, IObjective> IObjectiveGroup.OnObjectiveCompleted
        {
            add => _onObjectiveInGroupCompleted += value;
            remove => _onObjectiveInGroupCompleted -= value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new runtime achievement from ScriptableObject data.
        /// </summary>
        /// <param name="data">The ScriptableObject containing achievement configuration.</param>
        public AchievementRuntime(Achievement_SO data)
        {
            Data = data;
            CurrentState = ObjectiveState.NotStarted;
            CurrentValue = data.StartValue;
            UnlockTime = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts tracking this achievement.
        /// </summary>
        public void StartTracking()
        {
            if (CurrentState != ObjectiveState.NotStarted) return;

            CurrentState = ObjectiveState.InProgress;

            // Subscribe to unlock condition if available
            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.SubscribeToEvent(Unlock);
            }

            QuestLogger.Log(LogSubsystem.Achievement, $"Achievement '{DevName}' tracking started.");
            _onObjectiveStarted?.Invoke(this);
            _onGroupStarted?.Invoke(this);
        }

        /// <summary>
        /// Unlocks this achievement.
        /// </summary>
        public void Unlock()
        {
            if (CurrentState == ObjectiveState.Completed) return;

            // Unsubscribe from condition
            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(Unlock);
            }

            CurrentState = ObjectiveState.Completed;
            CurrentValue = TargetValue;
            UnlockTime = DateTime.UtcNow;

            QuestLogger.Log(LogSubsystem.Achievement, $"Achievement '{DevName}' unlocked!");

            _onObjectiveProgressChanged?.Invoke(this);
            _onGroupProgressChanged?.Invoke(this);
            OnAchievementUnlocked?.Invoke(this);
            _onObjectiveCompleted?.Invoke(this);
            _onGroupCompleted?.Invoke(this);
            _onObjectiveInGroupCompleted?.Invoke(this, this);
        }

        /// <summary>
        /// Increments progress (for progressive achievements).
        /// </summary>
        /// <param name="amount">Amount to add.</param>
        public void IncrementProgress(int amount = 1)
        {
            if (CurrentState != ObjectiveState.InProgress) return;
            if (Data.AchievementType != AchievementType.Progressive) return;

            CurrentValue = Math.Min(CurrentValue + amount, TargetValue);
            QuestLogger.LogVerbose(LogSubsystem.Achievement, $"Achievement '{DevName}' progress: {CurrentValue}/{TargetValue}");

            _onObjectiveProgressChanged?.Invoke(this);
            _onGroupProgressChanged?.Invoke(this);
            OnProgressUpdated?.Invoke(this);

            // Check for completion
            if (CurrentValue >= TargetValue)
            {
                Unlock();
            }
        }

        /// <summary>
        /// Sets progress to a specific value (for progressive achievements).
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetProgress(int value)
        {
            if (CurrentState != ObjectiveState.InProgress) return;
            if (Data.AchievementType != AchievementType.Progressive) return;

            CurrentValue = Math.Clamp(value, 0, TargetValue);
            QuestLogger.LogVerbose(LogSubsystem.Achievement, $"Achievement '{DevName}' progress set: {CurrentValue}/{TargetValue}");

            _onObjectiveProgressChanged?.Invoke(this);
            _onGroupProgressChanged?.Invoke(this);
            OnProgressUpdated?.Invoke(this);

            // Check for completion
            if (CurrentValue >= TargetValue)
            {
                Unlock();
            }
        }

        /// <summary>
        /// Resets this achievement to initial state.
        /// </summary>
        public void ResetProgress()
        {
            // Unsubscribe from condition
            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(Unlock);
            }

            CurrentState = ObjectiveState.NotStarted;
            CurrentValue = Data.StartValue;
            UnlockTime = null;

            QuestLogger.Log(LogSubsystem.Achievement, $"Achievement '{DevName}' reset.");
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Restores achievement state from a save.
        /// </summary>
        /// <param name="isUnlocked">Whether the achievement was unlocked.</param>
        /// <param name="currentValue">The saved progress value.</param>
        /// <param name="unlockTime">The saved unlock time (if unlocked).</param>
        public void RestoreState(bool isUnlocked, int currentValue, DateTime? unlockTime)
        {
            // First unsubscribe from any existing condition subscription
            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(Unlock);
            }

            CurrentValue = currentValue;
            UnlockTime = unlockTime;

            if (isUnlocked)
            {
                CurrentState = ObjectiveState.Completed;
            }
            else if (currentValue > Data.StartValue)
            {
                CurrentState = ObjectiveState.InProgress;

                // Re-subscribe to condition if we're in progress
                if (Data.UnlockCondition is IConditionEventDriven eventCond)
                {
                    eventCond.SubscribeToEvent(Unlock);
                }
            }
            else
            {
                CurrentState = ObjectiveState.NotStarted;
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is AchievementRuntime other)
            {
                return AchievementId == other.AchievementId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return AchievementId.GetHashCode();
        }

        #endregion
    }
}
