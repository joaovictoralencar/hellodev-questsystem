# Tasks

*Last Updated: 2026-01-04*

## Overview

Tasks represent individual objectives within a quest. Split into Task_SO (data) and TaskRuntime (runtime) with specialized types for different objective styles.

## Files

| File | Class | Purpose | Location |
|------|-------|---------|----------|
| `Task_SO.cs` | `Task_SO` | Abstract task data base | `Runtime/Scripts/Core/ScriptableObjects/` |
| `TaskRuntime.cs` | `TaskRuntime` | Abstract runtime task base | `Runtime/Scripts/Core/Tasks/` |
| `TaskInt_SO.cs` | `TaskInt_SO` | Counter task data | `Runtime/Scripts/Core/ScriptableObjects/Task Types/` |
| `IntTaskRuntime.cs` | `IntTaskRuntime` | Counter task runtime | `Runtime/Scripts/Core/Tasks/` |
| `TaskBool_SO.cs` | `TaskBool_SO` | Boolean task data | `Runtime/Scripts/Core/ScriptableObjects/Task Types/` |
| `BoolTaskRuntime.cs` | `BoolTaskRuntime` | Boolean task runtime | `Runtime/Scripts/Core/Tasks/` |
| `TaskString_SO.cs` | `TaskString_SO` | String task data | `Runtime/Scripts/Core/ScriptableObjects/Task Types/` |
| `StringTaskRuntime.cs` | `StringTaskRuntime` | String task runtime | `Runtime/Scripts/Core/Tasks/` |
| `TaskLocation_SO.cs` | `TaskLocation_SO` | Location task data | `Runtime/Scripts/Core/ScriptableObjects/Task Types/` |
| `LocationTaskRuntime.cs` | `LocationTaskRuntime` | Location task runtime | `Runtime/Scripts/Core/Tasks/` |
| `TaskTimed_SO.cs` | `TaskTimed_SO` | Timed task data | `Runtime/Scripts/Core/ScriptableObjects/Task Types/` |
| `TimedTaskRuntime.cs` | `TimedTaskRuntime` | Timed task runtime | `Runtime/Scripts/Core/Tasks/` |
| `TaskDiscovery_SO.cs` | `TaskDiscovery_SO` | Discovery task data | `Runtime/Scripts/Core/ScriptableObjects/Task Types/` |
| `DiscoveryTaskRuntime.cs` | `DiscoveryTaskRuntime` | Discovery task runtime | `Runtime/Scripts/Core/Tasks/` |

---

## Task_SO (Base)

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/ScriptableObjects/Task_SO.cs`
**Inherits:** `RuntimeScriptableObject`

Abstract base class for task configuration assets.

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `devName` | `string` | Developer name |
| `taskId` | `string` | GUID (auto-generated) |
| `displayName` | `LocalizedString` | Localized task name |
| `taskDescription` | `LocalizedString` | Localized description |
| `conditions` | `List<Condition_SO>` | Completion conditions |
| `failureConditions` | `List<Condition_SO>` | Failure conditions |

### Abstract Methods

#### `GetRuntimeTask()` → `TaskRuntime`
Factory method creating runtime instance.

#### `SetupTaskLocalizedVariables(LocalizedString, TaskRuntime)`
Configures localization variables (e.g., {current}/{required}).

---

## TaskRuntime (Base)

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/Tasks/TaskRuntime.cs`
**Namespace:** `HelloDev.QuestSystem.Tasks`

Abstract base class for runtime task instances.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `TaskId` | `Guid` | Unique identifier |
| `DevName` | `string` | Developer name |
| `DisplayName` | `LocalizedString` | Localized name |
| `Description` | `LocalizedString` | Localized description |
| `CurrentState` | `TaskState` | Current task state |
| `Data` | `Task_SO` | Source data reference |
| `Progress` | `float` | Abstract: completion progress (0-1) |

### Events

| Event | Type | Description |
|-------|------|-------------|
| `OnTaskUpdated` | `UnityEvent<TaskRuntime>` | Progress changed |
| `OnTaskStarted` | `UnityEvent<TaskRuntime>` | Task started |
| `OnTaskCompleted` | `UnityEvent<TaskRuntime>` | Task completed |
| `OnTaskFailed` | `UnityEvent<TaskRuntime>` | Task failed |

### Public Methods

#### `StartTask()`
Transitions from NotStarted to InProgress.
- Calls SubscribeToEvents()
- Fires OnTaskStarted

#### `CompleteTask()`
Marks task as completed (only if InProgress).
- Calls UnsubscribeFromEvents()
- Calls ForceCompleteState()
- Fires OnTaskUpdated, OnTaskCompleted

#### `FailTask()`
Marks task as failed (only if InProgress).
- Calls UnsubscribeFromEvents()
- Fires OnTaskFailed

#### `ResetTask()`
Resets task to NotStarted.
- Calls UnsubscribeFromEvents()

#### `IncrementStep()`
Calls OnIncrementStep() and fires OnTaskUpdated if successful.

#### `DecrementStep()`
Calls OnDecrementStep() and fires OnTaskUpdated if successful.

### Abstract Methods

#### `ForceCompleteState()`
Forces internal state to completed values.

#### `OnIncrementStep()` → `bool`
Increments progress. Returns true on success.

#### `OnDecrementStep()` → `bool`
Decrements progress. Returns true on success.

#### `CheckCompletion(TaskRuntime)`
Evaluates if task should auto-complete.

---

## IntTaskRuntime / TaskInt_SO

**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Tasks/Int Task`
**Data Path:** `Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskInt_SO.cs`
**Runtime Path:** `Runtime/Scripts/Core/Tasks/IntTaskRuntime.cs`

Counter-based task for tracking numeric objectives (kill counts, collection, etc.)

### TaskInt_SO Fields

| Field | Type | Description |
|-------|------|-------------|
| `targetId` | `ID_SO` | Target identifier (e.g., goblin ID) |
| `requiredCount` | `int` | Number needed to complete |

### IntTaskRuntime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | CurrentCount / RequiredCount |
| `RequiredCount` | `int` | Target count |
| `CurrentCount` | `int` | Current count |

### IntTaskRuntime Methods

#### `OnIncrementStep()` → `bool`
Increments count if InProgress and not at max.

#### `OnDecrementStep()` → `bool`
Decrements count if InProgress and count > 0.

#### `ForceCompleteState()`
Sets CurrentCount = RequiredCount.

#### `CheckCompletion(TaskRuntime)`
Completes task if CurrentCount >= RequiredCount.

### Example: Kill Quest
```csharp
// TaskInt_SO configuration:
// - targetId: ID_Goblin
// - requiredCount: 10
// - displayName: "Kill {current}/{required} goblins"

// In game (via event-driven condition):
// OnMonsterKilled event fires with ID_SO parameter
// ConditionID_SO checks if it matches targetId
// Task increments automatically
```

---

## BoolTaskRuntime / TaskBool_SO

**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Tasks/Bool Task`
**Data Path:** `Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskBool_SO.cs`
**Runtime Path:** `Runtime/Scripts/Core/Tasks/BoolTaskRuntime.cs`

Binary task for single-action objectives (talk to NPC, reach location, etc.)

### BoolTaskRuntime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | 1 if Completed, 0 otherwise |

### BoolTaskRuntime Methods

#### `OnIncrementStep()` → `bool`
Completes task immediately if InProgress.

#### `ForceCompleteState()`
No-op (no internal state to force).

#### `CheckCompletion(TaskRuntime)`
Evaluates all conditions and completes if all true.

### Example: Talk to NPC
```csharp
// TaskBool_SO configuration:
// - Add ConditionID_SO to conditions
// - Condition listens for NPC interaction event

// When NPC interaction event fires with matching ID,
// condition is met and task auto-completes
```

---

## StringTaskRuntime / TaskString_SO

**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Tasks/String Task`
**Data Path:** `Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskString_SO.cs`
**Runtime Path:** `Runtime/Scripts/Core/Tasks/StringTaskRuntime.cs`

String matching task for text-based objectives (enter password, say phrase, etc.)

### TaskString_SO Fields

| Field | Type | Description |
|-------|------|-------------|
| `targetString` | `string` | String to match |
| `caseSensitive` | `bool` | Whether comparison is case-sensitive |

### StringTaskRuntime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | 1 if Completed, 0 otherwise |
| `TargetString` | `string` | String to match |
| `CurrentString` | `string` | Current input |

### StringTaskRuntime Methods

#### `SetString(string value)`
Sets the current string and checks for completion.

#### `OnIncrementStep()` → `bool`
Returns false (use SetString instead).

#### `CheckCompletion(TaskRuntime)`
Compares CurrentString to TargetString, completes if matched.

### Example: Enter Password
```csharp
// TaskString_SO configuration:
// - targetString: "Scarface"
// - caseSensitive: false

// In game:
var stringTask = task as StringTaskRuntime;
stringTask.SetString(playerInput);
// Task auto-completes if input matches "scarface" (case-insensitive)
```

---

## LocationTaskRuntime / TaskLocation_SO

**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Tasks/Location Task`
**Data Path:** `Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskLocation_SO.cs`
**Runtime Path:** `Runtime/Scripts/Core/Tasks/LocationTaskRuntime.cs`

Location-based task for reaching waypoints or areas.

### TaskLocation_SO Fields

| Field | Type | Description |
|-------|------|-------------|
| `targetLocation` | `ID_SO` | Location identifier |

### LocationTaskRuntime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | 1 if reached, 0 otherwise |
| `TargetLocation` | `ID_SO` | Target location ID |
| `HasReached` | `bool` | Whether player reached location |

### LocationTaskRuntime Methods

#### `MarkLocationReached()`
Marks location as reached and completes task.

#### `OnIncrementStep()` → `bool`
Calls MarkLocationReached().

### Example: Reach Waypoint
```csharp
// TaskLocation_SO configuration:
// - targetLocation: ID_BanditCamp

// In game (via trigger zone or event):
var locationTask = task as LocationTaskRuntime;
locationTask.MarkLocationReached();
// Or use event-driven ConditionID_SO that fires on location entered
```

---

## TimedTaskRuntime / TaskTimed_SO

**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Tasks/Timed Task`
**Data Path:** `Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskTimed_SO.cs`
**Runtime Path:** `Runtime/Scripts/Core/Tasks/TimedTaskRuntime.cs`

Timer-based task with countdown and objective completion.

### TaskTimed_SO Fields

| Field | Type | Description |
|-------|------|-------------|
| `timeLimit` | `float` | Time limit in seconds |
| `failOnExpire` | `bool` | Whether task fails when timer expires |

### TimedTaskRuntime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | TimeRemaining / TimeLimit |
| `TimeLimit` | `float` | Original time limit |
| `TimeRemaining` | `float` | Current time remaining |
| `ObjectiveComplete` | `bool` | Whether objective was achieved |

### TimedTaskRuntime Methods

#### `AddTime(float seconds)`
Adds time to the remaining timer.

#### `ExpireTimer()`
Forces timer to 0, triggers failure if `failOnExpire`.

#### `MarkObjectiveComplete()`
Marks objective complete, stops timer, completes task.

#### `UpdateTimer(float deltaTime)`
Called each frame to tick down timer.

### Example: Survive
```csharp
// TaskTimed_SO configuration:
// - timeLimit: 60
// - failOnExpire: true

// In game (call in Update):
var timedTask = task as TimedTaskRuntime;
timedTask.UpdateTimer(Time.deltaTime);

// To complete early:
timedTask.MarkObjectiveComplete();

// To add bonus time:
timedTask.AddTime(30f);
```

---

## DiscoveryTaskRuntime / TaskDiscovery_SO

**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Tasks/Discovery Task`
**Data Path:** `Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskDiscovery_SO.cs`
**Runtime Path:** `Runtime/Scripts/Core/Tasks/DiscoveryTaskRuntime.cs`

Discovery-based task for finding hidden items or clues.

### TaskDiscovery_SO Fields

| Field | Type | Description |
|-------|------|-------------|
| `itemsToDiscover` | `List<ID_SO>` | Items that must be found |

### DiscoveryTaskRuntime Properties

| Property | Type | Description |
|----------|------|-------------|
| `Progress` | `float` | DiscoveredCount / TotalCount |
| `ItemsToDiscover` | `List<ID_SO>` | All items to find |
| `DiscoveredItems` | `HashSet<ID_SO>` | Items found so far |
| `RemainingItems` | `IEnumerable<ID_SO>` | Items not yet found |

### DiscoveryTaskRuntime Methods

#### `DiscoverItem(ID_SO item)`
Marks item as discovered, completes if all found.

#### `OnIncrementStep()` → `bool`
Discovers the next undiscovered item (for debug).

### Example: Find Clues
```csharp
// TaskDiscovery_SO configuration:
// - itemsToDiscover: [ID_Clue_Footprint, ID_Clue_BloodStain]

// In game:
var discoveryTask = task as DiscoveryTaskRuntime;
discoveryTask.DiscoverItem(foundClueId);
// Task completes when all clues discovered
```

---

## Condition Integration

Tasks subscribe to conditions for automatic completion/failure:

**Path:** `Runtime/Scripts/Core/Tasks/TaskRuntime.cs:SubscribeToEvents()`

```csharp
// In SubscribeToEvents():
foreach (var condition in Data.Conditions)
{
    if (condition is IConditionEventDriven eventCondition)
    {
        eventCondition.SubscribeToEvent(OnConditionMet, CompleteTask);
    }
}

foreach (var condition in Data.FailureConditions)
{
    if (condition is IConditionEventDriven eventCondition)
    {
        eventCondition.SubscribeToEvent(OnConditionMet, FailTask);
    }
}
```

---

## Usage Examples

### Creating Task Assets
```csharp
// Via menu: Assets > Create > HelloDev/Quest System/Scriptable Objects/Tasks/Int Task
// Configure:
// - Required count: 5
// - Target ID: ID_GoldOre
// - Display name: "Mine {current}/{required} gold ore"
// - Add completion condition if event-driven
```

### Manual Task Progression
```csharp
// 📁 Your game code
public class ResourceCollector : MonoBehaviour
{
    public void OnResourceCollected(ID_SO resourceId)
    {
        var quest = QuestManager.Instance.GetActiveQuest(questId);
        foreach (var task in quest.CurrentTasks)
        {
            if (task is IntTaskRuntime intTask &&
                task.CurrentState == TaskState.InProgress)
            {
                intTask.IncrementStep();
            }
        }
    }
}
```

### Event-Driven Task Completion
```csharp
// Configure TaskBool_SO with:
// - ConditionInt_SO listening to GameEventInt_SO
// - Target value: 100
// - Comparison: GreaterThanOrEqual

// When event fires with value >= 100, task auto-completes
```

---

## State Flow

```
                    ┌─────────────────┐
                    │   NotStarted    │
                    └────────┬────────┘
                             │
               StartTask()   │
                             ▼
                    ┌─────────────────┐
                    │   InProgress    │◄──── IncrementStep()
                    └────────┬────────┘      DecrementStep()
                             │               SetString()
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   │                   ▼
┌─────────────────┐          │          ┌─────────────────┐
│   Completed     │   (Condition met)   │     Failed      │
└─────────────────┘          │          └─────────────────┘
                             │
                   ResetTask()
                             │
                             ▼
                    ┌─────────────────┐
                    │   NotStarted    │
                    └─────────────────┘
```

---

## Localization Variables

| Task Type | Variables | Example |
|-----------|-----------|---------|
| IntTaskRuntime | `{current}`, `{required}`, `{target}` | "Kill {current}/{required} goblins" |
| DiscoveryTaskRuntime | `{current}`, `{required}` | "Found {current}/{required} clues" |
| TimedTaskRuntime | `{remaining}`, `{limit}` | "{remaining}s remaining" |
| LocationTaskRuntime | `{target}` | "Go to {target}" |
| StringTaskRuntime | `{target}` | "Say the password: {target}" |
| BoolTaskRuntime | (none) | "Talk to the merchant" |
