using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelloDev.Conditions;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Manages saving and loading of quest system state.
    /// Uses the ISaveDataProvider interface for storage, allowing integration
    /// with any save system (JSON files, cloud saves, etc.).
    /// Registers itself with a QuestSaveService_SO for decoupled access.
    /// </summary>
    public class QuestSaveManager : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [Title("Service")]
        [Required]
        [InfoBox("Reference the QuestSaveService_SO asset. This manager will register itself with the service on enable.")]
#endif
        [SerializeField]
        [Tooltip("The service SO that provides decoupled access to this manager.")]
        private QuestSaveService_SO service;

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
        [Tooltip("The WorldFlagService_SO for accessing flag runtime values during save/load.")]
        private WorldFlagService_SO worldFlagService;

#if ODIN_INSPECTOR
        [Title("Options")]
#endif
        [SerializeField]
        [Tooltip("If true, this manager persists across scene loads.")]
        private bool persistent = true;

        #endregion

        #region Private Fields

        private ISaveDataProvider _provider;
        private WorldFlagRegistry_SO _worldFlagRegistrySO;

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
        /// Gets the service this manager is registered with.
        /// </summary>
        public QuestSaveService_SO Service => service;

        /// <summary>
        /// Gets whether a save provider has been set.
        /// </summary>
        public bool HasProvider => _provider != null;

        /// <summary>
        /// Gets the current save provider.
        /// </summary>
        public ISaveDataProvider Provider => _provider;

        /// <summary>
        /// Gets the number of registered world flags.
        /// </summary>
        public int WorldFlagCount => GetAllWorldFlags().Count;

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
            // Register with service
            if (service != null)
            {
                service.Register(this);

                // Forward events to service
                OnBeforeSave.AddListener(slot => service.OnBeforeSave?.Invoke(slot));
                OnAfterSave.AddListener((slot, success) => service.OnAfterSave?.Invoke(slot, success));
                OnBeforeLoad.AddListener(slot => service.OnBeforeLoad?.Invoke(slot));
                OnAfterLoad.AddListener((slot, success) => service.OnAfterLoad?.Invoke(slot, success));

                QuestLogger.Log($"[QuestSaveManager] Registered with service.");
            }
            else
            {
                Debug.LogWarning($"[QuestSaveManager] No service assigned on {name}. Save/load will not be accessible via service.");
            }
        }

        private void OnDisable()
        {
            // Unregister from service
            if (service != null)
            {
                OnBeforeSave.RemoveAllListeners();
                OnAfterSave.RemoveAllListeners();
                OnBeforeLoad.RemoveAllListeners();
                OnAfterLoad.RemoveAllListeners();

                service.Unregister(this);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the save data provider. Must be called before save/load operations.
        /// </summary>
        /// <param name="provider">The save data provider to use.</param>
        public void SetProvider(ISaveDataProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            QuestLogger.Log($"[QuestSaveManager] Provider set: {provider.GetType().Name}");
        }

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
                QuestLogger.Log($"[QuestSaveManager] WorldFlagRegistry set with {registry.Count} flags.");
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
                QuestLogger.LogWarning("[QuestSaveManager] QuestManager not found. Snapshot will be empty.");
                return new QuestSystemSnapshot { Version = 1, Timestamp = DateTime.UtcNow.ToString("O") };
            }

            var snapshot = SnapshotCapturer.CaptureFullSnapshot(
                questManager.GetActiveQuests(),
                questManager.GetCompletedQuests(),
                questManager.GetFailedQuests(),
                questManager.GetActiveQuestLines(),
                questManager.GetCompletedQuestLines(),
                GetAllWorldFlags(),
                worldFlagService
            );

            snapshot.Version = 1;

            QuestLogger.Log($"[QuestSaveManager] Snapshot captured: {snapshot.ActiveQuests.Count} active, {snapshot.CompletedQuests.Count} completed, {snapshot.FailedQuests.Count} failed quests.");

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
                QuestLogger.LogError("[QuestSaveManager] Cannot restore null snapshot.");
                return false;
            }

            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                QuestLogger.LogError("[QuestSaveManager] QuestManager not found. Cannot restore snapshot.");
                return false;
            }

            try
            {
                // Clear current state
                questManager.ShutdownManager();
                questManager.InitializeManager(questManager.QuestsDatabase.ToList());

                // Restore world flags first (quests may depend on them)
                SnapshotRestorer.RestoreWorldFlags(snapshot.WorldFlags, GetAllWorldFlags(), worldFlagService);

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

                QuestLogger.Log($"[QuestSaveManager] Snapshot restored from {snapshot.Timestamp}");
                return true;
            }
            catch (Exception ex)
            {
                QuestLogger.LogError($"[QuestSaveManager] Restore failed: {ex.Message}");
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
            if (_provider == null)
            {
                QuestLogger.LogError("[QuestSaveManager] No save provider set. Call SetProvider first.");
                return false;
            }

            OnBeforeSave?.Invoke(slotKey);

            var snapshot = CaptureSnapshot();
            bool success = await _provider.SaveAsync(slotKey, snapshot);

            OnAfterSave?.Invoke(slotKey, success);

            return success;
        }

        /// <summary>
        /// Loads quest system state from the specified slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if load was successful.</returns>
        public async Task<bool> LoadAsync(string slotKey)
        {
            if (_provider == null)
            {
                QuestLogger.LogError("[QuestSaveManager] No save provider set. Call SetProvider first.");
                return false;
            }

            OnBeforeLoad?.Invoke(slotKey);

            var snapshot = await _provider.LoadAsync(slotKey);
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
            if (_provider == null) return false;
            return await _provider.ExistsAsync(slotKey);
        }

        /// <summary>
        /// Deletes a save slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if deletion was successful.</returns>
        public async Task<bool> DeleteSaveAsync(string slotKey)
        {
            if (_provider == null) return false;
            return await _provider.DeleteAsync(slotKey);
        }

        /// <summary>
        /// Gets metadata for a save slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>Metadata for the save slot, or null if not found.</returns>
        public async Task<SaveSlotMetadata> GetSaveMetadataAsync(string slotKey)
        {
            if (_provider == null) return null;
            return await _provider.GetMetadataAsync(slotKey);
        }

        /// <summary>
        /// Lists all available save slots.
        /// </summary>
        /// <returns>Array of save slot identifiers.</returns>
        public async Task<string[]> GetAllSaveSlotsAsync()
        {
            if (_provider == null) return Array.Empty<string>();
            return await _provider.GetAllSlotsAsync();
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
        [PropertyOrder(200)]
        private bool ServiceRegistered => service != null && service.IsAvailable;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(201)]
        private bool ProviderSet => _provider != null;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(202)]
        private string ProviderType => _provider?.GetType().Name ?? "(none)";

        [ShowInInspector, ReadOnly]
        [PropertyOrder(203)]
        private int RegisteredWorldFlags => WorldFlagCount;

        [Button("Quick Save (debug_save)", ButtonSizes.Medium)]
        [PropertyOrder(210)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private async void DebugQuickSave()
        {
            if (_provider == null)
            {
                Debug.LogError("[QuestSaveManager] No provider set. Call SetProvider() first.");
                return;
            }

            QuestLogger.Log("[DEBUG] Quick Save started...");
            var success = await SaveAsync(DEBUG_SLOT);
            QuestLogger.Log($"[DEBUG] Quick Save {(success ? "succeeded" : "failed")}");
        }

        [Button("Quick Load (debug_save)", ButtonSizes.Medium)]
        [PropertyOrder(211)]
        [GUIColor(0.4f, 0.6f, 0.9f)]
        private async void DebugQuickLoad()
        {
            if (_provider == null)
            {
                Debug.LogError("[QuestSaveManager] No provider set. Call SetProvider() first.");
                return;
            }

            QuestLogger.Log("[DEBUG] Quick Load started...");
            var success = await LoadAsync(DEBUG_SLOT);
            QuestLogger.Log($"[DEBUG] Quick Load {(success ? "succeeded" : "failed")}");
        }

        [Button("Log Current Snapshot", ButtonSizes.Medium)]
        [PropertyOrder(212)]
        private void DebugLogSnapshot()
        {
            var snapshot = CaptureSnapshot();

            QuestLogger.Log("=== SNAPSHOT DETAILS ===");
            QuestLogger.Log($"Timestamp: {snapshot.Timestamp}");
            QuestLogger.Log($"Active Quests: {snapshot.ActiveQuests.Count}");
            QuestLogger.Log($"Completed Quests: {snapshot.CompletedQuests.Count}");
            QuestLogger.Log($"Failed Quests: {snapshot.FailedQuests.Count}");
            QuestLogger.Log($"Active QuestLines: {snapshot.ActiveQuestLines.Count}");
            QuestLogger.Log($"Completed QuestLines: {snapshot.CompletedQuestLines.Count}");
            QuestLogger.Log($"World Flags: {snapshot.WorldFlags.Count}");

            if (snapshot.WorldFlags.Count > 0)
            {
                QuestLogger.Log("--- World Flag Values ---");
                foreach (var flag in snapshot.WorldFlags)
                {
                    string value = flag.IsBoolFlag ? flag.BoolValue.ToString() : flag.IntValue.ToString();
                    string type = flag.IsBoolFlag ? "Bool" : "Int";
                    QuestLogger.Log($"  [{type}] {flag.FlagGuid}: {value}");
                }
            }
        }

        [Button("Reset All World Flags", ButtonSizes.Medium)]
        [PropertyOrder(220)]
        [GUIColor(0.9f, 0.6f, 0.4f)]
        private void DebugResetWorldFlags()
        {
            if (worldFlagService != null && worldFlagService.IsAvailable)
            {
                worldFlagService.ResetAllFlags();
                QuestLogger.Log("[DEBUG] All world flags reset to defaults.");
            }
            else
            {
                Debug.LogWarning("[QuestSaveManager] WorldFlagService not available.");
            }
        }

        [Button("Delete Debug Save", ButtonSizes.Medium)]
        [PropertyOrder(221)]
        [GUIColor(0.9f, 0.4f, 0.4f)]
        private async void DebugDeleteSave()
        {
            if (_provider == null)
            {
                Debug.LogError("[QuestSaveManager] No provider set.");
                return;
            }

            var success = await DeleteSaveAsync(DEBUG_SLOT);
            QuestLogger.Log($"[DEBUG] Delete save {(success ? "succeeded" : "failed")}");
        }

#endif

        #endregion
    }
}
