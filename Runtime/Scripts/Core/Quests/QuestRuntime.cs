using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Quests
{
    /// <summary>
    /// Represents a runtime quest instance. This class provides the core structure and
    /// state management for all quests. Supports both legacy (flat task groups) and
    /// stage-based quests (Skyrim-style multi-phase).
    /// </summary>
    public class QuestRuntime
    {
        #region Events

        /// <summary>Fired when the quest starts.</summary>
        public UnityEvent<QuestRuntime> OnQuestStarted = new();

        /// <summary>Fired when the quest completes successfully.</summary>
        public UnityEvent<QuestRuntime> OnQuestCompleted = new();

        /// <summary>Fired when the quest fails.</summary>
        public UnityEvent<QuestRuntime> OnQuestFailed = new();

        /// <summary>Fired when the quest is reset and restarted.</summary>
        public UnityEvent<QuestRuntime> OnQuestRestarted = new();

        /// <summary>Fired when quest progress changes (stage advances, group advances, task completes, etc.).</summary>
        public UnityEvent<QuestRuntime> OnQuestUpdated = new();

        /// <summary>Fired when any task in this quest starts.</summary>
        public UnityEvent<TaskRuntime> OnAnyTaskStarted = new();

        /// <summary>Fired when any task in this quest is updated.</summary>
        public UnityEvent<TaskRuntime> OnAnyTaskUpdated = new();

        /// <summary>Fired when any task in this quest completes.</summary>
        public UnityEvent<TaskRuntime> OnAnyTaskCompleted = new();

        /// <summary>Fired when any task in this quest fails.</summary>
        public UnityEvent<TaskRuntime> OnAnyTaskFailed = new();

        /// <summary>Fired when a stage is entered.</summary>
        public UnityEvent<QuestRuntime, QuestStageRuntime> OnStageEntered = new();

        /// <summary>Fired when a stage is completed.</summary>
        public UnityEvent<QuestRuntime, QuestStageRuntime> OnStageCompleted = new();

        /// <summary>Fired when a stage transition occurs.</summary>
        public UnityEvent<QuestRuntime, int, int> OnStageTransition = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique identifier for this quest.
        /// </summary>
        public Guid QuestId { get; }

        /// <summary>
        /// Gets the current state of this quest.
        /// </summary>
        public QuestState CurrentState { get; private set; }

        /// <summary>
        /// Gets the ScriptableObject data for this quest.
        /// </summary>
        public Quest_SO QuestData { get; }

        /// <summary>
        /// Gets all stages in this quest.
        /// </summary>
        public List<QuestStageRuntime> Stages { get; }

        /// <summary>
        /// Gets the currently active stage, or null if quest is not in progress.
        /// </summary>
        public QuestStageRuntime CurrentStage { get; private set; }

        /// <summary>
        /// Gets the index of the current stage.
        /// </summary>
        public int CurrentStageIndex => CurrentStage?.StageIndex ?? -1;

        /// <summary>
        /// Gets all task groups across all stages (flattened for backward compatibility).
        /// </summary>
        public List<TaskGroupRuntime> TaskGroups => Stages.SelectMany(s => s.TaskGroups).ToList();

        /// <summary>
        /// Gets the currently active task group from the current stage.
        /// </summary>
        public TaskGroupRuntime CurrentGroup => CurrentStage?.CurrentGroup;

        /// <summary>
        /// Gets all tasks that are currently in progress (can be multiple for parallel groups).
        /// </summary>
        public IReadOnlyList<TaskRuntime> CurrentTasks =>
            CurrentStage?.CurrentTasks ?? Array.Empty<TaskRuntime>();

        /// <summary>
        /// Gets the first currently in-progress task, or null if none.
        /// Use CurrentTasks for parallel groups where multiple tasks may be active.
        /// </summary>
        public TaskRuntime CurrentTask => CurrentTasks.FirstOrDefault();

        /// <summary>
        /// Gets all tasks across all stages and groups (flattened list for backward compatibility).
        /// </summary>
        public List<TaskRuntime> Tasks => Stages.SelectMany(s => s.AllTasks).ToList();

        /// <summary>
        /// Gets the overall progress of this quest (0-1).
        /// Calculated as the weighted average of stage progress.
        /// </summary>
        public float CurrentProgress
        {
            get
            {
                if (Stages.Count == 0) return CurrentState == QuestState.Completed ? 1f : 0f;

                float totalProgress = 0f;
                int totalTaskCount = 0;

                foreach (var stage in Stages)
                {
                    int stageTaskCount = stage.AllTasks.Count;
                    totalProgress += stage.Progress * stageTaskCount;
                    totalTaskCount += stageTaskCount;
                }

                return totalTaskCount > 0 ? totalProgress / totalTaskCount : 1f;
            }
        }

        /// <summary>
        /// Dictionary tracking which branch decisions were made (for branching quests).
        /// Key is branch ID, value is choice ID.
        /// </summary>
        public Dictionary<string, string> BranchDecisions { get; } = new();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestRuntime"/> class.
        /// </summary>
        /// <param name="questData">The quest data.</param>
        /// <remarks>
        /// This constructor creates a runtime instance from a Quest_SO asset.
        /// Creates QuestStageRuntime instances from the quest's stages.
        /// Legacy quests (without stages) are auto-wrapped in a single stage.
        /// </remarks>
        public QuestRuntime(Quest_SO questData)
        {
            QuestData = questData;
            QuestId = questData.QuestId;
            CurrentState = QuestState.NotStarted;

            // Create runtime stages from the quest data
            // Quest_SO.Stages already handles legacy mode by returning a single auto-generated stage
            Stages = questData.Stages
                .Select(stageData => new QuestStageRuntime(stageData))
                .ToList();
        }

        private void UpdateQuestState(QuestState newState)
        {
            CurrentState = newState;
        }

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// Starts the first stage.
        /// </summary>
        public void StartQuest()
        {
            if (CurrentState != QuestState.NotStarted)
            {
                QuestLogger.Log($"Quest '{QuestData.DevName}' is already in progress.");
                return;
            }

            UnsubscribeFromStartConditions();
            SubscribeToAllEvents();

            UpdateQuestState(QuestState.InProgress);

            // Start the first stage (lowest index)
            var firstStage = GetStageByIndex(GetFirstStageIndex());
            if (firstStage != null)
            {
                TransitionToStage(firstStage);
                QuestLogger.Log($"Quest '{QuestData.DevName}' started. First stage: '{firstStage.StageName}'");
            }
            else
            {
                QuestLogger.Log($"Quest '{QuestData.DevName}' started with no stages.");
            }

            OnQuestStarted.SafeInvoke(this);
        }

        /// <summary>
        /// Marks the quest as completed, changing its state to Completed.
        /// Distributes all rewards and fires completion events.
        /// </summary>
        public void CompleteQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                UnsubscribeFromAllEvents();

                // Distribute rewards
                if (QuestData.Rewards != null)
                {
                    foreach (var reward in QuestData.Rewards)
                    {
                        if (reward.RewardType != null)
                        {
                            reward.RewardType.GiveReward(reward.Amount);
                            QuestLogger.Log($"Distributed reward: {reward.RewardType.name} x{reward.Amount}");
                        }
                    }
                }

                OnQuestUpdated.SafeInvoke(this);
                UpdateQuestState(QuestState.Completed);
                OnQuestCompleted.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Marks the quest as failed, changing its state to Failed.
        /// </summary>
        public void FailQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                UpdateQuestState(QuestState.Failed);
                UnsubscribeFromAllEvents();
                OnQuestFailed.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Resets the quest to its initial state and restarts it.
        /// </summary>
        public void ResetQuest()
        {
            UnsubscribeFromAllEvents();

            // Reset all stages
            foreach (var stage in Stages)
            {
                stage.Reset();
            }

            CurrentStage = null;
            BranchDecisions.Clear();
            UpdateQuestState(QuestState.NotStarted);
            StartQuest();
            OnQuestRestarted.SafeInvoke(this);
        }

        #region Stage Management

        /// <summary>
        /// Attempts to set the quest to a specific stage by index.
        /// Used for manual stage transitions (e.g., from dialogue).
        /// </summary>
        /// <param name="stageIndex">The index of the stage to transition to.</param>
        /// <returns>True if transition was successful.</returns>
        public bool TrySetStage(int stageIndex)
        {
            if (CurrentState != QuestState.InProgress)
            {
                QuestLogger.LogWarning($"Cannot set stage on quest '{QuestData.DevName}' - not in progress.");
                return false;
            }

            var targetStage = GetStageByIndex(stageIndex);
            if (targetStage == null)
            {
                QuestLogger.LogWarning($"Stage index {stageIndex} not found in quest '{QuestData.DevName}'.");
                return false;
            }

            int previousIndex = CurrentStageIndex;
            TransitionToStage(targetStage);
            OnStageTransition.SafeInvoke(this, previousIndex, stageIndex);

            return true;
        }

        /// <summary>
        /// Gets a stage by its index.
        /// </summary>
        /// <param name="stageIndex">The stage index.</param>
        /// <returns>The stage, or null if not found.</returns>
        public QuestStageRuntime GetStageByIndex(int stageIndex)
        {
            return Stages.FirstOrDefault(s => s.StageIndex == stageIndex);
        }

        /// <summary>
        /// Gets the first stage index (lowest number).
        /// </summary>
        private int GetFirstStageIndex()
        {
            return Stages.Count > 0 ? Stages.Min(s => s.StageIndex) : -1;
        }

        /// <summary>
        /// Transitions to a new stage.
        /// </summary>
        private void TransitionToStage(QuestStageRuntime targetStage)
        {
            // Complete current stage if it's still in progress
            if (CurrentStage?.CurrentState == StageState.InProgress)
            {
                CurrentStage.Complete();
                OnStageCompleted.SafeInvoke(this, CurrentStage);
            }

            CurrentStage = targetStage;
            targetStage.Enter();
            OnStageEntered.SafeInvoke(this, targetStage);
            NotifyQuestUpdated();
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToAllEvents()
        {
            // Subscribe to stage events
            foreach (var stage in Stages)
            {
                stage.OnStageEntered.SafeSubscribe(HandleStageEntered);
                stage.OnStageCompleted.SafeSubscribe(HandleStageCompleted);
                stage.OnStageFailed.SafeSubscribe(HandleStageFailed);
                stage.OnTransitionReady.SafeSubscribe(HandleTransitionReady);
                stage.OnTaskInStageUpdated.SafeSubscribe(HandleTaskInStageUpdated);
                stage.OnGroupInStageStarted.SafeSubscribe(HandleGroupInStageStarted);
                stage.OnGroupInStageCompleted.SafeSubscribe(HandleGroupInStageCompleted);
                stage.OnGroupInStageFailed.SafeSubscribe(HandleGroupInStageFailed);
            }

            // Subscribe to global task failure conditions
            if (QuestData.GlobalTaskFailureConditions != null)
            {
                foreach (Condition_SO condition in QuestData.GlobalTaskFailureConditions)
                {
                    if (condition is IConditionEventDriven conditionEventDriven)
                    {
                        conditionEventDriven.SubscribeToEvent(HandleGlobalTaskFailure);
                        QuestLogger.Log($"Subscribed to global task failure condition '{condition.name}' for quest '{QuestData.DevName}'.");
                    }
                }
            }
        }

        private void UnsubscribeFromAllEvents()
        {
            // Unsubscribe from stage events
            foreach (var stage in Stages)
            {
                stage.OnStageEntered.SafeUnsubscribe(HandleStageEntered);
                stage.OnStageCompleted.SafeUnsubscribe(HandleStageCompleted);
                stage.OnStageFailed.SafeUnsubscribe(HandleStageFailed);
                stage.OnTransitionReady.SafeUnsubscribe(HandleTransitionReady);
                stage.OnTaskInStageUpdated.SafeUnsubscribe(HandleTaskInStageUpdated);
                stage.OnGroupInStageStarted.SafeUnsubscribe(HandleGroupInStageStarted);
                stage.OnGroupInStageCompleted.SafeUnsubscribe(HandleGroupInStageCompleted);
                stage.OnGroupInStageFailed.SafeUnsubscribe(HandleGroupInStageFailed);
            }

            // Unsubscribe from global task failure conditions
            if (QuestData.GlobalTaskFailureConditions != null)
            {
                foreach (Condition_SO condition in QuestData.GlobalTaskFailureConditions)
                {
                    if (condition is IConditionEventDriven conditionEventDriven)
                    {
                        conditionEventDriven.UnsubscribeFromEvent();
                    }
                }
            }
        }

        private void UnsubscribeFromStartConditions()
        {
            if (QuestData.StartConditions == null) return;

            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.UnsubscribeFromEvent();
            }
        }

        public void SubscribeToStartQuestEvents()
        {
            if (QuestData.StartConditions == null) return;

            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.SubscribeToEvent(TryStartQuestIfConditionsMet);
                QuestLogger.Log($"Subscribed to '{condition.name}' for quest '{QuestData.DevName}' start conditions.");
            }
        }

        #endregion

        #region Stage Event Handlers

        private void HandleStageEntered(QuestStageRuntime stage)
        {
            QuestLogger.Log($"Stage '{stage.StageName}' entered in quest '{QuestData.DevName}'.");
        }

        private void HandleStageCompleted(QuestStageRuntime stage)
        {
            QuestLogger.Log($"Stage '{stage.StageName}' completed in quest '{QuestData.DevName}'.");

            // Check if all stages are completed (for terminal stages)
            if (stage.Data.IsTerminal)
            {
                CompleteQuest();
            }
        }

        private void HandleStageFailed(QuestStageRuntime stage)
        {
            QuestLogger.Log($"Stage '{stage.StageName}' failed in quest '{QuestData.DevName}'.");
            FailQuest();
        }

        private void HandleTransitionReady(QuestStageRuntime stage, int targetStageIndex)
        {
            QuestLogger.Log($"Transition ready from stage '{stage.StageName}' to stage index {targetStageIndex}.");

            var targetStage = GetStageByIndex(targetStageIndex);
            if (targetStage != null)
            {
                int previousIndex = CurrentStageIndex;
                TransitionToStage(targetStage);
                OnStageTransition.SafeInvoke(this, previousIndex, targetStageIndex);
            }
            else
            {
                QuestLogger.LogWarning($"Target stage index {targetStageIndex} not found. Completing quest.");
                CompleteQuest();
            }
        }

        private void HandleTaskInStageUpdated(QuestStageRuntime stage, TaskRuntime task)
        {
            OnAnyTaskUpdated.SafeInvoke(task);
            NotifyQuestUpdated();
        }

        private void HandleGroupInStageStarted(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' started in stage '{stage.StageName}'.");

            // Subscribe to task events for this group
            foreach (var task in group.Tasks)
            {
                task.OnTaskStarted.SafeSubscribe(t => OnAnyTaskStarted.SafeInvoke(t));
                task.OnTaskCompleted.SafeSubscribe(t => OnAnyTaskCompleted.SafeInvoke(t));
                task.OnTaskFailed.SafeSubscribe(t => OnAnyTaskFailed.SafeInvoke(t));
            }
        }

        private void HandleGroupInStageCompleted(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' completed in stage '{stage.StageName}'.");
            NotifyQuestUpdated();
        }

        private void HandleGroupInStageFailed(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' failed in stage '{stage.StageName}'.");
        }

        #endregion

        #region Other Event Handlers

        private void HandleGlobalTaskFailure()
        {
            var currentTasks = CurrentTasks;
            if (currentTasks.Count > 0)
            {
                foreach (var task in currentTasks)
                {
                    QuestLogger.Log($"Global task failure condition met. Failing task '{task.DevName}' in quest '{QuestData.DevName}'.");
                    task.FailTask();
                }
            }
        }

        private void TryStartQuestIfConditionsMet()
        {
            if (CurrentState != QuestState.NotStarted) return;

            if (CheckStartConditions())
            {
                QuestLogger.Log($"All start conditions met for quest '{QuestData.DevName}'. Starting quest.");
                StartQuest();
            }
        }

        #endregion

        /// <summary>
        /// Single point for firing OnQuestUpdated to prevent double-fires.
        /// </summary>
        private void NotifyQuestUpdated()
        {
            OnQuestUpdated.SafeInvoke(this);
        }

        #region Condition Checking

        public bool CheckForConditionsAndStart()
        {
            if (CheckStartConditions())
            {
                StartQuest();
                return true;
            }
            return false;
        }

        public bool CheckStartConditions()
        {
            if (QuestData.StartConditions == null || QuestData.StartConditions.Count == 0)
                return true;

            return QuestData.StartConditions.All(c => c != null && c.Evaluate());
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Increments the current task's step. No-op if no task is in progress.
        /// </summary>
        public void IncrementCurrentTask() => CurrentTask?.IncrementStep();

        /// <summary>
        /// Decrements the current task's step. No-op if no task is in progress.
        /// </summary>
        public void DecrementCurrentTask() => CurrentTask?.DecrementStep();

        /// <summary>
        /// Force completes all remaining tasks and the quest.
        /// Useful for debugging or skip functionality.
        /// </summary>
        public void ForceComplete()
        {
            foreach (var task in Tasks)
            {
                if (task.CurrentState != TaskState.Completed)
                {
                    task.CompleteTask();
                }
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is QuestRuntime other)
            {
                return QuestId == other.QuestId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return QuestId.GetHashCode();
        }

        #endregion
    }
}
