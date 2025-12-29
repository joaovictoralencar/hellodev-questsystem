using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
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
    /// </summary>
    public class QuestStageRuntime
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
        public List<TaskGroupRuntime> TaskGroups { get; }

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
        public List<TaskRuntime> AllTasks => TaskGroups.SelectMany(g => g.Tasks).ToList();

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
        /// Cached list of event-driven conditions for cleanup.
        /// </summary>
        private readonly List<IConditionEventDriven> _activeConditionSubscriptions = new();

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

        #region Public Methods

        /// <summary>
        /// Enters this stage and starts its first task group.
        /// </summary>
        public void Enter()
        {
            if (CurrentState == StageState.InProgress)
            {
                QuestLogger.Log($"Stage '{StageName}' is already in progress.");
                return;
            }

            CurrentState = StageState.InProgress;
            SubscribeToGroupEvents();
            SubscribeToConditionTransitions();

            // Start the first group
            _currentGroupIndex = 0;
            if (TaskGroups.Count > 0)
            {
                TaskGroups[0].StartGroup();
                QuestLogger.Log($"Stage '{StageName}' entered. First group: '{TaskGroups[0].GroupName}'");
            }
            else
            {
                QuestLogger.Log($"Stage '{StageName}' entered with no task groups.");
                // If no task groups, immediately check for transitions
                CheckAndExecuteTransition();
            }

            OnStageEntered.SafeInvoke(this);
        }

        /// <summary>
        /// Completes this stage successfully.
        /// </summary>
        public void Complete()
        {
            if (CurrentState != StageState.InProgress) return;

            CurrentState = StageState.Completed;
            UnsubscribeFromAllEvents();

            QuestLogger.Log($"Stage '{StageName}' completed.");
            OnStageCompleted.SafeInvoke(this);
        }

        /// <summary>
        /// Fails this stage.
        /// </summary>
        public void Fail()
        {
            if (CurrentState != StageState.InProgress) return;

            CurrentState = StageState.Failed;
            UnsubscribeFromAllEvents();

            QuestLogger.Log($"Stage '{StageName}' failed.");
            OnStageFailed.SafeInvoke(this);
        }

        /// <summary>
        /// Skips this stage without completing it.
        /// </summary>
        public void Skip()
        {
            if (CurrentState == StageState.Completed || CurrentState == StageState.Skipped) return;

            CurrentState = StageState.Skipped;
            UnsubscribeFromAllEvents();

            QuestLogger.Log($"Stage '{StageName}' skipped.");
            OnStageSkipped.SafeInvoke(this);
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
            CurrentState = StageState.NotReached;

            QuestLogger.Log($"Stage '{StageName}' reset.");
        }

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
                        eventDriven.SubscribeToEvent(() => CheckConditionTransition(transition));
                        _activeConditionSubscriptions.Add(eventDriven);
                    }
                }
            }
        }

        private void UnsubscribeFromConditionTransitions()
        {
            foreach (var eventDriven in _activeConditionSubscriptions)
            {
                eventDriven.UnsubscribeFromEvent();
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
            QuestLogger.Log($"Group '{group.GroupName}' started in stage '{StageName}'.");
            OnGroupInStageStarted.SafeInvoke(this, group);
        }

        private void HandleGroupCompleted(TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' completed in stage '{StageName}'.");
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
                    QuestLogger.Log($"Starting next group '{TaskGroups[_currentGroupIndex].GroupName}' in stage '{StageName}'.");
                }
                OnStageUpdated.SafeInvoke(this);
            }
        }

        private void HandleGroupFailed(TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' failed in stage '{StageName}'.");
            OnGroupInStageFailed.SafeInvoke(this, group);

            // Stage fails if any group fails
            Fail();
        }

        private void HandleTaskInGroupUpdated(TaskGroupRuntime group, TaskRuntime task)
        {
            OnTaskInStageUpdated.SafeInvoke(this, task);
            OnStageUpdated.SafeInvoke(this);
        }

        private void CheckConditionTransition(StageTransition transition)
        {
            if (CurrentState != StageState.InProgress) return;

            if (transition.EvaluateConditions())
            {
                QuestLogger.Log($"Condition transition triggered in stage '{StageName}'. Target: {transition.TargetStageIndex}");
                OnTransitionReady.SafeInvoke(this, transition.TargetStageIndex);
            }
        }

        private void CheckAndExecuteTransition()
        {
            if (CurrentState != StageState.InProgress) return;

            // Check for terminal stage
            if (Data.IsTerminal)
            {
                QuestLogger.Log($"Stage '{StageName}' is terminal. Completing.");
                Complete();
                return;
            }

            // Check if this stage requires player choice
            // If so, do NOT auto-transition - wait for player selection via QuestRuntime.SelectChoice()
            if (Data.RequiresPlayerChoice)
            {
                QuestLogger.Log($"Stage '{StageName}' requires player choice. Waiting for selection.");
                // The stage remains InProgress until player makes a choice
                // QuestRuntime will fire OnChoicesAvailable when it handles this stage
                return;
            }

            // Check for OnGroupsComplete transition
            int nextStage = GetNextStageOnGroupsComplete();
            if (nextStage >= 0)
            {
                QuestLogger.Log($"Groups complete transition in stage '{StageName}'. Target: {nextStage}");
                Complete();
                OnTransitionReady.SafeInvoke(this, nextStage);
            }
            else
            {
                // No transition defined, stage is terminal by default
                QuestLogger.Log($"Stage '{StageName}' has no valid transition. Treating as terminal.");
                Complete();
            }
        }

        #endregion
    }
}
