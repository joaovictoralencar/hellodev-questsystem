using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Tutorials
{
    /// <summary>
    /// Runtime representation of a tutorial.
    /// Implements IMission for unified objective system compatibility.
    /// Tutorials are sequential - one step at a time.
    /// </summary>
    public class TutorialRuntime : IMission
    {
        #region Events

        /// <summary>
        /// Fired when this tutorial starts.
        /// </summary>
        public UnityEvent<TutorialRuntime> OnTutorialStarted = new();

        /// <summary>
        /// Fired when this tutorial completes.
        /// </summary>
        public UnityEvent<TutorialRuntime> OnTutorialCompleted = new();

        /// <summary>
        /// Fired when a step in this tutorial starts.
        /// </summary>
        public UnityEvent<TutorialRuntime, TutorialStepRuntime> OnStepStarted = new();

        /// <summary>
        /// Fired when a step in this tutorial completes.
        /// </summary>
        public UnityEvent<TutorialRuntime, TutorialStepRuntime> OnStepCompleted = new();

        /// <summary>
        /// Fired when this tutorial is skipped entirely.
        /// </summary>
        public UnityEvent<TutorialRuntime> OnTutorialSkipped = new();

        #endregion

        #region IMission Backing Fields

        private event Action<IMission> _onStarted;
        private event Action<IMission> _onProgressChanged;
        private event Action<IMission> _onCompleted;
        private event Action<IMission> _onFailed;
        private event Action<IMission, IStage> _onStageEntered;
        private event Action<IMission, IStage> _onStageCompleted;

        /// <summary>
        /// Steps cast as IStage for IMission interface.
        /// </summary>
        private readonly IReadOnlyList<IStage> _stepsAsStages;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ScriptableObject data for this tutorial.
        /// </summary>
        public Tutorial_SO Data { get; }

        /// <summary>
        /// Gets the unique identifier for this tutorial.
        /// </summary>
        public Guid TutorialId => Data.TutorialId;

        /// <summary>
        /// Gets the developer name for this tutorial.
        /// </summary>
        public string DevName => Data.DevName;

        /// <summary>
        /// Gets the current state of this tutorial.
        /// </summary>
        public ObjectiveState CurrentState { get; private set; }

        /// <summary>
        /// Gets all runtime steps in this tutorial.
        /// </summary>
        public IReadOnlyList<TutorialStepRuntime> Steps { get; }

        /// <summary>
        /// Gets the index of the current step (-1 if not started).
        /// </summary>
        public int CurrentStepIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the current step, or null if not started or completed.
        /// </summary>
        public TutorialStepRuntime CurrentStep =>
            CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count
                ? Steps[CurrentStepIndex]
                : null;

        /// <summary>
        /// Gets whether this tutorial was skipped.
        /// </summary>
        public bool WasSkipped { get; private set; }

        /// <summary>
        /// Gets the progress of this tutorial (0-1).
        /// </summary>
        public float Progress => Steps.Count == 0 ? 1f :
            (float)Steps.Count(s => s.CurrentState == ObjectiveState.Completed) / Steps.Count;

        #endregion

        #region IMission Implementation

        /// <inheritdoc />
        Guid IMission.MissionId => TutorialId;

        /// <inheritdoc />
        string IMission.DisplayName => Data.DisplayName?.GetLocalizedString() ?? DevName;

        /// <inheritdoc />
        ObjectiveState IMission.State => CurrentState;

        /// <inheritdoc />
        float IMission.Progress => Progress;

        /// <inheritdoc />
        IReadOnlyList<IStage> IMission.Stages => _stepsAsStages;

        /// <inheritdoc />
        IStage IMission.CurrentStage => CurrentStep;

        /// <inheritdoc />
        int IMission.CurrentStageIndex => CurrentStepIndex;

        /// <inheritdoc />
        void IMission.Start() => StartTutorial();

        /// <inheritdoc />
        void IMission.Complete() => CompleteTutorial();

        /// <inheritdoc />
        void IMission.Fail() => FailTutorial();

        /// <inheritdoc />
        void IMission.Reset() => ResetTutorial();

        /// <inheritdoc />
        event Action<IMission> IMission.OnStarted
        {
            add => _onStarted += value;
            remove => _onStarted -= value;
        }

        /// <inheritdoc />
        event Action<IMission> IMission.OnProgressChanged
        {
            add => _onProgressChanged += value;
            remove => _onProgressChanged -= value;
        }

        /// <inheritdoc />
        event Action<IMission> IMission.OnCompleted
        {
            add => _onCompleted += value;
            remove => _onCompleted -= value;
        }

        /// <inheritdoc />
        event Action<IMission> IMission.OnFailed
        {
            add => _onFailed += value;
            remove => _onFailed -= value;
        }

        /// <inheritdoc />
        event Action<IMission, IStage> IMission.OnStageEntered
        {
            add => _onStageEntered += value;
            remove => _onStageEntered -= value;
        }

        /// <inheritdoc />
        event Action<IMission, IStage> IMission.OnStageCompleted
        {
            add => _onStageCompleted += value;
            remove => _onStageCompleted -= value;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new runtime tutorial from ScriptableObject data.
        /// </summary>
        /// <param name="data">The ScriptableObject containing tutorial configuration.</param>
        public TutorialRuntime(Tutorial_SO data)
        {
            Data = data;
            CurrentState = ObjectiveState.NotStarted;
            WasSkipped = false;

            // Create runtime steps
            var steps = data.Steps
                .Where(s => s != null)
                .Select(s => s.GetRuntimeStep())
                .ToList();

            // Set step indices
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].StepIndex = i;
            }

            Steps = steps;
            _stepsAsStages = steps.Cast<IStage>().ToList();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts this tutorial, beginning with the first step.
        /// </summary>
        public void StartTutorial()
        {
            if (CurrentState != ObjectiveState.NotStarted) return;
            if (Steps.Count == 0)
            {
                QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' has no steps, completing immediately.");
                CurrentState = ObjectiveState.Completed;
                OnTutorialCompleted?.Invoke(this);
                _onCompleted?.Invoke(this);
                return;
            }

            CurrentState = ObjectiveState.InProgress;
            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' started with {Steps.Count} steps.");

            // Subscribe to step events
            foreach (var step in Steps)
            {
                step.OnStepCompleted.AddListener(HandleStepCompleted);
            }

            OnTutorialStarted?.Invoke(this);
            _onStarted?.Invoke(this);

            // Start first step
            CurrentStepIndex = 0;
            Steps[0].StartStep();
            OnStepStarted?.Invoke(this, Steps[0]);
            _onStageEntered?.Invoke(this, Steps[0]);
        }

        /// <summary>
        /// Completes this tutorial.
        /// </summary>
        public void CompleteTutorial()
        {
            if (CurrentState != ObjectiveState.InProgress) return;

            CurrentState = ObjectiveState.Completed;
            UnsubscribeFromStepEvents();

            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' completed.");

            _onProgressChanged?.Invoke(this);
            OnTutorialCompleted?.Invoke(this);
            _onCompleted?.Invoke(this);
        }

        /// <summary>
        /// Skips this tutorial entirely (if allowed).
        /// </summary>
        /// <returns>True if skipped, false if skipping is not allowed.</returns>
        public bool SkipTutorial()
        {
            if (CurrentState != ObjectiveState.InProgress) return false;
            if (!Data.CanSkip) return false;

            WasSkipped = true;
            CurrentState = ObjectiveState.Completed;
            UnsubscribeFromStepEvents();

            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' skipped.");

            _onProgressChanged?.Invoke(this);
            OnTutorialSkipped?.Invoke(this);
            OnTutorialCompleted?.Invoke(this);
            _onCompleted?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Advances to complete the current step (for manual completion triggers).
        /// </summary>
        public void CompleteCurrentStep()
        {
            CurrentStep?.CompleteStep();
        }

        /// <summary>
        /// Skips the current step (if allowed).
        /// </summary>
        /// <returns>True if skipped, false if not allowed.</returns>
        public bool SkipCurrentStep()
        {
            return CurrentStep?.SkipStep() ?? false;
        }

        /// <summary>
        /// Updates timed steps.
        /// </summary>
        /// <param name="deltaTime">Time since last update.</param>
        public void UpdateTime(float deltaTime)
        {
            if (CurrentState != ObjectiveState.InProgress) return;
            CurrentStep?.UpdateTime(deltaTime);
        }

        /// <summary>
        /// Fails this tutorial.
        /// </summary>
        public void FailTutorial()
        {
            if (CurrentState != ObjectiveState.InProgress) return;

            CurrentState = ObjectiveState.Failed;
            UnsubscribeFromStepEvents();

            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' failed.");

            _onFailed?.Invoke(this);
        }

        /// <summary>
        /// Resets this tutorial to its initial state.
        /// </summary>
        public void ResetTutorial()
        {
            UnsubscribeFromStepEvents();

            foreach (var step in Steps)
            {
                step.ResetStep();
            }

            CurrentState = ObjectiveState.NotStarted;
            CurrentStepIndex = -1;
            WasSkipped = false;

            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' reset.");
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Restores this tutorial's state from a save.
        /// Called after step states have already been restored.
        /// </summary>
        /// <param name="state">The state to restore to.</param>
        /// <param name="currentStepIndex">The step index to restore to.</param>
        /// <param name="fireEvents">If true, fires OnTutorialStarted and OnStepStarted events so UI can update.</param>
        public void RestoreTutorialState(ObjectiveState state, int currentStepIndex, bool fireEvents = true)
        {
            CurrentState = state;
            CurrentStepIndex = currentStepIndex;

            // Subscribe to step events for in-progress tutorials
            if (state == ObjectiveState.InProgress)
            {
                foreach (var step in Steps)
                {
                    step.OnStepCompleted.AddListener(HandleStepCompleted);
                }

                // Resume the current step's condition subscription if needed
                if (CurrentStep != null && CurrentStep.CurrentState == ObjectiveState.InProgress)
                {
                    CurrentStep.ResumeStep();

                    // Fire events so UI can display current state
                    if (fireEvents)
                    {
                        QuestLogger.Log(LogSubsystem.Tutorial, $"Firing restore events for '{DevName}' step '{CurrentStep.DevName}'");
                        OnTutorialStarted?.Invoke(this);
                        _onStarted?.Invoke(this);
                        OnStepStarted?.Invoke(this, CurrentStep);
                        _onStageEntered?.Invoke(this, CurrentStep);
                    }
                }
                else
                {
                    QuestLogger.LogWarning(LogSubsystem.Tutorial, $"Cannot fire restore events: CurrentStep={CurrentStep?.DevName ?? "null"}, State={CurrentStep?.CurrentState.ToString() ?? "N/A"}");
                }
            }

            QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{DevName}' state restored: {state} at step {currentStepIndex}.");
        }

        #endregion

        #region Private Methods

        private void HandleStepCompleted(TutorialStepRuntime step)
        {
            OnStepCompleted?.Invoke(this, step);
            _onStageCompleted?.Invoke(this, step);
            _onProgressChanged?.Invoke(this);

            // Check if there are more steps
            int nextIndex = CurrentStepIndex + 1;
            if (nextIndex < Steps.Count)
            {
                CurrentStepIndex = nextIndex;
                Steps[nextIndex].StartStep();
                OnStepStarted?.Invoke(this, Steps[nextIndex]);
                _onStageEntered?.Invoke(this, Steps[nextIndex]);
            }
            else
            {
                // All steps complete
                CompleteTutorial();
            }
        }

        private void UnsubscribeFromStepEvents()
        {
            foreach (var step in Steps)
            {
                step.OnStepCompleted.RemoveListener(HandleStepCompleted);
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is TutorialRuntime other)
            {
                return TutorialId == other.TutorialId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return TutorialId.GetHashCode();
        }

        #endregion
    }
}
