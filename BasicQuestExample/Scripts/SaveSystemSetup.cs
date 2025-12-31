using System.Threading.Tasks;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.Saving;
using HelloDev.Utils;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicQuestExample
{
    /// <summary>
    /// Initializes the save system with a JsonSaveProvider via SaveService.
    /// Add this component to your scene to enable save/load functionality.
    /// Slot management is now handled by QuestSaveManager and accessed via the locator.
    /// Implements IBootstrapInitializable for proper initialization ordering (priority 250 - Data Loading phase).
    /// </summary>
    public class SaveSystemSetup : MonoBehaviour, IBootstrapInitializable
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [Title("Save Locator")]
        [Required]
#endif
        [SerializeField]
        [Tooltip("The QuestSaveLocator_SO to configure.")]
        private QuestSaveLocator_SO saveLocator;

#if ODIN_INSPECTOR
        [Title("Provider Settings")]
#endif
        [SerializeField]
        [Tooltip("Subdirectory within Application.persistentDataPath for save files.")]
        private string saveSubdirectory = "QuestSaves";

        [SerializeField]
        [Tooltip("File extension for save files.")]
        private string fileExtension = ".questsave";

        [SerializeField]
        [Tooltip("If true, JSON output is formatted for readability (larger files).")]
        private bool prettyPrint = true;

#if ODIN_INSPECTOR
        [Title("Slot Settings")]
#endif
        [SerializeField]
        [Tooltip("The default slot index to activate on start. Set to -1 to not auto-activate any slot. Requires SlotConfig on QuestSaveManager.")]
        [Min(-1)]
        private int defaultSlotIndex = 0;

#if ODIN_INSPECTOR
        [Title("Auto Save/Load")]
#endif
        [SerializeField]
        [Tooltip("Fallback slot used for auto-save/load when no slot is active.")]
        private string fallbackAutoSlot = "autosave";

        [SerializeField]
        [Tooltip("If true, automatically loads from the auto slot on startup.")]
        private bool autoLoadOnStart;

        [SerializeField]
        [Tooltip("If true, automatically saves to the auto slot when the application quits.")]
        private bool autoSaveOnQuit;

        [SerializeField]
        [Tooltip("If true, automatically saves when the application loses focus (useful for mobile).")]
        private bool autoSaveOnPause;

        [SerializeField]
        [Tooltip("If greater than 0, automatically saves at this interval in seconds.")]
        [Min(0f)]
        private float autoSaveInterval;

#if ODIN_INSPECTOR
        [Title("Initialization")]
        [ToggleLeft]
#else
        [Header("Initialization")]
#endif
        [SerializeField]
        [Tooltip("If true, initializes in Start. If false, waits for GameBootstrap.")]
        private bool selfInitialize = true;

        #endregion

        #region Private Fields

        private float _autoSaveTimer;
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current autosave slot key.
        /// Uses locator's slot if available and active, otherwise falls back to fallbackAutoSlot.
        /// </summary>
        private string CurrentAutoSlot =>
            (saveLocator != null && saveLocator.HasActiveSlot)
                ? saveLocator.CurrentAutosaveSlotKey
                : fallbackAutoSlot;

        #endregion

        #region IBootstrapInitializable

        /// <inheritdoc />
        public bool SelfInitialize => selfInitialize;

        /// <summary>
        /// Priority 250 - Data Loading phase. Runs after QuestManager (150) and QuestSaveManager (200).
        /// </summary>
        public int InitializationPriority => 250;

        /// <summary>
        /// Whether this system has completed initialization.
        /// </summary>
        bool IBootstrapInitializable.IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the save system and optionally loads save data.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            Debug.Log("[SaveSystemSetup] InitializeAsync() called.");

            if (saveLocator == null)
            {
                Debug.LogWarning("[SaveSystemSetup] No QuestSaveLocator_SO assigned. Save system will not be initialized.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Locator available: {saveLocator.IsAvailable}");

            if (!saveLocator.IsAvailable)
            {
                Debug.LogWarning("[SaveSystemSetup] QuestSaveManager not registered with locator. Ensure QuestSaveManager is in the scene.");
                return;
            }

            // Configure save provider via SaveService (global, used by QuestSaveManager)
            var provider = new JsonSaveProvider(saveSubdirectory, fileExtension, prettyPrint);
            SaveService.SetProvider(provider);

            Debug.Log($"[SaveSystemSetup] Provider set. HasProvider: {saveLocator.HasProvider}");

            // Set default active slot via the locator
            if (defaultSlotIndex >= 0 && saveLocator.HasSlotConfig)
            {
                saveLocator.SetActiveSlot(defaultSlotIndex);
                Debug.Log($"[SaveSystemSetup] Active slot set to {defaultSlotIndex}. Autosave target: {saveLocator.CurrentAutosaveSlotKey}");
            }

            _isInitialized = true;
            _autoSaveTimer = autoSaveInterval;

            if (autoLoadOnStart)
            {
                await AutoLoadAsync();
            }
        }

        /// <summary>
        /// Cleans up the save system.
        /// </summary>
        public void Shutdown()
        {
            _isInitialized = false;
            Debug.Log("[SaveSystemSetup] Shutdown.");
        }

        #endregion

        #region Unity Lifecycle

        private async void Start()
        {
            // If already initialized by bootstrap or not self-initializing, skip
            if (_isInitialized || !selfInitialize) return;

            // Standalone mode - initialize ourselves
            Debug.Log("[SaveSystemSetup] Start() - standalone initialization.");
            await InitializeAsync();
        }

        private void Update()
        {
            if (!_isInitialized || autoSaveInterval <= 0f)
                return;

            _autoSaveTimer -= Time.deltaTime;
            if (_autoSaveTimer <= 0f)
            {
                _autoSaveTimer = autoSaveInterval;
                _ = AutoSaveAsync("interval");
            }
        }

        private void OnApplicationQuit()
        {
            if (_isInitialized && autoSaveOnQuit)
            {
                AutoSaveSync("quit");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_isInitialized && autoSaveOnPause && pauseStatus)
            {
                _ = AutoSaveAsync("pause");
            }
        }

        #endregion

        #region Auto Save/Load

        private async System.Threading.Tasks.Task AutoLoadAsync()
        {
            string targetSlot = CurrentAutoSlot;
            if (string.IsNullOrEmpty(targetSlot))
            {
                Debug.LogWarning("[SaveSystemSetup] Auto-load enabled but no slot specified.");
                return;
            }

            bool exists = await saveLocator.SaveExistsAsync(targetSlot);
            if (!exists)
            {
                Debug.Log($"[SaveSystemSetup] Auto-load slot '{targetSlot}' does not exist. Skipping.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Auto-loading from slot '{targetSlot}'...");
            bool success = await saveLocator.LoadAsync(targetSlot);

            if (success)
            {
                Debug.Log($"[SaveSystemSetup] Auto-load from '{targetSlot}' successful.");
            }
            else
            {
                Debug.LogWarning($"[SaveSystemSetup] Auto-load from '{targetSlot}' failed.");
            }
        }

        private async System.Threading.Tasks.Task AutoSaveAsync(string trigger)
        {
            string targetSlot = CurrentAutoSlot;
            if (string.IsNullOrEmpty(targetSlot))
            {
                Debug.LogWarning("[SaveSystemSetup] Auto-save triggered but no slot specified.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Auto-saving to slot '{targetSlot}' (trigger: {trigger})...");
            bool success = await saveLocator.SaveAsync(targetSlot);

            if (success)
            {
                Debug.Log($"[SaveSystemSetup] Auto-save to '{targetSlot}' successful.");
            }
            else
            {
                Debug.LogWarning($"[SaveSystemSetup] Auto-save to '{targetSlot}' failed.");
            }
        }

        private void AutoSaveSync(string trigger)
        {
            string targetSlot = CurrentAutoSlot;
            if (string.IsNullOrEmpty(targetSlot))
            {
                Debug.LogWarning("[SaveSystemSetup] Auto-save triggered but no slot specified.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Auto-saving to slot '{targetSlot}' (trigger: {trigger})...");

            // Use synchronous snapshot + save for quit scenarios
            var snapshot = saveLocator.CaptureSnapshot();
            if (snapshot != null)
            {
                // We need to save synchronously, so we'll wait on the task
                var task = saveLocator.SaveAsync(targetSlot);
                task.Wait();

                if (task.Result)
                {
                    Debug.Log($"[SaveSystemSetup] Auto-save to '{targetSlot}' successful.");
                }
                else
                {
                    Debug.LogWarning($"[SaveSystemSetup] Auto-save to '{targetSlot}' failed.");
                }
            }
        }

        #endregion

        #region Debug

#if ODIN_INSPECTOR && UNITY_EDITOR
        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private bool IsLocatorAvailable => saveLocator != null && saveLocator.IsAvailable;

        [ShowInInspector, ReadOnly]
        private bool HasProvider => saveLocator != null && saveLocator.HasProvider;

        [ShowInInspector, ReadOnly]
        private bool IsInitialized => _isInitialized;

        [ShowInInspector, ReadOnly]
        [ShowIf("@autoSaveInterval > 0")]
        private float TimeUntilNextAutoSave => _autoSaveTimer;

        [ShowInInspector, ReadOnly]
        private string SavePath => System.IO.Path.Combine(Application.persistentDataPath, saveSubdirectory);

        [ShowInInspector, ReadOnly]
        private string DebugCurrentAutoSlot => CurrentAutoSlot ?? "(none)";

        [ShowInInspector, ReadOnly]
        private bool DebugHasSlotConfig => saveLocator != null && saveLocator.HasSlotConfig;

        [ShowInInspector, ReadOnly]
        [ShowIf("DebugHasSlotConfig")]
        private int DebugCurrentSlotIndex => saveLocator?.CurrentSlotIndex ?? -1;

        [Button("Save Now")]
        [PropertyOrder(100)]
        [EnableIf("_isInitialized")]
        private async void DebugSaveNow()
        {
            await AutoSaveAsync("manual");
        }

        [Button("Load Now")]
        [PropertyOrder(101)]
        [EnableIf("_isInitialized")]
        private async void DebugLoadNow()
        {
            await AutoLoadAsync();
        }

        [Button("Open Save Folder")]
        [PropertyOrder(102)]
        private void OpenSaveFolder()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, saveSubdirectory);
            if (System.IO.Directory.Exists(path))
            {
                Application.OpenURL("file://" + path);
            }
            else
            {
                Debug.Log($"[SaveSystemSetup] Save folder does not exist yet: {path}");
            }
        }
#endif

        #endregion
    }
}
