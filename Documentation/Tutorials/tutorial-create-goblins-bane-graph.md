# Tutorial: Creating "Goblin's Bane" Quest in the Graph Editor

This tutorial walks through creating the complete "Goblin's Bane" quest using the Quest Graph Editor. This quest demonstrates:
- 4 stages with sequential flow
- Conditional stage skipping (Investigation -> Boss Confrontation)
- Different execution modes (Sequential, Parallel)
- Multiple task types (Discovery, Location, Int, Timed)

---

## Quest Overview

**Goblin's Bane** is a main story quest with the following structure:

```
Stage 0: Investigation
    └─> TaskGroup: "Investigate Attacks" (Sequential)
        └─> Task: InvestigateAttacks (Discovery, 3 discoveries)
    └─> Transitions:
        - If "campsite found" condition: Skip to Stage 20 (priority 1)
        - Default: Continue to Stage 10 (priority 0)

Stage 10: Tracking & Combat
    └─> TaskGroup: "Track and Clear" (Parallel)
        └─> Task: TrackGoblinCamp (Location)
        └─> Task: Kill_Goblin (Int, 5 kills)
    └─> Transitions: Continue to Stage 20

Stage 20: Boss Confrontation
    └─> TaskGroup: "Find and Defeat Chief" (Sequential)
        └─> Task: FindGoblinsCampsite (Location)
        └─> Task: DefeatGoblinChief (Timed, 180 seconds)
    └─> Transitions: Continue to Stage 30

Stage 30: Resolution (Terminal)
    └─> TaskGroup: "Return to Village" (Sequential)
        └─> Task: ReturnToVillage (Location)
```

---

## Prerequisites

Before starting, ensure you have:
1. Unity 6.2+ with Graph Toolkit installed
2. Quest System package imported
3. The BasicQuestExample assets (tasks, conditions, rewards)

---

## Part 1: Create the Quest Graph Asset

### Step 1.1: Create New Quest Graph
1. In Project window, navigate to `BasicQuestExample/Graphs/Quests/`
2. Right-click > **Create > HelloDev > Quest System > Graphs > Quest Graph**
3. Name it `Graph_GoblinsBane`
4. Double-click to open in Graph Editor

### Step 1.2: Configure Graph Properties
1. With the graph open, look at the **Inspector** panel
2. Set the following properties:
   - **Dev Name**: `GoblinsBane`
   - **Quest Id**: (auto-generated, or copy from existing: `8d2a2220-1f56-4561-a04f-471478376098`)
   - **Quest Type**: Select `SO_QuestType_Main` from `BasicQuestExample/ScriptableObjects/QuestTypes/`
   - **Recommended Level**: `5`
   - **Display Name**: Select from Quests localization table (key: `736137437036544`)
   - **Description**: Select from Quests localization table (key: `736338700713984`)
   - **Location**: Select from Locations localization table (key: `735651774382080`)
   - **Sprite**: Select quest icon from `BasicQuestExample/Sprites/`

---

## Part 2: Create Blackboard Variables

The Blackboard holds reusable values that connect to node ports. Create these variables:

### Step 2.1: Open Blackboard Panel
1. In Graph Editor, click **View > Blackboard** (or press `B`)
2. The Blackboard panel appears on the left side

### Step 2.2: Create Quest Identity Variables
Right-click in Blackboard > **Create Variable** for each:

| Variable Name | Type | Initial Value |
|---------------|------|---------------|
| DevName | `String` | `GoblinsBane` |
| RecommendedLevel | `Int` | `5` |
| Quest Type | `QuestType_SO` | (assign Main quest type) |
| Display Name | `LocalizedString` | (assign from table) |
| Description | `LocalizedString` | (assign from table) |
| Location | `LocalizedString` | (assign from table) |
| Sprite | `Sprite` | (assign quest icon) |

### Step 2.3: Create Task Group Name Variables
For cleaner graphs, create string variables for task group names:

| Variable Name | Type | Initial Value |
|---------------|------|---------------|
| TG_InvestigateAttacks | `String` | `Investigate Attacks` |
| TG_TrackAndClear | `String` | `Track and Clear` |
| TG_FindDefeatChief | `String` | `Find and Defeat Chief` |
| TG_ReturnToVillage | `String` | `Return to Village` |

---

## Part 3: Add the Start Node

### Step 3.1: Locate or Add QuestStartNode
1. A `QuestStartNode` should already exist (entry point)
2. If not, right-click canvas > **Create Node > Quest > Quest Start Node**

### Step 3.2: Configure Start Node
1. Select the start node
2. In Inspector, set **Output Mode** to `QuestFlow` (connects to QuestNode)
3. Optionally add start conditions in the inspector list

---

## Part 4: Add the Quest Node

### Step 4.1: Create QuestNode
1. Right-click canvas > **Create Node > Quest > Quest Node**
2. Position it to the right of the Start Node

### Step 4.2: Configure QuestNode Options
Select the QuestNode and set in Inspector:
- **Use Quest Asset**: `false` (we're defining inline)
- **Start Condition Count**: `2` (if you have start conditions)
- **Failure Condition Count**: `1`
- **Reward Count**: `1`

### Step 4.3: Understanding QuestNode Output Ports

In Define mode, QuestNode has two output ports:

| Port | Type | Purpose |
|------|------|---------|
| **Then** | `QuestFlow` | Connects to the next quest in a questline chain |
| **Stages** | `StageFlow` | Connects to the first StageNode of this quest |

### Step 4.4: Connect Blackboard Variables to QuestNode
Drag each Blackboard variable onto the graph, then connect:

1. **DevName variable** -> QuestNode `Dev Name` port
2. **RecommendedLevel variable** -> QuestNode `Recommended Level` port
3. **Quest Type variable** -> QuestNode `Quest Type` port
4. **Display Name variable** -> QuestNode `Display Name` port
5. **Description variable** -> QuestNode `Description` port
6. **Location variable** -> QuestNode `Location` port
7. **Sprite variable** -> QuestNode `Sprite` port

### Step 4.5: Connect Flow
Draw a wire from **QuestStartNode** `First Quest` output -> **QuestNode** `In` input

---

## Part 5: Add Stage Nodes

### Step 5.1: Create StageNodes
1. Right-click canvas > **Create Node > Stage > Stage Node**
2. Create 4 StageNodes, positioning them below the QuestNode

### Step 5.2: Connect Stages to Quest
1. Connect QuestNode `Stages` output -> Stage 0 `From` input
2. For basic linear progression, chain stages directly:
   - Stage 10 `Then` -> Stage 20 `From`
   - Stage 20 `Then` -> Stage 30 `From`
3. Stage 30 is terminal, so it has no `Then` output
4. **Important:** For Stage 0's transitions, we'll use TransitionNodes (see Part 5.5)

> **Why use 0, 10, 20, 30?** Leaving gaps between stage indices allows future expansion. If you later need to add a stage between Investigation and Tracking, you can use index 5 without renumbering existing stages.

> **Note:** StageNode's `From` port accepts multiple connections, allowing both direct connections and TransitionNodes to target the same stage.

### Step 5.3: Configure Stage 0 (Investigation)

**Options:**
- **Use Stage Subgraph**: `false`
- **Has Player Choices**: `false`

**Ports (connect or set inline):**
- **Stage Name**: `Investigation`
- **Stage Index**: `0`
- **Journal Entry**: (select from Stages localization table)
- **Is Terminal**: `false`
- **Is Optional**: `false`
- **Is Hidden**: `false`

### Step 5.4: Configure Stage 10 (Tracking & Combat)

**Ports:**
- **Stage Name**: `Tracking & Combat`
- **Stage Index**: `10`
- **Journal Entry**: (select from table)
- **Is Terminal**: `false`

### Step 5.5: Configure Stage 20 (Boss Confrontation)

**Ports:**
- **Stage Name**: `Boss Confrontation`
- **Stage Index**: `20`
- **Journal Entry**: (select from table)
- **Is Terminal**: `false`

### Step 5.6: Configure Stage 30 (Resolution)

**Ports:**
- **Stage Name**: `Resolution`
- **Stage Index**: `30`
- **Journal Entry**: (select from table)
- **Is Terminal**: `true` (quest completes when this stage finishes)

---

## Part 5.5: Add TransitionNodes for Conditional Progression

The Goblin's Bane quest has a conditional skip path: if the player has already found the goblin campsite, they can skip Stage 10 (Tracking & Combat) and go directly to Stage 20 (Boss Confrontation).

### Understanding TransitionNode vs Direct Connections

**Direct Stage→Stage connections** (like Stage 10→Stage 20) create implicit transitions:
- Trigger: `OnGroupsComplete` (fires when all task groups complete)
- Priority: `0`
- No conditions

**TransitionNode** provides explicit control over:
- **Trigger**: When the transition activates
- **Priority**: Order when multiple transitions are valid (higher = first)
- **Conditions**: Optional conditions that must be met

### Step 5.5.1: Create TransitionNodes for Stage 0

Stage 0 (Investigation) needs two outgoing transitions:
1. **Normal path**: Continue to Stage 10 (priority 0)
2. **Skip path**: If campsite found, skip to Stage 20 (priority 1)

Create two TransitionNodes:

**TransitionNode 1: Normal Path**
1. Right-click canvas > **Create Node > Flow > Transition Node**
2. Position it between Stage 0 and Stage 10
3. Configure options:
   - **Trigger**: `OnGroupsComplete`
   - **Priority**: `0`
   - **Label**: `Normal Path` (optional)
4. Connect wires:
   - Stage 0 `Then` -> TransitionNode `From`
   - TransitionNode `To` -> Stage 10 `From`

**TransitionNode 2: Conditional Skip**
1. Create another TransitionNode
2. Position it between Stage 0 and Stage 20
3. Configure options:
   - **Trigger**: `OnConditionsMet`
   - **Priority**: `1` (higher than normal path)
   - **Label**: `Skip to Boss` (optional)
4. Connect wires:
   - Stage 0 `Then` -> TransitionNode `From`
   - TransitionNode `To` -> Stage 20 `From`
5. Add condition:
   - Create or select ConditionContextNode with `SO_Condition_Event_ID_EnterLocation_GoblinCampsite`
   - Connect ConditionContextNode output -> TransitionNode `Conditions`

### Step 5.5.2: Why This Works

When Stage 0 completes:
1. The system evaluates all transitions from Stage 0 by priority (highest first)
2. **Skip path (priority 1)**: Checks if "campsite found" condition is met
   - If true: Transition to Stage 20 fires
   - If false: Continue to next transition
3. **Normal path (priority 0)**: Always valid (OnGroupsComplete with no conditions)
   - Fires if skip path didn't

This allows players who found the campsite during investigation to skip the tracking stage.

### Visual Layout: Stage 0 Transitions

```
                                    +------------------+
                                +-->| TransitionNode   |----+
                                |   | Skip to Boss     |    |
+---------------+               |   | OnConditionsMet  |    |    +---------------+
|   Stage 0     |---------------+   | Priority: 1      |    +--->|   Stage 20    |
| Investigation |               |   +------------------+         | Boss          |
+---------------+               |          ^                     +---------------+
                                |          |
                                |   [ConditionContext]
                                |   "Campsite Found"
                                |
                                |   +------------------+
                                +-->| TransitionNode   |----+
                                    | Normal Path      |    |    +---------------+
                                    | OnGroupsComplete |    +--->|   Stage 10    |
                                    | Priority: 0      |         | Tracking      |
                                    +------------------+         +---------------+
```

---

## Part 6: Add Task Group Context Nodes

TaskGroupContextNodes are container nodes that hold task blocks. Each stage receives task groups via its `Task Groups` input port.

### Step 6.1: Create TaskGroupContextNode for Stage 0

1. Right-click canvas > **Create Node > Context > Task Group Context**
2. Position it near Stage 0

### Step 6.2: Configure TaskGroupContextNode

TaskGroupContextNode uses ports for identity and options for behavior.

**Ports:**

- **Group Name** (`String` input) - Developer-friendly name for this task group. Connect from Blackboard variable or set inline.
- **Then** (`StageFlow` output) - Connects TO the StageNode's `Task Groups` input port.

**Options (in Node Properties panel):**

- **Execution Mode** (`TaskExecutionMode`) - How tasks in this group execute:
  - `Sequential` - Tasks must complete in order (top to bottom)
  - `Parallel` - All tasks active simultaneously, complete in any order
  - `AnyOrder` - Same as Parallel
  - `OptionalXofY` - Only a subset of tasks need to complete
- **Required Count** (`int`) - Only visible when Execution Mode is `OptionalXofY`. Specifies how many tasks must complete.
- **Fail On Any Task Failure** (`bool`) - Inspector-only option. If true, the entire group fails when any task fails.

### Step 6.3: Connect TaskGroupContextNode to Stage

**Important:** TaskGroupContextNode's `Then` output connects TO StageNode's `Task Groups` input:

```
[TaskGroupContextNode]         [StageNode]
       Then  ─────────────>  Task Groups
    (output)                    (input)
```

1. Draw a wire from TaskGroupContextNode `Then` -> StageNode `Task Groups`
2. Multiple TaskGroupContextNodes can connect to the same stage

### Step 6.4: Configure Task Group for Stage 0

1. Connect **TG_InvestigateAttacks** variable -> TaskGroupContextNode `Group Name` port
2. Set **Execution Mode**: `Sequential`
3. Connect `Then` output -> Stage 0 `Task Groups` input

### Step 6.5: Understanding Task Block Modes

Task blocks can be configured in two modes:

**Asset Mode** - Reference an existing Task_SO asset:
- **Use Task Asset**: `true`
- **Task Asset**: Drag an existing Task_SO file

**Define Mode** - Create task data inline in the graph:
- **Use Task Asset**: `false`
- Configure task properties via ports

When **Use Task Asset** is `false`, the block shows inline configuration ports:

**Common Ports (all task types):**
- **Dev Name** (`String`) - Internal developer name for this task
- **Display Name** (`LocalizedString`) - Localized name shown to players
- **Description** (`LocalizedString`) - Localized task description

**Condition Ports (controlled by options):**
- **Trigger Condition Count** option controls how many `Trigger Condition` ports appear
  - These are conditions that, when met, complete the task (or increment progress for Int/Discovery tasks)
- **Failure Condition Count** option controls how many `Fail Condition` ports appear
  - These are conditions that, when met, cause the task to fail
- Each condition port accepts a `Condition_SO` reference

**Important:** BoolTask has NO internal completion logic - the `Trigger Condition` list is the ONLY way to complete it. All other task types can complete via internal logic (counter, timer, location match) but can also use conditions as additional triggers.

**Type-Specific Ports:**

| Task Block Type | Extra Ports |
|-----------------|-------------|
| TaskBoolBlock | (none) |
| TaskLocationBlock | (none) |
| TaskIntBlock | **Required Count** (`int`) - How many times condition must trigger |
| TaskStringBlock | **Target Value** (`string`) - String value to match |
| TaskDiscoveryBlock | **Required Discoveries** (`int`) - How many conditions must be fulfilled (0 = all) |
| TaskTimedBlock | **Time Limit** (`float`) - Seconds allowed; **Fail Quest On Expire** (`bool`) |

### Step 6.6: Stage 0 Task Group - "Investigate Attacks"

Create the TaskGroupContextNode and configure:

**TaskGroupContextNode Settings:**
- **Group Name**: `Investigate Attacks` (connect from Blackboard or type inline)
- **Execution Mode**: `Sequential`
- Connect **Then** → Stage 0 **Task Groups**

**Task: InvestigateAttacks (Discovery)**

1. Right-click inside context > **Add Block > Task Discovery Block**
2. Set **Use Task Asset**: `false`
3. Configure ports:
   - **Dev Name**: `InvestigateAttacks`
   - **Display Name**: (select from Tasks localization table)
   - **Description**: (select from Tasks localization table)
   - **Required Discoveries**: `3`
4. Set **Trigger Condition Count**: `3`
5. Set **Failure Condition Count**: `0` (investigation is open-ended, no failure)
6. Connect condition ports:
   - **Trigger Condition 1** → `SO_Condition_Event_ID_Discover_Footprints`
   - **Trigger Condition 2** → `SO_Condition_Event_ID_Discover_BrokenCart`
   - **Trigger Condition 3** → `SO_Condition_Event_ID_Discover_Witness`

### Step 6.7: Stage 10 Task Group - "Track and Clear"

Create a new TaskGroupContextNode:

**TaskGroupContextNode Settings:**
- **Group Name**: `Track and Clear`
- **Execution Mode**: `Parallel` (both tasks active simultaneously)
- Connect **Then** → Stage 10 **Task Groups**

**Task 1: TrackGoblinCamp (Location)**

1. Add **Task Location Block**
2. Set **Use Task Asset**: `false`
3. Configure ports:
   - **Dev Name**: `TrackGoblinCamp`
   - **Display Name**: (select from localization table)
   - **Description**: (select from localization table)
4. Set **Trigger Condition Count**: `1`
5. Set **Failure Condition Count**: `1` (stealth failure)
6. Connect trigger condition:
   - **Trigger Condition 1** → `SO_Condition_Event_ID_EnterLocation_GoblinCampsite`
7. Connect failure condition:
   - **Fail Condition 1** → `SO_Condition_Event_ID_Alert_GoblinScout` (goblin scout spotted you!)

**Task 2: Kill_Goblin (Int)**

1. Add **Task Int Block**
2. Set **Use Task Asset**: `false`
3. Configure ports:
   - **Dev Name**: `Kill_Goblin`
   - **Display Name**: (select from localization table)
   - **Description**: (select from localization table)
   - **Required Count**: `5`
4. Set **Trigger Condition Count**: `1`
5. Set **Failure Condition Count**: `1` (too many escapees)
6. Connect trigger condition:
   - **Trigger Condition 1** → `SO_Condition_Event_ID_Kill_Goblin`
7. Connect failure condition:
   - **Fail Condition 1** → `SO_Condition_Event_Int_GoblinsEscaped` (3+ goblins escaped = task fails)

### Step 6.8: Stage 20 Task Group - "Find and Defeat Chief"

Create a new TaskGroupContextNode:

**TaskGroupContextNode Settings:**
- **Group Name**: `Find and Defeat Chief`
- **Execution Mode**: `Sequential` (must find campsite before fighting chief)
- Connect **Then** → Stage 20 **Task Groups**

**Task 1: FindGoblinsCampsite (Location)**

1. Add **Task Location Block**
2. Set **Use Task Asset**: `false`
3. Configure ports:
   - **Dev Name**: `FindGoblinsCampsite`
   - **Display Name**: (select from localization table)
   - **Description**: (select from localization table)
4. Set **Trigger Condition Count**: `1`
5. Set **Failure Condition Count**: `0` (exploration task, no failure)
6. Connect trigger condition:
   - **Trigger Condition 1** → `SO_Condition_Event_ID_EnterLocation_GoblinCampsite`

**Task 2: DefeatGoblinChief (Timed)**

1. Add **Task Timed Block**
2. Set **Use Task Asset**: `false`
3. Configure ports:
   - **Dev Name**: `DefeatGoblinChief`
   - **Display Name**: (select from localization table)
   - **Description**: (select from localization table)
   - **Time Limit**: `180` (3 minutes to defeat the chief)
   - **Fail Quest On Expire**: `false` (only fails the task, not the entire quest)
4. Set **Trigger Condition Count**: `1`
5. Set **Failure Condition Count**: `0` (timer expiration is built-in failure for TimedTask)
6. Connect trigger condition:
   - **Trigger Condition 1** → `SO_Condition_Event_ID_Defeat_GoblinChief`

> **Note:** TimedTask has built-in failure when the timer expires. You don't need to add a failure condition for timer expiration. However, you CAN add additional failure conditions (e.g., player death) if desired.

### Step 6.9: Stage 30 Task Group - "Return to Village"

Create a new TaskGroupContextNode:

**TaskGroupContextNode Settings:**
- **Group Name**: `Return to Village`
- **Execution Mode**: `Sequential`
- Connect **Then** → Stage 30 **Task Groups**

**Task: ReturnToVillage (Location)**

1. Add **Task Location Block**
2. Set **Use Task Asset**: `false`
3. Configure ports:
   - **Dev Name**: `ReturnToVillage`
   - **Display Name**: (select from localization table)
   - **Description**: (select from localization table)
4. Set **Trigger Condition Count**: `1`
5. Set **Failure Condition Count**: `0` (resolution task, no failure)
6. Connect trigger condition:
   - **Trigger Condition 1** → `SO_Condition_Event_ID_EnterLocation_Village`

---

## Part 7: Add Conditions and Rewards

### Step 7.1: Configure Start Conditions
On the QuestNode, connect to the `Start Condition` ports:
1. `SO_Condition_CanStartQuest_GoblinsBane` (level requirement)
2. Additional prerequisite conditions

### Step 7.2: Configure Failure Conditions
Connect to the `Failure Condition` port:
1. `SO_Condition_QuestFailed_Timer` (if quest has time limit)

### Step 7.3: Configure Rewards
Connect to the `Reward` port:
- Reward Type: `SO_RewardType_Gold`
- Amount: `1500`

---

## Part 8: Validate the Graph

### Step 8.1: Run Validation
1. In Graph Editor toolbar, click **Validate** (checkmark icon)
2. Review the Validation Results panel

### Step 8.2: Expected Warnings (OK to ignore)
- "Variable node not connected" - Unused Blackboard variables

### Step 8.3: Must Fix Errors
- "Quest node has no stages" - Connect Stages output to first StageNode
- "Stage has no task groups" - Connect TaskGroupContextNode to each stage
- "Task group has no valid tasks" - Each task block needs a configured task
- "Terminal stage has Then connection" - Stage 30 should have IsTerminal = true

---

## Part 9: Export to ScriptableObject

### Step 9.1: Set Target Asset
1. In Graph Inspector, find **Target Asset** field
2. Either:
   - Leave empty to create new asset on export
   - Drag existing `SO_Quest_GoblinsBane` to update it

### Step 9.2: Export
1. Click **Export** button in toolbar
2. If no target asset, choose save location
3. Verify the generated `Quest_SO` matches expected structure

---

## Visual Layout Reference

```
+------------------+      +-------------------+
| QuestStartNode   | ---> |    QuestNode      | ---> (Then: Next Quest)
|                  |      |                   |
| Output: QuestFlow|      +-------------------+
+------------------+               |
                             (Stages)
                                   |
                                   v
+---------------+      +---------------+      +---------------+      +---------------+
|   Stage 0     | ---> |   Stage 10    | ---> |   Stage 20    | ---> |   Stage 30    |
| Investigation |      | Tracking      |      | Boss          |      | Resolution    |
| Index: 0      |      | Index: 10     |      | Index: 20     |      | Terminal: Yes |
+---------------+      +---------------+      +---------------+      +---------------+
       ^                      ^                      ^                      ^
       |                      |                      |                      |
  Task Groups            Task Groups            Task Groups            Task Groups
       |                      |                      |                      |
+---------------+      +---------------+      +---------------+      +---------------+
| TaskGroup     |      | TaskGroup     |      | TaskGroup     |      | TaskGroup     |
| Context Node  |      | Context Node  |      | Context Node  |      | Context Node  |
| "Investigate" |      | "Track/Clear" |      | "Find/Defeat" |      | "Return"      |
| Sequential    |      | Parallel      |      | Sequential    |      | Sequential    |
+---------------+      +---------------+      +---------------+      +---------------+
       |                   |      |              |      |                   |
   [Discovery]        [Location] [Int]      [Location] [Timed]         [Location]
   Task Block         Task Block Task       Task Block Task            Task Block
                                 Block                 Block
```

### Connection Detail: TaskGroupContextNode to StageNode

```
+-------------------------+          +------------------+
| TaskGroupContextNode    |          |    StageNode     |
|                         |          |                  |
| Group Name: [port] <----+-- From Blackboard variable  |
| Execution Mode: [opt]   |          |                  |
|                         |          |  Task Groups <---+-- (StageFlow input)
| +---------------------+ |          |                  |
| | TaskBoolBlock       | |    +---->|  From            |
| | "Talk to NPC"       | |    |     |  Then ---------> (to next stage)
| +---------------------+ |    |     +------------------+
|                         |    |
| Then -------------------+----+
| (StageFlow output)      |
+-------------------------+
```

---

## Summary

Creating "Goblin's Bane" in the Graph Editor involves:

1. **Create Graph Asset** - New Quest Graph with identity properties
2. **Add Blackboard Variables** - Reusable values for ports
3. **Add Start Node** - Entry point with output mode
4. **Add Quest Node** - Configure inline quest data
5. **Add Stage Nodes** - Chain stages via `Then` ports, connect first to Quest's `Stages`
5.5. **Add TransitionNodes** - For conditional progression (skip paths based on conditions)
6. **Add TaskGroupContextNodes** - Configure each with tasks, connect `Then` to Stage's `Task Groups`
7. **Add Conditions/Rewards** - Start/failure conditions, completion rewards
8. **Validate** - Check for errors
9. **Export** - Generate Quest_SO asset

### Key Connection Patterns

| From | Port | To | Port | Flow Type |
|------|------|----|------|-----------|
| QuestStartNode | First Quest | QuestNode | In | QuestFlow |
| QuestNode | Stages | StageNode (first) | From | StageFlow |
| StageNode | Then | StageNode (next) | From | StageFlow |
| StageNode | Then | TransitionNode | From | StageFlow |
| TransitionNode | To | StageNode | From | StageFlow |
| TaskGroupContextNode | Then | StageNode | Task Groups | StageFlow |

**Note:** StageNode's `From` port accepts multiple connections (multi-capacity), allowing both direct stage connections and TransitionNodes to target the same stage.

---

## Task Reference

For reference, here are the task assets used in this quest:

| Task Asset | Type | Description | Trigger Conditions | Failure Conditions |
|------------|------|-------------|-------------------|-------------------|
| `SO_Task_InvestigateAttacks` | Discovery | Examine 3 clues (footprints, cart, witness) | 3 (one per clue) | None |
| `SO_Task_TrackGoblinCamp` | Location | Find the goblin camp location | 1 | 1 (scout alert) |
| `SO_Task_Kill_Goblin` | Int | Defeat 5 goblins (counter task) | 1 | 1 (goblins escaped) |
| `SO_Task_FindGoblinsCampsite` | Location | Locate the chief's tent | 1 | None |
| `SO_Task_DefeatGoblinChief` | Timed | Defeat the goblin chief within time limit (boss fight) | 1 | Built-in (timer) |
| `SO_Task_ReturnToVillage` | Location | Return to the village elder | 1 | None |

Located in: `BasicQuestExample/ScriptableObjects/Quests/Goblin's Bane/Tasks/`

### Task Condition Summary

| Task Type | Conditions Required? | Failure Behavior |
|-----------|---------------------|------------------|
| **BoolTask** | **Yes** (only way to complete) | Via failure conditions |
| **IntTask** | Optional (internal counter) | Via failure conditions |
| **LocationTask** | Optional (internal location match) | Via failure conditions |
| **TimedTask** | Optional (internal timer) | Built-in timer expiration OR failure conditions |
| **DiscoveryTask** | Optional (internal discovery progress) | Via failure conditions |
| **StringTask** | Optional (internal string match) | Via failure conditions |

---

## Condition Reference

Each task uses conditions to determine completion and failure. Here are the condition assets referenced:

### Trigger Conditions (Complete Task)

| Task | Condition Asset | Purpose |
|------|-----------------|---------|
| InvestigateAttacks | `SO_Condition_Event_ID_Discover_Footprints` | Discovery clue 1 |
| InvestigateAttacks | `SO_Condition_Event_ID_Discover_BrokenCart` | Discovery clue 2 |
| InvestigateAttacks | `SO_Condition_Event_ID_Discover_Witness` | Discovery clue 3 |
| TrackGoblinCamp | `SO_Condition_Event_ID_EnterLocation_GoblinCampsite` | Location reached |
| Kill_Goblin | `SO_Condition_Event_ID_Kill_Goblin` | Enemy kill counter |
| FindGoblinsCampsite | `SO_Condition_Event_ID_EnterLocation_GoblinCampsite` | Location reached |
| DefeatGoblinChief | `SO_Condition_Event_ID_Defeat_GoblinChief` | Boss defeated |
| ReturnToVillage | `SO_Condition_Event_ID_EnterLocation_Village` | Location reached |

### Failure Conditions (Fail Task)

| Task | Condition Asset | Purpose |
|------|-----------------|---------|
| TrackGoblinCamp | `SO_Condition_Event_ID_Alert_GoblinScout` | Stealth failed - goblin scout spotted you |
| Kill_Goblin | `SO_Condition_Event_Int_GoblinsEscaped` | Too many goblins escaped (>= 3) |
| DefeatGoblinChief | *(Built-in timer)* | Timer expiration fails the task |

Located in: `BasicQuestExample/ScriptableObjects/Conditions/`
