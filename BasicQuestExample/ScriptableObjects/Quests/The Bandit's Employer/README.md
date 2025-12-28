# The Bandit's Employer Quest

A chain quest that continues the story from "The Merchant's Stolen Goods." The player investigates who hired the bandits, uncovering a deeper conspiracy.

> **Chain Quest:** Requires completing "The Merchant's Stolen Goods" first
> **Created:** 2025-12-28

## Quest Overview

| Property | Value |
|----------|-------|
| **Name** | The Bandit's Employer |
| **Type** | Secondary Quest |
| **Recommended Level** | 4 |
| **Rewards** | 1000 Gold, 750 Experience |
| **Prerequisite** | Complete "The Merchant's Stolen Goods" |

### Story Summary

After recovering the merchant's stolen goods and learning the bandit leader's name ("Scarface"), the player realizes there's more to the story. The bandits were hired by someone else. Return to the bandit camp, search for evidence of their employer, confront the captured bandit leader with the new evidence, track down their mysterious contact, and report your findings to the guard captain.

---

## Quest Chain Context

```
The Merchant's Stolen Goods (Lvl 3)
         │
         ▼
  The Bandit's Employer (Lvl 4)  ←── YOU ARE HERE
         │
         ▼
   The Goblin Conspiracy (Lvl 6)
         ▲
         │
    Goblin's Bane (Lvl 5) ──────────┘
```

This quest connects to "The Goblin Conspiracy" which can be unlocked by completing EITHER this quest OR "Goblin's Bane."

---

## Stage Structure (Proposed)

### Stage 0: Evidence Gathering
| Task | Type | Description |
|------|------|-------------|
| Return to Bandit Camp | LocationTask | Travel back to the bandit hideout |
| Search for Evidence | DiscoveryTask | Find payment ledger and sealed orders (2/2) |

**Transition:** Automatic on all tasks complete

### Stage 1: Confrontation
| Task | Type | Description |
|------|------|-------------|
| Interrogate Bandit Again | BoolTask | Confront Scarface with new evidence |
| Find the Contact | LocationTask | Track down the mysterious contact at Shadow Hideout |

**Transition:** Automatic on all tasks complete

### Stage 2: Resolution
| Task | Type | Description |
|------|------|-------------|
| Report to Captain | BoolTask | Report findings to the guard captain |

**Transition:** Quest completes

---

## Task Breakdown

### Task 1: Return to Bandit Camp (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | Bandit Camp |
| **Completion** | Enter the bandit camp area |

**How It Works:**
1. Player travels back to the bandit hideout from the previous quest
2. Entering the camp area triggers location event
3. Task completes, enabling the search

---

### Task 2: Search for Evidence (DiscoveryTask)

| Property | Value |
|----------|-------|
| **Type** | DiscoveryTask |
| **Discoverable Items** | Payment Ledger, Sealed Orders |
| **Required Discoveries** | 2 |

**How It Works:**
1. Player searches the camp for clues about the employer
2. Finding the payment ledger reveals regular payments from an unknown source
3. Finding sealed orders shows instructions with an unfamiliar symbol
4. Task completes when both clues discovered

---

### Task 3: Interrogate Bandit Again (BoolTask)

| Property | Value |
|----------|-------|
| **Type** | BoolTask |
| **Completion** | Confront Scarface with evidence |

**Conditions (REQUIRED):**
| Condition | Description |
|-----------|-------------|
| Dialogue Complete | Triggers when player completes confrontation dialogue |

**How It Works:**
1. Player finds the captured bandit leader (Scarface)
2. Presents the evidence found in the camp
3. Scarface reveals the location of their contact
4. Dialogue completion triggers task complete

---

### Task 4: Find the Contact (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | Shadow Hideout |
| **Completion** | Reach the mysterious contact's location |

**How It Works:**
1. Player follows directions given by Scarface
2. Travels to the Shadow Hideout
3. Entering the location triggers completion
4. (Contact may have fled, leaving more clues for the next quest)

---

### Task 5: Report to Captain (BoolTask)

| Property | Value |
|----------|-------|
| **Type** | BoolTask |
| **Completion** | Report findings to guard captain |

**Conditions (REQUIRED):**
| Condition | Description |
|-----------|-------------|
| Report Complete | Triggers when player completes report dialogue |

**How It Works:**
1. Player returns to the village barracks
2. Reports all findings to the guard captain
3. Captain reveals that similar activities have been linked to goblins
4. This sets up "The Goblin Conspiracy" quest

---

## Folder Structure

```
The Bandit's Employer/
├── README.md                              ← This file
├── SO_Quest_TheBanditsEmployer.asset      ← Main quest definition
└── Tasks/
    ├── SO_Task_ReturnToBanditCamp.asset   ← Task 1 (LocationTask)
    ├── SO_Task_SearchForEvidence.asset    ← Task 2 (DiscoveryTask)
    ├── SO_Task_InterrogateBanditAgain.asset ← Task 3 (BoolTask)
    ├── SO_Task_FindTheContact.asset       ← Task 4 (LocationTask)
    └── SO_Task_ReportToCaptain.asset      ← Task 5 (BoolTask)
```

## Related Assets

### Start Conditions (in `../Conditions/`)

| Asset | Type | Purpose |
|-------|------|---------|
| `SO_Condition_QuestState_MerchantsStolenGoodsCompleted` | ConditionQuestState_SO | Requires Merchant's Stolen Goods completion |

### ID_SO References (in `../IDs/`)

| Asset | Purpose |
|-------|---------|
| `Locations/SO_ID_Location_BanditCamp` | Task 1 destination |
| `Locations/SO_ID_Location_ShadowHideout` | Task 4 destination |
| `Discoveries/SO_ID_Discovery_PaymentLedger` | Task 2 clue |
| `Discoveries/SO_ID_Discovery_SealedOrders` | Task 2 clue |

### Events (in `../Events/`)

| Asset | Type | Triggers |
|-------|------|----------|
| `SO_GameEvent_OnFindLocation` | GameEventID_SO | Location tasks |
| `SO_GameEvent_OnItemDiscovered` | GameEventID_SO | Discovery tasks |
| `SO_GameEvent_OnDialogueComplete` | GameEventBool_SO | BoolTask completion |

### Rewards (in `../Rewards/`)

| Asset | Amount |
|-------|--------|
| `GoldQuestReward` | 1000 Gold |
| `ExperienceQuestReward` | 750 XP |

---

## Design Notes

### Quest Chain Mechanics

This quest demonstrates:
1. **ConditionQuestState_SO** - Checking if a previous quest is completed
2. **Chain Quest Pattern** - Building on story established in previous quest
3. **Clue Discovery** - Using DiscoveryTask for investigation mechanics
4. **NPC Interrogation** - Using BoolTask for dialogue-triggered completion
5. **Story Setup** - Final dialogue sets up the next quest in the chain

### Narrative Purpose

- Deepens the mystery from a simple bandit robbery
- Introduces the "Shadow Hideout" as a recurring location
- Connects to "The Goblin Conspiracy" through shared villain (the cult)
- Guard captain becomes an important NPC contact

---

## Testing Guide

### Happy Path (Full Completion)
1. Complete "The Merchant's Stolen Goods" quest first
2. Start this quest - should auto-activate when prereq is met
3. **Task 1:** Click "Trigger Location Reached"
4. **Task 2:** Click "Discover Next Item" 2 times
5. **Task 3:** Complete dialogue trigger
6. **Task 4:** Click "Trigger Location Reached"
7. **Task 5:** Complete report dialogue trigger
8. Quest completes, rewards distributed

### Chain Verification
1. Start a new game (no quests completed)
2. Verify this quest is NOT available (locked by start condition)
3. Complete "The Merchant's Stolen Goods"
4. Verify this quest becomes available
5. Complete this quest
6. Verify "The Goblin Conspiracy" becomes available

---

## Localization Keys (Suggested)

| Asset | Key | Example Text |
|-------|-----|--------------|
| Quest | quest_bandits_employer_name | "The Bandit's Employer" |
| Quest | quest_bandits_employer_desc | "Investigate who hired the bandits to rob the merchant." |
| Task 1 | task_return_camp_name | "Return to Bandit Camp" |
| Task 1 | task_return_camp_desc | "Go back to the bandit hideout to search for evidence" |
| Task 2 | task_search_evidence_name | "Search for Evidence" |
| Task 2 | task_search_evidence_desc | "Find clues about who hired the bandits ({current}/{required})" |
| Task 3 | task_interrogate_again_name | "Interrogate the Bandit" |
| Task 3 | task_interrogate_again_desc | "Confront Scarface with the evidence you found" |
| Task 4 | task_find_contact_name | "Find the Contact" |
| Task 4 | task_find_contact_desc | "Track down the mysterious contact at the Shadow Hideout" |
| Task 5 | task_report_captain_name | "Report to Captain" |
| Task 5 | task_report_captain_desc | "Tell the guard captain what you've discovered" |
