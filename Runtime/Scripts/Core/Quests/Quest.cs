using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Localization;
using UnityEngine;
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

        public UnityEvent<Quest, QuestState> OnQuestStateChanged = new();
        public UnityEvent<Quest> OnQuestStarted = new();
        public UnityEvent<Quest> OnQuestCompleted = new();
        public UnityEvent<Quest> OnQuestFailed = new();
        public UnityEvent<Quest> OnQuestUpdated = new();
        public UnityEvent<Quest> OnAnyTaskUpdated = new();

        #endregion

        #region Properties

        public Guid QuestId { get; private set; }
        public QuestState CurrentState { get; private set; }
        public List<Task> Tasks { get; private set; }
        public Quest_SO QuestData { get; private set; }

        public float CurrentProgress
        {
            get { return Tasks.Sum(t => t.Progress) / Tasks.Count; }
        }

        #endregion

        public Quest(Quest_SO questData)
        {
            QuestData = questData;
            QuestId = questData.QuestId;
            CurrentState = QuestState.NotStarted;
            Tasks = questData.Tasks.Select(so => so.GetRuntimeTask()).ToList();
        }

        private void UpdateQuestState(QuestState newState)
        {
            CurrentState = newState;
            OnQuestStateChanged?.SafeInvoke(this, CurrentState);
        }

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// </summary>
        public void StartQuest()
        {
            if (CurrentState != QuestState.NotStarted)
            {
                QuestLogger.Log($"Quest '{QuestData.DevName}' is already in progress.");
                return;
            }

            Task firstTask = Tasks.FirstOrDefault();
            firstTask?.StartTask();
            QuestLogger.Log($"Quest '{QuestData.DevName}' started.");

            UnsubscribeFromStartConditions();
            SubscribeToAllEvents();

            UpdateQuestState(QuestState.InProgress);
            OnQuestStarted?.SafeInvoke(this);
        }

        /// <summary>
        /// Marks the quest as completed, changing its state to Completed.
        /// This method should contain logic for rewards and events.
        /// </summary>
        public void CompleteQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                UnsubscribeFromAllEvents();
                OnQuestUpdated?.SafeInvoke(this);
                UpdateQuestState(QuestState.Completed);
                OnQuestCompleted?.SafeInvoke(this);
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
                OnQuestFailed?.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Resets the quest to its initial state.
        /// </summary>
        public void ResetQuest()
        {
            UnsubscribeFromAllEvents();
            UpdateQuestState(QuestState.NotStarted);
        }

        private void SubscribeToAllEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnTaskCompleted.SafeSubscribe(HandleTaskCompleted);
                task.OnTaskUpdated.SafeSubscribe(HandleTaskUpdated);
            }
        }

        private bool CheckCompletion()
        {
            if (CurrentState == QuestState.InProgress)
            {
                return Tasks.All(task => task.CurrentState == TaskState.Completed);
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

        /// <summary>
        /// Handles when a task within the quest is updated.
        /// Logs information about the task and calls <see cref="OnQuestUpdated"/> if the event is not null.
        /// </summary>
        /// <param name="task">The task which was updated.</param>
        private void HandleTaskUpdated(Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' in quest '{QuestData.DevName}' was updated.");
            OnAnyTaskUpdated?.SafeInvoke(this);
        }

        private void UnsubscribeFromAllEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnTaskCompleted.Unsubscribe(HandleTaskCompleted);
                task.OnTaskUpdated.Unsubscribe(HandleTaskUpdated);
            }
        }

        /// <summary>
        /// The event handler for a task completing. Calls CheckCompletion to re-evaluate the quest's state.
        /// </summary>
        /// <param name="completedTask">The task that was completed.</param>
        private void HandleTaskCompleted(Task completedTask)
        {
            QuestLogger.Log($"Task '{completedTask.DevName}' in quest '{QuestData.DevName}' completed. Checking quest completion.");
            if (CheckCompletion())
            {
                CompleteQuest();
            }
            else
            {
                foreach (Task task in Tasks)
                {
                    if (task.CurrentState == TaskState.NotStarted)
                    {
                        task.StartTask();
                        break;
                    }
                }
            }
            OnQuestUpdated?.SafeInvoke(this);
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
    }
}