# HelloDev Quest System: Implementation Plan

**Last Updated:** 2026-01-14
**Current Version:** 3.10.0

---

## Executive Summary

This document tracks the implementation status of the HelloDev Quest System. Phase 7 core features (Stages, Branching, World State, Save/Load) and Phase 8.1 Quest Graph Tool are complete. Recent additions include TransitionNode for configurable stage transitions (3.10.0), QuestChoiceNode for questline branching (3.9.0), and Graph Node UX improvements (3.8.0). The next priorities are Dialogue Integration, Quest Categories/Filtering API, and an extensible architecture for tutorials/achievements.

---

## Phase 7: Core Features - Status

### 7.0 QuestManager SRP Split ✅ COMPLETED (2025-12-23)

**Objective:** Split QuestManager into focused subsystems.

**Final State:**
```
QuestManager.cs (~527 lines, facade)
QuestManager.Editor.cs (~300 lines, Odin inspector)
├── QuestRegistry (internal, quest storage)
├── QuestLineRegistry (internal, questline storage)
└── QuestQueryService (internal, queries)
```

---

### 7.1 Quest Stages ✅ COMPLETED (2025-12-27)

**Objective:** Add stage layer between Quest and TaskGroups for Skyrim-style structure.

**Implementation:**
- `QuestStage` - Stage data with task groups, transitions, journal entries
- `StageTransition` - Defines how stages connect (OnComplete, OnCondition, PlayerChoice)
- `StageTrigger` - Trigger enum for transitions
- Non-sequential stage indices (0, 10, 20, 100) for flexible design
- Backward compatible: legacy quests auto-wrap in single stage

**Files:**
- `Runtime/Scripts/Core/Stages/QuestStage.cs`
- `Runtime/Scripts/Core/Stages/StageTransition.cs`
- `Runtime/Scripts/Core/Stages/StageTrigger.cs`

---

### 7.2 Branching Quest Support ✅ COMPLETED (2025-12-28)

**Objective:** Enable player choices that affect quest progression.

**Implementation:**
- `isPlayerChoice` flag on transitions for explicit choices
- `choiceId` for unique choice identification
- Choice gating via conditions (e.g., reputation requirements)
- `WorldFlagsOnSelect` - apply world flag modifications when choice is made
- Branch decision tracking (`BranchDecisions` dictionary)

**Events:**
- `OnChoicesAvailable` - fired when stage has player choices
- `OnChoiceMade` - fired when player selects a choice
- `OnChoiceAvailabilityChanged` - fired when choice conditions change

**API:**
```csharp
var choices = quest.GetAvailableChoices();
quest.SelectChoice(choice);
quest.SelectChoiceById("combat_path");
```

**Example Quest:** "The Merchant's Dilemma" demonstrates all branching features.

---

### 7.3 World State Flags ✅ COMPLETED (2025-12-28)

**Objective:** Track persistent game state for cross-quest consequences.

**Implementation (ScriptableObject-based):**

The original plan proposed an interface-based approach (`IWorldState`, `WorldStateProvider`). The actual implementation uses self-contained ScriptableObjects, which is more designer-friendly and consistent with HelloDev's data-driven philosophy.

**Files (in com.hellodev.conditions package):**
- `WorldFlagBase_SO` - Abstract base class
- `WorldFlagBool_SO` - Boolean flags (met_king, chose_evil_path)
- `WorldFlagInt_SO` - Integer flags with min/max (reputation, kill_count)
- `WorldFlagModification` - Defines how to modify flags (Set, Add, Subtract)
- `ConditionWorldFlagBool_SO` - Check boolean flag values
- `ConditionWorldFlagInt_SO` - Check integer flags with comparisons

**Usage:**
```csharp
// In quest transitions
[SerializeField] private List<WorldFlagModification> worldFlagsOnSelect;

// Apply on choice
foreach (var mod in worldFlagsOnSelect)
    mod.Apply();

// Check in conditions
[SerializeField] private ConditionWorldFlagInt_SO highRepCondition;
if (highRepCondition.Evaluate()) { /* unlock special dialogue */ }
```

**Design Decision:** ScriptableObject approach chosen over interface for:
- Designer-friendly (drag-drop in inspector)
- Self-contained (each flag stores its own value)
- Event-driven (fires events on change)
- Consistent with HelloDev patterns

---

### 7.4 Save/Load System ✅ COMPLETED (2025-12-29)

**Objective:** Persist quest and world state.

**Implementation:**
- `QuestSaveManager` singleton for managing save/load operations
- `ISaveDataProvider` interface for custom save system integration (Asset Store ready)
- `JsonFileSaveProvider` default implementation using JSON files
- Async API for non-blocking operations

**Features:**
- Quest state serialization (active, completed, failed)
- Stage and task progress (type-specific: IntTask count, TimedTask remaining, etc.)
- Branch decisions tracking
- World flag values (boolean and integer)
- QuestLine progress
- Save slot metadata for UI
- Events: `OnBeforeSave`, `OnAfterSave`, `OnBeforeLoad`, `OnAfterLoad`

**Files created:**
- `Runtime/Scripts/Core/SaveLoad/QuestSystemSnapshot.cs`
- `Runtime/Scripts/Core/SaveLoad/ISaveDataProvider.cs`
- `Runtime/Scripts/Core/SaveLoad/JsonFileSaveProvider.cs`
- `Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs`

**API:**
```csharp
// 📁 Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs

// Setup (via locator pattern or direct)
var saveManager = questSaveLocator.Manager;
// Or: QuestSaveManager.Instance

// Save/Load
await saveManager.SaveAsync("slot_1");
await saveManager.LoadAsync("slot_1");

// Manual snapshots
var snapshot = saveManager.CaptureSnapshot();
saveManager.RestoreSnapshot(snapshot);

// Delete slot
await saveManager.DeleteSlotAsync("slot_1");

// Get slot metadata
var metadata = await saveManager.GetSlotMetadataAsync("slot_1");
```

---

## Phase 8: Medium Priority Features

### 8.1 Quest Graph Tool ✅ COMPLETED (2026-01-04)

**Objective:** Visual node-based editor for quest design.

**Implementation:**
- Uses Unity Graph Toolkit 0.4.0-exp.2 (Unity 6.2+)
- Node types: QuestStartNode, StageNode, ChoiceNode, TaskNode, TaskGroupNode, QuestRefNode
- Subgraph support for reusable components (StageGraph, TaskGroupGraph)
- ScriptedImporter converts .quest/.questline files to Quest_SO/QuestLine_SO
- Validation system with reachability analysis
- USS styling for visual differentiation

**Files:**
- `Editor/Graphs/Scripts/Nodes/` - All node types
- `Editor/Graphs/Scripts/Converters/` - Graph → ScriptableObject conversion
- `Editor/Graphs/Scripts/Importers/` - ScriptedImporter for .quest/.questline
- `Editor/Graphs/Scripts/Validation/` - Validation system

**Documentation:**
- `Documentation/quest-graph-editor-guide.md`
- `Documentation/quest-graph-creation-reference.md`
- `Documentation/Tutorials/tutorial-graph-editor.md`

### 8.2 Dialogue Integration Points
Create `IDialogueIntegration` interface for third-party dialogue systems.

### 8.3 Quest Categories/Filtering
Add filtering API to QuestManager:
```csharp
// 📁 Runtime/Scripts/Core/QuestManager.cs
List<QuestRuntime> GetQuestsByType(QuestType_SO type);
List<QuestRuntime> GetActiveQuestsByType(QuestType_SO type);
```

### 8.4 New Quest States
Add `Locked` and `Hidden` to `QuestState` enum.

---

## Phase 9: Low Priority Features

- Choice Rewards (different rewards per branch)
- Contextual Journal (journal adapts to player choices)
- Quest Metadata (difficulty, estimated time, etc.)
- Failure Hints (guidance when quest fails)
- Memory Optimization (pooling, lazy loading)

---

## Success Criteria Summary

| Phase | Status | Date |
|-------|--------|------|
| 7.0 QuestManager Split | ✅ Complete | 2025-12-23 |
| 7.1 Quest Stages | ✅ Complete | 2025-12-27 |
| 7.2 Branching Support | ✅ Complete | 2025-12-28 |
| 7.3 World State Flags | ✅ Complete | 2025-12-28 |
| 7.4 Save/Load System | ✅ Complete | 2025-12-29 |
| 8.1 Quest Graph Tool | ✅ Complete | 2026-01-04 |
| 8.1.1 Native Subgraph Migration | ✅ Complete | 2026-01-05 |
| 8.1.2 Graph Node UX Improvements | ✅ Complete | 2026-01-11 |
| 8.1.3 QuestChoiceNode | ✅ Complete | 2026-01-11 |
| 8.1.4 TransitionNode & Port Capacity | ✅ Complete | 2026-01-14 |
| 8.2 Dialogue Integration | 🔲 Not Started | - |
| 8.3 Quest Categories API | 🔲 Not Started | - |
| 8.4 Extensible Architecture | 🔲 Not Started | - |

---

## Next Steps

1. **Dialogue Integration (8.2)** - Create `IDialogueIntegration` interface for third-party dialogue systems
   - Quest stages settable from dialogue scripts
   - Integrate with popular dialogue assets

2. **Quest Categories/Filtering (8.3)** - Add filtering API to QuestManager
   - `GetQuestsByType(QuestType_SO type)`
   - `GetActiveQuestsByType(QuestType_SO type)`

3. **Extensible Architecture (8.4)** - AAA-quality modular design for reuse
   - Abstract "Objective System" usable for quests, tutorials, achievements
   - Generic "Progress Tracker" pattern
   - Plugin architecture for custom task types
   - Decoupled UI layer with clear contracts

4. **Unit Tests** - Implement actual test code (currently stubs)
   - Quest creation and state transitions
   - Task progression
   - Save/Load snapshots

---

*Implementation plan maintained since 2025-12-28. Updated 2026-01-14.*
