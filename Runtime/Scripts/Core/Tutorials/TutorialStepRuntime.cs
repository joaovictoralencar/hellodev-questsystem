using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.Logging;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Tutorials
{
    /// <summary>
    /// Runtime representation of a tutorial step.
    /// Implements IStage for unified objective system compatibility.
    /// Supports simple steps, multi-step (substeps), and count-based steps.
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

        /// <summary>
        /// Fired when a substep is completed.
        /// </summary>
        public UnityEvent<TutorialStepRuntime, TutorialSubstep_SO> OnSubstepCompleted = new();

        /// <summary>
        /// Fired when count progress changes for count-based steps.
        /// </summary>
        public UnityEvent<TutorialStepRuntime, int, int> OnCountProgressChanged = new();

        #endregion

        #region IStage Backing Fields

        private event Action<IStage> _onEntered;
        private event Action<IStage> _onProgressChanged;
        private event Action<IStage> _onCompleted;
        private event Action<IStage> _onFailed;
        private event Action<IStage> _onExited;

        #endregion

        #region Private Fields

        private readonly HashSet<Guid> _completedSubstepIds = new();
        private readonly Dictionary<Guid, Action> _substepCallbacks = new();
        private int _currentCount;

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
        public State CurrentState { get; private set; }

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
        /// Gets the current count for count-based steps.
        /// </summary>
        public int CurrentCount => _currentCount;

        /// <summary>
        /// Gets the required count for count-based steps.
        /// </summary>
        public int RequiredCount => Data.RequiredCount;

        /// <summary>
        /// Gets the number of completed substeps.
        /// </summary>
        public int CompletedSubstepCount => _completedSubstepIds.Count;

        /// <summary>
        /// Gets the total number of substeps.
        /// </summary>
        public int TotalSubstepCount => Data.SubstepCount;

        /// <summary>
        /// Gets whether this step has substeps.
        /// </summary>
        public bool HasSubsteps => Data.HasSubsteps;

        /// <summary>
        /// Gets whether this is a count-based step.
        /// </summary>
        public bool IsCountBased => Data.IsCountBased;

        /// <summary>
        /// Gets the currently active substep (the first incomplete one), or null if all complete.
        /// </summary>
        public TutorialSubstep_SO CurrentSubstep
        {
            get
            {
                if (!HasSubsteps) return null;
                return Data.Substeps.FirstOrDefault(s => !_completedSubstepIds.Contains(s.SubstepId));
            }
        }

        /// <summary>
        /// Gets the index of the current substep (0-based), or -1 if no substeps or all complete.
        /// </summary>
        public int CurrentSubstepIndex
        {
            get
            {
                if (!HasSubsteps) return -1;
                for (int i = 0; i < Data.Substeps.Count; i++)
                {
                    if (!_completedSubstepIds.Contains(Data.Substeps[i].SubstepId))
                        return i;
                }
                return -1;
            }
        }

        /// <summary>
        /// Gets the progress of this step (0-1).
        /// </summary>
        public float Progress
        {
            get
            {
                if (CurrentState == State.Completed) return 1f;
                if (CurrentState != State.InProgress) return 0f;

                // Substep-based progress
                if (HasSubsteps)
                {
                    return TotalSubstepCount == 0 ? 1f : (float)CompletedSubstepCount / TotalSubstepCount;
                }

                // Count-based progress
                if (IsCountBased)
                {
                    return RequiredCount == 0 ? 1f : (float)_currentCount / RequiredCount;
                }

                // Timer-based progress
                if (Data.IsTimedStep && Data.Duration > 0)
                {
                    return Math.Min(1f, ElapsedTime / Data.Duration);
                }

                // Simple step - 50% when in progress
                return 0.5f;
            }
        }

        #endregion

        #region IStage Implementation

        /// <inheritdoc />
        int IStage.Index => StepIndex;

        /// <inheritdoc />
        string IStage.Name => StepId.ToString();

        /// <inheritdoc />
        State IStage.State => CurrentState;

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
            CurrentState = State.NotStarted;
            WasSkipped = false;
            ElapsedTime = 0f;
            _currentCount = 0;
        }

        #endregion

        #region Virtual Lifecycle Hooks

        /// <summary>
        /// Called after the step enters InProgress state and subscriptions are set up,
        /// but before events fire. Override in subclasses for step-specific enter behavior.
        /// </summary>
        protected virtual void OnEnterHook() { }

        /// <summary>
        /// Called when the step is about to exit (complete, fail, or skip),
        /// before unsubscribing from conditions. Override in subclasses for step-specific exit behavior.
        /// </summary>
        protected virtual void OnExitHook() { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Centralized state setter that automatically fires lifecycle hooks on transitions.
        /// Do NOT use for RestoreStepState (raw save/load) or constructor (initial state).
        /// </summary>
        private void SetState(State newState)
        {
            State oldState = CurrentState;

            // Fire exit hooks when leaving InProgress
            if (oldState == State.InProgress && newState != State.InProgress)
            {
                OnExitHook();
                TutorialManager.Instance?.NotifyStepExiting(this);
            }

            CurrentState = newState;

            // Fire enter hooks when entering InProgress
            if (newState == State.InProgress && oldState != State.InProgress)
            {
                OnEnterHook();
                TutorialManager.Instance?.NotifyStepEntering(this);
            }
        }

        /// <summary>
        /// Starts this tutorial step.
        /// </summary>
        public void StartStep()
        {
            if (CurrentState != State.NotStarted) return;

            SetState(State.InProgress);

            // Subscribe based on step type
            if (HasSubsteps)
            {
                SubscribeToSubsteps();
            }
            else if (IsCountBased)
            {
                SubscribeToCountCondition();
            }
            else if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                // Simple step with condition
                eventCondition.SubscribeToEvent(CompleteStep);
            }

            Logger.Log("Tutorial", $"Tutorial step '{DevName}' started.");

            OnStepStarted?.Invoke(this);
            _onEntered?.Invoke(this);
        }

        /// <summary>
        /// Completes this tutorial step.
        /// </summary>
        public void CompleteStep()
        {
            if (CurrentState != State.InProgress) return;

            UnsubscribeFromAllConditions();

            SetState(State.Completed);
            Logger.Log("Tutorial", $"Tutorial step '{DevName}' completed.");

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
            if (CurrentState != State.InProgress) return false;
            if (!Data.CanSkip) return false;

            UnsubscribeFromAllConditions();

            WasSkipped = true;
            SetState(State.Completed);
            Logging.Logger.Log("Tutorial", $"Tutorial step '{DevName}' skipped.");

            _onProgressChanged?.Invoke(this);
            OnStepSkipped?.Invoke(this);
            OnStepCompleted?.Invoke(this);
            _onCompleted?.Invoke(this);
            _onExited?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Skips the current substep (if the step has substeps and allows skipping).
        /// </summary>
        /// <returns>True if a substep was skipped, false if not applicable or not allowed.</returns>
        public bool SkipCurrentSubstep()
        {
            if (CurrentState != State.InProgress) return false;
            if (!Data.CanSkip) return false;
            if (!HasSubsteps) return false;

            TutorialSubstep_SO currentSubstep = CurrentSubstep;
            if (currentSubstep == null) return false;

            // Mark the substep as completed
            _completedSubstepIds.Add(currentSubstep.SubstepId);

            // Unsubscribe from this substep's condition
            if (currentSubstep.CompletionCondition is IConditionEventDriven eventCondition &&
                _substepCallbacks.TryGetValue(currentSubstep.SubstepId, out Action callback))
            {
                eventCondition.UnsubscribeFromEvent(callback);
                _substepCallbacks.Remove(currentSubstep.SubstepId);
            }

            Logger.Log("Tutorial", $"Tutorial step '{DevName}' substep '{currentSubstep.DevName}' skipped ({CompletedSubstepCount}/{TotalSubstepCount}).");

            OnSubstepCompleted?.Invoke(this, currentSubstep);
            _onProgressChanged?.Invoke(this);

            // Check if all substeps are complete
            if (CompletedSubstepCount >= TotalSubstepCount)
            {
                CompleteStep();
            }

            return true;
        }

        /// <summary>
        /// Increments the count for count-based steps (if the step allows skipping).
        /// </summary>
        /// <returns>True if the count was incremented, false if not applicable or not allowed.</returns>
        public bool IncrementCount()
        {
            if (CurrentState != State.InProgress) return false;
            if (!Data.CanSkip) return false;
            if (!IsCountBased) return false;
            if (_currentCount >= RequiredCount) return false;

            _currentCount++;
            Logging.Logger.Log("Tutorial", $"Tutorial step '{DevName}' count manually incremented: {_currentCount}/{RequiredCount}.");

            OnCountProgressChanged?.Invoke(this, _currentCount, RequiredCount);
            _onProgressChanged?.Invoke(this);

            // Check if count requirement is met
            if (_currentCount >= RequiredCount)
            {
                CompleteStep();
            }

            return true;
        }

        /// <summary>
        /// Updates the elapsed time for timed steps.
        /// </summary>
        /// <param name="deltaTime">Time since last update.</param>
        public void UpdateTime(float deltaTime)
        {
            if (CurrentState != State.InProgress) return;
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
            if (CurrentState != State.InProgress) return;

            UnsubscribeFromAllConditions();

            SetState(State.Failed);
            Logging.Logger.Log("Tutorial", $"Tutorial step '{DevName}' failed.");

            _onProgressChanged?.Invoke(this);
            _onFailed?.Invoke(this);
            _onExited?.Invoke(this);
        }

        /// <summary>
        /// Resets this step to its initial state.
        /// </summary>
        public void ResetStep()
        {
            UnsubscribeFromAllConditions();

            SetState(State.NotStarted);
            WasSkipped = false;
            ElapsedTime = 0f;
            _currentCount = 0;
            _completedSubstepIds.Clear();
            _substepCallbacks.Clear();
        }

        /// <summary>
        /// Checks if a specific substep is completed.
        /// </summary>
        /// <param name="substep">The substep to check.</param>
        /// <returns>True if the substep is completed.</returns>
        public bool IsSubstepCompleted(TutorialSubstep_SO substep)
        {
            return substep != null && _completedSubstepIds.Contains(substep.SubstepId);
        }

        /// <summary>
        /// Gets a list of completed substep IDs.
        /// </summary>
        public IReadOnlyCollection<Guid> GetCompletedSubstepIds()
        {
            return _completedSubstepIds;
        }

        #endregion

        #region Private Methods

        private void SubscribeToSubsteps()
        {
            foreach (TutorialSubstep_SO substep in Data.Substeps)
            {
                if (substep?.CompletionCondition is IConditionEventDriven eventCondition)
                {
                    // Create a callback that captures this specific substep
                    Action callback = () => OnSubstepConditionMet(substep);
                    _substepCallbacks[substep.SubstepId] = callback;
                    eventCondition.SubscribeToEvent(callback);
                }
            }
        }

        private void SubscribeToCountCondition()
        {
            if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.SubscribeToEvent(OnCountConditionMet);
            }
        }

        private void UnsubscribeFromAllConditions()
        {
            // Unsubscribe from substep conditions
            if (HasSubsteps)
            {
                foreach (TutorialSubstep_SO substep in Data.Substeps)
                {
                    if (substep?.CompletionCondition is IConditionEventDriven eventCondition &&
                        _substepCallbacks.TryGetValue(substep.SubstepId, out Action callback))
                    {
                        eventCondition.UnsubscribeFromEvent(callback);
                    }
                }
                _substepCallbacks.Clear();
            }
            // Unsubscribe from count-based or simple condition
            else if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                if (IsCountBased)
                {
                    eventCondition.UnsubscribeFromEvent(OnCountConditionMet);
                }
                else
                {
                    eventCondition.UnsubscribeFromEvent(CompleteStep);
                }
            }
        }

        private void OnSubstepConditionMet(TutorialSubstep_SO substep)
        {
            if (CurrentState != State.InProgress) return;
            if (_completedSubstepIds.Contains(substep.SubstepId)) return;

            _completedSubstepIds.Add(substep.SubstepId);

            // Unsubscribe from this substep's condition
            if (substep.CompletionCondition is IConditionEventDriven eventCondition &&
                _substepCallbacks.TryGetValue(substep.SubstepId, out Action callback))
            {
                eventCondition.UnsubscribeFromEvent(callback);
                _substepCallbacks.Remove(substep.SubstepId);
            }

            Logging.Logger.Log("Tutorial", $"Tutorial step '{DevName}' substep '{substep.DevName}' completed ({CompletedSubstepCount}/{TotalSubstepCount}).");

            OnSubstepCompleted?.Invoke(this, substep);
            _onProgressChanged?.Invoke(this);

            // Check if all substeps are complete
            if (CompletedSubstepCount >= TotalSubstepCount)
            {
                CompleteStep();
            }
        }

        private void OnCountConditionMet()
        {
            if (CurrentState != State.InProgress) return;
            if (_currentCount >= RequiredCount) return;

            _currentCount++;
            Logging.Logger.Log("Tutorial", $"Tutorial step '{DevName}' count progress: {_currentCount}/{RequiredCount}.");

            OnCountProgressChanged?.Invoke(this, _currentCount, RequiredCount);
            _onProgressChanged?.Invoke(this);

            // Check if count requirement is met
            if (_currentCount >= RequiredCount)
            {
                CompleteStep();
            }
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Restores this step's state from a save.
        /// Sets state directly without triggering events or condition subscriptions.
        /// </summary>
        /// <param name="state">The state to restore to.</param>
        /// <param name="elapsedTime">The elapsed time to restore (for timed steps).</param>
        /// <param name="currentCount">The current count to restore (for count-based steps).</param>
        /// <param name="completedSubstepIds">The completed substep IDs to restore.</param>
        public void RestoreStepState(State state, float elapsedTime, int currentCount = 0, IEnumerable<Guid> completedSubstepIds = null)
        {
            CurrentState = state;
            ElapsedTime = elapsedTime;
            _currentCount = currentCount;

            _completedSubstepIds.Clear();
            if (completedSubstepIds != null)
            {
                foreach (Guid id in completedSubstepIds)
                {
                    _completedSubstepIds.Add(id);
                }
            }

            Logger.LogVerbose("Tutorial", $"Step '{DevName}' state restored: {state}");
        }

        /// <summary>
        /// Overload for backward compatibility.
        /// </summary>
        public void RestoreStepState(State state, float elapsedTime)
        {
            RestoreStepState(state, elapsedTime, 0, null);
        }

        /// <summary>
        /// Resumes an in-progress step after loading.
        /// Re-subscribes to completion condition events and fires lifecycle hooks.
        /// </summary>
        public void ResumeStep()
        {
            if (CurrentState != State.InProgress) return;

            if (HasSubsteps)
            {
                // Only subscribe to incomplete substeps
                foreach (TutorialSubstep_SO substep in Data.Substeps)
                {
                    if (!_completedSubstepIds.Contains(substep.SubstepId) &&
                        substep?.CompletionCondition is IConditionEventDriven eventCondition)
                    {
                        Action callback = () => OnSubstepConditionMet(substep);
                        _substepCallbacks[substep.SubstepId] = callback;
                        eventCondition.SubscribeToEvent(callback);
                    }
                }
                Logger.LogVerbose("Tutorial", $"Step '{DevName}' resumed substep subscriptions ({TotalSubstepCount - CompletedSubstepCount} remaining).");
            }
            else if (Data.CompletionCondition is IConditionEventDriven eventCondition)
            {
                if (IsCountBased)
                {
                    eventCondition.SubscribeToEvent(OnCountConditionMet);
                }
                else
                {
                    eventCondition.SubscribeToEvent(CompleteStep);
                }
                Logger.LogVerbose("Tutorial", $"Step '{DevName}' resumed condition subscription.");
            }

            // Fire lifecycle hooks on resume (same as entering a fresh step)
            OnEnterHook();
            TutorialManager.Instance?.NotifyStepEntering(this);
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
