using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Saving;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Manages saving and loading of quest system state.
    /// Uses SaveService.Provider for storage, allowing integration with any save system.
    /// Configure the provider at startup via SaveService.SetProvider().
    /// Registers itself with a QuestSaveLocator_SO for decoupled access.
    /// Implements IBootstrapInitializable for proper initialization ordering (priority 200 - Persistence phase).
    /// </summary>
    public class QuestSaveManager : MonoBehaviour, IBootstrapInitializable
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [Title("Locator")]
        [Required]
        [InfoBox("Reference the QuestSaveLocator_SO asset. This manager will register itself with the locator on enable.")]
#endif
        [SerializeField]
        [Tooltip("The locator SO that provides decoupled access to this manager.")]
        private QuestSaveLocator_SO locator;

#if ODIN_INSPECTOR
        [Title("World Flags")]
#endif
        [SerializeField]
        [Tooltip("All WorldFlag assets in the game. Required for save/load of world state.")]
        private List<WorldFlagBase_SO> worldFlagRegistry = new();

        [SerializeField]
        [Tooltip("Optional: Use a WorldFlagRegistry_SO for easier management.")]
        private WorldFlagRegistry_SO worldFlagRegistryAsset;

        [SerializeField]
        [Tooltip("The WorldFlagLocator_SO for accessing flag runtime values during save/load.")]
        private WorldFlagLocator_SO worldFlagLocator;

#if ODIN_INSPECTOR
        [Title("Slot Management")]
#endif
        [SerializeField]
        [Tooltip("Optional slot config for slot-based saving. Provides slot naming conventions and current slot tracking.")]
        private SaveSlotConfig_SO slotConfig;

#if ODIN_INSPECTOR
        [Title("Options")]
#endif
        [SerializeField]
        [Tooltip("If true, this manager persists across scene loads.")]
        private bool persistent = true;

#if ODIN_INSPECTOR
        [Title("Initialization Mode")]
        [ToggleLeft]
        [InfoBox("Disable when using GameBootstrap for coordinated initialization.")]
#else
        [Header("Initialization Mode")]
#endif
        [SerializeField]
        [Tooltip("If true, self-initializes in OnEnable. Disable when using GameBootstrap.")]
        private bool selfInitialize = true;

        #endregion

        #region Private Fields

        private WorldFlagRegistry_SO _worldFlagRegistrySO;
        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Fired before a save operation starts.
        /// </summary>
        public UnityEvent<string> OnBeforeSave = new();

        /// <summary>
        /// Fired after a save operation completes.
        /// </summary>
        public UnityEvent<string, bool> OnAfterSave = new();

        /// <summary>
        /// Fired before a load operation starts.
        /// </summary>
        public UnityEvent<string> OnBeforeLoad = new();

        /// <summary>
        /// Fired after a load operation completes.
        /// </summary>
        public UnityEvent<string, bool> OnAfterLoad = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the locator this manager is registered with.
        /// </summary>
        public QuestSaveLocator_SO Locator => locator;

        /// <summary>
        /// Gets whether a save provider has been configured via SaveService.SetProvider().
        /// </summary>
        public bool HasProvider => SaveService.IsConfigured;

        /// <summary>
        /// Gets the number of registered world flags.
        /// </summary>
        public int WorldFlagCount => GetAllWorldFlags().Count;

        /// <summary>
        /// Gets the slot config, if assigned.
        /// </summary>
        public SaveSlotConfig_SO SlotConfig => slotConfig;

        /// <summary>
        /// Gets whether slot management is available (slot config assigned).
        /// </summary>
        public bool HasSlotConfig => slotConfig != null;

        /// <summary>
        /// Gets whether a slot is currently active.
        /// </summary>
        public bool HasActiveSlot => slotConfig != null && slotConfig.HasActiveSlot;

        /// <summary>
        /// Gets the current slot index, or -1 if no slot config or no active slot.
        /// </summary>
        public int CurrentSlotIndex => slotConfig?.CurrentSlotIndex ?? -1;

        /// <summary>
        /// Gets the current manual save slot key (e.g., "save-1"), or null if no active slot.
        /// </summary>
        public string CurrentManualSlotKey => slotConfig?.CurrentManualSlotKey;

        /// <summary>
        /// Gets the current autosave slot key (e.g., "autosave-1"), or null if no active slot.
        /// </summary>
        public string CurrentAutosaveSlotKey => slotConfig?.CurrentAutosaveSlotKey;

        #endregion

        #region IBootstrapInitializable

        /// <summary>
        /// Whether this manager should self-initialize.
        /// </summary>
        public bool SelfInitialize => selfInitialize;

        /// <summary>
        /// Priority 200 - Persistence phase. Runs after QuestManager (150) and before SaveSystemSetup (250).
        /// </summary>
        public int InitializationPriority => 200;

        /// <summary>
        /// Whether this manager has completed initialization.
        /// </summary>
        bool IBootstrapInitializable.IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the save manager and registers with the locator.
        /// </summary>
        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            RegisterWithLocator();
            _isInitialized = true;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleans up the save manager.
        /// </summary>
        public void Shutdown()
        {
            UnregisterFromLocator();
            _isInitialized = false;
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (persistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            // Self-initialize if not using bootstrap
            if (selfInitialize && !_isInitialized)
            {
                _ = InitializeAsync();
            }
        }

        private void OnDisable()
        {
            UnregisterFromLocator();
        }

        private void RegisterWithLocator()
        {
            if (locator != null)
            {
                locator.Register(this);

                // Forward events to locator
                OnBeforeSave.AddListener(slot => locator.OnBeforeSave?.Invoke(slot));
                OnAfterSave.AddListener((slot, success) => locator.OnAfterSave?.Invoke(slot, success));
                OnBeforeLoad.AddListener(slot => locator.OnBeforeLoad?.Invoke(slot));
                OnAfterLoad.AddListener((slot, success) => locator.OnAfterLoad?.Invoke(slot, success));

                QuestLogger.LogVerbose(LogSubsystem.Save, "SaveManager registered with locator");
            }
            else
            {
                QuestLogger.LogWarning(LogSubsystem.Save, $"No locator assigned on {name}");
            }
        }

        private void UnregisterFromLocator()
        {
            if (locator != null)
            {
                OnBeforeSave.RemoveAllListeners();
                OnAfterSave.RemoveAllListeners();
                OnBeforeLoad.RemoveAllListeners();
                OnAfterLoad.RemoveAllListeners();

                locator.Unregister(this);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a world flag for save/load. Call this for dynamically created flags.
        /// </summary>
        /// <param name="flag">The world flag to register.</param>
        public void RegisterWorldFlag(WorldFlagBase_SO flag)
        {
            if (flag != null && !worldFlagRegistry.Contains(flag))
            {
                worldFlagRegistry.Add(flag);
            }
        }

        /// <summary>
        /// Sets the world flag registry for auto-discovery. This replaces manual registration.
        /// </summary>
        /// <param name="registry">The registry containing all world flags.</param>
        public void SetWorldFlagRegistry(WorldFlagRegistry_SO registry)
        {
            _worldFlagRegistrySO = registry;
            if (registry != null)
            {
                QuestLogger.LogVerbose(LogSubsystem.Save, $"WorldFlagRegistry: {registry.Count} flags");
            }
        }

        /// <summary>
        /// Validates a snapshot before restoration. Use this to check if a save file
        /// is compatible with the current game version.
        /// </summary>
        /// <param name="snapshot">The snapshot to validate.</param>
        /// <returns>Validation result with any issues found.</returns>
        public SnapshotValidationResult ValidateSnapshot(QuestSystemSnapshot snapshot)
        {
            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                var result = new SnapshotValidationResult();
                result.AddCritical("QuestManager", "QuestManager not found.");
                return result;
            }

            return SnapshotValidator.Validate(
                snapshot,
                FindQuestByGuid,
                FindQuestLineByGuid,
                GetAllWorldFlags()
            );
        }

        /// <summary>
        /// Captures the current state of the quest system without saving to storage.
        /// Useful for custom save implementations or debugging.
        /// </summary>
        /// <returns>A snapshot of the current quest system state.</returns>
        public QuestSystemSnapshot CaptureSnapshot()
        {
            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "QuestManager not found, snapshot empty");
                return new QuestSystemSnapshot { Version = 1, Timestamp = DateTime.UtcNow.ToString("O") };
            }

            var snapshot = SnapshotCapturer.CaptureFullSnapshot(
                questManager.GetActiveQuests(),
                questManager.GetCompletedQuests(),
                questManager.GetFailedQuests(),
                questManager.GetActiveQuestLines(),
                questManager.GetCompletedQuestLines(),
                GetAllWorldFlags(),
                worldFlagLocator
            );

            snapshot.Version = 1;

            QuestLogger.Log(LogSubsystem.Save, $"Captured: <b>{snapshot.ActiveQuests.Count}</b> active, <b>{snapshot.CompletedQuests.Count}</b> completed, <b>{snapshot.FailedQuests.Count}</b> failed");

            return snapshot;
        }

        /// <summary>
        /// Restores the quest system state from a snapshot without loading from storage.
        /// Useful for custom save implementations or debugging.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore.</param>
        /// <returns>True if restoration was successful.</returns>
        public bool RestoreSnapshot(QuestSystemSnapshot snapshot)
        {
            if (snapshot == null)
            {
                QuestLogger.LogError(LogSubsystem.Save, "Cannot restore null snapshot");
                return false;
            }

            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                QuestLogger.LogError(LogSubsystem.Save, "QuestManager not found");
                return false;
            }

            try
            {
                // Clear current state
                questManager.ShutdownManager();
                questManager.InitializeManager(questManager.QuestsDatabase.ToList());

                // Restore world flags first (quests may depend on them)
                SnapshotRestorer.RestoreWorldFlags(snapshot.WorldFlags, GetAllWorldFlags(), worldFlagLocator);

                // Restore quests
                SnapshotRestorer.RestoreQuests(snapshot.ActiveQuests, QuestState.InProgress, questManager, FindQuestByGuid);
                SnapshotRestorer.RestoreQuests(snapshot.CompletedQuests, QuestState.Completed, questManager, FindQuestByGuid);
                SnapshotRestorer.RestoreQuests(snapshot.FailedQuests, QuestState.Failed, questManager, FindQuestByGuid);

                // Restore questlines
                SnapshotRestorer.RestoreQuestLines(snapshot.ActiveQuestLines, questManager, FindQuestLineByGuid);
                SnapshotRestorer.RestoreQuestLines(snapshot.CompletedQuestLines, questManager, FindQuestLineByGuid);

                // Re-subscribe NotStarted quests to their start condition events
                // This must happen AFTER all quests are restored to prevent events from triggering auto-start during restore
                questManager.ResubscribeNotStartedQuestsToEvents();

                QuestLogger.LogLoad(snapshot.Timestamp, true);
                return true;
            }
            catch (Exception ex)
            {
                QuestLogger.LogError(LogSubsystem.Save, $"Restore failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves the current quest system state to the specified slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if save was successful.</returns>
        public async Task<bool> SaveAsync(string slotKey)
        {
            if (!SaveService.IsConfigured)
            {
                QuestLogger.LogError(LogSubsystem.Save, "No provider configured. Call SaveService.SetProvider() at startup.");
                return false;
            }

            OnBeforeSave?.Invoke(slotKey);

            var snapshot = CaptureSnapshot();

            // Save snapshot
            bool success = await SaveService.Provider.SaveAsync(slotKey, snapshot);

            // Save metadata separately for quick access
            if (success)
            {
                var metadata = CreateMetadata(slotKey, snapshot);
                await SaveService.Provider.SaveAsync($"{slotKey}.meta", metadata);
            }

            OnAfterSave?.Invoke(slotKey, success);

            return success;
        }

        private SaveSlotMetadata CreateMetadata(string slotKey, QuestSystemSnapshot snapshot)
        {
            return new SaveSlotMetadata
            {
                SlotKey = slotKey,
                Timestamp = snapshot.Timestamp,
                ActiveQuestCount = snapshot.ActiveQuests?.Count ?? 0,
                CompletedQuestCount = snapshot.CompletedQuests?.Count ?? 0
            };
        }

        /// <summary>
        /// Loads quest system state from the specified slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if load was successful.</returns>
        public async Task<bool> LoadAsync(string slotKey)
        {
            if (!SaveService.IsConfigured)
            {
                QuestLogger.LogError(LogSubsystem.Save, "No provider configured. Call SaveService.SetProvider() at startup.");
                return false;
            }

            OnBeforeLoad?.Invoke(slotKey);

            var snapshot = await SaveService.Provider.LoadAsync<QuestSystemSnapshot>(slotKey);
            if (snapshot == null)
            {
                OnAfterLoad?.Invoke(slotKey, false);
                return false;
            }

            bool success = RestoreSnapshot(snapshot);

            OnAfterLoad?.Invoke(slotKey, success);

            return success;
        }

        /// <summary>
        /// Checks if a save slot exists.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if the slot exists.</returns>
        public async Task<bool> SaveExistsAsync(string slotKey)
        {
            if (!SaveService.IsConfigured) return false;
            return await SaveService.Provider.ExistsAsync(slotKey);
        }

        /// <summary>
        /// Deletes a save slot and its metadata.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if deletion was successful.</returns>
        public async Task<bool> DeleteSaveAsync(string slotKey)
        {
            if (!SaveService.IsConfigured) return false;

            // Delete both the snapshot and metadata
            bool snapshotDeleted = await SaveService.Provider.DeleteAsync(slotKey);
            await SaveService.Provider.DeleteAsync($"{slotKey}.meta");

            return snapshotDeleted;
        }

        /// <summary>
        /// Gets metadata for a save slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>Metadata for the save slot, or null if not found.</returns>
        public async Task<SaveSlotMetadata> GetSaveMetadataAsync(string slotKey)
        {
            if (!SaveService.IsConfigured) return null;
            return await SaveService.Provider.LoadAsync<SaveSlotMetadata>($"{slotKey}.meta");
        }

        /// <summary>
        /// Lists all available save slots (excludes .meta files).
        /// </summary>
        /// <returns>Array of save slot identifiers.</returns>
        public async Task<string[]> GetAllSaveSlotsAsync()
        {
            if (!SaveService.IsConfigured) return Array.Empty<string>();

            var allKeys = await SaveService.Provider.GetKeysAsync();

            // Filter out .meta files
            return allKeys
                .Where(key => !key.EndsWith(".meta"))
                .ToArray();
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Sets the active slot index. Requires SlotConfig to be assigned.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-based). Use -1 to clear active slot.</param>
        /// <returns>True if the slot was set successfully.</returns>
        public bool SetActiveSlot(int slotIndex)
        {
            if (slotConfig == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "Cannot set active slot: no SlotConfig assigned");
                return false;
            }

            slotConfig.SetActiveSlot(slotIndex);
            return true;
        }

        /// <summary>
        /// Clears the active slot (sets to -1). Requires SlotConfig to be assigned.
        /// </summary>
        public void ClearActiveSlot()
        {
            slotConfig?.ClearActiveSlot();
        }

        /// <summary>
        /// Saves to the current manual save slot (e.g., "save-1").
        /// Requires SlotConfig to be assigned and an active slot.
        /// </summary>
        /// <returns>True if save was successful, false if no active slot or save failed.</returns>
        public async Task<bool> SaveToCurrentSlotAsync()
        {
            if (!HasActiveSlot)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "Cannot save to current slot: no active slot");
                return false;
            }

            return await SaveAsync(CurrentManualSlotKey);
        }

        /// <summary>
        /// Loads from the current manual save slot (e.g., "save-1").
        /// Requires SlotConfig to be assigned and an active slot.
        /// </summary>
        /// <returns>True if load was successful, false if no active slot or load failed.</returns>
        public async Task<bool> LoadFromCurrentSlotAsync()
        {
            if (!HasActiveSlot)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "Cannot load from current slot: no active slot");
                return false;
            }

            return await LoadAsync(CurrentManualSlotKey);
        }

        /// <summary>
        /// Saves to the current autosave slot (e.g., "autosave-1").
        /// Requires SlotConfig to be assigned and an active slot.
        /// </summary>
        /// <returns>True if save was successful, false if no active slot or save failed.</returns>
        public async Task<bool> AutoSaveAsync()
        {
            if (!HasActiveSlot)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "Cannot autosave: no active slot");
                return false;
            }

            return await SaveAsync(CurrentAutosaveSlotKey);
        }

        /// <summary>
        /// Loads from the current autosave slot (e.g., "autosave-1").
        /// Requires SlotConfig to be assigned and an active slot.
        /// </summary>
        /// <returns>True if load was successful, false if no active slot or load failed.</returns>
        public async Task<bool> LoadFromAutosaveAsync()
        {
            if (!HasActiveSlot)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "Cannot load autosave: no active slot");
                return false;
            }

            return await LoadAsync(CurrentAutosaveSlotKey);
        }

        #endregion

        #region Private Methods

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

            // Add flags from runtime-set registry
            if (_worldFlagRegistrySO != null && _worldFlagRegistrySO != worldFlagRegistryAsset)
            {
                foreach (var flag in _worldFlagRegistrySO.AllFlags)
                {
                    if (flag != null && !allFlags.Contains(flag))
                        allFlags.Add(flag);
                }
            }

            return allFlags;
        }

        private Quest_SO FindQuestByGuid(string guidString)
        {
            if (!Guid.TryParse(guidString, out var guid)) return null;

            var questManager = QuestManager.Instance;
            return questManager?.QuestsDatabase.FirstOrDefault(q => q.QuestId == guid);
        }

        private QuestLine_SO FindQuestLineByGuid(string guidString)
        {
            if (!Guid.TryParse(guidString, out var guid)) return null;

            var questManager = QuestManager.Instance;
            return questManager?.QuestLinesDatabase.FirstOrDefault(l => l.QuestLineId == guid);
        }

        #endregion

        #region Debug

#if ODIN_INSPECTOR && UNITY_EDITOR
        private const string DEBUG_SLOT = "debug_save";

        [Title("Debug - Save/Load")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(199)]
        private bool IsInitialized => _isInitialized;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(200)]
        private bool LocatorRegistered => locator != null && locator.IsAvailable;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(201)]
        private bool ProviderConfigured => SaveService.IsConfigured;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(202)]
        private string ProviderType => SaveService.IsConfigured ? SaveService.Provider.GetType().Name : "(none)";

        [ShowInInspector, ReadOnly]
        [PropertyOrder(203)]
        private int RegisteredWorldFlags => WorldFlagCount;

        [Button("Quick Save (debug_save)", ButtonSizes.Medium)]
        [PropertyOrder(210)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private async void DebugQuickSave()
        {
            if (!SaveService.IsConfigured)
            {
                QuestLogger.LogError(LogSubsystem.Save, "No provider configured");
                return;
            }

            var success = await SaveAsync(DEBUG_SLOT);
            QuestLogger.LogSave(DEBUG_SLOT, success);
        }

        [Button("Quick Load (debug_save)", ButtonSizes.Medium)]
        [PropertyOrder(211)]
        [GUIColor(0.4f, 0.6f, 0.9f)]
        private async void DebugQuickLoad()
        {
            if (!SaveService.IsConfigured)
            {
                QuestLogger.LogError(LogSubsystem.Save, "No provider configured");
                return;
            }

            var success = await LoadAsync(DEBUG_SLOT);
            QuestLogger.LogLoad(DEBUG_SLOT, success);
        }

        [Button("Log Current Snapshot", ButtonSizes.Medium)]
        [PropertyOrder(212)]
        private void DebugLogSnapshot()
        {
            var snapshot = CaptureSnapshot();

            Debug.Log($"<color=#A8D8EA><b>=== SNAPSHOT ({snapshot.Timestamp}) ===</b></color>");
            Debug.Log($"  Active: {snapshot.ActiveQuests.Count} | Completed: {snapshot.CompletedQuests.Count} | Failed: {snapshot.FailedQuests.Count}");
            Debug.Log($"  QuestLines - Active: {snapshot.ActiveQuestLines.Count} | Completed: {snapshot.CompletedQuestLines.Count}");
            Debug.Log($"  World Flags: {snapshot.WorldFlags.Count}");
        }

        [Button("Reset All World Flags", ButtonSizes.Medium)]
        [PropertyOrder(220)]
        [GUIColor(0.9f, 0.6f, 0.4f)]
        private void DebugResetWorldFlags()
        {
            if (worldFlagLocator != null && worldFlagLocator.IsAvailable)
            {
                worldFlagLocator.ResetAllFlags();
                QuestLogger.Log(LogSubsystem.Save, "All world flags reset");
            }
            else
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "WorldFlagLocator not available");
            }
        }

        [Button("Delete Debug Save", ButtonSizes.Medium)]
        [PropertyOrder(221)]
        [GUIColor(0.9f, 0.4f, 0.4f)]
        private async void DebugDeleteSave()
        {
            if (!SaveService.IsConfigured)
            {
                QuestLogger.LogError(LogSubsystem.Save, "No provider configured");
                return;
            }

            var success = await DeleteSaveAsync(DEBUG_SLOT);
            QuestLogger.Log(LogSubsystem.Save, $"Delete <b>'{DEBUG_SLOT}'</b> {(success ? "succeeded" : "failed")}");
        }

#endif

        #endregion
    }
}
