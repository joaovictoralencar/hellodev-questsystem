using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using HelloDev.QuestSystem.Internal;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.SaveLoad;
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
    /// <para>
    /// Architecture: QuestManager acts as a facade, delegating data storage to
    /// internal registries (QuestRegistry, QuestLineRegistry) while maintaining
    /// the public API for quest and questline lifecycle operations.
    /// </para>
    /// <para>
    /// Supports two initialization modes:
    /// </para>
    /// <list type="bullet">
    /// <item><term>Standalone</term><description>Self-initializes in Awake (default)</description></item>
    /// <item><term>Bootstrap</term><description>Waits for GameBootstrap to call InitializeAsync</description></item>
    /// </list>
    /// <para>
    /// Set <c>initializeOnAwake = false</c> when using with GameBootstrap.
    /// </para>
    /// </remarks>
    public partial class QuestManager : MonoBehaviour, IBootstrapInitializable
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
        private bool _isInitialized;

        #endregion

        #region IBootstrapInitializable

        /// <inheritdoc />
        public int InitializationPriority => 150; // Game systems layer

        /// <inheritdoc />
        public bool IsInitialized => _isInitialized;

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

        // Aggregate Events for Save System
        /// <summary>
        /// Fired whenever quest data changes (quest started, completed, failed, task updated, etc.).
        /// Use this for auto-save triggers. Passes the type of change that occurred.
        /// </summary>
        [HideInInspector] public UnityEvent<QuestDataChangeType> OnQuestDataChanged = new();

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

                // Only self-initialize if in standalone mode
                if (initializeOnAwake)
                {
                    _ = InitializeAsync();
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
                QuestLogger.LogVerbose(LogSubsystem.Manager, $"Auto-adding {questsDatabase.Count} quests from database");
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

        /// <inheritdoc />
        public Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            InitializeManager(questsDatabase);
            _isInitialized = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            ShutdownManager();
            _isInitialized = false;
        }

        /// <summary>
        /// Initializes the quest manager with the given quest data.
        /// </summary>
        /// <param name="allQuestData">The list of all available quest data.</param>
        public void InitializeManager(List<Quest_SO> allQuestData)
        {
            if (allQuestData == null)
            {
                QuestLogger.LogError(LogSubsystem.Manager, "InitializeManager: allQuestData is null");
                return;
            }

            _questRegistry.InitializeDatabase(allQuestData);
            _questLineRegistry.InitializeDatabase(questLinesDatabase);

            QuestLogger.Log(LogSubsystem.Manager, $"Initialized with <b>{_questRegistry.DatabaseCount}</b> quests, <b>{_questLineRegistry.DatabaseCount}</b> questlines");
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

            QuestLogger.LogVerbose(LogSubsystem.Manager, "Shutdown complete");
        }

        #endregion

        #region Quest Lifecycle

        /// <summary>
        /// Adds a quest to the active quests and optionally starts it.
        /// </summary>
        /// <param name="questData">The quest data to add.</param>
        /// <param name="forceStart">If true, starts the quest immediately regardless of conditions.</param>
        /// <param name="skipAutoStart">If true, the quest will not auto-start even if start conditions are met.
        /// Used during save/load restore to prevent auto-starting quests that should remain NotStarted.</param>
        /// <param name="skipEventSubscription">If true, the quest will not subscribe to start condition events.
        /// Used during save/load restore to prevent events from triggering auto-start before restore completes.</param>
        /// <returns>True if the quest was successfully added.</returns>
        public bool AddQuest(Quest_SO questData, bool forceStart = false, bool skipAutoStart = false, bool skipEventSubscription = false)
        {
            if (questData == null)
            {
                QuestLogger.LogError(LogSubsystem.Manager, "AddQuest: questData is null");
                return false;
            }

            Guid questId = questData.QuestId;

            // Validation checks
            if (requireQuestInDatabase && !_questRegistry.IsInDatabase(questId))
            {
                QuestLogger.LogWarning(LogSubsystem.Manager, $"Quest '{questData.DevName}' not in database");
                return false;
            }

            if (_questRegistry.IsActive(questId))
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{questData.DevName}' already active");
                return false;
            }

            if (!allowReplayingCompletedQuests && _questRegistry.IsCompleted(questId))
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{questData.DevName}' already completed, replay disabled");
                return false;
            }

            if (!allowMultipleActiveQuests && _questRegistry.ActiveCount > 0)
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"Cannot add '{questData.DevName}': multiple active quests disabled");
                return false;
            }

            // Create runtime quest - use database version if available
            Quest_SO sourceData = _questRegistry.GetFromDatabase(questId) ?? questData;
            QuestRuntime newQuest = sourceData.GetRuntimeQuest();

            if (!_questRegistry.AddActive(newQuest))
            {
                QuestLogger.LogError(LogSubsystem.Manager, $"Failed to add quest '{questData.DevName}'");
                return false;
            }

            SubscribeToQuestEvents(newQuest);

            QuestLogger.Log(LogSubsystem.Quest, $"Added <b>'{questData.DevName}'</b>");
            QuestAdded.SafeInvoke(newQuest);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestAdded);

            // Start or wait for conditions
            // When skipAutoStart is true (e.g., during restore), don't auto-start even if conditions are met
            if (!skipAutoStart && (forceStart || newQuest.CheckStartConditions()))
            {
                newQuest.StartQuest();
            }
            else if (!skipEventSubscription)
            {
                // Only subscribe to events if not skipping event subscription
                // (e.g., during restore, we skip to prevent events from triggering auto-start)
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
                QuestLogger.LogWarning(LogSubsystem.Quest, $"Cannot fail '{questData.DevName}': not active");
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
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{quest.QuestData.DevName}' removed");
                QuestRemoved.SafeInvoke(quest);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestUpdated);
                return true;
            }

            QuestLogger.LogWarning(LogSubsystem.Quest, $"Cannot remove '{questData.DevName}': not active");
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

            QuestLogger.LogWarning(LogSubsystem.Quest, $"Cannot restart '{questData.DevName}': not found");
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
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestStarted);
        }

        private void HandleQuestCompleted(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _questRegistry.MoveToCompleted(quest.QuestId);
            QuestLogger.LogComplete(LogSubsystem.Quest, "Quest", quest.QuestData.DevName);
            QuestCompleted.SafeInvoke(quest);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestCompleted);

            // Notify questlines that contain this quest
            NotifyQuestLinesOfQuestCompleted(quest);
        }

        private void HandleQuestFailed(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _questRegistry.MoveToFailed(quest.QuestId);
            QuestLogger.LogFail(LogSubsystem.Quest, "Quest", quest.QuestData.DevName);
            QuestFailed.SafeInvoke(quest);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestFailed);

            // Notify questlines that contain this quest
            NotifyQuestLinesOfQuestFailed(quest);
        }

        private void HandleQuestUpdated(QuestRuntime quest)
        {
            QuestUpdated.SafeInvoke(quest);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestUpdated);
        }

        private void HandleQuestRestarted(QuestRuntime quest)
        {
            QuestRestarted.SafeInvoke(quest);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestRestarted);
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
                QuestLogger.LogError(LogSubsystem.Manager, "AddQuestLine: lineData is null");
                return false;
            }

            Guid lineId = lineData.QuestLineId;

            if (_questLineRegistry.IsActive(lineId))
            {
                QuestLogger.LogVerbose(LogSubsystem.QuestLine, $"'{lineData.DevName}' already active");
                return false;
            }

            if (_questLineRegistry.IsCompleted(lineId))
            {
                QuestLogger.LogVerbose(LogSubsystem.QuestLine, $"'{lineData.DevName}' already completed");
                return false;
            }

            // Create runtime questline
            QuestLineRuntime newLine = lineData.GetRuntimeQuestLine();

            // Check prerequisite
            if (!newLine.CheckPrerequisite())
            {
                QuestLogger.LogVerbose(LogSubsystem.QuestLine, $"'{lineData.DevName}' prerequisite not met");
                return false;
            }

            if (!_questLineRegistry.AddActive(newLine))
            {
                QuestLogger.LogError(LogSubsystem.Manager, $"Failed to add questline '{lineData.DevName}'");
                return false;
            }

            SubscribeToQuestLineEvents(newLine);

            QuestLogger.Log(LogSubsystem.QuestLine, $"Added <b>'{lineData.DevName}'</b>");
            QuestLineAdded.SafeInvoke(newLine);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineAdded);

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
            QuestLogger.LogStart(LogSubsystem.QuestLine, "QuestLine", line.Data.DevName);
            QuestLineStarted.SafeInvoke(line);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineStarted);
        }

        private void HandleQuestLineCompleted(QuestLineRuntime line)
        {
            UnsubscribeFromQuestLineEvents(line);
            _questLineRegistry.MoveToCompleted(line.QuestLineId);
            line.DistributeCompletionRewards();
            QuestLogger.LogComplete(LogSubsystem.QuestLine, "QuestLine", line.Data.DevName);
            QuestLineCompleted.SafeInvoke(line);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineCompleted);
        }

        private void HandleQuestLineUpdated(QuestLineRuntime line)
        {
            QuestLineUpdated.SafeInvoke(line);
        }

        private void HandleQuestLineFailed(QuestLineRuntime line)
        {
            UnsubscribeFromQuestLineEvents(line);
            _questLineRegistry.RemoveActive(line.QuestLineId);
            QuestLogger.LogFail(LogSubsystem.QuestLine, "QuestLine", line.Data.DevName);
            QuestLineFailed.SafeInvoke(line);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineFailed);
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
        /// Re-subscribes all NotStarted quests to their start condition events.
        /// Called after save/load restore completes to enable auto-start for restored NotStarted quests.
        /// </summary>
        public void ResubscribeNotStartedQuestsToEvents()
        {
            foreach (var quest in _questRegistry.GetAllActive())
            {
                if (quest.CurrentState == QuestState.NotStarted)
                {
                    quest.SubscribeToStartQuestEvents();
                    QuestLogger.LogVerbose(LogSubsystem.Quest, $"Re-subscribed '{quest.QuestData.DevName}' to start events");
                }
            }
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
