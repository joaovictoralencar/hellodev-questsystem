# The Merchant's Stolen Goods Quest

A beginner-friendly quest demonstrating **all available task types** in the HelloDev Quest System.

## Quest Overview

| Property | Value |
|----------|-------|
| **Type** | Secondary Quest |
| **Level** | 3 |
| **Rewards** | 750 Gold, 500 Experience |
| **Prerequisite** | None (starting quest) |

### Story
A traveling merchant's goods have been stolen by bandits. The player investigates, tracks the bandits, recovers the goods, and learns the bandit leader's name.

---

## Quest Chain Position

```
The Merchant's Stolen Goods (Lvl 3)  <-- THIS QUEST (starting point)
         |
         v
  The Bandit's Employer (Lvl 4)
         |
         v
   The Goblin Conspiracy (Lvl 6)
```

---

## Key Feature: All Task Types Demo

This quest demonstrates every available task type:

| Task | Type | Mechanic Demonstrated |
|------|------|----------------------|
| Talk to Merchant | **BoolTask** | Dialogue trigger (requires condition) |
| Search Crime Scene | **DiscoveryTask** | Multi-item discovery (2/2) |
| Follow the Trail | **LocationTask** | Exploration/navigation |
| Recover Goods | **IntTask** | Item collection counter (3/3) |
| Interrogate Leader | **StringTask** | Information gathering |
| Return to Merchant | **LocationTask** | Quest completion |

---

## Stage Structure

| Stage | Tasks | Description |
|-------|-------|-------------|
| 0 | BoolTask | Talk to merchant (dialogue) |
| 1 | DiscoveryTask, LocationTask | Search clues, follow trail |
| 2 | IntTask, StringTask | Recover goods (3/3), learn name |
| 3 | LocationTask | Return to merchant |

---

## Folder Structure

```
Merchant's Stolen Goods/
├── README.md
├── SO_Quest_MerchantsStolenGoods.asset
└── Tasks/
    ├── SO_Task_TalkToMerchant.asset         (BoolTask)
    ├── SO_Task_SearchCrimeScene.asset       (DiscoveryTask)
    ├── SO_Task_FollowTheTrail.asset         (LocationTask)
    ├── SO_Task_RecoverGoods.asset           (IntTask)
    ├── SO_Task_InterrogateBanditLeader.asset(StringTask)
    └── SO_Task_ReturnToMerchant.asset       (LocationTask)
```

---

## Design Notes

- **No failure conditions** - Beginner-friendly, forgiving gameplay
- **Comprehensive demo** - Shows all non-timed task types
- **Chain starter** - Completing this unlocks "The Bandit's Employer"

---

## Testing

1. Start quest via QuestManager
2. Trigger dialogue completion for Task 1
3. Discover 2 items for Task 2
4. Trigger location for Task 3
5. Increment counter 3 times for Task 4
6. Enter "Scarface" for Task 5
7. Trigger location for Task 6
8. Verify "The Bandit's Employer" unlocks
