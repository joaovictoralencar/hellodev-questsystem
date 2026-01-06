 # Quest Graph Editor - Designer Workflow Guide

*Version 1.1 | For: Game Designers | Prerequisites: Unity Editor basics*

This guide shows how to use the Quest Graph Editor to create complex questlines with branching paths, player choices, and multiple endings - all without writing code.

---

## Table of Contents

1. [Quick Start: Your First Quest](#quick-start-your-first-quest)
2. [Understanding the Graph Hierarchy](#understanding-the-graph-hierarchy)
3. [Building Branching Quests](#building-branching-quests)
4. [Working with Task Groups](#working-with-task-groups)
5. [Stage Indexing Best Practices](#stage-indexing-best-practices)
6. [Conditions and Gating](#conditions-and-gating)
7. [Event Triggers](#event-triggers)
8. [World Flags and Consequences](#world-flags-and-consequences)
9. [Rewards](#rewards)
10. [Creating QuestLines](#creating-questlines)
11. [Validation and Debugging](#validation-and-debugging)
12. [Visual Design Tips](#visual-design-tips)
13. [Complete Example: Building "The Merchant's Dilemma"](#complete-example-building-the-merchants-dilemma)
14. [Workflow Summary](#workflow-summary)

> **Looking for step-by-step tutorials?**
> - [Task Creation Tutorial](tutorial-creating-tasks.md) - Creating Bool, Int, Location, and Discovery tasks
> - [Quest Creation Tutorial](tutorial-creating-quests.md) - Building linear, multi-stage, and branching quests

---

## Quick Start: Your First Quest

### Step 1: Create a New Quest Graph

1. **Right-click** in the Project window
2. Select **Create > HelloDev > Quest System > Quest Graph**
3. Name it `MyFirstQuest` (creates `MyFirstQuest.quest`)
4. **Double-click** to open in the Graph Editor

### Step 2: Add Basic Nodes

1. **Right-click** in the graph canvas
2. Add a **QuestStartNode** (entry point - required)
3. Add a **StageNode** (your first objective)
4. **Connect** QuestStartNode's "FirstStage" port to StageNode's "In" port

> **Note:** At this point, you'll see validation errors like *"Quest must have at least one terminal stage"* and *"Non-terminal stage has no output connections"*. This is expected! These errors will be resolved as you complete the remaining steps.

### Step 3: Configure the Stage

Select the StageNode and configure in the **Node Options** (shown on the node and in Inspector):

**On the Node Header:**
- **Stage Index** = `0` (first stage)
- **Stage Name** = `"Introduction"` (developer-friendly name)
- **Is Terminal** = `true` (since this is our only stage, it ends the quest)

**In the Inspector Panel** (additional options):
- **Journal Entry** = Select a LocalizedString for the quest journal
- **Stage Icon** = Optional sprite for UI display

> **Note:** For a simple single-stage quest, mark `Is Terminal = true`. The validation errors will now be resolved.

### Step 3.5: Add Tasks (Optional for First Quest)

For a complete quest, you'll want to add tasks. There are two approaches:

**Option A: Reference Existing Task_SO Assets**
1. Right-click canvas → Add **TaskNode**
2. In the TaskNode, set **Task Asset** to an existing Task_SO (e.g., `SO_Task_TalkToMerchant`)
3. Connect the TaskNode to your stage's workflow

**Option B: Use a TaskGroup Subgraph**
1. First create a TaskGroupGraph asset with your tasks
2. In your QuestGraph, add a **TaskGroupNode**
3. Set **Subgraph** to reference your TaskGroupGraph

> **Tip:** For your first quest, you can skip tasks entirely. The stage will complete immediately when activated.

### Step 4: Save and Test

- **Ctrl+S** to save the graph
- Unity automatically converts it to a **Quest_SO** asset
- Reference the Quest_SO in your QuestManager

---

## Understanding the Graph Hierarchy

The Quest Graph Editor uses a hierarchical structure:

```
QuestLineGraph (.questline)
│   Contains multiple quests in sequence or parallel
│
└── QuestGraph (.quest)
    │   Contains stages with branching and choices
    │
    └── StageGraph (.stage) [optional subgraph]
        │   Detailed stage with task groups
        │
        └── TaskGroupGraph (.taskgroup) [optional subgraph]
                Individual tasks to complete
```

### When to Use Each Type

| Graph Type | Use When |
|------------|----------|
| QuestLineGraph | Grouping related quests (story arc, faction questline) |
| QuestGraph | Single quest with stages and branches |
| StageGraph | Complex stage needing its own graph (optional) |
| TaskGroupGraph | Reusable task sets (e.g., "Collect 3 Herbs" used in multiple quests) |

### Node Connection Reference

Every graph type has a **Start Node** and specific nodes you can connect.

#### QuestLineGraph
| Node | Ports | Connect To |
|------|-------|------------|
| **QuestLineStartNode** | `FirstQuest →` | QuestRefNode |
| **QuestRefNode** | `← In`, `Out →`, `Then →`, `Else →` | Other QuestRefNodes |

#### QuestGraph
| Node | Ports | Connect To |
|------|-------|------------|
| **QuestStartNode** | `FirstStage →` | StageNode |
| **StageNode** | `← In`, `TaskGroups →`, `Then →`, `Else →`, `Choices →` | TaskGroupNode, StageNode, ChoiceNode |
| **ChoiceNode** | `← In`, `Target →` | StageNode |
| **ConditionGateNode** | `← In`, `Then →`, `Else →` | StageNode, utility nodes |
| **EventTriggerNode** | `← In`, `Then →` | StageNode, utility nodes |
| **WorldFlagSetNode** | `← In`, `Then →` | StageNode, utility nodes |
| **RewardNode** | `← In`, `Then →` | StageNode, utility nodes |
| **TaskGroupNode** | `← In`, `Tasks →`, `Then →`, `Else →` | TaskNode, TaskGroupNode |
| **TaskNode** | `← In`, `Then →` | TaskNode |

#### StageGraph (subgraph)
| Node | Ports | Connect To |
|------|-------|------------|
| **StageStartNode** | `FirstTaskGroup →` | TaskGroupNode |
| **TaskGroupNode** | `← In`, `Tasks →`, `Then →`, `Else →` | TaskNode, TaskGroupNode |
| **TaskNode** | `← In`, `Then →` | TaskNode |

#### TaskGroupGraph (subgraph)
| Node | Ports | Connect To |
|------|-------|------------|
| **TaskGroupStartNode** | `FirstTask →` | TaskNode |
| **TaskNode** | `← In`, `Then →` | TaskNode |

### Quick Port Reference

| Port Name | Meaning |
|-----------|---------|
| `In` | Entry point - connect from previous node |
| `Then` | Success path - where to go on completion |
| `Else` | Failure path - where to go on failure |
| `Tasks` | Connect to TaskNodes (inline tasks) |
| `TaskGroups` | Connect to TaskGroupNodes (inline task groups) |
| `Choices` | Connect to ChoiceNodes (player branching) |
| `Target` | Where the choice leads to |

---

## Building Branching Quests

### Example: The Merchant's Dilemma

This quest demonstrates **player choices** with **different outcomes**.

```
Stage 0: Introduction
    │
    ▼ (Then)
Stage 1: The Choice
    │
    ├─[Choice A: Combat]─────► Stage 10 → Stage 100
    ├─[Choice B: Diplomacy]──► Stage 20 → Stage 100
    └─[Choice C: Lawful]─────► Stage 30 → Stage 100
                                            │
                                            ▼
                                   Stage 100: Resolution (Terminal)
```

### Step-by-Step: Creating Branches

#### 1. Create the Introduction Stage

Add a **StageNode**:
- Stage Index: `0`
- IsTerminal: `false`
- Connect **Then** to the choice stage

#### 2. Create the Choice Stage

Add a **StageNode** for Stage 1:
- Stage Index: `1`
- HasPlayerChoices: `true`
- IsTerminal: `false`

#### 3. Add Choice Nodes

For each choice, add a **ChoiceNode**:

**Choice A - Combat:**
- Choice Text: "Confront the Bandits"
- Target Stage Index: `10`
- Conditions: (none - always available)

**Choice B - Diplomacy:**
- Choice Text: "Negotiate with Bandits"
- Target Stage Index: `20`
- Conditions: (none - always available)

**Choice C - Lawful (Gated):**
- Choice Text: "Report to Guards"
- Target Stage Index: `30`
- Conditions: `ConditionGuardReputation20` (requires reputation)

#### 4. Connect Choices to Stage

From the Choice Stage's **Choices** output port, connect to each ChoiceNode's **In** port.

#### 5. Create Branch Stages

Add StageNodes for each branch:
- Stage 10: Combat resolution (connect Then → Stage 100)
- Stage 20: Diplomacy resolution (connect Then → Stage 100)
- Stage 30: Lawful resolution (connect Then → Stage 100)

#### 6. Create Terminal Stage

Add the final **StageNode**:
- Stage Index: `100`
- IsTerminal: `true` (marks quest completion)

---

## Working with Task Groups

Task groups organize multiple tasks within a stage.

### Task Execution Modes

| Mode | Description | Example |
|------|-------------|---------|
| **Sequential** | Tasks must be completed in order | "First talk to NPC, then go to location" |
| **Parallel** | All tasks active simultaneously, group completes when ALL done | "Collect 3 items AND defeat 5 enemies" |
| **AnyOrder** | All tasks available, can be completed in any order (player choice) | "Complete these 3 objectives in any order" |
| **OptionalXofY** | Complete X out of Y tasks | "Find 2 of 4 clues" |

### Creating a Task Group

#### Option A: Inline Tasks (Simple)

1. In the StageNode, find the **TaskGroups** section
2. Add a new TaskGroup inline
3. Configure execution mode
4. Add Task_SO references

#### Option B: Subgraph (Reusable)

1. Create > HelloDev > Quest System > TaskGroup Graph
2. Configure tasks visually
3. In your QuestGraph, drag the .taskgroup file onto the canvas (creates a native SubgraphNode)
4. The subgraph node automatically displays In/Then ports from the TaskGroup's Graph Variables

---

## Stage Indexing Best Practices

Stage indices determine the progression order. Use the **Skyrim-style** gap convention:

```
Stage 0:   Introduction
Stage 10:  First objective
Stage 20:  Second objective
Stage 30:  Branch A
Stage 40:  Branch B
Stage 100: Resolution
```

### Why Use Gaps?

- **Insertions**: Add Stage 15 between 10 and 20 without renumbering
- **Branches**: Use different ranges (30s, 40s) for parallel paths
- **Clarity**: Higher numbers clearly indicate later stages

### Automatic Indexing

Right-click in the graph and select **"Add Stage (Auto Index)"** to automatically assign the next available index (+10 from the highest).

---

## Conditions and Gating

### ConditionGateNode (Automatic Branching)

Use **ConditionGateNode** when you want the quest to automatically branch based on game state (not player choice).

```
Stage 0: Introduction
    │
    ▼
ConditionGateNode (HasCompletedTutorial?)
    ├── Then → Stage 10 (Advanced Path)
    └── Else → Stage 5 (Tutorial Path)
```

**When to use ConditionGateNode vs ChoiceNode:**
| Scenario | Use |
|----------|-----|
| Player picks from options | ChoiceNode |
| Automatic branch based on reputation | ConditionGateNode |
| Dialog choice with conditions | ChoiceNode + Conditions |
| Silent check for world flag | ConditionGateNode |

**Configuration:**
- **Condition**: Reference to a Condition_SO asset
- **Gate Name**: Optional developer-friendly name
- **Invert Result**: Swaps Then/Else behavior

### Common Condition Types

| Condition | Use Case |
|-----------|----------|
| `ConditionQuestState_SO` | Requires another quest completed |
| `ConditionWorldFlag_SO` | Requires world flag set |
| `ConditionEventID_SO` | Requires specific event triggered |
| `ConditionInt_SO` | Requires numeric value (reputation, level) |

### Adding Conditions to Choices

1. Create a Condition_SO asset
2. In the ChoiceNode, add it to the **Conditions** list
3. If conditions aren't met, the choice appears locked in UI

---

## Event Triggers

### EventTriggerNode (Fire Events)

Use **EventTriggerNode** to fire GameEvents when a point in the quest flow is reached.

```
Stage 10: Defeat the Boss
    │
    ▼
EventTriggerNode (OnBossDefeated)
    │
    ▼
Stage 20: Victory Celebration
```

**Use cases:**
- Trigger cutscenes or dialogue
- Notify achievement systems
- Update world state
- Play sounds or music
- Trigger UI animations

**Configuration:**
- **Event**: Reference to a GameEventVoid_SO asset
- **Trigger Name**: Optional developer-friendly name
- **Delay Frames**: Wait N frames before firing (0 = immediate)

---

## World Flags and Consequences

World flags track persistent game state that affects future quests and dialogue.

### WorldFlagSetNode (Set Flags in Flow)

Use **WorldFlagSetNode** to set world flags at any point in the quest flow.

```
Stage 10: Player Spares the Merchant
    │
    ▼
WorldFlagSetNode (spared_merchant = true, merchant_reputation += 10)
    │
    ▼
Stage 20: Merchant Thanks Player
```

**Configuration:**
- **Node Name**: Optional developer-friendly name
- **Flag Locator**: Reference to WorldFlagLocator_SO
- **Modifications**: List of flag changes (Inspector panel)

**Modification Types:**
| Type | Operations | Example |
|------|------------|---------|
| Boolean | Set true/false | `spared_merchant = true` |
| Integer | Set, Add, Subtract | `reputation += 10` |

### Setting Flags on Choice

Alternatively, set flags directly on ChoiceNodes:

In the ChoiceNode's **WorldFlagsOnSelect**:
- Add `WF_MerchantDilemma_ChoseCombat` = `true`

### Using Flags Later

Other quests can check these flags:
- Use `ConditionGateNode` with `ConditionWorldFlag_SO`
- Future quest uses condition checking `WF_MerchantDilemma_ChoseCombat`
- Different dialogue based on player's past choices

---

## Rewards

### RewardNode (Grant Rewards)

Use **RewardNode** to grant rewards when a point in the quest flow is reached.

```
Stage 100: Quest Complete
    │
    ▼
RewardNode (500 XP, 100 Gold, Sword of Heroes)
    │
    ▼
EventTriggerNode (OnQuestComplete)
```

**Configuration:**
- **Node Name**: Optional developer-friendly name
- **XP Amount**: Experience points to award
- **Currency Amount**: Gold/currency to award
- **Rewards**: List of item rewards (Inspector panel)
- **On Rewards Granted**: Optional event to fire

**Use Cases:**
- Quest completion rewards
- Stage milestone bonuses
- Branch-specific rewards (combat path gives weapon, diplomacy gives gold)

**Integration:**
The RewardNode stores reward data but doesn't implement granting logic.
Your game handles rewards by:
1. Listening to the `OnRewardsGranted` event
2. Reading rewards during graph conversion
3. Processing in your quest runtime system

---

## Creating QuestLines

A QuestLine groups related quests together.

### Step 1: Create QuestLine Graph

1. Create > HelloDev > Quest System > QuestLine Graph
2. Name it `TheMerchantTroubles.questline`

### Step 2: Add Quest References

1. Add a **QuestLineStartNode**
2. Add **QuestRefNode** for each quest
3. Connect in sequence: Start → Quest1 → Quest2 → ...

### Quest Reference Options

| Mode | Description |
|------|-------------|
| **ExistingAsset** | Reference a Quest_SO directly |
| **GraphAsset** | Reference a .quest graph (embedded subgraph) |

### Optional Quests

In the QuestRefNode:
- Set **IsOptional** = `true`
- Quest can be skipped without breaking the questline

---

## Validation and Debugging

### Graph Validation

The editor validates your graph automatically:

| Error | Fix |
|-------|-----|
| "No QuestStartNode found" | Add a QuestStartNode |
| "No terminal stage" | Mark at least one stage as IsTerminal |
| "Duplicate stage index" | Change conflicting indices |
| "Unreachable nodes" | Connect all nodes to the flow |

### Running Validation

- Errors appear in the Graph Editor's console
- Right-click graph → **Validate Graph** for manual check
- Export fails if errors exist (warnings allowed)

### Highlighting Unreachable Nodes

Right-click → **Highlight Unreachable Nodes** to visually identify disconnected content.

---

## Visual Design Tips

### Node Colors

| Node Type | Color | Meaning |
|-----------|-------|---------|
| Start Nodes | Green | Entry points |
| Stage Nodes | Blue-green | Progression |
| Terminal Stages | Red border | Quest endpoints |
| Choice Nodes | Blue | Player decisions |
| Task Nodes | Gray | Work items |
| Subgraph References | Dashed border | External content |

### Using Sticky Notes

Add notes to explain design decisions:
- Right-click → **Add Sticky Note**
- Great for documenting branch reasoning

### Using Placemats

Group related stages visually:
- Right-click → **Add Placemat**
- Name it (e.g., "Act 1: Discovery")
- Helps navigate complex quests

---

## Complete Example: Building "The Merchant's Dilemma"

### 1. Create the Quest Graph

```
File: TheMerchantsDilemma.quest
```

### 2. Add Nodes

| Node | Configuration |
|------|--------------|
| QuestStartNode | (no configuration required) |
| Stage 0 | Introduction - Talk to Merchant |
| Stage 1 | The Choice (HasPlayerChoices = true) |
| ChoiceNode A | "Confront Bandits" → Stage 10 |
| ChoiceNode B | "Negotiate" → Stage 20 |
| ChoiceNode C | "Report to Guards" → Stage 30 (gated) |
| Stage 10 | Combat Path (Then → 100) |
| Stage 20 | Diplomacy Path (Then → 100) |
| Stage 30 | Lawful Path (Then → 100) |
| Stage 100 | Resolution (IsTerminal = true) |

### 3. Configure Connections

```
QuestStartNode.FirstStage → Stage0.In
Stage0.Then → Stage1.In
Stage1.Choices → ChoiceA.In, ChoiceB.In, ChoiceC.In
ChoiceA.Target → Stage10.In
ChoiceB.Target → Stage20.In
ChoiceC.Target → Stage30.In
Stage10.Then → Stage100.In
Stage20.Then → Stage100.In
Stage30.Then → Stage100.In
```

### 4. Add Tasks to Stages

**Stage 0 TaskGroup:**
- Task: `SO_Task_TalkToMerchant` (Bool Task)

**Stage 10 TaskGroup:**
- Task: `SO_Task_DefeatBandits` (Bool Task)

**Stage 20 TaskGroup:**
- Task: `SO_Task_NegotiateWithBandits` (Bool Task)

**Stage 30 TaskGroup:**
- Task: `SO_Task_ReportToGuards` (Bool Task)

**Stage 100 TaskGroup:**
- Task: `SO_Task_ReturnToMerchant` (Bool Task)

### 5. Add World Flags to Choices

| Choice | WorldFlag Set |
|--------|--------------|
| Combat | `WF_MerchantDilemma_ChoseCombat = true` |
| Diplomacy | `WF_MerchantDilemma_ChoseDiplomacy = true` |
| Lawful | `WF_MerchantDilemma_ChoseLawful = true` |

### 6. Add Condition to Lawful Choice

Add to ChoiceC's Conditions:
- `SO_Condition_GuardReputation20` (ConditionInt_SO, >= 20)

### 7. Save and Validate

- **Ctrl+S** to save
- Check for validation errors
- Verify Quest_SO was generated

---

## Workflow Summary

### Designer Checklist

1. [ ] Plan quest structure on paper first
2. [ ] Create .quest file and open in Graph Editor
3. [ ] Add QuestStartNode and configure identity
4. [ ] Add stages with proper indexing (gaps of 10)
5. [ ] Connect stages with transitions
6. [ ] Add ChoiceNodes for branches
7. [ ] Configure conditions on gated content
8. [ ] Add world flags for consequences
9. [ ] Mark terminal stages
10. [ ] Validate graph (no errors)
11. [ ] Test in Play Mode

### Common Mistakes to Avoid

| Mistake | Solution |
|---------|----------|
| No QuestStartNode | Always add one entry point |
| No terminal stage | Mark at least one stage IsTerminal |
| Consecutive indices (0,1,2,3) | Use gaps (0,10,20,30) |
| Orphan nodes | Connect all nodes to the flow |
| Missing task groups | Empty stages need at least one task |
| **Tasks chained in Parallel group** | Connect all tasks directly to TaskGroup.Tasks port |
| Empty quest metadata | Configure Display Name, Description, Quest Type |
| Missing localization entries | Create entries in localization table before referencing |
| Placeholder Task_SO references | Create actual Task_SO assets before adding to graph |

> **Parallel Task Warning:** If you chain TaskNode.Then → TaskNode.In within a Parallel group, tasks execute sequentially even though the group is marked Parallel. See [Task Tutorial - Connection Pattern Warning](tutorial-creating-tasks.md#connection-pattern-warning).

---

## Advanced: Using Subgraphs

For large projects or team collaboration, consider using **Subgraphs**:
- **StageGraph (.stage)**: Reusable stage definitions with Graph Variables for ports
- **TaskGroupGraph (.taskgroup)**: Reusable task collections with Graph Variables for ports
- **Native SubgraphNodeModel**: Drag subgraph files onto canvas; ports auto-generate from Graph Variables

See [Quest Creation Tutorial - Creating Reusable Quests with Subgraphs](tutorial-creating-quests.md#advanced-example-creating-reusable-quests-with-subgraphs) for details.

---

## Related Documentation

**Tutorials:**
- [Task Creation Tutorial](tutorial-creating-tasks.md) - Step-by-step guide to creating tasks
- [Quest Creation Tutorial](tutorial-creating-quests.md) - Step-by-step guide to creating quests

**Reference:**
- [Quest System Overview](overview.md)
- [Tasks Reference](tasks.md) - Technical task documentation
- [Quest Graph Editor Implementation Guide](quest-graph-editor-guide.md) (for programmers)

**Examples:**
- [BasicQuestExample README](../../Assets/com.hellodev.questsystem/BasicQuestExample/README.md)
- [The Merchant's Dilemma README](../../Assets/com.hellodev.questsystem/BasicQuestExample/ScriptableObjects/Quests/The%20Merchant's%20Dilemma/README.md)
