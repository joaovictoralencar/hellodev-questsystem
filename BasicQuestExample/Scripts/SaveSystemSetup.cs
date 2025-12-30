using HelloDev.QuestSystem.SaveLoad;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicQuestExample
{
    /// <summary>
    /// Initializes the save system with a JsonFileSaveProvider.
    /// Add this component to your scene to enable save/load functionality.
    /// </summary>
    public class SaveSystemSetup : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [Title("Save Service")]
        [Required]
#endif
        [SerializeField]
        [Tooltip("The QuestSaveService_SO to configure.")]
        private QuestSaveService_SO saveService;

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
        [Title("Auto Save/Load")]
#endif
        [SerializeField]
        [Tooltip("The slot used for auto-save and auto-load.")]
        private string autoSlot = "autosave";

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

        #endregion

        #region Private Fields

        private float _autoSaveTimer;
        private bool _isInitialized;

        #endregion

        #region Unity Lifecycle

        private async void Start()
        {
            Debug.Log("[SaveSystemSetup] Start() called.");

            if (saveService == null)
            {
                Debug.LogWarning("[SaveSystemSetup] No QuestSaveService_SO assigned. Save system will not be initialized.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Service available: {saveService.IsAvailable}");

            if (!saveService.IsAvailable)
            {
                Debug.LogWarning("[SaveSystemSetup] QuestSaveManager not registered with service. Ensure QuestSaveManager is in the scene.");
                return;
            }

            var provider = new JsonFileSaveProvider(saveSubdirectory, fileExtension, prettyPrint);
            saveService.SetProvider(provider);

            Debug.Log($"[SaveSystemSetup] Provider set. HasProvider: {saveService.HasProvider}");

            _isInitialized = true;
            _autoSaveTimer = autoSaveInterval;

            if (autoLoadOnStart)
            {
                await AutoLoadAsync();
            }
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
            if (string.IsNullOrEmpty(autoSlot))
            {
                Debug.LogWarning("[SaveSystemSetup] Auto-load enabled but no slot specified.");
                return;
            }

            bool exists = await saveService.SaveExistsAsync(autoSlot);
            if (!exists)
            {
                Debug.Log($"[SaveSystemSetup] Auto-load slot '{autoSlot}' does not exist. Skipping.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Auto-loading from slot '{autoSlot}'...");
            bool success = await saveService.LoadAsync(autoSlot);

            if (success)
            {
                Debug.Log($"[SaveSystemSetup] Auto-load from '{autoSlot}' successful.");
            }
            else
            {
                Debug.LogWarning($"[SaveSystemSetup] Auto-load from '{autoSlot}' failed.");
            }
        }

        private async System.Threading.Tasks.Task AutoSaveAsync(string trigger)
        {
            if (string.IsNullOrEmpty(autoSlot))
            {
                Debug.LogWarning("[SaveSystemSetup] Auto-save triggered but no slot specified.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Auto-saving to slot '{autoSlot}' (trigger: {trigger})...");
            bool success = await saveService.SaveAsync(autoSlot);

            if (success)
            {
                Debug.Log($"[SaveSystemSetup] Auto-save to '{autoSlot}' successful.");
            }
            else
            {
                Debug.LogWarning($"[SaveSystemSetup] Auto-save to '{autoSlot}' failed.");
            }
        }

        private void AutoSaveSync(string trigger)
        {
            if (string.IsNullOrEmpty(autoSlot))
            {
                Debug.LogWarning("[SaveSystemSetup] Auto-save triggered but no slot specified.");
                return;
            }

            Debug.Log($"[SaveSystemSetup] Auto-saving to slot '{autoSlot}' (trigger: {trigger})...");

            // Use synchronous snapshot + save for quit scenarios
            var snapshot = saveService.CaptureSnapshot();
            if (snapshot != null)
            {
                // We need to save synchronously, so we'll wait on the task
                var task = saveService.SaveAsync(autoSlot);
                task.Wait();

                if (task.Result)
                {
                    Debug.Log($"[SaveSystemSetup] Auto-save to '{autoSlot}' successful.");
                }
                else
                {
                    Debug.LogWarning($"[SaveSystemSetup] Auto-save to '{autoSlot}' failed.");
                }
            }
        }

        #endregion

        #region Debug

#if ODIN_INSPECTOR && UNITY_EDITOR
        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        private bool IsServiceAvailable => saveService != null && saveService.IsAvailable;

        [ShowInInspector, ReadOnly]
        private bool HasProvider => saveService != null && saveService.HasProvider;

        [ShowInInspector, ReadOnly]
        private bool IsInitialized => _isInitialized;

        [ShowInInspector, ReadOnly]
        [ShowIf("@autoSaveInterval > 0")]
        private float TimeUntilNextAutoSave => _autoSaveTimer;

        [ShowInInspector, ReadOnly]
        private string SavePath => System.IO.Path.Combine(Application.persistentDataPath, saveSubdirectory);

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
