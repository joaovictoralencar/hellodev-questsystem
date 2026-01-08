# The Goblin Conspiracy Quest

The chain finale demonstrating **CompositeCondition_SO with OR logic** for multi-path quest prerequisites.

## Quest Overview

| Property | Value |
|----------|-------|
| **Type** | Main Quest |
| **Level** | 6 |
| **Rewards** | 2000 Experience, 1500 Gold |
| **Prerequisite** | "The Bandit's Employer" OR "Goblin's Bane" completed |

### Story
Evidence points to a dark cult manipulating both bandits and goblins. The player must infiltrate their ritual site and stop a dangerous ceremony.

---

## Quest Chain (Multi-Path)

```
Merchant's Stolen Goods (Lvl 3)          Goblin's Bane (Lvl 5)
         |                                      |
         v                                      |
  The Bandit's Employer (Lvl 4)                 |
         |                                      |
         +------------------+-------------------+
                            v
              The Goblin Conspiracy (Lvl 6)  <-- THIS QUEST
```

**Two valid paths to reach this quest!**

---

## Key Feature: OR-Based Prerequisites

This quest demonstrates `CompositeCondition_SO` with ANY (OR) mode:

```
Start Conditions (CompositeCondition - ANY mode):
├── ConditionQuestState: "The Bandit's Employer" == Completed
└── ConditionQuestState: "Goblin's Bane" == Completed
```

Player can unlock this quest by completing EITHER prerequisite chain.

---

## Stage Structure

| Stage | Tasks | Description |
|-------|-------|-------------|
| 0 | DiscoveryTask, BoolTask | Find evidence (2/2), meet informant |
| 1 | LocationTask | Infiltrate ritual site |
| 2 | TimedTask (120s) | Stop the ritual before time runs out |
| 3 | LocationTask | Return with evidence |

---

## Unique Features Demonstrated

1. **OR Prerequisites** - Multiple paths to unlock
2. **TimedTask** - Creates urgency with countdown timer
3. **Narrative Convergence** - Two storylines merge into one finale

---

## Folder Structure

```
The Goblin Conspiracy/
├── README.md
├── SO_Quest_TheGoblinConspiracy.asset
└── Tasks/
    ├── SO_Task_InvestigateConnection.asset  (DiscoveryTask)
    ├── SO_Task_MeetTheInformant.asset       (BoolTask)
    ├── SO_Task_InfiltrateCultMeeting.asset  (LocationTask)
    ├── SO_Task_StopTheRitual.asset          (TimedTask)
    └── SO_Task_ReturnWithEvidence.asset     (LocationTask)
```

---

## Testing

### Multi-Path Verification
1. **Path A:** Complete Merchant -> Bandit's Employer -> Verify this unlocks
2. **Path B:** Complete only Goblin's Bane -> Verify this unlocks
3. **Both:** Complete all prerequisites -> Verify still works

### Timer Test
1. Progress to Task 4 (Stop the Ritual)
2. Let timer expire
3. Verify behavior based on `failQuestOnExpire` setting
