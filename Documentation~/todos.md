# Quest System TODOs & Gaps

*Last Updated: 2026-01-04*

## Overview

This document tracks incomplete implementations, missing features, and known issues.

---

## Open Issues

### 1. Tests Are Empty Stubs

**Severity:** Medium
**Files:**
- `Assets/com.hellodev.questsystem/Tests/Runtime/QuestSystemTests.cs`
- `Assets/com.hellodev.questsystem/Tests/Editor/QuestEditorTests.cs`

**Current State:**
All test methods contain only comments, no actual test code.

**Required:**
Implement actual unit tests for:
- Quest creation and initialization
- State transitions (NotStarted → InProgress → Completed/Failed)
- Task progression (increment, decrement, complete)
- Stage transitions and branching
- World flag modifications
- Condition evaluation
- Save/Load snapshots

---

### 2. Items Table Import Error

**Severity:** Low
**Issue:** The Items localization table was imported with Discovery_ entries instead of Item_ entries.
**Impact:** `SO_ID_Item_StolenCrate` has no localized display name.
**Fix:** Reimport Items.csv to the Items table.

---

### 3. Quest Categories/Filtering API Missing

**Severity:** Low
**Issue:** `QuestType_SO` exists but no filtering API in QuestManager.

**Suggested API:**
```csharp
// 📁 Runtime/Scripts/Core/QuestManager.cs
List<QuestRuntime> GetQuestsByType(QuestType_SO type);
List<QuestRuntime> GetActiveQuestsByType(QuestType_SO type);
```

---

## Architectural Gaps

*Based on validation against the 5 Architectural Tips (see `Assets/Docs/5 archtechtural tips.txt`)*

### TIP 1: Core Interfaces ✅ COMPLETE

**Current Score:** 8.5/10 (improved from 7/10)

**Implemented (2026-01-04):**

| Interface | Location | Implemented By |
|-----------|----------|----------------|
| `ITask` | `Runtime/Scripts/Core/Interfaces/ITask.cs` | `TaskRuntime` |
| `IQuest` | `Runtime/Scripts/Core/Interfaces/IQuest.cs` | `QuestRuntime` |
| `ITaskGroup` | `Runtime/Scripts/Core/Interfaces/ITaskGroup.cs` | `TaskGroupRuntime` |

**Usage Notes:**
- Interfaces are for **testability** and **contracts**, not for replacing concrete types everywhere
- Existing code (UI, SaveLoad) intentionally uses concrete types because:
  - UI needs `.Data` (ScriptableObject) for localization
  - SaveLoad needs `CaptureProgress()` which is save-specific
  - Events are typed with concrete classes (`UnityEvent<TaskRuntime>`)
- Use interfaces in **new code** and **unit tests** where appropriate

**Still Available (Low Priority):**

| Missing Interface | Purpose | Benefit |
|-------------------|---------|---------|
| `IRewardType` | Contract for reward processing | Enables custom reward systems |
| `IGameEvent` | Contract for event operations | Allows non-ScriptableObject events |

**All Interfaces:**
- `ICondition`, `IConditionEventDriven` - Condition system
- `IQuestRegistry`, `IQuestLineRegistry` - Internal registries
- `IBootstrapInitializable`, `ISaveDataProvider` - Utility contracts
- `ITask`, `IQuest`, `ITaskGroup` - Core runtime contracts ✅

---

### TIP 4: Event-Driven Architecture Gaps

**Current Score:** 9/10 (improved from 8.5/10)

#### 4.1 OnChoiceAvailabilityChanged ✅ COMPLETE

**Implemented:** 2026-01-04
**File:** `Runtime/Scripts/Core/Quests/QuestRuntime.cs`

**Changes:**
- Added `_choiceAvailabilityCache` dictionary to track availability state
- `SubscribeToPlayerChoiceConditions()` now captures initial state and uses shared handler
- New `HandleChoiceConditionChanged()` method re-evaluates ALL choices on any condition fire
- Event fires for both `true` (became available) and `false` (became unavailable)
- Implicit choice selection still works correctly

#### 4.2 World Flags Event-Driven ✅ ALREADY COMPLETE

**Status:** Already implemented - documentation was incorrect.

**Files:**
- `com.hellodev.conditions/Runtime/Scripts/Types/ConditionWorldFlagBool_SO.cs`
- `com.hellodev.conditions/Runtime/Scripts/Types/ConditionWorldFlagInt_SO.cs`

**Implementation:**
- `ConditionWorldFlagBool_SO` implements `IConditionEventDriven`
- Subscribes to `WorldFlagBoolRuntime.OnValueChanged`
- Has proper multi-subscriber support with reference counting
- Same pattern for `ConditionWorldFlagInt_SO`

#### 4.3 QuestLine Prerequisites Are Polled

**Severity:** Low
**File:** `Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs`

**Issue:** `CheckPrerequisite()` polls state instead of reacting to events when prerequisite questline completes.

**Fix:** Subscribe to `prerequisiteLine.OnQuestLineCompleted` and cache availability.

#### 4.4 No Events for Individual Quest Restoration

**Severity:** Low
**File:** `Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs`

**Issue:** `OnAfterLoad` fires once for entire load, but no event fires per-quest during restoration.

**Suggested API:**
```csharp
public event Action<QuestRuntime> OnQuestRestored;
```

#### 4.5 No Global World Flag Change Event

**Severity:** Low
**File:** `com.hellodev.conditions/Runtime/Scripts/WorldFlags/WorldFlagManager.cs`

**Issue:** Individual flags fire `OnValueChanged`, but no manager-level event aggregates all flag changes.

**Suggested API:**
```csharp
public event Action<WorldFlagBase_SO, object> OnAnyFlagChanged;
```

---

### TIP 5: Registry Pattern Deviations

**Current Score:** 7/10

The Quest System uses a centralized manager-driven approach rather than self-registration. This is intentional for save/load persistence but deviates from TIP 5.

#### 5.1 No Self-Registration Pattern

**Issue:** QuestRuntime/QuestLineRuntime do not register themselves. QuestManager explicitly adds/removes.

**Current Flow:**
```
QuestManager.AddQuest() → _questRegistry.AddActive(quest)
```

**TIP 5 Pattern (not implemented):**
```csharp
// In factory method
public QuestRuntime GetRuntimeQuest()
{
    var quest = new QuestRuntime(this);
    QuestRegistry.Register(quest);  // Self-registration
    return quest;
}
```

**Note:** Current approach is intentional for deterministic save/load. Consider hybrid if needed.

#### 5.2 No Selection Strategy Delegates

**Severity:** Low
**File:** `Runtime/Scripts/Core/Internal/IQuestRegistry.cs`

**Issue:** Registries only provide `GetAll*()` methods. No delegate-based selection.

**Suggested API:**
```csharp
public delegate QuestRuntime QuestSelectionStrategy(IEnumerable<QuestRuntime> quests);

// In IQuestRegistry
QuestRuntime SelectActive(QuestSelectionStrategy strategy);
```

#### 5.3 Registry Is Internal Only

**Severity:** Low
**Files:** `Runtime/Scripts/Core/Internal/QuestRegistry.cs`, `IQuestRegistry.cs`

**Issue:** Registries are `internal`, accessible only via QuestManager. Limits direct querying for advanced use cases.

**Consider:** Exposing `IQuestRegistry` publicly for read-only queries while keeping mutation internal.

#### 5.4 No GetFirst() Convenience Method

**Severity:** Low

**Issue:** Must use `GetAllActive().FirstOrDefault()` instead of simple `GetFirst()`.

**Suggested API:**
```csharp
QuestRuntime GetFirstActive();
QuestRuntime GetFirstActive(Func<QuestRuntime, bool> predicate);
```

---

## Future Features

### Quest Graph Tool ✅ COMPLETE (v1.4)
- Visual node-based editor using Unity Graph Toolkit 0.4.0-exp.2
- Visualize stages, task groups, dependencies, quest chains, questlines
- Node types: QuestStartNode, StageNode, ChoiceNode, TaskNode, TaskGroupNode, QuestRefNode
- Subgraph support for reusable components (StageGraph, TaskGroupGraph)
- ScriptedImporter auto-converts .quest/.questline files to Quest_SO/QuestLine_SO
- Validation system with reachability analysis
- USS styling for visual differentiation
- See `docs/questsystem/quest-graph-editor-guide.md` and `quest-graph-designer-workflow.md`

### Dialogue Integration
- `IDialogueIntegration` interface for third-party dialogue systems
- Quest stages settable from dialogue scripts
- See implementation-plan.md Phase 8.2

### Quest Tracking (Distance/Direction)
- `IQuestTracker` interface for waypoint/compass integration
- Distance to objective, direction arrows
- See comprehensive-critique.md Part IV

---

## Completed Features

| Feature | Date | Location |
|---------|------|----------|
| Quest Graph Editor v1.4 (Phases 1-7) | 2026-01-04 | `Editor/Graphs/` |
| Graph Validation System | 2026-01-04 | `Editor/Graphs/Scripts/Validation/` |
| Graph → ScriptableObject Conversion | 2026-01-04 | `Editor/Graphs/Scripts/Converters/` |
| ScriptedImporters for .quest/.questline | 2026-01-04 | `Editor/Graphs/Scripts/Importers/` |
| Designer Workflow Documentation | 2026-01-04 | `docs/questsystem/quest-graph-designer-workflow.md` |
| OnChoiceAvailabilityChanged event | 2026-01-04 | `Runtime/Scripts/Core/Quests/QuestRuntime.cs` |
| Core Interfaces (ITask, IQuest, ITaskGroup) | 2026-01-04 | `Runtime/Scripts/Core/Interfaces/` |
| Save/Load System | 2025-12-29 | `Runtime/Scripts/Core/SaveLoad/` |
| World State Flags | 2025-12-28 | `com.hellodev.conditions/Runtime/Scripts/WorldFlags/` |
| Branching/Player Choices | 2025-12-28 | `Runtime/Scripts/Core/Stages/StageTransition.cs` |
| Quest Stages | 2025-12-27 | `Runtime/Scripts/Core/Stages/` |
| QuestManager SRP Split | 2025-12-23 | `Runtime/Scripts/Core/QuestManager.cs` |
| QuestLines | 2025-12-24 | `Runtime/Scripts/Core/QuestLines/` |
| Quest Chains (ConditionQuestState_SO) | 2025-12-23 | `Runtime/Scripts/Core/Conditions/` |
| Task Groups | 2025-12-22 | `Runtime/Scripts/Core/TaskGroups/` |
| Rewards auto-distribution | 2025-12-21 | `Runtime/Scripts/Core/Quests/QuestRuntime.cs` |
| GlobalTaskFailureConditions | 2025-12-21 | `Runtime/Scripts/Core/ScriptableObjects/Quest_SO.cs` |
| UnsubscribeFromQuestEvents | 2025-12-21 | `Runtime/Scripts/Core/QuestManager.cs` |
| StringTask implementation | 2025-12-21 | `Runtime/Scripts/Core/Tasks/StringTaskRuntime.cs` |
| LocationTask | 2025-12-21 | `Runtime/Scripts/Core/Tasks/LocationTaskRuntime.cs` |
| TimedTask | 2025-12-21 | `Runtime/Scripts/Core/Tasks/TimedTaskRuntime.cs` |
| DiscoveryTask | 2025-12-21 | `Runtime/Scripts/Core/Tasks/DiscoveryTaskRuntime.cs` |

---

## Version History

| Version | Date | Major Changes |
|---------|------|---------------|
| 3.6.0 | 2026-01-04 | Quest Graph Editor v1.4 (Phases 5-7 complete) |
| 3.5.1 | 2026-01-03 | Logger ScriptableObject configuration |
| 3.5.0 | 2026-01-02 | Multi-subscriber condition support |
| 3.3.0 | 2025-12-30 | Autosave, slot metadata |
| 3.1.0 | 2025-12-29 | Save/Load system complete |
| 3.0.0 | 2025-12-28 | Stages, branching, world flags |
| 2.0.0 | 2025-12-24 | QuestLines, AAA inspectors |
| 1.2.0 | 2025-12-22 | Task Groups |
| 1.0.0 | 2025-12-21 | Initial release |
