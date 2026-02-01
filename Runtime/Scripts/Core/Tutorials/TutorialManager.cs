using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelloDev.Logging;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Tutorials.SaveLoad;
using HelloDev.QuestSystem.Utils;
using HelloDev.Saving;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = HelloDev.Logging.Logger;
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
                        Logging.Logger.LogWarning(LogSystems.Tutorial,"No TutorialManager found in scene.");
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
        #endregion

        #region Events

        /// <summary>
        /// Fired before any tutorial starts (before any step initialization).
        /// Use this to perform global setup that should happen before steps are configured.
        /// </summary>
        public UnityEvent<TutorialRuntime> OnTutorialStarting = new();

        /// <summary>
        /// Fired when any tutorial has fully started and all step initialization is complete.
        /// Use this for post-initialization tasks that depend on step state being ready.
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
        private GameContext _context;
        private bool _isInitialized;
        private TutorialSnapshotProvider _snapshotProvider;

        // Filtered subscription storage for step lifecycle hooks
        private readonly List<(Func<TutorialStepRuntime, bool> filter, Action<TutorialStepRuntime> handler)> _stepEnterSubscriptions = new();
        private readonly List<(Func<TutorialStepRuntime, bool> filter, Action<TutorialStepRuntime> handler)> _stepExitSubscriptions = new();

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
        public bool IsTutorialActive => _currentTutorial is { CurrentState: ObjectiveState.InProgress };

        /// <summary>
        /// Gets the IDs of all completed tutorials.
        /// </summary>
        public IReadOnlyCollection<Guid> CompletedTutorialIds => _completedTutorialIds;

        #endregion

        #region IBootstrapInitializable

        /// <summary>
        /// Whether this manager should self-initialize.
        /// </summary>
        public bool SelfInitialize
        {
            get => selfInitialize;
            set => selfInitialize = value;
        }

        /// <summary>
        /// Priority 105 - Core phase. Runs after QuestManager (100).
        /// </summary>


        /// <summary>
        /// Whether this manager has completed initialization.
        /// </summary>
        bool IBootstrapInitializable.IsInitialized => _isInitialized;

        /// <summary>
        /// Receives the game context from GameBootstrap.
        /// </summary>
        /// <param name="context">The game context for service registration.</param>
        public void ReceiveContext(GameContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Initializes the tutorial manager.
        /// </summary>
        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            Logging.Logger.Log(LogSystems.Tutorial, "TutorialManager starting initialization...", this);

            Initialize();

            Logging.Logger.Log(LogSystems.Tutorial, "TutorialManager initialized.", this);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Shuts down the tutorial manager.
        /// </summary>
        public void Shutdown()
        {
            // Unregister snapshot provider from unified save system
            if (_snapshotProvider != null && _context != null && _context.TryGet<UnifiedSaveManager>(out var saveManager))
            {
                saveManager.UnregisterSystem(_snapshotProvider);
                Logging.Logger.LogVerbose(LogSystems.Tutorial, "TutorialSnapshotProvider unregistered from unified save system", this);
            }
            _snapshotProvider = null;

            // Unsubscribe from all active tutorial events
            foreach (var tutorial in _activeTutorials.Values)
            {
                tutorial.OnTutorialStarting.SafeUnsubscribe(HandleTutorialStarting);
                tutorial.OnTutorialStarted.SafeUnsubscribe(HandleTutorialStarted);
                tutorial.OnTutorialCompleted.SafeUnsubscribe(HandleTutorialCompleted);
                tutorial.OnStepStarted.SafeUnsubscribe(HandleStepStarted);
                tutorial.OnStepCompleted.SafeUnsubscribe(HandleStepCompleted);
            }

            _activeTutorials.Clear();
            _tutorialQueue.Clear();
            _currentTutorial = null;
            _isInitialized = false;

            Logging.Logger.Log(LogSystems.Tutorial, "TutorialManager shutdown.", this);
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
                tutorial.OnTutorialStarted.SafeUnsubscribe(HandleTutorialStarted);
                tutorial.OnTutorialCompleted.SafeUnsubscribe(HandleTutorialCompleted);
                tutorial.OnStepStarted.SafeUnsubscribe(HandleStepStarted);
                tutorial.OnStepCompleted.SafeUnsubscribe(HandleStepCompleted);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            QuestLogger.IsLoggingEnabled = enableDebugLogging;
            Logging.Logger.Log(LogSystems.Tutorial, $"TutorialManager initialized with {tutorialDatabase.Count} tutorials.", this);

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

            if (_context != null && _context.TryGet<UnifiedSaveManager>(out var saveManager))
            {
                saveManager.RegisterSystem(_snapshotProvider);
                Logging.Logger.Log(LogSystems.Tutorial, "TutorialSnapshotProvider registered with unified save system", this);
            }
            else
            {
                Logging.Logger.LogVerbose(LogSystems.Tutorial, "No UnifiedSaveManager in context - snapshot provider created but not registered", this);
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
                Logging.Logger.Log(LogSystems.Tutorial, "Cannot start null tutorial.", this);
                return null;
            }

            // Check if already completed (for PlayOnce tutorials)
            if (tutorialData.PlayOnce && _completedTutorialIds.Contains(tutorialData.TutorialId))
            {
                Logging.Logger.Log(LogSystems.Tutorial, $"Tutorial '{tutorialData.DevName}' already completed (PlayOnce).", this);
                return null;
            }

            // Check if already active
            if (_activeTutorials.ContainsKey(tutorialData.TutorialId))
            {
                Logging.Logger.Log(LogSystems.Tutorial, $"Tutorial '{tutorialData.DevName}' is already active.", this);
                return _activeTutorials[tutorialData.TutorialId];
            }

            // Create runtime instance
            var runtime = tutorialData.GetRuntimeTutorial();
            _activeTutorials[tutorialData.TutorialId] = runtime;

            // Subscribe to events
            runtime.OnTutorialStarting.SafeSubscribe(HandleTutorialStarting);
            runtime.OnTutorialStarted.SafeSubscribe(HandleTutorialStarted);
            runtime.OnTutorialCompleted.SafeSubscribe(HandleTutorialCompleted);
            runtime.OnStepStarted.SafeSubscribe(HandleStepStarted);
            runtime.OnStepCompleted.SafeSubscribe(HandleStepCompleted);

            // Queue or start immediately
            if (allowTutorialQueue && _currentTutorial != null &&
                _currentTutorial.CurrentState == ObjectiveState.InProgress)
            {
                _tutorialQueue.Enqueue(runtime);
                Logging.Logger.Log(LogSystems.Tutorial, $"Tutorial '{tutorialData.DevName}' queued.", this);
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
                Logging.Logger.Log(LogSystems.Tutorial, $"Tutorial with ID '{tutorialId}' not found in database.", this);
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
        /// Checks if a specific tutorial is currently active (in progress or queued).
        /// Use this before calling StartTutorial to prevent duplicates after save/load.
        /// </summary>
        /// <param name="tutorialId">The tutorial ID.</param>
        /// <returns>True if the tutorial is currently active.</returns>
        public bool GetIsTutorialActive(Guid tutorialId)
        {
            return _activeTutorials.ContainsKey(tutorialId);
        }

        /// <summary>
        /// Checks if a specific tutorial is currently active (in progress or queued).
        /// Use this before calling StartTutorial to prevent duplicates after save/load.
        /// </summary>
        /// <param name="tutorialData">The tutorial ScriptableObject.</param>
        /// <returns>True if the tutorial is currently active.</returns>
        public bool GetIsTutorialActive(Tutorial_SO tutorialData)
        {
            return tutorialData != null && _activeTutorials.ContainsKey(tutorialData.TutorialId);
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

            Logging.Logger.Log(LogSystems.Tutorial, "Tutorial progress reset.", this);
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

        #region Runtime Lookup Methods

        /// <summary>
        /// Gets an active tutorial runtime by its ScriptableObject reference.
        /// </summary>
        /// <param name="tutorialData">The tutorial SO.</param>
        /// <returns>The runtime tutorial, or null if not active.</returns>
        public TutorialRuntime GetTutorialRuntime(Tutorial_SO tutorialData)
        {
            if (tutorialData == null) return null;
            return _activeTutorials.TryGetValue(tutorialData.TutorialId, out var runtime) ? runtime : null;
        }

        /// <summary>
        /// Gets an active tutorial runtime by its GUID.
        /// </summary>
        /// <param name="tutorialId">The tutorial GUID.</param>
        /// <returns>The runtime tutorial, or null if not active.</returns>
        public TutorialRuntime GetTutorialRuntime(Guid tutorialId)
        {
            return _activeTutorials.TryGetValue(tutorialId, out var runtime) ? runtime : null;
        }

        /// <summary>
        /// Gets a step runtime by its ScriptableObject reference.
        /// Searches all active tutorials for the step.
        /// </summary>
        /// <param name="stepData">The step SO.</param>
        /// <returns>The runtime step, or null if not found in any active tutorial.</returns>
        public TutorialStepRuntime GetStepRuntime(TutorialStep_SO stepData)
        {
            if (stepData == null) return null;
            return GetStepRuntime(stepData.StepId);
        }

        /// <summary>
        /// Gets a step runtime by its GUID.
        /// Searches all active tutorials for the step.
        /// </summary>
        /// <param name="stepId">The step GUID.</param>
        /// <returns>The runtime step, or null if not found in any active tutorial.</returns>
        public TutorialStepRuntime GetStepRuntime(Guid stepId)
        {
            foreach (var tutorial in _activeTutorials.Values)
            {
                var step = tutorial.Steps.FirstOrDefault(s => s.StepId == stepId);
                if (step != null) return step;
            }
            return null;
        }

        /// <summary>
        /// Gets a step runtime from a specific tutorial by step SO.
        /// </summary>
        /// <param name="tutorialData">The tutorial SO.</param>
        /// <param name="stepData">The step SO.</param>
        /// <returns>The runtime step, or null if not found.</returns>
        public TutorialStepRuntime GetStepRuntime(Tutorial_SO tutorialData, TutorialStep_SO stepData)
        {
            if (tutorialData == null || stepData == null) return null;
            var tutorial = GetTutorialRuntime(tutorialData);
            return tutorial?.Steps.FirstOrDefault(s => s.StepId == stepData.StepId);
        }

        /// <summary>
        /// Tries to get the current step of the active tutorial.
        /// </summary>
        /// <returns>The current step runtime, or null if no tutorial is active.</returns>
        public TutorialStepRuntime GetCurrentStep()
        {
            return _currentTutorial?.CurrentStep;
        }

        #endregion

        #region Step Lifecycle Subscriptions

        /// <summary>
        /// Subscribes to step enter events with a filter.
        /// Handler is called when a step starts and matches the filter.
        /// </summary>
        /// <param name="filter">Predicate to filter which steps trigger the handler.</param>
        /// <param name="handler">Action to execute when a matching step starts.</param>
        public void SubscribeToStepEnter(Func<TutorialStepRuntime, bool> filter, Action<TutorialStepRuntime> handler)
        {
            if (filter == null || handler == null) return;
            _stepEnterSubscriptions.Add((filter, handler));
        }

        /// <summary>
        /// Subscribes to step exit events with a filter.
        /// Handler is called when a step completes/fails/skips and matches the filter.
        /// </summary>
        /// <param name="filter">Predicate to filter which steps trigger the handler.</param>
        /// <param name="handler">Action to execute when a matching step exits.</param>
        public void SubscribeToStepExit(Func<TutorialStepRuntime, bool> filter, Action<TutorialStepRuntime> handler)
        {
            if (filter == null || handler == null) return;
            _stepExitSubscriptions.Add((filter, handler));
        }

        /// <summary>
        /// Unsubscribes a handler from step enter events.
        /// </summary>
        /// <param name="handler">The handler to remove.</param>
        public void UnsubscribeFromStepEnter(Action<TutorialStepRuntime> handler)
        {
            if (handler == null) return;
            _stepEnterSubscriptions.RemoveAll(sub => sub.handler == handler);
        }

        /// <summary>
        /// Unsubscribes a handler from step exit events.
        /// </summary>
        /// <param name="handler">The handler to remove.</param>
        public void UnsubscribeFromStepExit(Action<TutorialStepRuntime> handler)
        {
            if (handler == null) return;
            _stepExitSubscriptions.RemoveAll(sub => sub.handler == handler);
        }

        /// <summary>
        /// Clears all step lifecycle subscriptions.
        /// </summary>
        public void ClearStepSubscriptions()
        {
            _stepEnterSubscriptions.Clear();
            _stepExitSubscriptions.Clear();
        }

        /// <summary>
        /// Invokes all matching enter subscriptions for a step.
        /// </summary>
        private void InvokeStepEnterSubscriptions(TutorialStepRuntime step)
        {
            foreach (var (filter, handler) in _stepEnterSubscriptions)
            {
                try
                {
                    if (filter(step))
                    {
                        handler(step);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logger.LogError(LogSystems.Tutorial, $"Error in step enter subscription: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Invokes all matching exit subscriptions for a step.
        /// </summary>
        private void InvokeStepExitSubscriptions(TutorialStepRuntime step)
        {
            foreach (var (filter, handler) in _stepExitSubscriptions)
            {
                try
                {
                    if (filter(step))
                    {
                        handler(step);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logger.LogError(LogSystems.Tutorial, $"Error in step exit subscription: {ex.Message}", this);
                }
            }
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

            Logging.Logger.Log(LogSystems.Tutorial, $"Restored {_completedTutorialIds.Count} completed tutorial IDs.", this);
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
            runtime.OnTutorialStarting.SafeSubscribe(HandleTutorialStarting);
            runtime.OnTutorialStarted.SafeSubscribe(HandleTutorialStarted);
            runtime.OnTutorialCompleted.SafeSubscribe(HandleTutorialCompleted);
            runtime.OnStepStarted.SafeSubscribe(HandleStepStarted);
            runtime.OnStepCompleted.SafeSubscribe(HandleStepCompleted);

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

            Logging.Logger.Log(LogSystems.Tutorial, $"Captured tutorial snapshot: {snapshot.CompletedTutorialIds.Count} completed" +
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
                Logging.Logger.LogWarning(LogSystems.Tutorial,"Cannot restore null snapshot.", this);
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
            Logger.Log(LogSystems.Tutorial, $"Restored tutorial snapshot: {snapshot.CompletedTutorialIds?.Count ?? 0} completed" +
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
                var stepSnapshot = new TutorialStepSnapshot
                {
                    StepGuid = step.StepId.ToString(),
                    State = (int)step.CurrentState,
                    ElapsedTime = step.ElapsedTime,
                    CurrentCount = step.CurrentCount,
                };

                var completedSubstepIds = step.GetCompletedSubstepIds();
                if (completedSubstepIds != null && completedSubstepIds.Count > 0)
                {
                    foreach (var substepId in completedSubstepIds)
                    {
                        stepSnapshot.CompletedSubstepIds.Add(substepId.ToString());
                    }
                }

                snapshot.Steps.Add(stepSnapshot);
            }

            return snapshot;
        }

        private void RestoreActiveTutorial(TutorialSnapshot tutorialSnapshot)
        {
            if (!Guid.TryParse(tutorialSnapshot.TutorialGuid, out var tutorialId))
            {
                Logging.Logger.LogWarning(LogSystems.Tutorial, $"Invalid tutorial GUID: {tutorialSnapshot.TutorialGuid}", this);
                return;
            }

            // Find the tutorial data
            var tutorialData = GetTutorialData(tutorialId);
            if (tutorialData == null)
            {
                Logging.Logger.LogWarning(LogSystems.Tutorial, $"Tutorial not found in database: {tutorialSnapshot.TutorialGuid}", this);
                return;
            }

            // Start the tutorial for restore (creates runtime without starting)
            var tutorial = StartTutorialForRestore(tutorialData);
            if (tutorial == null)
            {
                Logging.Logger.LogWarning(LogSystems.Tutorial, $"Failed to create tutorial for restore: {tutorialData.DevName}", this);
                return;
            }

            // Restore step states
            RestoreTutorialSteps(tutorial, tutorialSnapshot);

            // Restore tutorial state and fire events so UI can display current state
            tutorial.RestoreTutorialState(
                (ObjectiveState)tutorialSnapshot.State,
                tutorialSnapshot.CurrentStepIndex,
                fireEvents: true
            );

            Logging.Logger.LogVerbose(LogSystems.Tutorial, $"Restored active tutorial '{tutorialData.DevName}' at step {tutorialSnapshot.CurrentStepIndex}", this);
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
            runtime.OnTutorialStarting.SafeSubscribe(HandleTutorialStarting);
            runtime.OnTutorialStarted.SafeSubscribe(HandleTutorialStarted);
            runtime.OnTutorialCompleted.SafeSubscribe(HandleTutorialCompleted);
            runtime.OnStepStarted.SafeSubscribe(HandleStepStarted);
            runtime.OnStepCompleted.SafeSubscribe(HandleStepCompleted);

            _tutorialQueue.Enqueue(runtime);

            Logging.Logger.LogVerbose(LogSystems.Tutorial, $"Restored queued tutorial '{tutorialData.DevName}'", this);
        }

        private void RestoreTutorialSteps(TutorialRuntime tutorial, TutorialSnapshot snapshot)
        {
            Logger.Log(LogSystems.Tutorial, $"Restoring {snapshot.Steps.Count} steps from snapshot");

            foreach (var stepSnapshot in snapshot.Steps)
            {
                var stepId = Guid.Parse(stepSnapshot.StepGuid);
                var step = tutorial.Steps.FirstOrDefault(s => s.StepId == stepId);

                if (step == null)
                {
                    Logger.LogWarning(LogSystems.Tutorial, $"Could not find step with GUID {stepId} in tutorial");
                    continue;
                }

                Logger.Log(LogSystems.Tutorial, $"Processing snapshot for step GUID: {stepSnapshot.StepGuid}, State: {stepSnapshot.State}");

                var state = (ObjectiveState)stepSnapshot.State;

                Logger.Log(LogSystems.Tutorial, $"Restoring step '{step.DevName}' (GUID: {stepId}) to state {stepSnapshot.State}");

                var completedSubstepIds = stepSnapshot.CompletedSubstepIds?
                    .Select(Guid.Parse)
                    .ToList();

                step.RestoreStepState(
                    state, 
                    stepSnapshot.ElapsedTime,
                    stepSnapshot.CurrentCount,   
                    completedSubstepIds
                );

                Logger.Log(LogSystems.Tutorial, $"Restored step '{step.DevName}' to state {stepSnapshot.State}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleTutorialStarting(TutorialRuntime tutorial)
        {
            OnTutorialStarting?.Invoke(tutorial);
        }

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
            tutorial.OnTutorialStarting.SafeUnsubscribe(HandleTutorialStarting);
            tutorial.OnTutorialStarted.SafeUnsubscribe(HandleTutorialStarted);
            tutorial.OnTutorialCompleted.SafeUnsubscribe(HandleTutorialCompleted);
            tutorial.OnStepStarted.SafeUnsubscribe(HandleStepStarted);
            tutorial.OnStepCompleted.SafeUnsubscribe(HandleStepCompleted);

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
            // Note: Filtered subscriptions are invoked via NotifyStepEntering() before this event fires
            OnStepStarted?.Invoke(tutorial, step);
        }

        private void HandleStepCompleted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            // Note: Filtered subscriptions are invoked via NotifyStepExiting() before this event fires
            OnStepCompleted?.Invoke(tutorial, step);
        }

        #endregion

        #region Internal Step Notifications

        /// <summary>
        /// Called by TutorialStepRuntime when a step is about to enter (before state change).
        /// Invokes filtered enter subscriptions while step is still NotStarted.
        /// </summary>
        internal void NotifyStepEntering(TutorialStepRuntime step)
        {
            InvokeStepEnterSubscriptions(step);
        }

        /// <summary>
        /// Called by TutorialStepRuntime when a step is about to exit (before state change).
        /// Invokes filtered exit subscriptions while step is still InProgress.
        /// </summary>
        internal void NotifyStepExiting(TutorialStepRuntime step)
        {
            InvokeStepExitSubscriptions(step);
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
