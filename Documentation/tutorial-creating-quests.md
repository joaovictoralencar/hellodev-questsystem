# Tutorial: Creating Quests from Scratch

*Version 1.0 | For: Game Designers | Prerequisites: [Designer Workflow Guide](quest-graph-designer-workflow.md), [Task Creation Tutorial](tutorial-creating-tasks.md)*

This comprehensive tutorial walks you through creating complete quests using the Quest Graph Editor. We'll build progressively complex quests, from a simple linear quest to a full branching narrative with player choices.

**What you'll learn:**
- How to plan and structure quests
- How to use the Quest Graph Editor
- How to create linear and branching quests
- How to add rewards, events, and world flags
- How to validate and test your quests

**Time required:** 30-45 minutes

---

## Table of Contents

1. [Understanding Quest Structure](#understanding-quest-structure)
2. [Part A: Planning Your Quest](#part-a-planning-your-quest)
3. [Part B: Creating a Simple Linear Quest](#part-b-creating-a-simple-linear-quest)
4. [Part C: Creating a Multi-Stage Quest](#part-c-creating-a-multi-stage-quest)
5. [Part D: Creating a Branching Quest with Player Choices](#part-d-creating-a-branching-quest-with-player-choices)
6. [Part E: Adding Rewards and Events](#part-e-adding-rewards-and-events)
7. [Part F: Adding World Flags for Consequences](#part-f-adding-world-flags-for-consequences)
8. [Part G: Validation and Testing](#part-g-validation-and-testing)
9. [Complete Quest Creation Checklist](#complete-quest-creation-checklist)
10. [Advanced Examples](#advanced-examples)

---

## Understanding Quest Structure

Before creating quests, understand the hierarchy:

```
Quest
│
├── Stage 0: Introduction
│   └── TaskGroup: Talk to NPC
│       └── Task: "Speak with the Merchant"
│
├── Stage 10: Investigation
│   └── TaskGroup: Gather Evidence
│       ├── Task: "Search the crime scene"
│       └── Task: "Interview witnesses"
│
├── Stage 20: Confrontation (Has Player Choices)
│   ├── [Choice A] → Stage 30: Combat Path
│   ├── [Choice B] → Stage 40: Diplomacy Path
│   └── [Choice C] → Stage 50: Stealth Path
│
├── Stage 30/40/50: Resolution Paths
│   └── TaskGroup: Complete objective
│
└── Stage 100: Conclusion (Terminal)
    └── TaskGroup: Return to quest giver
```

### Key Concepts

| Concept | Description |
|---------|-------------|
| **Stage** | A discrete phase of quest progression |
| **Stage Index** | Unique number identifying the stage (use gaps of 10) |
| **Terminal Stage** | Completing this stage ends the quest |
| **Task Group** | Collection of tasks with an execution mode |
| **Player Choice** | Branch point where player picks the path |
| **Transition** | Connection from one stage to another |

### Node Types Quick Reference

| Node | Purpose | Key Ports |
|------|---------|-----------|
| **QuestStartNode** | Entry point (required) | `FirstStage →` |
| **StageNode** | Quest phase | `← In`, `TaskGroups →`, `Then →`, `Else →`, `Choices →` |
| **TaskGroupNode** | Groups tasks | `← In`, `Tasks →`, `Then →`, `Else →` |
| **TaskNode** | Individual task | `← In`, `Then →` |
| **ChoiceNode** | Player decision | `← In`, `Target →` |
| **ConditionGateNode** | Automatic branch | `← In`, `Then →`, `Else →` |
| **RewardNode** | Grant rewards | `← In`, `Then →` |
| **EventTriggerNode** | Fire game event | `← In`, `Then →` |
| **WorldFlagSetNode** | Set world flags | `← In`, `Then →` |

---

## Part A: Planning Your Quest

**Always plan before building.** Sketch your quest structure on paper or in a document first.

### Step A.1: Define the Quest Concept

Answer these questions:

| Question | Example Answer |
|----------|----------------|
| What is the quest about? | Help a merchant recover stolen goods |
| Who gives the quest? | Marcus the Merchant |
| What's the main objective? | Find and retrieve the stolen cargo |
| Are there player choices? | Yes - combat, diplomacy, or stealth approach |
| What are the rewards? | 100 gold, 50 XP, merchant discount |
| What are the consequences? | World flags affect future dialogue |

### Step A.2: Outline the Stages

Write out each stage with its objectives:

```
Quest: The Merchant's Stolen Goods

Stage 0: Talk to Merchant
- Task: Speak with Marcus about the theft

Stage 10: Investigate the Scene
- Task: Search the crime scene for clues
- Task: Follow the trail

Stage 20: Find the Bandits (Choice Point)
- Choice A: "Attack the camp" → Stage 30
- Choice B: "Negotiate return" → Stage 40
- Choice C: "Sneak in at night" → Stage 50

Stage 30: Combat Resolution
- Task: Defeat the bandits
- Task: Recover the goods

Stage 40: Diplomacy Resolution
- Task: Negotiate with bandit leader
- Task: Pay the ransom OR convince them

Stage 50: Stealth Resolution
- Task: Infiltrate the camp unseen
- Task: Steal back the goods

Stage 100: Return to Merchant (Terminal)
- Task: Deliver goods to Marcus
- Reward: 100 gold, 50 XP
```

### Step A.3: Identify Required Assets

Before building the graph, ensure you have:

| Asset Type | Examples Needed |
|------------|-----------------|
| **Task_SO assets** | One for each task (see [Task Tutorial](tutorial-creating-tasks.md)) |
| **ID_SO assets** | NPC IDs, Location IDs, Item IDs |
| **Condition_SO assets** | For gated choices or prerequisites |
| **GameEvent_SO assets** | For triggers and notifications |
| **Reward assets** | QuestRewardType_SO for rewards |
| **Localization entries** | Stage descriptions, choice text |

---

## Part B: Creating a Simple Linear Quest

Let's start with the simplest quest: one stage, one task.

### Step B.1: Create the Quest Graph Asset

1. Navigate to your quest folder: `Assets/YourGame/Quests/`
2. **Right-click** → **Create** → **HelloDev** → **Quest System** → **Quest Graph**
3. Name it `Quest_TalkToMerchant.quest`
4. **Double-click** to open in the Graph Editor

### Step B.2: Add the Quest Start Node

1. **Right-click** on the canvas → **Add Node** → **QuestStartNode**
2. Position it on the left side of the canvas

> **Note:** QuestStartNode has no required configuration. Optionally, you can add **Start Conditions** in the Inspector to gate when the quest can begin.

### Step B.3: Add a Stage Node

1. **Right-click** → **Add Node** → **StageNode**
2. Configure:

| Field | Value | Explanation |
|-------|-------|-------------|
| **Stage Index** | `0` | First and only stage |
| **Stage Name** | `Introduction` | Developer reference |
| **Is Terminal** | `true` | Quest ends when this completes |
| **Journal Entry** | (LocalizedString) | What shows in quest log |

3. **Connect**: Drag from `QuestStartNode.FirstStage` → `StageNode.In`

### Step B.4: Add a Task Group Node

1. **Right-click** → **Add Node** → **TaskGroupNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Group Name** | `Main Objective` |
| **Execution Mode** | `Sequential` |

3. **Connect**: Drag from `StageNode.TaskGroups` → `TaskGroupNode.In`

### Step B.5: Add a Task Node

1. **Right-click** → **Add Node** → **TaskNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Task Asset** | `SO_Task_TalkToMerchant` (drag from Project) |

3. **Connect**: Drag from `TaskGroupNode.Tasks` → `TaskNode.In`

### Step B.6: Verify and Save

Your graph should look like:

```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ QuestStartNode   │     │    StageNode     │     │  TaskGroupNode   │     │    TaskNode      │
│                  │────►│  Index: 0        │────►│ "Main Objective" │────►│ SO_Task_TalkTo.. │
│                  │     │  IsTerminal: ✓   │     │ Mode: Sequential │     │                  │
└──────────────────┘     └──────────────────┘     └──────────────────┘     └──────────────────┘
```

1. Press **Ctrl+S** to save
2. Check console for validation errors (should be none)
3. Verify `Quest_TalkToMerchant` (Quest_SO) was generated

**Congratulations!** You've created your first quest.

---

## Part C: Creating a Multi-Stage Quest

Now let's create a quest with multiple sequential stages.

### Step C.1: Create the Quest Graph

1. **Create** → **HelloDev** → **Quest System** → **Quest Graph**
2. Name it `Quest_MerchantStolenGoods.quest`
3. **Double-click** to open

### Step C.2: Add Quest Start Node

1. Add **QuestStartNode**
2. Position on the left side of the canvas

### Step C.3: Add Stage 0 - Introduction

1. Add **StageNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Stage Index** | `0` |
| **Stage Name** | `TalkToMerchant` |
| **Is Terminal** | `false` |

3. Connect: `QuestStartNode.FirstStage` → `Stage0.In`

4. Add **TaskGroupNode** + **TaskNode** for "Talk to Marcus"

### Step C.4: Add Stage 10 - Investigation

1. Add another **StageNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Stage Index** | `10` |
| **Stage Name** | `Investigation` |
| **Is Terminal** | `false` |

3. Connect: `Stage0.Then` → `Stage10.In`

4. Add **TaskGroupNode** (Mode: `Parallel` for simultaneous tasks)

5. Add two **TaskNodes**:
   - `SO_Task_SearchCrimeScene`
   - `SO_Task_FollowTrail`

6. Connect both to `TaskGroupNode.Tasks`

### Step C.5: Add Stage 100 - Conclusion

1. Add final **StageNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Stage Index** | `100` |
| **Stage Name** | `ReturnToMerchant` |
| **Is Terminal** | `true` |

3. Connect: `Stage10.Then` → `Stage100.In`

4. Add **TaskGroupNode** + **TaskNode** for "Return to Marcus"

### Step C.6: Complete Graph Structure

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ QuestStart  │────►│  Stage 0    │────►│  Stage 10   │────►│  Stage 100  │
│             │     │ Talk to NPC │ Then│ Investigate │ Then│ Return      │
│             │     │             │     │ (Parallel)  │     │ (Terminal)  │
└─────────────┘     └──────┬──────┘     └──────┬──────┘     └──────┬──────┘
                           │                   │                   │
                           ▼                   ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
                    │ TaskGroup   │     │ TaskGroup   │     │ TaskGroup   │
                    │ Sequential  │     │ Parallel    │     │ Sequential  │
                    └──────┬──────┘     └──────┬──────┘     └──────┬──────┘
                           │              ┌────┴────┐              │
                           ▼              ▼         ▼              ▼
                    ┌───────────┐  ┌───────────┐┌───────────┐┌───────────┐
                    │ Task:Talk │  │Task:Search││Task:Follow││Task:Return│
                    └───────────┘  └───────────┘└───────────┘└───────────┘
```

### Step C.7: Save and Validate

1. **Ctrl+S** to save
2. Verify no validation errors
3. Check generated Quest_SO

---

## Part D: Creating a Branching Quest with Player Choices

Now the exciting part: player choices that lead to different paths.

### Step D.1: Create the Quest Graph

1. **Create** → **HelloDev** → **Quest System** → **Quest Graph**
2. Name it `Quest_MerchantDilemma.quest`
3. Open in Graph Editor

### Step D.2: Build the Initial Stages

Create stages 0 and 10 as before (Introduction and Investigation).

### Step D.3: Create the Choice Stage (Stage 20)

This is where the player makes a decision.

1. Add **StageNode**
2. Configure:

| Field | Value | Explanation |
|-------|-------|-------------|
| **Stage Index** | `20` |  |
| **Stage Name** | `TheChoice` |  |
| **Is Terminal** | `false` |  |
| **Has Player Choices** | `true` | **This enables the Choices port!** |

3. Connect: `Stage10.Then` → `Stage20.In`

4. Add a TaskGroup + Task for any pre-choice objective (optional)

### Step D.4: Add Choice Nodes

Now add the player options:

**Choice A - Combat:**
1. Add **ChoiceNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Choice Text** | (LocalizedString) "Attack the bandit camp" |
| **Priority** | `0` |

3. Connect: `Stage20.Choices` → `ChoiceA.In`
4. Connect: `ChoiceA.Target` → `Stage30.In` (target stage)

**Choice B - Diplomacy:**
1. Add **ChoiceNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Choice Text** | (LocalizedString) "Negotiate for the goods" |
| **Priority** | `1` |

3. Connect: `Stage20.Choices` → `ChoiceB.In`
4. Connect: `ChoiceB.Target` → `Stage40.In` (target stage)

**Choice C - Stealth (Gated):**
1. Add **ChoiceNode**
2. Configure:

| Field | Value |
|-------|-------|
| **Choice Text** | (LocalizedString) "Sneak in at night" |
| **Priority** | `2` |
| **Conditions** | Add `SO_Condition_StealthSkill10` (in Inspector) |

3. Connect: `Stage20.Choices` → `ChoiceC.In`
4. Connect: `ChoiceC.Target` → `Stage50.In` (target stage)

> **Note:** Choice C requires a condition - it won't be available unless the player meets the stealth requirement. The target stage is set via wire connection, not a field.

### Step D.5: Create Branch Stages

**Stage 30 - Combat Path:**
1. Add **StageNode**: Index `30`, Name `CombatPath`, Not Terminal
2. Add TaskGroup + Tasks for combat objectives
3. (Connection from `ChoiceA.Target` was made in Step D.4)

**Stage 40 - Diplomacy Path:**
1. Add **StageNode**: Index `40`, Name `DiplomacyPath`, Not Terminal
2. Add TaskGroup + Tasks for diplomacy objectives
3. (Connection from `ChoiceB.Target` was made in Step D.4)

**Stage 50 - Stealth Path:**
1. Add **StageNode**: Index `50`, Name `StealthPath`, Not Terminal
2. Add TaskGroup + Tasks for stealth objectives
3. (Connection from `ChoiceC.Target` was made in Step D.4)

### Step D.6: Merge Back to Conclusion

All paths converge at Stage 100:

1. Add **StageNode**: Index `100`, Name `Conclusion`, **Is Terminal: true**
2. Connect all branch stages:
   - `Stage30.Then` → `Stage100.In`
   - `Stage40.Then` → `Stage100.In`
   - `Stage50.Then` → `Stage100.In`

### Step D.7: Complete Branching Structure

```
                                    ┌──────────────┐
                               ┌───►│  Stage 30    │───┐
                               │    │ Combat Path  │   │
                               │    └──────────────┘   │
┌─────────┐   ┌─────────┐   ┌─────────┐                │   ┌──────────────┐
│ Start   │──►│Stage 0  │──►│Stage 10 │──►┌─────────┐  │   │  Stage 100   │
│         │   │         │   │         │   │Stage 20 │  ├──►│  Conclusion  │
└─────────┘   └─────────┘   └─────────┘   │ CHOICE  │  │   │  (Terminal)  │
                                          └────┬────┘  │   └──────────────┘
                                               │       │
                               ┌───────────────┼───────┤
                               │               │       │
                               ▼               ▼       │
                        ┌──────────────┐┌──────────────┐
                        │  Stage 40    ││  Stage 50    │───┘
                        │ Diplomacy    ││ Stealth      │
                        └──────────────┘└──────────────┘
```

### Step D.8: Save and Validate

1. **Ctrl+S** to save
2. Check for validation errors
3. Verify all paths lead to a terminal stage

---

## Part E: Adding Rewards and Events

### Step E.1: Add a Reward Node

Place rewards before the terminal stage:

1. Add **RewardNode** between Stage 30/40/50 and Stage 100
2. Configure:

| Field | Value |
|-------|-------|
| **Node Name** | `QuestRewards` |
| **Rewards** | Add entries in Inspector |

3. For each reward, add a **RewardInstance**:
   - **Reward Type**: `SO_Reward_Gold` (QuestRewardType_SO)
   - **Amount**: `100`

4. Update connections:
   - `Stage30.Then` → `RewardNode.In`
   - `RewardNode.Then` → `Stage100.In`

> **Tip:** You can have different RewardNodes for different paths (combat path gives weapon, diplomacy gives extra gold).

### Step E.2: Add Path-Specific Rewards

For branch-specific rewards:

```
Stage 30 (Combat) ──► RewardNode (Weapon + 50 XP) ──► Stage 100
Stage 40 (Diplo)  ──► RewardNode (150 Gold)       ──► Stage 100
Stage 50 (Stealth)──► RewardNode (Rare Item + XP) ──► Stage 100
```

Create three separate RewardNodes with different configurations.

### Step E.3: Add Event Triggers

Fire events at key moments:

1. Add **EventTriggerNode** after quest completion
2. Configure:

| Field | Value |
|-------|-------|
| **Event** | `GE_OnQuestCompleted` (GameEventVoid_SO) |
| **Trigger Name** | `NotifyQuestComplete` |
| **Delay Frames** | `0` |

3. Connect: `RewardNode.Then` → `EventTriggerNode.In` → `Stage100.In`

**Common event use cases:**
- Trigger cutscenes: `GE_PlayCutscene_MerchantThanks`
- Update achievements: `GE_OnFirstQuestCompleted`
- Spawn NPCs: `GE_SpawnMerchantGuard`
- Change music: `GE_PlayVictoryMusic`

---

## Part F: Adding World Flags for Consequences

World flags create persistent consequences that affect future quests and dialogue.

### Step F.1: Set Flags on Player Choice

When player chooses combat:

1. Add **WorldFlagSetNode** between Choice and Stage 30
2. Configure:

| Field | Value |
|-------|-------|
| **Node Name** | `SetCombatFlag` |
| **Flag Locator** | `WFL_MerchantQuest` (WorldFlagLocator_SO) |

3. In Inspector, add modifications:
   - `chose_combat` = `true`
   - `merchant_reputation` += `-5` (combat hurts reputation)

4. Connect: `ChoiceA.Target` → `WorldFlagSetNode.In` → `Stage30.In`

### Step F.2: Different Flags Per Path

**Combat Path:**
```
chose_combat = true
merchant_reputation -= 5
bandit_leader_dead = true
```

**Diplomacy Path:**
```
chose_diplomacy = true
merchant_reputation += 10
paid_ransom = true
```

**Stealth Path:**
```
chose_stealth = true
merchant_reputation += 5
bandits_unaware = true
```

### Step F.3: Using Flags in Future Quests

In a sequel quest, use **ConditionGateNode**:

```
┌─────────────┐
│ Stage Start │
└──────┬──────┘
       │
       ▼
┌──────────────────────┐
│ ConditionGateNode    │
│ Condition: bandit_   │
│ leader_dead?         │
├──────────┬───────────┤
│ Then     │ Else      │
└────┬─────┴─────┬─────┘
     │           │
     ▼           ▼
┌─────────┐ ┌─────────┐
│ Stage A │ │ Stage B │
│ Revenge │ │ Normal  │
│ Plot    │ │ Path    │
└─────────┘ └─────────┘
```

---

## Part G: Validation and Testing

### Step G.1: Graph Validation

The editor validates automatically. Common errors:

| Error | Cause | Fix |
|-------|-------|-----|
| "No QuestStartNode found" | Missing entry point | Add QuestStartNode |
| "No terminal stage" | No endpoint | Mark a stage IsTerminal |
| "Duplicate stage index" | Two stages with same index | Change one index |
| "Unreachable nodes" | Disconnected nodes | Connect or delete them |
| "Stage has no tasks" | Empty stage | Add TaskGroup + Task |
| "Choice has no target" | Incomplete choice | Set Target Stage Index |

### Step G.2: Manual Validation Checklist

```
[ ] QuestStartNode exists and is connected
[ ] At least one terminal stage exists
[ ] All stages have unique indices
[ ] All stages have at least one task (or are pass-through)
[ ] All choice nodes have valid target indices
[ ] All branches eventually reach a terminal stage
[ ] No orphan nodes (everything connected)
[ ] Stage indices use gaps (0, 10, 20... not 0, 1, 2)
```

### Step G.3: Testing in Play Mode

1. **Add quest to QuestManager**
```csharp
public class QuestTester : MonoBehaviour
{
    [SerializeField] private Quest_SO testQuest;

    void Start()
    {
        QuestManager.Instance.StartQuest(testQuest);
    }
}
```

2. **Progress through stages manually**
```csharp
// Debug: Complete current stage
quest.CurrentStage.Complete();

// Debug: Trigger a specific task
someGameEvent.Raise(targetId);
```

3. **Verify in Quest UI**
- Check quest appears in log
- Verify tasks display correctly
- Confirm stage transitions work
- Test all choice branches

### Step G.4: Debugging Tips

| Issue | Debug Approach |
|-------|----------------|
| Quest doesn't start | Log QuestManager.StartQuest() |
| Task doesn't complete | Log event raises and condition matches |
| Wrong stage transition | Check Then/Else connections |
| Choice not appearing | Verify HasPlayerChoices flag |
| Choice is locked | Check condition requirements |
| Rewards not granted | Verify RewardNode connections |

---

## Complete Quest Creation Checklist

```
[ ] Part A: Plan the Quest
    [ ] Define concept (who, what, why)
    [ ] Outline all stages on paper
    [ ] Identify required assets (tasks, IDs, conditions)
    [ ] Create all Task_SO assets first

[ ] Part B: Create Quest Graph
    [ ] Create .quest file
    [ ] Add QuestStartNode
    [ ] Add all StageNodes with proper indices (gaps of 10)
    [ ] Mark exactly one path to terminal stage(s)

[ ] Part C: Add Tasks to Stages
    [ ] Add TaskGroupNode for each stage
    [ ] Set appropriate execution mode
    [ ] Add TaskNodes referencing Task_SO assets
    [ ] Connect TaskGroups to Stages

[ ] Part D: Add Branching (if applicable)
    [ ] Enable "Has Player Choices" on choice stages
    [ ] Add ChoiceNodes for each option
    [ ] Configure choice text and target indices
    [ ] Add conditions for gated choices
    [ ] Ensure all branches reach terminal

[ ] Part E: Add Rewards and Events
    [ ] Add RewardNodes before terminal stage
    [ ] Configure reward types and amounts
    [ ] Add EventTriggerNodes for notifications
    [ ] Connect in proper order

[ ] Part F: Add World Flags (if applicable)
    [ ] Add WorldFlagSetNodes after choices
    [ ] Configure flag modifications
    [ ] Plan how flags affect future content

[ ] Part G: Validate and Test
    [ ] Save and check for errors (Ctrl+S)
    [ ] Run manual validation checklist
    [ ] Test all paths in Play mode
    [ ] Verify rewards grant correctly
    [ ] Confirm events fire
```

---

## Advanced Examples

### Example 1: Quest with Prerequisites

A quest that requires another quest completed first.

**Step 1: Create prerequisite condition**
1. Create `SO_Condition_QuestState_MainQuestComplete`
2. Configure to check `Quest_MainStory` is `Completed`

**Step 2: Add to QuestStartNode or first stage**
- In the generated Quest_SO, add the condition to prerequisites
- Or use ConditionGateNode at the start

### Example 2: Timed Stage

A stage that must be completed within a time limit.

**Approach:**
1. Create a `TaskTimed_SO` with time limit
2. Add to the stage's task group
3. Connect stage's **Else** port to a failure stage

```
┌─────────────┐
│  Stage 20   │
│ Timed Escape│
├─────┬───────┤
│Then │ Else  │
└──┬──┴───┬───┘
   │      │
   ▼      ▼
Success  Failure
Stage    Stage
```

### Example 3: Optional Side Objective

A bonus task that's not required to complete the stage.

**Approach:**
1. Use `OptionalXofY` execution mode
2. Set `Required Count` to less than total tasks

```
TaskGroupNode (Mode: OptionalXofY, Required: 2)
├── Task: "Main objective 1" (required)
├── Task: "Main objective 2" (required)
├── Task: "Bonus: Find secret" (optional)
└── Task: "Bonus: No damage" (optional)
```

Player must complete 2 of 4 tasks to proceed.

### Example 4: Repeatable Quest

For daily/weekly quests that can be done multiple times.

**Approach:**
1. Set the Quest_SO's `IsRepeatable` flag
2. Configure reset conditions
3. Use world flags to track completion count

### Example 5: Quest Chain (QuestLine)

Multiple quests in sequence.

**Step 1: Create individual Quest Graphs**
- `Quest_MerchantPart1.quest`
- `Quest_MerchantPart2.quest`
- `Quest_MerchantPart3.quest`

**Step 2: Create QuestLine Graph**
1. **Create** → **HelloDev** → **Quest System** → **QuestLine Graph**
2. Name it `QuestLine_MerchantSaga.questline`
3. Add QuestLineStartNode
4. Add QuestRefNode for each quest
5. Connect in sequence

```
┌───────────────┐     ┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│QuestLineStart │────►│ QuestRefNode  │────►│ QuestRefNode  │────►│ QuestRefNode  │
│               │     │ Part 1        │     │ Part 2        │     │ Part 3        │
└───────────────┘     └───────────────┘     └───────────────┘     └───────────────┘
```

### Example 6: Conditional Quest Availability

Quest only available during certain conditions.

**Approach: ConditionGateNode at start**

```
┌─────────────┐     ┌─────────────────────┐
│ QuestStart  │────►│ ConditionGateNode   │
│             │     │ "IsNightTime?"      │
└─────────────┘     └─────────┬───────────┘
                         Then │ Else
                    ┌─────────┴─────────┐
                    ▼                   ▼
             ┌─────────────┐     ┌─────────────┐
             │ Stage 0     │     │ Stage: Wait │
             │ Night Quest │     │ "Come back  │
             │             │     │  at night"  │
             └─────────────┘     └─────────────┘
```

---

## Complete Example: The Merchant's Dilemma (Using Existing Assets)

This walkthrough recreates **The Merchant's Dilemma** quest using existing ScriptableObject assets from `BasicQuestExample/`. No new assets needed - just reference existing ones.

### Quest Structure

```
┌─────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ QuestStart  │────►│    Stage 0       │────►│    Stage 1       │
│             │     │  Introduction    │     │   THE CHOICE     │
└─────────────┘     │                  │     │                  │
                    │  ┌────────────┐  │     │  ┌────────────┐  │
                    │  │ TaskGroup  │  │     │  │ TaskGroup  │  │
                    │  │ Meet the   │  │     │  │ Make Your  │  │
                    │  │ Merchant   │  │     │  │ Choice     │  │
                    │  │     │      │  │     │  │     │      │  │
                    │  │  ┌──▼───┐  │  │     │  │  ┌──▼───┐  │  │
                    │  │  │ Task │  │  │     │  │  │ Task │  │  │
                    │  │  │ Talk │  │  │     │  │  │Decide│  │  │
                    │  │  └──────┘  │  │     │  │  └──────┘  │  │
                    │  └────────────┘  │     │  └────────────┘  │
                    └──────────────────┘     └────────┬─────────┘
                                                      │
                              ┌────────────────┬──────┴──────┬────────────────┐
                              │                │             │                │
                              ▼                ▼             ▼                │
                       ┌────────────┐  ┌────────────┐  ┌────────────┐        │
                       │  Choice A  │  │  Choice B  │  │  Choice C  │        │
                       │  Combat    │  │ Diplomacy  │  │  Lawful*   │        │
                       └─────┬──────┘  └─────┬──────┘  └─────┬──────┘        │
                             │               │               │               │
                             ▼               ▼               ▼               │
                       ┌──────────┐    ┌──────────┐    ┌──────────┐         │
                       │ Stage 10 │    │ Stage 20 │    │ Stage 30 │         │
                       │ Combat   │    │Diplomacy │    │ Lawful   │         │
                       │ Path     │    │ Path     │    │ Path     │         │
                       └────┬─────┘    └────┬─────┘    └────┬─────┘         │
                            │               │               │               │
                            └───────────────┴───────┬───────┘               │
                                                    │                       │
                                                    ▼                       │
                                           ┌───────────────┐                │
                                           │   Stage 100   │                │
                                           │  Resolution   │                │
                                           │  (Terminal)   │                │
                                           └───────────────┘                │
                                                                            │
                              * Requires Guard Rep >= 20 ───────────────────┘
```

### Asset Paths Reference

All assets are located in `BasicQuestExample/ScriptableObjects/`:

| Asset Type | Path |
|------------|------|
| **Tasks** | `Quests/The Merchant's Dilemma/Tasks/` |
| **Conditions** | `Conditions/Branching/` |
| **World Flags** | `WorldFlags/` |

### Step 1: Create the Quest Graph

1. Navigate to `BasicQuestExample/Graphs/Quests/`
2. **Right-click** → **Create** → **HelloDev** → **Quest System** → **Quest Graph**
3. Name it `Graph_Quest_TheMerchantsDilemma.quest`
4. **Double-click** to open

### Step 2: Configure Quest Metadata

In the **Inspector** panel (with the graph background selected):

| Field | Value |
|-------|-------|
| **Dev Name** | `TheMerchantsDilemma` |
| **Quest Type** | `SO_QuestType_Secondary` |
| **Recommended Level** | `5` |
| **Display Name** | (Set localized string) |
| **Quest Description** | (Set localized string) |

### Step 3: Add QuestStartNode

1. **Right-click** on canvas → **Add Node** → **QuestStartNode**
2. Position on the left side of the canvas
3. (Optional) Add start conditions in Inspector if needed

### Step 4: Add Stage 0 - Introduction

1. Add **StageNode** and configure:
   | Field | Value |
   |-------|-------|
   | **Stage Index** | `0` |
   | **Stage Name** | `Introduction` |
   | **Is Terminal** | `false` |
   | **Journal Entry** | (Set localized string) |

2. **Connect**: `QuestStartNode.FirstStage` → `Stage0.In`

3. Add **TaskGroupNode** and configure:
   | Field | Value |
   |-------|-------|
   | **Group Name** | `Meet the Merchant` |
   | **Execution Mode** | `Sequential` |

4. **Connect**: `Stage0.TaskGroups` → `TaskGroupNode.In`

5. Add **TaskNode** and configure:
   - **Task Asset**: Drag `SO_Task_TalkToMerchant.asset`

6. **Connect**: `TaskGroupNode.Tasks` → `TaskNode.In`

### Step 5: Add Stage 1 - The Choice Point

1. Add **StageNode** and configure:
   | Field | Value |
   |-------|-------|
   | **Stage Index** | `1` |
   | **Stage Name** | `TheChoice` |
   | **Has Player Choices** | `true` *(enables Choices port)* |
   | **Is Terminal** | `false` |
   | **Journal Entry** | (Set localized string) |

2. **Connect**: `Stage0.Then` → `Stage1.In`

3. Add **TaskGroupNode** and configure:
   | Field | Value |
   |-------|-------|
   | **Group Name** | `Make Your Choice` |
   | **Execution Mode** | `Sequential` |

4. **Connect**: `Stage1.TaskGroups` → `TaskGroupNode.In`

5. Add **TaskNode** and configure:
   - **Task Asset**: Drag `SO_Task_DecideBanditApproach.asset`

6. **Connect**: `TaskGroupNode.Tasks` → `TaskNode.In`

### Step 6: Add Player Choices

> **Note**: The `Choices` port on Stage 1 can connect to multiple ChoiceNodes. Each choice leads to a different branch.

**Choice A - Combat:**
1. Add **ChoiceNode**
2. Configure in Inspector:
   | Field | Value |
   |-------|-------|
   | **Choice ID** | `combat_path` |
   | **Priority** | `0` |
   | **Choice Text** | "Attack the bandits" *(LocalizedString)* |
   | **Choice Tooltip** | "Confront the bandits directly" |
   | **World Flags On Select** | Add `SO_WF_MerchantDilemma_ChoseCombat` → `true` |

3. **Connect**: `Stage1.Choices` → `ChoiceA.In`

**Choice B - Diplomacy:**
1. Add **ChoiceNode**
2. Configure in Inspector:
   | Field | Value |
   |-------|-------|
   | **Choice ID** | `diplomacy_path` |
   | **Priority** | `1` |
   | **Choice Text** | "Negotiate for the goods" |
   | **Choice Tooltip** | "Try to reason with the bandits" |
   | **World Flags On Select** | Add `SO_WF_MerchantDilemma_ChoseDiplomacy` → `true` |

3. **Connect**: `Stage1.Choices` → `ChoiceB.In`

**Choice C - Lawful (Gated):**
1. Add **ChoiceNode**
2. Configure in Inspector:
   | Field | Value |
   |-------|-------|
   | **Choice ID** | `lawful_path` |
   | **Priority** | `2` |
   | **Choice Text** | "Report to the guards" |
   | **Choice Tooltip** | "Let the authorities handle it" |
   | **Conditions** | Add `SO_Condition_GuardReputation20.asset` from `Conditions/Branching/` |
   | **World Flags On Select** | Add `SO_WF_MerchantDilemma_ChoseLawful` → `true` |

3. **Connect**: `Stage1.Choices` → `ChoiceC.In`

> **Important**: Choice C will only be available to players who have Guard Reputation >= 20.

### Step 7: Add Branch Stages

Each branch follows the same pattern: StageNode → TaskGroupNode → TaskNode.

**Stage 10 - Combat Path:**
1. Add **StageNode**:
   - **Stage Index**: `10`
   - **Stage Name**: `CombatPath`
   - **Is Terminal**: `false`
2. **Connect**: `ChoiceA.Target` → `Stage10.In`
3. Add **TaskGroupNode**:
   - **Group Name**: `Defeat Bandits`
   - **Execution Mode**: `Sequential`
4. **Connect**: `Stage10.TaskGroups` → `TaskGroupNode.In`
5. Add **TaskNode** with `SO_Task_DefeatBandits.asset`
6. **Connect**: `TaskGroupNode.Tasks` → `TaskNode.In`

**Stage 20 - Diplomacy Path:**
1. Add **StageNode**:
   - **Stage Index**: `20`
   - **Stage Name**: `DiplomacyPath`
   - **Is Terminal**: `false`
2. **Connect**: `ChoiceB.Target` → `Stage20.In`
3. Add **TaskGroupNode**:
   - **Group Name**: `Negotiate`
   - **Execution Mode**: `Sequential`
4. **Connect**: `Stage20.TaskGroups` → `TaskGroupNode.In`
5. Add **TaskNode** with `SO_Task_NegotiateWithBandits.asset`
6. **Connect**: `TaskGroupNode.Tasks` → `TaskNode.In`

**Stage 30 - Lawful Path:**
1. Add **StageNode**:
   - **Stage Index**: `30`
   - **Stage Name**: `LawfulPath`
   - **Is Terminal**: `false`
2. **Connect**: `ChoiceC.Target` → `Stage30.In`
3. Add **TaskGroupNode**:
   - **Group Name**: `Report to Guards`
   - **Execution Mode**: `Sequential`
4. **Connect**: `Stage30.TaskGroups` → `TaskGroupNode.In`
5. Add **TaskNode** with `SO_Task_ReportToGuards.asset`
6. **Connect**: `TaskGroupNode.Tasks` → `TaskNode.In`

### Step 8: Add Terminal Stage

1. Add **StageNode**:
   - **Stage Index**: `100`
   - **Stage Name**: `Resolution`
   - **Is Terminal**: `true` *(quest ends when this stage completes)*
2. Add **TaskGroupNode**:
   - **Group Name**: `Return to Merchant`
   - **Execution Mode**: `Sequential`
3. **Connect**: `Stage100.TaskGroups` → `TaskGroupNode.In`
4. Add **TaskNode** with `SO_Task_ReturnToMerchant.asset`
5. **Connect**: `TaskGroupNode.Tasks` → `TaskNode.In`
6. **Connect all branch stages to terminal**:
   - `Stage10.Then` → `Stage100.In`
   - `Stage20.Then` → `Stage100.In`
   - `Stage30.Then` → `Stage100.In`

### Step 9: Configure Rewards (Optional)

In the **Inspector** panel (graph background selected), add rewards:

| Reward Type | Amount |
|-------------|--------|
| Gold | 500 |
| Experience | 750 |

### Step 10: Save and Validate

1. **Ctrl+S** to save the graph
2. Check the **Console** for validation errors
3. Verify the graph structure matches the diagram above

### Complete Wire Connections Checklist

Use this checklist to verify all connections are made:

| # | From Port | To Port | Status |
|---|-----------|---------|--------|
| 1 | `QuestStart.FirstStage` | `Stage0.In` | [ ] |
| 2 | `Stage0.TaskGroups` | `TaskGroup_Intro.In` | [ ] |
| 3 | `TaskGroup_Intro.Tasks` | `Task_TalkToMerchant.In` | [ ] |
| 4 | `Stage0.Then` | `Stage1.In` | [ ] |
| 5 | `Stage1.TaskGroups` | `TaskGroup_Choice.In` | [ ] |
| 6 | `TaskGroup_Choice.Tasks` | `Task_DecideBanditApproach.In` | [ ] |
| 7 | `Stage1.Choices` | `ChoiceA.In` | [ ] |
| 8 | `Stage1.Choices` | `ChoiceB.In` | [ ] |
| 9 | `Stage1.Choices` | `ChoiceC.In` | [ ] |
| 10 | `ChoiceA.Target` | `Stage10.In` | [ ] |
| 11 | `ChoiceB.Target` | `Stage20.In` | [ ] |
| 12 | `ChoiceC.Target` | `Stage30.In` | [ ] |
| 13 | `Stage10.TaskGroups` | `TaskGroup_Combat.In` | [ ] |
| 14 | `TaskGroup_Combat.Tasks` | `Task_DefeatBandits.In` | [ ] |
| 15 | `Stage20.TaskGroups` | `TaskGroup_Diplomacy.In` | [ ] |
| 16 | `TaskGroup_Diplomacy.Tasks` | `Task_NegotiateWithBandits.In` | [ ] |
| 17 | `Stage30.TaskGroups` | `TaskGroup_Lawful.In` | [ ] |
| 18 | `TaskGroup_Lawful.Tasks` | `Task_ReportToGuards.In` | [ ] |
| 19 | `Stage10.Then` | `Stage100.In` | [ ] |
| 20 | `Stage20.Then` | `Stage100.In` | [ ] |
| 21 | `Stage30.Then` | `Stage100.In` | [ ] |
| 22 | `Stage100.TaskGroups` | `TaskGroup_Resolution.In` | [ ] |
| 23 | `TaskGroup_Resolution.Tasks` | `Task_ReturnToMerchant.In` | [ ] |

### Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| "Stage has no tasks" warning | Missing TaskGroup connection | Connect `Stage.TaskGroups` → `TaskGroupNode.In` |
| Choice not appearing in game | `HasPlayerChoices` not enabled | Check "Has Player Choices" on the Stage node |
| Quest never completes | No terminal stage | Set `IsTerminal = true` on final stage |
| Gated choice always visible | Condition not set | Add condition asset to ChoiceNode's Conditions field |
| World flag not set on choice | WorldFlagsOnSelect empty | Add world flag modification in Inspector |
| Validation error: duplicate stage index | Two stages have same index | Use unique indices (0, 1, 10, 20, 30, 100) |

---

## Advanced Example: Creating Reusable Quests with Subgraphs

Subgraphs let you create reusable quest components that can be shared across multiple quests. This is ideal for teams or large projects. The quest system uses Unity Graph Toolkit's native SubgraphNodeModel system for maximum compatibility.

### Benefits of Subgraphs

- **Reusability**: Define once, use in multiple quests
- **Team Collaboration**: Designers work on separate files
- **Maintainability**: Change subgraph once, update everywhere
- **Native Unity Integration**: Uses Graph Toolkit's SubgraphNodeModel for automatic port generation

### How Native Subgraphs Work

Subgraph ports are generated automatically from **Graph Variables** defined in the subgraph file:
- Variables with `ModifierFlags.Read` create **INPUT** ports on the subgraph node
- Variables with `ModifierFlags.Write` create **OUTPUT** ports on the subgraph node

### Step 1: Create a TaskGroupGraph Subgraph

TaskGroupGraph (.taskgroup) contains reusable task collections.

1. **Right-click** in Project → **Create** → **HelloDev** → **Quest System** → **Graphs** → **TaskGroup Subgraph**
2. Name it `TaskGroup_GatherEvidence.taskgroup`
3. **Double-click** to open

Inside the subgraph:
1. The subgraph has an **In** variable (input port) automatically
2. Add **TaskNode** references to your Task_SO assets
3. Configure in Inspector:
   - **Execution Mode**: `Parallel`
   - **Required Count**: `2`
   - **Group Name**: `Gather Evidence`

### Step 2: Create a StageGraph Subgraph

StageGraph (.stage) contains reusable stage definitions with their own properties.

1. **Right-click** → **Create** → **HelloDev** → **Quest System** → **Graphs** → **Stage Subgraph**
2. Name it `Stage_Investigation.stage`
3. **Double-click** to open

Inside the subgraph:
1. Configure stage properties in Inspector:
   - **Stage Index**: `10`
   - **Stage Name**: `Investigation`
   - **Is Terminal**: `false`
2. The stage has an **In** variable (input port) and a **Then** variable (output port)
3. Add **TaskGroupNode** or reference a TaskGroup subgraph

> **Note:** Each stage file is a complete, self-contained definition. The StageIndex, StageName, and IsTerminal properties are defined directly in the stage file.

### Step 3: Create QuestGraph Using Subgraphs

1. Create new **Quest Graph**: `Quest_MysteryInvestigation.quest`
2. Open and add:
   - **QuestStartNode**
   - **Add Subgraph Node** (via right-click menu or drag the .stage file onto canvas)

The native SubgraphNodeModel automatically displays:
- **In** port - Connect from QuestStartNode.FirstStage or previous stage's Then port
- **Then** port - Connect to the next stage's In port

### Step 4: Reusing Stages Across Quests

Each stage subgraph is a complete definition:

**Stage_Investigation.stage:**
```
Stage Index: 10
Stage Name: "Investigation"
Is Terminal: false
Contains: TaskGroup with search/interview tasks
```

To use the same investigation pattern with different settings, create a new stage file:

**Stage_HauntedHouseSearch.stage:**
```
Stage Index: 20
Stage Name: "Search the Haunted House"
Is Terminal: false
Contains: Same or similar TaskGroup structure
```

This approach keeps each stage self-contained while allowing task group reuse.

### Subgraph Port Reference

| Graph Type | Input Ports | Output Ports |
|------------|-------------|--------------|
| **StageGraph** | `In` (StageFlow) | `Then` (StageFlow), optionally `Else` |
| **TaskGroupGraph** | `In` (TaskGroupFlow) | `Then` (TaskGroupFlow), optionally `Else` |

---

## Related Documentation

- [Designer Workflow Guide](quest-graph-designer-workflow.md) - Quick reference for all features
- [Task Creation Tutorial](tutorial-creating-tasks.md) - Creating task assets
- [Quest System Overview](overview.md) - System architecture
- [Tasks Reference](tasks.md) - Technical task documentation
- [Quest Graph Editor Guide](quest-graph-editor-guide.md) - For programmers
