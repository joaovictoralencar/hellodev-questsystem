# The Goblin Conspiracy Quest

The climax of the example quest chain. A main quest that reveals the dark cult behind both the goblin attacks and the bandit raids. Features a timed ritual disruption.

> **Chain Quest:** Requires completing EITHER "The Bandit's Employer" OR "Goblin's Bane"
> **Created:** 2025-12-28

## Quest Overview

| Property | Value |
|----------|-------|
| **Name** | The Goblin Conspiracy |
| **Type** | Main Quest |
| **Recommended Level** | 6 |
| **Rewards** | 2000 Experience, 1500 Gold |
| **Prerequisite** | Complete "The Bandit's Employer" OR "Goblin's Bane" |

### Story Summary

Evidence points to a connection between the bandits and the goblins - both have been manipulated by a dark cult. The player must investigate this connection, meet with a mysterious informant who knows the cult's plans, infiltrate their ritual site at the ancient ruins, stop a dangerous ritual before it completes, and return with evidence to warn the authorities.

---

## Quest Chain Context

```
The Merchant's Stolen Goods (Lvl 3)          Goblin's Bane (Lvl 5)
         │                                         │
         ▼                                         │
  The Bandit's Employer (Lvl 4)                    │
         │                                         │
         └──────────────┬──────────────────────────┘
                        ▼
              The Goblin Conspiracy (Lvl 6)  ←── YOU ARE HERE
```

This quest can be unlocked through TWO different paths:
1. **Merchant Path:** Merchant's Stolen Goods → The Bandit's Employer → This Quest
2. **Goblin Path:** Goblin's Bane → This Quest

This demonstrates the CompositeCondition_SO with OR logic for branching quest chains.

---

## Stage Structure (Proposed)

### Stage 0: Investigation
| Task | Type | Description |
|------|------|-------------|
| Investigate Connection | DiscoveryTask | Find cult symbol and ritual scroll (2/2) |
| Meet the Informant | BoolTask | Meet with mysterious informant |

**Transition:** Automatic on all tasks complete

### Stage 1: Infiltration
| Task | Type | Description |
|------|------|-------------|
| Infiltrate Cult Meeting | LocationTask | Reach the ancient ruins ritual site |

**Transition:** Automatic on task complete

### Stage 2: Confrontation
| Task | Type | Description |
|------|------|-------------|
| Stop the Ritual | TimedTask | Disrupt the ceremony within 120 seconds |

**Transition:** Automatic on task complete (or stage fails if timer expires)

### Stage 3: Resolution
| Task | Type | Description |
|------|------|-------------|
| Return with Evidence | LocationTask | Return to the village with proof |

**Transition:** Quest completes

---

## Task Breakdown

### Task 1: Investigate Connection (DiscoveryTask)

| Property | Value |
|----------|-------|
| **Type** | DiscoveryTask |
| **Discoverable Items** | Cult Symbol, Ritual Scroll |
| **Required Discoveries** | 2 |

**How It Works:**
1. Player searches locations from previous quests for new clues
2. Finding the cult symbol connects the bandits and goblins
3. Finding the ritual scroll reveals the cult's plans
4. Task completes when both clues discovered

**Design Notes:**
- The cult symbol matches the one found on the sealed orders (from The Bandit's Employer)
- The ritual scroll is written in a strange language (sets up the informant meeting)

---

### Task 2: Meet the Informant (BoolTask)

| Property | Value |
|----------|-------|
| **Type** | BoolTask |
| **Completion** | Meet with the mysterious informant |

**Conditions (REQUIRED):**
| Condition | Description |
|-----------|-------------|
| Informant Met | Triggers when player completes informant dialogue |

**How It Works:**
1. A note leads the player to meet someone who knows about the cult
2. The informant is a former cult member seeking redemption
3. Dialogue reveals the location of the ritual site
4. Informant warns about the dangerous ritual being planned

**Narrative Purpose:**
- Provides exposition about the cult's goals
- Creates moral complexity (is the informant trustworthy?)
- Gives the player a reason to hurry (ritual is happening soon)

---

### Task 3: Infiltrate Cult Meeting (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | Ancient Ruins |
| **Completion** | Reach the ritual site |

**How It Works:**
1. Player follows the informant's directions
2. Must navigate through dangerous territory
3. Entering the ancient ruins triggers completion
4. This starts the timed ritual disruption

**Design Notes:**
- The ruins could be guarded by cult members and/or goblins
- Stealth approach possible but not required
- Arrival triggers the timed task immediately

---

### Task 4: Stop the Ritual (TimedTask)

| Property | Value |
|----------|-------|
| **Type** | TimedTask |
| **Time Limit** | 120 seconds (2 minutes) |
| **Fail Quest on Expire** | Configurable |
| **Completion** | Disrupt the ritual before time runs out |

**How It Works:**
1. Task starts immediately when player enters ritual area
2. Timer counts down from 120 seconds
3. Player must defeat cultists / destroy ritual components
4. Completing the objective stops the timer
5. If timer expires, task fails (quest may fail depending on config)

**Design Notes:**
- Creates urgency and tension
- Multiple ways to complete: kill cult leader, destroy altar, etc.
- Consider adding time bonuses for skilled play

**What Happens on Timer Failure:**
- If `failQuestOnExpire = true`: Quest fails, massive consequences
- If `failQuestOnExpire = false`: Only task fails, player can retry

---

### Task 5: Return with Evidence (LocationTask)

| Property | Value |
|----------|-------|
| **Type** | LocationTask |
| **Target Location** | Village |
| **Completion** | Return to the village |

**How It Works:**
1. Player collects evidence from the disrupted ritual
2. Travels back to the village
3. Entering the village triggers completion
4. Quest completes, rewards distributed

**Narrative Purpose:**
- Provides closure to the quest chain
- Sets up potential future content (cult remnants, consequences)
- Rewards feel earned after the tense ritual sequence

---

## Folder Structure

```
The Goblin Conspiracy/
├── README.md                                ← This file
├── SO_Quest_TheGoblinConspiracy.asset       ← Main quest definition
└── Tasks/
    ├── SO_Task_InvestigateConnection.asset  ← Task 1 (DiscoveryTask)
    ├── SO_Task_MeetTheInformant.asset       ← Task 2 (BoolTask)
    ├── SO_Task_InfiltrateCultMeeting.asset  ← Task 3 (LocationTask)
    ├── SO_Task_StopTheRitual.asset          ← Task 4 (TimedTask)
    └── SO_Task_ReturnWithEvidence.asset     ← Task 5 (LocationTask)
```

## Related Assets

### Start Conditions (in `../Conditions/`)

| Asset | Type | Purpose |
|-------|------|---------|
| `SO_Condition_Composite_GoblinConspiracyPrereq` | CompositeCondition_SO | OR logic for multiple paths |

**Composite Condition Structure:**
```
CompositeCondition (ANY/OR mode):
├── ConditionQuestState: "The Bandit's Employer" == Completed
└── ConditionQuestState: "Goblin's Bane" == Completed
```

### ID_SO References (in `../IDs/`)

| Asset | Purpose |
|-------|---------|
| `Locations/SO_ID_Location_AncientRuins` | Task 3 destination |
| `Locations/SO_ID_Location_Village` | Task 5 destination |
| `Discoveries/SO_ID_Discovery_CultSymbol` | Task 1 clue |
| `Discoveries/SO_ID_Discovery_RitualScroll` | Task 1 clue |
| `NPCs/SO_ID_NPC_Informant` | Task 2 contact |

### Events (in `../Events/`)

| Asset | Type | Triggers |
|-------|------|----------|
| `SO_GameEvent_OnFindLocation` | GameEventID_SO | Location tasks |
| `SO_GameEvent_OnItemDiscovered` | GameEventID_SO | Discovery tasks |
| `SO_GameEvent_OnDialogueComplete` | GameEventBool_SO | BoolTask completion |
| `SO_GameEvent_OnRitualDisrupted` | GameEventBool_SO | TimedTask completion |

### Rewards (in `../Rewards/`)

| Asset | Amount |
|-------|--------|
| `ExperienceQuestReward` | 2000 XP |
| `GoldQuestReward` | 1500 Gold |

---

## Unique Features Demonstrated

### 1. OR-Based Quest Prerequisites
This quest shows how to use CompositeCondition_SO with OR logic:
- Player can reach this quest through the merchant storyline OR the goblin storyline
- Both paths are valid and lead to the same conclusion
- Creates player agency and replayability

### 2. TimedTask for Tension
The ritual disruption task demonstrates:
- Creating urgency in gameplay
- Timer-based failure conditions
- High-stakes moments in quest design

### 3. Multi-Path Narrative Convergence
Two separate storylines merge:
- Merchant's Stolen Goods → The Bandit's Employer (bandits path)
- Goblin's Bane (goblins path)
- Both reveal the same cult as the true villain

---

## Design Notes

### Why Two Paths?

1. **Player Freedom:** Different playstyles can experience the story
2. **Replayability:** Second playthrough takes a different route
3. **Pacing:** Main quest (Goblin's Bane) players get faster access
4. **Completionists:** Can do both paths for extra content

### Narrative Themes

- **Hidden Villains:** The cult was manipulating events from behind the scenes
- **Interconnected Threats:** What seemed like separate problems are related
- **Urgency:** The timed ritual creates a "point of no return" feeling
- **Resolution:** The player has made a real difference in the world

---

## Testing Guide

### Happy Path (Full Completion)
1. Complete prerequisite quest (either path)
2. Start this quest
3. **Task 1:** Click "Discover Next Item" 2 times
4. **Task 2:** Complete dialogue trigger
5. **Task 3:** Click "Trigger Location Reached"
6. **Task 4:** Click "Complete Timed Objective" (before timer expires)
7. **Task 5:** Click "Trigger Location Reached"
8. Quest completes, rewards distributed

### Timer Failure Path
1. Progress to Task 4 (Stop the Ritual)
2. Wait for timer to expire OR click "Expire Timer"
3. Task 4 fails
4. Verify quest behavior based on failQuestOnExpire setting

### Multi-Path Verification
1. **Path A:** Complete Merchant → Bandit's Employer
   - Verify this quest unlocks
2. **Path B:** Complete only Goblin's Bane
   - Verify this quest unlocks
3. **Both Paths:** Complete Merchant, Bandit's Employer, AND Goblin's Bane
   - Verify this quest still works correctly

---

## Localization Keys (Suggested)

| Asset | Key | Example Text |
|-------|-----|--------------|
| Quest | quest_goblin_conspiracy_name | "The Goblin Conspiracy" |
| Quest | quest_goblin_conspiracy_desc | "Uncover the dark cult that has been manipulating events in the region." |
| Task 1 | task_investigate_conn_name | "Investigate the Connection" |
| Task 1 | task_investigate_conn_desc | "Search for evidence linking the threats ({current}/{required})" |
| Task 2 | task_meet_informant_name | "Meet the Informant" |
| Task 2 | task_meet_informant_desc | "Meet with someone who knows about the cult" |
| Task 3 | task_infiltrate_cult_name | "Infiltrate the Ritual Site" |
| Task 3 | task_infiltrate_cult_desc | "Find and enter the ancient ruins" |
| Task 4 | task_stop_ritual_name | "Stop the Ritual" |
| Task 4 | task_stop_ritual_desc | "Disrupt the ceremony! Time: {time}" |
| Task 5 | task_return_evidence_name | "Return with Evidence" |
| Task 5 | task_return_evidence_desc | "Bring proof of the cult's activities to the village" |

---

## Quest Chain Summary

| Quest | Level | Type | Prerequisites | Leads To |
|-------|-------|------|---------------|----------|
| Merchant's Stolen Goods | 3 | Secondary | None | The Bandit's Employer |
| Goblin's Bane | 5 | Main | Level 5, In Village | The Goblin Conspiracy |
| The Bandit's Employer | 4 | Secondary | Merchant's Stolen Goods | The Goblin Conspiracy |
| **The Goblin Conspiracy** | **6** | **Main** | **Bandit's Employer OR Goblin's Bane** | **None (Chain End)** |
