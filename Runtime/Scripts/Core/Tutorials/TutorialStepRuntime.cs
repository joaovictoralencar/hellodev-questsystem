using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Tutorials
{
    /// <summary>
    /// Runtime representation of a tutorial step.
    /// Implements IStage for unified objective system compatibility.
    /// </summary>
    public class TutorialStepRuntime : IStage
    {
        #region Events

        /// <summary>
        /// Fired when this step starts.
        /// </summary>
        public UnityEvent<TutorialStepRuntime> OnStepStarted = new();

        /// <summary>
        /// Fired when this step completes.
        /// </summary>
        public UnityEvent<TutorialStepRuntime> OnStepCompleted = new();

        /// <summary>
        /// Fired when this step is skipped.
        /// </summary>
        public UnityEvent<TutorialStepRuntime> OnStepSkipped = new();

        #endregion

        #region IStage Backing Fields

        private event Action<IStage> _onEntered;
        private event Action<IStage> _onProgressChanged;
        private event Action<IStage> _onCompleted;
        private event Action<IStage> _onFailed;
        private event Action<IStage> _onExited;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ScriptableObject data for this step.
        /// </summary>
        public TutorialStep_SO Data { get; }

        /// <summary>
        /// Gets the unique identifier for this step.
        /// </summary>
        public Guid StepId => Data.StepId;

        /// <summary>
        /// Gets the developer name for this step.
        /// </summary>
        public string DevName => Data.DevName;

        /// <summary>
        /// Gets the current state of this step.
        /// </summary>
        public ObjectiveState CurrentState { get; private set; }

        /// <summary>
        /// Gets whether this step was skipped (completed via skip, not normal completion).
        /// </summary>
        public bool WasSkipped { get; private set; }

        /// <summary>
        /// Gets the elapsed time for timed steps.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// Gets or sets the index of this step within its parent tutorial.
        /// </summary>
        public int StepIndex { get; internal set; } = -1;

        /// <summary>
        /// Gets the progress of this step (0-1).
        /// </summary>
        public float Progress => CurrentState switch
        {
            ObjectiveState.Completed => 1f,
            ObjectiveState.InProgress when Data.IsTimedStep && Data.Duration > 0 =>
                Math.Min(1f, ElapsedTime / Data.Duration),
            ObjectiveState.InProgress => 0.5f,
            _ => 0f
        };

        #endregion

        #region IStage Implementation

        /// <inheritdoc />
        int IStage.Index => StepIndex;

        /// <inheritdoc />
        string IStage.Id => StepId.ToString();

        /// <inheritdoc />
        ObjectiveState IStage.State => CurrentState;

        /// <inheritdoc />
        float IStage.Progress => Progress;

        /// <inheritdoc />
        IReadOnlyList<IObjectiveGroup> IStage.ObjectiveGroups => Array.Empty<IObjectiveGroup>();

        /// <inheritdoc />
        bool IStage.IsTerminal => false; // Tutorial steps are never terminal on their own

        /// <inheritdoc />
        bool IStage.IsOptional => Data.CanSkip;

        /// <inheritdoc />
        bool IStage.IsHidden => false;

        /// <inheritdoc />
        event Action<IStage> IStage.OnEntered
        {
            add => _onEntered += value;
            remove => _onEntered -= value;
        }

        /// <inheritdoc />
        event Action<IStage> IStage.OnProgressChanged
        {
            add => _onProgressChanged += value;
            remove => _onProgressChanged -= value;
        }

        /// <inheritdoc />
        event Action<IStage> IStage.OnCompleted
        {
            add => _onCompleted += value;
            remove => _onCompleted -= value;
        }

        /// <inheritdoc />
        event Action<IStage> IStage.OnFailed
        {
            add => _onFailed += value;
            remove => _onFailed -= value;
        }

        /// <inheritdoc />
        event Action<IStage> IStage.OnExited
        {
            add => _onExited += value;
            remove => _onExited -= value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new runtime tutorial step from ScriptableObject data.
        /// </summary>
        /// <param name="data">The ScriptableObject containing step configuration.</param>
        public TutorialStepRuntime(TutorialStep_SO data)
        {
            Data = data;
            CurrentState = ObjectiveState.NotStarted;
            WasSkipped = false;
            ElapsedTime = 0f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts this tutorial step.
        /// </summary>
        public void StartStep()
        {
            if (CurrentState != ObjectiveState.NotStarted) return;

            CurrentState = ObjectiveState.InProgress;

            // Subscribe to completion condition if available
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.SubscribeToEvent(CompleteStep);
            }

            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial step '{DevName}' started.");

            OnStepStarted?.Invoke(this);
            _onEntered?.Invoke(this);
        }

        /// <summary>
        /// Completes this tutorial step.
        /// </summary>
        public void CompleteStep()
        {
            if (CurrentState != ObjectiveState.InProgress) return;

            // Unsubscribe from condition
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(CompleteStep);
            }

            CurrentState = ObjectiveState.Completed;
            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial step '{DevName}' completed.");

            _onProgressChanged?.Invoke(this);
            OnStepCompleted?.Invoke(this);
            _onCompleted?.Invoke(this);
            _onExited?.Invoke(this);
        }

        /// <summary>
        /// Skips this tutorial step (if allowed).
        /// </summary>
        /// <returns>True if the step was skipped, false if skipping is not allowed.</returns>
        public bool SkipStep()
        {
            if (CurrentState != ObjectiveState.InProgress) return false;
            if (!Data.CanSkip) return false;

            // Unsubscribe from condition
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(CompleteStep);
            }

            WasSkipped = true;
            CurrentState = ObjectiveState.Completed;
            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial step '{DevName}' skipped.");

            _onProgressChanged?.Invoke(this);
            OnStepSkipped?.Invoke(this);
            OnStepCompleted?.Invoke(this);
            _onCompleted?.Invoke(this);
            _onExited?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Updates the elapsed time for timed steps.
        /// </summary>
        /// <param name="deltaTime">Time since last update.</param>
        public void UpdateTime(float deltaTime)
        {
            if (CurrentState != ObjectiveState.InProgress) return;
            if (!Data.IsTimedStep) return;

            ElapsedTime += deltaTime;
            _onProgressChanged?.Invoke(this);

            if (ElapsedTime >= Data.Duration)
            {
                CompleteStep();
            }
        }

        /// <summary>
        /// Fails this tutorial step.
        /// </summary>
        public void FailStep()
        {
            if (CurrentState != ObjectiveState.InProgress) return;

            // Unsubscribe from condition
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(CompleteStep);
            }

            CurrentState = ObjectiveState.Failed;
            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial step '{DevName}' failed.");

            _onProgressChanged?.Invoke(this);
            _onFailed?.Invoke(this);
            _onExited?.Invoke(this);
        }

        /// <summary>
        /// Resets this step to its initial state.
        /// </summary>
        public void ResetStep()
        {
            // Unsubscribe from condition if we were in progress
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.UnsubscribeFromEvent(CompleteStep);
            }

            CurrentState = ObjectiveState.NotStarted;
            WasSkipped = false;
            ElapsedTime = 0f;
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Restores this step's state from a save.
        /// Sets state directly without triggering events or condition subscriptions.
        /// </summary>
        /// <param name="state">The state to restore to.</param>
        /// <param name="elapsedTime">The elapsed time to restore (for timed steps).</param>
        public void RestoreStepState(ObjectiveState state, float elapsedTime)
        {
            CurrentState = state;
            ElapsedTime = elapsedTime;

            QuestLogger.LogVerbose(LogSubsystem.Tutorial, $"Step '{DevName}' state restored: {state}, elapsed={elapsedTime}");
        }

        /// <summary>
        /// Resumes an in-progress step after loading.
        /// Re-subscribes to completion condition events.
        /// </summary>
        public void ResumeStep()
        {
            if (CurrentState != ObjectiveState.InProgress) return;

            // Re-subscribe to completion condition if available
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.SubscribeToEvent(CompleteStep);
                QuestLogger.LogVerbose(LogSubsystem.Tutorial, $"Step '{DevName}' resumed condition subscription.");
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is TutorialStepRuntime other)
            {
                return StepId == other.StepId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return StepId.GetHashCode();
        }

        #endregion
    }
}
