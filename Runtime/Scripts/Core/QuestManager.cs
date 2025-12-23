using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// The central manager for all quests. This singleton handles quest lifecycle,
    /// state, and event delegation. It provides a robust, clean API
    /// for all other game systems to interact with quest data without knowing its internal logic.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Quest Database")]
        [PropertyOrder(0)]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        [InfoBox("$" + nameof(GetDatabaseInfoMessage), InfoMessageType.Info)]
#else
        [Header("Quest Database")]
#endif
        [Tooltip("The list of all available quests in the game.")]
        [SerializeField]
        private List<Quest_SO> questsDatabase = new();

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [PropertyOrder(10)]
        [ToggleLeft]
#else
        [Header("Configuration")]
#endif
        [Tooltip("If true, the manager will initialize itself on Awake.")]
        [SerializeField]
        private bool initializeOnAwake = true;

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [PropertyOrder(11)]
        [ToggleLeft]
#endif
        [Tooltip("If true, all quests in the database will be added and started on Start.")]
        [SerializeField]
        private bool autoStartQuestsOnStart = true;

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [PropertyOrder(12)]
        [ToggleLeft]
#endif
        [Tooltip("If true, debug messages will be logged to the console.")]
        [SerializeField]
        private bool enableDebugLogging = true;

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [PropertyOrder(13)]
        [ToggleLeft]
#endif
        [Tooltip("If true, multiple quests can be active at the same time.")]
        [SerializeField]
        private bool allowMultipleActiveQuests = true;

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [PropertyOrder(14)]
        [ToggleLeft]
#endif
        [Tooltip("If true, completed quests can be replayed.")]
        [SerializeField]
        private bool allowReplayingCompletedQuests = true;

        #endregion

        #region Private Fields

        private Dictionary<Guid, Quest_SO> _availableQuestsData = new();
        private Dictionary<Guid, QuestRuntime> _activeQuests = new();
        private Dictionary<Guid, QuestRuntime> _completedQuests = new();
        private Dictionary<Guid, QuestRuntime> _failedQuests = new();

        #endregion

        #region Events

        /// <summary>Fired when a quest is added to the active quests.</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestAdded = new();

        /// <summary>Fired when a quest starts (transitions to InProgress).</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestStarted = new();

        /// <summary>Fired when a quest is removed from the active quests.</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestRemoved = new();

        /// <summary>Fired when a quest is restarted.</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestRestarted = new();

        /// <summary>Fired when a quest fails.</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestFailed = new();

        /// <summary>Fired when a quest is updated (task progress, etc.).</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestUpdated = new();

        /// <summary>Fired when a quest is completed.</summary>
        [HideInInspector] public UnityEvent<QuestRuntime> QuestCompleted = new();

        #endregion

        #region Properties

        /// <summary>The singleton instance of the QuestManager.</summary>
        public static QuestManager Instance { get; private set; }

        /// <summary>Read-only access to the quest database.</summary>
        public IReadOnlyList<Quest_SO> QuestsDatabase => questsDatabase;

        /// <summary>Gets the count of active quests.</summary>
        public int ActiveQuestCount => _activeQuests.Count;

        /// <summary>Gets the count of completed quests.</summary>
        public int CompletedQuestCount => _completedQuests.Count;

        /// <summary>Gets the count of failed quests.</summary>
        public int FailedQuestCount => _failedQuests.Count;

        /// <summary>Configuration: Whether multiple quests can be active simultaneously.</summary>
        public bool AllowMultipleActiveQuests => allowMultipleActiveQuests;

        /// <summary>Configuration: Whether completed quests can be replayed.</summary>
        public bool AllowReplayingCompletedQuests => allowReplayingCompletedQuests;

        #endregion

        #region Runtime State Display (Odin)

#if ODIN_INSPECTOR
        [TitleGroup("Runtime State")]
        [PropertyOrder(50)]
        [ShowInInspector, ReadOnly]
        [InfoBox("Runtime state is only visible during Play mode.", InfoMessageType.Info, nameof(IsNotPlaying))]
        private string StatusSummary => Application.isPlaying
            ? $"Active: {ActiveQuestCount} | Completed: {CompletedQuestCount} | Failed: {FailedQuestCount}"
            : "Not in Play mode";

        [TitleGroup("Runtime State")]
        [PropertyOrder(51)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithActiveQuests))]
        private List<string> ActiveQuestNames => _activeQuests.Values
            .Select(q => $"{q.QuestData.DevName} ({q.CurrentState})")
            .ToList() ?? new List<string>();

        [TitleGroup("Runtime State")]
        [PropertyOrder(52)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithCompletedQuests))]
        private List<string> CompletedQuestNames => _completedQuests.Values
            .Select(q => q.QuestData.DevName)
            .ToList() ?? new List<string>();

        [TitleGroup("Runtime State")]
        [PropertyOrder(53)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithFailedQuests))]
        private List<string> FailedQuestNames => _failedQuests.Values
            .Select(q => q.QuestData.DevName)
            .ToList() ?? new List<string>();

        private bool IsNotPlaying => !Application.isPlaying;
        private bool IsPlayingWithActiveQuests => Application.isPlaying && _activeQuests.Count > 0;
        private bool IsPlayingWithCompletedQuests => Application.isPlaying && _completedQuests.Count > 0;
        private bool IsPlayingWithFailedQuests => Application.isPlaying && _failedQuests.Count > 0;

        private string GetDatabaseInfoMessage()
        {
            if (questsDatabase == null || questsDatabase.Count == 0)
                return "No quests in database. Add Quest_SO assets to enable quests.";
            int validCount = questsDatabase.Count(q => q != null);
            return $"{validCount} quest(s) in database.";
        }
#endif

        #endregion

        #region Debug Actions (Odin)

#if ODIN_INSPECTOR
        [TitleGroup("Debug Actions")]
        [PropertyOrder(60)]
        [Button("Complete All Active Quests", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithActiveQuests))]
        private void DebugCompleteAllQuests()
        {
            var questIds = _activeQuests.Keys.ToList();
            foreach (var questId in questIds)
            {
                CompleteQuest(questId);
            }
            Debug.Log("[QuestManager] All active quests completed.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(61)]
        [Button("Fail All Active Quests", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithActiveQuests))]
        private void DebugFailAllQuests()
        {
            var questIds = _activeQuests.Keys.ToList();
            foreach (var questId in questIds)
            {
                FailQuest(questId);
            }
            Debug.Log("[QuestManager] All active quests failed.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(62)]
        [Button("Increment Current Task (All Quests)", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithActiveQuests))]
        private void DebugIncrementAllCurrentTasks()
        {
            foreach (var quest in _activeQuests.Values)
            {
                var currentTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
                currentTask?.IncrementStep();
            }
            Debug.Log("[QuestManager] Incremented current task for all active quests.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(63)]
        [Button("Restart All Failed Quests", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithFailedQuests))]
        private void DebugRestartFailedQuests()
        {
            var failedQuestData = _failedQuests.Values.Select(q => q.QuestData).ToList();
            _failedQuests.Clear();
            foreach (var questData in failedQuestData)
            {
                AddQuest(questData, forceStart: true);
            }
            Debug.Log("[QuestManager] All failed quests restarted.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(64)]
        [Button("Log State to Console", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        private void DebugLogState()
        {
            Debug.Log($"[QuestManager] === Current State ===");
            Debug.Log($"Active Quests ({ActiveQuestCount}):");
            foreach (var quest in _activeQuests.Values)
            {
                var currentTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
                Debug.Log($"  - {quest.QuestData.DevName}: {quest.CurrentState} | Current Task: {currentTask?.DevName ?? "None"} | Progress: {quest.CurrentProgress:P0}");
            }
            Debug.Log($"Completed Quests ({CompletedQuestCount}):");
            foreach (var quest in _completedQuests.Values)
            {
                Debug.Log($"  - {quest.QuestData.DevName}");
            }
            Debug.Log($"Failed Quests ({FailedQuestCount}):");
            foreach (var quest in _failedQuests.Values)
            {
                Debug.Log($"  - {quest.QuestData.DevName}");
            }
        }
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                QuestLogger.IsLoggingEnabled = enableDebugLogging;

                if (initializeOnAwake)
                {
                    InitializeManager(questsDatabase);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (autoStartQuestsOnStart)
            {
                foreach (Quest_SO quest in _availableQuestsData.Values)
                {
                    AddQuest(quest, forceStart: true);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // Clean up all active quest subscriptions
                foreach (QuestRuntime quest in _activeQuests.Values)
                {
                    UnsubscribeFromQuestEvents(quest);
                }
                _activeQuests.Clear();
                _completedQuests.Clear();
                _failedQuests.Clear();
                _availableQuestsData.Clear();
                Instance = null;
            }
        }

        #endregion

        #region Core Manager Lifecycle

        /// <summary>
        /// Initializes the quest manager with the given quest data.
        /// </summary>
        /// <param name="allQuestData">The list of all available quest data.</param>
        public void InitializeManager(List<Quest_SO> allQuestData)
        {
            if (allQuestData == null)
            {
                QuestLogger.LogError("InitializeManager: allQuestData is null.");
                return;
            }

            _availableQuestsData.Clear();
            foreach (Quest_SO questData in allQuestData)
            {
                if (questData == null)
                {
                    QuestLogger.LogWarning("InitializeManager: Null quest found in database, skipping.");
                    continue;
                }

                _availableQuestsData.TryAdd(questData.QuestId, questData);
            }

            QuestLogger.Log($"QuestManager initialized with {_availableQuestsData.Count} quests.");
        }

        /// <summary>
        /// Shuts down the quest manager and clears all state.
        /// </summary>
        public void ShutdownManager()
        {
            foreach (QuestRuntime quest in _activeQuests.Values)
            {
                UnsubscribeFromQuestEvents(quest);
            }

            _activeQuests.Clear();
            _completedQuests.Clear();
            _failedQuests.Clear();
            _availableQuestsData.Clear();

            QuestLogger.Log("QuestManager shut down.");
        }

        #endregion

        #region Quest Lifecycle & State

        /// <summary>
        /// Adds a quest to the active quests and optionally starts it.
        /// </summary>
        /// <param name="questData">The quest data to add.</param>
        /// <param name="forceStart">If true, starts the quest immediately regardless of conditions.</param>
        /// <returns>True if the quest was successfully added.</returns>
#if ODIN_INSPECTOR
        [TitleGroup("Runtime Actions")]
        [Button("Add Quest")]
        [PropertyOrder(70)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
#endif
        public bool AddQuest(
#if ODIN_INSPECTOR
            [ValueDropdown(nameof(GetAvailableQuests))]
#endif
            Quest_SO questData, bool forceStart = false)
        {
            if (questData == null)
            {
                QuestLogger.LogError("AddQuest: questData is null.");
                return false;
            }

            Guid questId = questData.QuestId;

            if (!_availableQuestsData.ContainsKey(questId))
            {
                QuestLogger.LogWarning($"Quest '{questData.DevName}' is not in the available quests database.");
                return false;
            }

            if (_activeQuests.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest '{questData.DevName}' is already active.");
                return false;
            }

            if (!allowReplayingCompletedQuests && _completedQuests.ContainsKey(questId))
            {
                QuestLogger.Log($"Quest '{questData.DevName}' has already been completed and replaying is disabled.");
                return false;
            }

            if (!allowMultipleActiveQuests && _activeQuests.Count > 0)
            {
                QuestLogger.Log($"Cannot add '{questData.DevName}': Another quest is already active and multiple active quests are disabled.");
                return false;
            }

            QuestRuntime newQuest = _availableQuestsData[questId].GetRuntimeQuest();
            _activeQuests.Add(questId, newQuest);
            SubscribeToQuestEvents(newQuest);

            QuestLogger.Log($"Quest '{questData.DevName}' added.");
            QuestAdded.SafeInvoke(newQuest);

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

#if ODIN_INSPECTOR
        private IEnumerable<Quest_SO> GetAvailableQuests()
        {
            return questsDatabase.Where(q => q != null) ?? Enumerable.Empty<Quest_SO>();
        }
#endif

        /// <summary>
        /// Force completes a quest by its ID.
        /// </summary>
        public void CompleteQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                foreach (TaskRuntime task in quest.Tasks)
                {
                    if (task.CurrentState != TaskState.Completed)
                    {
                        task.CompleteTask();
                    }
                }
            }
            else
            {
                QuestLogger.LogWarning($"CompleteQuest: Quest with ID '{questId}' is not active.");
            }
        }

        /// <summary>
        /// Force completes a quest.
        /// </summary>
        public void CompleteQuest(Quest_SO questData)
        {
            if (questData == null) return;
            CompleteQuest(questData.QuestId);
        }

        /// <summary>
        /// Force completes a quest.
        /// </summary>
        public void CompleteQuest(QuestRuntime quest)
        {
            if (quest == null) return;
            CompleteQuest(quest.QuestId);
        }

        /// <summary>
        /// Fails a quest by its ID.
        /// </summary>
        public void FailQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                quest.FailQuest();
            }
            else
            {
                QuestLogger.LogWarning($"FailQuest: Quest with ID '{questId}' is not active.");
            }
        }

        /// <summary>
        /// Fails a quest.
        /// </summary>
        public void FailQuest(Quest_SO questData)
        {
            if (questData == null) return;
            FailQuest(questData.QuestId);
        }

        /// <summary>
        /// Fails a quest.
        /// </summary>
        public void FailQuest(QuestRuntime quest)
        {
            if (quest == null) return;
            FailQuest(quest.QuestId);
        }

        /// <summary>
        /// Removes a quest from the active quests.
        /// </summary>
        /// <returns>True if the quest was successfully removed.</returns>
        public bool RemoveQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                UnsubscribeFromQuestEvents(quest);
                _activeQuests.Remove(questId);
                QuestLogger.Log($"Quest '{quest.QuestData.DevName}' removed.");
                QuestRemoved.SafeInvoke(quest);
                return true;
            }

            QuestLogger.LogWarning($"RemoveQuest: Quest with ID '{questId}' is not active.");
            return false;
        }

        /// <summary>
        /// Removes a quest from the active quests.
        /// </summary>
        public bool RemoveQuest(Quest_SO questData)
        {
            if (questData == null) return false;
            return RemoveQuest(questData.QuestId);
        }

        /// <summary>
        /// Removes a quest from the active quests.
        /// </summary>
        public bool RemoveQuest(QuestRuntime quest)
        {
            if (quest == null) return false;
            return RemoveQuest(quest.QuestId);
        }

        /// <summary>
        /// Restarts a quest. Works for active, completed, or failed quests.
        /// </summary>
        /// <param name="questId">The quest ID.</param>
        /// <param name="forceStart">If true, starts immediately without checking conditions.</param>
        /// <returns>True if the quest was successfully restarted.</returns>
        public bool RestartQuest(Guid questId, bool forceStart = false)
        {
            QuestRuntime quest = null;

            // Check active quests first
            if (_activeQuests.TryGetValue(questId, out quest))
            {
                quest.ResetQuest();
                return true;
            }

            // Check completed quests
            if (_completedQuests.TryGetValue(questId, out quest))
            {
                _completedQuests.Remove(questId);
                _activeQuests.Add(questId, quest);
                SubscribeToQuestEvents(quest);
                quest.ResetQuest();
                return true;
            }

            // Check failed quests
            if (_failedQuests.TryGetValue(questId, out quest))
            {
                _failedQuests.Remove(questId);
                _activeQuests.Add(questId, quest);
                SubscribeToQuestEvents(quest);
                quest.ResetQuest();
                return true;
            }

            QuestLogger.LogWarning($"RestartQuest: Quest with ID '{questId}' not found in active, completed, or failed quests.");
            return false;
        }

        /// <summary>
        /// Restarts a quest.
        /// </summary>
        public bool RestartQuest(Quest_SO questData, bool forceStart = false)
        {
            if (questData == null) return false;
            return RestartQuest(questData.QuestId, forceStart);
        }

        /// <summary>
        /// Restarts a quest.
        /// </summary>
        public bool RestartQuest(QuestRuntime quest, bool forceStart = false)
        {
            if (quest == null) return false;
            return RestartQuest(quest.QuestId, forceStart);
        }

        #endregion

        #region Event Subscription Management

        private void SubscribeToQuestEvents(QuestRuntime quest)
        {
            quest.OnQuestStarted.SafeSubscribe(HandleQuestStarted);
            quest.OnQuestCompleted.SafeSubscribe(HandleQuestCompleted);
            quest.OnQuestFailed.SafeSubscribe(HandleQuestFailed);
            quest.OnQuestUpdated.SafeSubscribe(HandleQuestUpdated);
            quest.OnQuestRestarted.SafeSubscribe(HandleQuestRestarted);
        }

        private void UnsubscribeFromQuestEvents(QuestRuntime quest)
        {
            if (quest == null) return;

            quest.OnQuestStarted.SafeUnsubscribe(HandleQuestStarted);
            quest.OnQuestCompleted.SafeUnsubscribe(HandleQuestCompleted);
            quest.OnQuestFailed.SafeUnsubscribe(HandleQuestFailed);
            quest.OnQuestUpdated.SafeUnsubscribe(HandleQuestUpdated);
            quest.OnQuestRestarted.SafeUnsubscribe(HandleQuestRestarted);
        }

        private void HandleQuestStarted(QuestRuntime quest)
        {
            QuestStarted.SafeInvoke(quest);
        }

        private void HandleQuestCompleted(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _activeQuests.Remove(quest.QuestId);
            _completedQuests.TryAdd(quest.QuestId, quest);
            QuestLogger.Log($"Quest '{quest.QuestData.DevName}' completed.");
            QuestCompleted.SafeInvoke(quest);
        }

        private void HandleQuestFailed(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _activeQuests.Remove(quest.QuestId);
            _failedQuests.TryAdd(quest.QuestId, quest);
            QuestLogger.Log($"Quest '{quest.QuestData.DevName}' failed.");
            QuestFailed.SafeInvoke(quest);
        }

        private void HandleQuestUpdated(QuestRuntime quest)
        {
            QuestUpdated.SafeInvoke(quest);
        }

        private void HandleQuestRestarted(QuestRuntime quest)
        {
            QuestRestarted.SafeInvoke(quest);
        }

        #endregion

        #region Task Operations

        /// <summary>
        /// Increments the current task's step for a quest.
        /// </summary>
#if ODIN_INSPECTOR
        [TitleGroup("Runtime Actions")]
        [Button("Increment Task Step")]
        [PropertyOrder(71)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
#endif
        public void IncrementTaskStep(
#if ODIN_INSPECTOR
            [ValueDropdown(nameof(GetAvailableQuests))]
#endif
            Quest_SO questData)
        {
            if (questData == null) return;
            IncrementTaskStep(questData.QuestId);
        }

        /// <summary>
        /// Increments the current task's step for a quest.
        /// </summary>
        public void IncrementTaskStep(Guid questId)
        {
            TaskRuntime task = GetCurrentTask(questId);
            task?.IncrementStep();
        }

        /// <summary>
        /// Increments the current task's step for a quest.
        /// </summary>
        public void IncrementTaskStep(QuestRuntime quest)
        {
            if (quest == null) return;
            IncrementTaskStep(quest.QuestId);
        }

        /// <summary>
        /// Decrements the current task's step for a quest.
        /// </summary>
        public void DecrementTaskStep(Quest_SO questData)
        {
            if (questData == null) return;
            DecrementTaskStep(questData.QuestId);
        }

        /// <summary>
        /// Decrements the current task's step for a quest.
        /// </summary>
        public void DecrementTaskStep(Guid questId)
        {
            TaskRuntime task = GetCurrentTask(questId);
            task?.DecrementStep();
        }

        /// <summary>
        /// Decrements the current task's step for a quest.
        /// </summary>
        public void DecrementTaskStep(QuestRuntime quest)
        {
            if (quest == null) return;
            DecrementTaskStep(quest.QuestId);
        }

        /// <summary>
        /// Decrements a specific task's step.
        /// </summary>
        public void DecrementTaskStep(Guid questId, Guid taskId)
        {
            TaskRuntime task = GetTask(questId, taskId);
            task?.DecrementStep();
        }

        /// <summary>
        /// Completes a specific task.
        /// </summary>
        public void CompleteTask(Guid questId, Guid taskId)
        {
            TaskRuntime task = GetTask(questId, taskId);
            task?.CompleteTask();
        }

        /// <summary>
        /// Fails a specific task.
        /// </summary>
        public void FailTask(Guid questId, Guid taskId)
        {
            TaskRuntime task = GetTask(questId, taskId);
            task?.FailTask();
        }

        #endregion

        #region Query & Data Access

        /// <summary>
        /// Gets an active quest by its ID.
        /// </summary>
        public QuestRuntime GetActiveQuest(Guid questId)
        {
            _activeQuests.TryGetValue(questId, out QuestRuntime quest);
            return quest;
        }

        /// <summary>
        /// Gets an active quest by its data.
        /// </summary>
        public QuestRuntime GetActiveQuest(Quest_SO questData)
        {
            if (questData == null) return null;
            return GetActiveQuest(questData.QuestId);
        }

        /// <summary>
        /// Gets all active quests as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestRuntime> GetActiveQuests()
        {
            return _activeQuests.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all completed quests as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestRuntime> GetCompletedQuests()
        {
            return _completedQuests.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all failed quests as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestRuntime> GetFailedQuests()
        {
            return _failedQuests.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all tasks for a quest as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<TaskRuntime> GetTasksForQuest(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                return quest.Tasks.AsReadOnly();
            }
            return null;
        }

        /// <summary>
        /// Gets all tasks for a quest as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<TaskRuntime> GetTasksForQuest(Quest_SO questData)
        {
            if (questData == null) return null;
            return GetTasksForQuest(questData.QuestId);
        }

        /// <summary>
        /// Gets a specific task from a quest.
        /// </summary>
        public TaskRuntime GetTask(Guid questId, Guid taskId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                return quest.Tasks.FirstOrDefault(t => t.TaskId == taskId);
            }
            return null;
        }

        /// <summary>
        /// Gets the current in-progress task for a quest.
        /// </summary>
        public TaskRuntime GetCurrentTask(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                return quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            }
            return null;
        }

        /// <summary>
        /// Gets the current in-progress task for a quest.
        /// </summary>
        public TaskRuntime GetCurrentTask(QuestRuntime quest)
        {
            if (quest == null) return null;
            return quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
        }

        /// <summary>
        /// Gets the current in-progress task for a quest.
        /// </summary>
        public TaskRuntime GetCurrentTask(Quest_SO questData)
        {
            if (questData == null) return null;
            return GetCurrentTask(questData.QuestId);
        }

        /// <summary>
        /// Checks if a quest is currently active.
        /// </summary>
        public bool IsQuestActive(Guid questId)
        {
            return _activeQuests.ContainsKey(questId);
        }

        /// <summary>
        /// Checks if a quest is currently active.
        /// </summary>
        public bool IsQuestActive(Quest_SO questData)
        {
            if (questData == null) return false;
            return IsQuestActive(questData.QuestId);
        }

        /// <summary>
        /// Checks if a quest has been completed.
        /// </summary>
        public bool IsQuestCompleted(Guid questId)
        {
            return _completedQuests.ContainsKey(questId);
        }

        /// <summary>
        /// Checks if a quest has been completed.
        /// </summary>
        public bool IsQuestCompleted(Quest_SO questData)
        {
            if (questData == null) return false;
            return IsQuestCompleted(questData.QuestId);
        }

        /// <summary>
        /// Checks if a quest has failed.
        /// </summary>
        public bool IsQuestFailed(Guid questId)
        {
            return _failedQuests.ContainsKey(questId);
        }

        /// <summary>
        /// Checks if a quest has failed.
        /// </summary>
        public bool IsQuestFailed(Quest_SO questData)
        {
            if (questData == null) return false;
            return IsQuestFailed(questData.QuestId);
        }

        #endregion

        #region Task Group Operations

        /// <summary>
        /// Gets all currently in-progress tasks for a quest (can be multiple for parallel groups).
        /// </summary>
        public IReadOnlyList<TaskRuntime> GetCurrentTasks(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                return quest.CurrentTasks;
            }
            return Array.Empty<TaskRuntime>();
        }

        /// <summary>
        /// Gets all currently in-progress tasks for a quest.
        /// </summary>
        public IReadOnlyList<TaskRuntime> GetCurrentTasks(Quest_SO questData)
        {
            if (questData == null) return Array.Empty<TaskRuntime>();
            return GetCurrentTasks(questData.QuestId);
        }

        /// <summary>
        /// Gets all currently in-progress tasks for a quest.
        /// </summary>
        public IReadOnlyList<TaskRuntime> GetCurrentTasks(QuestRuntime quest)
        {
            if (quest == null) return Array.Empty<TaskRuntime>();
            return quest.CurrentTasks;
        }

        /// <summary>
        /// Gets the current task group for a quest.
        /// </summary>
        public TaskGroupRuntime GetCurrentTaskGroup(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                return quest.CurrentGroup;
            }
            return null;
        }

        /// <summary>
        /// Gets the current task group for a quest.
        /// </summary>
        public TaskGroupRuntime GetCurrentTaskGroup(Quest_SO questData)
        {
            if (questData == null) return null;
            return GetCurrentTaskGroup(questData.QuestId);
        }

        /// <summary>
        /// Gets the current task group for a quest.
        /// </summary>
        public TaskGroupRuntime GetCurrentTaskGroup(QuestRuntime quest)
        {
            return quest.CurrentGroup;
        }

        /// <summary>
        /// Gets all task groups for a quest.
        /// </summary>
        public IReadOnlyList<TaskGroupRuntime> GetTaskGroups(Guid questId)
        {
            if (_activeQuests.TryGetValue(questId, out QuestRuntime quest))
            {
                return quest.TaskGroups.AsReadOnly();
            }
            return Array.Empty<TaskGroupRuntime>();
        }

        /// <summary>
        /// Gets all task groups for a quest.
        /// </summary>
        public IReadOnlyList<TaskGroupRuntime> GetTaskGroups(Quest_SO questData)
        {
            if (questData == null) return Array.Empty<TaskGroupRuntime>();
            return GetTaskGroups(questData.QuestId);
        }

        /// <summary>
        /// Gets all task groups for a quest.
        /// </summary>
        public IReadOnlyList<TaskGroupRuntime> GetTaskGroups(QuestRuntime quest)
        {
            if (quest == null) return Array.Empty<TaskGroupRuntime>();
            return quest.TaskGroups.AsReadOnly();
        }

        /// <summary>
        /// Increments a specific task's step by task ID.
        /// </summary>
        public void IncrementTaskStep(Guid questId, Guid taskId)
        {
            TaskRuntime task = GetTask(questId, taskId);
            task?.IncrementStep();
        }

        #endregion
    }
}
