using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Quests
{
    /// <summary>
    /// Represents a base quest. This class provides the core structure and
    /// state management for all quests. It now listens for game events to
    /// evaluate its conditions.
    /// </summary>
    public class Quest
    {
        #region Events

        /// <summary>Fired when the quest starts.</summary>
        public UnityEvent<Quest> OnQuestStarted = new();

        /// <summary>Fired when the quest completes successfully.</summary>
        public UnityEvent<Quest> OnQuestCompleted = new();

        /// <summary>Fired when the quest fails.</summary>
        public UnityEvent<Quest> OnQuestFailed = new();

        /// <summary>Fired when the quest is reset and restarted.</summary>
        public UnityEvent<Quest> OnQuestRestarted = new();

        /// <summary>Fired when quest progress changes (group advances, task completes, etc.).</summary>
        public UnityEvent<Quest> OnQuestUpdated = new();

        /// <summary>Fired when any task in this quest starts.</summary>
        public UnityEvent<Task> OnAnyTaskStarted = new();

        /// <summary>Fired when any task in this quest is updated.</summary>
        public UnityEvent<Task> OnAnyTaskUpdated = new();

        /// <summary>Fired when any task in this quest completes.</summary>
        public UnityEvent<Task> OnAnyTaskCompleted = new();

        /// <summary>Fired when any task in this quest fails.</summary>
        public UnityEvent<Task> OnAnyTaskFailed = new();

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
        /// Gets all task groups in this quest.
        /// </summary>
        public List<TaskGroupRuntime> TaskGroups { get; }

        /// <summary>
        /// Gets the currently active task group, or null if quest is not in progress.
        /// </summary>
        public TaskGroupRuntime CurrentGroup =>
            _currentGroupIndex >= 0 && _currentGroupIndex < TaskGroups.Count
                ? TaskGroups[_currentGroupIndex]
                : null;

        /// <summary>
        /// Gets all tasks that are currently in progress (can be multiple for parallel groups).
        /// </summary>
        public IReadOnlyList<Task> CurrentTasks =>
            CurrentGroup.CurrentTasks ?? Array.Empty<Task>();

        /// <summary>
        /// Gets all tasks across all groups (flattened list for backward compatibility).
        /// </summary>
        public List<Task> Tasks => TaskGroups.SelectMany(g => g.Tasks).ToList();

        /// <summary>
        /// Gets the overall progress of this quest (0-1).
        /// Calculated as the weighted average of group progress.
        /// </summary>
        public float CurrentProgress
        {
            get
            {
                if (TaskGroups.Count == 0) return 1f;

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

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Quest"/> class.
        /// </summary>
        /// <param name="questData">The quest data.</param>
        /// <remarks>
        /// This constructor is used to create a runtime instance of a quest from a <see cref="Quest_SO"/> asset.
        /// Creates TaskGroupRuntime instances from the quest's task groups.
        /// </remarks>
        public Quest(Quest_SO questData)
        {
            QuestData = questData;
            QuestId = questData.QuestId;
            CurrentState = QuestState.NotStarted;

            // Create runtime task groups from the quest data
            TaskGroups = questData.TaskGroups
                .Select(groupData => new TaskGroupRuntime(groupData))
                .ToList();
        }

        private void UpdateQuestState(QuestState newState)
        {
            CurrentState = newState;
        }

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// Starts the first task group.
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

            // Start the first group
            _currentGroupIndex = 0;
            if (TaskGroups.Count > 0)
            {
                TaskGroups[0].StartGroup();
                QuestLogger.Log($"Quest '{QuestData.DevName}' started. First group: '{TaskGroups[0].GroupName}'");
            }
            else
            {
                QuestLogger.Log($"Quest '{QuestData.DevName}' started with no task groups.");
            }

            UpdateQuestState(QuestState.InProgress);
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
        /// This method can contain specific failure logic.
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

            // Reset all task groups
            foreach (var group in TaskGroups)
            {
                group.ResetGroup();
            }

            _currentGroupIndex = -1;
            UpdateQuestState(QuestState.NotStarted);
            StartQuest();
            OnQuestRestarted.SafeInvoke(this);
        }

        private void SubscribeToAllEvents()
        {
            // Subscribe to group events
            foreach (var group in TaskGroups)
            {
                group.OnGroupStarted.SafeSubscribe(HandleGroupStarted);
                group.OnGroupCompleted.SafeSubscribe(HandleGroupCompleted);
                group.OnGroupFailed.SafeSubscribe(HandleGroupFailed);
                group.OnTaskInGroupStarted.SafeSubscribe(HandleTaskInGroupStarted);
                group.OnTaskInGroupUpdated.SafeSubscribe(HandleTaskInGroupUpdated);
                group.OnTaskInGroupCompleted.SafeSubscribe(HandleTaskInGroupCompleted);
                group.OnTaskInGroupFailed.SafeSubscribe(HandleTaskInGroupFailed);
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

        /// <summary>
        /// Handles when a global task failure condition is met.
        /// Fails all current in-progress tasks.
        /// </summary>
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

        private bool CheckCompletion()
        {
            if (CurrentState == QuestState.InProgress)
            {
                return TaskGroups.All(group => group.CurrentState == TaskGroupState.Completed);
            }

            return false;
        }

        private void UnsubscribeFromStartConditions()
        {
            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven) 
                    conditionEventDriven.UnsubscribeFromEvent();
            }
        }

        public void SubscribeToStartQuestEvents()
        {
            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven) 
                    conditionEventDriven.SubscribeToEvent(StartQuest);
                QuestLogger.Log($"Subscribed event {condition.name} to start conditions for quest '{QuestData.DevName}'.");
            }
        }

        #region Group Event Handlers

        /// <summary>
        /// Handles when a task group starts.
        /// </summary>
        private void HandleGroupStarted(TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' started in quest '{QuestData.DevName}'.");
        }

        /// <summary>
        /// Handles when a task group completes.
        /// Advances to the next group or completes the quest.
        /// </summary>
        private void HandleGroupCompleted(TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' completed in quest '{QuestData.DevName}'.");

            if (CheckCompletion())
            {
                CompleteQuest();
            }
            else
            {
                // Advance to next group
                _currentGroupIndex++;
                if (_currentGroupIndex < TaskGroups.Count)
                {
                    TaskGroups[_currentGroupIndex].StartGroup();
                    QuestLogger.Log($"Starting next group '{TaskGroups[_currentGroupIndex].GroupName}' in quest '{QuestData.DevName}'.");
                }
                NotifyQuestUpdated();
            }
        }

        /// <summary>
        /// Handles when a task group fails.
        /// Fails the entire quest.
        /// </summary>
        private void HandleGroupFailed(TaskGroupRuntime group)
        {
            QuestLogger.Log($"Group '{group.GroupName}' failed in quest '{QuestData.DevName}'.");
            FailQuest();
        }

        #endregion

        #region Task Event Handlers

        /// <summary>
        /// Handles when a task within a group starts.
        /// </summary>
        private void HandleTaskInGroupStarted(TaskGroupRuntime group, Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' in group '{group.GroupName}' started.");
            OnAnyTaskStarted.SafeInvoke(task);
        }

        /// <summary>
        /// Handles when a task within a group is updated.
        /// </summary>
        private void HandleTaskInGroupUpdated(TaskGroupRuntime group, Task task)
        {
            OnAnyTaskUpdated.SafeInvoke(task);
            NotifyQuestUpdated();
        }

        /// <summary>
        /// Handles when a task within a group completes.
        /// </summary>
        private void HandleTaskInGroupCompleted(TaskGroupRuntime group, Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' in group '{group.GroupName}' completed.");
            OnAnyTaskCompleted.SafeInvoke(task);
        }

        /// <summary>
        /// Handles when a task within a group fails.
        /// </summary>
        private void HandleTaskInGroupFailed(TaskGroupRuntime group, Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' in group '{group.GroupName}' failed.");
            OnAnyTaskFailed.SafeInvoke(task);
        }

        #endregion

        /// <summary>
        /// Single point for firing OnQuestUpdated to prevent double-fires.
        /// </summary>
        private void NotifyQuestUpdated()
        {
            OnQuestUpdated.SafeInvoke(this);
        }

        private void UnsubscribeFromAllEvents()
        {
            // Unsubscribe from group events
            foreach (var group in TaskGroups)
            {
                group.OnGroupStarted.SafeUnsubscribe(HandleGroupStarted);
                group.OnGroupCompleted.SafeUnsubscribe(HandleGroupCompleted);
                group.OnGroupFailed.SafeUnsubscribe(HandleGroupFailed);
                group.OnTaskInGroupStarted.SafeUnsubscribe(HandleTaskInGroupStarted);
                group.OnTaskInGroupUpdated.SafeUnsubscribe(HandleTaskInGroupUpdated);
                group.OnTaskInGroupCompleted.SafeUnsubscribe(HandleTaskInGroupCompleted);
                group.OnTaskInGroupFailed.SafeUnsubscribe(HandleTaskInGroupFailed);
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

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// Checks if all start conditions are met before proceeding.   
        /// </summary>
        /// <returns> True if the quest was successfully started, false otherwise.</returns>
        public bool CheckForConditionsAndStart()
        {
            bool allConditionsMet = QuestData.StartConditions.All(c => c.Evaluate());
            if (allConditionsMet)
            {
                StartQuest();
                return true;
            }

            return false;
        }

        public bool CheckStartConditions()
        {
            bool allConditionsMet = QuestData.StartConditions.All(c => c.Evaluate());
            if (allConditionsMet)
            {
                return true;
            }

            return false;
        }
        public override bool Equals(object obj)
        {
            if (obj is Quest other)
            {
                return QuestId == other.QuestId;
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            return QuestId.GetHashCode();
        }
    }
}