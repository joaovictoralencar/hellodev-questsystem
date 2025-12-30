using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// ScriptableObject service for managing save slot selection.
    /// Tracks the current active slot and provides slot-based naming conventions.
    /// When playing on slot X, autosaves go to "autosave-X" instead of overwriting a single autosave file.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveSlotService", menuName = "HelloDev/Services/Save Slot Service")]
    public class SaveSlotService_SO : ScriptableObject
    {
        #region Configuration

#if ODIN_INSPECTOR
        [Title("Slot Configuration")]
#endif
        [SerializeField]
        [Tooltip("Maximum number of save slots available (1-indexed in UI, 0-indexed internally).")]
        [Min(1)]
        private int maxSlots = 3;

        [SerializeField]
        [Tooltip("Prefix for manual save slot names (e.g., 'save' -> 'save-0', 'save-1').")]
        private string manualSavePrefix = "save";

        [SerializeField]
        [Tooltip("Prefix for autosave slot names (e.g., 'autosave' -> 'autosave-0', 'autosave-1').")]
        private string autosavePrefix = "autosave";

        #endregion

        #region Runtime State

        /// <summary>
        /// The currently active slot index (0-based).
        /// -1 means no slot is active (new game not yet saved).
        /// </summary>
        private int _currentSlotIndex = -1;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the maximum number of slots.
        /// </summary>
        public int MaxSlots => maxSlots;

        /// <summary>
        /// Gets the current slot index (0-based). Returns -1 if no slot is active.
        /// </summary>
        public int CurrentSlotIndex => _currentSlotIndex;

        /// <summary>
        /// Returns true if a slot is currently active.
        /// </summary>
        public bool HasActiveSlot => _currentSlotIndex >= 0;

        /// <summary>
        /// Gets the current manual save slot key (e.g., "save-1").
        /// Returns null if no slot is active.
        /// </summary>
        public string CurrentManualSlotKey =>
            HasActiveSlot ? GetManualSlotKey(_currentSlotIndex) : null;

        /// <summary>
        /// Gets the current autosave slot key (e.g., "autosave-1").
        /// Returns null if no slot is active.
        /// </summary>
        public string CurrentAutosaveSlotKey =>
            HasActiveSlot ? GetAutosaveSlotKey(_currentSlotIndex) : null;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the active slot changes. Parameters: (previousIndex, newIndex)
        /// </summary>
        [System.NonSerialized]
        public UnityEvent<int, int> OnSlotChanged = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the active slot index. Called when loading a save or starting new game.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-based). Use -1 to clear active slot.</param>
        public void SetActiveSlot(int slotIndex)
        {
            if (slotIndex < -1 || slotIndex >= maxSlots)
            {
                Debug.LogWarning($"[SaveSlotService] Invalid slot index: {slotIndex}. Must be -1 to {maxSlots - 1}.");
                return;
            }

            int previousIndex = _currentSlotIndex;
            _currentSlotIndex = slotIndex;

            if (previousIndex != slotIndex)
            {
                Debug.Log($"[SaveSlotService] Active slot changed: {previousIndex} -> {slotIndex}");
                OnSlotChanged?.Invoke(previousIndex, slotIndex);
            }
        }

        /// <summary>
        /// Clears the active slot (sets to -1).
        /// </summary>
        public void ClearActiveSlot()
        {
            SetActiveSlot(-1);
        }

        /// <summary>
        /// Gets the manual save slot key for a specific index.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-based).</param>
        /// <returns>The slot key (e.g., "save-0", "save-1").</returns>
        public string GetManualSlotKey(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                Debug.LogWarning($"[SaveSlotService] Invalid slot index: {slotIndex}. Must be 0 to {maxSlots - 1}.");
                return null;
            }
            return $"{manualSavePrefix}-{slotIndex}";
        }

        /// <summary>
        /// Gets the autosave slot key for a specific index.
        /// </summary>
        /// <param name="slotIndex">The slot index (0-based).</param>
        /// <returns>The autosave slot key (e.g., "autosave-0", "autosave-1").</returns>
        public string GetAutosaveSlotKey(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                Debug.LogWarning($"[SaveSlotService] Invalid slot index: {slotIndex}. Must be 0 to {maxSlots - 1}.");
                return null;
            }
            return $"{autosavePrefix}-{slotIndex}";
        }

        /// <summary>
        /// Extracts the slot index from a slot key (manual or autosave).
        /// Returns -1 if the key doesn't match expected format.
        /// </summary>
        /// <param name="slotKey">The slot key to parse.</param>
        /// <returns>The slot index, or -1 if not recognized.</returns>
        public int GetSlotIndexFromKey(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey)) return -1;

            // Try manual save prefix
            string manualPrefix = manualSavePrefix + "-";
            if (slotKey.StartsWith(manualPrefix))
            {
                if (int.TryParse(slotKey.Substring(manualPrefix.Length), out int index))
                    return index;
            }

            // Try autosave prefix
            string autoPrefix = autosavePrefix + "-";
            if (slotKey.StartsWith(autoPrefix))
            {
                if (int.TryParse(slotKey.Substring(autoPrefix.Length), out int index))
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// Checks if a slot index is valid.
        /// </summary>
        /// <param name="slotIndex">The slot index to check.</param>
        /// <returns>True if the index is valid (0 to maxSlots-1).</returns>
        public bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < maxSlots;
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            // Reset runtime state when entering play mode
            _currentSlotIndex = -1;
        }

        #endregion

        #region Debug

#if ODIN_INSPECTOR && UNITY_EDITOR
        [Title("Debug (Runtime)")]
        [ShowInInspector, ReadOnly]
        private int DebugCurrentSlotIndex => _currentSlotIndex;

        [ShowInInspector, ReadOnly]
        private bool DebugHasActiveSlot => HasActiveSlot;

        [ShowInInspector, ReadOnly]
        private string DebugCurrentManualSlotKey => CurrentManualSlotKey ?? "(none)";

        [ShowInInspector, ReadOnly]
        private string DebugCurrentAutosaveSlotKey => CurrentAutosaveSlotKey ?? "(none)";

        [Button("Set Slot 0")]
        [ButtonGroup("SlotButtons")]
        private void DebugSetSlot0() => SetActiveSlot(0);

        [Button("Set Slot 1")]
        [ButtonGroup("SlotButtons")]
        private void DebugSetSlot1() => SetActiveSlot(1);

        [Button("Set Slot 2")]
        [ButtonGroup("SlotButtons")]
        private void DebugSetSlot2() => SetActiveSlot(2);

        [Button("Clear Slot")]
        [ButtonGroup("SlotButtons")]
        private void DebugClearSlot() => ClearActiveSlot();
#endif

        #endregion
    }
}
