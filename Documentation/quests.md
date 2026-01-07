# Quests

## Overview

Quest represents a complete objective with multiple tasks, conditions, and rewards. Split into Quest_SO (data) and Quest (runtime).

## Files

| File | Class | Purpose |
|------|-------|---------|
| `Quest_SO.cs` | `Quest_SO` | Quest configuration asset |
| `Quest.cs` | `Quest` | Runtime quest instance |
| `QuestState.cs` | `QuestState`, `TaskState` | State enums |
| `QuestType_SO.cs` | `QuestType_SO` | Quest category |

## Quest_SO

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/ScriptableObjects/Quest_SO.cs`
**Inherits:** `RuntimeScriptableObject`
**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Quest`

Configuration asset defining a quest's structure and requirements.

### Serialized Fields

| Field | Type | Description |
|-------|------|-------------|
| `devName` | `string` | Developer-friendly name |
| `questId` | `string` | GUID (auto-generated, read-only) |
| `displayName` | `LocalizedString` | Localized UI name |
| `questDescription` | `LocalizedString` | Localized description |
| `questLocation` | `LocalizedString` | Optional location text |
| `questSprite` | `Sprite` | Quest icon |
| `tasks` | `List<Task_SO>` | Quest objectives |
| `startConditions` | `List<Condition_SO>` | Requirements to start |
| `failureConditions` | `List<Condition_SO>` | Conditions causing failure |
| `globalTaskFailureConditions` | `List<Condition_SO>` | Fail any task (not connected) |
| `questType` | `QuestType_SO` | Category/type |
| `rewards` | `List<RewardInstance>` | Completion rewards |
| `recommendedLevel` | `int` | Suggested player level |

### Properties (Read-Only)

All serialized fields have read-only property accessors with matching names.

### Methods

#### `GetRuntimeQuest()` → `Quest`
Factory method creating runtime instance from this data.

```csharp
Quest runtimeQuest = questSO.GetRuntimeQuest();
```

#### `GenerateNewGuid()` [Button]
Generates new GUID for questId. Available in inspector.

---

## Quest

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/Quests/Quest.cs`
**Namespace:** `HelloDev.QuestSystem.Quests`

Runtime representation of an active quest instance.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `QuestId` | `Guid` | Unique identifier |
| `CurrentState` | `QuestState` | Current quest state |
| `Tasks` | `List<Task>` | Runtime task instances |
| `QuestData` | `Quest_SO` | Source data reference |
| `CurrentProgress` | `float` | Average task progress (0-1) |

### Events

| Event | Type | Description |
|-------|------|-------------|
| `OnQuestStateChanged` | `UnityEvent<Quest, QuestState>` | State transitions |
| `OnQuestStarted` | `UnityEvent<Quest>` | Quest started |
| `OnQuestCompleted` | `UnityEvent<Quest>` | Quest completed |
| `OnQuestFailed` | `UnityEvent<Quest>` | Quest failed |
| `OnQuestRestarted` | `UnityEvent<Quest>` | Quest restarted |
| `OnQuestUpdated` | `UnityEvent<Quest>` | Progress changed |
| `OnAnyTaskUpdated` | `UnityEvent<Task>` | Any task updated |
| `OnAnyTaskCompleted` | `UnityEvent<Task>` | Any task completed |

### Constructor

```csharp
public Quest(Quest_SO questData)
```
- Creates runtime instance from ScriptableObject
- Initializes all tasks from Task_SO list
- Sets state to NotStarted

### Methods

#### `StartQuest()`
Transitions quest from NotStarted to InProgress.
- Starts first task
- Unsubscribes from start conditions
- Fires OnQuestStarted

#### `CompleteQuest()`
Marks quest as completed (only if InProgress).
- Unsubscribes from all events
- Changes state to Completed
- Fires OnQuestCompleted

#### `FailQuest()`
Marks quest as failed (only if InProgress).
- Changes state to Failed
- Unsubscribes from all events
- Fires OnQuestFailed

#### `ResetQuest()`
Resets quest to initial state and restarts.
- Unsubscribes from all events
- Resets all tasks
- Sets state to NotStarted
- Calls StartQuest()
- Fires OnQuestRestarted

#### `SubscribeToStartQuestEvents()`
For event-driven conditions, subscribes to events that trigger StartQuest.

#### `CheckStartConditions()` → `bool`
Evaluates all start conditions. Returns true if all met.

#### `CheckForConditionsAndStart()` → `bool`
Checks conditions and starts if met. Returns true if started.

---

## QuestState

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/Quests/QuestState.cs`

```csharp
public enum QuestState
{
    NotStarted,  // Quest added but not active
    InProgress,  // Quest currently active
    Completed,   // Quest successfully completed
    Failed       // Quest failed
}

public enum TaskState
{
    NotStarted,  // Task not started
    InProgress,  // Task currently active
    Completed,   // Task successfully completed
    Failed       // Task failed
}
```

---

## QuestType_SO

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/ScriptableObjects/QuestType_SO.cs`
**Create Menu:** `HelloDev/Quest System/Scriptable Objects/Quest Type`

Represents a quest category for grouping and UI presentation.

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `devName` | `string` | Developer name |
| `displayName` | `LocalizedString` | Display name |
| `color` | `Color` | Category color |
| `icon` | `Sprite` | Category icon |

## Usage Examples

### Creating a Quest Asset
```csharp
// Via menu: Assets > Create > HelloDev/Quest System/Scriptable Objects/Quest
// Configure in inspector:
// - Add Task_SO references
// - Set start conditions
// - Add rewards
// - Assign quest type
```

### Working with Quest Events
```csharp
public class QuestTracker : MonoBehaviour
{
    private Quest currentQuest;

    public void TrackQuest(Quest quest)
    {
        currentQuest = quest;

        quest.OnQuestStarted.AddListener(OnStarted);
        quest.OnQuestCompleted.AddListener(OnCompleted);
        quest.OnAnyTaskCompleted.AddListener(OnTaskDone);
    }

    private void OnStarted(Quest q) => Debug.Log("Quest started!");
    private void OnCompleted(Quest q) => Debug.Log("Quest completed!");
    private void OnTaskDone(Task t) => Debug.Log($"Task {t.DevName} done!");
}
```

### Checking Quest Progress
```csharp
public void ShowProgress(Quest quest)
{
    float progress = quest.CurrentProgress; // 0-1
    string percent = QuestUtils.GetPercentage(progress); // "75%"

    foreach (var task in quest.Tasks)
    {
        Debug.Log($"{task.DevName}: {task.CurrentState}");
    }
}
```

### Condition-Based Quest Start
```csharp
// Quest with start conditions waits for conditions to be met
public void AddConditionalQuest(Quest_SO questData)
{
    // AddQuest with forceStart: false
    QuestManager.Instance.AddQuest(questData, forceStart: false);

    // Quest will auto-start when all start conditions are met
    // (if conditions are IConditionEventDriven)
}
```

## State Flow

```
                    ┌─────────────────┐
                    │   NotStarted    │
                    └────────┬────────┘
                             │
              StartQuest()   │
                             ▼
                    ┌─────────────────┐
                    │   InProgress    │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   │                   ▼
┌─────────────────┐          │          ┌─────────────────┐
│   Completed     │          │          │     Failed      │
└─────────────────┘          │          └─────────────────┘
                             │
                   ResetQuest()
                             │
                             ▼
                    ┌─────────────────┐
                    │   NotStarted    │ (then auto-starts)
                    └─────────────────┘
```
