# Stage Configuration Guide

This guide provides the complete configuration for each quest in the BasicQuestExample, including **Events**, **Conditions**, and **Stages** - the three pillars of the quest system.

> **Created:** 2025-12-28
> **Purpose:** Configure multi-stage quest structures with proper event/condition wiring

---

## Table of Contents

1. [Event-Condition Architecture](#event-condition-architecture)
2. [Available Events](#available-events)
3. [Available IDs](#available-ids)
4. [Available Conditions](#available-conditions)
5. [Quest Chain Conditions](#quest-chain-conditions)
6. [Quest 1: Merchant's Stolen Goods](#quest-1-the-merchants-stolen-goods)
7. [Quest 2: Goblin's Bane](#quest-2-goblins-bane)
8. [Quest 3: The Bandit's Employer](#quest-3-the-bandits-employer)
9. [Quest 4: The Goblin Conspiracy](#quest-4-the-goblin-conspiracy)
10. [Testing Guide](#testing-after-configuration)

---

## Event-Condition Architecture

The quest system is built on an **Event-Driven** architecture. Understanding this is critical:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        QUEST SYSTEM SCAFFOLD                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   GAME SYSTEMS                    QUEST SYSTEM                      │
│   ────────────                    ────────────                      │
│                                                                     │
│   ┌─────────────┐    Raise()     ┌─────────────────┐               │
│   │ Combat      │ ──────────────▶│ GameEvent_SO    │               │
│   │ Dialogue    │                │ (Generic)       │               │
│   │ Movement    │                └────────┬────────┘               │
│   │ Discovery   │                         │                        │
│   └─────────────┘                         │ Listened by            │
│                                           ▼                        │
│                                  ┌─────────────────┐               │
│                                  │ Condition_SO    │               │
│                                  │ (Specific)      │               │
│                                  │ + targetValue   │               │
│                                  └────────┬────────┘               │
│                                           │                        │
│                                           │ Wired to               │
│                                           ▼                        │
│                                  ┌─────────────────┐               │
│                                  │ Task_SO         │               │
│                                  │ .conditions[]   │               │
│                                  └────────┬────────┘               │
│                                           │                        │
│                                           │ Part of                │
│                                           ▼                        │
│                                  ┌─────────────────┐               │
│                                  │ Quest_SO        │               │
│                                  │ .stages[]       │               │
│                                  └─────────────────┘               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Principle: Generic Events, Specific Conditions

| Layer | Example | Purpose |
|-------|---------|---------|
| **Event** | `OnMonsterKilled` (GameEventID_SO) | Generic, reusable across all quests |
| **Condition** | `SO_Condition_Event_ID_GoblinKill` | Specific: listens to event, expects Goblin ID |
| **Task** | `SO_Task_Kill_Goblin` | References condition in `.conditions[]` |

This pattern means:
- **One event** serves many conditions (e.g., `OnMonsterKilled` → GoblinKill, OrcKill, SkeletonKill)
- **Conditions hold the expected value** (which monster, which location, which item)
- **Game code only raises events** - no quest-specific logic needed

---

## Available Events

All events are in `BasicQuestExample/ScriptableObjects/Events/`

### Location & Movement Events

| Event | Type | Payload | Purpose |
|-------|------|---------|---------|
| `SO_GameEvent_OnFindLocation` | GameEventID_SO | Location ID | Player enters a location |
| `SO_GameEvent_OnLocationAttacked` | GameEventID_SO | Location ID | Location under attack |

### Combat Events

| Event | Type | Payload | Purpose |
|-------|------|---------|---------|
| `SO_GameEvent_OnMonsterKilled` | GameEventID_SO | Enemy ID | Any monster killed |
| `SO_GameEvent_OnNPCKilled` | GameEventID_SO | NPC ID | Any NPC killed |
| `SO_GameEvent_OnBossDefeated` | GameEventID_SO | Boss ID | Boss defeated |
| `SO_GameEvent_OnEnemyAlert` | GameEventID_SO | Enemy ID | Enemy spots player (stealth) |
| `SO_GameEvent_OnGoblinEscaped` | GameEventInt_SO | Escape count | Goblins escaped during combat |

### Dialogue & NPC Events

| Event | Type | Payload | Purpose |
|-------|------|---------|---------|
| `SO_GameEvent_OnNPCDialogue` | GameEventID_SO | NPC ID | Dialogue completed with NPC |
| `SO_GameEvent_OnInterrogation` | GameEventString_SO | Info string | Information extracted |

### Discovery & Items Events

| Event | Type | Payload | Purpose |
|-------|------|---------|---------|
| `SO_GameEvent_OnItemDiscovered` | GameEventID_SO | Item/Clue ID | Player discovers something |
| `SO_GameEvent_OnItemCollected` | GameEventID_SO | Item ID | Player picks up item |
| `SO_GameEvent_OnItemDestroyed` | GameEventID_SO | Item ID | Item destroyed |
| `SO_GameEvent_OnGoodsDestroyed` | GameEventInt_SO | Count | Goods destroyed (failure trigger) |

### Player State Events

| Event | Type | Payload | Purpose |
|-------|------|---------|---------|
| `SO_GameEvent_OnPlayerLevelUp` | GameEventInt_SO | New level | Player leveled up |
| `SO_GameEvent_OnPlayerDeath` | GameEventBool_SO | true | Player died |

---

## Available IDs

All IDs are in `BasicQuestExample/ScriptableObjects/IDs/`

### Locations (`IDs/Locations/`)

| ID | Dev Name | Used By |
|----|----------|---------|
| `SO_ID_Location_Village` | Village | Goblin's Bane (start, end) |
| `SO_ID_Location_Market` | Market | Merchant's Stolen Goods (start, end) |
| `SO_ID_Location_GoblinCampsite` | GoblinCampsite | Goblin's Bane (tracking) |
| `SO_ID_Location_BanditCamp` | BanditCamp | Merchant's, Bandit's Employer |
| `SO_ID_Location_ShadowHideout` | ShadowHideout | Bandit's Employer |
| `SO_ID_Location_GuardBarracks` | GuardBarracks | Bandit's Employer |
| `SO_ID_Location_RitualSite` | RitualSite | Goblin Conspiracy |
| `SO_ID_Location_Wasteland` | Wasteland | (Available) |

### Enemies (`IDs/Enemies/`)

| ID | Dev Name | Used By |
|----|----------|---------|
| `SO_ID_Enemy_Goblin` | Goblin | Goblin's Bane (kill count) |
| `SO_ID_Enemy_GoblinChief` | GoblinChief | Goblin's Bane (boss) |
| `SO_ID_Enemy_GoblinScout` | GoblinScout | Goblin's Bane (stealth fail) |
| `SO_ID_Enemy_BanditScout` | BanditScout | Bandit's Employer (stealth fail) |
| `SO_ID_Enemy_CultLeader` | CultLeader | Goblin Conspiracy (boss) |

### NPCs (`IDs/NPCs/`)

| ID | Dev Name | Used By |
|----|----------|---------|
| `SO_ID_NPC_Merchant` | Merchant | Merchant's Stolen Goods |
| `SO_ID_NPC_BanditLeader` | BanditLeader | Bandit's Employer (Scarface) |
| `SO_ID_NPC_Captain` | Captain | Bandit's Employer |
| `SO_ID_NPC_Informant` | Informant | Goblin Conspiracy |

### Discoveries (`IDs/Discoveries/`)

| ID | Dev Name | Used By |
|----|----------|---------|
| `SO_ID_Discovery_Footprints` | Footprints | Goblin's Bane |
| `SO_ID_Discovery_BrokenCart` | BrokenCart | Goblin's Bane |
| `SO_ID_Discovery_Witness` | Witness | Goblin's Bane |
| `SO_ID_Discovery_WagonTracks` | WagonTracks | Merchant's Stolen Goods |
| `SO_ID_Discovery_TornCloth` | TornCloth | Merchant's Stolen Goods |
| `SO_ID_Discovery_PaymentLedger` | PaymentLedger | Bandit's Employer |
| `SO_ID_Discovery_SealedOrders` | SealedOrders | Bandit's Employer |
| `SO_ID_Discovery_CultSymbol` | CultSymbol | Goblin Conspiracy |
| `SO_ID_Discovery_RitualScroll` | RitualScroll | Goblin Conspiracy |

### Items (`IDs/Items/`)

| ID | Dev Name | Used By |
|----|----------|---------|
| `SO_ID_Item_StolenCrate` | StolenCrate | Merchant's Stolen Goods |

---

## Available Conditions

All conditions are in `BasicQuestExample/ScriptableObjects/Conditions/`

### Location Conditions (ConditionID_SO → OnFindLocation)

| Condition | Target ID | Purpose |
|-----------|-----------|---------|
| `SO_Condition_Event_ID_PlayerInVillage` | Village | Quest start (Goblin's Bane) |
| `SO_Condition_Event_ID_FindGoblinsCampsite` | GoblinCampsite | Task completion |
| `SO_Condition_Event_ID_BanditCamp` | BanditCamp | Task completion |
| `SO_Condition_Event_ID_Market` | Market | Task completion |
| `SO_Condition_Event_ID_ShadowHideout` | ShadowHideout | Task completion |
| `SO_Condition_Event_ID_RitualSite` | RitualSite | Task completion |

### Combat Conditions

| Condition | Event | Target | Purpose |
|-----------|-------|--------|---------|
| `SO_Condition_Event_ID_GoblinKill` | OnMonsterKilled | Goblin | IntTask increment |
| `SO_Condition_Event_ID_GoblinChiefDefeated` | OnBossDefeated | GoblinChief | Boss task |
| `SO_Condition_Event_ID_CultLeaderDefeated` | OnBossDefeated | CultLeader | Boss task |
| `SO_Condition_Event_Int_GoblinsEscaped` | OnGoblinEscaped | >= 3 | Task failure |

### Dialogue Conditions (ConditionID_SO → OnNPCDialogue)

| Condition | Target ID | Purpose |
|-----------|-----------|---------|
| `SO_Condition_Event_ID_MerchantDialogue` | Merchant | BoolTask completion |
| `SO_Condition_Event_ID_BanditLeaderDialogue` | BanditLeader | BoolTask completion |
| `SO_Condition_Event_ID_CaptainDialogue` | Captain | BoolTask completion |
| `SO_Condition_Event_ID_InformantDialogue` | Informant | BoolTask completion |

### Discovery Conditions (ConditionID_SO → OnItemDiscovered)

| Condition | Target ID | Purpose |
|-----------|-----------|---------|
| `SO_Condition_Event_ID_Footprints` | Footprints | DiscoveryTask |
| `SO_Condition_Event_ID_BrokenCart` | BrokenCart | DiscoveryTask |
| `SO_Condition_Event_ID_Witness` | Witness | DiscoveryTask |
| `SO_Condition_Event_ID_WagonTracks` | WagonTracks | DiscoveryTask |
| `SO_Condition_Event_ID_TornCloth` | TornCloth | DiscoveryTask |
| `SO_Condition_Event_ID_PaymentLedger` | PaymentLedger | DiscoveryTask |
| `SO_Condition_Event_ID_SealedOrders` | SealedOrders | DiscoveryTask |
| `SO_Condition_Event_ID_CultSymbol` | CultSymbol | DiscoveryTask |
| `SO_Condition_Event_ID_RitualScroll` | RitualScroll | DiscoveryTask |

### Item Conditions

| Condition | Event | Target | Purpose |
|-----------|-------|--------|---------|
| `SO_Condition_Event_ID_StolenCrate` | OnItemCollected | StolenCrate | IntTask increment |

### String Conditions (ConditionString_SO → OnInterrogation)

| Condition | Target Value | Purpose |
|-----------|--------------|---------|
| `SO_Condition_Event_String_Scarface` | "Scarface" | StringTask completion |

### Failure Conditions

| Condition | Event | Comparison | Purpose |
|-----------|-------|------------|---------|
| `SO_Condition_Event_Bool_PlayerDeath` | OnPlayerDeath | == true | Global task failure |
| `SO_Condition_Event_ID_VillageAttacked` | OnLocationAttacked | Village | Quest failure |
| `SO_Condition_Event_ID_MerchantKilled` | OnNPCKilled | Merchant | Quest failure |
| `SO_Condition_Event_ID_GoblinScoutAlert` | OnEnemyAlert | GoblinScout | Task failure (stealth) |
| `SO_Condition_Event_ID_BanditScoutSpotted` | OnEnemyAlert | BanditScout | Task failure (stealth) |
| `SO_Condition_Event_Int_GoblinsEscaped` | OnGoblinEscaped | >= 3 | Task failure |
| `SO_Condition_Event_Int_GoodsDestroyed` | OnGoodsDestroyed | >= 2 | Task failure |

### Level Conditions

| Condition | Event | Comparison | Purpose |
|-----------|-------|------------|---------|
| `SO_Condition_Event_Int_AtLeastLevel5` | OnPlayerLevelUp | >= 5 | Quest start |

---

## Quest Chain Conditions

Located in `Conditions/QuestChains/`

| Condition | Type | Target Quest | State | Purpose |
|-----------|------|--------------|-------|---------|
| `SO_Condition_QuestState_MerchantsGoodsCompleted` | ConditionQuestState_SO | Merchant's Stolen Goods | Completed | Unlocks Bandit's Employer |
| `SO_Condition_QuestState_GoblinsBaneCompleted` | ConditionQuestState_SO | Goblin's Bane | Completed | Unlocks Goblin Conspiracy |
| `SO_Condition_QuestState_BanditsEmployerCompleted` | ConditionQuestState_SO | Bandit's Employer | Completed | Unlocks Goblin Conspiracy |
| `SO_Condition_Composite_EitherPathCompleted` | CompositeCondition_SO | (see below) | OR | Goblin Conspiracy start |

### Composite Condition: Either Path Completed

```
SO_Condition_Composite_EitherPathCompleted
├── Mode: ANY (OR logic)
├── Conditions:
│   ├── SO_Condition_QuestState_BanditsEmployerCompleted
│   └── SO_Condition_QuestState_GoblinsBaneCompleted
└── Result: TRUE if EITHER quest is completed
```

---

## Quest 1: The Merchant's Stolen Goods

### Event/Condition Wiring

```
┌─────────────────────────────────────────────────────────────────────┐
│                    MERCHANT'S STOLEN GOODS                          │
├─────────────────────────────────────────────────────────────────────┤
│ QUEST START CONDITIONS: None (available immediately)               │
│ QUEST FAILURE CONDITIONS: SO_Condition_Event_ID_MerchantKilled      │
│ GLOBAL TASK FAILURE: None                                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ STAGE 0: Introduction                                               │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Talk to Merchant (BoolTask)                               │ │
│ │ ├── Event: OnNPCDialogue                                        │ │
│ │ ├── Condition: SO_Condition_Event_ID_MerchantDialogue           │ │
│ │ └── Target: SO_ID_NPC_Merchant                                  │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 1: Investigation (Parallel)                                   │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Search Crime Scene (DiscoveryTask - 2 items)              │ │
│ │ ├── Event: OnItemDiscovered                                     │ │
│ │ ├── Condition 1: SO_Condition_Event_ID_WagonTracks              │ │
│ │ │   └── Target: SO_ID_Discovery_WagonTracks                     │ │
│ │ └── Condition 2: SO_Condition_Event_ID_TornCloth                │ │
│ │     └── Target: SO_ID_Discovery_TornCloth                       │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Follow the Trail (LocationTask)                           │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_BanditCamp                 │ │
│ │ └── Target: SO_ID_Location_BanditCamp                           │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 2: Recovery (Parallel)                                        │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Recover Goods (IntTask - 3 crates)                        │ │
│ │ ├── Event: OnItemCollected                                      │ │
│ │ ├── Condition: SO_Condition_Event_ID_StolenCrate                │ │
│ │ ├── Target: SO_ID_Item_StolenCrate                              │ │
│ │ └── Failure: SO_Condition_Event_Int_GoodsDestroyed (>= 2)       │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Interrogate Leader (StringTask)                           │ │
│ │ ├── Event: OnInterrogation                                      │ │
│ │ ├── Condition: SO_Condition_Event_String_Scarface               │ │
│ │ └── Target Value: "Scarface"                                    │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 3: Resolution                                                 │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Return to Merchant (LocationTask)                         │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_Market                     │ │
│ │ └── Target: SO_ID_Location_Market                               │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Stage Configuration

| Stage | Name | Tasks | Mode | Transition |
|-------|------|-------|------|------------|
| 0 | Introduction | Talk to Merchant | Sequential | OnGroupsComplete |
| 1 | Investigation | Search Crime Scene, Follow Trail | **Parallel** | OnGroupsComplete |
| 2 | Recovery | Recover Goods, Interrogate Leader | **Parallel** | OnGroupsComplete |
| 3 | Resolution | Return to Merchant | Sequential | OnGroupsComplete |

---

## Quest 2: Goblin's Bane

### Event/Condition Wiring

```
┌─────────────────────────────────────────────────────────────────────┐
│                         GOBLIN'S BANE                               │
├─────────────────────────────────────────────────────────────────────┤
│ QUEST START CONDITIONS:                                             │
│   - SO_Condition_Event_Int_AtLeastLevel5 (OnPlayerLevelUp >= 5)     │
│   - SO_Condition_Event_ID_PlayerInVillage (OnFindLocation)          │
│ QUEST FAILURE CONDITIONS:                                           │
│   - SO_Condition_Event_ID_VillageAttacked (OnLocationAttacked)      │
│ GLOBAL TASK FAILURE:                                                │
│   - SO_Condition_Event_Bool_PlayerDeath (OnPlayerDeath)             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ STAGE 0: Investigation                                              │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Investigate Attacks (DiscoveryTask - 3 items)             │ │
│ │ ├── Event: OnItemDiscovered                                     │ │
│ │ ├── Condition 1: SO_Condition_Event_ID_Footprints               │ │
│ │ ├── Condition 2: SO_Condition_Event_ID_BrokenCart               │ │
│ │ └── Condition 3: SO_Condition_Event_ID_Witness                  │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 1: Tracking & Combat (Parallel)                               │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Track Goblin Camp (LocationTask)                          │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_FindGoblinsCampsite        │ │
│ │ ├── Target: SO_ID_Location_GoblinCampsite                       │ │
│ │ └── Failure: SO_Condition_Event_ID_GoblinScoutAlert             │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Clear the Camp (IntTask - 5 kills)                        │ │
│ │ ├── Event: OnMonsterKilled                                      │ │
│ │ ├── Condition: SO_Condition_Event_ID_GoblinKill                 │ │
│ │ ├── Target: SO_ID_Enemy_Goblin                                  │ │
│ │ └── Failure: SO_Condition_Event_Int_GoblinsEscaped (>= 3)       │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 2: Boss Confrontation (Sequential)                            │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Find Goblin Chief (BoolTask)                              │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_FindGoblinsCampsite        │ │
│ │ └── Target: (Chief's chamber area)                              │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Defeat Goblin Chief (TimedTask - 120s)                    │ │
│ │ ├── Event: OnBossDefeated                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_GoblinChiefDefeated        │ │
│ │ └── Target: SO_ID_Enemy_GoblinChief                             │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 3: Resolution                                                 │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Return to Village (LocationTask)                          │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_PlayerInVillage            │ │
│ │ └── Target: SO_ID_Location_Village                              │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Stage Configuration

| Stage | Name | Tasks | Mode | Transition |
|-------|------|-------|------|------------|
| 0 | Investigation | Investigate Attacks | Sequential | OnGroupsComplete |
| 1 | Tracking & Combat | Track Camp, Clear Camp | **Parallel** | OnGroupsComplete |
| 2 | Boss Confrontation | Find Chief, Defeat Chief | Sequential | OnGroupsComplete |
| 3 | Resolution | Return to Village | Sequential | OnGroupsComplete |

---

## Quest 3: The Bandit's Employer

### Event/Condition Wiring

```
┌─────────────────────────────────────────────────────────────────────┐
│                      THE BANDIT'S EMPLOYER                          │
├─────────────────────────────────────────────────────────────────────┤
│ QUEST START CONDITIONS:                                             │
│   - SO_Condition_QuestState_MerchantsGoodsCompleted                 │
│ QUEST FAILURE CONDITIONS: None                                      │
│ GLOBAL TASK FAILURE: None                                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ STAGE 0: Evidence Gathering (Parallel)                              │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Return to Bandit Camp (LocationTask)                      │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_BanditCamp                 │ │
│ │ └── Target: SO_ID_Location_BanditCamp                           │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Search for Evidence (DiscoveryTask - 2 items)             │ │
│ │ ├── Event: OnItemDiscovered                                     │ │
│ │ ├── Condition 1: SO_Condition_Event_ID_PaymentLedger            │ │
│ │ │   └── Target: SO_ID_Discovery_PaymentLedger                   │ │
│ │ └── Condition 2: SO_Condition_Event_ID_SealedOrders             │ │
│ │     └── Target: SO_ID_Discovery_SealedOrders                    │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 1: Confrontation (Sequential)                                 │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Interrogate Bandit Again (BoolTask)                       │ │
│ │ ├── Event: OnNPCDialogue                                        │ │
│ │ ├── Condition: SO_Condition_Event_ID_BanditLeaderDialogue       │ │
│ │ └── Target: SO_ID_NPC_BanditLeader                              │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Find the Contact (LocationTask)                           │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_ShadowHideout              │ │
│ │ ├── Target: SO_ID_Location_ShadowHideout                        │ │
│ │ └── Failure: SO_Condition_Event_ID_BanditScoutSpotted           │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 2: Resolution                                                 │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Report to Captain (BoolTask)                              │ │
│ │ ├── Event: OnNPCDialogue                                        │ │
│ │ ├── Condition: SO_Condition_Event_ID_CaptainDialogue            │ │
│ │ └── Target: SO_ID_NPC_Captain                                   │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Stage Configuration

| Stage | Name | Tasks | Mode | Transition |
|-------|------|-------|------|------------|
| 0 | Evidence Gathering | Return to Camp, Search Evidence | **Parallel** | OnGroupsComplete |
| 1 | Confrontation | Interrogate, Find Contact | Sequential | OnGroupsComplete |
| 2 | Resolution | Report to Captain | Sequential | OnGroupsComplete |

---

## Quest 4: The Goblin Conspiracy

### Event/Condition Wiring

```
┌─────────────────────────────────────────────────────────────────────┐
│                      THE GOBLIN CONSPIRACY                          │
├─────────────────────────────────────────────────────────────────────┤
│ QUEST START CONDITIONS:                                             │
│   - SO_Condition_Composite_EitherPathCompleted (OR logic)           │
│     ├── SO_Condition_QuestState_BanditsEmployerCompleted            │
│     └── SO_Condition_QuestState_GoblinsBaneCompleted                │
│ QUEST FAILURE CONDITIONS: None                                      │
│ GLOBAL TASK FAILURE: SO_Condition_Event_Bool_PlayerDeath            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ STAGE 0: Investigation (Parallel)                                   │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Investigate Connection (DiscoveryTask - 2 items)          │ │
│ │ ├── Event: OnItemDiscovered                                     │ │
│ │ ├── Condition 1: SO_Condition_Event_ID_CultSymbol               │ │
│ │ │   └── Target: SO_ID_Discovery_CultSymbol                      │ │
│ │ └── Condition 2: SO_Condition_Event_ID_RitualScroll             │ │
│ │     └── Target: SO_ID_Discovery_RitualScroll                    │ │
│ ├─────────────────────────────────────────────────────────────────┤ │
│ │ Task: Meet the Informant (BoolTask)                             │ │
│ │ ├── Event: OnNPCDialogue                                        │ │
│ │ ├── Condition: SO_Condition_Event_ID_InformantDialogue          │ │
│ │ └── Target: SO_ID_NPC_Informant                                 │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 1: Infiltration                                               │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Infiltrate Cult Meeting (LocationTask)                    │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_RitualSite                 │ │
│ │ └── Target: SO_ID_Location_RitualSite                           │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 2: Action (Timed!)                                            │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Stop the Ritual (TimedTask - 120s)                        │ │
│ │ ├── Event: OnBossDefeated                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_CultLeaderDefeated         │ │
│ │ ├── Target: SO_ID_Enemy_CultLeader                              │ │
│ │ └── Timer: 120 seconds (failQuestOnExpire = configurable)       │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│ STAGE 3: Resolution                                                 │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Task: Return with Evidence (LocationTask)                       │ │
│ │ ├── Event: OnFindLocation                                       │ │
│ │ ├── Condition: SO_Condition_Event_ID_PlayerInVillage            │ │
│ │ └── Target: SO_ID_Location_Village                              │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Stage Configuration

| Stage | Name | Tasks | Mode | Transition |
|-------|------|-------|------|------------|
| 0 | Investigation | Investigate Connection, Meet Informant | **Parallel** | OnGroupsComplete |
| 1 | Infiltration | Infiltrate Cult Meeting | Sequential | OnGroupsComplete |
| 2 | Action | Stop the Ritual (TIMED) | Sequential | OnGroupsComplete |
| 3 | Resolution | Return with Evidence | Sequential | OnGroupsComplete |

---

## Game Integration: Raising Events

For game developers integrating with the quest system, here's when to raise each event:

```csharp
// LOCATION EVENTS
// When player enters a trigger zone:
OnFindLocation.Raise(locationID);

// When a location is attacked:
OnLocationAttacked.Raise(locationID);

// COMBAT EVENTS
// When any monster dies:
OnMonsterKilled.Raise(monsterTypeID);

// When boss is defeated:
OnBossDefeated.Raise(bossID);

// When enemy spots player (stealth):
OnEnemyAlert.Raise(enemyID);

// When goblins escape during combat:
OnGoblinEscaped.Raise(totalEscapeCount);

// DIALOGUE EVENTS
// When dialogue with NPC completes:
OnNPCDialogue.Raise(npcID);

// When player extracts information:
OnInterrogation.Raise("information string");

// DISCOVERY EVENTS
// When player examines/discovers something:
OnItemDiscovered.Raise(itemID);

// When player picks up an item:
OnItemCollected.Raise(itemID);

// PLAYER STATE EVENTS
// When player levels up:
OnPlayerLevelUp.Raise(newLevel);

// When player dies:
OnPlayerDeath.Raise(true);
```

---

## Testing After Configuration

### Event Testing Checklist

For each task, verify:
1. [ ] Event is raised by game system
2. [ ] Condition receives event and evaluates correctly
3. [ ] Task updates state when condition is met
4. [ ] Stage transitions when all tasks complete

### Debug Commands (in UI_QuestDetails)

- **Discover Next Item** - Simulates OnItemDiscovered
- **Trigger Location Reached** - Simulates OnFindLocation
- **Increment Task** - Simulates OnMonsterKilled / OnItemCollected
- **Complete Current Task** - Forces task completion
- **Expire Timer** - Tests TimedTask failure

---

## Summary: Event → Condition → Task Flow

| Event Type | Condition Type | Task Types Using It |
|------------|----------------|---------------------|
| GameEventID_SO | ConditionID_SO | LocationTask, BoolTask, DiscoveryTask |
| GameEventInt_SO | ConditionInt_SO | IntTask, Start/Failure conditions |
| GameEventBool_SO | ConditionBool_SO | BoolTask, Failure conditions |
| GameEventString_SO | ConditionString_SO | StringTask |

### Quick Reference: Condition → Event Mapping

| Condition Pattern | Listens To | Expects |
|-------------------|------------|---------|
| `*_ID_Location*` | OnFindLocation | Location ID |
| `*_ID_*Kill` | OnMonsterKilled | Enemy ID |
| `*_ID_*Defeated` | OnBossDefeated | Boss ID |
| `*_ID_*Dialogue` | OnNPCDialogue | NPC ID |
| `*_ID_Discovery*` | OnItemDiscovered | Discovery ID |
| `*_Int_*` | Various Int events | Numeric threshold |
| `*_Bool_*` | Various Bool events | true/false |
| `*_String_*` | OnInterrogation | String match |
