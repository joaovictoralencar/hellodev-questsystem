# The Bandit's Employer Quest

A chain quest demonstrating **ConditionQuestState_SO** for prerequisite-based quest unlocking.

## Quest Overview

| Property | Value |
|----------|-------|
| **Type** | Secondary Quest |
| **Level** | 4 |
| **Rewards** | 1000 Gold, 750 Experience |
| **Prerequisite** | "The Merchant's Stolen Goods" completed |

### Story
After recovering the merchant's stolen goods, the player investigates who hired the bandits, uncovering a deeper conspiracy.

---

## Quest Chain

```
The Merchant's Stolen Goods (Lvl 3)
         |
         v
  The Bandit's Employer (Lvl 4)  <-- THIS QUEST
         |
         v
   The Goblin Conspiracy (Lvl 6)
```

---

## Key Feature: Quest Chaining

This quest demonstrates using `ConditionQuestState_SO` to require a previous quest completion:

```
Start Conditions:
- ConditionQuestState_SO: "Merchant's Stolen Goods" == Completed
```

---

## Stage Structure

| Stage | Tasks | Description |
|-------|-------|-------------|
| 0 | LocationTask, DiscoveryTask | Return to camp, find evidence (2/2) |
| 1 | BoolTask, LocationTask | Interrogate bandit, find contact |
| 2 | BoolTask | Report to captain |

---

## Folder Structure

```
The Bandit's Employer/
├── README.md
├── SO_Quest_TheBanditsEmployer.asset
└── Tasks/
    ├── SO_Task_ReturnToBanditCamp.asset      (LocationTask)
    ├── SO_Task_SearchForEvidence.asset       (DiscoveryTask)
    ├── SO_Task_InterrogateBanditAgain.asset  (BoolTask)
    ├── SO_Task_FindTheContact.asset          (LocationTask)
    └── SO_Task_ReportToCaptain.asset         (BoolTask)
```

---

## Testing

1. Complete "The Merchant's Stolen Goods" first
2. Verify this quest unlocks automatically
3. Complete tasks in order
4. Verify "The Goblin Conspiracy" unlocks after completion
