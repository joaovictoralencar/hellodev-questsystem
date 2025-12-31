using System.Threading.Tasks;
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
    /// ScriptableObject locator for QuestSaveManager.
    /// Acts as a "channel" that any asset can reference to access save/load functionality.
    /// The QuestSaveManager registers itself with this locator on enable.
    ///
    /// Usage:
    /// 1. Create a single QuestSaveLocator_SO asset in your project
    /// 2. Assign it to QuestSaveManager's "Locator" field
    /// 3. Reference the same asset anywhere you need save/load access
    /// </summary>
    [CreateAssetMenu(fileName = "QuestSaveLocator", menuName = "HelloDev/Locators/Quest Save Locator")]
    public class QuestSaveLocator_SO : LocatorBase_SO
    {
        #region LocatorBase_SO Implementation

        /// <inheritdoc/>
        public override string LocatorId => "HelloDev.QuestSystem.Save";

        /// <inheritdoc/>
        public override bool IsAvailable => _manager != null;

        /// <inheritdoc/>
        public override void PrepareForBootstrap()
        {
            // Manager will re-register during bootstrap
            _manager = null;
        }

        #endregion

        #region Private Fields

        private QuestSaveManager _manager;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the registered manager instance.
        /// </summary>
        public QuestSaveManager Manager => _manager;

        /// <summary>
        /// Gets whether a save provider has been configured via SaveService.SetProvider().
        /// </summary>
        public bool HasProvider => SaveService.IsConfigured;

        /// <summary>
        /// Gets the number of registered world flags.
        /// </summary>
        public int WorldFlagCount => _manager?.WorldFlagCount ?? 0;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a manager registers with this locator.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent OnManagerRegistered = new();

        /// <summary>
        /// Fired when a manager unregisters from this locator.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent OnManagerUnregistered = new();

        /// <summary>
        /// Fired before a save operation starts.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent<string> OnBeforeSave = new();

        /// <summary>
        /// Fired after a save operation completes.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent<string, bool> OnAfterSave = new();

        /// <summary>
        /// Fired before a load operation starts.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent<string> OnBeforeLoad = new();

        /// <summary>
        /// Fired after a load operation completes.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent<string, bool> OnAfterLoad = new();

        #endregion

        #region Registration

        /// <summary>
        /// Registers a QuestSaveManager with this locator.
        /// Called by QuestSaveManager.OnEnable().
        /// </summary>
        public void Register(QuestSaveManager manager)
        {
            if (manager == null) return;

            if (_manager != null && _manager != manager)
            {
                Debug.LogWarning($"[QuestSaveLocator] Replacing existing manager. Old: {_manager.name}, New: {manager.name}");
            }

            _manager = manager;
            OnManagerRegistered?.Invoke();
        }

        /// <summary>
        /// Unregisters a QuestSaveManager from this locator.
        /// Called by QuestSaveManager.OnDisable().
        /// </summary>
        public void Unregister(QuestSaveManager manager)
        {
            if (_manager == manager)
            {
                _manager = null;
                OnManagerUnregistered?.Invoke();
            }
        }

        #endregion

        #region Save/Load Operations

        /// <summary>
        /// Saves the current quest system state to the specified slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if save was successful.</returns>
        public async Task<bool> SaveAsync(string slotKey)
        {
            if (_manager == null)
            {
                Debug.LogWarning("[QuestSaveLocator] No manager registered. Cannot save.");
                return false;
            }
            return await _manager.SaveAsync(slotKey);
        }

        /// <summary>
        /// Loads quest system state from the specified slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if load was successful.</returns>
        public async Task<bool> LoadAsync(string slotKey)
        {
            if (_manager == null)
            {
                Debug.LogWarning("[QuestSaveLocator] No manager registered. Cannot load.");
                return false;
            }
            return await _manager.LoadAsync(slotKey);
        }

        /// <summary>
        /// Checks if a save slot exists.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if the slot exists.</returns>
        public async Task<bool> SaveExistsAsync(string slotKey)
        {
            if (_manager == null) return false;
            return await _manager.SaveExistsAsync(slotKey);
        }

        /// <summary>
        /// Deletes a save slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if deletion was successful.</returns>
        public async Task<bool> DeleteSaveAsync(string slotKey)
        {
            if (_manager == null) return false;
            return await _manager.DeleteSaveAsync(slotKey);
        }

        /// <summary>
        /// Gets metadata for a save slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>Metadata for the save slot, or null if not found.</returns>
        public async Task<SaveSlotMetadata> GetSaveMetadataAsync(string slotKey)
        {
            if (_manager == null) return null;
            return await _manager.GetSaveMetadataAsync(slotKey);
        }

        /// <summary>
        /// Lists all available save slots.
        /// </summary>
        /// <returns>Array of save slot identifiers.</returns>
        public async Task<string[]> GetAllSaveSlotsAsync()
        {
            if (_manager == null) return System.Array.Empty<string>();
            return await _manager.GetAllSaveSlotsAsync();
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Gets the slot config from the registered manager, if available.
        /// </summary>
        public SaveSlotConfig_SO SlotConfig => _manager?.SlotConfig;

        /// <summary>
        /// Gets whether slot management is available (manager registered and has slot config).
        /// </summary>
        public bool HasSlotConfig => _manager?.HasSlotConfig ?? false;

        /// <summary>
        /// Gets whether a slot is currently active.
        /// </summary>
        public bool HasActiveSlot => _manager?.HasActiveSlot ?? false;

        /// <summary>
        /// Gets the current slot index, or -1 if no active slot.
        /// </summary>
        public int CurrentSlotIndex => _manager?.CurrentSlotIndex ?? -1;

        /// <summary>
        /// Gets the current manual save slot key (e.g., "save-1"), or null if no active slot.
        /// </summary>
        public string CurrentManualSlotKey => _manager?.CurrentManualSlotKey;

        /// <summary>
        /// Gets the current autosave slot key (e.g., "autosave-1"), or null if no active slot.
        /// </summary>
        public string CurrentAutosaveSlotKey => _manager?.CurrentAutosaveSlotKey;

        /// <summary>
        /// Sets the active slot index.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-based). Use -1 to clear active slot.</param>
        /// <returns>True if the slot was set successfully.</returns>
        public bool SetActiveSlot(int slotIndex)
        {
            return _manager?.SetActiveSlot(slotIndex) ?? false;
        }

        /// <summary>
        /// Clears the active slot (sets to -1).
        /// </summary>
        public void ClearActiveSlot()
        {
            _manager?.ClearActiveSlot();
        }

        /// <summary>
        /// Saves to the current manual save slot (e.g., "save-1").
        /// </summary>
        /// <returns>True if save was successful.</returns>
        public async Task<bool> SaveToCurrentSlotAsync()
        {
            if (_manager == null) return false;
            return await _manager.SaveToCurrentSlotAsync();
        }

        /// <summary>
        /// Loads from the current manual save slot (e.g., "save-1").
        /// </summary>
        /// <returns>True if load was successful.</returns>
        public async Task<bool> LoadFromCurrentSlotAsync()
        {
            if (_manager == null) return false;
            return await _manager.LoadFromCurrentSlotAsync();
        }

        /// <summary>
        /// Saves to the current autosave slot (e.g., "autosave-1").
        /// </summary>
        /// <returns>True if save was successful.</returns>
        public async Task<bool> AutoSaveAsync()
        {
            if (_manager == null) return false;
            return await _manager.AutoSaveAsync();
        }

        /// <summary>
        /// Loads from the current autosave slot (e.g., "autosave-1").
        /// </summary>
        /// <returns>True if load was successful.</returns>
        public async Task<bool> LoadFromAutosaveAsync()
        {
            if (_manager == null) return false;
            return await _manager.LoadFromAutosaveAsync();
        }

        #endregion

        #region Snapshot Operations

        /// <summary>
        /// Captures the current state of the quest system without saving to storage.
        /// </summary>
        /// <returns>A snapshot of the current quest system state.</returns>
        public QuestSystemSnapshot CaptureSnapshot()
        {
            return _manager?.CaptureSnapshot();
        }

        /// <summary>
        /// Restores the quest system state from a snapshot without loading from storage.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore.</param>
        /// <returns>True if restoration was successful.</returns>
        public bool RestoreSnapshot(QuestSystemSnapshot snapshot)
        {
            return _manager?.RestoreSnapshot(snapshot) ?? false;
        }

        /// <summary>
        /// Validates a snapshot before restoration.
        /// </summary>
        /// <param name="snapshot">The snapshot to validate.</param>
        /// <returns>Validation result with any issues found.</returns>
        public SnapshotValidationResult ValidateSnapshot(QuestSystemSnapshot snapshot)
        {
            return _manager?.ValidateSnapshot(snapshot);
        }

        #endregion

        #region Debug

#if ODIN_INSPECTOR && UNITY_EDITOR
        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        [PropertyOrder(100)]
        private bool ManagerRegistered => IsAvailable;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(101)]
        private string ManagerName => _manager != null ? _manager.name : "(none)";

        [ShowInInspector, ReadOnly]
        [PropertyOrder(102)]
        private bool ProviderConfigured => SaveService.IsConfigured;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(103)]
        private int RegisteredWorldFlagCount => WorldFlagCount;
#endif

        #endregion
    }
}
