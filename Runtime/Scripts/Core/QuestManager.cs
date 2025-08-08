using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Quests;
using UnityEngine;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using HelloDev.Events;
using HelloDev.QuestSystem.Conditions;
using HelloDev.QuestSystem.Conditions.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// The central manager for all quests. This singleton handles quest lifecycle,
    /// state, saving, loading, and event delegation. It provides a robust, clean API
    /// for all other game systems to interact with quest data without knowing its internal logic.
    /// </summary>
    public partial class QuestManager : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private bool InitializeOnAwake = true;

        [SerializeField] private bool EnableDebugLogging = true;
        [SerializeField] private List<Quest_SO> QuestsDatabase = new();
        [SerializeField] private bool NonRepeatableQuests;
        [SerializeField] private bool AllowMultipleActiveQuests = true;
        [SerializeField] private bool AllowPlayingCompletedQuests = true;

        private Dictionary<Guid, Quest_SO> _availableQuestsData = new();
        private Dictionary<Guid, Quest> _activeQuests = new();
        private HashSet<Guid> _completedQuests = new();

        private Dictionary<Type, List<Quest>> _eventListeners = new();

        public Action<Quest> QuestAdded;
        public Action<Quest> QuestRemoved;
        public Action<Quest> QuestRestarted;
        public Action<Quest> QuestFailed;
        public Action<Quest> QuestUpdated;
        public Action<Quest> QuestCompleted;

        public static QuestManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (InitializeOnAwake)
                {
                    InitializeManager(QuestsDatabase);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Core Manager Lifecycle

        public void InitializeManager(List<Quest_SO> allQuestData)
        {
            _availableQuestsData.Clear();
            foreach (var questData in allQuestData)
            {
                if (!_availableQuestsData.TryAdd(questData.QuestId, questData))
                {
                    QuestLogger.LogWarning($"Duplicate quest ID found for '{questData.DevName}'. ID: {questData.QuestId}");
                }
            }
        }

        public void ShutdownManager()
        {
            _activeQuests.Clear();
            _completedQuests.Clear();
            _availableQuestsData.Clear();
            _eventListeners.Clear();
        }

        #endregion

        #region Quest Lifecycle & State

        [Button]
        public bool AddQuest(Quest_SO quest, bool forceStart = false)
        {
            Guid questId = quest.QuestId;

            if (!_availableQuestsData.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest with ID '{questId}' is not a registered available quest.");
                return false;
            }

            if (NonRepeatableQuests && _activeQuests.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest '{quest.DevName}' is already active and NonRepeatableQuests is enabled.");
                return false;
            }

            if (!AllowPlayingCompletedQuests && _completedQuests.Contains(questId))
            {
                QuestLogger.Log($"Quest '{_availableQuestsData[questId].DevName}' has already been completed and AllowPlayingCompletedQuests is disabled.");
                return false;
            }

            if (!AllowMultipleActiveQuests && _activeQuests.Count > 0)
            {
                QuestLogger.Log($"There's already one active quest ({_activeQuests.Values.First().DevName}) and AllowMultipleActiveQuests is disabled.");
                return false;
            }

            Quest newQuest = _availableQuestsData[questId].GetRuntimeQuest();
            _activeQuests.Add(questId, newQuest);


            QuestAdded?.Invoke(newQuest);

            if (forceStart || newQuest.CheckStartConditions())
            {
                newQuest.StartQuest();
            }
            else
            {
                newQuest.SubscribeToStartQuestEvents();
            }

            return true;
        }

        public void CompleteQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                quest.OnCompleteQuest();
            }
        }

        public void FailQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                quest.OnFailQuest();
            }
        }

        public bool RemoveQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                UnsubscribeFromQuestEvents(quest);
                quest.ResetQuest();
                QuestLogger.LogWarning($"Quest of ID '{quest.DevName}' was removed.");
                _activeQuests.Remove(questId);
                QuestRemoved?.Invoke(quest);
                return true;
            }

            QuestLogger.LogWarning($"Quest of ID '{questId}' is not active.");
            return false;
        }

        public bool RestartQuest(Guid questId, bool forceStart = false)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                UnsubscribeFromQuestEvents(quest);
                quest.ResetQuest();
                quest.QuestRestarted?.Invoke(quest);
                if (forceStart)
                {
                    quest.StartQuest();
                }
                else
                {
                    quest.CheckForConditionsAndStart();
                }

                return true;
            }

            QuestLogger.LogWarning($"Quest of ID '{questId}' is not active.");
            return false;
        }

        private void HandleQuestCompleted(Quest quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _activeQuests.Remove(quest.QuestId);
            _completedQuests.Add(quest.QuestId);
            QuestLogger.Log($"Quest '{quest.DevName}' moved to completed quests.");
        }

        private void HandleQuestFailed(Quest quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _activeQuests.Remove(quest.QuestId);
            QuestLogger.Log($"Quest '{quest.DevName}' has failed.");
        }

        #endregion

        #region Dynamic Subscription Management

        private void UnsubscribeFromQuestEvents(Quest quest)
        {
            // var eventTypes = quest.GetTriggerEvents();
            // foreach (var eventType in eventTypes)
            // {
            //     if (_eventListeners.TryGetValue(eventType, out var quests))
            //     {
            //         quests.Remove(quest);
            //         if (quests.Count == 0)
            //         {
            //             UnsubscribeFromEvent(eventType);
            //             _eventListeners.Remove(eventType);
            //         }
            //     }
            // }
            //
            // quest.OnQuestCompleted -= HandleQuestCompleted;
            // quest.OnQuestFailed -= HandleQuestFailed;
        }

        private void SubscribeToEvent(Type eventType)
        {
        }

        private void UnsubscribeFromEvent(Type eventType)
        {
        }

        // private void HandleEvent<T>(T gameEvent) where T :  IEvent<object>
        // {
        //     var eventType = typeof(T);
        //     if (_eventListeners.TryGetValue(eventType, out var quests))
        //     {
        //         var questsToHandle = quests.ToList();
        //         foreach (var quest in questsToHandle)
        //         {
        //             // quest.HandleEvent(gameEvent);
        //         }
        //     }
        // }

        #endregion

        #region Task Lifecycle & Events

        [Button]
        public void IncrementTaskStep(Quest_SO quest)
        {
            if (_activeQuests.TryGetValue(quest.QuestId, out var q))
            {
                Task task = q.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
                task?.IncrementStep();
            }
        }

        public void DecrementTaskStep(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                var task = quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
                task?.DecrementStep();
            }
        }

        public void CompleteTask(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                var task = quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
                task?.CompleteTask();
            }
        }

        public void FailTask(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                var task = quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
                task?.FailTask();
            }
        }

        #endregion

        #region Query & Data Access

        public Quest GetActiveQuest(Guid questId)
        {
            _activeQuests.TryGetValue(questId, out var quest);
            return quest;
        }

        public ReadOnlyCollection<Quest> GetActiveQuests()
        {
            return _activeQuests.Values.ToList().AsReadOnly();
        }

        public ReadOnlyCollection<Task> GetTasksForQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out var quest))
            {
                return quest.Tasks.AsReadOnly();
            }

            return null;
        }

        public bool IsQuestCompleted(Guid questId)
        {
            return _completedQuests.Contains(questId);
        }

        #endregion
    }
}