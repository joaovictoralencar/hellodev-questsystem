using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Quests;
using UnityEngine;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// The central manager for all quests. This singleton handles quest lifecycle,
    /// state, saving, loading, and event delegation. It provides a robust, clean API
    /// for all other game systems to interact with quest data without knowing its internal logic.
    /// </summary>
    public partial class QuestManager : MonoBehaviour
    {
        [SerializeField] private List<Quest_SO> questsDatabase = new();
        [Header("Configuration")] [SerializeField]
        private bool InitializeOnAwake = true;

        [SerializeField] private bool EnableDebugLogging = true;
        [SerializeField] private bool AllowMultipleActiveQuests = true;
        [SerializeField] private bool AllowPlayingCompletedQuests = true;

        private Dictionary<Guid, Quest_SO> _availableQuestsData = new();
        private Dictionary<Guid, Quest> _activeQuests = new();
        private Dictionary<Guid, Quest> _completedQuests = new();
        private Dictionary<Type, List<Quest>> _eventListeners = new();

        [HideInInspector] public UnityEvent<Quest> QuestAdded = new();
        [HideInInspector] public UnityEvent<Quest> QuestStarted = new();
        [HideInInspector] public UnityEvent<Quest> QuestRemoved = new();
        [HideInInspector] public UnityEvent<Quest> QuestRestarted = new();
        [HideInInspector] public UnityEvent<Quest> QuestFailed = new();
        [HideInInspector] public UnityEvent<Quest> QuestUpdated = new();
        [HideInInspector] public UnityEvent<Quest> QuestCompleted = new();

        public static QuestManager Instance { get; private set; }

        public Dictionary<Guid, Quest> ActiveQuests => _activeQuests;
        public List<Quest_SO> QuestsDatabase => questsDatabase;
        public Dictionary<Guid, Quest> CompletedQuests => _completedQuests;

        private void Awake()
        {
            QuestLogger.IsLoggingEnabled = EnableDebugLogging;
            
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
            foreach (Quest_SO questData in allQuestData)
            {
                if (!_availableQuestsData.TryAdd(questData.QuestId, questData))
                {
                    QuestLogger.LogWarning($"Duplicate quest ID found for '{questData.DevName}'. ID: {questData.QuestId}");
                }
            }
        }

        private void Start()
        {
            foreach (Quest_SO quest in _availableQuestsData.Values)
            {
                AddQuest(quest, true);
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
            if (quest == null)
            {
                QuestLogger.LogError($"Added quest is null.");
                return false;
            }

            Guid questId = quest.QuestId;

            if (!_availableQuestsData.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest with ID '{questId}' is not a registered available quest.");
                return false;
            }

            if (_activeQuests.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest '{quest.DevName}' is was already added.");
                return false;
            }

            if (!AllowPlayingCompletedQuests && _completedQuests.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest '{_availableQuestsData[questId].DevName}' has already been completed and AllowPlayingCompletedQuests is disabled.");
                return false;
            }

            if (!AllowMultipleActiveQuests && _activeQuests.Count > 0)
            {
                QuestLogger.Log($"There's already one active quest ({_activeQuests.Values.First().QuestData.DevName}) and AllowMultipleActiveQuests is disabled.");
                return false;
            }

            Quest newQuest = _availableQuestsData[questId].GetRuntimeQuest();
            _activeQuests.Add(questId, newQuest);
            QuestLogger.Log($"Added quest '{quest.DevName}'.");

            QuestAdded?.SafeInvoke(newQuest);

            newQuest.OnQuestStarted.SafeSubscribe(HandleQuestStarted);
            newQuest.OnQuestCompleted.SafeSubscribe(HandleQuestCompleted);
            newQuest.OnQuestFailed.SafeSubscribe(HandleQuestFailed);
            newQuest.OnQuestUpdated.SafeSubscribe(HandleQuestUpdated);
            newQuest.OnQuestRestarted.SafeSubscribe(HandleQuestRestarted);

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

        private void HandleQuestRestarted(Quest quest)
        {
            QuestRestarted.Invoke(quest);
        }

        private void HandleQuestUpdated(Quest quest)
        {
            QuestUpdated?.SafeInvoke(quest);
        }

        private void HandleQuestStarted(Quest quest)
        {
            QuestStarted?.SafeInvoke(quest);
        }

        public void CompleteQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                quest.CompleteQuest();
            }
        }

        public void FailQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                quest.FailQuest();
            }
        }

        public bool RemoveQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                UnsubscribeFromQuestEvents(quest);
                quest.ResetQuest();
                QuestLogger.LogWarning($"Quest of ID '{quest.QuestData.DevName}' was removed.");
                _activeQuests.Remove(questId);
                QuestRemoved?.SafeInvoke(quest);
                return true;
            }

            QuestLogger.LogWarning($"Quest of ID '{questId}' is not active.");
            return false;
        }

        public bool RestartQuest(Guid questId, bool forceStart = false)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                UnsubscribeFromQuestEvents(quest);
                quest.ResetQuest();
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
            _completedQuests.TryAdd(quest.QuestId, quest);
            QuestLogger.Log($"Quest '{quest.QuestData.DevName}' moved to completed quests.");
            QuestCompleted?.SafeInvoke(quest);
        }

        private void HandleQuestFailed(Quest quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _activeQuests.Remove(quest.QuestId);
            QuestLogger.Log($"Quest '{quest.QuestData.DevName}' has failed.");
            QuestFailed?.SafeInvoke(quest);
        }

        #endregion

        #region Dynamic Subscription Management

        private void UnsubscribeFromQuestEvents(Quest quest)
        {
        }
        #endregion

        #region Task Lifecycle & Events

        [Button]
        public void IncrementTaskStep(Quest_SO quest)
        {
            if (_activeQuests.TryGetValue(quest.QuestId, out Quest q))
            {
                Task task = q.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
                task?.IncrementStep();
            }
        }

        public void DecrementTaskStep(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                Task task = quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
                task?.DecrementStep();
            }
        }

        public void CompleteTask(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                Task task = quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
                task?.CompleteTask();
            }
        }

        public void FailTask(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                Task task = quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
                task?.FailTask();
            }
        }

        #endregion

        #region Query & Data Access

        public Quest GetActiveQuest(Guid questId)
        {
            _activeQuests.TryGetValue(questId, out Quest quest);
            return quest;
        }

        public ReadOnlyCollection<Quest> GetActiveQuests()
        {
            return _activeQuests.Values.ToList().AsReadOnly();
        }

        public ReadOnlyCollection<Task> GetTasksForQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out Quest quest))
            {
                return quest.Tasks.AsReadOnly();
            }

            return null;
        }

        public bool IsQuestCompleted(Guid questId)
        {
            return _completedQuests.ContainsKey(questId);
        }

        #endregion
    }
}