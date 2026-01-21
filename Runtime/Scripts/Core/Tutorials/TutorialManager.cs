using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Tutorials.SaveLoad;
using HelloDev.QuestSystem.Utils;
using HelloDev.Saving;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Tutorials
{
    /// <summary>
    /// Central manager for all tutorials. Handles tutorial lifecycle and event delegation.
    /// Implements IBootstrapInitializable for proper initialization ordering (priority 105 - Core phase).
    /// </summary>
    public class TutorialManager : MonoBehaviour, IBootstrapInitializable
    {
        #region Singleton

        private static TutorialManager _instance;

        /// <summary>
        /// Gets the singleton instance of the TutorialManager.
        /// </summary>
        public static TutorialManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TutorialManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[TutorialManager] No TutorialManager found in scene.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Tutorial Database"), ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
#else
        [Header("Tutorial Database")]
#endif
        [SerializeField, Tooltip("All available tutorials.")]
        private List<Tutorial_SO> tutorialDatabase = new();

#if ODIN_INSPECTOR
        [TitleGroup("Configuration"), ToggleLeft]
#else
        [Header("Configuration")]
#endif
        [SerializeField, Tooltip("If true, debug messages will be logged.")]
        private bool enableDebugLogging = true;

#if ODIN_INSPECTOR
        [TitleGroup("Configuration"), ToggleLeft]
#endif
        [SerializeField, Tooltip("If true, tutorials can queue and play sequentially.")]
        private bool allowTutorialQueue = true;

#if ODIN_INSPECTOR
        [TitleGroup("Initialization Mode")]
        [ToggleLeft]
        [InfoBox("Disable when using GameBootstrap for coordinated initialization.")]
#else
        [Header("Initialization Mode")]
#endif
        [SerializeField]
        [Tooltip("If true, self-initializes in Awake. Disable when using GameBootstrap.")]
        private bool selfInitialize = true;

#if ODIN_INSPECTOR
        [TitleGroup("Save System")]
#else
        [Header("Save System")]
#endif
        [SerializeField]
        [Tooltip("The unified save locator for registering the snapshot provider. Optional - if not set, snapshot provider won't auto-register.")]
        private UnifiedSaveLocator_SO unifiedSaveLocator;

        #endregion

        #region Events

        /// <summary>
        /// Fired when any tutorial starts.
        /// </summary>
        public UnityEvent<TutorialRuntime> OnTutorialStarted = new();

        /// <summary>
        /// Fired when any tutorial completes.
        /// </summary>
        public UnityEvent<TutorialRuntime> OnTutorialCompleted = new();

        /// <summary>
        /// Fired when any tutorial step starts.
        /// </summary>
        public UnityEvent<TutorialRuntime, TutorialStepRuntime> OnStepStarted = new();

        /// <summary>
        /// Fired when any tutorial step completes.
        /// </summary>
        public UnityEvent<TutorialRuntime, TutorialStepRuntime> OnStepCompleted = new();

        #endregion

        #region Private Fields

        private readonly Dictionary<Guid, TutorialRuntime> _activeTutorials = new();
        private readonly HashSet<Guid> _completedTutorialIds = new();
        private readonly Queue<TutorialRuntime> _tutorialQueue = new();
        private TutorialRuntime _currentTutorial;
        private bool _isInitialized;
        private TutorialSnapshotProvider _snapshotProvider;

        #endregion

        #region Properties

        /// <summary>
        /// Gets all currently active tutorials.
        /// </summary>
        public IReadOnlyCollection<TutorialRuntime> ActiveTutorials => _activeTutorials.Values;

        /// <summary>
        /// Gets the currently playing tutorial.
        /// </summary>
        public TutorialRuntime CurrentTutorial => _currentTutorial;

        /// <summary>
        /// Gets whether a tutorial is currently playing.
        /// </summary>
        public bool IsTutorialActive => _currentTutorial != null &&
            _currentTutorial.CurrentState == ObjectiveState.InProgress;

        /// <summary>
        /// Gets the IDs of all completed tutorials.
        /// </summary>
        public IReadOnlyCollection<Guid> CompletedTutorialIds => _completedTutorialIds;

        /// <summary>
        /// Gets the snapshot provider for unified save system integration.
        /// </summary>
        public TutorialSnapshotProvider SnapshotProvider => _snapshotProvider;

        #endregion

        #region IBootstrapInitializable

        /// <summary>
        /// Whether this manager should self-initialize.
        /// </summary>
        public bool SelfInitialize => selfInitialize;

        /// <summary>
        /// Priority 105 - Core phase. Runs after QuestManager (100).
        /// </summary>
        public int InitializationPriority => 105;

        /// <summary>
        /// Whether this manager has completed initialization.
        /// </summary>
        bool IBootstrapInitializable.IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the tutorial manager.
        /// </summary>
        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            QuestLogger.Log(LogSubsystem.Tutorial, "TutorialManager starting initialization...");

            Initialize();

            QuestLogger.Log(LogSubsystem.Tutorial, "TutorialManager initialized.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Shuts down the tutorial manager.
        /// </summary>
        public void Shutdown()
        {
            // Unregister snapshot provider from unified save system
            if (_snapshotProvider != null && unifiedSaveLocator != null && unifiedSaveLocator.IsAvailable)
            {
                unifiedSaveLocator.Manager.UnregisterSystem(_snapshotProvider);
                QuestLogger.LogVerbose(LogSubsystem.Tutorial, "TutorialSnapshotProvider unregistered from unified save system");
            }
            _snapshotProvider = null;

            // Unsubscribe from all active tutorial events
            foreach (var tutorial in _activeTutorials.Values)
            {
                tutorial.OnTutorialStarted.RemoveListener(HandleTutorialStarted);
                tutorial.OnTutorialCompleted.RemoveListener(HandleTutorialCompleted);
                tutorial.OnStepStarted.RemoveListener(HandleStepStarted);
                tutorial.OnStepCompleted.RemoveListener(HandleStepCompleted);
            }

            _activeTutorials.Clear();
            _tutorialQueue.Clear();
            _currentTutorial = null;
            _isInitialized = false;

            QuestLogger.Log(LogSubsystem.Tutorial, "TutorialManager shutdown.");
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Self-initialize if not using bootstrap
            if (selfInitialize && !_isInitialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            // Update timed steps
            if (_currentTutorial != null && _currentTutorial.CurrentState == ObjectiveState.InProgress)
            {
                _currentTutorial.UpdateTime(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            // Unsubscribe from all active tutorial events
            foreach (var tutorial in _activeTutorials.Values)
            {
                tutorial.OnTutorialStarted.RemoveListener(HandleTutorialStarted);
                tutorial.OnTutorialCompleted.RemoveListener(HandleTutorialCompleted);
                tutorial.OnStepStarted.RemoveListener(HandleStepStarted);
                tutorial.OnStepCompleted.RemoveListener(HandleStepCompleted);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            QuestLogger.IsLoggingEnabled = enableDebugLogging;
            QuestLogger.Log(LogSubsystem.Tutorial, $"TutorialManager initialized with {tutorialDatabase.Count} tutorials.");

            // Create and register snapshot provider for unified save system
            CreateAndRegisterSnapshotProvider();

            _isInitialized = true;
        }

        /// <summary>
        /// Creates the TutorialSnapshotProvider and registers it with the unified save system.
        /// </summary>
        private void CreateAndRegisterSnapshotProvider()
        {
            _snapshotProvider = new TutorialSnapshotProvider(this);

            if (unifiedSaveLocator != null && unifiedSaveLocator.IsAvailable)
            {
                unifiedSaveLocator.Manager.RegisterSystem(_snapshotProvider);
                QuestLogger.Log(LogSubsystem.Tutorial, "TutorialSnapshotProvider registered with unified save system");
            }
            else
            {
                QuestLogger.LogVerbose(LogSubsystem.Tutorial, "No UnifiedSaveLocator assigned or not available - snapshot provider created but not registered");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts a tutorial by its ScriptableObject.
        /// </summary>
        /// <param name="tutorialData">The tutorial to start.</param>
        /// <returns>The runtime tutorial instance, or null if it couldn't be started.</returns>
        public TutorialRuntime StartTutorial(Tutorial_SO tutorialData)
        {
            if (tutorialData == null)
            {
                QuestLogger.Log(LogSubsystem.Tutorial, "Cannot start null tutorial.");
                return null;
            }

            // Check if already completed (for PlayOnce tutorials)
            if (tutorialData.PlayOnce && _completedTutorialIds.Contains(tutorialData.TutorialId))
            {
                QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{tutorialData.DevName}' already completed (PlayOnce).");
                return null;
            }

            // Check if already active
            if (_activeTutorials.ContainsKey(tutorialData.TutorialId))
            {
                QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{tutorialData.DevName}' is already active.");
                return _activeTutorials[tutorialData.TutorialId];
            }

            // Create runtime instance
            var runtime = tutorialData.GetRuntimeTutorial();
            _activeTutorials[tutorialData.TutorialId] = runtime;

            // Subscribe to events
            runtime.OnTutorialStarted.AddListener(HandleTutorialStarted);
            runtime.OnTutorialCompleted.AddListener(HandleTutorialCompleted);
            runtime.OnStepStarted.AddListener(HandleStepStarted);
            runtime.OnStepCompleted.AddListener(HandleStepCompleted);

            // Queue or start immediately
            if (allowTutorialQueue && _currentTutorial != null &&
                _currentTutorial.CurrentState == ObjectiveState.InProgress)
            {
                _tutorialQueue.Enqueue(runtime);
                QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial '{tutorialData.DevName}' queued.");
            }
            else
            {
                _currentTutorial = runtime;
                runtime.StartTutorial();
            }

            return runtime;
        }

        /// <summary>
        /// Starts a tutorial by its ID (from the database).
        /// </summary>
        /// <param name="tutorialId">The tutorial ID.</param>
        /// <returns>The runtime tutorial instance, or null if not found.</returns>
        public TutorialRuntime StartTutorial(Guid tutorialId)
        {
            var tutorialData = tutorialDatabase.FirstOrDefault(t => t.TutorialId == tutorialId);
            if (tutorialData == null)
            {
                QuestLogger.Log(LogSubsystem.Tutorial, $"Tutorial with ID '{tutorialId}' not found in database.");
                return null;
            }

            return StartTutorial(tutorialData);
        }

        /// <summary>
        /// Completes the current step of the active tutorial.
        /// </summary>
        public void CompleteCurrentStep()
        {
            _currentTutorial?.CompleteCurrentStep();
        }

        /// <summary>
        /// Skips the current step of the active tutorial (if allowed).
        /// </summary>
        /// <returns>True if skipped.</returns>
        public bool SkipCurrentStep()
        {
            return _currentTutorial?.SkipCurrentStep() ?? false;
        }

        /// <summary>
        /// Skips the entire active tutorial (if allowed).
        /// </summary>
        /// <returns>True if skipped.</returns>
        public bool SkipCurrentTutorial()
        {
            return _currentTutorial?.SkipTutorial() ?? false;
        }

        /// <summary>
        /// Checks if a tutorial has been completed.
        /// </summary>
        /// <param name="tutorialId">The tutorial ID.</param>
        /// <returns>True if completed.</returns>
        public bool IsTutorialCompleted(Guid tutorialId)
        {
            return _completedTutorialIds.Contains(tutorialId);
        }

        /// <summary>
        /// Marks a tutorial as completed (for save/load restoration).
        /// </summary>
        /// <param name="tutorialId">The tutorial ID.</param>
        public void MarkTutorialCompleted(Guid tutorialId)
        {
            _completedTutorialIds.Add(tutorialId);
        }

        /// <summary>
        /// Resets all tutorial progress.
        /// </summary>
        public void ResetAllProgress()
        {
            _completedTutorialIds.Clear();
            _activeTutorials.Clear();
            _tutorialQueue.Clear();
            _currentTutorial = null;

            QuestLogger.Log(LogSubsystem.Tutorial, "Tutorial progress reset.");
        }

        /// <summary>
        /// Gets a tutorial from the database by ID.
        /// </summary>
        /// <param name="tutorialId">The tutorial ID.</param>
        /// <returns>The tutorial ScriptableObject, or null if not found.</returns>
        public Tutorial_SO GetTutorialData(Guid tutorialId)
        {
            return tutorialDatabase.FirstOrDefault(t => t.TutorialId == tutorialId);
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Gets the IDs of all completed tutorials for saving.
        /// </summary>
        public List<string> GetCompletedTutorialIdsForSave()
        {
            return _completedTutorialIds.Select(id => id.ToString()).ToList();
        }

        /// <summary>
        /// Restores completed tutorial IDs from a save.
        /// </summary>
        /// <param name="tutorialIds">The list of completed tutorial IDs.</param>
        public void RestoreCompletedTutorialIds(List<string> tutorialIds)
        {
            _completedTutorialIds.Clear();
            foreach (var idString in tutorialIds)
            {
                if (Guid.TryParse(idString, out var id))
                {
                    _completedTutorialIds.Add(id);
                }
            }

            QuestLogger.Log(LogSubsystem.Tutorial, $"Restored {_completedTutorialIds.Count} completed tutorial IDs.");
        }

        /// <summary>
        /// Starts a tutorial for restore purposes (skips PlayOnce check and doesn't auto-start).
        /// </summary>
        /// <param name="tutorialData">The tutorial to start.</param>
        /// <returns>The runtime tutorial instance.</returns>
        public TutorialRuntime StartTutorialForRestore(Tutorial_SO tutorialData)
        {
            if (tutorialData == null) return null;

            // Check if already active
            if (_activeTutorials.ContainsKey(tutorialData.TutorialId))
            {
                return _activeTutorials[tutorialData.TutorialId];
            }

            // Create runtime instance
            var runtime = tutorialData.GetRuntimeTutorial();
            _activeTutorials[tutorialData.TutorialId] = runtime;
            _currentTutorial = runtime;

            // Subscribe to events
            runtime.OnTutorialStarted.AddListener(HandleTutorialStarted);
            runtime.OnTutorialCompleted.AddListener(HandleTutorialCompleted);
            runtime.OnStepStarted.AddListener(HandleStepStarted);
            runtime.OnStepCompleted.AddListener(HandleStepCompleted);

            // Don't start the tutorial yet - state will be restored by caller
            return runtime;
        }

        /// <summary>
        /// Captures the current tutorial state as a snapshot for saving.
        /// </summary>
        /// <returns>A snapshot containing all tutorial state.</returns>
        public TutorialSystemSnapshot CaptureSnapshot()
        {
            var snapshot = new TutorialSystemSnapshot();

            // Capture completed tutorial IDs
            snapshot.CompletedTutorialIds = GetCompletedTutorialIdsForSave();

            // Capture active tutorial if any
            if (_currentTutorial != null && _currentTutorial.CurrentState == ObjectiveState.InProgress)
            {
                snapshot.ActiveTutorial = CaptureTutorial(_currentTutorial);
            }

            // Capture queued tutorials
            foreach (var queuedTutorial in _tutorialQueue)
            {
                snapshot.QueuedTutorials.Add(CaptureTutorial(queuedTutorial));
            }

            QuestLogger.Log(LogSubsystem.Tutorial, $"Captured tutorial snapshot: {snapshot.CompletedTutorialIds.Count} completed" +
                (snapshot.ActiveTutorial != null ? ", 1 active" : "") +
                (snapshot.QueuedTutorials.Count > 0 ? $", {snapshot.QueuedTutorials.Count} queued" : ""));

            return snapshot;
        }

        /// <summary>
        /// Restores tutorial state from a snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        public void RestoreSnapshot(TutorialSystemSnapshot snapshot)
        {
            if (snapshot == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Tutorial, "Cannot restore null snapshot.");
                return;
            }

            // Reset current state
            ResetAllProgress();

            // Restore completed tutorial IDs
            if (snapshot.CompletedTutorialIds != null && snapshot.CompletedTutorialIds.Count > 0)
            {
                RestoreCompletedTutorialIds(snapshot.CompletedTutorialIds);
            }

            // Restore active tutorial
            if (snapshot.ActiveTutorial != null)
            {
                RestoreActiveTutorial(snapshot.ActiveTutorial);
            }

            // Restore queued tutorials
            foreach (var queuedSnapshot in snapshot.QueuedTutorials)
            {
                RestoreQueuedTutorial(queuedSnapshot);
            }

            QuestLogger.Log(LogSubsystem.Tutorial, $"Restored tutorial snapshot: {snapshot.CompletedTutorialIds?.Count ?? 0} completed" +
                (snapshot.ActiveTutorial != null ? ", 1 active" : "") +
                (snapshot.QueuedTutorials.Count > 0 ? $", {snapshot.QueuedTutorials.Count} queued" : ""));
        }

        private TutorialSnapshot CaptureTutorial(TutorialRuntime tutorial)
        {
            var snapshot = new TutorialSnapshot
            {
                TutorialGuid = tutorial.TutorialId.ToString(),
                State = (int)tutorial.CurrentState,
                CurrentStepIndex = tutorial.CurrentStepIndex
            };

            // Capture all steps
            foreach (var step in tutorial.Steps)
            {
                snapshot.Steps.Add(new TutorialStepSnapshot
                {
                    StepGuid = step.StepId.ToString(),
                    State = (int)step.CurrentState,
                    ElapsedTime = step.ElapsedTime
                });
            }

            return snapshot;
        }

        private void RestoreActiveTutorial(TutorialSnapshot tutorialSnapshot)
        {
            if (!Guid.TryParse(tutorialSnapshot.TutorialGuid, out var tutorialId))
            {
                QuestLogger.LogWarning(LogSubsystem.Tutorial, $"Invalid tutorial GUID: {tutorialSnapshot.TutorialGuid}");
                return;
            }

            // Find the tutorial data
            var tutorialData = GetTutorialData(tutorialId);
            if (tutorialData == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Tutorial, $"Tutorial not found in database: {tutorialSnapshot.TutorialGuid}");
                return;
            }

            // Start the tutorial for restore (creates runtime without starting)
            var tutorial = StartTutorialForRestore(tutorialData);
            if (tutorial == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Tutorial, $"Failed to create tutorial for restore: {tutorialData.DevName}");
                return;
            }

            // Restore step states
            RestoreTutorialSteps(tutorial, tutorialSnapshot);

            // Restore tutorial state
            tutorial.RestoreTutorialState(
                (ObjectiveState)tutorialSnapshot.State,
                tutorialSnapshot.CurrentStepIndex
            );

            QuestLogger.LogVerbose(LogSubsystem.Tutorial, $"Restored active tutorial '{tutorialData.DevName}' at step {tutorialSnapshot.CurrentStepIndex}");
        }

        private void RestoreQueuedTutorial(TutorialSnapshot tutorialSnapshot)
        {
            if (!Guid.TryParse(tutorialSnapshot.TutorialGuid, out var tutorialId))
            {
                return;
            }

            var tutorialData = GetTutorialData(tutorialId);
            if (tutorialData == null)
            {
                return;
            }

            // Create runtime and add to queue (don't set as current)
            var runtime = tutorialData.GetRuntimeTutorial();
            _activeTutorials[tutorialData.TutorialId] = runtime;

            // Subscribe to events
            runtime.OnTutorialStarted.AddListener(HandleTutorialStarted);
            runtime.OnTutorialCompleted.AddListener(HandleTutorialCompleted);
            runtime.OnStepStarted.AddListener(HandleStepStarted);
            runtime.OnStepCompleted.AddListener(HandleStepCompleted);

            _tutorialQueue.Enqueue(runtime);

            QuestLogger.LogVerbose(LogSubsystem.Tutorial, $"Restored queued tutorial '{tutorialData.DevName}'");
        }

        private void RestoreTutorialSteps(TutorialRuntime tutorial, TutorialSnapshot tutorialSnapshot)
        {
            foreach (var stepSnapshot in tutorialSnapshot.Steps)
            {
                if (!Guid.TryParse(stepSnapshot.StepGuid, out var stepId))
                {
                    continue;
                }

                var step = tutorial.Steps.FirstOrDefault(s => s.StepId == stepId);
                if (step == null)
                {
                    QuestLogger.LogVerbose(LogSubsystem.Tutorial, $"Step not found: {stepSnapshot.StepGuid}");
                    continue;
                }

                // Restore step state
                step.RestoreStepState(
                    (ObjectiveState)stepSnapshot.State,
                    stepSnapshot.ElapsedTime
                );
            }
        }

        #endregion

        #region Event Handlers

        private void HandleTutorialStarted(TutorialRuntime tutorial)
        {
            OnTutorialStarted?.Invoke(tutorial);
        }

        private void HandleTutorialCompleted(TutorialRuntime tutorial)
        {
            // Mark as completed
            _completedTutorialIds.Add(tutorial.TutorialId);
            _activeTutorials.Remove(tutorial.TutorialId);

            OnTutorialCompleted?.Invoke(tutorial);

            // Unsubscribe from events
            tutorial.OnTutorialStarted.RemoveListener(HandleTutorialStarted);
            tutorial.OnTutorialCompleted.RemoveListener(HandleTutorialCompleted);
            tutorial.OnStepStarted.RemoveListener(HandleStepStarted);
            tutorial.OnStepCompleted.RemoveListener(HandleStepCompleted);

            // Start next queued tutorial
            if (_tutorialQueue.Count > 0)
            {
                _currentTutorial = _tutorialQueue.Dequeue();
                _currentTutorial.StartTutorial();
            }
            else
            {
                _currentTutorial = null;
            }
        }

        private void HandleStepStarted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            OnStepStarted?.Invoke(tutorial, step);
        }

        private void HandleStepCompleted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            OnStepCompleted?.Invoke(tutorial, step);
        }

        #endregion

        #region Editor Support

#if ODIN_INSPECTOR && UNITY_EDITOR
        [TitleGroup("Debug"), Button("Start First Tutorial"), PropertyOrder(100)]
        private void DebugStartFirstTutorial()
        {
            if (tutorialDatabase.Count > 0)
            {
                StartTutorial(tutorialDatabase[0]);
            }
        }

        [TitleGroup("Debug"), Button("Complete Current Step"), PropertyOrder(101)]
        private void DebugCompleteStep()
        {
            CompleteCurrentStep();
        }

        [TitleGroup("Debug"), Button("Skip Current Tutorial"), PropertyOrder(102)]
        private void DebugSkipTutorial()
        {
            SkipCurrentTutorial();
        }

        [TitleGroup("Debug"), Button("Reset All Progress"), PropertyOrder(103)]
        private void DebugResetProgress()
        {
            ResetAllProgress();
        }
#endif

        #endregion
    }
}
