# Quest Save System - Comprehensive Critique

**Date:** 2025-12-29
**Version:** 3.1.0
**Perspective:** Asset Store developer implementing custom save integration

---

## Executive Summary

This document analyzes the Quest System's save/load architecture from the perspective of a developer integrating it with their own save system (e.g., Easy Save 3, PlayFab, custom binary format). We evaluate the API design, identify pain points, and propose improvements for Asset Store readiness.

---

### Current Design

```
┌──────────────────────────────────────────────────────────────┐
│                      QuestSaveManager                        │
│  - Singleton MonoBehaviour                                   │
│  - CaptureSnapshot() / RestoreSnapshot()                     │
│  - SaveAsync() / LoadAsync() delegates to provider           │
└─────────────────────────────────┬────────────────────────────┘
                                  │
                                  ▼
┌──────────────────────────────────────────────────────────────┐
│                     ISaveDataProvider                        │
│  - SaveAsync(slotKey, snapshot)                              │
│  - LoadAsync(slotKey) → snapshot                             │
│  - ExistsAsync(slotKey)                                      │
│  - DeleteAsync(slotKey)                                      │
│  - GetMetadataAsync(slotKey) 
## 1. Architecture Analysis
                                │
│  - GetAllSlotsAsync()                                        │
└──────────────────────────────────────────────────────────────┘
```

### Pros

1. **Clean Abstraction:** `ISaveDataProvider` allows full customization of storage backend
2. **Snapshot Pattern:** `CaptureSnapshot()` and `RestoreSnapshot()` enable manual integration
3. **Async API:** Non-blocking operations (though implementation could improve)
4. **Events:** `OnBeforeSave`, `OnAfterSave`, etc. for UI integration
5. **Slot-based:** Supports multiple save slots out of the box
6. **Metadata Separation:** Quick access to save info without loading full data

### Cons

1. **Tight Coupling to QuestManager:** Cannot save/load without QuestManager singleton
2. **Manual WorldFlag Registration:** Easy to forget, no auto-discovery
3. **No Lifecycle Hooks:** Can't inject save/load into QuestManager's lifecycle
4. **Synchronous Restore:** `RestoreSnapshot` blocks main thread
5. **Missing Validation API:** No way to validate snapshot before restore

---

## 2. Integration Pain Points

### Pain Point 1: Initializing the Save System

**Scenario:** Developer wants save system ready when game loads.

**Current Approach:**
```csharp
void Start()
{
    QuestSaveManager.Instance.SetProvider(new JsonFileSaveProvider());

    // Must manually register every world flag!
    foreach (var flag in allWorldFlags)
        QuestSaveManager.Instance.RegisterWorldFlag(flag);
}
```

**Pain:**
- Must manually enumerate all WorldFlags
- No registry pattern for auto-discovery
- Easy to forget flags, leading to save bugs

**Suggested Fix:**
```csharp
// Option A: Scriptable singleton registry
[CreateAssetMenu]
public class WorldFlagRegistry_SO : ScriptableObject
{
    public List<WorldFlagBase_SO> AllFlags;
}

// Option B: Auto-discovery via attributes
[AutoRegister]
public class MyWorldFlag : WorldFlagBool_SO { }
```

---

### Pain Point 2: Integrating with Existing Save System

**Scenario:** Game uses Easy Save 3 for all saves, wants quests included.

**Current Approach:**
```csharp
// In your save manager:
public void SaveGame(string slot)
{
    var questSnapshot = QuestSaveManager.Instance.CaptureSnapshot();

    ES3.Save("questData", questSnapshot, slot + ".es3");
    ES3.Save("playerData", playerData, slot + ".es3");
    // etc.
}
```

**Pain:**
- Must manually capture snapshot at right time
- No way to hook into QuestManager's internal save points
- Snapshot is a separate class - can't merge with game's save format easily

**Suggested Fix:**
```csharp
// Event-based integration
QuestManager.Instance.OnQuestStateChanged += (quest) => {
    if (autoSaveEnabled) TriggerAutoSave();
};

// Or lifecycle hooks
public class QuestManager
{
    public Action<QuestSystemSnapshot> OnRequestSave; // Called when quests change
    public Func<QuestSystemSnapshot> OnRequestLoad;    // Called at initialization
}
```

---

### Pain Point 3: Partial Saves (Save Only Changed Data)

**Scenario:** Game wants incremental saves, not full snapshot every time.

**Current Limitation:** `CaptureSnapshot()` captures everything. No delta/diff support.

**Suggested Fix:**
```csharp
public class QuestChangeTracker
{
    public List<QuestSnapshot> ChangedQuests { get; }
    public List<WorldFlagSnapshot> ChangedFlags { get; }
    public void MarkClean();
}

// Usage
var changes = QuestSaveManager.Instance.GetChangedSinceLastSave();
SaveOnlyChanges(changes);
```

---

### Pain Point 4: Binary/Custom Serialization

**Scenario:** Developer wants binary format for smaller saves.

**Current Limitation:** `QuestSystemSnapshot` uses Unity's `[Serializable]` which works with `JsonUtility` but not with custom binary serializers without extra work.

**Pain:**
- Nested classes with List<T> don't serialize well to binary
- No `ISerializable` implementation
- Developer must manually map snapshot to their format

**Suggested Fix:**
```csharp
// Flat data transfer objects
public struct QuestSaveData
{
    public string[] QuestGuids;
    public int[] QuestStates;
    public int[] CurrentStages;
    // Flat arrays = easy binary serialization
}

// Conversion
public QuestSaveData ToFlatFormat();
public static QuestSystemSnapshot FromFlatFormat(QuestSaveData data);
```

---

### Pain Point 5: Subscribing to Quest Events for Auto-Save

**Scenario:** Auto-save whenever significant quest progress happens.

**Current Approach:**
```csharp
void Start()
{
    QuestManager.Instance.QuestCompleted.AddListener(OnQuestCompleted);
    QuestManager.Instance.QuestStarted.AddListener(OnQuestStarted);
    QuestManager.Instance.QuestFailed.AddListener(OnQuestFailed);
    // ...many more events
}

void OnQuestCompleted(QuestRuntime quest)
{
    TriggerAutoSave();
}
```

**Pain:**
- Must subscribe to many events manually
- No single "quest data changed" event
- Easy to miss events

**Suggested Fix:**
```csharp
// Single aggregate event
public UnityEvent<QuestChangeType> OnQuestDataChanged;

// Or callback interface
public interface IQuestPersistenceListener
{
    void OnQuestDataDirty();
}
QuestManager.Instance.RegisterPersistenceListener(this);
```

---

### Pain Point 6: QuestManager Lifecycle Integration

**Scenario:** Load quest data before QuestManager fully initializes.

**Current Limitation:**
```csharp
void Start()
{
    // QuestManager.InitializeManager() is called in Awake
    // By the time we get here, quests are already in NotStarted state
    // Loading now means we have to clear and re-add everything
}
```

**Pain:**
- `ShutdownManager()` + `InitializeManager()` is heavy for restore
- No pre-initialization hook

**Suggested Fix:**
```csharp
// Pre-init hook
public class QuestManager
{
    [SerializeField] private bool loadOnInitialize = true;

    protected override void Awake()
    {
        if (loadOnInitialize && HasSavedData())
        {
            InitializeFromSave();
        }
        else
        {
            InitializeClean();
        }
    }
}
```

---

### Pain Point 7: Testing Save/Load in Editor

**Scenario:** Designer wants to test save/load without playing the game.

**Current Limitation:** No editor-time save/load preview.

**Suggested Fix:**
```csharp
#if UNITY_EDITOR
[MenuItem("Tools/Quest System/Test Save")]
static void TestSave() { ... }

[MenuItem("Tools/Quest System/Test Load")]
static void TestLoad() { ... }
#endif
```

---

## 3. Issues Fixed in This Session

| Issue | Fix Applied |
|-------|-------------|
| WorldFlag uses asset name | Added `flagGuid` field with auto-generation |
| StringTask restoration | Now uses `SetValue()` instead of `IncrementStep()` |
| TimedTask remaining time | Added `SetRemainingTime(float)` method |
| DiscoveryTask conditions | Now uses condition indices (with legacy fallback) |
| QuestLine state not restored | Added `RestoreState()` and `HasStarted` property |

---

## 4. Remaining Issues

### High Priority

1. ~~**No Auto-Discovery for WorldFlags**~~ ✅ FIXED - WorldFlagRegistry_SO
   - ~~Risk: Developers forget to register flags~~
   - ~~Fix: ScriptableObject registry or attribute-based discovery~~

2. ~~**No Validation API**~~ ✅ FIXED - ValidateSnapshot() + SnapshotValidationResult
   - ~~Risk: Loading corrupted save silently fails~~
   - ~~Fix: `ValidateSnapshot()` method with detailed results~~

3. **Blocking Restore**
   - Risk: Large saves freeze the game
   - Fix: Async `RestoreSnapshotAsync()` with progress callback

### Medium Priority

4. **No Version Migration**
   - Risk: Old saves break on update
   - Fix: Version-aware migration system

5. ~~**No Single "Data Changed" Event**~~ ✅ FIXED - OnQuestDataChanged
   - ~~Risk: Developers miss auto-save triggers~~
   - ~~Fix: Aggregate dirty flag event~~

6. **No Editor Testing Tools**
   - Risk: Slow iteration for designers
   - Fix: Editor menu commands

### Low Priority

7. **No Delta/Incremental Saves**
8. **No Binary Serialization Support**
9. **No Save Corruption Recovery**

---

## 5. Recommendations for Asset Store

### Must Have
- [x] Auto-discovery for WorldFlags ✅ WorldFlagRegistry_SO
- [x] Validation API with meaningful errors ✅ ValidateSnapshot()
- [ ] Documentation with integration examples
- [ ] Sample scenes for testing

### Should Have
- [x] Single "data dirty" event for auto-save ✅ OnQuestDataChanged
- [ ] Async restore with progress
- [ ] Editor testing tools
- [ ] Version migration framework

### Nice to Have
- [ ] Binary serialization support
- [ ] Delta/incremental saves
- [ ] Cloud save integration examples

---

## 6. Code Quality Assessment

### What's Good
- Clean interface abstraction (`ISaveDataProvider`)
- Comprehensive event system
- Good documentation comments
- Type-safe snapshot classes

### What Needs Improvement
- More defensive coding (null checks, validation)
- Better error messages
- Performance profiling for large datasets

---

## 7. Conclusion

The save system has a solid foundation with the `ISaveDataProvider` abstraction and snapshot pattern. However, for true Asset Store readiness, it needs:

1. **Better Discovery:** Auto-register WorldFlags and other saveable data
2. **Better Integration:** Single events for save triggers, lifecycle hooks
3. **Better Validation:** Pre-restore validation with actionable errors
4. **Better Tooling:** Editor commands for testing

With these improvements, the system would be competitive with other Asset Store quest systems.

---

## 8. Codebase Complexity Analysis

### Summary Table

| Issue | Severity | Files Affected | Impact |
|-------|----------|----------------|--------|
| Event Explosion | CRITICAL | QuestRuntime, QuestStageRuntime, TaskGroupRuntime | 58 public UnityEvent fields |
| Type-Specific Save/Load Switching | CRITICAL | QuestSaveManager | 6+ task types, 2 switch statements |
| QuestRuntime God Object | CRITICAL | QuestRuntime.cs | 865 lines, 6 responsibilities |
| Odin Conditional Pollution | CRITICAL | Quest_SO.cs | 20+ #if blocks, 40% of code |
| Task Type Inheritance | HIGH | 12 task type files | Parallel hierarchy pattern |
| QuestSaveManager Size | HIGH | QuestSaveManager.cs | 840 lines, 6 massive methods |
| Stage Transition Coupling | HIGH | StageTransition.cs | 25 fields, mixed concerns |
| 4-Level Nesting | ARCHITECTURAL | All quest files | Quest → Stage → Group → Task |

---

### CRITICAL: Event Explosion (58 UnityEvents)

**Files:** QuestRuntime.cs (lines 22-77), QuestStageRuntime.cs (lines 19-71), TaskGroupRuntime.cs (lines 17-53)

**Problem:** QuestRuntime alone has 16 event types:
- OnQuestStarted, OnQuestCompleted, OnQuestFailed, OnQuestRestarted, OnQuestUpdated
- OnAnyTaskStarted, OnAnyTaskUpdated, OnAnyTaskCompleted, OnAnyTaskFailed
- OnStageEntered, OnStageCompleted, OnStageTransition
- OnChoicesAvailable, OnChoiceMade, OnChoiceAvailabilityChanged

**Impact:**
- Steep learning curve for new developers
- Memory overhead from 58 delegate list allocations
- UI binding nightmare for designers

**Suggested Fix:**
```csharp
public struct QuestEventData {
    public QuestRuntime quest;
    public QuestEventType type;
    public object context;
}
public UnityEvent<QuestEventData> OnQuestChanged = new();
```

---

### CRITICAL: Type-Specific Save/Load Switching

**File:** QuestSaveManager.cs (lines 541-590, 713-795)

**Problem:** Two 50-80 line switch statements handling 6 task types:
```csharp
case IntTaskRuntime intTask:
    snapshot.ProgressData.IntValue = intTask.CurrentCount;
    break;
case TimedTaskRuntime timedTask:
    snapshot.ProgressData.FloatValue = timedTask.RemainingTime;
    break;
// ... repeated 6 times in TWO places
```

**Impact:** Adding new task type requires modifying two methods in unrelated file. Violates Open/Closed Principle.

**Suggested Fix:** Polymorphic pattern - add to TaskRuntime:
```csharp
public abstract void CaptureSnapshot(TaskSnapshot snapshot);
public abstract void RestoreSnapshot(TaskSnapshot snapshot);
```

---

### CRITICAL: QuestRuntime God Object (865 lines)

**File:** QuestRuntime.cs

**Problem:** Single class manages:
- Quest lifecycle (start, complete, fail, reset)
- Stage transitions (6+ methods)
- Player choice logic (8+ methods)
- Event subscriptions (12+ handlers)
- Condition checking (3+ methods)
- Task management (3+ methods)

**Impact:** Any quest feature change risks side effects across entire class.

**Suggested Fix:** Extract into focused classes:
1. `QuestLifecycleManager` - start/complete/fail/reset
2. `QuestStageManager` - stage transitions
3. `QuestChoiceManager` - player choice logic
4. `QuestEventManager` - subscriptions

---

### CRITICAL: Odin Conditional Pollution

**File:** Quest_SO.cs (20+ #if ODIN_INSPECTOR blocks)

**Problem:**
```csharp
#if ODIN_INSPECTOR
[TabGroup("Tabs", "Configuration", SdfIconType.GearFill, Order = 1)]
[BoxGroup(TAB_CONFIG + "/Identity")]
[LabelText("Dev Name"), PropertyOrder(0)]
#endif
[SerializeField] private string devName;
// Repeats 30+ times
```

**Impact:** Code-to-comment ratio is nearly 1:1. Hard to read actual data structure.

**Suggested Fix:** Move ALL Odin attributes to Quest_SO.Odin.cs (already exists but conditionals remain in base).

---

### HIGH: Task Type Inheritance Hierarchy

**Files:** 12 files (6 Task_SO + 6 TaskRuntime classes)

**Problem:** Parallel hierarchy:
```
Task_SO → TaskInt_SO, TaskTimed_SO, TaskLocation_SO, TaskBool_SO, TaskString_SO, TaskDiscovery_SO
TaskRuntime → IntTaskRuntime, TimedTaskRuntime, LocationTaskRuntime, BoolTaskRuntime, StringTaskRuntime, DiscoveryTaskRuntime
```

Every new task type requires: 2 new classes + 2 switch statement updates + tests.

**Suggested Fix:** Data-driven approach or composition over inheritance.

---

### HIGH: QuestSaveManager Size (840 lines)

**File:** QuestSaveManager.cs

**Problem:** 6 massive methods each doing multiple responsibilities:
- `CaptureSnapshot()`: 50+ lines
- `RestoreSnapshot()`: 40+ lines
- `ValidateSnapshot()`: 60+ lines
- `RestoreQuests()`: 55+ lines
- `RestoreTaskStates()`: 80+ lines

**Suggested Fix:** Decompose into:
1. `SnapshotValidator` class
2. `SnapshotRestorer` class
3. `SnapshotCapture` static utility
4. `QuestSaveManager` as thin facade (100-150 lines)

---

### HIGH: StageTransition Mixed Concerns (25 fields)

**File:** StageTransition.cs (312 lines)

**Problem:** Single class handles:
- Basic transitions (OnGroupsComplete, OnTasksComplete)
- Conditional transitions (OnConditionsMet)
- Player choice transitions (isPlayerChoice, choiceId, choiceText, choiceIcon)
- World flag modifications

**Impact:** Unrelated concerns bundled. Player choice fields appear even when unused.

**Suggested Fix:** Separate into two classes:
```csharp
public class StageTransition { /* automatic transitions */ }
public class PlayerChoice : StageTransition { /* choice-specific */ }
```

---

### ARCHITECTURAL: 4-Level Nesting

```
QuestRuntime (865 lines)
├── QuestStageRuntime (453 lines)
│   ├── TaskGroupRuntime (357 lines)
│   │   └── TaskRuntime (250 lines)
```

**Impact:** Completing a task requires 7 method calls across 3 files:
1. Task.CompleteTask() → OnTaskCompleted
2. TaskGroupRuntime.HandleTaskCompleted() → checks group
3. OnGroupCompleted
4. QuestStageRuntime.HandleGroupCompleted() → checks stage
5. OnStageCompleted
6. QuestRuntime.HandleStageCompleted() → checks terminal
7. CompleteQuest()

**Suggested Fix:** Central event dispatcher or flatten hierarchy.

---

### MEDIUM: HelloDev Package Issues

#### UIContainer Transform Depth Calculation
**File:** UIContainer.cs (lines 37-71)
**Problem:** O(n * m) complexity recalculating transform depth every call.
**Fix:** Cache depths in Dictionary.

#### CompositeCondition State Tracking
**File:** CompositeCondition_SO.cs (lines 20-150)
**Problem:** Evaluate() can be called multiple times per event.
**Fix:** Add dirty flag and cached result.

#### SceneLoader Method Overloading
**File:** SceneLoader.cs (lines 44-103)
**Problem:** 5+ parameter methods with duplication.
**Fix:** Use builder/configuration pattern.

---

### LOW: Registry Pattern Duplication

**Issue:** QuestRegistry and QuestLineRegistry have identical code (~450 lines each).

**Suggested Fix:** Generic `Registry<T>` base class would reduce to ~150 lines.

---

### LOW: Multiple State Enums

**Issue:** 4 separate state enums (QuestState, StageState, TaskGroupState, TaskState).

**Suggested Fix:** Unified `LifecycleState` enum or proper state machine pattern.

---

## 9. Refactoring Roadmap

### Phase 1: Quick Wins (Low Risk)
1. **Move Odin attributes** to .Odin.cs partial classes (already done for Quest_SO, extend to others)
2. ✅ **Cache transform depths** in UIContainer.cs - DONE
3. ✅ **Add polymorphic save/load** to TaskRuntime (eliminates switch statements) - DONE
4. **Extract Registry<T>** generic base class (deferred - structural differences between registries)

### Phase 2: Moderate Refactoring
1. ✅ **Decompose QuestSaveManager** into Validator, Restorer, Capturer - DONE (840→423 lines)
2. **Add dirty flag** to CompositeCondition for cached evaluation
3. **Use builder pattern** for SceneLoader
4. **Separate StageTransition** from PlayerChoice

### Phase 3: Architectural Changes (High Impact)
1. **Consolidate events** into single QuestEventData pattern
2. **Split QuestRuntime** into focused managers
3. **Flatten hierarchy** or add central event dispatcher
4. **Consider composition** for task types

### Priority Recommendations
| Change | Risk | Impact | Recommended Order |
|--------|------|--------|-------------------|
| Polymorphic save/load | Low | High | 1st |
| Decompose QuestSaveManager | Low | High | 2nd |
| Move Odin to partials | Low | Medium | 3rd |
| Registry<T> base | Low | Low | 4th |
| Consolidate events | Medium | High | 5th |
| Split QuestRuntime | High | High | 6th |

---

## 10. Improvements Made This Session

| Improvement | Description |
|-------------|-------------|
| WorldFlagRegistry_SO | Auto-discovery for world flags with editor button |
| ValidateSnapshot API | Pre-load validation with detailed error messages |
| OnQuestDataChanged | Single aggregate event for all quest state changes |
| WorldFlag GUID | Stable identification instead of asset names |
| Task Restoration | TimedTask.SetRemainingTime(), StringTask.SetValue() |
| QuestLine Restoration | RestoreState() method for proper state recovery |
| Complexity Analysis | Comprehensive review of codebase overcomplexity |
| **Polymorphic Save/Load** | Abstract CaptureProgress/RestoreProgress in TaskRuntime - eliminates switch statements |
| **QuestSaveManager Decomposition** | Split into SnapshotCapturer, SnapshotRestorer, SnapshotValidator (840→423 lines) |
| **UIContainer Caching** | Transform depth caching to eliminate O(n*m) recalculation |

---

## 11. Files Modified/Created

### New Files
- `Runtime/Scripts/Core/SaveLoad/WorldFlagRegistry_SO.cs` - Auto-discovery registry
- `Runtime/Scripts/Core/SaveLoad/SnapshotValidationResult.cs` - Validation result class
- `Runtime/Scripts/Core/SaveLoad/QuestDataChangeType.cs` - Change type enum
- `Runtime/Scripts/Core/SaveLoad/SnapshotCapturer.cs` - Capture utility class
- `Runtime/Scripts/Core/SaveLoad/SnapshotRestorer.cs` - Restore utility class
- `Runtime/Scripts/Core/SaveLoad/SnapshotValidator.cs` - Validation utility class

### Modified Files
- `Runtime/Scripts/Core/QuestManager.cs` - Added OnQuestDataChanged event
- `Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs` - Refactored to use utilities (840→423 lines)
- `Runtime/Scripts/Core/Tasks/TaskRuntime.cs` - Added abstract CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/Tasks/IntTaskRuntime.cs` - Implemented CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/Tasks/BoolTaskRuntime.cs` - Implemented CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/Tasks/TimedTaskRuntime.cs` - SetRemainingTime() + CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/Tasks/LocationTaskRuntime.cs` - Implemented CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/Tasks/StringTaskRuntime.cs` - Implemented CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/Tasks/DiscoveryTaskRuntime.cs` - Implemented CaptureProgress/RestoreProgress
- `Runtime/Scripts/Core/WorldFlags/WorldFlagBase_SO.cs` - GUID field
- `Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs` - RestoreState()
- `Runtime/Scripts/Core/SaveLoad/QuestSystemSnapshot.cs` - HasStarted, condition indices
- `Assets/HelloDev/com.hellodev.ui/Runtime/Scripts/UIContainer.cs` - Transform depth caching

---

## 12. Overcomplicated Features Summary

This section provides a concise summary of features that are more complex than necessary, suitable for inclusion in a "pain points" document.

### Immediately Addressable (Low Risk)

| Feature | Why It's Overcomplicated | Suggested Simplification |
|---------|-------------------------|--------------------------|
| **Task Save/Load** | ~~Type-specific switch statements in unrelated file~~ | ✅ FIXED - Polymorphic pattern |
| **QuestSaveManager** | ~~840 lines, 6 responsibilities in one class~~ | ✅ FIXED - Decomposed to 423 lines |
| **UIContainer Depth** | ~~O(n*m) recalculation every call~~ | ✅ FIXED - Dictionary caching |
| **Odin Attributes** | 20+ #if blocks interleaved with code | Move to .Odin.cs partial classes |

### Medium Term (Moderate Risk)

| Feature | Why It's Overcomplicated | Suggested Simplification |
|---------|-------------------------|--------------------------|
| **Event Explosion** | 58 UnityEvent fields across quest classes | Single `OnQuestChanged(QuestEventData)` pattern |
| **Registry Duplication** | QuestRegistry & QuestLineRegistry identical | Generic `Registry<T>` base class |
| **StageTransition** | 25 fields mixing automatic + player choice | Separate into two focused classes |
| **State Enums** | 4 nearly-identical state enums | Unified `LifecycleState` or state machine |

### Architectural (High Risk, Future Work)

| Feature | Why It's Overcomplicated | Suggested Simplification |
|---------|-------------------------|--------------------------|
| **QuestRuntime** | 865 lines, God Object with 6 responsibilities | Split into QuestLifecycleManager, StageManager, ChoiceManager, EventManager |
| **4-Level Nesting** | Quest→Stage→Group→Task, 7 method calls to complete task | Central event dispatcher or flatter hierarchy |
| **Task Inheritance** | 12 files (6 SO + 6 Runtime) per task type | Data-driven approach or composition |

### Key Metrics

| Metric | Before Refactoring | After Refactoring |
|--------|-------------------|-------------------|
| QuestSaveManager lines | 840 | 423 |
| Type-specific switch statements | 2 | 0 |
| UIContainer complexity | O(n*m) | O(1) cached |
| Task types requiring save/load changes | QuestSaveManager.cs | Each TaskRuntime (polymorphic) |

### Remaining Pain Points

1. **Adding a new task type** still requires 2 new classes (but no longer requires modifying QuestSaveManager)
2. **Event subscriptions** still require subscribing to multiple events
3. ~~**Odin attributes** still pollute base classes~~ ✅ CONSOLIDATED - attributes now single-line, ~130 lines saved across 5 files
4. **QuestRuntime** is still a 865-line God Object

### Odin Attribute Consolidation Details

Files consolidated (attributes moved to single lines):
- `QuestStage.cs`: 442 → 408 lines (-34)
- `StageTransition.cs`: 313 → 279 lines (-34)
- `TaskGroup.cs`: 162 → 150 lines (-12)
- `Quest_SO.cs`: 367 → 339 lines (-28)
- `Task_SO.cs`: 219 → 197 lines (-22)

**Total: ~130 lines saved (~10% reduction)**

---

*Updated 2025-12-29*
