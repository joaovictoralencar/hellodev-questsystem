using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using HelloDev.Bootstrap;
using HelloDev.Conditions;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.Internal;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Saving;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// Determines how quests are auto-added from the database on initialization.
    /// </summary>
    public enum QuestAutoAddMode
    {
        /// <summary>Do not auto-add any quests. Use for normal gameplay where quests are added via events/NPCs.</summary>
        Disabled,
        /// <summary>Add all quests from database regardless of start conditions. Useful for debugging.</summary>
        AllQuests,
        /// <summary>Only add quests whose start conditions are already met.</summary>
        WithConditionsMet
    }

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
#endif
        [Tooltip("Controls how quests are auto-added from the database on initialization.\n\n" +
                 "Disabled: No auto-add. Quests added via gameplay (NPCs, events, etc.).\n" +
                 "AllQuests: Add all quests regardless of conditions (debug mode).\n" +
                 "WithConditionsMet: Only add quests whose start conditions are met.")]
        [SerializeField]
        private QuestAutoAddMode autoAddMode = QuestAutoAddMode.Disabled;

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

#if ODIN_INSPECTOR
        [TitleGroup("Save System")]
        [PropertyOrder(20)]
#else
        [Header("Save System")]
#endif
        [Tooltip("The unified save locator for registering the snapshot provider. Optional - if not set, snapshot provider won't auto-register.")]
        [SerializeField]
        private UnifiedSaveLocator_SO unifiedSaveLocator;

#if ODIN_INSPECTOR
        [TitleGroup("Save System")]
        [PropertyOrder(21)]
#endif
        [Tooltip("All WorldFlag assets in the game. Required for save/load of world state.")]
        [SerializeField]
        private List<WorldFlagBase_SO> worldFlagRegistry = new();

#if ODIN_INSPECTOR
        [TitleGroup("Save System")]
        [PropertyOrder(22)]
#endif
        [Tooltip("Optional: Use a WorldFlagRegistry_SO for easier management of world flags.")]
        [SerializeField]
        private WorldFlagRegistry_SO worldFlagRegistryAsset;

#if ODIN_INSPECTOR
        [TitleGroup("Save System")]
        [PropertyOrder(23)]
#endif
        [Tooltip("The WorldFlagLocator_SO for accessing flag runtime values during save/load.")]
        [SerializeField]
        private WorldFlagLocator_SO worldFlagLocator;

        #endregion

        #region Internal Registries

        private readonly QuestRegistry _questRegistry = new();
        private readonly QuestLineRegistry _questLineRegistry = new();
        private bool _isInitialized;

        /// <summary>
        /// Tracks nested event processing depth. When > 0, events are being processed
        /// and operations like Load should be deferred to prevent invalid state.
        /// </summary>
        private int _eventProcessingDepth;

        /// <summary>
        /// The snapshot provider for unified save system integration.
        /// </summary>
        private QuestSnapshotProvider _snapshotProvider;

        #endregion

        #region Operation Guards

        /// <summary>
        /// Returns true if the manager is currently processing events.
        /// Operations like Load should check this and defer if true.
        /// </summary>
        public bool IsProcessingEvents => _eventProcessingDepth > 0;

        /// <summary>
        /// Returns true if any active quest is currently transitioning between stages.
        /// Save operations should check this and defer if true to avoid inconsistent snapshots.
        /// </summary>
        public bool IsAnyQuestTransitioning
        {
            get
            {
                foreach (var quest in _questRegistry.GetAllActive())
                {
                    if (quest.IsTransitioningStage)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if it's safe to perform save/load operations.
        /// Both event processing and stage transitions must be complete.
        /// </summary>
        public bool IsSafeForSaveLoad => !IsProcessingEvents && !IsAnyQuestTransitioning;

        /// <summary>
        /// Call before firing events. Supports nesting.
        /// </summary>
        private void BeginEventProcessing() => _eventProcessingDepth++;

        /// <summary>
        /// Call after firing events (in finally block). Supports nesting.
        /// </summary>
        private void EndEventProcessing()
        {
            _eventProcessingDepth--;
            if (_eventProcessingDepth < 0)
            {
                QuestLogger.LogWarning(LogSubsystem.Manager, "Event processing depth went negative - mismatched Begin/End calls");
                _eventProcessingDepth = 0;
            }
        }

        #endregion

        #region IBootstrapInitializable

        /// <inheritdoc />
        public bool SelfInitialize => initializeOnAwake;

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

        /// <summary>Fired when a questline is removed from tracking.</summary>
        [HideInInspector] public UnityEvent<QuestLineRuntime> QuestLineRemoved = new();

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

        /// <summary>Gets the snapshot provider for unified save system integration.</summary>
        public QuestSnapshotProvider SnapshotProvider => _snapshotProvider;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Set singleton early for bootstrap mode (InitializeAsync may be called before Awake)
            if (Instance == null)
            {
                SetupSingleton();

                // Only self-initialize if in standalone mode
                if (initializeOnAwake)
                {
                    _ = InitializeAsync();
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void SetupSingleton()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            QuestLogger.IsLoggingEnabled = enableDebugLogging;
        }

        private void Start()
        {
            // Auto-add is now handled in InitializeAsync() for proper bootstrap ordering
            // This allows save loading (priority 250) to overwrite the initial state
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // Unsubscribe from bootstrap event (if still subscribed)
                GameBootstrap.OnBootstrapComplete -= HandleBootstrapComplete;

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

            QuestLogger.Log(LogSubsystem.Manager, "Starting initialization...");

            // Ensure singleton is set (bootstrap may call this before Awake)
            if (Instance == null)
                SetupSingleton();

            InitializeManager(questsDatabase);

            // Subscribe to post-bootstrap hook for auto-adding quests
            // This ensures save loading (priority 250) completes before quests auto-start
            if (autoAddMode != QuestAutoAddMode.Disabled)
            {
                if (initializeOnAwake)
                {
                    // Standalone mode: auto-add immediately (no bootstrap)
                    AutoAddQuestsFromDatabase();
                }
                else
                {
                    // Bootstrap mode: defer auto-add until after all systems are ready
                    GameBootstrap.OnBootstrapComplete += HandleBootstrapComplete;
                    QuestLogger.LogVerbose(LogSubsystem.Manager, "Auto-add deferred until post-bootstrap");
                }
            }

            // Create and register snapshot provider for unified save system
            CreateAndRegisterSnapshotProvider();

            _isInitialized = true;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates the QuestSnapshotProvider and registers it with the unified save system.
        /// </summary>
        private void CreateAndRegisterSnapshotProvider()
        {
            _snapshotProvider = new QuestSnapshotProvider(this, worldFlagLocator, GetAllWorldFlags);

            if (unifiedSaveLocator != null)
            {
                unifiedSaveLocator.RegisterSystem(_snapshotProvider);
                QuestLogger.Log(LogSubsystem.Manager, "QuestSnapshotProvider registered with unified save system");
            }
            else
            {
                QuestLogger.LogVerbose(LogSubsystem.Manager, "No UnifiedSaveLocator assigned - snapshot provider created but not registered");
            }
        }

        /// <summary>
        /// Gets all world flags from the registry and registry asset.
        /// </summary>
        private List<WorldFlagBase_SO> GetAllWorldFlags()
        {
            var allFlags = new List<WorldFlagBase_SO>(worldFlagRegistry);

            // Add flags from registry asset
            if (worldFlagRegistryAsset != null)
            {
                foreach (var flag in worldFlagRegistryAsset.AllFlags)
                {
                    if (flag != null && !allFlags.Contains(flag))
                        allFlags.Add(flag);
                }
            }

            return allFlags;
        }

        /// <summary>
        /// Called after all bootstrap systems have completed initialization.
        /// Auto-adds quests from the database based on the configured mode.
        /// </summary>
        private void HandleBootstrapComplete()
        {
            // Unsubscribe immediately to prevent multiple calls
            GameBootstrap.OnBootstrapComplete -= HandleBootstrapComplete;

            QuestLogger.Log(LogSubsystem.Manager, "[PostBootstrap] Auto-adding quests from database...");
            AutoAddQuestsFromDatabase();
        }

        /// <summary>
        /// Auto-adds quests from the database based on the configured autoAddMode.
        /// </summary>
        private void AutoAddQuestsFromDatabase()
        {
            if (autoAddMode == QuestAutoAddMode.Disabled)
                return;

            QuestLogger.LogVerbose(LogSubsystem.Manager, $"Auto-add mode: {autoAddMode}, checking {questsDatabase.Count} quests");

            int addedCount = 0;
            foreach (Quest_SO quest in questsDatabase)
            {
                if (quest == null) continue;

                // Skip quests already in any registry (may have been restored from save)
                Guid questId = quest.QuestId;
                if (_questRegistry.IsActive(questId) ||
                    _questRegistry.IsCompleted(questId) ||
                    _questRegistry.IsFailed(questId))
                {
                    continue;
                }

                bool shouldAdd = autoAddMode == QuestAutoAddMode.AllQuests || CanQuestBeAdded(quest);
                if (shouldAdd)
                {
                    AddQuest(quest);
                    addedCount++;
                }
            }

            QuestLogger.Log(LogSubsystem.Manager, $"[PostBootstrap] Auto-added {addedCount} quests");
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            // Unregister snapshot provider from unified save system
            if (_snapshotProvider != null && unifiedSaveLocator != null)
            {
                unifiedSaveLocator.UnregisterSystem(_snapshotProvider);
                QuestLogger.LogVerbose(LogSubsystem.Manager, "QuestSnapshotProvider unregistered from unified save system");
            }
            _snapshotProvider = null;

            ShutdownManager();
            _isInitialized = false;
        }

        /// <summary>
        /// Initializes the quest manager with the given quest data.
        /// </summary>
        /// <param name="allQuestData">The list of all available quest data.</param>
        /// <param name="isRestore">True if this initialization is restoring from a save file.</param>
        public void InitializeManager(List<Quest_SO> allQuestData, bool isRestore = false)
        {
            if (allQuestData == null)
            {
                QuestLogger.LogError(LogSubsystem.Manager, "InitializeManager: allQuestData is null");
                return;
            }

            _questRegistry.InitializeDatabase(allQuestData);
            _questLineRegistry.InitializeDatabase(questLinesDatabase);

#if UNITY_EDITOR
            ValidateDuplicateGUIDs();
#endif

            string action = isRestore ? "Restored runtime state" : "Database initialized";
            QuestLogger.Log(LogSubsystem.Manager, $"{action}: <b>{_questRegistry.DatabaseCount}</b> quests, <b>{_questLineRegistry.DatabaseCount}</b> questlines");
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
        /// Checks if a quest's start conditions are met (or has no conditions).
        /// Used by <see cref="QuestAutoAddMode.WithConditionsMet"/> mode.
        /// </summary>
        /// <param name="questData">The quest data to check.</param>
        /// <returns>True if the quest has no start conditions or all conditions evaluate to true.</returns>
        private bool CanQuestBeAdded(Quest_SO questData)
        {
            if (questData == null)
                return false;

            var conditions = questData.StartConditions;
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (Condition_SO condition in conditions)
            {
                if (condition != null && !condition.Evaluate())
                {
                    QuestLogger.LogVerbose(LogSubsystem.Manager,
                        $"Quest '{questData.DevName}' skipped: start condition '{condition.name}' not met");
                    return false;
                }
            }

            return true;
        }

        #region AddQuest Methods

        /// <summary>
        /// Adds a quest to tracking. If start conditions are met, the quest starts automatically.
        /// If conditions are not met, the quest subscribes to events and will start when conditions become true.
        /// </summary>
        /// <param name="questData">The quest data to add.</param>
        /// <returns>True if the quest was successfully added.</returns>
        public bool AddQuest(Quest_SO questData)
        {
            var quest = AddQuestCore(questData);
            if (quest == null) return false;

            // Check conditions and start if met, otherwise subscribe to events
            bool conditionsMet = quest.CheckStartConditions();
            QuestLogger.LogVerbose(LogSubsystem.Quest, $"Quest '{questData.DevName}': conditionsMet={conditionsMet}");

            if (conditionsMet)
            {
                quest.StartQuest();
            }
            else
            {
                quest.SubscribeToStartQuestEvents();
            }

            return true;
        }

        /// <summary>
        /// Adds a quest and starts it immediately, bypassing start condition checks.
        /// Use this when you want to force-start a quest regardless of its conditions.
        /// </summary>
        /// <param name="questData">The quest data to add.</param>
        /// <returns>True if the quest was successfully added and started.</returns>
        public bool AddAndStartQuest(Quest_SO questData)
        {
            var quest = AddQuestCore(questData);
            if (quest == null) return false;

            quest.StartQuest();
            return true;
        }

        /// <summary>
        /// Adds a quest during save/load restore with fine-grained control over behavior.
        /// Internal method - use AddQuest or AddAndStartQuest for normal gameplay.
        /// </summary>
        /// <param name="questData">The quest data to add.</param>
        /// <param name="skipAutoStart">If true, don't auto-start even if conditions are met.</param>
        /// <param name="skipEventSubscription">If true, don't subscribe to start condition events.</param>
        /// <returns>True if the quest was successfully added.</returns>
        internal bool AddQuestForRestore(Quest_SO questData, bool skipAutoStart = true, bool skipEventSubscription = true)
        {
            var quest = AddQuestCore(questData);
            if (quest == null) return false;

            if (!skipAutoStart)
            {
                // Check conditions and start if met
                bool conditionsMet = quest.CheckStartConditions();
                if (conditionsMet)
                {
                    quest.StartQuest();
                    return true;
                }
            }

            if (!skipEventSubscription)
            {
                // Subscribe to events with auto-start blocked during subscription
                // to prevent events that fire immediately from bypassing skipAutoStart
                quest.SubscribeToStartQuestEvents(blockAutoStart: skipAutoStart);
                if (skipAutoStart)
                {
                    quest.UnblockAutoStart();
                }
            }

            return true;
        }

        /// <summary>
        /// Core logic for adding a quest: validation, creation, registration, and event subscription.
        /// Returns the created QuestRuntime, or null if validation failed.
        /// </summary>
        private QuestRuntime AddQuestCore(Quest_SO questData)
        {
            if (questData == null)
            {
                QuestLogger.LogError(LogSubsystem.Manager, "AddQuest: questData is null");
                return null;
            }

            Guid questId = questData.QuestId;

            // Validation checks
            if (requireQuestInDatabase && !_questRegistry.IsInDatabase(questId))
            {
                QuestLogger.LogWarning(LogSubsystem.Manager, $"Quest '{questData.DevName}' not in database");
                return null;
            }

            if (_questRegistry.IsActive(questId))
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{questData.DevName}' already active");
                return null;
            }

            if (!allowReplayingCompletedQuests && _questRegistry.IsCompleted(questId))
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{questData.DevName}' already completed, replay disabled");
                return null;
            }

            if (!allowMultipleActiveQuests && _questRegistry.ActiveCount > 0)
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"Cannot add '{questData.DevName}': multiple active quests disabled");
                return null;
            }

            // Create runtime quest - use database version if available
            Quest_SO sourceData = _questRegistry.GetFromDatabase(questId) ?? questData;
            QuestRuntime newQuest = sourceData.GetRuntimeQuest();

            if (!_questRegistry.AddActive(newQuest))
            {
                QuestLogger.LogError(LogSubsystem.Manager, $"Failed to add quest '{questData.DevName}'");
                return null;
            }

            // Subscribe manager to quest events
            SubscribeToQuestEvents(newQuest);

            // Fire events
            QuestAdded.SafeInvoke(newQuest);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestAdded);

            return newQuest;
        }

        #endregion

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
        /// Resets the quest to its initial state (NotStarted).
        /// </summary>
        /// <param name="questData">The quest data.</param>
        /// <returns>True if the quest was successfully restarted.</returns>
        public bool RestartQuest(Quest_SO questData)
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
            BeginEventProcessing();
            try
            {
                QuestStarted.SafeInvoke(quest);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestStarted);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestCompleted(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _questRegistry.MoveToCompleted(quest.QuestId);
            QuestLogger.LogComplete(LogSubsystem.Quest, "Quest", quest.QuestData.DevName);

            BeginEventProcessing();
            try
            {
                QuestCompleted.SafeInvoke(quest);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestCompleted);

                // Notify questlines that contain this quest
                NotifyQuestLinesOfQuestCompleted(quest);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestFailed(QuestRuntime quest)
        {
            UnsubscribeFromQuestEvents(quest);
            _questRegistry.MoveToFailed(quest.QuestId);
            QuestLogger.LogFail(LogSubsystem.Quest, "Quest", quest.QuestData.DevName);

            BeginEventProcessing();
            try
            {
                QuestFailed.SafeInvoke(quest);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestFailed);

                // Notify questlines that contain this quest
                NotifyQuestLinesOfQuestFailed(quest);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestUpdated(QuestRuntime quest)
        {
            BeginEventProcessing();
            try
            {
                QuestUpdated.SafeInvoke(quest);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestUpdated);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestRestarted(QuestRuntime quest)
        {
            BeginEventProcessing();
            try
            {
                QuestRestarted.SafeInvoke(quest);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestRestarted);
            }
            finally
            {
                EndEventProcessing();
            }
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

            QuestLineAdded.SafeInvoke(newLine);
            OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineAdded);

            // Check initial progress (some quests may already be complete)
            newLine.CheckProgress();

            return true;
        }

        /// <summary>
        /// Removes a questline from tracking.
        /// </summary>
        /// <param name="lineData">The questline data to remove.</param>
        /// <returns>True if the questline was successfully removed.</returns>
        public bool RemoveQuestLine(QuestLine_SO lineData)
        {
            if (lineData == null) return false;

            Guid lineId = lineData.QuestLineId;
            QuestLineRuntime line = _questLineRegistry.GetActive(lineId);

            if (line != null)
            {
                UnsubscribeFromQuestLineEvents(line);
                _questLineRegistry.RemoveActive(lineId);
                QuestLogger.LogVerbose(LogSubsystem.QuestLine, $"'{line.Data.DevName}' removed");

                BeginEventProcessing();
                try
                {
                    QuestLineRemoved.SafeInvoke(line);
                    OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineUpdated);
                }
                finally
                {
                    EndEventProcessing();
                }
                return true;
            }

            QuestLogger.LogWarning(LogSubsystem.QuestLine, $"Cannot remove '{lineData.DevName}': not active");
            return false;
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

        /// <summary>
        /// Gets all failed questlines as a read-only collection.
        /// </summary>
        public ReadOnlyCollection<QuestLineRuntime> GetFailedQuestLines()
        {
            return _questLineRegistry.GetAllFailed().ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if a questline has failed.
        /// </summary>
        public bool IsQuestLineFailed(QuestLine_SO lineData)
        {
            if (lineData == null) return false;
            return _questLineRegistry.IsFailed(lineData.QuestLineId);
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

            BeginEventProcessing();
            try
            {
                QuestLineStarted.SafeInvoke(line);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineStarted);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestLineCompleted(QuestLineRuntime line)
        {
            UnsubscribeFromQuestLineEvents(line);
            _questLineRegistry.MoveToCompleted(line.QuestLineId);
            line.DistributeCompletionRewards();
            QuestLogger.LogComplete(LogSubsystem.QuestLine, "QuestLine", line.Data.DevName);

            BeginEventProcessing();
            try
            {
                QuestLineCompleted.SafeInvoke(line);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineCompleted);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestLineUpdated(QuestLineRuntime line)
        {
            BeginEventProcessing();
            try
            {
                QuestLineUpdated.SafeInvoke(line);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineUpdated);
            }
            finally
            {
                EndEventProcessing();
            }
        }

        private void HandleQuestLineFailed(QuestLineRuntime line)
        {
            UnsubscribeFromQuestLineEvents(line);
            _questLineRegistry.MoveToFailed(line.QuestLineId);
            QuestLogger.LogFail(LogSubsystem.QuestLine, "QuestLine", line.Data.DevName);

            BeginEventProcessing();
            try
            {
                QuestLineFailed.SafeInvoke(line);
                OnQuestDataChanged.SafeInvoke(QuestDataChangeType.QuestLineFailed);
            }
            finally
            {
                EndEventProcessing();
            }
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
        /// Evaluates all quests in the database that are not in any registry (active, completed, or failed).
        /// If their start conditions are met, adds and starts them.
        /// Call this after loading to catch quests that should have started but weren't persisted.
        /// </summary>
        public void EvaluateUnstartedDatabaseQuests()
        {
            int evaluated = 0;
            int added = 0;

            foreach (var questData in questsDatabase)
            {
                if (questData == null) continue;

                Guid questId = questData.QuestId;

                // Skip quests already in any registry
                if (_questRegistry.IsActive(questId) ||
                    _questRegistry.IsCompleted(questId) ||
                    _questRegistry.IsFailed(questId))
                {
                    continue;
                }

                evaluated++;

                // AddQuest handles condition checking internally:
                // - If conditions met → quest starts automatically
                // - If conditions not met → quest subscribes to events for future activation
                // No need to create a temporary QuestRuntime just to check conditions.
                if (AddQuest(questData))
                {
                    added++;
                }
            }

            QuestLogger.Log(LogSubsystem.Quest, $"[PostLoad] Evaluated {evaluated} untracked quests, added {added}");
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

        #region GUID Validation

        /// <summary>
        /// Validates all quests and tasks in the database for duplicate GUIDs.
        /// Duplicate GUIDs cause incorrect save/load behavior.
        /// Call this during development to catch duplicated assets.
        /// </summary>
        /// <returns>True if no duplicates found, false if duplicates exist.</returns>
        #if ODIN_INSPECTOR
        [Button("Validate GUIDs")]
        #endif
        public bool ValidateDuplicateGUIDs()
        {
            bool isValid = true;

            // Check for duplicate Quest GUIDs
            var questGuidMap = new Dictionary<string, List<string>>();
            foreach (var quest in questsDatabase)
            {
                if (quest == null) continue;

                string guid = quest.QuestId.ToString();
                if (!questGuidMap.ContainsKey(guid))
                {
                    questGuidMap[guid] = new List<string>();
                }
                questGuidMap[guid].Add(quest.DevName);
            }

            foreach (var kvp in questGuidMap)
            {
                if (kvp.Value.Count > 1)
                {
                    QuestLogger.LogError(LogSubsystem.Manager,
                        $"DUPLICATE QUEST GUID: '{kvp.Key}' shared by: {string.Join(", ", kvp.Value)}. " +
                        "Use 'Generate New ID' button in the Inspector to fix.");
                    isValid = false;
                }
            }

            // Check for duplicate Task GUIDs across all quests
            var taskGuidMap = new Dictionary<string, List<string>>();
            foreach (var quest in questsDatabase)
            {
                if (quest == null) continue;

                foreach (var task in quest.AllTasks)
                {
                    if (task == null) continue;

                    string guid = task.TaskId.ToString();
                    string taskInfo = $"{quest.DevName}/{task.DevName}";

                    if (!taskGuidMap.ContainsKey(guid))
                    {
                        taskGuidMap[guid] = new List<string>();
                    }
                    taskGuidMap[guid].Add(taskInfo);
                }
            }

            foreach (var kvp in taskGuidMap)
            {
                if (kvp.Value.Count > 1)
                {
                    QuestLogger.LogError(LogSubsystem.Manager,
                        $"DUPLICATE TASK GUID: '{kvp.Key}' shared by: {string.Join(", ", kvp.Value)}. " +
                        "Use 'Generate New ID' button in the Inspector to fix.");
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validates GUIDs for a specific quest and its tasks.
        /// Use this after adding a quest to check for conflicts with existing quests.
        /// </summary>
        /// <param name="questData">The quest to validate.</param>
        /// <returns>True if no duplicates found.</returns>
        public bool ValidateQuestGUIDs(Quest_SO questData)
        {
            if (questData == null) return true;

            bool isValid = true;
            string questGuid = questData.QuestId.ToString();

            // Check quest GUID against database
            foreach (var other in questsDatabase)
            {
                if (other == null || other == questData) continue;

                if (other.QuestId.ToString() == questGuid)
                {
                    QuestLogger.LogError(LogSubsystem.Manager,
                        $"DUPLICATE QUEST GUID: '{questData.DevName}' has same GUID as '{other.DevName}'");
                    isValid = false;
                }
            }

            // Check task GUIDs against all tasks in database
            var allTaskGuids = new Dictionary<string, string>(); // guid -> questName/taskName
            foreach (var quest in questsDatabase)
            {
                if (quest == null) continue;

                foreach (var task in quest.AllTasks)
                {
                    if (task == null) continue;
                    allTaskGuids[task.TaskId.ToString()] = $"{quest.DevName}/{task.DevName}";
                }
            }

            foreach (var task in questData.AllTasks)
            {
                if (task == null) continue;

                string taskGuid = task.TaskId.ToString();
                if (allTaskGuids.TryGetValue(taskGuid, out string existingTask))
                {
                    string currentTask = $"{questData.DevName}/{task.DevName}";
                    if (existingTask != currentTask)
                    {
                        QuestLogger.LogError(LogSubsystem.Manager,
                            $"DUPLICATE TASK GUID: '{currentTask}' has same GUID as '{existingTask}'");
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        #endregion
    }
}
