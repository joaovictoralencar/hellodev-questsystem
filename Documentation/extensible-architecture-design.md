# Extensible Architecture Design: Objective System

*Created: 2026-01-14*
*Updated: 2026-01-18*
*Status: COMPLETE*

## Executive Summary

This document defines how to extend the Quest System to support **any objective-based experience** (tutorials, achievements, challenges) by adding an **interface layer above existing code**. No renaming, no breaking changes - just new interfaces that existing classes implement.

**Core Strategy:** Quest, Task, Stage become *implementations* of generic Objective interfaces.

---

## Design Principles (from 5 Architectural Tips)

| Principle | Application |
|-----------|-------------|
| **1. Start with Interfaces** | Define IObjective, IObjectiveGroup, IMission first |
| **2. Separate Logic from MonoBehaviors** | Interfaces are pure C#, no Unity dependencies |
| **3. Separate Data from Logic** | ScriptableObjects for config, Runtime classes for state |
| **4. Event-Driven Architecture** | All interfaces expose events, systems react |
| **5. Registry Pattern** | Each manager tracks its own type (QuestManager, TutorialManager, etc.) |

---

## Architecture Overview

### Interface Hierarchy

```
IMission (ordered stages with transitions)
│   └── Implementations: QuestRuntime, TutorialRuntime
│
├── IStage (phase with objective groups)
│   │   └── Implementations: QuestStageRuntime, TutorialStepRuntime
│   │
│   └── IObjectiveGroup (collection of objectives)
│       │   └── Implementations: TaskGroupRuntime, AchievementRuntime
│       │
│       └── IObjective (single trackable goal)
│               └── Implementations: TaskRuntime (and all task types)
```

### Mapping: Existing Classes → New Interfaces

| Existing Class | Implements | Notes |
|----------------|------------|-------|
| `TaskRuntime` | `IObjective` | No changes to class, just implements interface |
| `TaskGroupRuntime` | `IObjectiveGroup` | No changes to class |
| `QuestStageRuntime` | `IStage` | No changes to class |
| `QuestRuntime` | `IMission` | No changes to class |

**Key Insight:** Existing code stays exactly the same. We're adding contracts, not changing implementations.

---

## Interface Definitions

### IObjective

The fundamental unit - something the player must accomplish.

```csharp
namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents a single trackable objective.
    /// Implemented by: TaskRuntime (and all task subtypes)
    /// </summary>
    public interface IObjective
    {
        // Identity
        string Id { get; }

        // State
        ObjectiveState State { get; }
        float Progress { get; } // 0.0 to 1.0
        bool IsComplete { get; }
        bool IsFailed { get; }

        // Lifecycle
        void Start();
        void Complete();
        void Fail();
        void Reset();

        // Events (Principle 4: Event-Driven)
        event Action<IObjective> OnStarted;
        event Action<IObjective> OnProgressChanged;
        event Action<IObjective> OnCompleted;
        event Action<IObjective> OnFailed;
    }

    public enum ObjectiveState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }
}
```

### IObjectiveGroup

A collection of objectives with execution rules.

```csharp
namespace HelloDev.Objectives
{
    /// <summary>
    /// A group of objectives with execution mode logic.
    /// Implemented by: TaskGroupRuntime, AchievementRuntime
    /// </summary>
    public interface IObjectiveGroup
    {
        // Identity
        string Id { get; }

        // State
        ObjectiveState State { get; }
        float Progress { get; }

        // Objectives
        IReadOnlyList<IObjective> Objectives { get; }
        ObjectiveExecutionMode ExecutionMode { get; }
        int RequiredCount { get; }  // For OptionalXOfY mode
        int CompletedCount { get; }

        // Events
        event Action<IObjectiveGroup> OnStarted;
        event Action<IObjectiveGroup> OnProgressChanged;
        event Action<IObjectiveGroup> OnCompleted;
        event Action<IObjectiveGroup> OnFailed;
        event Action<IObjectiveGroup, IObjective> OnObjectiveCompleted;
    }

    public enum ObjectiveExecutionMode
    {
        Sequential,    // One at a time, in order
        Parallel,      // All active at once
        AnyOrder,      // One at a time, player chooses
        OptionalXOfY   // Complete X of Y objectives
    }
}
```

### IStage

A phase within a mission, containing objective groups.

```csharp
namespace HelloDev.Objectives
{
    /// <summary>
    /// A stage/phase within a mission.
    /// Implemented by: QuestStageRuntime, TutorialStepRuntime
    /// </summary>
    public interface IStage
    {
        // Identity
        int Index { get; }
        string Id { get; }

        // State
        ObjectiveState State { get; }
        float Progress { get; }

        // Content (optional - tutorials may have empty groups)
        IReadOnlyList<IObjectiveGroup> ObjectiveGroups { get; }

        // Stage properties
        bool IsTerminal { get; }
        bool IsOptional { get; }
        bool IsHidden { get; }

        // Events
        event Action<IStage> OnEntered;
        event Action<IStage> OnProgressChanged;
        event Action<IStage> OnCompleted;
        event Action<IStage> OnFailed;
        event Action<IStage> OnExited;
    }
}
```

### IMission

An ordered collection of stages with transitions. The top-level container.

```csharp
namespace HelloDev.Objectives
{
    /// <summary>
    /// A mission is a sequence of stages (quest, tutorial, challenge).
    /// Implemented by: QuestRuntime, TutorialRuntime
    /// </summary>
    public interface IMission
    {
        // Identity
        Guid MissionId { get; }
        string DisplayName { get; }

        // State
        ObjectiveState State { get; }
        float Progress { get; }

        // Stages
        IReadOnlyList<IStage> Stages { get; }
        IStage CurrentStage { get; }
        int CurrentStageIndex { get; }

        // Lifecycle
        void Start();
        void Complete();
        void Fail();
        void Reset();

        // Events
        event Action<IMission> OnStarted;
        event Action<IMission> OnProgressChanged;
        event Action<IMission> OnCompleted;
        event Action<IMission> OnFailed;
        event Action<IMission, IStage> OnStageEntered;
        event Action<IMission, IStage> OnStageCompleted;
    }
}
```

---

## Implementation Plan

### Phase 1: Create Interfaces (Non-Breaking)

**Goal:** Add interface files without touching existing code.

**Files to Create:**
```
Runtime/Scripts/Core/Abstractions/
├── IObjective.cs
├── IObjectiveGroup.cs
├── IStage.cs
├── IMission.cs
└── ObjectiveEnums.cs  (ObjectiveState, ObjectiveExecutionMode)
```

**Validation:** Project compiles. Existing functionality unchanged.

### Phase 2: Implement Interfaces on Existing Classes

**Goal:** Make existing classes implement new interfaces.

**Changes:**

```csharp
// TaskRuntime.cs - ADD interface, no other changes
public abstract class TaskRuntime : IObjective
{
    // Existing code stays exactly the same

    // IObjective explicit implementation (maps existing members)
    string IObjective.Id => Data.taskId;
    ObjectiveState IObjective.State => MapTaskStateToObjectiveState(State);
    float IObjective.Progress => Progress;
    bool IObjective.IsComplete => State == TaskState.Completed;
    bool IObjective.IsFailed => State == TaskState.Failed;

    // Map existing events to interface events
    event Action<IObjective> IObjective.OnStarted
    {
        add => OnTaskStarted.AddListener(_ => value?.Invoke(this));
        remove { }
    }
    // ... similar for other events
}

// TaskGroupRuntime.cs
public class TaskGroupRuntime : IObjectiveGroup
{
    // Existing code unchanged

    // IObjectiveGroup explicit implementation
    string IObjectiveGroup.Id => Data.groupId;
    IReadOnlyList<IObjective> IObjectiveGroup.Objectives => Tasks.Cast<IObjective>().ToList();
    ObjectiveExecutionMode IObjectiveGroup.ExecutionMode => MapExecutionMode(Data.executionMode);
    // ... etc
}

// QuestStageRuntime.cs
public class QuestStageRuntime : IStage
{
    // Existing code unchanged

    // IStage explicit implementation
    string IStage.Id => Data.stageId;
    IReadOnlyList<IObjectiveGroup> IStage.ObjectiveGroups => TaskGroups.Cast<IObjectiveGroup>().ToList();
    // ... etc
}

// QuestRuntime.cs
public class QuestRuntime : IQuest, IMission
{
    // Existing code unchanged

    // IMission explicit implementation
    Guid IMission.MissionId => QuestId;
    string IMission.DisplayName => Data.displayName.GetLocalizedString();
    IReadOnlyList<IStage> IMission.Stages => StageRuntimes.Cast<IStage>().ToList();
    IStage IMission.CurrentStage => CurrentStage;
    // ... etc
}
```

**Key Points:**
- Use explicit interface implementation where names differ
- Map existing events to interface events
- No breaking changes to existing API

**Validation:**
- Project compiles
- All existing tests pass
- Existing quests work identically

### Phase 3: Tutorial System Implementation

**Goal:** Create a simple but functional tutorial system using the interfaces.

#### 3.1 Tutorial Data (Principle 3: Separate Data)

```csharp
namespace HelloDev.Tutorials
{
    [CreateAssetMenu(menuName = "HelloDev/Tutorials/Tutorial")]
    public class Tutorial_SO : ScriptableObject
    {
        [Header("Identity")]
        public string tutorialId;
        public LocalizedString displayName;

        [Header("Steps")]
        public List<TutorialStep_SO> steps;

        [Header("Behavior")]
        public bool canSkip = true;
        public bool pausesDuringSteps = false;

        public TutorialRuntime CreateRuntime() => new TutorialRuntime(this);
    }

    [CreateAssetMenu(menuName = "HelloDev/Tutorials/Step")]
    public class TutorialStep_SO : ScriptableObject
    {
        [Header("Identity")]
        public string stepId;
        public LocalizedString instruction;

        [Header("Completion")]
        public Condition_SO completionCondition;  // Reuse existing condition system!

        [Header("UI Hints (Optional)")]
        public string highlightElementId;
        public Vector2 tooltipPosition;
    }
}
```

#### 3.2 Tutorial Runtime (Principle 2: Separate Logic)

```csharp
namespace HelloDev.Tutorials
{
    /// <summary>
    /// Runtime tutorial instance. Implements IMission.
    /// Logic separated from MonoBehavior (Principle 2).
    /// </summary>
    public class TutorialRuntime : IMission
    {
        private readonly Tutorial_SO _data;
        private readonly List<TutorialStepRuntime> _steps;
        private int _currentStepIndex = -1;
        private ObjectiveState _state = ObjectiveState.NotStarted;

        // IMission Implementation
        public Guid MissionId { get; } = Guid.NewGuid();
        public string DisplayName => _data.displayName.GetLocalizedString();
        public ObjectiveState State => _state;
        public float Progress => CalculateProgress();

        public IReadOnlyList<IStage> Stages => _steps.Cast<IStage>().ToList();
        public IStage CurrentStage => _currentStepIndex >= 0 && _currentStepIndex < _steps.Count
            ? _steps[_currentStepIndex]
            : null;
        public int CurrentStageIndex => _currentStepIndex;

        // Events
        public event Action<IMission> OnStarted;
        public event Action<IMission> OnProgressChanged;
        public event Action<IMission> OnCompleted;
        public event Action<IMission> OnFailed;
        public event Action<IMission, IStage> OnStageEntered;
        public event Action<IMission, IStage> OnStageCompleted;

        // Tutorial-specific
        public Tutorial_SO Data => _data;
        public bool CanSkip => _data.canSkip;
        public TutorialStepRuntime CurrentStep => CurrentStage as TutorialStepRuntime;

        public TutorialRuntime(Tutorial_SO data)
        {
            _data = data;
            _steps = new List<TutorialStepRuntime>();
            for (int i = 0; i < data.steps.Count; i++)
            {
                _steps.Add(new TutorialStepRuntime(data.steps[i], this, i));
            }
        }

        public void Start()
        {
            if (_state != ObjectiveState.NotStarted) return;

            _state = ObjectiveState.InProgress;
            OnStarted?.Invoke(this);

            // Enter first step
            if (_steps.Count > 0)
            {
                _currentStepIndex = 0;
                _steps[0].Enter();
                OnStageEntered?.Invoke(this, _steps[0]);
            }
            else
            {
                Complete(); // Empty tutorial completes immediately
            }
        }

        internal void AdvanceToNextStep()
        {
            if (_currentStepIndex < 0 || _state != ObjectiveState.InProgress) return;

            var currentStep = _steps[_currentStepIndex];
            currentStep.Exit();
            OnStageCompleted?.Invoke(this, currentStep);

            _currentStepIndex++;

            if (_currentStepIndex >= _steps.Count)
            {
                Complete();
            }
            else
            {
                _steps[_currentStepIndex].Enter();
                OnStageEntered?.Invoke(this, _steps[_currentStepIndex]);
                OnProgressChanged?.Invoke(this);
            }
        }

        public void Skip()
        {
            if (!CanSkip || _state != ObjectiveState.InProgress) return;

            // Exit current step if any
            if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
            {
                _steps[_currentStepIndex].Exit();
            }

            Complete();
        }

        public void Complete()
        {
            if (_state == ObjectiveState.Completed) return;

            _state = ObjectiveState.Completed;
            OnCompleted?.Invoke(this);
        }

        public void Fail()
        {
            // Tutorials typically don't fail, but interface requires it
            if (_state == ObjectiveState.Failed) return;
            _state = ObjectiveState.Failed;
            OnFailed?.Invoke(this);
        }

        public void Reset()
        {
            if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
            {
                _steps[_currentStepIndex].Exit();
            }

            _state = ObjectiveState.NotStarted;
            _currentStepIndex = -1;

            foreach (var step in _steps)
            {
                step.Reset();
            }
        }

        private float CalculateProgress()
        {
            if (_steps.Count == 0) return 1f;
            if (_state == ObjectiveState.Completed) return 1f;
            return (float)Math.Max(0, _currentStepIndex) / _steps.Count;
        }
    }
}
```

#### 3.3 Tutorial Step Runtime

```csharp
namespace HelloDev.Tutorials
{
    public class TutorialStepRuntime : IStage
    {
        private readonly TutorialStep_SO _data;
        private readonly TutorialRuntime _tutorial;
        private ObjectiveState _state = ObjectiveState.NotStarted;

        // IStage Implementation
        public int Index { get; }
        public string Id => _data.stepId;
        public ObjectiveState State => _state;
        public float Progress => _state == ObjectiveState.Completed ? 1f : 0f;
        public IReadOnlyList<IObjectiveGroup> ObjectiveGroups => Array.Empty<IObjectiveGroup>();
        public bool IsTerminal => Index == _tutorial.Stages.Count - 1;
        public bool IsOptional => false;
        public bool IsHidden => false;

        // Events
        public event Action<IStage> OnEntered;
        public event Action<IStage> OnProgressChanged;
        public event Action<IStage> OnCompleted;
        public event Action<IStage> OnFailed;
        public event Action<IStage> OnExited;

        // Tutorial-specific
        public TutorialStep_SO Data => _data;
        public string HighlightElementId => _data.highlightElementId;
        public string Instruction => _data.instruction.GetLocalizedString();
        public Vector2 TooltipPosition => _data.tooltipPosition;

        public TutorialStepRuntime(TutorialStep_SO data, TutorialRuntime tutorial, int index)
        {
            _data = data;
            _tutorial = tutorial;
            Index = index;
        }

        public void Enter()
        {
            _state = ObjectiveState.InProgress;
            OnEntered?.Invoke(this);

            // Subscribe to completion condition
            if (_data.completionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.Subscribe();
                eventCondition.OnConditionMet += HandleConditionMet;
            }
            else if (_data.completionCondition != null)
            {
                // Poll-based condition - check immediately
                if (_data.completionCondition.Evaluate())
                {
                    HandleConditionMet();
                }
            }
            else
            {
                // No condition - auto-complete (useful for "intro" steps)
                HandleConditionMet();
            }
        }

        public void Exit()
        {
            // Unsubscribe from condition
            if (_data.completionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.OnConditionMet -= HandleConditionMet;
                eventCondition.Unsubscribe();
            }

            OnExited?.Invoke(this);
        }

        public void Reset()
        {
            _state = ObjectiveState.NotStarted;
        }

        private void HandleConditionMet()
        {
            if (_state != ObjectiveState.InProgress) return;

            _state = ObjectiveState.Completed;
            OnCompleted?.Invoke(this);
            _tutorial.AdvanceToNextStep();
        }
    }
}
```

#### 3.4 Tutorial Manager

```csharp
namespace HelloDev.Tutorials
{
    /// <summary>
    /// Manages tutorial lifecycle. Tracks active and completed tutorials.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] private bool persistCompletion = true;

        private readonly Dictionary<string, TutorialRuntime> _activeTutorials = new();
        private readonly HashSet<string> _completedTutorialIds = new();

        // Events for UI
        public event Action<TutorialRuntime> OnTutorialStarted;
        public event Action<TutorialRuntime> OnTutorialCompleted;
        public event Action<TutorialRuntime, TutorialStepRuntime> OnStepEntered;
        public event Action<TutorialRuntime, TutorialStepRuntime> OnStepCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public TutorialRuntime StartTutorial(Tutorial_SO data)
        {
            if (data == null) return null;
            if (IsCompleted(data.tutorialId)) return null;
            if (_activeTutorials.ContainsKey(data.tutorialId)) return _activeTutorials[data.tutorialId];

            var runtime = data.CreateRuntime();

            // Subscribe to events
            runtime.OnCompleted += HandleTutorialCompleted;
            runtime.OnStageEntered += HandleStepEntered;
            runtime.OnStageCompleted += HandleStepCompleted;

            _activeTutorials[data.tutorialId] = runtime;
            runtime.Start();

            OnTutorialStarted?.Invoke(runtime);
            return runtime;
        }

        public void SkipTutorial(string tutorialId)
        {
            if (_activeTutorials.TryGetValue(tutorialId, out var runtime))
            {
                runtime.Skip();
            }
        }

        public bool IsCompleted(string tutorialId) => _completedTutorialIds.Contains(tutorialId);
        public bool IsActive(string tutorialId) => _activeTutorials.ContainsKey(tutorialId);

        public TutorialRuntime GetActiveTutorial(string tutorialId)
        {
            return _activeTutorials.TryGetValue(tutorialId, out var runtime) ? runtime : null;
        }

        public IEnumerable<TutorialRuntime> GetAllActiveTutorials() => _activeTutorials.Values;

        private void HandleTutorialCompleted(IMission mission)
        {
            if (mission is TutorialRuntime tutorial)
            {
                _completedTutorialIds.Add(tutorial.Data.tutorialId);
                _activeTutorials.Remove(tutorial.Data.tutorialId);

                // Unsubscribe
                tutorial.OnCompleted -= HandleTutorialCompleted;
                tutorial.OnStageEntered -= HandleStepEntered;
                tutorial.OnStageCompleted -= HandleStepCompleted;

                OnTutorialCompleted?.Invoke(tutorial);
            }
        }

        private void HandleStepEntered(IMission mission, IStage stage)
        {
            if (mission is TutorialRuntime tutorial && stage is TutorialStepRuntime step)
            {
                OnStepEntered?.Invoke(tutorial, step);
            }
        }

        private void HandleStepCompleted(IMission mission, IStage stage)
        {
            if (mission is TutorialRuntime tutorial && stage is TutorialStepRuntime step)
            {
                OnStepCompleted?.Invoke(tutorial, step);
            }
        }

        // Save/Load support
        public HashSet<string> GetCompletedTutorialIds() => new(_completedTutorialIds);

        public void SetCompletedTutorialIds(IEnumerable<string> ids)
        {
            _completedTutorialIds.Clear();
            foreach (var id in ids)
            {
                _completedTutorialIds.Add(id);
            }
        }
    }
}
```

**Validation:** Tutorial system works. Steps complete via conditions.

### Phase 4: Achievement System Implementation

**Goal:** Create a simple but functional achievement system.

#### 4.1 Achievement Data

```csharp
namespace HelloDev.Achievements
{
    [CreateAssetMenu(menuName = "HelloDev/Achievements/Achievement")]
    public class Achievement_SO : ScriptableObject
    {
        [Header("Identity")]
        public string achievementId;
        public LocalizedString displayName;
        public LocalizedString description;
        public Sprite icon;

        [Header("Tracking")]
        public Condition_SO completionCondition;  // Event-driven condition
        public int targetValue = 1;  // For counter-based (kill 100 enemies)

        [Header("Rewards")]
        public int points;

        [Header("Display")]
        public bool isSecret;

        public AchievementRuntime CreateRuntime() => new AchievementRuntime(this);
    }
}
```

#### 4.2 Achievement Runtime

```csharp
namespace HelloDev.Achievements
{
    /// <summary>
    /// Runtime achievement. Implements IObjectiveGroup and IObjective.
    /// Achievements are single-objective groups that track progress.
    /// </summary>
    public class AchievementRuntime : IObjectiveGroup, IObjective
    {
        private readonly Achievement_SO _data;
        private ObjectiveState _state = ObjectiveState.NotStarted;
        private int _currentValue;
        private DateTime? _unlockedAt;

        // IObjective Implementation
        public string Id => _data.achievementId;
        public ObjectiveState State => _state;
        public float Progress => _data.targetValue > 0 ? (float)_currentValue / _data.targetValue : 0f;
        public bool IsComplete => _state == ObjectiveState.Completed;
        public bool IsFailed => _state == ObjectiveState.Failed;

        // IObjectiveGroup Implementation
        public IReadOnlyList<IObjective> Objectives => new IObjective[] { this };
        public ObjectiveExecutionMode ExecutionMode => ObjectiveExecutionMode.Parallel;
        public int RequiredCount => 1;
        public int CompletedCount => IsComplete ? 1 : 0;

        // IObjective Events
        public event Action<IObjective> OnStarted;
        public event Action<IObjective> OnProgressChanged;
        public event Action<IObjective> OnCompleted;
        public event Action<IObjective> OnFailed;

        // IObjectiveGroup Events (delegate to IObjective events)
        event Action<IObjectiveGroup> IObjectiveGroup.OnStarted
        {
            add => OnStarted += _ => value?.Invoke(this);
            remove { }
        }
        event Action<IObjectiveGroup> IObjectiveGroup.OnProgressChanged
        {
            add => OnProgressChanged += _ => value?.Invoke(this);
            remove { }
        }
        event Action<IObjectiveGroup> IObjectiveGroup.OnCompleted
        {
            add => OnCompleted += _ => value?.Invoke(this);
            remove { }
        }
        event Action<IObjectiveGroup> IObjectiveGroup.OnFailed
        {
            add => OnFailed += _ => value?.Invoke(this);
            remove { }
        }
        public event Action<IObjectiveGroup, IObjective> OnObjectiveCompleted;

        // Achievement-specific
        public Achievement_SO Data => _data;
        public bool IsSecret => _data.isSecret;
        public bool IsUnlocked => _state == ObjectiveState.Completed;
        public DateTime? UnlockedAt => _unlockedAt;
        public int Points => _data.points;
        public int CurrentValue => _currentValue;
        public int TargetValue => _data.targetValue;

        public AchievementRuntime(Achievement_SO data)
        {
            _data = data;
        }

        public void Start()
        {
            if (_state != ObjectiveState.NotStarted) return;

            _state = ObjectiveState.InProgress;
            OnStarted?.Invoke(this);

            // Subscribe to event-driven condition
            if (_data.completionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.Subscribe();
                eventCondition.OnConditionMet += HandleConditionMet;
            }
        }

        public void IncrementProgress(int amount = 1)
        {
            if (_state != ObjectiveState.InProgress) return;

            int oldValue = _currentValue;
            _currentValue = Math.Min(_currentValue + amount, _data.targetValue);

            if (_currentValue != oldValue)
            {
                OnProgressChanged?.Invoke(this);

                if (_currentValue >= _data.targetValue)
                {
                    Complete();
                }
            }
        }

        public void SetProgress(int value)
        {
            if (_state != ObjectiveState.InProgress) return;

            int oldValue = _currentValue;
            _currentValue = Math.Clamp(value, 0, _data.targetValue);

            if (_currentValue != oldValue)
            {
                OnProgressChanged?.Invoke(this);

                if (_currentValue >= _data.targetValue)
                {
                    Complete();
                }
            }
        }

        public void Complete()
        {
            if (_state == ObjectiveState.Completed) return;

            // Unsubscribe from condition
            if (_data.completionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.OnConditionMet -= HandleConditionMet;
                eventCondition.Unsubscribe();
            }

            _state = ObjectiveState.Completed;
            _currentValue = _data.targetValue;
            _unlockedAt = DateTime.Now;

            OnCompleted?.Invoke(this);
            OnObjectiveCompleted?.Invoke(this, this);
        }

        public void Fail()
        {
            // Achievements typically don't fail
        }

        public void Reset()
        {
            if (_data.completionCondition is IConditionEventDriven eventCondition)
            {
                eventCondition.OnConditionMet -= HandleConditionMet;
                eventCondition.Unsubscribe();
            }

            _state = ObjectiveState.NotStarted;
            _currentValue = 0;
            _unlockedAt = null;
        }

        private void HandleConditionMet()
        {
            IncrementProgress();
        }
    }
}
```

#### 4.3 Achievement Manager

```csharp
namespace HelloDev.Achievements
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [SerializeField] private List<Achievement_SO> allAchievements;

        private readonly Dictionary<string, AchievementRuntime> _runtimes = new();

        // Events for UI
        public event Action<AchievementRuntime> OnAchievementUnlocked;
        public event Action<AchievementRuntime> OnAchievementProgress;

        public int TotalPoints => _runtimes.Values.Where(a => a.IsUnlocked).Sum(a => a.Points);
        public int UnlockedCount => _runtimes.Values.Count(a => a.IsUnlocked);
        public int TotalCount => _runtimes.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeAchievements();
        }

        private void InitializeAchievements()
        {
            foreach (var data in allAchievements)
            {
                if (data == null) continue;
                if (_runtimes.ContainsKey(data.achievementId)) continue;

                var runtime = data.CreateRuntime();
                runtime.OnCompleted += HandleAchievementCompleted;
                runtime.OnProgressChanged += HandleAchievementProgress;
                _runtimes[data.achievementId] = runtime;
                runtime.Start();
            }
        }

        public AchievementRuntime GetAchievement(string id)
        {
            return _runtimes.TryGetValue(id, out var runtime) ? runtime : null;
        }

        public IEnumerable<AchievementRuntime> GetAllAchievements() => _runtimes.Values;

        public IEnumerable<AchievementRuntime> GetUnlockedAchievements() =>
            _runtimes.Values.Where(a => a.IsUnlocked);

        public IEnumerable<AchievementRuntime> GetLockedAchievements() =>
            _runtimes.Values.Where(a => !a.IsUnlocked);

        public IEnumerable<AchievementRuntime> GetVisibleAchievements() =>
            _runtimes.Values.Where(a => !a.IsSecret || a.IsUnlocked);

        private void HandleAchievementCompleted(IObjective objective)
        {
            if (objective is AchievementRuntime achievement)
            {
                OnAchievementUnlocked?.Invoke(achievement);
            }
        }

        private void HandleAchievementProgress(IObjective objective)
        {
            if (objective is AchievementRuntime achievement)
            {
                OnAchievementProgress?.Invoke(achievement);
            }
        }

        // Save/Load support
        public Dictionary<string, int> GetAchievementProgress()
        {
            return _runtimes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.CurrentValue
            );
        }

        public void SetAchievementProgress(Dictionary<string, int> progress)
        {
            foreach (var kvp in progress)
            {
                if (_runtimes.TryGetValue(kvp.Key, out var runtime))
                {
                    runtime.SetProgress(kvp.Value);
                }
            }
        }
    }
}
```

**Validation:** Achievements track progress and unlock correctly.

### Phase 5: Documentation

**Goal:** Document how to set up example scenes.

#### Tutorial Example Scene Setup

```
Scene: TutorialExample

GameObjects:
1. TutorialManager (add TutorialManager component)

2. TutorialUI (Canvas)
   - InstructionPanel (Panel with Text)
   - HighlightOverlay (Image for highlighting)
   - SkipButton (Button)

3. Player (simple character with movement)

4. TutorialTrigger (empty GameObject)
   - TutorialTrigger.cs component
   - Reference to Tutorial_SO asset

ScriptableObjects to create:
1. Tutorial_SO "MovementTutorial"
   - tutorialId: "movement_tutorial"
   - displayName: "Movement Tutorial"
   - canSkip: true
   - steps:
     - TutorialStep_SO "Step1_Move"
     - TutorialStep_SO "Step2_Jump"

2. TutorialStep_SO "Step1_Move"
   - stepId: "move"
   - instruction: "Use WASD to move"
   - completionCondition: (event-driven condition for player movement)
   - highlightElementId: "" (optional)

3. TutorialStep_SO "Step2_Jump"
   - stepId: "jump"
   - instruction: "Press Space to jump"
   - completionCondition: (event-driven condition for jump)
```

#### Achievement Example Scene Setup

```
Scene: AchievementExample

GameObjects:
1. AchievementManager (add AchievementManager component)
   - Assign all Achievement_SO assets to allAchievements list

2. AchievementUI (Canvas)
   - AchievementList (ScrollView with achievement entries)
   - UnlockPopup (Panel that shows when achievement unlocks)

3. EnemySpawner (spawns clickable enemies)

ScriptableObjects to create:
1. Achievement_SO "FirstBlood"
   - achievementId: "first_blood"
   - displayName: "First Blood"
   - description: "Defeat your first enemy"
   - targetValue: 1
   - completionCondition: ConditionEventDriven (OnEnemyKilled)
   - points: 10
   - isSecret: false

2. Achievement_SO "MonsterHunter"
   - achievementId: "monster_hunter"
   - displayName: "Monster Hunter"
   - description: "Defeat 10 enemies"
   - targetValue: 10
   - completionCondition: ConditionEventDriven (OnEnemyKilled)
   - points: 50
   - isSecret: false

3. Achievement_SO "Exterminator"
   - achievementId: "exterminator"
   - displayName: "Exterminator"
   - description: "Defeat 100 enemies"
   - targetValue: 100
   - completionCondition: ConditionEventDriven (OnEnemyKilled)
   - points: 100
   - isSecret: true
```

---

## File Structure

```
Runtime/Scripts/
├── Core/
│   ├── Abstractions/           ← NEW (Phase 1)
│   │   ├── IObjective.cs
│   │   ├── IObjectiveGroup.cs
│   │   ├── IStage.cs
│   │   ├── IMission.cs
│   │   └── ObjectiveEnums.cs
│   │
│   ├── Quests/                 ← MODIFIED (Phase 2)
│   │   └── QuestRuntime.cs     (add : IMission)
│   │
│   ├── Stages/                 ← MODIFIED (Phase 2)
│   │   └── QuestStageRuntime.cs (add : IStage)
│   │
│   ├── TaskGroups/             ← MODIFIED (Phase 2)
│   │   └── TaskGroupRuntime.cs (add : IObjectiveGroup)
│   │
│   └── Tasks/                  ← MODIFIED (Phase 2)
│       └── TaskRuntime.cs      (add : IObjective)
│
├── Tutorials/                  ← NEW (Phase 3)
│   ├── TutorialRuntime.cs
│   ├── TutorialStepRuntime.cs
│   ├── TutorialManager.cs
│   └── ScriptableObjects/
│       ├── Tutorial_SO.cs
│       └── TutorialStep_SO.cs
│
└── Achievements/               ← NEW (Phase 4)
    ├── AchievementRuntime.cs
    ├── AchievementManager.cs
    └── ScriptableObjects/
        └── Achievement_SO.cs
```

---

## Backward Compatibility

| Existing Code | Impact |
|---------------|--------|
| `QuestManager.AddQuest()` | No change |
| `QuestRuntime` API | No change |
| `TaskRuntime` API | No change |
| Quest_SO assets | No change |
| Save/Load snapshots | No change |
| All existing tests | Pass unchanged |

**Why:** We're adding interfaces to existing classes, not modifying their behavior.

---

## Validation Checklist

### Phase 1: Interfaces
- [ ] IObjective.cs created
- [ ] IObjectiveGroup.cs created
- [ ] IStage.cs created
- [ ] IMission.cs created
- [ ] ObjectiveEnums.cs created
- [ ] Project compiles
- [ ] No existing code modified yet

### Phase 2: Interface Implementation
- [ ] TaskRuntime implements IObjective
- [ ] TaskGroupRuntime implements IObjectiveGroup
- [ ] QuestStageRuntime implements IStage
- [ ] QuestRuntime implements IMission
- [ ] All existing tests pass
- [ ] BasicQuestExample scene works identically

### Phase 3: Tutorials
- [ ] Tutorial_SO.cs created
- [ ] TutorialStep_SO.cs created
- [ ] TutorialRuntime.cs created
- [ ] TutorialStepRuntime.cs created
- [ ] TutorialManager.cs created
- [ ] Can create Tutorial_SO assets in editor
- [ ] Tutorials start and progress through steps
- [ ] Conditions complete steps correctly
- [ ] Skip functionality works
- [ ] TutorialManager tracks completion

### Phase 4: Achievements
- [ ] Achievement_SO.cs created
- [ ] AchievementRuntime.cs created
- [ ] AchievementManager.cs created
- [ ] Can create Achievement_SO assets in editor
- [ ] Achievements track progress
- [ ] Event-driven conditions update progress
- [ ] Achievements unlock at target value
- [ ] AchievementManager queries work

### Phase 5: Documentation
- [x] Tutorial example setup documented (see [tutorials.md](tutorials.md))
- [x] Achievement example setup documented (see [achievements.md](achievements.md))
- [x] Integration notes complete

---

## Summary

This design:
1. **Adds interfaces** without breaking existing code
2. **Follows all 5 principles** from the architectural tips
3. **Keeps Quest system intact** - it becomes an implementation of IMission
4. **Enables Tutorials and Achievements** using the same patterns
5. **Each manager tracks its own type** - no unnecessary unified registry

**Implementation Order:**
1. Create interfaces (Phase 1)
2. Implement on existing classes (Phase 2)
3. Build Tutorial system (Phase 3)
4. Build Achievement system (Phase 4)
5. Document example setups (Phase 5)

Each phase is independently testable and validates before moving to the next.

---

*This design follows the 5 Architectural Tips: Interfaces first, Logic separated from MonoBehaviors, Data separated from Logic, Event-Driven Architecture, and Registry Pattern (each manager is its own registry).*
