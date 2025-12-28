using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HelloDev.QuestSystem.Internal;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
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
    /// state, and event delegation. It provides a clean API for game systems to
    /// interact with quest data without knowing its internal logic.
    /// </summary>
    /// <remarks>
    /// Architecture: QuestManager acts as a facade, delegating data storage to
    /// internal registries (QuestRegistry, QuestLineRegistry) while maintaining
    /// the public API for quest and questline lifecycle operations.
    /// </remarks>
    public partial class QuestManager : MonoBehaviour
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
        [TitleGroup("QuestLine Database")]
        [PropertyOrder(1)]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        [InfoBox("$" + nameof(GetQuestLineDatabaseInfoMessage), InfoMessageType.Info)]
#else
        [Header("QuestLine Database")]
#endif
        [Tooltip("The list of all available questlines in the game.")]
        [SerializeField]
        private List<QuestLine_SO> questLinesDatabase = new();

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
        [Tooltip("If true, all quests in the database will be added on Start. Quests only start if their start conditions are met.")]
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

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [PropertyOrder(15)]
        [ToggleLeft]
#endif
        [Tooltip("If true, only quests in the database can be added. If false, any Quest_SO can be added.")]
        [SerializeField]
        private bool requireQuestInDatabase = true;

        #endregion

        #region Internal Registries

        private readonly QuestRegistry _questRegistry = new();
        private readonly QuestLineRegistry _questLineRegistry = new();

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

        // QuestLine Events
        /// <summary>Fired when a questline is added to tracking.</summary>
        [HideInInspector] public UnityEvent<QuestLineRuntime> QuestLineAdded = new();

        /// <summary>Fired when a questline starts (first quest starts).</summary>
        [HideInInspector] public UnityEvent<QuestLineRuntime> QuestLineStarted = new();

        /// <summary>Fired when questline progress changes.</summary>
        [HideInInspector] public UnityEvent<QuestLineRuntime> QuestLineUpdated = new();

        /// <summary>Fired when a questline is completed.</summary>
        [HideInInspector] public UnityEvent<QuestLineRuntime> QuestLineCompleted = new();

        /// <summary>Fired when a questline fails.</summary>
        [HideInInspector] public UnityEvent<QuestLineRuntime> QuestLineFailed = new();

        #endregion

        #region Properties

        /// <summary>The singleton instance of the QuestManager.</summary>
        public static QuestManager Instance { get; private set; }

        /// <summary>Read-only access to the quest database.</summary>
        public IReadOnlyList<Quest_SO> QuestsDatabase => questsDatabase;

        /// <summary>Gets the count of active quests.</summary>
        public int ActiveQuestCount => _questRegistry.ActiveCount;

        /// <summary>Gets the count of completed quests.</summary>
        public int CompletedQuestCount => _questRegistry.CompletedCount;

        /// <summary>Gets the count of failed quests.</summary>
        public int FailedQuestCount => _questRegistry.FailedCount;

        /// <summary>Configuration: Whether multiple quests can be active simultaneously.</summary>
        public bool AllowMultipleActiveQuests => allowMultipleActiveQuests;

        /// <summary>Configuration: Whether completed quests can be replayed.</summary>
        public bool AllowReplayingCompletedQuests => allowReplayingCompletedQuests;

        /// <summary>Configuration: Whether quests must be in the database to be added.</summary>
        public bool RequireQuestInDatabase => requireQuestInDatabase;

        /// <summary>Read-only access to the questline database.</summary>
        public IReadOnlyList<QuestLine_SO> QuestLinesDatabase => questLinesDatabase;

        /// <summary>Gets the count of active questlines.</summary>
        public int ActiveQuestLineCount => _questLineRegistry.ActiveCount;

        /// <summary>Gets the count of completed questlines.</summary>
        public int CompletedQuestLineCount => _questLineRegistry.CompletedCount;

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
                foreach (Quest_SO quest in questsDatabase)
                {
                    if (quest != null)
                    {
                        AddQuest(quest, forceStart: false);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // Unsubscribe from all quest events
                foreach (QuestRuntime quest in _questRegistry.ActiveQuestsEnumerable)
                {
                    UnsubscribeFromQuestEvents(quest);
                }
                foreach (QuestLineRuntime line in _questLineRegistry.ActiveQuestLinesEnumerable)
                {
                    UnsubscribeFromQuestLineEvents(line);
                }

                // Clear registries
                _questRegistry.ClearRuntimeState();
                _questLineRegistry.ClearRuntimeState();

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

            _questRegistry.InitializeDatabase(allQuestData);
            _questLineRegistry.InitializeDatabase(questLinesDatabase);

            QuestLogger.Log($"QuestManager initialized with {_questRegistry.DatabaseCount} quests and {_questLineRegistry.DatabaseCount} questlines.");
        }

        /// <summary>
        /// Shuts down the quest manager and clears all state.
        /// </summary>
        public void ShutdownManager()
        {
            // Unsubscribe from all events
            foreach (QuestRuntime quest in _questRegistry.ActiveQuestsEnumerable)
            {
                UnsubscribeFromQuestEvents(quest);
            }
            foreach (QuestLineRuntime line in _questLineRegistry.ActiveQuestLinesEnumerable)
            {
                UnsubscribeFromQuestLineEvents(line);
            }

            // Clear registries
            _questRegistry.ClearRuntimeState();
            _questLineRegistry.ClearRuntimeState();

            QuestLogger.Log("QuestManager shut down.");
        }

        #endregion

        #region Quest Lifecycle

        /// <summary>
        /// Adds a quest to the active quests and optionally starts it.
        /// </summary>
        /// <param name="questData">The quest data to add.</param>
        /// <param name="forceStart">If true, starts the quest immediately regardless of conditions.</param>
        /// <returns>True if the quest was successfully added.</returns>
        public bool AddQuest(Quest_SO questData, bool forceStart = false)
        {
            if (questData == null)
            {
                QuestLogger.LogError("AddQuest: questData is null.");
                return false;
            }

            Guid questId = questData.QuestId;

            // Validation checks
            if (requireQuestInDatabase && !_questRegistry.IsInDatabase(questId))
            {
                QuestLogger.LogWarning($"Quest '{questData.DevName}' is not in the available quests database.");
                return false;
            }

            if (_questRegistry.IsActive(questId))
            {
                QuestLogger.Log($"Quest '{questData.DevName}' is already active.");
                return false;
            }

            if (!allowReplayingCompletedQuests && _questRegistry.IsCompleted(questId))
            {
                QuestLogger.Log($"Quest '{questData.DevName}' has already been completed and replaying is disabled.");
                return false;
            }

            if (!allowMultipleActiveQuests && _questRegistry.ActiveCount > 0)
            {
                QuestLogger.Log($"Cannot add '{questData.DevName}': Another quest is already active and multiple active quests are disabled.");
                return false;
            }

            // Create runtime quest - use database version if available
            Quest_SO sourceData = _questRegistry.GetFromDatabase(questId) ?? questData;
            QuestRuntime newQuest = sourceData.GetRuntimeQuest();

            if (!_questRegistry.AddActive(newQuest))
            {
                QuestLogger.LogError($"Failed to add quest '{questData.DevName}' to active quests.");
                return false;
            }

            SubscribeToQuestEvents(newQuest);

            QuestLogger.Log($"Quest '{questData.DevName}' added.");
            QuestAdded.SafeInvoke(newQuest);

            // Start or wait for conditions
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

        /// <summary>
        /// Fails a quest.
        /// </summary>
        public void FailQuest(Quest_SO questData)
        {
            if (questData == null) return;

            QuestRuntime quest = _questRegistry.GetActive(questData.QuestId);
            if (quest != null)
            {
                quest.FailQuest();
            }
            else
            {
                QuestLogger.LogWarning($"FailQuest: Quest '{questData.DevName}' is not active.");
            }
        }

        /// <summary>
        /// Removes a quest from the active quests.
        /// </summary>
        /// <returns>True if the quest was successfully removed.</returns>
        public bool RemoveQuest(Quest_SO questData)
        {
            if (questData == null) return false;

            Guid questId = questData.QuestId;
            QuestRuntime quest = _questRegistry.GetActive(questId);

            if (quest != null)
            {
                UnsubscribeFromQuestEvents(quest);
                _questRegistry.RemoveActive(questId);
                QuestLogger.Log($"Quest '{quest.QuestData.DevName}' removed.");
                QuestRemoved.SafeInvoke(quest);
                return true;
            }

            QuestLogger.LogWarning($"RemoveQuest: Quest '{questData.DevName}' is not active.");
            return false;
        }

        /// <summary>
        /// Restarts a quest. Works for active, completed, or failed quests.
        /// </summary>
        /// <param name="questData">The quest data.</param>
        /// <param name="forceStart">If true, starts immediately without checking conditions.</param>
        /// <returns>True if the quest was successfully restarted.</returns>
        public bool RestartQuest(Quest_SO questData, bool forceStart = false)
        {
            if (questData == null) return false;

            Guid questId = questData.QuestId;
            QuestRuntime quest;

            // Check active quests first
            quest = _questRegistry.GetActive(questId);
            if (quest != null)
            {
                quest.ResetQuest();
                return true;
            }

            // Check completed quests
            quest = _questRegistry.GetCompleted(questId);
            if (quest != null)
            {
                _questRegistry.MoveFromCompletedToActive(questId);
                SubscribeToQuestEvents(quest);
                quest.ResetQuest();
                return true;
            }

            // Check failed quests
            quest = _questRegistry.GetFailed(questId);
            if (quest != null)
            {
                _questRegistry.MoveFromFailedToActive(questId);
                SubscribeToQuestEvents(quest);
                quest.ResetQuest();
                return true;
            }

            QuestLogger.LogWarning($"RestartQuest: Quest '{questData.DevName}' not found in active, completed, or failed quests.");
            return false;
        }

        #endregion

        #region Quest Event Subscription

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
            _questRegistry.MoveToCompleted(quest.QuestId);
            QuestLogger.Log($"Quest '{quest.QuestData.DevName}' completed.");
            QuestCompleted.SafeInvoke(quest);

            // Notify questlines that contain this quest
            NotifyQuestLinesOfQuestCompleted(quest);
        }

        private void HandleQuestFailed(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _questRegistry.MoveToFailed(quest.QuestId);
            QuestLogger.Log($"Quest '{quest.QuestData.DevName}' failed.");
            QuestFailed.SafeInvoke(quest);

            // Notify questlines that contain this quest
            NotifyQuestLinesOfQuestFailed(quest);
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

        #region QuestLine Lifecycle

        /// <summary>
        /// Adds a questline to tracking.
        /// </summary>
        /// <param name="lineData">The questline data to add.</param>
        /// <returns>True if the questline was successfully added.</returns>
        public bool AddQuestLine(QuestLine_SO lineData)
        {
            if (lineData == null)
            {
                QuestLogger.LogError("AddQuestLine: lineData is null.");
                return false;
            }

            Guid lineId = lineData.QuestLineId;

            if (_questLineRegistry.IsActive(lineId))
            {
                QuestLogger.Log($"QuestLine '{lineData.DevName}' is already active.");
                return false;
            }

            if (_questLineRegistry.IsCompleted(lineId))
            {
                QuestLogger.Log($"QuestLine '{lineData.DevName}' has already been completed.");
                return false;
            }

            // Create runtime questline
            QuestLineRuntime newLine = lineData.GetRuntimeQuestLine();

            // Check prerequisite
            if (!newLine.CheckPrerequisite())
            {
                QuestLogger.Log($"QuestLine '{lineData.DevName}' prerequisite not met. Not adding.");
                return false;
            }

            if (!_questLineRegistry.AddActive(newLine))
            {
                QuestLogger.LogError($"Failed to add questline '{lineData.DevName}' to active questlines.");
                return false;
            }

            SubscribeToQuestLineEvents(newLine);

            QuestLogger.Log($"QuestLine '{lineData.DevName}' added.");
            QuestLineAdded.SafeInvoke(newLine);

            // Check initial progress (some quests may already be complete)
            newLine.CheckProgress();

            return true;
        }

        /// <summary>
        /// Gets an active or completed questline by its data.
        /// </summary>
        public QuestLineRuntime GetQuestLine(QuestLine_SO lineData)
        {
            if (lineData == null) return null;

            Guid lineId = lineData.QuestLineId;
            return _questLineRegistry.GetActive(lineId) ?? _questLineRegistry.GetCompleted(lineId);
        }

        /// <summary>
        /// Checks if a questline has been completed.
        /// </summary>
        public bool IsQuestLineCompleted(QuestLine_SO lineData)
        {
            if (lineData == null) return false;
            return _questLineRegistry.IsCompleted(lineData.QuestLineId);
        }

        /// <summary>
        /// Checks if a questline is currently active.
        /// </summary>
        public bool IsQuestLineActive(QuestLine_SO lineData)
        {
            if (lineData == null) return false;
            return _questLineRegistry.IsActive(lineData.QuestLineId);
        }

        /// <summary>
        /// Gets all active questlines as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestLineRuntime> GetActiveQuestLines()
        {
            return _questLineRegistry.GetAllActive().ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all completed questlines as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestLineRuntime> GetCompletedQuestLines()
        {
            return _questLineRegistry.GetAllCompleted().ToList().AsReadOnly();
        }

        #endregion

        #region QuestLine Event Subscription

        private void SubscribeToQuestLineEvents(QuestLineRuntime line)
        {
            line.OnQuestLineStarted.SafeSubscribe(HandleQuestLineStarted);
            line.OnQuestLineCompleted.SafeSubscribe(HandleQuestLineCompleted);
            line.OnQuestLineUpdated.SafeSubscribe(HandleQuestLineUpdated);
            line.OnQuestLineFailed.SafeSubscribe(HandleQuestLineFailed);
        }

        private void UnsubscribeFromQuestLineEvents(QuestLineRuntime line)
        {
            if (line == null) return;

            line.OnQuestLineStarted.SafeUnsubscribe(HandleQuestLineStarted);
            line.OnQuestLineCompleted.SafeUnsubscribe(HandleQuestLineCompleted);
            line.OnQuestLineUpdated.SafeUnsubscribe(HandleQuestLineUpdated);
            line.OnQuestLineFailed.SafeUnsubscribe(HandleQuestLineFailed);
        }

        private void HandleQuestLineStarted(QuestLineRuntime line)
        {
            QuestLogger.Log($"QuestLine '{line.Data.DevName}' started.");
            QuestLineStarted.SafeInvoke(line);
        }

        private void HandleQuestLineCompleted(QuestLineRuntime line)
        {
            UnsubscribeFromQuestLineEvents(line);
            _questLineRegistry.MoveToCompleted(line.QuestLineId);
            line.DistributeCompletionRewards();
            QuestLogger.Log($"QuestLine '{line.Data.DevName}' completed.");
            QuestLineCompleted.SafeInvoke(line);
        }

        private void HandleQuestLineUpdated(QuestLineRuntime line)
        {
            QuestLineUpdated.SafeInvoke(line);
        }

        private void HandleQuestLineFailed(QuestLineRuntime line)
        {
            UnsubscribeFromQuestLineEvents(line);
            _questLineRegistry.RemoveActive(line.QuestLineId);
            QuestLogger.Log($"QuestLine '{line.Data.DevName}' failed.");
            QuestLineFailed.SafeInvoke(line);
        }

        /// <summary>
        /// Notifies all active questlines when a quest completes.
        /// </summary>
        private void NotifyQuestLinesOfQuestCompleted(QuestRuntime quest)
        {
            foreach (var line in _questLineRegistry.ActiveQuestLinesEnumerable.ToList())
            {
                if (line.Data.Quests.Contains(quest.QuestData))
                {
                    line.NotifyQuestCompleted(quest);
                }
            }
        }

        /// <summary>
        /// Notifies all active questlines when a quest fails.
        /// </summary>
        private void NotifyQuestLinesOfQuestFailed(QuestRuntime quest)
        {
            foreach (var line in _questLineRegistry.ActiveQuestLinesEnumerable.ToList())
            {
                if (line.Data.Quests.Contains(quest.QuestData))
                {
                    line.NotifyQuestFailed(quest);
                }
            }
        }

        #endregion

        #region Query & Data Access

        /// <summary>
        /// Gets an active quest by its data.
        /// </summary>
        public QuestRuntime GetActiveQuest(Quest_SO questData)
        {
            if (questData == null) return null;
            return _questRegistry.GetActive(questData.QuestId);
        }

        /// <summary>
        /// Gets all active quests as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestRuntime> GetActiveQuests()
        {
            return _questRegistry.GetAllActive().ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all completed quests as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestRuntime> GetCompletedQuests()
        {
            return _questRegistry.GetAllCompleted().ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all failed quests as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestRuntime> GetFailedQuests()
        {
            return _questRegistry.GetAllFailed().ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if a quest is currently active.
        /// </summary>
        public bool IsQuestActive(Quest_SO questData)
        {
            if (questData == null) return false;
            return _questRegistry.IsActive(questData.QuestId);
        }

        /// <summary>
        /// Checks if a quest has been completed.
        /// </summary>
        public bool IsQuestCompleted(Quest_SO questData)
        {
            if (questData == null) return false;
            return _questRegistry.IsCompleted(questData.QuestId);
        }

        /// <summary>
        /// Checks if a quest has failed.
        /// </summary>
        public bool IsQuestFailed(Quest_SO questData)
        {
            if (questData == null) return false;
            return _questRegistry.IsFailed(questData.QuestId);
        }

        #endregion

        #region Internal Registry Access (for Editor)

        /// <summary>
        /// Gets the internal quest registry. Used by QuestManager.Editor.cs.
        /// </summary>
        internal QuestRegistry QuestRegistry => _questRegistry;

        /// <summary>
        /// Gets the internal questline registry. Used by QuestManager.Editor.cs.
        /// </summary>
        internal QuestLineRegistry QuestLineRegistry => _questLineRegistry;

        #endregion
    }
}
