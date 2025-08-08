using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Localization;
using UnityEngine;
using HelloDev.QuestSystem.Conditions.ScriptableObjects;

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

        public Action<Quest, QuestState> OnQuestStateChanged;
        public Action<Quest> OnQuestStarted;
        public Action<Quest> OnQuestCompleted;
        public Action<Quest> OnQuestFailed;
        public Action<Quest> OnQuestCanceled;
        public Action<Quest> QuestRestarted;
        public Action<Quest> QuestFailed;
        public Action<Quest> QuestUpdated;
        public Action<Quest> QuestCompleted;

        #endregion

        #region Properties

        public Guid QuestId { get; private set; }
        public string DevName { get; private set; }
        public LocalizedString DisplayName { get; private set; }
        public LocalizedString Description { get; private set; }
        public QuestState CurrentState { get; private set; }
        public List<Task> Tasks { get; private set; }
        public List<Condition_SO> StartConditions { get; private set; }
        public List<Condition_SO> FailureConditions { get; private set; }
        public Quest_SO QuestData { get; private set; }

        #endregion

        public Quest(Quest_SO questData)
        {
            QuestData = questData;
            QuestId = questData.QuestId;
            DevName = questData.DevName;
            DisplayName = questData.DisplayName;
            Description = questData.QuestDescription;
            CurrentState = QuestState.NotStarted;

            StartConditions = InstantiateConditionsList(questData.StartConditions);
            FailureConditions = InstantiateConditionsList(questData.FailureConditions);
            Tasks = questData.Tasks.Select(so => so.GetRuntimeTask()).ToList();
        }

        List<Condition_SO> InstantiateConditionsList(List<Condition_SO> list)
        {
            var newList = new List<Condition_SO>();
            foreach (var so in list)
            {
                newList.Add(UnityEngine.Object.Instantiate(so));
            }

            return newList;
        }

        /// <summary>
        /// Handles a generic event from the QuestManager and evaluates relevant conditions.
        /// </summary>
        // public void HandleEvent(IEvent gameEvent)
        // {
        //     if (CurrentState == QuestState.NotStarted)
        //     {
        //         EvaluateStartConditions(gameEvent);
        //     }
        //     else if (CurrentState == QuestState.InProgress)
        //     {
        //         EvaluateCompletionConditions(gameEvent);
        //         EvaluateFailureConditions(gameEvent);
        //     }
        // }

        // private void EvaluateStartConditions(IEvent gameEvent)
        // {
        //     // var eventType = gameEvent.GetType();
        //     //
        //     // if (!StartConditions.Any(c => !c.IsEventDriven || c.EventType == eventType)) return;
        //     //
        //     // QuestLogger.Log($"Event '{eventType.Name}' triggered a check for quest '{DevName}' start conditions.");
        //     //
        //     // bool allConditionsMet = StartConditions.All(condition =>
        //     // {
        //     //     if (condition.IsEventDriven && condition.EventType == eventType)
        //     //     {
        //     //         return condition.OnConditionMet(gameEvent);
        //     //     }
        //     //     else if (!condition.IsEventDriven)
        //     //     {
        //     //         return condition.OnConditionMet();
        //     //     }
        //     //
        //     //     return false;
        //     // });
        //     //
        //     // if (allConditionsMet)
        //     // {
        //     //     StartQuest();
        //     // }
        // }

        // private void EvaluateCompletionConditions(IEvent gameEvent)
        // {
        //     // var eventType = gameEvent.GetType();
        //     //
        //     // if (!CompletionConditions.Any(c => !c.IsEventDriven || c.EventType == eventType)) return;
        //     //
        //     // QuestLogger.Log($"Event '{eventType.Name}' triggered a check for quest '{DevName}' completion conditions.");
        //     //
        //     // bool allConditionsMet = CompletionConditions.All(condition =>
        //     // {
        //     //     if (condition.IsEventDriven && condition.EventType == eventType)
        //     //     {
        //     //         return condition.OnConditionMet(gameEvent);
        //     //     }
        //     //     else if (!condition.IsEventDriven)
        //     //     {
        //     //         return condition.OnConditionMet();
        //     //     }
        //     //
        //     //     return false;
        //     // });
        //     //
        //     // if (allConditionsMet)
        //     // {
        //     //     OnCompleteQuest();
        //     // }
        // }

        // private void EvaluateFailureConditions(IEvent gameEvent)
        // {
        //     // var eventType = gameEvent.GetType();
        //     //
        //     // if (!FailureConditions.Any(c => !c.IsEventDriven || c.EventType == eventType)) return;
        //     //
        //     // QuestLogger.Log($"Event '{eventType.Name}' triggered a check for quest '{DevName}' failure conditions.");
        //     //
        //     // bool anyConditionsMet = FailureConditions.Any(condition =>
        //     // {
        //     //     if (condition.IsEventDriven && condition.EventType == eventType)
        //     //     {
        //     //         return condition.OnConditionMet(gameEvent);
        //     //     }
        //     //     else if (!condition.IsEventDriven)
        //     //     {
        //     //         return condition.OnConditionMet();
        //     //     }
        //     //
        //     //     return false;
        //     // });
        //     //
        //     // if (anyConditionsMet)
        //     // {
        //     //     OnFailQuest();
        //     // }
        // }

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// </summary>
        public void StartQuest()
        {
            if (CurrentState != QuestState.NotStarted)
            {
                QuestLogger.Log($"Quest '{DevName}' is already in progress.");
                return;
            }

            CurrentState = QuestState.InProgress;
            Task firstTask = Tasks.FirstOrDefault();
            firstTask?.StartTask();
            QuestLogger.Log($"Quest '{DevName}' started.");

            SubscribeToAllEvents();

            OnQuestStarted?.Invoke(this);
            OnQuestStateChanged?.Invoke(this, CurrentState);
        }

        /// <summary>
        /// Marks the quest as completed, changing its state to Completed.
        /// This method should contain logic for rewards and events.
        /// </summary>
        public void OnCompleteQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                CurrentState = QuestState.Completed;
                QuestLogger.Log($"Quest '{DevName}' completed!");

                UnsubscribeFromAllEvents();

                OnQuestCompleted?.Invoke(this);
                QuestCompleted?.Invoke(this);
                OnQuestStateChanged?.Invoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Marks the quest as failed, changing its state to Failed.
        /// This method can contain specific failure logic.
        /// </summary>
        public void OnFailQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                CurrentState = QuestState.Failed;
                QuestLogger.Log($"Quest '{DevName}' failed.");

                UnsubscribeFromAllEvents();

                OnQuestFailed?.Invoke(this);
                QuestFailed?.Invoke(this);
                OnQuestStateChanged?.Invoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Marks the quest as canceled, changing its state to NotStarted.
        /// </summary>
        public void CancelQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                CurrentState = QuestState.NotStarted;
                QuestLogger.Log($"Quest '{DevName}' canceled.");

                UnsubscribeFromAllEvents();

                OnQuestCanceled?.Invoke(this);
                OnQuestStateChanged?.Invoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Resets the quest to its initial state.
        /// </summary>
        public void ResetQuest()
        {
            CurrentState = QuestState.NotStarted;
            QuestLogger.Log($"Quest '{DevName}' reset.");

            UnsubscribeFromAllEvents();

            OnQuestStateChanged?.Invoke(this, CurrentState);
        }

        private void SubscribeToAllEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnTaskCompleted += HandleTaskCompleted;
                task.OnTaskUpdated += HandleTaskUpdated;
            }
        }

        private void HandleTaskUpdated(Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' in quest '{DevName}' was updated.");
            QuestUpdated?.Invoke(this);
        }

        private void UnsubscribeFromAllEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnTaskCompleted -= HandleTaskCompleted;
                task.OnTaskUpdated -= HandleTaskUpdated;
            }
        }

        /// <summary>
        /// The event handler for a task completing. Calls CheckCompletion to re-evaluate the quest's state.
        /// </summary>
        /// <param name="task">The task that was completed.</param>
        private void HandleTaskCompleted(Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' in quest '{DevName}' completed. Checking quest completion.");

            // EvaluateCompletionConditions(tempEvent);
            // EvaluateFailureConditions(tempEvent);
        }

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// Checks if all start conditions are met before proceeding.   
        /// </summary>
        /// <returns> True if the quest was successfully started, false otherwise.</returns>
        public bool CheckForConditionsAndStart()
        {
            bool allConditionsMet = StartConditions.All(c => c.Evaluate());
            if (allConditionsMet)
            {
                StartQuest();
                return true;
            }

            return false;
        }

        public bool CheckStartConditions()
        {
            bool allConditionsMet = StartConditions.All(c => c.Evaluate());
            if (allConditionsMet)
            {
                return true;
            }

            return false;
        }

        public void SubscribeToStartQuestEvents()
        {
            foreach (Condition_SO condition in StartConditions)
            {
                if (condition is IEventDrivenCondition eventDrivenCondition)
                {
                    eventDrivenCondition.SubscribeToEvent(StartQuest);
                }
            }
        }
    }
}