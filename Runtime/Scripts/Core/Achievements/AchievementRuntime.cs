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
    /// Implements <see cref="IObjectiveGroup"/> (which extends <see cref="IObjective"/>).
    /// </summary>
    public class AchievementRuntime : IObjectiveGroup
    {
        #region IObjective Events (UnityEvents)

        /// <inheritdoc />
        public UnityEvent<IObjective> Started { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> ProgressChanged { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> Completed { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> Failed { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> Updated { get; set; } = new();

        #endregion

        #region IObjectiveGroup – additional child‑completion event

        /// <inheritdoc />
        public UnityEvent<IObjectiveGroup, IObjective> OnObjectiveCompleted { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ScriptableObject data.
        /// </summary>
        public Achievement_SO Data { get; }

        /// <inheritdoc />
        public Guid Id { get; }

        /// <summary>
        /// Gets the developer‑friendly name.
        /// </summary>
        public string DevName => Data.DevName;

        /// <inheritdoc />
        public State State { get; private set; }

        /// <summary>
        /// Gets the current progress value (for progressive achievements).
        /// </summary>
        public int CurrentValue { get; private set; }

        /// <summary>
        /// Gets the target value.
        /// </summary>
        public int TargetValue => Data.TargetValue;

        /// <inheritdoc />
        public float Progress => Data.AchievementType == AchievementType.Progressive && TargetValue > 0
            ? Math.Min(1f, (float)CurrentValue / TargetValue)
            : IsComplete ? 1f : 0f;

        /// <inheritdoc />
        public bool IsComplete => State == State.Completed;

        /// <inheritdoc />
        public bool IsFailed => State == State.Failed;

        /// <summary>
        /// Gets the unlock timestamp (if unlocked).
        /// </summary>
        public DateTime? UnlockTime { get; private set; }

        #endregion

        #region IObjectiveGroup – child management (stub, achievements have no children)

        /// <inheritdoc />
        public IReadOnlyList<IObjective> Objectives { get; } = Array.Empty<IObjective>();

        /// <inheritdoc />
        public ObjectiveExecutionMode ExecutionMode => ObjectiveExecutionMode.Sequential;

        /// <inheritdoc />
        public int RequiredCount => 1;

        /// <inheritdoc />
        public int CompletedCount => IsComplete ? 1 : 0;


        #endregion

        /// <summary>
        /// Creates a new runtime achievement from ScriptableObject data.
        /// </summary>
        public AchievementRuntime(Achievement_SO data)
        {
            Data = data;
            Id = data.AchievementId;
            State = State.NotStarted;
            CurrentValue = data.StartValue;
            UnlockTime = null;
        }

        #region IObjective Lifecycle

        /// <inheritdoc />
        public void Start() => StartTracking();

        /// <inheritdoc />
        public void Complete() => Unlock();

        /// <inheritdoc />
        public void Fail() { /* Achievements don't fail */ }

        /// <inheritdoc />
        public void Reset() => ResetProgress();

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts tracking this achievement.
        /// </summary>
        public void StartTracking()
        {
            if (State != State.NotStarted) return;

            State = State.InProgress;

            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
                eventCondition.SubscribeToEvent(Unlock);

            QuestLogger.Log(LogSubsystem.Achievement, $"Achievement '{DevName}' tracking started.");
            Started.Invoke(this);
        }

        /// <summary>
        /// Unlocks this achievement.
        /// </summary>
        public void Unlock()
        {
            if (State == State.Completed) return;

            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
                eventCondition.UnsubscribeFromEvent(Unlock);

            State = State.Completed;
            CurrentValue = TargetValue;
            UnlockTime = DateTime.UtcNow;

            QuestLogger.Log(LogSubsystem.Achievement, $"Achievement '{DevName}' unlocked!");

            ProgressChanged.Invoke(this);
            Completed.Invoke(this);
        }

        /// <summary>
        /// Increments progress (for progressive achievements).
        /// </summary>
        public void IncrementProgress(int amount = 1)
        {
            if (State != State.InProgress) return;
            if (Data.AchievementType != AchievementType.Progressive) return;

            CurrentValue = Math.Min(CurrentValue + amount, TargetValue);
            QuestLogger.LogVerbose(LogSubsystem.Achievement,
                $"Achievement '{DevName}' progress: {CurrentValue}/{TargetValue}");

            ProgressChanged.Invoke(this);
            Updated.Invoke(this);

            if (CurrentValue >= TargetValue)
                Unlock();
        }

        /// <summary>
        /// Sets progress to a specific value.
        /// </summary>
        public void SetProgress(int value)
        {
            if (State != State.InProgress) return;
            if (Data.AchievementType != AchievementType.Progressive) return;

            CurrentValue = Math.Clamp(value, 0, TargetValue);
            QuestLogger.LogVerbose(LogSubsystem.Achievement,
                $"Achievement '{DevName}' progress set: {CurrentValue}/{TargetValue}");

            ProgressChanged.Invoke(this);
            Updated.Invoke(this);

            if (CurrentValue >= TargetValue)
                Unlock();
        }

        /// <summary>
        /// Resets this achievement to its initial state.
        /// </summary>
        public void ResetProgress()
        {
            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
                eventCondition.UnsubscribeFromEvent(Unlock);

            State = State.NotStarted;
            CurrentValue = Data.StartValue;
            UnlockTime = null;

            QuestLogger.Log(LogSubsystem.Achievement, $"Achievement '{DevName}' reset.");
        }

        #endregion

        #region Save / Load

        /// <summary>
        /// Restores achievement state from a save.
        /// </summary>
        public void RestoreState(bool isUnlocked, int currentValue, DateTime? unlockTime)
        {
            if (Data.UnlockCondition is IConditionEventDriven eventCondition)
                eventCondition.UnsubscribeFromEvent(Unlock);

            CurrentValue = currentValue;
            UnlockTime = unlockTime;

            if (isUnlocked)
            {
                State = State.Completed;
            }
            else if (currentValue > Data.StartValue)
            {
                State = State.InProgress;
                if (Data.UnlockCondition is IConditionEventDriven eventCond)
                    eventCond.SubscribeToEvent(Unlock);
            }
            else
            {
                State = State.NotStarted;
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj) =>
            obj is AchievementRuntime other && Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();

        #endregion
    }
}