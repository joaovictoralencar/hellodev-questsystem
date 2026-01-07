# Quest System Overview

*Last Updated: 2026-01-04*

## Introduction

A comprehensive quest management system built on ScriptableObjects. Features modular architecture with support for stages, branching quests, player choices, world state flags, questlines, task groups, save/load, and localization.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              QuestManager                                    │
│  (Singleton - manages all quest and questline lifecycle)                     │
│  📁 Runtime/Scripts/Core/QuestManager.cs                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                        QuestLineRuntime                              │    │
│  │  (Runtime instance - tracks progress across related quests)          │    │
│  │  📁 Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs              │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                          QuestRuntime                                │    │
│  │  (Runtime instance - manages stages, state, branching)               │    │
│  │  📁 Runtime/Scripts/Core/Quests/QuestRuntime.cs                      │    │
│  ├─────────────────────────────────────────────────────────────────────┤    │
│  │                                                                      │    │
│  │  ┌───────────────────────────────────────────────────────────┐      │    │
│  │  │                      QuestStage                            │      │    │
│  │  │  (Stage data - groups tasks, defines transitions)         │      │    │
│  │  │  📁 Runtime/Scripts/Core/Stages/QuestStage.cs              │      │    │
│  │  ├───────────────────────────────────────────────────────────┤      │    │
│  │  │                                                            │      │    │
│  │  │  ┌─────────────────────────────────────────────────┐      │      │    │
│  │  │  │              TaskGroupRuntime                    │      │      │    │
│  │  │  │  (Execution mode: Sequential/Parallel/AnyOrder)  │      │      │    │
│  │  │  │  📁 Runtime/Scripts/Core/TaskGroups/             │      │      │    │
│  │  │  │     TaskGroupRuntime.cs                          │      │      │    │
│  │  │  ├─────────────────────────────────────────────────┤      │      │    │
│  │  │  │                                                  │      │      │    │
│  │  │  │  ┌───────────┐ ┌───────────┐ ┌───────────┐      │      │      │    │
│  │  │  │  │TaskRuntime│ │TaskRuntime│ │TaskRuntime│      │      │      │    │
│  │  │  │  │ (Int/Bool │ │ (Location │ │ (Timed/   │      │      │      │    │
│  │  │  │  │  /String) │ │ /Discovery│ │  Custom)  │      │      │      │    │
│  │  │  │  └───────────┘ └───────────┘ └───────────┘      │      │      │    │
│  │  │  │  📁 Runtime/Scripts/Core/Tasks/                  │      │      │    │
│  │  │  └─────────────────────────────────────────────────┘      │      │    │
│  │  │                                                            │      │    │
│  │  │  ┌─────────────────────────────────────────────────┐      │      │    │
│  │  │  │              StageTransition                     │      │      │    │
│  │  │  │  (OnComplete/OnFail/Conditional/PlayerChoice)    │      │      │    │
│  │  │  │  📁 Runtime/Scripts/Core/Stages/StageTransition.cs│     │      │    │
│  │  │  └─────────────────────────────────────────────────┘      │      │    │
│  │  └───────────────────────────────────────────────────────────┘      │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                        QuestSaveManager                              │    │
│  │  (Singleton - handles save/load operations)                          │    │
│  │  📁 Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs                │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

Data Assets (ScriptableObjects)
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  QuestLine_SO   │───▶│    Quest_SO     │───▶│    Task_SO[]    │
│  📁 ScriptableObjects/│  📁 ScriptableObjects/│  📁 ScriptableObjects/
│  QuestLine_SO.cs│    │  Quest_SO.cs    │    │  Task_SO.cs     │
└─────────────────┘    └────────┬────────┘    └────────┬────────┘
                                │                      │
                       ┌────────┴────────┐    ┌────────┴────────┐
                       │  Condition_SO[] │    │ RewardType_SO[] │
                       │  📁 com.hellodev.│    │  📁 ScriptableObjects/
                       │  conditions/    │    │  QuestRewardType_SO.cs
                       └─────────────────┘    └─────────────────┘

World State (com.hellodev.conditions)
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ WorldFlagBool_SO│    │ WorldFlagInt_SO │    │WorldFlagManager │
│  📁 com.hellodev.│    │  📁 com.hellodev.│    │  📁 com.hellodev.│
│  conditions/    │    │  conditions/    │    │  conditions/    │
│  WorldFlags/    │    │  WorldFlags/    │    │  WorldFlags/    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Core Concepts

### Data/Runtime Split

ScriptableObjects hold configuration (immutable), runtime classes hold mutable state:

| Asset (Immutable) | Runtime (Mutable) | Location |
|-------------------|-------------------|----------|
| `Quest_SO` | `QuestRuntime` | `Runtime/Scripts/Core/Quests/` |
| `Task_SO` | `TaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `TaskInt_SO` | `IntTaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `TaskBool_SO` | `BoolTaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `TaskLocation_SO` | `LocationTaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `TaskTimed_SO` | `TimedTaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `TaskDiscovery_SO` | `DiscoveryTaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `TaskString_SO` | `StringTaskRuntime` | `Runtime/Scripts/Core/Tasks/` |
| `QuestLine_SO` | `QuestLineRuntime` | `Runtime/Scripts/Core/QuestLines/` |
| `TaskGroup` | `TaskGroupRuntime` | `Runtime/Scripts/Core/TaskGroups/` |

Factory methods create runtime instances:
- `Quest_SO.GetRuntimeQuest()` → `QuestRuntime`
- `Task_SO.GetRuntimeTask()` → `TaskRuntime`

### Quest Stages

Quests are organized into stages for Skyrim-style multi-phase progression:

```
📁 Runtime/Scripts/Core/Stages/

Quest: "The Merchant's Dilemma"
├── Stage 0: Talk to Merchant
│   └── Tasks: [TalkToMerchant]
│   └── Transition: OnComplete → Stage 1
│
├── Stage 1: The Choice (PlayerChoice)
│   └── Transitions:
│       ├── [Combat] → Stage 10
│       ├── [Diplomacy] → Stage 20
│       └── [Lawful] → Stage 30 (requires reputation)
│
├── Stage 10: Combat Path
├── Stage 20: Diplomacy Path
├── Stage 30: Lawful Path
│
└── Stage 100: Resolution (Terminal)
```

### State Machines

Both Quest and Task use state machines:

```
Quest States (📁 Runtime/Scripts/Core/Quests/QuestState.cs):
NotStarted → InProgress → Completed
                       → Failed

Task States (📁 Runtime/Scripts/Core/Tasks/TaskState.cs):
NotStarted → InProgress → Completed
                       → Failed

QuestLine States (📁 Runtime/Scripts/Core/QuestLines/QuestLineState.cs):
Locked → Available → InProgress → Completed
                               → Failed
```

### Event-Driven Architecture

Systems communicate via UnityEvents:

```csharp
// QuestManager events (📁 Runtime/Scripts/Core/QuestManager.cs)
QuestManager.Instance.QuestAdded
QuestManager.Instance.QuestStarted
QuestManager.Instance.QuestCompleted
QuestManager.Instance.QuestFailed
QuestManager.Instance.QuestUpdated
QuestManager.Instance.QuestLineCompleted

// Quest events (📁 Runtime/Scripts/Core/Quests/QuestRuntime.cs)
quest.OnQuestStarted
quest.OnQuestCompleted
quest.OnQuestFailed
quest.OnQuestUpdated
quest.OnAnyTaskCompleted
quest.OnChoicesAvailable    // Branching
quest.OnChoiceMade          // Branching

// Task events (📁 Runtime/Scripts/Core/Tasks/TaskRuntime.cs)
task.OnTaskStarted
task.OnTaskCompleted
task.OnTaskFailed
task.OnTaskUpdated
```

### Condition Integration

Quests and tasks use the HelloDev Conditions system (`com.hellodev.conditions`):

- **Start Conditions** - Requirements to start a quest
- **Completion Conditions** - Task completion triggers (event-driven)
- **Failure Conditions** - Conditions that cause failure
- **Quest State Conditions** - Chain quests together (`ConditionQuestState_SO`)
- **QuestLine State Conditions** - Chain questlines (`ConditionQuestLineState_SO`)
- **World Flag Conditions** - Check world state (`ConditionWorldFlagBool_SO`, `ConditionWorldFlagInt_SO`)

### World State Flags

Persistent game state for cross-quest consequences:

```
📁 com.hellodev.conditions/Runtime/Scripts/WorldFlags/

WorldFlagBool_SO  - Boolean flags (met_king, chose_evil_path)
WorldFlagInt_SO   - Integer flags with min/max (reputation, kill_count)
WorldFlagManager  - Centralized runtime management
WorldFlagLocator_SO - Decoupled access pattern
```

### Save/Load System

Persist quest and world state:

```
📁 Runtime/Scripts/Core/SaveLoad/

QuestSaveManager.cs      - Singleton manager
QuestSaveLocator_SO.cs   - Decoupled access
QuestSystemSnapshot.cs   - State capture
SaveSlotConfig_SO.cs     - Per-slot configuration
```

## File Structure

```
Assets/com.hellodev.questsystem/
├── Runtime/
│   └── Scripts/
│       ├── Core/
│       │   ├── QuestManager.cs              # Singleton manager
│       │   ├── QuestManager.Editor.cs       # Odin inspector code
│       │   ├── Quests/
│       │   │   ├── QuestRuntime.cs          # Runtime quest
│       │   │   └── QuestState.cs            # State enum
│       │   ├── QuestLines/
│       │   │   ├── QuestLineRuntime.cs      # Runtime questline
│       │   │   └── QuestLineState.cs        # State enum
│       │   ├── Stages/
│       │   │   ├── QuestStage.cs            # Stage data
│       │   │   ├── StageTransition.cs       # Transition definition
│       │   │   └── TransitionTrigger.cs     # Trigger enum
│       │   ├── Tasks/
│       │   │   ├── TaskRuntime.cs           # Abstract base
│       │   │   ├── IntTaskRuntime.cs        # Counter task
│       │   │   ├── BoolTaskRuntime.cs       # Boolean task
│       │   │   ├── StringTaskRuntime.cs     # String matching
│       │   │   ├── LocationTaskRuntime.cs   # Location-based
│       │   │   ├── TimedTaskRuntime.cs      # Timer-based
│       │   │   └── DiscoveryTaskRuntime.cs  # Find items
│       │   ├── TaskGroups/
│       │   │   ├── TaskGroup.cs             # Group data
│       │   │   ├── TaskGroupRuntime.cs      # Runtime execution
│       │   │   ├── TaskExecutionMode.cs     # Execution modes
│       │   │   └── TaskGroupState.cs        # State enum
│       │   ├── Conditions/
│       │   │   ├── ConditionQuestState_SO.cs     # Quest chains
│       │   │   └── ConditionQuestLineState_SO.cs # QuestLine chains
│       │   ├── SaveLoad/
│       │   │   ├── QuestSaveManager.cs      # Save/load manager
│       │   │   ├── QuestSaveLocator_SO.cs   # Locator pattern
│       │   │   ├── QuestSystemSnapshot.cs   # State snapshot
│       │   │   └── SaveSlotConfig_SO.cs     # Slot configuration
│       │   └── ScriptableObjects/
│       │       ├── Quest_SO.cs              # Quest data
│       │       ├── Task_SO.cs               # Task base data
│       │       ├── QuestLine_SO.cs          # QuestLine data
│       │       ├── QuestType_SO.cs          # Quest category
│       │       ├── QuestRewardType_SO.cs    # Reward base
│       │       └── Task Types/
│       │           ├── TaskInt_SO.cs
│       │           ├── TaskBool_SO.cs
│       │           ├── TaskString_SO.cs
│       │           ├── TaskLocation_SO.cs
│       │           ├── TaskTimed_SO.cs
│       │           └── TaskDiscovery_SO.cs
│       └── Utils/
│           └── QuestLogger.cs               # Debug logging
├── Editor/
│   └── Scripts/
│       └── QuestCreationWizard.cs           # Quest creation tool
├── Tests/
│   ├── Runtime/QuestSystemTests.cs
│   └── Editor/QuestEditorTests.cs
└── BasicQuestExample/                       # Example implementation
    ├── Scripts/
    │   ├── Conditions/                      # ConditionID_SO
    │   ├── GameEvents/                      # GameEventID_SO
    │   ├── Rewards/                         # Example rewards
    │   ├── UI/                              # Quest UI components
    │   └── SaveSystemSetup.cs               # Save configuration
    └── ScriptableObjects/
        ├── Quests/                          # Example quests
        ├── QuestLines/                      # Example questlines
        ├── WorldFlags/                      # Example world flags
        └── SaveLoad/                        # Save slot configs
```

## Typical Flow

### 1. Quest Creation (Design Time)
```
1. Create Quest_SO asset (📁 ScriptableObjects/Quest_SO.cs)
2. Add stages with task groups (📁 Stages/QuestStage.cs)
3. Add Task_SO assets to task groups
4. Configure stage transitions (OnComplete, PlayerChoice, etc.)
5. Configure conditions (start, completion, failure)
6. Set rewards and quest type
7. Add Quest_SO to QuestManager database
8. (Optional) Add to QuestLine_SO for narrative grouping
```

### 2. Quest Lifecycle (Runtime)
```
1. QuestManager.AddQuest(quest_SO)
   - Creates QuestRuntime instance
   - Creates stage and task runtime instances
   - Checks start conditions
   - Subscribes to events

2. QuestRuntime.StartQuest() (manual or condition-triggered)
   - State → InProgress
   - Starts first stage
   - Starts first task group in stage
   - Fires OnQuestStarted

3. Task progression
   - IncrementStep() / CompleteTask()
   - Fires OnTaskUpdated, OnTaskCompleted
   - Group checks completion based on ExecutionMode
   - Stage transitions when all groups complete

4. Stage transitions
   - Evaluate transition conditions
   - If PlayerChoice: fire OnChoicesAvailable
   - When choice made: apply world flag modifications
   - Move to target stage

5. QuestRuntime.CompleteQuest() (final stage terminal)
   - State → Completed
   - Distribute rewards
   - Moves to CompletedQuests
   - Fires OnQuestCompleted
   - Updates QuestLine progress

(Alternative: QuestRuntime.FailQuest())
   - State → Failed
   - Fires OnQuestFailed
```

### 3. Save/Load Flow
```
1. Setup (application start)
   SaveService.SetProvider(new JsonSaveProvider(...));

2. Save
   await questSaveLocator.SaveAsync("slot_1");
   - Captures QuestSystemSnapshot
   - Serializes quest states, task progress, branch decisions
   - Serializes world flag values

3. Load
   await questSaveLocator.LoadAsync("slot_1");
   - Deserializes snapshot
   - Restores quest states
   - Restores world flags
   - Fires OnAfterLoad event
```

## Integration with HelloDev

| HelloDev System | Quest System Usage | Location |
|-----------------|-------------------|----------|
| Events | Task completion triggers | `com.hellodev.events` |
| Conditions | Start/completion/failure conditions | `com.hellodev.conditions` |
| World Flags | Cross-quest consequences | `com.hellodev.conditions/WorldFlags/` |
| RuntimeScriptableObject | Base for Quest_SO, Task_SO | `com.hellodev.utils` |
| Utils | SafeInvoke, SafeSubscribe | `com.hellodev.utils` |
| IDs | Location targets, discovery items | `com.hellodev.ids` |
| Bootstrap | Initialization ordering | `com.hellodev.bootstrap` |
| Localization | Display names, descriptions | Unity Localization |

## Dependencies

### Required
- com.hellodev.utils (1.3.0+)
- com.hellodev.events (1.1.0+)
- com.hellodev.conditions (1.3.0+)
- com.hellodev.ids (1.1.0+)
- com.unity.localization

### Optional
- Odin Inspector (for enhanced inspectors and Quick Actions)
- com.hellodev.bootstrap (for controlled initialization order)
