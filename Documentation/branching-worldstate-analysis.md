# BranchChoice & WorldState Implementation Analysis

**Date:** 2025-12-28
**Purpose:** Deep analysis of implementation approaches for Quest Branching and World State systems
**Context:** HelloDev Quest System v2.2.0 → v3.0.0 planning

---

## Executive Summary

This analysis examines how to implement **BranchChoice** (mutually exclusive quest paths) and **WorldState** (persistent game flags) for the HelloDev Quest System. We evaluate approaches through three lenses:

1. **AAA Studio Patterns** - How Bethesda, CD Projekt RED, BioWare, and Larian solve these problems
2. **Asset Store Flexibility** - Interface-first design for maximum reusability
3. **Leveraging Existing Tools** - Minimizing new code by extending current systems

**Key Finding:** The existing architecture (Events, Conditions, Stages, Transitions) already provides 80% of what's needed. The recommended approach extends existing patterns rather than creating parallel systems.

---

## Part 1: AAA Studio Approaches

### 1.1 Bethesda (Skyrim, Fallout, Starfield)

**Architecture:**
```
Quest
├── Stage 0: "Begin Quest"
├── Stage 10: "Investigate"
├── Stage 20: "Confront" ← mutually exclusive with 30
├── Stage 30: "Sneak"   ← mutually exclusive with 20
└── Stage 100: "Resolution"

Global Variables (World State)
├── bGoblinChiefDead = bool
├── iPlayerFaction = int
└── sLastDialogueChoice = string
```

**Key Insights:**
- **Stages are numbered with gaps (0, 10, 20)** for easy insertion
- **SetStage command** from dialogue/scripts drives branching
- **Global Variables** are simple key-value pairs
- **Conditions check globals**, not vice versa
- **Branching is emergent** - no explicit "branch point" data structure

**What We Already Have:**
- ✅ Numbered stages (`stageIndex: 0, 10, 20`)
- ✅ Transitions with conditions
- ✅ `TrySetStage(int)` API
- ❌ Global variable system (need WorldState)

---

### 1.2 CD Projekt RED (Witcher 3, Cyberpunk 2077)

**Architecture:**
```
Fact Database (World State)
├── "q001_chose_violence" = true
├── "npc_triss_romance" = 2
└── "world_emperor_alive" = true

Quest Conditions
└── fact("q001_chose_violence") == true → unlock stage

Consequences
└── Setting a fact ripples across all quests checking it
```

**Key Insights:**
- **"Fact" system** - everything is a fact with a value
- **Facts are first-class citizens** - not hidden in quest state
- **Dialogue sets facts**, quests react to them
- **Cross-quest consequences** via shared facts
- **No separate "branch" data structure** - branches are just conditional stage transitions

**What We Already Have:**
- ✅ Condition system (`ICondition`, `IConditionEventDriven`)
- ✅ Composite conditions (`CompositeCondition_SO`)
- ✅ Event-driven reactivity
- ❌ Persistent fact/flag storage

---

### 1.3 BioWare (Mass Effect, Dragon Age)

**Architecture:**
```
Flags (categorized)
├── Character Flags
│   ├── "liara_loyalty" = true
│   └── "garrus_alive" = true
├── World Flags
│   ├── "council_saved" = true
│   └── "collector_base_destroyed" = false
└── Mission Flags
    └── "priority_earth_complete" = true

Memory System
└── Imports flags from previous games
```

**Key Insights:**
- **Categorized flags** for organization
- **Cross-game persistence** (ME save import)
- **Paragon/Renegade points** as special counters
- **Character relationship tracking** as integers

**What We Already Have:**
- ✅ ID_SO for categorization/organization
- ✅ Type-safe references
- ❌ Flag persistence across sessions

---

### 1.4 Larian (Divinity: Original Sin 2, Baldur's Gate 3)

**Architecture:**
```
Tag System
├── Characters have tags: "HERO", "UNDEAD", "SCHOLAR"
├── Quests check tag combinations
└── Highly composable

Origin Flags
├── Per-character quest progress
└── Shared world state
```

**Key Insights:**
- **Tags are additive** - characters accumulate them
- **Conditions compose tags** with AND/OR logic
- **Extreme flexibility** - same quest plays differently per character

**What We Already Have:**
- ✅ CompositeCondition_SO (AND/OR)
- ✅ Flexible condition system
- ❌ Tag system (could use ID_SO + Set)

---

### 1.5 Common Patterns Across All Studios

| Pattern | Bethesda | CDPR | BioWare | Larian | HelloDev |
|---------|----------|------|---------|--------|----------|
| Key-value world state | ✅ Globals | ✅ Facts | ✅ Flags | ✅ Tags | ❌ Need |
| Event-driven updates | ❌ | ✅ | ✅ | ✅ | ✅ Have |
| Condition composition | ✅ | ✅ | ✅ | ✅ | ✅ Have |
| Stage-based quests | ✅ | ✅ | ✅ | ✅ | ✅ Have |
| Explicit branch points | ❌ | ❌ | ❌ | ❌ | ❌ Don't need! |

**Critical Insight:** None of these studios have an explicit "BranchPoint" data structure. **Branching is emergent from conditional transitions.**

---

## Part 2: Asset Store Flexibility Analysis

### 2.1 Requirements for Asset Store Package

An Asset Store package must be:

1. **Dependency-free** - Work without Odin, DOTween, etc.
2. **Implementation-agnostic** - Don't force storage backend
3. **Extensible** - Easy to add new condition/event types
4. **Serialization-flexible** - Support JSON, binary, cloud saves
5. **UI-agnostic** - Provide events, not UI components
6. **Backward compatible** - Never break existing projects

### 2.2 Interface-First Design Principles

```csharp
// BAD: Forces implementation
public class WorldStateManager : MonoBehaviour
{
    private Dictionary<string, object> _state = new();  // Forces dictionary
    public void Save(string path) { ... }               // Forces file system
}

// GOOD: Interface allows any implementation
public interface IWorldState
{
    void SetBool(string key, bool value);
    bool GetBool(string key, bool defaultValue = false);
    event Action<string, object, object> OnValueChanged;
    // No Save/Load - that's the game's responsibility
}

// Game implements:
public class MyGameWorldState : IWorldState
{
    // Could use: Dictionary, PlayerPrefs, PlayFab, Steam Cloud, etc.
}
```

### 2.3 Flexibility Spectrum

| Approach | Flexibility | Complexity | When to Use |
|----------|-------------|------------|-------------|
| Concrete classes only | Low | Low | Internal tools, never published |
| Interface + Default impl | Medium | Medium | **Asset Store (recommended)** |
| Interface only | High | High | Enterprise SDKs |
| Event-driven extension | Very High | Low | **HelloDev pattern (best)** |

---

## Part 3: Leveraging Existing Tools

### 3.1 What We Already Have

```
HelloDev Condition System
├── ICondition (passive evaluation)
├── IConditionEventDriven (reactive evaluation)
├── Condition_SO (ScriptableObject base)
├── ConditionEventDriven_SO<T> (generic typed)
├── ConditionBool_SO, ConditionInt_SO, etc.
└── CompositeCondition_SO (AND/OR composition)

HelloDev Event System
├── GameEventBase_SO (base class)
├── GameEvent_SO<T> (generic typed)
│   ├── LastValue (stores most recent raise value)
│   ├── HasBeenRaised (tracks if ever raised)
│   └── RemoveAllListeners()
└── GameEventBool_SO, GameEventInt_SO, etc.

HelloDev Quest System - Stages
├── QuestStage (stage data)
├── StageTransition (transition rules)
│   ├── TargetStageIndex
│   ├── Trigger (OnGroupsComplete, OnConditionsMet, Manual)
│   ├── Conditions (List<Condition_SO>)
│   └── Priority
└── TransitionTrigger enum
```

### 3.2 Gap Analysis for Branching

**Current StageTransition:**
```csharp
[Serializable]
public class StageTransition
{
    private int targetStageIndex;
    private TransitionTrigger trigger;
    private List<Condition_SO> conditions;
    private string transitionLabel;     // ← Already has label!
    private int priority;
}
```

**What's Missing for Player Choice Branching:**
```csharp
// Need to add:
private bool isPlayerChoice;           // Marks as choice vs automatic
private LocalizedString choiceLabel;   // Localized text for UI
private Sprite choiceIcon;             // Optional icon
```

**That's it.** Three fields turn transitions into branch choices.

### 3.3 Gap Analysis for World State

**Current GameEvent_SO<T>:**
```csharp
public abstract class GameEvent_SO<T> : GameEventBase_SO
{
    private T _lastValue;           // ← Already stores value!
    private bool _hasBeenRaised;    // ← Already tracks state!

    public T LastValue => _lastValue;
    public bool HasBeenRaised => _hasBeenRaised;
}
```

**Insight:** GameEvents already function as world state. `LastValue` persists the most recent value.

**What's Missing:**
1. `IsPersistent` flag - marks events for save/load
2. Registry - tracks all persistent events
3. Save/Load - serializes persistent events' LastValue

### 3.4 Minimal Implementation Strategy

#### For Branching (Extend StageTransition):

```csharp
// Add to StageTransition.cs (3 new fields)
[SerializeField] private bool isPlayerChoice;
[SerializeField] private LocalizedString choiceLabel;
[SerializeField] private Sprite choiceIcon;

public bool IsPlayerChoice => isPlayerChoice;
public LocalizedString ChoiceLabel => choiceLabel;
public Sprite ChoiceIcon => choiceIcon;

// Add to QuestStage.cs (helper property)
public List<StageTransition> GetAvailableChoices()
{
    return Transitions
        .Where(t => t.IsPlayerChoice && t.EvaluateConditions())
        .OrderByDescending(t => t.Priority)
        .ToList();
}

// Add to QuestRuntime (events for UI)
public UnityEvent<QuestRuntime, List<StageTransition>> OnChoiceRequired = new();

public bool SelectChoice(StageTransition choice)
{
    if (!choice.IsPlayerChoice || !CurrentStage.Transitions.Contains(choice))
        return false;

    TrySetStage(choice.TargetStageIndex);
    return true;
}
```

**Total new code: ~50 lines**

#### For World State (Extend Event System):

**Option A: Minimal (extend existing)**
```csharp
// Add to GameEventBase_SO
[SerializeField] private bool isPersistent;
public bool IsPersistent => isPersistent;

// New singleton
public class PersistentEventRegistry : SingletonBase<PersistentEventRegistry>
{
    private List<GameEventBase_SO> _persistentEvents = new();

    public void Register(GameEventBase_SO evt) { ... }
    public Dictionary<string, object> CaptureState() { ... }
    public void RestoreState(Dictionary<string, object> state) { ... }
}
```

**Option B: Full Interface (implementation-plan.md approach)**
```csharp
public interface IWorldState
{
    void SetBool(string key, bool value);
    bool GetBool(string key, bool defaultValue = false);
    // ... other types
    event Action<string, object, object> OnValueChanged;
}

public static class WorldStateProvider
{
    public static IWorldState Instance { get; private set; }
    public static void SetProvider(IWorldState worldState) { ... }
}

// New condition types
public class ConditionWorldStateBool_SO : ConditionEventDriven_SO<bool>
{
    [SerializeField] private string flagName;
    [SerializeField] private bool expectedValue;

    public override bool Evaluate()
    {
        return WorldStateProvider.Instance.GetBool(flagName) == expectedValue;
    }
}
```

---

## Part 4: Recommendation

### 4.1 Recommended Approach: Hybrid Minimal

**For Branching: Extend StageTransition**
- Add 3 fields: `isPlayerChoice`, `choiceLabel`, `choiceIcon`
- Add events to QuestRuntime for UI integration
- Add `SelectChoice(StageTransition)` API
- **Zero new data structures**

**For World State: Interface + Event Extension**
- Create `IWorldState` interface (future-proof)
- Create `DictionaryWorldState` default implementation
- Add `IsPersistent` to GameEventBase_SO for dual-mode usage
- Create `ConditionWorldState*_SO` condition types
- **Maximum flexibility, minimum breaking changes**

### 4.2 Why Not Follow implementation-plan.md Exactly?

The implementation plan proposes:
```csharp
[Serializable]
public class BranchChoice
{
    public string choiceId;
    public LocalizedString choiceLabel;
    public int targetStageIndex;
    public List<Condition_SO> availabilityConditions;
    public Sprite choiceIcon;
}

[Serializable]
public class BranchPoint
{
    public string branchId;
    public LocalizedString promptText;
    public List<BranchChoice> choices;
    public int defaultChoiceIndex;
    public float timeoutSeconds;
}
```

**Issues:**
1. **Duplicates StageTransition** - BranchChoice has targetStageIndex, conditions (same as StageTransition)
2. **Two ways to do same thing** - Designer confusion
3. **More code to maintain** - Bug surface area
4. **Violates DRY** - Don't Repeat Yourself

**Better:** Extend StageTransition with `isPlayerChoice` flag. A branch is just a set of player-selectable transitions from one stage.

### 4.3 Recommended Implementation Order

```
Phase 7.2a: Branching via Extended Transitions (8-12 hours)
├── Add isPlayerChoice, choiceLabel, choiceIcon to StageTransition
├── Add OnChoiceRequired event to QuestRuntime
├── Add SelectChoice API
├── Update Odin inspector for choice configuration
├── Add TransitionTrigger.PlayerChoice enum value
└── Create example quest with branching

Phase 7.2b: World State Interface (12-16 hours)
├── Create IWorldState interface
├── Create DictionaryWorldState default
├── Create WorldStateProvider static accessor
├── Add IsPersistent to GameEventBase_SO
├── Create ConditionWorldStateBool_SO
├── Create ConditionWorldStateInt_SO
├── Create ConditionWorldStateFloat_SO
├── Create ConditionWorldStateString_SO
└── Update Odin inspectors

Phase 7.2c: Integration (8-12 hours)
├── Add worldStateChanges to StageTransition (for consequences)
├── Track branch decisions in QuestRuntime.BranchDecisions
├── Add branch decision to save/load snapshots
└── Create comprehensive example quest
```

---

## Part 5: Detailed Design Recommendations

### 5.1 StageTransition Extensions

```csharp
[Serializable]
public class StageTransition
{
    // Existing
    [SerializeField] private int targetStageIndex;
    [SerializeField] private TransitionTrigger trigger;
    [SerializeField] private List<Condition_SO> conditions;
    [SerializeField] private string transitionLabel;
    [SerializeField] private int priority;

    // NEW: Player Choice
    [SerializeField] private bool isPlayerChoice;
    [SerializeField] private LocalizedString choiceText;
    [SerializeField] private Sprite choiceIcon;
    [SerializeField] private string choiceId;  // For tracking/save

    // NEW: World State Consequences
    [SerializeField] private List<WorldStateChange> consequences;

    // Properties
    public bool IsPlayerChoice => isPlayerChoice;
    public LocalizedString ChoiceText => choiceText;
    public Sprite ChoiceIcon => choiceIcon;
    public string ChoiceId => string.IsNullOrEmpty(choiceId)
        ? $"choice_{targetStageIndex}"
        : choiceId;
    public List<WorldStateChange> Consequences => consequences;

    /// <summary>
    /// Applies all consequences to world state.
    /// </summary>
    public void ApplyConsequences()
    {
        if (consequences == null) return;
        foreach (var change in consequences)
        {
            change.Apply();
        }
    }
}

[Serializable]
public class WorldStateChange
{
    public string flagName;
    public WorldStateValueType valueType;
    public bool boolValue;
    public int intValue;
    public float floatValue;
    public string stringValue;

    public void Apply()
    {
        var ws = WorldStateProvider.Instance;
        switch (valueType)
        {
            case WorldStateValueType.Bool:
                ws.SetBool(flagName, boolValue);
                break;
            case WorldStateValueType.Int:
                ws.SetInt(flagName, intValue);
                break;
            // etc.
        }
    }
}

public enum WorldStateValueType { Bool, Int, Float, String }
```

### 5.2 TransitionTrigger Extension

```csharp
public enum TransitionTrigger
{
    /// <summary>Transition when all task groups complete.</summary>
    OnGroupsComplete,

    /// <summary>Transition when any task group completes.</summary>
    OnAnyGroupComplete,

    /// <summary>Transition when conditions are met (polled).</summary>
    OnConditionsMet,

    /// <summary>Transition via explicit API call.</summary>
    Manual,

    /// <summary>Transition requires player selection (shows UI).</summary>
    PlayerChoice  // NEW
}
```

### 5.3 World State Interface

```csharp
namespace HelloDev.QuestSystem.WorldState
{
    /// <summary>
    /// Interface for world state storage. Implement for custom backends.
    /// </summary>
    public interface IWorldState
    {
        // Setters
        void SetBool(string key, bool value);
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void SetString(string key, string value);

        // Getters
        bool GetBool(string key, bool defaultValue = false);
        int GetInt(string key, int defaultValue = 0);
        float GetFloat(string key, float defaultValue = 0f);
        string GetString(string key, string defaultValue = "");

        // Metadata
        bool HasKey(string key);
        void RemoveKey(string key);
        IEnumerable<string> GetAllKeys();

        // Events (for reactive conditions)
        event Action<string, object, object> OnValueChanged;

        // Serialization
        Dictionary<string, object> CaptureSnapshot();
        void RestoreSnapshot(Dictionary<string, object> snapshot);
    }

    /// <summary>
    /// Default dictionary-based implementation.
    /// </summary>
    public class DictionaryWorldState : IWorldState
    {
        private readonly Dictionary<string, object> _values = new();
        public event Action<string, object, object> OnValueChanged;

        public void SetBool(string key, bool value) => SetValue(key, value);
        public bool GetBool(string key, bool def = false)
            => _values.TryGetValue(key, out var v) && v is bool b ? b : def;
        // ... other type implementations

        private void SetValue(string key, object value)
        {
            _values.TryGetValue(key, out var oldValue);
            _values[key] = value;
            if (!Equals(oldValue, value))
                OnValueChanged?.Invoke(key, oldValue, value);
        }

        public Dictionary<string, object> CaptureSnapshot() => new(_values);
        public void RestoreSnapshot(Dictionary<string, object> snapshot)
        {
            _values.Clear();
            foreach (var kv in snapshot) _values[kv.Key] = kv.Value;
        }
    }

    /// <summary>
    /// Static accessor for world state.
    /// </summary>
    public static class WorldStateProvider
    {
        private static IWorldState _instance;

        public static IWorldState Instance
        {
            get => _instance ??= new DictionaryWorldState();
        }

        public static void SetProvider(IWorldState provider)
        {
            _instance = provider ?? throw new ArgumentNullException();
        }

        public static void Reset() => _instance = null;
    }
}
```

### 5.4 World State Conditions

```csharp
[CreateAssetMenu(menuName = "HelloDev/Conditions/World State/Bool")]
public class ConditionWorldStateBool_SO : Condition_SO, IConditionEventDriven
{
    [SerializeField] private string flagName;
    [SerializeField] private bool expectedValue = true;

    private Action _onConditionMet;
    private bool _isSubscribed;

    public override bool Evaluate()
    {
        bool current = WorldStateProvider.Instance.GetBool(flagName);
        bool result = current == expectedValue;
        return IsInverted ? !result : result;
    }

    public void SubscribeToEvent(Action onConditionMet)
    {
        if (_isSubscribed) return;
        _onConditionMet = onConditionMet;
        WorldStateProvider.Instance.OnValueChanged += OnWorldStateChanged;
        _isSubscribed = true;
    }

    public void UnsubscribeFromEvent()
    {
        if (!_isSubscribed) return;
        WorldStateProvider.Instance.OnValueChanged -= OnWorldStateChanged;
        _isSubscribed = false;
    }

    public void ForceFulfillCondition()
    {
        WorldStateProvider.Instance.SetBool(flagName, expectedValue);
    }

    private void OnWorldStateChanged(string key, object oldVal, object newVal)
    {
        if (key == flagName && Evaluate())
            _onConditionMet?.Invoke();
    }
}
```

---

## Part 6: Comparison Matrix

### 6.1 Implementation Approaches

| Aspect | Plan's BranchPoint | Extended Transition | Recommendation |
|--------|-------------------|---------------------|----------------|
| New data structures | 2 (BranchPoint, BranchChoice) | 0 | Extended ✅ |
| Lines of new code | ~200 | ~80 | Extended ✅ |
| Learning curve | New concepts | Same concepts | Extended ✅ |
| Flexibility | Good | Same | Tie |
| AAA alignment | Divergent | Matches Bethesda | Extended ✅ |
| Backward compat | 100% | 100% | Tie |

### 6.2 World State Approaches

| Aspect | String Keys Only | ScriptableObject Keys | Interface (Recommended) |
|--------|------------------|----------------------|-------------------------|
| Type safety | Low | High | Medium |
| Designer discoverability | Low | High | Medium |
| Runtime flexibility | High | Low | High |
| Save/load complexity | Low | Medium | Low |
| AAA alignment | Bethesda | BioWare | CD Projekt ✅ |
| Asset Store fit | Medium | Medium | High ✅ |

---

## Conclusion

### For Branching:
**Extend StageTransition** with `isPlayerChoice`, `choiceText`, `choiceIcon`, and `consequences`. This matches how AAA studios actually implement branching (conditional stage transitions, not explicit branch points).

### For World State:
**Use Interface-first design** with `IWorldState`, `DictionaryWorldState` default, and `ConditionWorldState*_SO` types. This provides Asset Store flexibility while remaining simple to use.

### Key Principle:
**Composition over creation.** Extend existing patterns rather than creating parallel systems. The HelloDev architecture is already well-suited for these features.

---

*Analysis prepared 2025-12-28*
