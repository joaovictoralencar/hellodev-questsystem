using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// ScriptableObject service locator for QuestSaveManager.
    /// Acts as a "channel" that any asset can reference to access save/load functionality.
    /// The QuestSaveManager registers itself with this service on enable.
    ///
    /// Usage:
    /// 1. Create a single QuestSaveService_SO asset in your project
    /// 2. Assign it to QuestSaveManager's "Service" field
    /// 3. Reference the same asset anywhere you need save/load access
    /// </summary>
    [CreateAssetMenu(fileName = "QuestSaveService", menuName = "HelloDev/Services/Quest Save Service")]
    public class QuestSaveService_SO : ScriptableObject
    {
        #region Private Fields

        private QuestSaveManager _manager;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if a QuestSaveManager is currently registered.
        /// </summary>
        public bool IsAvailable => _manager != null;

        /// <summary>
        /// Gets whether a save provider has been set.
        /// </summary>
        public bool HasProvider => _manager != null && _manager.HasProvider;

        /// <summary>
        /// Gets the number of registered world flags.
        /// </summary>
        public int WorldFlagCount => _manager?.WorldFlagCount ?? 0;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a manager registers with this service.
        /// </summary>
        [System.NonSerialized]
        public UnityEvent OnManagerRegistered = new();

        /// <summary>
        /// Fired when a manager unregisters from this service.
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
        /// Registers a QuestSaveManager with this service.
        /// Called by QuestSaveManager.OnEnable().
        /// </summary>
        public void Register(QuestSaveManager manager)
        {
            if (manager == null) return;

            if (_manager != null && _manager != manager)
            {
                Debug.LogWarning($"[QuestSaveService] Replacing existing manager. Old: {_manager.name}, New: {manager.name}");
            }

            _manager = manager;
            OnManagerRegistered?.Invoke();
        }

        /// <summary>
        /// Unregisters a QuestSaveManager from this service.
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

        #region Provider

        /// <summary>
        /// Sets the save data provider.
        /// </summary>
        /// <param name="provider">The save data provider to use.</param>
        public void SetProvider(ISaveDataProvider provider)
        {
            _manager?.SetProvider(provider);
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
                Debug.LogWarning("[QuestSaveService] No manager registered. Cannot save.");
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
                Debug.LogWarning("[QuestSaveService] No manager registered. Cannot load.");
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
        private bool ProviderSet => HasProvider;

        [ShowInInspector, ReadOnly]
        [PropertyOrder(103)]
        private int RegisteredWorldFlagCount => WorldFlagCount;
#endif

        #endregion
    }
}
