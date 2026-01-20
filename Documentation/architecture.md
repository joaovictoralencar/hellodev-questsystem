# Quest System Architecture

*Last Updated: 2026-01-14*

## Hierarchy Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              QuestManager                                    │
│  (Singleton - Global entry point, owns all runtime quests and questlines)   │
│  📁 Runtime/Scripts/Core/QuestManager.cs                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                        QuestLineRuntime                                │  │
│  │  (Runtime instance - tracks progress across related quests)            │  │
│  │  📁 Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs                │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                          QuestRuntime                                  │  │
│  │  (Runtime instance - owns stages, manages quest state and branching)   │  │
│  │  📁 Runtime/Scripts/Core/Quests/QuestRuntime.cs                        │  │
│  ├───────────────────────────────────────────────────────────────────────┤  │
│  │                                                                        │  │
│  │  ┌─────────────────────────────────────────────────────────────────┐  │  │
│  │  │                        QuestStage                                │  │  │
│  │  │  (Stage data - defines task groups and transitions)              │  │  │
│  │  │  📁 Runtime/Scripts/Core/Stages/QuestStage.cs                    │  │  │
│  │  ├─────────────────────────────────────────────────────────────────┤  │  │
│  │  │                                                                  │  │  │
│  │  │  ┌───────────────────────────────────────────────────────────┐  │  │  │
│  │  │  │                  TaskGroupRuntime                          │  │  │  │
│  │  │  │  (Runtime group - owns tasks, manages group logic)         │  │  │  │
│  │  │  │  📁 Runtime/Scripts/Core/TaskGroups/TaskGroupRuntime.cs    │  │  │  │
│  │  │  ├───────────────────────────────────────────────────────────┤  │  │  │
│  │  │  │                                                            │  │  │  │
│  │  │  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │  │  │  │
│  │  │  │  │ TaskRuntime │ │ TaskRuntime │ │ TaskRuntime │          │  │  │  │
│  │  │  │  │  (Runtime)  │ │  (Runtime)  │ │  (Runtime)  │          │  │  │  │
│  │  │  │  └─────────────┘ └─────────────┘ └─────────────┘          │  │  │  │
│  │  │  │  📁 Runtime/Scripts/Core/Tasks/TaskRuntime.cs              │  │  │  │
│  │  │  └───────────────────────────────────────────────────────────┘  │  │  │
│  │  │                                                                  │  │  │
│  │  │  ┌───────────────────────────────────────────────────────────┐  │  │  │
│  │  │  │                  StageTransition                           │  │  │  │
│  │  │  │  (Defines how to move between stages)                      │  │  │  │
│  │  │  │  📁 Runtime/Scripts/Core/Stages/StageTransition.cs         │  │  │  │
│  │  │  └───────────────────────────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────────────────────────┘  │  │
│  │                                                                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                        QuestSaveManager                                │  │
│  │  (Singleton - handles save/load operations)                            │  │
│  │  📁 Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs                  │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Information Flow

### Events Bubble UP (Child → Parent)

```
TaskRuntime ──────► TaskGroupRuntime ──────► QuestRuntime ──────► QuestManager
  OnTaskUpdated       OnTaskInGroupUpdated     OnAnyTaskUpdated     QuestUpdated
  OnTaskCompleted     OnTaskInGroupCompleted   OnAnyTaskCompleted   (aggregated)
  OnTaskFailed        OnTaskInGroupFailed      OnAnyTaskFailed
  OnTaskStarted       OnGroupCompleted         OnQuestUpdated
                      OnGroupFailed            OnQuestCompleted
                      OnGroupStarted           OnQuestFailed
                                               OnQuestStarted
                                               OnQuestRestarted
                                               OnChoicesAvailable   ← Branching
                                               OnChoiceMade         ← Branching
                                               OnStageChanged       ← Stages
```

### Commands Flow DOWN (Parent → Child)

```
QuestManager ─────► QuestRuntime ─────► QuestStage ─────► TaskGroupRuntime ─────► TaskRuntime
  AddQuest()          StartQuest()       (data only)        StartGroup()           StartTask()
  CompleteQuest()     CompleteQuest()                       CompleteGroup()        CompleteTask()
  FailQuest()         FailQuest()                           FailGroup()            FailTask()
  RestartQuest()      ResetQuest()                          ResetGroup()           ResetTask()
  RemoveQuest()       SelectChoice()                                               IncrementStep()
                      GoToStage()                                                  DecrementStep()
```

---

## Script Responsibilities

### 1. TaskRuntime

**File:** `Runtime/Scripts/Core/Tasks/TaskRuntime.cs`

**Ownership:** Owned by TaskGroupRuntime

**Single Responsibility:** Manage individual task state and progress

**Responsibilities:**
- Track task state (NotStarted, InProgress, Completed, Failed)
- Track task progress (current step, required steps)
- Handle increment/decrement operations
- Subscribe to condition events for auto-completion
- Fire events when state changes

**Events to FIRE (notify parent):**
| Event | When Fired | Who Listens |
|-------|------------|-------------|
| `OnTaskStarted` | `StartTask()` called | TaskGroupRuntime |
| `OnTaskUpdated` | Progress changes | TaskGroupRuntime, UI |
| `OnTaskCompleted` | Task completes | TaskGroupRuntime |
| `OnTaskFailed` | Task fails | TaskGroupRuntime |

**Events to SUBSCRIBE TO:**
| Event | Source | Purpose |
|-------|--------|---------|
| Condition events | Task_SO.Conditions | Auto-completion triggers |

**NOT Responsible For:**
- ❌ Quest state
- ❌ Other tasks
- ❌ Group logic
- ❌ UI updates

---

### 2. TaskGroupRuntime

**File:** `Runtime/Scripts/Core/TaskGroups/TaskGroupRuntime.cs`

**Ownership:** Owned by QuestStage (via QuestRuntime)

**Single Responsibility:** Manage a group of tasks with execution mode logic

**Responsibilities:**
- Create runtime tasks from TaskGroup data
- Start tasks based on ExecutionMode (Sequential/Parallel/AnyOrder/OptionalXofY)
- Track group state (NotStarted, InProgress, Completed, Failed)
- Determine when group is complete (based on mode)
- Determine when group has failed (completion impossible)
- Forward task events to Quest with group context

**Events to FIRE (notify parent):**
| Event | When Fired | Who Listens |
|-------|------------|-------------|
| `OnGroupStarted` | `StartGroup()` called | QuestRuntime |
| `OnGroupCompleted` | Group completion criteria met | QuestRuntime |
| `OnGroupFailed` | Completion becomes impossible | QuestRuntime |
| `OnTaskInGroupUpdated` | Any task in group updates | QuestRuntime |
| `OnTaskInGroupCompleted` | Any task in group completes | QuestRuntime |
| `OnTaskInGroupFailed` | Any task in group fails | QuestRuntime |
| `OnTaskInGroupStarted` | Any task in group starts | QuestRuntime |

**Events to SUBSCRIBE TO:**
| Event | Source | Purpose |
|-------|--------|---------|
| `OnTaskCompleted` | TaskRuntime | Track completion, start next task |
| `OnTaskUpdated` | TaskRuntime | Forward to Quest |
| `OnTaskFailed` | TaskRuntime | Check if group failed |
| `OnTaskStarted` | TaskRuntime | Forward to Quest |

**NOT Responsible For:**
- ❌ Quest state
- ❌ Other groups
- ❌ Stage transitions
- ❌ Reward distribution
- ❌ UI updates

---

### 3. QuestStage

**File:** `Runtime/Scripts/Core/Stages/QuestStage.cs`

**Ownership:** Data class owned by Quest_SO, referenced by QuestRuntime

**Single Responsibility:** Define stage structure and transitions

**Data Fields:**
- `stageIndex` - Non-sequential index (0, 10, 20, 100...)
- `stageName` - Display name
- `taskGroups` - List of TaskGroup for this stage
- `transitions` - List of StageTransition defining exit paths
- `journalEntry` - Localized journal text for this stage
- `isTerminal` - Whether this stage ends the quest
- `isOptional` - Whether this stage can be skipped
- `isHidden` - Whether to hide from journal

**NOT a Runtime Class:** QuestStage is a serializable data class. Runtime logic is in QuestRuntime.

---

### 4. StageTransition

**File:** `Runtime/Scripts/Core/Stages/StageTransition.cs`

**Ownership:** Data class owned by QuestStage

**Single Responsibility:** Define how to transition between stages

**Data Fields:**
- `targetStageIndex` - Stage to transition to
- `trigger` - TransitionTrigger enum (OnGroupsComplete, OnConditionsMet, Manual, PlayerChoice)
- `conditions` - List of conditions that must be met
- `transitionLabel` - Optional developer-friendly label
- `priority` - Higher priority evaluated first (default: 0)
- `isPlayerChoice` - Whether this is a choice the player makes
- `choiceId` - Unique identifier for this choice
- `choiceText` - Localized text for UI
- `choiceIcon` - Optional icon for this choice
- `choiceTooltip` - Optional tooltip/description
- `worldFlagsOnSelect` - WorldFlagModifications to apply when selected

**TransitionTrigger Enum:**
```csharp
public enum TransitionTrigger
{
    OnGroupsComplete,   // All task groups in stage complete (default)
    OnConditionsMet,    // Transition when conditions are met
    Manual,             // Transition only via explicit API call
    PlayerChoice        // Player explicitly selects this path
}
```

---

### 5. QuestRuntime

**File:** `Runtime/Scripts/Core/Quests/QuestRuntime.cs`

**Ownership:** Owned by QuestManager

**Single Responsibility:** Manage quest lifecycle, stages, and branching

**Responsibilities:**
- Create TaskGroupRuntime instances from Quest_SO stage data
- Track quest state (NotStarted, InProgress, Completed, Failed)
- Track current stage and stage history
- Evaluate stage transitions when groups complete
- Handle player choices (expose available choices, process selection)
- Apply world flag modifications on choice selection
- Subscribe to start/failure conditions
- Distribute rewards on completion
- Aggregate task events for external listeners

**Events to FIRE (notify parent and external listeners):**
| Event | When Fired | Who Listens |
|-------|------------|-------------|
| `OnQuestStarted` | Quest starts | QuestManager, UI |
| `OnQuestCompleted` | Terminal stage reached | QuestManager, UI |
| `OnQuestFailed` | Any group fails critically | QuestManager, UI |
| `OnQuestRestarted` | Quest reset and started | QuestManager, UI |
| `OnQuestUpdated` | Any significant change | QuestManager, UI |
| `OnAnyTaskUpdated` | Any task updates | UI (details view) |
| `OnAnyTaskCompleted` | Any task completes | UI (details view) |
| `OnAnyTaskFailed` | Any task fails | UI (details view) |
| `OnAnyTaskStarted` | Any task starts | UI (details view) |
| `OnChoicesAvailable` | Stage has player choices | UI (choice dialog) |
| `OnChoiceMade` | Player selected a choice | UI, Game systems |
| `OnChoiceAvailabilityChanged` | Choice conditions changed | UI (choice dialog) |
| `OnStageChanged` | Transitioned to new stage | UI, Journal |

**Events to SUBSCRIBE TO:**
| Event | Source | Purpose |
|-------|--------|---------|
| `OnGroupCompleted` | TaskGroupRuntime | Evaluate transitions |
| `OnGroupFailed` | TaskGroupRuntime | Fail quest or handle |
| `OnTaskInGroup*` | TaskGroupRuntime | Forward to listeners |
| Start conditions | Condition_SO | Auto-start quest |
| Failure conditions | Condition_SO | Auto-fail quest |
| Global task failure | Condition_SO | Fail current tasks |
| Choice conditions | Condition_SO | Update choice availability |

**Choice API:**
```csharp
// Get available choices for current stage
List<StageTransition> GetAvailableChoices();

// Select a choice by transition reference
void SelectChoice(StageTransition choice);

// Select a choice by its choiceId
void SelectChoiceById(string choiceId);

// Get branch decisions made during this quest
Dictionary<string, string> BranchDecisions { get; }
```

**NOT Responsible For:**
- ❌ Other quests
- ❌ Quest database
- ❌ UI rendering
- ❌ Persistence/saving

---

### 6. QuestLineRuntime

**File:** `Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs`

**Ownership:** Owned by QuestManager

**Single Responsibility:** Track progress across a narrative arc of quests

**Responsibilities:**
- Track questline state (Locked, Available, InProgress, Completed, Failed)
- Monitor progress of contained quests
- Check prerequisite questlines
- Distribute completion rewards when all quests done

**Events to FIRE:**
| Event | When Fired | Who Listens |
|-------|------------|-------------|
| `OnQuestLineStarted` | First quest started | QuestManager |
| `OnQuestLineCompleted` | All quests completed | QuestManager, Achievements |
| `OnQuestLineFailed` | Any quest failed (if configured) | QuestManager |
| `OnQuestLineProgressChanged` | Quest added/completed | UI |

---

### 7. QuestManager (Singleton)

**Files:**
- `Runtime/Scripts/Core/QuestManager.cs` - Core runtime logic
- `Runtime/Scripts/Core/QuestManager.Editor.cs` - Odin inspector & debug UI

**Ownership:** Top-level, owns all runtime quests and questlines

**Single Responsibility:** Global quest lifecycle management and external API

**Responsibilities:**
- Maintain quest database (available quests, optional validation)
- Maintain questline database
- Track active, completed, and failed quests
- Provide API for quest operations (add, fail, restart, remove)
- Provide query methods (get quest, get quests by state, check status)
- Aggregate quest events for game systems
- Subscribe/unsubscribe to quest events properly

**Events to FIRE (notify game systems):**
| Event | When Fired | Who Listens |
|-------|------------|-------------|
| `QuestAdded` | Quest added to active | UI, Game systems |
| `QuestStarted` | Quest transitions to InProgress | UI, Game systems |
| `QuestCompleted` | Quest completed | UI, Game systems, Achievements |
| `QuestFailed` | Quest failed | UI, Game systems |
| `QuestRestarted` | Quest reset and restarted | UI |
| `QuestRemoved` | Quest removed from tracking | UI |
| `QuestUpdated` | Any quest progress change | UI (optional) |
| `QuestLineCompleted` | QuestLine all quests done | UI, Achievements |

**NOT Responsible For:**
- ❌ Individual task logic (in TaskRuntime)
- ❌ Group execution mode logic (in TaskGroupRuntime)
- ❌ Stage transition logic (in QuestRuntime)
- ❌ Condition evaluation
- ❌ UI rendering
- ❌ Task increment/decrement (use `quest.IncrementCurrentTask()`)

---

### 8. QuestSaveManager (Singleton)

**File:** `Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs`

**Ownership:** Top-level, manages persistence for quests

**Single Responsibility:** Save and load quest system state

**Responsibilities:**
- Capture QuestSystemSnapshot from current state
- Restore state from snapshot
- Interface with ISaveProvider for storage
- Manage save slots and metadata
- Handle autosave if configured

**Note:** Tutorial state is managed separately by `TutorialSaveManager` (for persistent storage via SaveService.Provider) or `TutorialManager.CaptureSnapshot()` / `RestoreSnapshot()` (for manual serialization). Both `TutorialManager` and `TutorialSaveManager` implement `IBootstrapInitializable` for coordinated initialization.

**Events to FIRE:**
| Event | When Fired | Who Listens |
|-------|------------|-------------|
| `OnBeforeSave` | About to capture snapshot | Game systems |
| `OnAfterSave` | Save completed | UI (confirmation) |
| `OnBeforeLoad` | About to restore snapshot | Game systems |
| `OnAfterLoad` | Load completed | UI, Game systems |

---

## Event Design Principles

### 1. Single Source of Truth
Each event should be fired from exactly ONE location.

```
BAD:  OnQuestUpdated fired from CompleteQuest(), HandleGroupCompleted(), HandleTaskInGroupUpdated()
GOOD: OnQuestUpdated fired only from dedicated method NotifyQuestUpdated()
```

### 2. Events Bubble Up Only
Children notify parents. Parents never fire events on children.

```
BAD:  QuestRuntime fires TaskRuntime.OnTaskCompleted
GOOD: QuestRuntime listens to TaskRuntime.OnTaskCompleted
```

### 3. Aggregate Events at Each Level
Each level should aggregate child events with context.

```
TaskRuntime fires: OnTaskCompleted(task)
TaskGroupRuntime fires: OnTaskInGroupCompleted(group, task)  ← adds group context
QuestRuntime fires: OnAnyTaskCompleted(task)                 ← removes group context for simplicity
```

### 4. Consistent Event Pairs
Every event type should have consistent start/complete/fail pairs.

```
Task:      OnTaskStarted, OnTaskCompleted, OnTaskFailed
Group:     OnGroupStarted, OnGroupCompleted, OnGroupFailed
Quest:     OnQuestStarted, OnQuestCompleted, OnQuestFailed
QuestLine: OnQuestLineStarted, OnQuestLineCompleted, OnQuestLineFailed
```

### 5. UI Subscribes to Highest Appropriate Level
UI should subscribe to the most relevant level for its needs.

```
Quest List UI    → QuestManager events (QuestAdded, QuestCompleted, etc.)
Quest Details UI → QuestRuntime events (OnQuestUpdated, OnAnyTaskCompleted, etc.)
Task Item UI     → TaskRuntime events (OnTaskUpdated, OnTaskCompleted, etc.)
Choice Dialog UI → QuestRuntime events (OnChoicesAvailable, OnChoiceAvailabilityChanged)
```

---

## Current Event Structure (Implemented)

### TaskRuntime.cs (4 events)
```csharp
// 📁 Runtime/Scripts/Core/Tasks/TaskRuntime.cs
// Lifecycle
public UnityEvent<TaskRuntime> OnTaskStarted = new();
public UnityEvent<TaskRuntime> OnTaskCompleted = new();
public UnityEvent<TaskRuntime> OnTaskFailed = new();

// Progress
public UnityEvent<TaskRuntime> OnTaskUpdated = new();
```

### TaskGroupRuntime.cs (7 events)
```csharp
// 📁 Runtime/Scripts/Core/TaskGroups/TaskGroupRuntime.cs
// Lifecycle
public UnityEvent<TaskGroupRuntime> OnGroupStarted = new();
public UnityEvent<TaskGroupRuntime> OnGroupCompleted = new();
public UnityEvent<TaskGroupRuntime> OnGroupFailed = new();

// Task forwarding (with group context)
public UnityEvent<TaskGroupRuntime, TaskRuntime> OnTaskInGroupStarted = new();
public UnityEvent<TaskGroupRuntime, TaskRuntime> OnTaskInGroupUpdated = new();
public UnityEvent<TaskGroupRuntime, TaskRuntime> OnTaskInGroupCompleted = new();
public UnityEvent<TaskGroupRuntime, TaskRuntime> OnTaskInGroupFailed = new();
```

### QuestRuntime.cs (13 events)
```csharp
// 📁 Runtime/Scripts/Core/Quests/QuestRuntime.cs
// Lifecycle
public UnityEvent<QuestRuntime> OnQuestStarted = new();
public UnityEvent<QuestRuntime> OnQuestCompleted = new();
public UnityEvent<QuestRuntime> OnQuestFailed = new();
public UnityEvent<QuestRuntime> OnQuestRestarted = new();

// Progress (consolidated via NotifyQuestUpdated())
public UnityEvent<QuestRuntime> OnQuestUpdated = new();

// Task aggregation (no group context - simplified for UI)
public UnityEvent<TaskRuntime> OnAnyTaskStarted = new();
public UnityEvent<TaskRuntime> OnAnyTaskUpdated = new();
public UnityEvent<TaskRuntime> OnAnyTaskCompleted = new();
public UnityEvent<TaskRuntime> OnAnyTaskFailed = new();

// Stages
public UnityEvent<QuestRuntime, int> OnStageChanged = new();

// Branching/Choices
public UnityEvent<QuestRuntime, List<StageTransition>> OnChoicesAvailable = new();
public UnityEvent<QuestRuntime, StageTransition> OnChoiceMade = new();
public UnityEvent<QuestRuntime> OnChoiceAvailabilityChanged = new();
```

### QuestManager.cs (8 events)
```csharp
// 📁 Runtime/Scripts/Core/QuestManager.cs
// Quest lifecycle (aggregated from all quests)
public UnityEvent<QuestRuntime> QuestAdded = new();
public UnityEvent<QuestRuntime> QuestStarted = new();
public UnityEvent<QuestRuntime> QuestCompleted = new();
public UnityEvent<QuestRuntime> QuestFailed = new();
public UnityEvent<QuestRuntime> QuestRestarted = new();
public UnityEvent<QuestRuntime> QuestRemoved = new();

// Progress
public UnityEvent<QuestRuntime> QuestUpdated = new();

// QuestLines
public UnityEvent<QuestLineRuntime> QuestLineCompleted = new();
```

---

## Subscription Audit Summary

All Subscribe/Unsubscribe pairs verified:
- **QuestRuntime.cs**: 7 group event subscriptions ↔ 7 unsubscriptions ✅
- **TaskGroupRuntime.cs**: 4 task event subscriptions ↔ 4 unsubscriptions ✅
- **TaskRuntime.cs**: 1 self-subscription ↔ 1 unsubscription ✅
- **QuestManager.cs**: 5 quest event subscriptions ↔ 5 unsubscriptions ✅
- **UI_QuestDetails.cs**: 3 aggregate subscriptions ↔ 3 unsubscriptions ✅
- **UI_TaskItem.cs**: 4 subscriptions ↔ 4 unsubscriptions ✅
- **UI_QuestItem.cs**: 2 subscriptions ↔ 2 unsubscriptions ✅
- **UI_Quests.cs**: 5 subscriptions ↔ 5 unsubscriptions ✅

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 3.10.0 | 2026-01-14 | TransitionNode, PortCapacityHelper, StageNode multi-capacity input |
| 3.9.0 | 2026-01-11 | QuestChoiceNode for QuestLine branching |
| 3.8.0 | 2026-01-11 | Graph Node UX improvements (ports on nodes) |
| 3.7.0 | 2026-01-05 | Native Subgraph Migration (Graph Variables) |
| 3.6.0 | 2026-01-04 | Quest Graph Editor v1.4 (Phases 5-7) |
| 3.5.0 | 2026-01-02 | Multi-subscriber condition support |
| 3.1.0 | 2025-12-29 | QuestSaveManager, snapshot system |
| 3.0.0 | 2025-12-28 | QuestStage, StageTransition, branching events |
| 2.0.0 | 2025-12-24 | QuestLineRuntime, AAA inspectors |
| 1.2.0 | 2025-12-22 | TaskGroupRuntime, execution modes |
| 1.0.0 | 2025-12-21 | Initial architecture |
