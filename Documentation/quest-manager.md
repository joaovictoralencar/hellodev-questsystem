# QuestManager

## Overview

The central singleton managing all quest lifecycle, state, and event delegation. Provides the public API for quest operations.

## File

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/QuestManager.cs`
**Namespace:** `HelloDev.QuestSystem`
**Type:** `partial class` (allows extension in other files)
**Inherits:** `MonoBehaviour` (Manual Singleton pattern with DontDestroyOnLoad)

## Configuration

| Field | Type | Description |
|-------|------|-------------|
| `questsDatabase` | `List<Quest_SO>` | All available quest data |
| `InitializeOnAwake` | `bool` | Auto-initialize on Awake |
| `EnableDebugLogging` | `bool` | Enable QuestLogger output |
| `AllowMultipleActiveQuests` | `bool` | Allow concurrent quests |
| `AllowPlayingCompletedQuests` | `bool` | Allow replaying completed |

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Instance` | `QuestManager` | Static singleton instance |
| `ActiveQuests` | `Dictionary<Guid, Quest>` | Currently active quests |
| `CompletedQuests` | `Dictionary<Guid, Quest>` | Completed quest history |
| `QuestsDatabase` | `List<Quest_SO>` | Available quest data |

## Events

| Event | Type | Description |
|-------|------|-------------|
| `QuestAdded` | `UnityEvent<Quest>` | Quest added to active list |
| `QuestStarted` | `UnityEvent<Quest>` | Quest transitioned to InProgress |
| `QuestRemoved` | `UnityEvent<Quest>` | Quest removed from active |
| `QuestRestarted` | `UnityEvent<Quest>` | Quest was restarted |
| `QuestFailed` | `UnityEvent<Quest>` | Quest failed |
| `QuestUpdated` | `UnityEvent<Quest>` | Quest progress changed |
| `QuestCompleted` | `UnityEvent<Quest>` | Quest completed successfully |

## Public Methods

### Lifecycle Methods

#### `InitializeManager(List<Quest_SO> allQuestData)`
Initializes the manager with quest data. Called automatically if `InitializeOnAwake` is true.

```csharp
QuestManager.Instance.InitializeManager(myQuestDatabase);
```

#### `ShutdownManager()`
Clears all active/completed quests and event listeners.

```csharp
QuestManager.Instance.ShutdownManager();
```

---

### Quest Management

#### `AddQuest(Quest_SO quest, bool forceStart = false)` → `bool`
Adds a quest to the active list. Returns true on success.

**Validation:**
- Quest must exist in database
- Quest must not already be active
- Respects `AllowMultipleActiveQuests` setting
- Respects `AllowPlayingCompletedQuests` setting

```csharp
bool added = QuestManager.Instance.AddQuest(myQuest, forceStart: true);
```

#### `CompleteQuest(Guid questId)`
Marks a quest as completed.

```csharp
QuestManager.Instance.CompleteQuest(quest.QuestId);
```

#### `FailQuest(Guid questId)`
Marks a quest as failed.

```csharp
QuestManager.Instance.FailQuest(quest.QuestId);
```

#### `RemoveQuest(Guid questId)` → `bool`
Removes a quest from active list and resets it. Returns true on success.

```csharp
QuestManager.Instance.RemoveQuest(quest.QuestId);
```

#### `RestartQuest(Guid questId, bool forceStart = false)` → `bool`
Resets quest state and optionally restarts immediately.

```csharp
QuestManager.Instance.RestartQuest(quest.QuestId, forceStart: true);
```

---

### Task Management

#### `IncrementTaskStep(Quest_SO quest)`
Increments the first in-progress task's step counter.

```csharp
QuestManager.Instance.IncrementTaskStep(questSO);
```

#### `DecrementTaskStep(Guid questId, Guid taskId)`
Decrements a specific task's step counter.

```csharp
QuestManager.Instance.DecrementTaskStep(questId, taskId);
```

#### `CompleteTask(Guid questId, Guid taskId)`
Marks a specific task as completed.

```csharp
QuestManager.Instance.CompleteTask(questId, taskId);
```

#### `FailTask(Guid questId, Guid taskId)`
Marks a specific task as failed.

```csharp
QuestManager.Instance.FailTask(questId, taskId);
```

---

### Query Methods

#### `GetActiveQuest(Guid questId)` → `Quest`
Returns active quest by ID or null if not found.

```csharp
Quest quest = QuestManager.Instance.GetActiveQuest(questId);
```

#### `GetActiveQuests()` → `ReadOnlyCollection<Quest>`
Returns immutable collection of all active quests.

```csharp
var quests = QuestManager.Instance.GetActiveQuests();
foreach (var quest in quests) { ... }
```

#### `GetTasksForQuest(Guid questId)` → `ReadOnlyCollection<Task>`
Returns task list for a quest or null if quest not found.

```csharp
var tasks = QuestManager.Instance.GetTasksForQuest(questId);
```

#### `IsQuestCompleted(Guid questId)` → `bool`
Checks if quest exists in completed quests.

```csharp
if (QuestManager.Instance.IsQuestCompleted(questId))
{
    // Quest was already completed
}
```

## Usage Examples

### Setting Up QuestManager
```csharp
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private List<Quest_SO> allQuests;

    private void Start()
    {
        // Manual initialization (if InitializeOnAwake is false)
        QuestManager.Instance.InitializeManager(allQuests);
    }
}
```

### Subscribing to Quest Events
```csharp
public class QuestUI : MonoBehaviour
{
    private void OnEnable()
    {
        QuestManager.Instance.QuestAdded.AddListener(OnQuestAdded);
        QuestManager.Instance.QuestCompleted.AddListener(OnQuestCompleted);
    }

    private void OnDisable()
    {
        QuestManager.Instance.QuestAdded.RemoveListener(OnQuestAdded);
        QuestManager.Instance.QuestCompleted.RemoveListener(OnQuestCompleted);
    }

    private void OnQuestAdded(Quest quest)
    {
        ShowQuestNotification(quest.QuestData.DisplayName);
    }

    private void OnQuestCompleted(Quest quest)
    {
        ShowCompletionCelebration(quest);
    }
}
```

### Adding and Starting a Quest
```csharp
public class QuestGiver : MonoBehaviour
{
    [SerializeField] private Quest_SO questToGive;

    public void GiveQuest()
    {
        bool success = QuestManager.Instance.AddQuest(questToGive, forceStart: true);
        if (success)
        {
            Debug.Log("Quest accepted!");
        }
    }
}
```

### Tracking Quest Progress
```csharp
public class EnemyKillTracker : MonoBehaviour
{
    [SerializeField] private Quest_SO killQuest;

    public void OnEnemyKilled()
    {
        // Increment the current task's step
        QuestManager.Instance.IncrementTaskStep(killQuest);
    }
}
```

## Singleton Pattern

QuestManager implements singleton via manual pattern in Awake:
- First instance sets `Instance` and calls `DontDestroyOnLoad`
- Subsequent instances are destroyed

```csharp
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // ... initialization
    }
    else
    {
        Destroy(gameObject);
    }
}
```

## Internal Event Handling

QuestManager subscribes to quest events when adding:
- `OnQuestStarted` → Invokes `QuestStarted` event
- `OnQuestCompleted` → Moves to completed, invokes `QuestCompleted`
- `OnQuestFailed` → Removes from active, invokes `QuestFailed` event
- `OnQuestUpdated` → Invokes `QuestUpdated` event
- `OnQuestRestarted` → Invokes `QuestRestarted` event

## Automatic Quest Loading

In `Start()`, the manager automatically calls `AddQuest(quest, forceStart: true)` for all quests in the database. This means all quests are activated on scene load.

## ⚠️ Known Issues

1. **UnsubscribeFromQuestEvents is empty**: Method exists but has no implementation - event handlers are never properly cleaned up. May be intentional if Quest handles its own cleanup.

## ✅ Recently Fixed (2025-12-20)

- **QuestFailed event now fires**: `HandleQuestFailed()` now invokes `QuestFailed?.SafeInvoke(quest)`
- **HandleQuestUpdated now works**: `QuestUpdated` event is now properly fired
