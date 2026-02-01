using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.Objectives;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Runtime representation of a quest stage, managing task group execution and transitions.
    /// Created from a QuestStage (serialized data) at quest start.
    /// Implements <see cref="IStage"/> for unified objective system compatibility.
    /// </summary>
    public class QuestStageRuntime : IStage
    {
        #region Events

        /// <summary>
        /// Fired when this stage is entered.
        /// </summary>
        public UnityEvent<QuestStageRuntime> OnStageEntered = new();

        /// <summary>
        /// Fired when this stage completes successfully.
        /// </summary>
        public UnityEvent<QuestStageRuntime> OnStageCompleted = new();

        /// <summary>
        /// Fired when this stage fails.
        /// </summary>
        public UnityEvent<QuestStageRuntime> OnStageFailed = new();

        /// <summary>
        /// Fired when this stage is skipped.
        /// </summary>
        public UnityEvent<QuestStageRuntime> OnStageSkipped = new();

        /// <summary>
        /// Fired when stage progress changes (group advances, etc.).
        /// </summary>
        public UnityEvent<QuestStageRuntime> OnStageUpdated = new();

        /// <summary>
        /// Fired when a transition is ready to execute.
        /// The int parameter is the target stage index.
        /// </summary>
        public UnityEvent<QuestStageRuntime, int> OnTransitionReady = new();

        /// <summary>
        /// Fired when any task group in this stage starts.
        /// </summary>
        public UnityEvent<QuestStageRuntime, TaskGroupRuntime> OnGroupInStageStarted = new();

        /// <summary>
        /// Fired when any task group in this stage completes.
        /// </summary>
        public UnityEvent<QuestStageRuntime, TaskGroupRuntime> OnGroupInStageCompleted = new();

        /// <summary>
        /// Fired when any task group in this stage fails.
        /// </summary>
        public UnityEvent<QuestStageRuntime, TaskGroupRuntime> OnGroupInStageFailed = new();

        /// <summary>
        /// Fired when any task in this stage is updated.
        /// </summary>
        public UnityEvent<QuestStageRuntime, TaskRuntime> OnTaskInStageUpdated = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the serialized data for this stage.
        /// </summary>
        public QuestStage Data { get; }

        /// <summary>
        /// Gets the stage index.
        /// </summary>
        public int StageIndex => Data.StageIndex;

        /// <summary>
        /// Gets the stage name.
        /// </summary>
        public string StageName => Data.StageName;

        /// <summary>
        /// Gets the current state of this stage.
        /// </summary>
        public StageState CurrentState { get; private set; }

        /// <summary>
        /// Gets all task groups in this stage.
        /// </summary>
        public IReadOnlyList<TaskGroupRuntime> TaskGroups { get; }

        /// <summary>
        /// Gets the currently active task group, or null if stage is not in progress.
        /// </summary>
        public TaskGroupRuntime CurrentGroup =>
            _currentGroupIndex >= 0 && _currentGroupIndex < TaskGroups.Count
                ? TaskGroups[_currentGroupIndex]
                : null;

        /// <summary>
        /// Gets all tasks that are currently in progress.
        /// </summary>
        public IReadOnlyList<TaskRuntime> CurrentTasks =>
            CurrentGroup?.CurrentTasks ?? Array.Empty<TaskRuntime>();

        /// <summary>
        /// Gets all tasks across all groups in this stage (flattened).
        /// </summary>
        public IReadOnlyList<TaskRuntime> AllTasks => TaskGroups.SelectMany(g => g.Tasks).ToList();

        /// <summary>
        /// Gets the progress of this stage (0-1).
        /// </summary>
        public float Progress
        {
            get
            {
                if (TaskGroups.Count == 0) return CurrentState == StageState.Completed ? 1f : 0f;

                float totalProgress = 0f;
                int totalTaskCount = 0;

                foreach (var group in TaskGroups)
                {
                    int groupTaskCount = group.Tasks.Count;
                    totalProgress += group.Progress * groupTaskCount;
                    totalTaskCount += groupTaskCount;
                }

                return totalTaskCount > 0 ? totalProgress / totalTaskCount : 1f;
            }
        }

        /// <summary>
        /// Index of the currently active task group (-1 if not started).
        /// </summary>
        private int _currentGroupIndex = -1;

        /// <summary>
        /// Cached list of event-driven conditions and their callbacks for proper cleanup.
        /// </summary>
        private readonly List<(IConditionEventDriven Condition, System.Action Callback)> _activeConditionSubscriptions = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a runtime stage from serialized data.
        /// </summary>
        /// <param name="stageData">The serialized stage data.</param>
        public QuestStageRuntime(QuestStage stageData)
        {
            Data = stageData;
            CurrentState = StageState.NotReached;

            // Create runtime task groups from the stage data
            TaskGroups = stageData.TaskGroups
                .Where(g => g != null)
                .Select(groupData => new TaskGroupRuntime(groupData))
                .ToList();
        }

        #endregion

        #region Virtual Lifecycle Hooks

        /// <summary>
        /// Called after the stage enters InProgress state and subscriptions are set up,
        /// but before events fire. Override in subclasses for stage-specific enter behavior.
        /// </summary>
        protected virtual void OnEnterHook() { }

        /// <summary>
        /// Called when the stage is about to exit (complete, fail, or skip),
        /// before unsubscribing from events. Override in subclasses for stage-specific exit behavior.
        /// </summary>
        protected virtual void OnExitHook() { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Centralized state setter that automatically fires lifecycle hooks on transitions.
        /// Do NOT use for RestoreStageState (raw save/load) or constructor (initial state).
        /// </summary>
        private void SetState(StageState newState)
        {
            var oldState = CurrentState;

            // Fire exit hooks when leaving InProgress
            if (oldState == StageState.InProgress && newState != StageState.InProgress)
            {
                OnExitHook();
            }

            CurrentState = newState;

            // Fire enter hooks when entering InProgress
            if (newState == StageState.InProgress && oldState != StageState.InProgress)
            {
                OnEnterHook();
            }
        }

        /// <summary>
        /// Enters this stage and starts its first task group.
        /// </summary>
        public void Enter()
        {
            if (CurrentState == StageState.InProgress)
            {
                QuestLogger.LogVerbose(LogSubsystem.Stage, $"'{StageName}' already in progress");
                return;
            }

            SetState(StageState.InProgress);
            SubscribeToGroupEvents();
            SubscribeToConditionTransitions();

            // Start the first group
            _currentGroupIndex = 0;

            if (TaskGroups.Count > 0)
            {
                // Log stage start BEFORE starting the group (correct chronological order)
                QuestLogger.LogStart(LogSubsystem.Stage, "Stage", StageName);
                OnStageEntered.SafeInvoke(this);
                RaiseIStageOnEntered();
                TaskGroups[0].StartGroup();
            }
            else
            {
                QuestLogger.LogStart(LogSubsystem.Stage, "Stage", $"{StageName} (no groups)");
                OnStageEntered.SafeInvoke(this);
                RaiseIStageOnEntered();
                // If no task groups, immediately check for transitions
                CheckAndExecuteTransition();
            }
        }

        /// <summary>
        /// Completes this stage successfully.
        /// </summary>
        public void Complete()
        {
            if (CurrentState != StageState.InProgress) return;

            SetState(StageState.Completed);
            UnsubscribeFromAllEvents();

            QuestLogger.LogComplete(LogSubsystem.Stage, "Stage", StageName);
            OnStageCompleted.SafeInvoke(this);
            RaiseIStageOnCompleted();
            RaiseIStageOnExited();
        }

        /// <summary>
        /// Fails this stage.
        /// </summary>
        public void Fail()
        {
            if (CurrentState != StageState.InProgress) return;

            SetState(StageState.Failed);
            UnsubscribeFromAllEvents();

            QuestLogger.LogFail(LogSubsystem.Stage, "Stage", StageName);
            OnStageFailed.SafeInvoke(this);
            RaiseIStageOnFailed();
            RaiseIStageOnExited();
        }

        /// <summary>
        /// Skips this stage without completing it.
        /// </summary>
        public void Skip()
        {
            if (CurrentState == StageState.Completed || CurrentState == StageState.Skipped) return;

            bool wasInProgress = CurrentState == StageState.InProgress;

            SetState(StageState.Skipped);
            UnsubscribeFromAllEvents();

            QuestLogger.LogVerbose(LogSubsystem.Stage, $"'{StageName}' skipped");
            OnStageSkipped.SafeInvoke(this);

            // Raise IStage exited event if the stage was in progress (treated as completed for IStage)
            if (wasInProgress)
            {
                RaiseIStageOnExited();
            }
        }

        /// <summary>
        /// Resets this stage to its initial state.
        /// </summary>
        public void Reset()
        {
            UnsubscribeFromAllEvents();

            foreach (var group in TaskGroups)
            {
                group.ResetGroup();
            }

            _currentGroupIndex = -1;
            SetState(StageState.NotReached);
        }

        #region Save/Load Restoration

        /// <summary>
        /// Directly sets the stage state and group index without triggering events or side effects.
        /// Used during save/load restoration.
        /// </summary>
        /// <param name="state">The state to set.</param>
        /// <param name="groupIndex">The current group index to set.</param>
        public void RestoreStageState(StageState state, int groupIndex)
        {
            CurrentState = state;
            _currentGroupIndex = groupIndex;
        }

        /// <summary>
        /// Resumes a stage that was restored to InProgress state.
        /// Subscribes to events so the stage can respond to game events.
        /// Call this AFTER all task states have been restored.
        /// </summary>
        public void ResumeStage()
        {
            if (CurrentState == StageState.InProgress)
            {
                SubscribeToGroupEvents();
                SubscribeToConditionTransitions();

                // Fire lifecycle hooks on resume (same as entering a fresh stage)
                OnEnterHook();

                QuestLogger.LogVerbose(LogSubsystem.Stage, $"Stage '{StageName}' resumed from save");
            }
        }

        #endregion

        /// <summary>
        /// Checks if all task groups are completed.
        /// </summary>
        /// <returns>True if all groups are completed.</returns>
        public bool AreAllGroupsCompleted()
        {
            return TaskGroups.All(g => g.CurrentState == TaskGroupState.Completed);
        }

        /// <summary>
        /// Gets the valid transition for when groups complete, if any.
        /// </summary>
        /// <returns>The target stage index, or -1 if no valid transition.</returns>
        public int GetNextStageOnGroupsComplete()
        {
            var transition = Data.GetValidTransition(TransitionTrigger.OnGroupsComplete);
            return transition?.TargetStageIndex ?? -1;
        }

        /// <summary>
        /// Gets the valid transition for a manual trigger, if any.
        /// </summary>
        /// <returns>The target stage index, or -1 if no valid transition.</returns>
        public int GetNextStageOnManualTrigger()
        {
            var transition = Data.GetValidTransition(TransitionTrigger.Manual);
            return transition?.TargetStageIndex ?? -1;
        }

        #endregion

        #region Private Methods

        private void SubscribeToGroupEvents()
        {
            foreach (var group in TaskGroups)
            {
                group.OnGroupStarted.SafeSubscribe(HandleGroupStarted);
                group.OnGroupCompleted.SafeSubscribe(HandleGroupCompleted);
                group.OnGroupFailed.SafeSubscribe(HandleGroupFailed);
                group.OnTaskInGroupUpdated.SafeSubscribe(HandleTaskInGroupUpdated);
            }
        }

        private void UnsubscribeFromGroupEvents()
        {
            foreach (var group in TaskGroups)
            {
                group.OnGroupStarted.SafeUnsubscribe(HandleGroupStarted);
                group.OnGroupCompleted.SafeUnsubscribe(HandleGroupCompleted);
                group.OnGroupFailed.SafeUnsubscribe(HandleGroupFailed);
                group.OnTaskInGroupUpdated.SafeUnsubscribe(HandleTaskInGroupUpdated);
            }
        }

        private void SubscribeToConditionTransitions()
        {
            if (Data.Transitions == null) return;

            foreach (var transition in Data.Transitions)
            {
                if (transition.Trigger != TransitionTrigger.OnConditionsMet) continue;
                if (transition.Conditions == null) continue;

                foreach (var condition in transition.Conditions)
                {
                    if (condition is IConditionEventDriven eventDriven)
                    {
                        // Store the callback so we can properly unsubscribe later
                        System.Action callback = () => CheckConditionTransition(transition);
                        eventDriven.SubscribeToEvent(callback);
                        _activeConditionSubscriptions.Add((eventDriven, callback));
                    }
                }
            }
        }

        private void UnsubscribeFromConditionTransitions()
        {
            foreach (var (condition, callback) in _activeConditionSubscriptions)
            {
                condition.UnsubscribeFromEvent(callback);
            }
            _activeConditionSubscriptions.Clear();
        }

        private void UnsubscribeFromAllEvents()
        {
            UnsubscribeFromGroupEvents();
            UnsubscribeFromConditionTransitions();
        }

        private void HandleGroupStarted(TaskGroupRuntime group)
        {
            OnGroupInStageStarted.SafeInvoke(this, group);
        }

        private void HandleGroupCompleted(TaskGroupRuntime group)
        {
            OnGroupInStageCompleted.SafeInvoke(this, group);

            if (AreAllGroupsCompleted())
            {
                CheckAndExecuteTransition();
            }
            else
            {
                // Advance to next group
                _currentGroupIndex++;
                if (_currentGroupIndex < TaskGroups.Count)
                {
                    TaskGroups[_currentGroupIndex].StartGroup();
                }
                OnStageUpdated.SafeInvoke(this);
                RaiseIStageOnProgressChanged();
            }
        }

        private void HandleGroupFailed(TaskGroupRuntime group)
        {
            QuestLogger.LogFail(LogSubsystem.Group, "Group", group.GroupName);
            OnGroupInStageFailed.SafeInvoke(this, group);

            // Stage fails if any group fails
            Fail();
        }

        private void HandleTaskInGroupUpdated(TaskGroupRuntime group, TaskRuntime task)
        {
            OnTaskInStageUpdated.SafeInvoke(this, task);
            OnStageUpdated.SafeInvoke(this);
            RaiseIStageOnProgressChanged();
        }

        private void CheckConditionTransition(StageTransition transition)
        {
            if (CurrentState != StageState.InProgress) return;

            if (transition.EvaluateConditions())
            {
                // Apply transition effects before transitioning
                transition.ApplyEffects();

                QuestLogger.LogTransition(LogSubsystem.Stage, StageName, $"Stage {transition.TargetStageIndex}");
                OnTransitionReady.SafeInvoke(this, transition.TargetStageIndex);
            }
        }

        private void CheckAndExecuteTransition()
        {
            if (CurrentState != StageState.InProgress) return;

            // Check for terminal stage
            if (Data.IsTerminal)
            {
                Complete();
                return;
            }

            // Check if this stage requires player choice
            // If so, do NOT auto-transition - wait for player selection via QuestRuntime.SelectChoice()
            if (Data.RequiresPlayerChoice)
            {
                QuestLogger.LogVerbose(LogSubsystem.Stage, $"'{StageName}' awaiting player choice");
                // The stage remains InProgress until player makes a choice
                // QuestRuntime will fire OnChoicesAvailable when it handles this stage
                return;
            }

            // Check for OnGroupsComplete transition
            var transition = Data.GetValidTransition(TransitionTrigger.OnGroupsComplete);
            if (transition != null)
            {
                // Apply transition effects before transitioning
                transition.ApplyEffects();

                Complete();
                OnTransitionReady.SafeInvoke(this, transition.TargetStageIndex);
            }
            else
            {
                // No transition defined, stage is terminal by default
                Complete();
            }
        }

        #endregion

        #region IStage Explicit Implementation

        // Backing fields for IStage events
        private event Action<IStage> _onEntered;
        private event Action<IStage> _onProgressChanged;
        private event Action<IStage> _onCompleted;
        private event Action<IStage> _onFailed;
        private event Action<IStage> _onExited;

        /// <summary>
        /// Gets the index of this stage within its parent mission.
        /// </summary>
        int IStage.Index => StageIndex;

        /// <summary>
        /// Gets the unique identifier for this stage.
        /// Uses the stage name as the ID since QuestStage doesn't have a separate stageId field.
        /// </summary>
        string IStage.Id => StageName;

        /// <summary>
        /// Gets the current state mapped to ObjectiveState.
        /// </summary>
        ObjectiveState IStage.State => CurrentState switch
        {
            StageState.NotReached => ObjectiveState.NotStarted,
            StageState.InProgress => ObjectiveState.InProgress,
            StageState.Completed => ObjectiveState.Completed,
            StageState.Failed => ObjectiveState.Failed,
            StageState.Skipped => ObjectiveState.Completed, // Treat skipped as completed for IStage
            _ => ObjectiveState.NotStarted
        };

        /// <summary>
        /// Gets the progress of this stage (0-1).
        /// </summary>
        float IStage.Progress => Progress;

        /// <summary>
        /// Gets the objective groups contained in this stage.
        /// Returns TaskGroups cast to IObjectiveGroup (requires TaskGroupRuntime to implement IObjectiveGroup).
        /// </summary>
        IReadOnlyList<IObjectiveGroup> IStage.ObjectiveGroups =>
            TaskGroups.OfType<IObjectiveGroup>().ToList();

        /// <summary>
        /// Gets whether this stage is a terminal (end) stage.
        /// </summary>
        bool IStage.IsTerminal => Data.IsTerminal;

        /// <summary>
        /// Gets whether this stage is optional.
        /// </summary>
        bool IStage.IsOptional => Data.IsOptional;

        /// <summary>
        /// Gets whether this stage is hidden from the player.
        /// </summary>
        bool IStage.IsHidden => Data.IsHidden;

        /// <summary>
        /// Fired when the stage is entered (becomes active).
        /// </summary>
        event Action<IStage> IStage.OnEntered
        {
            add => _onEntered += value;
            remove => _onEntered -= value;
        }

        /// <summary>
        /// Fired when the stage's progress changes.
        /// </summary>
        event Action<IStage> IStage.OnProgressChanged
        {
            add => _onProgressChanged += value;
            remove => _onProgressChanged -= value;
        }

        /// <summary>
        /// Fired when the stage is completed.
        /// </summary>
        event Action<IStage> IStage.OnCompleted
        {
            add => _onCompleted += value;
            remove => _onCompleted -= value;
        }

        /// <summary>
        /// Fired when the stage fails.
        /// </summary>
        event Action<IStage> IStage.OnFailed
        {
            add => _onFailed += value;
            remove => _onFailed -= value;
        }

        /// <summary>
        /// Fired when the stage is exited (no longer active).
        /// </summary>
        event Action<IStage> IStage.OnExited
        {
            add => _onExited += value;
            remove => _onExited -= value;
        }

        /// <summary>
        /// Raises the IStage.OnEntered event.
        /// Called internally when stage is entered.
        /// </summary>
        private void RaiseIStageOnEntered() => _onEntered?.Invoke(this);

        /// <summary>
        /// Raises the IStage.OnProgressChanged event.
        /// Called internally when stage progress changes.
        /// </summary>
        private void RaiseIStageOnProgressChanged() => _onProgressChanged?.Invoke(this);

        /// <summary>
        /// Raises the IStage.OnCompleted event.
        /// Called internally when stage completes.
        /// </summary>
        private void RaiseIStageOnCompleted() => _onCompleted?.Invoke(this);

        /// <summary>
        /// Raises the IStage.OnFailed event.
        /// Called internally when stage fails.
        /// </summary>
        private void RaiseIStageOnFailed() => _onFailed?.Invoke(this);

        /// <summary>
        /// Raises the IStage.OnExited event.
        /// Called internally when stage is exited.
        /// </summary>
        private void RaiseIStageOnExited() => _onExited?.Invoke(this);

        #endregion
    }
}
