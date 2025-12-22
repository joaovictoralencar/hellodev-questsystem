# The Merchant's Stolen Goods Quest

A secondary quest demonstrating all available task types in the HelloDev Quest System.

> **Validation Status:** All GUID references verified correct (2025-12-21)
> **Update:** Fixed BoolTask conditions - TalkToMerchant now has DialogueComplete condition

## Quest Overview

| Property | Value |
|----------|-------|
| **Name** | The Merchant's Stolen Goods |
| **Type** | Secondary Quest |
| **Recommended Level** | 3 |
| **Rewards** | 750 Gold, 500 Experience |

### Story Summary

A traveling merchant's goods have been stolen by bandits while passing through the forest. The player must investigate the crime scene, track down the bandits, recover the stolen goods, interrogate the bandit leader, and return the goods to the grateful merchant.

---

## Task Types Demonstration

This quest demonstrates ALL available task types:

| Task | Type | Purpose |
|------|------|---------|
| Talk to Merchant | BoolTask | Dialogue trigger |
| Search Crime Scene | DiscoveryTask | Multi-item discovery |
| Follow the Trail | LocationTask | Exploration |
| Recover Goods | IntTask | Item collection |
| Interrogate Leader | StringTask | Information gathering |
| Return to Merchant | LocationTask | Quest completion |

---

## Task Breakdown

### Task 1: Talk to the Merchant (BoolTask)

| Property | Value |
|----------|-------|
| **Type** | BoolTask |
| **Completion** | Speak to the merchant NPC |

**Conditions (REQUIRED):**
| Condition | Description |
|-----------|-------------|
| `SO_Condition_Event_Bool_DialogueComplete` | Triggers when player completes merchant dialogue |

**Why Conditions Are Required:**
BoolTask has no internal completion logic - it's a pure event-driven task. The Conditions list is the ONLY way to complete it.

**How It Works:**
1. Player approaches the merchant in the market
2. Initiating dialogue raises completion event
3. BoolTask completes on event trigger

---

### Task 2: Search the Crime Scene (DiscoveryTask)

| Property | Value |
|----------|-------|
| **Type** | DiscoveryTask |
| **Discoverable Items** | WagonTracks, TornCloth |
| **Required Discoveries** | 2 |

**How It Works:**
1. Player examines the robbery site
2. Finding wagon tracks reveals the bandits' escape route
3. Finding torn cloth with bandit insignia identifies the culprits
4. Task completes when both clues discovered

---

### Task 3: Follow the Trail (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | `SO_ID_Location_BanditCamp` |

**How It Works:**
1. Player follows wagon tracks through the forest
2. Entering bandit camp area triggers completion
3. LocationTask completes when player reaches destination

---

### Task 4: Recover the Stolen Goods (IntTask)

| Property | Value |
|----------|-------|
| **Type** | IntTask |
| **Target ID** | "StolenCrate" |
| **Required Count** | 3 |

**How It Works:**
1. Player searches the bandit camp
2. Interacting with stolen crates adds to counter
3. Task completes when 3/3 crates collected

---

### Task 5: Interrogate the Bandit Leader (StringTask)

| Property | Value |
|----------|-------|
| **Type** | StringTask |
| **Target Value** | "Scarface" |

**How It Works:**
1. Player finds and confronts the bandit leader
2. Through dialogue, player learns the leader's name
3. When "Scarface" is entered/discovered, task completes
4. This information can be used in future quests

**Design Notes:**
- StringTask is unique for information-gathering mechanics
- Can be used for passwords, codes, names, or key phrases
- Useful for investigation and detective-style gameplay

---

### Task 6: Return to the Merchant (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | `SO_ID_Location_Market` |

**How It Works:**
1. Player travels back to the market
2. Entering market area triggers completion
3. Quest completes, rewards distributed

---

## Folder Structure

```
Merchant's Stolen Goods/
├── README.md                                ← This file
├── SO_Quest_MerchantsStolenGoods.asset      ← Main quest definition
└── Tasks/
    ├── SO_Task_TalkToMerchant.asset         ← Task 1 (BoolTask)
    ├── SO_Task_SearchCrimeScene.asset       ← Task 2 (DiscoveryTask)
    ├── SO_Task_FollowTheTrail.asset         ← Task 3 (LocationTask)
    ├── SO_Task_RecoverGoods.asset           ← Task 4 (IntTask)
    ├── SO_Task_InterrogateBanditLeader.asset← Task 5 (StringTask)
    └── SO_Task_ReturnToMerchant.asset       ← Task 6 (LocationTask)
```

## Related Assets

### ID_SO References (in `../IDs/`)

| Asset | Purpose |
|-------|---------|
| `Locations/SO_ID_Location_Market` | Start/End location |
| `Locations/SO_ID_Location_BanditCamp` | Task 3 destination |
| `Discoveries/SO_ID_Discovery_WagonTracks` | Task 2 clue |
| `Discoveries/SO_ID_Discovery_TornCloth` | Task 2 clue |
| `NPCs/SO_ID_NPC_Merchant` | Quest giver |

### Rewards (in `../Rewards/`)

| Asset | Amount |
|-------|--------|
| `GoldQuestReward` | 750 Gold |
| `ExperienceQuestReward` | 500 XP |

### Conditions (in `../Conditions/`)

| Asset | Type | Used By |
|-------|------|---------|
| `SO_Condition_Event_Bool_DialogueComplete` | ConditionBool_SO | Task 1 (completion) |

### Events (in `../Events/`)

| Asset | Type | Triggers |
|-------|------|----------|
| `SO_GameEvent_OnDialogueComplete` | GameEventBool_SO | Task 1 dialogue condition |

---

## Events Required for Implementation

| Event | Type | When to Raise |
|-------|------|---------------|
| `SO_GameEvent_OnDialogueComplete` | GameEventBool_SO | Player finishes merchant dialogue (pass true) |
| OnItemDiscovered | GameEventID_SO | Player examines a clue |
| OnLocationEntered | GameEventID_SO | Player enters a location |
| OnItemCollected | GameEventString_SO | Player picks up a crate |
| OnStringInput | GameEventString_SO | Player enters/discovers a name |

---

## Validation Details

All references have been verified (2025-12-21):

| Reference Type | Count | Status |
|---------------|-------|--------|
| Task GUIDs in Quest | 6 | ✅ Verified |
| ID references in Tasks | 4 | ✅ Verified |
| Condition references | 1 | ✅ Verified |
| Reward type GUIDs | 2 | ✅ Verified |
| Quest type GUID | 1 | ✅ Verified |

**Verified ID References:**
- Task 2: WagonTracks (3c4d5e6f789012345678abcd00010203) ✓
- Task 2: TornCloth (4d5e6f789012345678abcd0001020304) ✓
- Task 3: BanditCamp (2b3c4d5e6f789012345678abcd000102) ✓
- Task 6: Market (1a2b3c4d5e6f789012345678abcd0001) ✓

**Verified Condition References:**
- Task 1: DialogueComplete (f6789012345678abcdef010203040506) ✓

---

## Comparison with Goblin's Bane

| Feature | Goblin's Bane | Merchant's Stolen Goods |
|---------|--------------|------------------------|
| **Type** | Main Quest | Secondary Quest |
| **Level** | 5 | 3 |
| **Task Types** | Discovery, Location, Int, Bool, Timed | Bool, Discovery, Location, Int, String |
| **Unique Feature** | TimedTask with timer | StringTask for info gathering |
| **Failure Conditions** | Yes (multiple) | None (forgiving) |
| **Rewards** | 1500 XP | 750 Gold + 500 XP |

---

## Design Philosophy

This quest is designed to be:

1. **Beginner-Friendly** - No failure conditions, lower level requirement
2. **Comprehensive Demo** - Shows all non-timed task types
3. **Narrative-Driven** - Clear story progression with investigation elements
4. **Reusable Patterns** - Each task demonstrates a common game mechanic

---

## Testing Guide

### Happy Path (Full Completion)
1. Start quest via QuestManager.StartQuest()
2. **Task 1:** Trigger dialogue completion event
3. **Task 2:** Click "Discover Next Item" 2 times
4. **Task 3:** Click "Trigger Location Reached"
5. **Task 4:** Click "Increment Task" 3 times
6. **Task 5:** Enter "Scarface" or trigger string match event
7. **Task 6:** Click "Trigger Location Reached"
8. Quest completes, rewards distributed

---

## Localization Keys (Suggested)

| Asset | Key | Example Text |
|-------|-----|--------------|
| Quest | quest_merchant_name | "The Merchant's Stolen Goods" |
| Quest | quest_merchant_desc | "A merchant needs help recovering stolen goods from bandits." |
| Task 1 | task_talk_merchant_name | "Talk to the Merchant" |
| Task 1 | task_talk_merchant_desc | "Speak with the distraught merchant" |
| Task 2 | task_search_scene_name | "Search the Crime Scene" |
| Task 2 | task_search_scene_desc | "Look for clues ({current}/{required})" |
| Task 3 | task_follow_trail_name | "Follow the Trail" |
| Task 3 | task_follow_trail_desc | "Track the bandits to their hideout" |
| Task 4 | task_recover_goods_name | "Recover the Goods" |
| Task 4 | task_recover_goods_desc | "Collect the stolen crates ({current}/{required})" |
| Task 5 | task_interrogate_name | "Interrogate the Leader" |
| Task 5 | task_interrogate_desc | "Learn the bandit leader's name" |
| Task 6 | task_return_merchant_name | "Return to the Merchant" |
| Task 6 | task_return_merchant_desc | "Bring back the recovered goods" |
