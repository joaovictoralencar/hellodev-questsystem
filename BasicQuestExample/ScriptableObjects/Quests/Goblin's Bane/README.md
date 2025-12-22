# Goblin's Bane Quest

A Witcher-style quest demonstrating all task types and the condition system in the HelloDev Quest System.

> **Validation Status:** All GUID references verified correct (2025-12-21)

## Quest Overview

| Property | Value |
|----------|-------|
| **Name** | Goblin's Bane |
| **Type** | Main Quest |
| **Recommended Level** | 5 |
| **Reward** | 1500 Experience Points |

### Story Summary

Goblins have been attacking travelers on the road to the village. The village elder asks the player to investigate the attacks, track down the goblin camp, clear it out, defeat their chief, and return to report their success.

---

## Condition System Overview

### How Conditions Work

The quest system uses three levels of conditions:

| Level | Condition Type | Purpose |
|-------|---------------|---------|
| **Quest** | Start Conditions | Gate when quest becomes available |
| **Quest** | Failure Conditions | Cause entire quest to fail permanently |
| **Quest** | Global Task Failure | Cause ANY active task to fail |
| **Task** | Conditions | Event-driven triggers that complete the task |
| **Task** | Failure Conditions | Cause this specific task to fail |

### Task Types and Completion

| Task Type | Internal Completion? | Conditions Required? |
|-----------|---------------------|---------------------|
| **IntTask** | Yes (counter reaches target) | Optional - can add extra triggers |
| **BoolTask** | No internal logic | **Required** - only way to complete |
| **LocationTask** | Yes (enters location) | Optional - can add extra triggers |
| **TimedTask** | Yes (MarkObjectiveComplete) | Optional - can add extra triggers |
| **DiscoveryTask** | Yes (discovers items) | Optional - can add extra triggers |

---

## Quest Conditions

### Start Conditions
What must be true for this quest to become available?

| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_Int_AtLeastLevel5` | ConditionInt_SO | Player level >= 5 |
| `SO_Condition_Event_ID_PlayerInVillage` | ConditionID_SO | Player must be in the village |

**Design Notes:**
- Quest requires BOTH: player level 5+ AND being in the village
- This ensures quests start in appropriate narrative locations
- Multiple start conditions use AND logic (all must be true)

### Failure Conditions
What causes the entire quest to fail?

| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_Bool_VillageAttacked` | ConditionBool_SO | Goblins attacked the village - player was too slow |

**Design Notes:**
- If the player takes too long dealing with the goblin threat, goblins attack the village
- This creates urgency and consequences for ignoring the quest
- The `OnVillageAttacked` event should be raised by a game timer or story trigger

### Global Task Failure Conditions
What causes ANY active task to fail?

| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_Bool_PlayerDeath` | ConditionBool_SO | Player death fails the current task |

**Design Notes:**
- Adds consequence to dying during quest progression
- Only the current task fails, not the entire quest
- Player can restart the task after respawning

---

## Task Breakdown

### Task 1: Investigate the Attacks (DiscoveryTask)

| Property | Value |
|----------|-------|
| **Type** | DiscoveryTask |
| **Completion** | Discover 3/3 clues |
| **Discoverable Items** | Footprints, BrokenCart, Witness |

**Conditions:** None required - DiscoveryTask completes internally when all items discovered.

**Failure Conditions:** None - investigation is open-ended, player can take their time.

**How It Works:**
1. Player explores the attack site
2. Interacting with clue objects raises `GameEventID_SO` with the item's ID
3. DiscoveryTask.OnItemDiscovered() tracks progress
4. When 3/3 discovered, task auto-completes

---

### Task 2: Track the Goblin Camp (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | `SO_ID_Location_GoblinCampsite` |
| **Completion** | Enter the goblin camp area |

**Conditions:** None required - LocationTask completes when player enters target location.

**Failure Conditions:**
| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_Bool_GoblinScoutAlert` | ConditionBool_SO | Goblin scout spotted you - stealth failed! |

**Design Notes:**
- This is a stealth/tracking segment - if a goblin scout sees you, the task fails
- Encourages careful exploration and using cover
- The `OnGoblinScoutAlert` event is raised when a goblin scout AI detects the player

**How It Works:**
1. Player follows environmental clues (tracks, broken branches)
2. Must avoid goblin scout patrols along the way
3. If spotted, `OnGoblinScoutAlert(true)` is raised → task fails
4. Location trigger raises `GameEventID_SO` with GoblinCampsite ID
5. LocationTask.OnPlayerEnteredLocation() checks for match
6. Task completes when location matches

---

### Task 3: Clear the Camp (IntTask)

| Property | Value |
|----------|-------|
| **Type** | IntTask |
| **Target ID** | "Goblin" |
| **Required Count** | 5 |
| **Completion** | Kill 5 goblins |

**Conditions:** None required - IntTask completes when counter reaches target.

**Failure Conditions:**
| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_Int_GoblinsEscaped` | ConditionInt_SO | 3+ goblins escaped - they'll warn others! |

**Design Notes:**
- If 3 or more goblins escape during combat, the task fails
- Escaped goblins will warn the chief, making the quest harder
- Encourages strategic combat to prevent fleeing enemies
- The `OnGoblinEscaped` event passes cumulative escape count

**How It Works:**
1. Player engages goblins in combat
2. Each goblin death raises event with "Goblin" target ID
3. IntTask.OnIncrement() increases kill counter
4. Fleeing goblins raise `OnGoblinEscaped(count)` event
5. If escape count >= 3, task fails
6. Task completes at 5/5 kills

---

### Task 4: Find the Goblin Chief (BoolTask)

| Property | Value |
|----------|-------|
| **Type** | BoolTask |
| **Completion** | Enter the chief's cave chamber |

**Conditions (REQUIRED):**
| Condition | Description |
|-----------|-------------|
| `SO_Condition_Event_ID_FindGoblinsCampsite` | Triggers when player enters chief's chamber |

**Why Conditions Are Required:**
BoolTask has no internal completion logic - it's a pure event-driven task. The Conditions list is the ONLY way to complete it.

**Failure Conditions:** None - exploration task.

**How It Works:**
1. Player explores the cave system
2. Entering chief's chamber triggers `GameEventID_SO`
3. ConditionID_SO evaluates and marks condition as met
4. BoolTask sees all conditions met → task completes

---

### Task 5: Defeat the Goblin Chief (TimedTask)

| Property | Value |
|----------|-------|
| **Type** | TimedTask |
| **Time Limit** | 120 seconds (2 minutes) |
| **Fail Quest on Expire** | No |
| **Completion** | Defeat the chief before time runs out |

**Conditions:** Optional - could add `SO_Condition_Event_ID_GoblinChiefDefeated`

**Failure Conditions:**
| Condition | Description |
|-----------|-------------|
| Timer Expiration | Built-in: if timer reaches 0, task fails |

**What Happens on Timer Failure:**
- `failQuestOnExpire = false` means only the task fails, not the quest
- Design choice: Player can retry the boss fight
- Alternative: Set `failQuestOnExpire = true` for hardcore mode

**How It Works:**
1. Task starts, timer begins counting down
2. Player fights the Goblin Chief boss
3. Boss death calls `TimedTask.MarkObjectiveComplete()`
4. If timer expires first, task fails (but quest continues)

---

### Task 6: Return to Village (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | `SO_ID_Location_Village` |
| **Completion** | Enter the village |

**Conditions:** None required - LocationTask handles internally.

**Failure Conditions:** None - just go home and celebrate!

**How It Works:**
1. Player travels back to village
2. Village area trigger raises location event
3. LocationTask completes → Quest completes!
4. Rewards distributed (1500 XP)

---

## Folder Structure

```
Goblin's Bane/
├── README.md                              ← This file
├── SO_Quest_GoblinsBane.asset             ← Main quest definition
└── Tasks/
    ├── SO_Task_InvestigateAttacks.asset   ← Task 1 (DiscoveryTask)
    ├── SO_Task_TrackGoblinCamp.asset      ← Task 2 (LocationTask)
    ├── SO_Task_Kill_Goblin.asset          ← Task 3 (IntTask)
    ├── SO_Task_FindGoblinsCampsite.asset  ← Task 4 (BoolTask)
    ├── SO_Task_DefeatGoblinChief.asset    ← Task 5 (TimedTask)
    └── SO_Task_ReturnToVillage.asset      ← Task 6 (LocationTask)
```

## Related Assets

### ID_SO References (in `../IDs/`)

| Asset | Purpose |
|-------|---------|
| `Locations/SO_ID_Location_GoblinCampsite` | Target for Task 2 |
| `Locations/SO_ID_Location_Village` | Target for Task 6 |
| `Discoveries/SO_ID_Discovery_Footprints` | Clue for Task 1 |
| `Discoveries/SO_ID_Discovery_BrokenCart` | Clue for Task 1 |
| `Discoveries/SO_ID_Discovery_Witness` | Clue for Task 1 |
| `Enemies/SO_ID_Enemy_GoblinChief` | Boss for Task 5 condition |

### Conditions (in `../Conditions/`)

| Asset | Type | Used By |
|-------|------|---------|
| `SO_Condition_Event_Int_AtLeastLevel5` | ConditionInt_SO | Quest Start |
| `SO_Condition_Event_ID_PlayerInVillage` | ConditionID_SO | Quest Start |
| `SO_Condition_Event_Bool_VillageAttacked` | ConditionBool_SO | Quest Failure |
| `SO_Condition_Event_Bool_PlayerDeath` | ConditionBool_SO | Global Task Failure |
| `SO_Condition_Event_Bool_GoblinScoutAlert` | ConditionBool_SO | Task 2 Failure |
| `SO_Condition_Event_Int_GoblinsEscaped` | ConditionInt_SO | Task 3 Failure |
| `SO_Condition_Event_ID_FindGoblinsCampsite` | ConditionID_SO | Task 4 |
| `SO_Condition_Event_ID_GoblinChiefDefeated` | ConditionID_SO | Task 5 (optional) |

### Events (in `../Events/`)

| Asset | Type | Triggers |
|-------|------|----------|
| `SO_GameEvent_OnPlayerLevelUp` | GameEventInt_SO | Start condition check |
| `SO_GameEvent_OnFindLocation` | GameEventID_SO | Location tasks, start condition |
| `SO_GameEvent_OnItemDiscovered` | GameEventID_SO | Discovery tasks |
| `SO_GameEvent_OnBossDefeated` | GameEventID_SO | Boss defeat condition |
| `SO_GameEvent_OnVillageAttacked` | GameEventBool_SO | Quest failure trigger |
| `SO_GameEvent_OnPlayerDeath` | GameEventBool_SO | Global task failure trigger |
| `SO_GameEvent_OnGoblinScoutAlert` | GameEventBool_SO | Task 2 stealth failure |
| `SO_GameEvent_OnGoblinEscaped` | GameEventInt_SO | Task 3 escape counter |

---

## Events Required for Full Implementation

To fully implement this quest in a game, you need these events raised by game systems:

| Event | Type | When to Raise |
|-------|------|---------------|
| OnPlayerLevelUp | GameEventInt_SO | Player gains a level (pass new level) |
| OnLocationEntered | GameEventID_SO | Player enters a trigger zone (pass location ID) |
| OnItemDiscovered | GameEventID_SO | Player interacts with discoverable (pass item ID) |
| OnEnemyKilled | GameEventString_SO | Enemy dies (pass enemy type like "Goblin") |
| OnBossDefeated | GameEventID_SO | Boss killed (pass boss ID) |

---

## Setup Checklist

After importing, verify in Unity Inspector:

### Pre-Configured (Already Done)
These are already set up in the asset files:

| Task | Field | Value | Status |
|------|-------|-------|--------|
| Quest | tasks | All 6 in order | ✅ Done |
| Quest | startConditions | AtLeastLevel5, PlayerInVillage | ✅ Done |
| Quest | failureConditions | VillageAttacked | ✅ Done |
| Quest | globalTaskFailureConditions | PlayerDeath | ✅ Done |
| Quest | questType | Main | ✅ Done |
| Quest | rewards | 1500 XP | ✅ Done |
| Task 1 | discoverableItems | Footprints, BrokenCart, Witness | ✅ Done |
| Task 1 | requiredDiscoveries | 3 | ✅ Done |
| Task 2 | targetLocation | GoblinCampsite | ✅ Done |
| Task 2 | failureConditions | GoblinScoutAlert | ✅ Done |
| Task 3 | targetId | "Goblin" | ✅ Done |
| Task 3 | requiredCount | 5 | ✅ Done |
| Task 3 | failureConditions | GoblinsEscaped (>= 3) | ✅ Done |
| Task 4 | conditions | FindGoblinsCampsite | ✅ Done |
| Task 5 | timeLimit | 120 | ✅ Done |
| Task 5 | failQuestOnExpire | false | ✅ Done |
| Task 6 | targetLocation | Village | ✅ Done |

### Optional Setup (In Unity Inspector)

#### Task 5: `SO_Task_DefeatGoblinChief`
If you want event-driven completion instead of code-based:
- [ ] Add condition → `SO_Condition_Event_ID_GoblinChiefDefeated`

All other references are pre-configured and ready to use.

---

## Localization Keys

| Asset | Key | Example Text |
|-------|-----|--------------|
| Quest | quest_goblins_bane_name | "Goblin's Bane" |
| Quest | quest_goblins_bane_desc | "Goblins terrorize the roads. Track them down and end their threat." |
| Task 1 | task_investigate_name | "Investigate the Attacks" |
| Task 1 | task_investigate_desc | "Examine clues left by the goblins ({current}/{required})" |
| Task 2 | task_track_name | "Track the Goblin Camp" |
| Task 2 | task_track_desc | "Follow the trail to find the goblin hideout" |
| Task 3 | task_clear_name | "Clear the Camp" |
| Task 3 | task_clear_desc | "Defeat the goblins ({current}/{required})" |
| Task 4 | task_find_chief_name | "Find the Goblin Chief" |
| Task 4 | task_find_chief_desc | "Locate the goblin chief in the cave" |
| Task 5 | task_defeat_chief_name | "Defeat the Goblin Chief" |
| Task 5 | task_defeat_chief_desc | "Defeat the chief! Time remaining: {time}" |
| Task 6 | task_return_name | "Return to Village" |
| Task 6 | task_return_desc | "Report your victory to the village elder" |

---

## Testing Guide

Use the debug buttons in `UI_QuestDetails` (Odin Inspector):

### Happy Path (Full Completion)
1. Start quest via QuestManager.StartQuest()
2. **Task 1:** Click "Discover Next Item" 3 times
3. **Task 2:** Click "Trigger Location Reached"
4. **Task 3:** Click "Increment Task" 5 times
5. **Task 4:** Click "Complete Current Task" (triggers condition)
6. **Task 5:** Click "Complete Timed Objective"
7. **Task 6:** Click "Trigger Location Reached"
8. ✅ Quest completes, moves to Completed section

### Timer Failure Path
1. Progress to Task 5
2. Click "Expire Timer" instead of completing objective
3. Task 5 fails (but quest continues since failQuestOnExpire = false)
4. Verify quest state and behavior

### Task Failure Tests

**Task 2 - Stealth Failure:**
1. Progress to Task 2 (Track the Goblin Camp)
2. Raise `OnGoblinScoutAlert(true)` event
3. Task 2 should fail (goblin spotted you)

**Task 3 - Escape Failure:**
1. Progress to Task 3 (Clear the Camp)
2. Raise `OnGoblinEscaped(3)` event (3 goblins escaped)
3. Task 3 should fail (too many escapees)

**Global Task Failure - Player Death:**
1. Be on any active task
2. Raise `OnPlayerDeath(true)` event
3. Current task should fail

### Quest Failure Test
1. Start the quest
2. Raise `OnVillageAttacked(true)` event
3. Entire quest should fail (goblins attacked the village)

### Restart Test
1. Complete the quest
2. Click "Restart Quest"
3. Verify all tasks reset to initial state
4. Complete again to verify full cycle
