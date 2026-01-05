# Tutorial: Creating Tasks from Scratch

*Version 1.0 | For: Game Designers | Prerequisites: [Designer Workflow Guide](quest-graph-designer-workflow.md)*

This comprehensive tutorial walks you through creating a complete task from beginning to end. We'll create a **"Kill 5 Goblins"** counter task that auto-completes when the player defeats enemies.

**What you'll learn:**
- How to choose the right task type
- How to create Task_SO assets
- How to set up event-driven conditions
- How to add tasks to a Quest Graph
- How to test your task in Play mode

**Time required:** 15-20 minutes

---

## Table of Contents

1. [Part A: Choosing the Right Task Type](#part-a-choosing-the-right-task-type)
2. [Part B: Creating the Task Asset](#part-b-creating-the-task-asset)
3. [Part C: Setting Up Event-Driven Completion](#part-c-setting-up-event-driven-completion)
4. [Part D: Adding the Task to a Quest Graph](#part-d-adding-the-task-to-a-quest-graph)
5. [Part E: Testing Your Task](#part-e-testing-your-task)
6. [Complete Task Creation Checklist](#complete-task-creation-checklist)
7. [Additional Examples](#additional-examples-other-task-types)
   - [Bool Task - "Talk to the Merchant"](#example-1-bool-task---talk-to-the-merchant)
   - [Location Task - "Find the Bandit Camp"](#example-2-location-task---find-the-bandit-camp)
   - [Discovery Task - "Find 3 Clues"](#example-3-discovery-task---find-3-clues)
   - [Sequential Task Group](#example-4-sequential-task-group---talk-then-go)
   - [Parallel Task Group](#example-5-parallel-task-group---collect-and-kill)
   - [Optional Tasks (X of Y)](#example-6-optional-tasks---find-2-of-4-clues)

---

## Part A: Choosing the Right Task Type

Before creating a task, you need to understand which task type fits your objective.

### Task Type Decision Tree

```
What kind of objective is it?
│
├─► Single action? (talk to NPC, interact with object)
│   └─► Use: Bool Task (TaskBool_SO)
│
├─► Count something? (kill X enemies, collect Y items)
│   └─► Use: Int Task (TaskInt_SO)
│
├─► Reach a location? (go to the castle, find the cave)
│   └─► Use: Location Task (TaskLocation_SO)
│
├─► Enter text/password? (say the magic word)
│   └─► Use: String Task (TaskString_SO)
│
├─► Find multiple unique items? (collect 3 different clues)
│   └─► Use: Discovery Task (TaskDiscovery_SO)
│
└─► Complete within time limit? (survive 60 seconds)
    └─► Use: Timed Task (TaskTimed_SO)
```

### Task Type Reference

| Task Type | Use Case | Example | Key Fields |
|-----------|----------|---------|------------|
| **Bool Task** | Single true/false objective | "Talk to the Merchant" | Conditions only |
| **Int Task** | Counter-based objective | "Kill 5 Goblins" | `requiredCount`, Conditions |
| **Location Task** | Reach a specific place | "Find the Bandit Camp" | `targetLocation` (ID_SO) |
| **String Task** | Text input matching | "Enter the Password" | `targetValue` |
| **Discovery Task** | Find multiple unique items | "Find 3 Clues" | `requiredDiscoveries`, Conditions (one per item) |
| **Timed Task** | Time-limited objective | "Survive for 60 seconds" | `timeLimit`, `failQuestOnExpire` |

### For Our Tutorial

We're creating a "Kill 5 Goblins" task, so we'll use an **Int Task** (TaskInt_SO).

---

## Part B: Creating the Task Asset

Now let's create the actual Task_SO asset.

### Step B.1: Create the Folder Structure

Organize your task assets in a dedicated folder:

```
Assets/
└── YourGame/
    └── Quests/
        └── GoblinSlayer/
            ├── Tasks/              ← We'll create tasks here
            ├── Conditions/         ← Event-driven conditions
            └── Events/             ← Game events (if needed)
```

1. In the **Project** window, navigate to your quest folder (or create one)
2. **Right-click** → **Create** → **Folder**
3. Name it `Tasks`

### Step B.2: Create the Int Task Asset

1. **Right-click** in the `Tasks` folder
2. Select **Create** → **HelloDev** → **Quest System** → **Scriptable Objects** → **Tasks** → **Int Task**
3. Name it `SO_Task_KillGoblins`

### Step B.3: Configure Basic Identity

Select the new `SO_Task_KillGoblins` asset and configure in the **Inspector**:

**Identity Section:**
| Field | Value | Explanation |
|-------|-------|-------------|
| **Dev Name** | `Task_KillGoblins` | Internal name for debugging/code |
| **Task Id** | (auto-generated) | Don't modify - unique identifier |

### Step B.4: Configure Display Text

**Display Section:**
| Field | Value | Explanation |
|-------|-------|-------------|
| **Display Name** | LocalizedString | What players see in the UI |
| **Task Description** | LocalizedString | Optional longer description |

**Setting up the Localized Display Name:**

1. Click the **Display Name** field
2. In the dropdown, select your Localization Table (e.g., `QuestStrings`)
3. Click **Add New Entry** if needed
4. Set the **Key** to something like `task_kill_goblins_name`
5. Set the **Value** to: `Kill {current}/{required} Goblins`

> **Important:** The `{current}` and `{required}` are **Smart Format variables** that automatically update as the player progresses. Int Tasks support these variables:
> - `{current}` - Current progress count
> - `{required}` - Target count needed

**Example localized values:**
- English: `"Kill {current}/{required} Goblins"`
- Spanish: `"Mata {current}/{required} Goblins"`
- German: `"Töte {current}/{required} Goblins"`

### Step B.5: Configure Int Task Settings

**Task Settings Section:**
| Field | Value | Explanation |
|-------|-------|-------------|
| **Required Count** | `5` | Number of goblins to kill |

This means the task will complete when `currentCount >= 5`.

### Step B.6: Leave Conditions Empty (For Now)

**Conditions Section:**
| Field | Value | Explanation |
|-------|-------|-------------|
| **Conditions** | (empty) | We'll add this in Part C |
| **Failure Conditions** | (empty) | Optional: conditions that fail the task |

**Your task asset is now created!** But it won't auto-complete yet - we need to set up event-driven completion.

---

## Part C: Setting Up Event-Driven Completion

For the task to automatically increment when goblins are killed, we need:
1. A **GameEvent** that fires when a monster is killed
2. An **ID_SO** that identifies "Goblin"
3. A **Condition** that links the event to the task

### Understanding the Event Flow

```
[Player kills goblin]
    → [GameEventID_SO fires with ID_Goblin]
    → [ConditionEventDrivenID_SO checks if ID matches]
    → [Task increments counter]
    → [When count >= 5, task completes]
```

### Step C.1: Create or Locate the Monster ID

First, we need an ID_SO for "Goblin":

1. Navigate to your `IDs` folder (or create: `Assets/YourGame/IDs/Enemies/`)
2. **Right-click** → **Create** → **HelloDev** → **IDs** → **ID**
3. Name it `ID_Enemy_Goblin`
4. Configure:
   - **Dev Name**: `Goblin`
   - **Display Name**: (LocalizedString) → `"Goblin"`

> **Note:** If you already have enemy IDs set up, use the existing one.

### Step C.2: Create or Locate the Kill Event

You need a GameEvent that fires when monsters die:

1. Navigate to your `Events` folder (or create: `Assets/YourGame/Events/`)
2. **Right-click** → **Create** → **HelloDev** → **Events** → **Game Event ID**
3. Name it `GE_OnMonsterKilled`

> **Pattern:** Use ONE generic `OnMonsterKilled` event for ALL monster types. The ID parameter identifies which monster. Don't create `OnGoblinKilled`, `OnOrcKilled`, etc.

### Step C.3: Create the Event-Driven Condition

Now create the condition that links the event to the task:

1. Navigate to your `Conditions` folder (or create: `Assets/YourGame/Quests/GoblinSlayer/Conditions/`)
2. **Right-click** → **Create** → **HelloDev** → **Conditions** → **ID Condition**
3. Name it `SO_Condition_Event_GoblinKilled`

Configure the condition:
| Field | Value | Explanation |
|-------|-------|-------------|
| **Dev Name** | `Cond_GoblinKilled` | Internal identifier |
| **Game Event ID** | `GE_OnMonsterKilled` | Reference to the kill event |
| **Target Value** | `ID_Enemy_Goblin` | The ID to match |

> **How it works:** When `GE_OnMonsterKilled.Raise(ID_Enemy_Goblin)` is called, this condition evaluates to `true` and notifies the task.

### Step C.4: Add Condition to the Task

Now link the condition to your task:

1. Select `SO_Task_KillGoblins`
2. In the **Conditions** section, click the **+** button
3. Drag in `SO_Condition_Event_GoblinKilled`

**The task is now fully configured for event-driven completion!**

### Step C.5: (Game Code) Fire the Event

In your game code, when an enemy dies:

```csharp
// In your Enemy.cs or EnemyHealth.cs
public class Enemy : MonoBehaviour
{
    [SerializeField] private ID_SO enemyId;           // e.g., ID_Enemy_Goblin
    [SerializeField] private GameEventID_SO onKilled; // e.g., GE_OnMonsterKilled

    public void Die()
    {
        // ... death animation, drops, etc.

        // Fire the event with this enemy's ID
        onKilled.Raise(enemyId);
    }
}
```

This is the ONLY code needed - the quest system handles the rest automatically.

---

## Part D: Adding the Task to a Quest Graph

Now let's use the task in a Quest Graph.

### Step D.1: Create or Open a Quest Graph

1. Navigate to your quest folder
2. **Right-click** → **Create** → **HelloDev** → **Quest System** → **Quest Graph**
3. Name it `Quest_GoblinSlayer.quest`
4. **Double-click** to open in the Graph Editor

### Step D.2: Add the Quest Start Node

1. **Right-click** on the graph canvas
2. Select **Add Node** → **QuestStartNode**
3. Position it on the left side of the canvas

> **Note:** QuestStartNode requires no configuration. Optionally add **Start Conditions** in Inspector.

### Step D.3: Add a Stage Node

1. **Right-click** on the canvas
2. Select **Add Node** → **StageNode**
3. Configure:
   - **Stage Index**: `0`
   - **Stage Name**: `Hunt the Goblins`
   - **Is Terminal**: `true` (since this is a simple one-stage quest)
4. **Connect**: Drag from `QuestStartNode.FirstStage` → `StageNode.In`

### Step D.4: Add a Task Group Node

Task Groups organize tasks within a stage:

1. **Right-click** on the canvas
2. Select **Add Node** → **TaskGroupNode**
3. Configure:
   - **Group Name**: `Main Objectives`
   - **Execution Mode**: `Sequential` (or `Parallel` if multiple tasks)
4. **Connect**: Drag from `StageNode.TaskGroups` → `TaskGroupNode.In`

### Step D.5: Add a Task Node

1. **Right-click** on the canvas
2. Select **Add Node** → **TaskNode**
3. Configure:
   - **Task Asset**: Drag in `SO_Task_KillGoblins`
4. **Connect**: Drag from `TaskGroupNode.Tasks` → `TaskNode.In`

### Step D.6: Verify the Complete Graph

Your graph should now look like this:

```
┌────────────────────┐
│  QuestStartNode    │
│                    │
└─────────┬──────────┘
          │ FirstStage
          ▼
┌────────────────────┐
│    StageNode       │
│  Index: 0          │
│  "Hunt the Goblins"│
│  IsTerminal: true  │
└─────────┬──────────┘
          │ TaskGroups
          ▼
┌────────────────────┐
│  TaskGroupNode     │
│  "Main Objectives" │
│  Mode: Sequential  │
└─────────┬──────────┘
          │ Tasks
          ▼
┌────────────────────┐
│    TaskNode        │
│ SO_Task_KillGoblins│
│  "Kill 5 Goblins"  │
└────────────────────┘
```

### Step D.7: Save the Graph

1. Press **Ctrl+S** to save
2. Unity automatically generates a `Quest_GoblinSlayer` (Quest_SO) asset
3. Check the console for any validation errors

---

## Part E: Testing Your Task

### Step E.1: Create a Test Scene

1. Create a new scene or use an existing gameplay scene
2. Add your QuestManager (if not already present)
3. Reference the generated `Quest_GoblinSlayer` Quest_SO

### Step E.2: Start the Quest

In your game startup or via a trigger:

```csharp
// Start the quest
QuestManager.Instance.StartQuest(questGoblinSlayer);
```

### Step E.3: Test Task Progression

**Option A: Kill Enemies Normally**
- Play the game and kill goblins
- Watch the task counter increment in your quest UI

**Option B: Debug Shortcut**
```csharp
// Simulate killing a goblin
onMonsterKilledEvent.Raise(goblinId);
```

### Step E.4: Verify Completion

- When 5 goblins are killed, the task should auto-complete
- Since it's the only task in a terminal stage, the quest should complete
- Check your quest log/UI for the completed state

### Debugging Checklist

If the task doesn't work:

| Issue | Check |
|-------|-------|
| Task doesn't increment | Is the GameEvent being raised? Check with Debug.Log |
| Condition not triggering | Is the ID_SO matching? Log both IDs |
| Task never completes | Is `requiredCount` set correctly? |
| Quest doesn't complete | Is the stage marked `IsTerminal`? |
| No UI feedback | Is your UI subscribed to task events? |

---

## Complete Task Creation Checklist

```
[ ] Part A: Choose Task Type
    [ ] Identify objective type (Bool/Int/Location/etc.)

[ ] Part B: Create Task Asset
    [ ] Create Task_SO via Create menu
    [ ] Set Dev Name
    [ ] Configure Display Name with localized variables
    [ ] Set type-specific fields (requiredCount, etc.)

[ ] Part C: Set Up Event-Driven Completion
    [ ] Create or reuse ID_SO for target
    [ ] Create or reuse GameEvent for trigger
    [ ] Create ConditionEventDriven_SO linking event to ID
    [ ] Add condition to task's Conditions list
    [ ] Ensure game code raises the event

[ ] Part D: Add to Quest Graph
    [ ] Open/create Quest Graph
    [ ] Add QuestStartNode
    [ ] Add StageNode and configure
    [ ] Add TaskGroupNode
    [ ] Add TaskNode and reference Task_SO
    [ ] Connect all nodes
    [ ] Save graph (Ctrl+S)

[ ] Part E: Test
    [ ] Add quest to QuestManager
    [ ] Start quest
    [ ] Trigger events
    [ ] Verify progression
    [ ] Verify completion
```

---

## Additional Examples: Other Task Types

These quick examples show how to create other common task types using the same workflow.

### Example 1: Bool Task - "Talk to the Merchant"

A Bool Task completes after a single action (talking to an NPC, interacting with an object).

**Step 1: Create the Bool Task Asset**
1. **Create** → **HelloDev** → **Quest System** → **Scriptable Objects** → **Tasks** → **Bool Task**
2. Name it `SO_Task_TalkToMerchant`
3. Configure:
   - **Dev Name**: `Task_TalkToMerchant`
   - **Display Name**: `"Talk to the Merchant"` (no variables needed)

**Step 2: Create the Condition**

1. Create an ID: `ID_NPC_Merchant`
2. Create a GameEventID: `GE_OnNPCInteract` (generic for all NPC interactions)
3. Create ConditionEventID: `SO_Condition_Event_TalkToMerchant`
   - **Game Event**: `GE_OnNPCInteract`
   - **Target Value**: `ID_NPC_Merchant`
4. Add condition to task's **Conditions** list

**Step 3: Game Code**

```csharp
public class NPC : MonoBehaviour
{
    [SerializeField] private ID_SO npcId;
    [SerializeField] private GameEventID_SO onInteract;

    public void Interact()
    {
        onInteract.Raise(npcId);
    }
}
```

**Step 4: Add to Graph**

Same as Int Task - add TaskGroupNode → TaskNode with `SO_Task_TalkToMerchant`.

---

### Example 2: Location Task - "Find the Bandit Camp"

A Location Task completes when the player reaches a specific area.

**Step 1: Create the Location Task Asset**
1. **Create** → **HelloDev** → **Quest System** → **Scriptable Objects** → **Tasks** → **Location Task**
2. Name it `SO_Task_FindBanditCamp`
3. Configure:
   - **Dev Name**: `Task_FindBanditCamp`
   - **Display Name**: `"Find the Bandit Camp"` (can use `{target}` for location name)
   - **Target Location**: `ID_Location_BanditCamp`

**Step 2: Create the Location ID**

1. **Create** → **HelloDev** → **IDs** → **ID**
2. Name it `ID_Location_BanditCamp`
3. Configure display name for UI

**Step 3: Create the Trigger Zone**

```csharp
public class LocationTrigger : MonoBehaviour
{
    [SerializeField] private ID_SO locationId;
    [SerializeField] private GameEventID_SO onLocationEntered;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onLocationEntered.Raise(locationId);
        }
    }
}
```

**Step 4: Create Condition and Add to Task**

1. Create `GE_OnLocationEntered` (GameEventID)
2. Create `SO_Condition_Event_BanditCampReached`:
   - **Game Event**: `GE_OnLocationEntered`
   - **Target Value**: `ID_Location_BanditCamp`
3. Add condition to task's **Conditions** list

---

### Example 3: Discovery Task - "Find 3 Clues"

A Discovery Task completes when the player finds multiple unique items. Each discoverable item is represented by a condition in the task's Conditions list.

**Step 1: Create the Discovery Task Asset**
1. **Create** → **HelloDev** → **Quest System** → **Scriptable Objects** → **Tasks** → **Discovery Task**
2. Name it `SO_Task_FindClues`
3. Configure:
   - **Dev Name**: `Task_FindClues`
   - **Display Name**: `"Find Clues ({current}/{required})"`
   - **Required Discoveries**: `0` (means all conditions must be fulfilled, or set to specific number like `2` for "find 2 of 3")

**Step 2: Create Clue IDs and Event**

1. Create each clue ID in `IDs/Clues/`:
   - `ID_Clue_Footprint`
   - `ID_Clue_BloodStain`
   - `ID_Clue_TornCloth`
2. Create `GE_OnClueFound` (GameEventID)

**Step 3: Create Conditions for Each Clue**

Create 3 ID Conditions (one per discoverable item):
- `SO_Condition_Event_FootprintFound` → Game Event ID: `GE_OnClueFound`, Target: `ID_Clue_Footprint`
- `SO_Condition_Event_BloodStainFound` → Game Event ID: `GE_OnClueFound`, Target: `ID_Clue_BloodStain`
- `SO_Condition_Event_TornClothFound` → Game Event ID: `GE_OnClueFound`, Target: `ID_Clue_TornCloth`

Add ALL 3 conditions to the task's **Conditions** list. Each condition can only be fulfilled once (duplicate-protected).

**Step 4: Clue Interaction Code**

```csharp
public class CluePickup : MonoBehaviour
{
    [SerializeField] private ID_SO clueId;
    [SerializeField] private GameEventID_SO onClueFound;

    public void Pickup()
    {
        onClueFound.Raise(clueId);
        gameObject.SetActive(false);
    }
}
```

---

### Example 4: Sequential Task Group - "Talk Then Go"

Sometimes you need tasks completed in order.

**Graph Structure:**
```
TaskGroupNode (Mode: Sequential)
    │
    ├─► TaskNode: "Talk to the Merchant"
    │        │ Then
    │        ▼
    └─► TaskNode: "Go to the Market"
```

**Configuration:**
1. TaskGroupNode: **Execution Mode** = `Sequential`
2. Connect first TaskNode's **Then** port to second TaskNode's **In** port

The second task won't become active until the first completes.

---

### Example 5: Parallel Task Group - "Collect AND Kill"

For tasks that can be done simultaneously:

**Graph Structure:**
```
TaskGroupNode (Mode: Parallel)
    │
    ├─► TaskNode: "Collect 5 Herbs"
    └─► TaskNode: "Kill 3 Wolves"
```

**Configuration:**
1. TaskGroupNode: **Execution Mode** = `Parallel`
2. Both TaskNodes connected to same TaskGroup's **Tasks** port

Both tasks are active immediately. Stage progresses when BOTH complete.

#### Connection Pattern Warning

When using Parallel mode, ensure ALL tasks connect directly to the TaskGroup's `Tasks` port:

**CORRECT - Both tasks from same port:**
```
TaskGroupNode.Tasks ─┬─► TaskNode A
                     └─► TaskNode B
```

**INCORRECT - Sequential chain in Parallel group:**
```
TaskGroupNode.Tasks ─► TaskNode A ─Then─► TaskNode B
                                        ↑
                                  (This makes B wait for A!)
```

> **Common Mistake:** Don't connect TaskNode.Then → TaskNode.In within a Parallel group. This creates a sequential dependency even though the group is marked Parallel. Task B won't activate until Task A completes, defeating the purpose of parallel execution.

---

### Example 6: Optional Tasks - "Find 2 of 4 Clues"

For optional X-of-Y completion:

**Graph Structure:**
```
TaskGroupNode (Mode: OptionalXofY, RequiredCount: 2)
    │
    ├─► TaskNode: "Clue A"
    ├─► TaskNode: "Clue B"
    ├─► TaskNode: "Clue C"
    └─► TaskNode: "Clue D"
```

**Configuration:**
1. TaskGroupNode: **Execution Mode** = `OptionalXofY`
2. TaskGroupNode: **Required Count** = `2`
3. All 4 TaskNodes connected to TaskGroup

Stage progresses when ANY 2 of the 4 tasks complete.

---

## Related Documentation

**Tutorials:**
- [Quest Creation Tutorial](tutorial-creating-quests.md) - Building linear, multi-stage, and branching quests

**Reference:**
- [Designer Workflow Guide](quest-graph-designer-workflow.md) - Main workflow reference
- [Quest System Overview](overview.md) - System architecture
- [Tasks Reference](tasks.md) - Technical task documentation
- [Quest Graph Editor Guide](quest-graph-editor-guide.md) - For programmers
