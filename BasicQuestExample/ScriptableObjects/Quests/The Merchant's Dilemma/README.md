# The Merchant's Dilemma Quest

A **branching quest example** demonstrating player choices with world state consequences in the HelloDev Quest System.

> **Validation Status:** All GUID references verified correct (2025-12-28)
> **Features Demonstrated:** Stage-based progression, player choices, world flags, gated choices

## Quest Overview

| Property | Value |
|----------|-------|
| **Name** | The Merchant's Dilemma |
| **Type** | Secondary Quest |
| **Recommended Level** | 5 |
| **Rewards** | 500 Gold, 750 Experience |
| **QuestLine** | The Merchant Troubles |

### Story Summary

A merchant has been robbed by bandits. The player must investigate and then choose how to resolve the situation:
- **Combat Path** - Confront the bandits directly and recover the goods by force
- **Diplomacy Path** - Negotiate with the bandits for a peaceful resolution
- **Lawful Path** - Report to the city guards (requires Guard Reputation >= 20)

Each choice sets a world flag that can affect future quests and NPC reactions.

---

## Branching Structure

```
    Stage 0: Introduction
    ┌────────────────────────────────────┐
    │  Task: Talk to the Merchant        │
    └────────────────────────────────────┘
                    │
                    ▼ (OnComplete)
    Stage 1: The Choice
    ┌────────────────────────────────────┐
    │  [PLAYER CHOICE]                   │
    │                                    │
    │  ┌─────────┐ ┌─────────┐ ┌───────┐ │
    │  │ Combat  │ │Diplomacy│ │Lawful │ │
    │  │ (free)  │ │ (free)  │ │(gated)│ │
    │  └────┬────┘ └────┬────┘ └───┬───┘ │
    └───────│───────────│──────────│─────┘
            │           │          │
            ▼           ▼          ▼
    Stage 10      Stage 20    Stage 30
    ┌──────────┐  ┌──────────┐  ┌──────────┐
    │ Defeat   │  │Negotiate │  │Report to │
    │ Bandits  │  │with      │  │ Guards   │
    │          │  │Bandits   │  │          │
    └────┬─────┘  └────┬─────┘  └────┬─────┘
         │             │             │
         ▼             ▼             ▼
    ┌────────────────────────────────────┐
    │      Stage 100: Resolution         │
    │  Task: Return to Merchant          │
    └────────────────────────────────────┘
```

### Choice Details

| Choice | Stage | Condition Required | World Flag Set |
|--------|-------|-------------------|----------------|
| Confront the Bandits | 10 | None | `WF_MerchantDilemma_ChoseCombat = true` |
| Negotiate with Bandits | 20 | None | `WF_MerchantDilemma_ChoseDiplomacy = true` |
| Report to Guards | 30 | Guard Rep >= 20 | `WF_MerchantDilemma_ChoseLawful = true` |

---

## Quest Chain Context

```
The Merchant Troubles QuestLine
├── The Merchant's Dilemma (Lvl 5) ←── THIS QUEST
│   ├── Combat Path → Future: Bandit Revenge Quest
│   ├── Diplomacy Path → Future: Bandit Alliance Quest
│   └── Lawful Path → Future: Guard Captain's Trust Quest
└── [Future quests in this storyline]
```

This quest is part of "The Merchant Troubles" questline. Player choices here set world flags that can:
- Unlock or lock future quests
- Change NPC dialogue and reactions
- Affect faction reputations

---

## Stage Structure

### Stage 0: Introduction

| Task | Type | Description |
|------|------|-------------|
| Talk to the Merchant | BoolTask | Learn about the stolen goods |

**Transition:** Automatic → Stage 1 (OnComplete)

---

### Stage 1: The Choice

This stage has **no tasks** - it exists purely for player choice presentation.

| Transition | Type | Target | Condition | World Flag |
|------------|------|--------|-----------|------------|
| Confront the Bandits | PlayerChoice | Stage 10 | None | ChoseCombat = true |
| Negotiate with Bandits | PlayerChoice | Stage 20 | None | ChoseDiplomacy = true |
| Report to Guards | PlayerChoice | Stage 30 | GuardRep >= 20 | ChoseLawful = true |

**UI Presentation:**
- Display all available choices to the player
- Gated choice (Report to Guards) should show why it's locked if conditions not met
- Each choice shows tooltip with consequences

---

### Stage 10: Combat Path

| Task | Type | Description |
|------|------|-------------|
| Defeat Bandits | BoolTask | Confront and defeat the bandits |

**Transition:** Automatic → Stage 100 (OnComplete)

**Narrative:** Player takes the direct approach, fighting the bandits. Sets a precedent that the player solves problems with violence.

---

### Stage 20: Diplomacy Path

| Task | Type | Description |
|------|------|-------------|
| Negotiate with Bandits | BoolTask | Negotiate peaceful resolution |

**Transition:** Automatic → Stage 100 (OnComplete)

**Narrative:** Player negotiates with the bandit leader. May involve paying a ransom or making a deal. Shows the player prefers peaceful solutions.

---

### Stage 30: Lawful Path

| Task | Type | Description |
|------|------|-------------|
| Report to Guards | BoolTask | Report to guards and escort them |

**Transition:** Automatic → Stage 100 (OnComplete)

**Narrative:** Player works with the authorities. Requires having built reputation with guards first. Guards handle the combat while player assists.

---

### Stage 100: Resolution

| Task | Type | Description |
|------|------|-------------|
| Return to Merchant | BoolTask | Return with news of recovered goods |

**Transition:** Quest Completes (Terminal stage)

---

## Condition System

### Start Conditions

| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_ID_Market` | ConditionID_SO | Player must be in the market |

### Global Task Failure Conditions

| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_Event_Bool_PlayerDeath` | ConditionBool_SO | Player death fails current task |

### Choice Gating Condition

| Condition | Type | Description |
|-----------|------|-------------|
| `SO_Condition_GuardReputation20` | ConditionInt_SO | Requires Guard Reputation >= 20 |

---

## World Flags

### Flags Set by This Quest

| Flag | Type | Set When |
|------|------|----------|
| `WF_MerchantDilemma_ChoseCombat` | WorldFlagBool_SO | Player chooses Combat path |
| `WF_MerchantDilemma_ChoseDiplomacy` | WorldFlagBool_SO | Player chooses Diplomacy path |
| `WF_MerchantDilemma_ChoseLawful` | WorldFlagBool_SO | Player chooses Lawful path |

### Flags Used by This Quest

| Flag | Type | Used For |
|------|------|----------|
| `WF_PlayerGuardReputation` | WorldFlagInt_SO | Gating the Lawful path choice |

---

## Folder Structure

```
The Merchant's Dilemma/
├── README.md                               <- This file
├── SO_Quest_TheMerchantsDilemma.asset      <- Main quest definition
└── Tasks/
    ├── SO_Task_TalkToMerchant.asset        <- Stage 0 (Introduction)
    ├── SO_Task_DefeatBandits.asset         <- Stage 10 (Combat Path)
    ├── SO_Task_NegotiateWithBandits.asset  <- Stage 20 (Diplomacy Path)
    ├── SO_Task_ReportToGuards.asset        <- Stage 30 (Lawful Path)
    └── SO_Task_ReturnToMerchant.asset      <- Stage 100 (Resolution)
```

---

## Related Assets

### World Flags (in `../WorldFlags/`)

| Asset | Type | Purpose |
|-------|------|---------|
| `WF_MerchantDilemma_ChoseCombat` | WorldFlagBool_SO | Tracks combat choice |
| `WF_MerchantDilemma_ChoseDiplomacy` | WorldFlagBool_SO | Tracks diplomacy choice |
| `WF_MerchantDilemma_ChoseLawful` | WorldFlagBool_SO | Tracks lawful choice |
| `WF_PlayerGuardReputation` | WorldFlagInt_SO | Player's guard reputation |

### Conditions (in `../Conditions/`)

| Asset | Type | Used By |
|-------|------|---------|
| `SO_Condition_Event_ID_Market` | ConditionID_SO | Quest Start |
| `SO_Condition_Event_Bool_PlayerDeath` | ConditionBool_SO | Global Task Failure |
| `SO_Condition_GuardReputation20` | ConditionInt_SO | Lawful Path Gate |

### Rewards (in `../Rewards/`)

| Asset | Type | Amount |
|-------|------|--------|
| `SO_QuestReward_Gold` | QuestRewardType_SO | 500 |
| `SO_QuestReward_Experience` | QuestRewardType_SO | 750 |

---

## Testing Guide

### Prerequisites
1. Ensure QuestManager is in scene with quest registered
2. Set `WF_PlayerGuardReputation` to various values to test gating

### Test 1: Combat Path

1. Start quest via QuestManager.StartQuest()
2. **Stage 0:** Complete "Talk to Merchant"
3. **Stage 1:** Select "Confront the Bandits"
4. Verify `WF_MerchantDilemma_ChoseCombat` is set to true
5. **Stage 10:** Complete "Defeat Bandits"
6. **Stage 100:** Complete "Return to Merchant"
7. Verify quest completes and rewards distributed

### Test 2: Diplomacy Path

1. Start quest
2. **Stage 0:** Complete talk task
3. **Stage 1:** Select "Negotiate with Bandits"
4. Verify `WF_MerchantDilemma_ChoseDiplomacy` is set to true
5. Complete remaining stages
6. Verify quest completes

### Test 3: Lawful Path (Gated)

**Without reputation:**
1. Start quest, complete Stage 0
2. **Stage 1:** Verify "Report to Guards" is NOT available (grayed out)
3. Verify tooltip shows "Requires Guard Reputation >= 20"

**With reputation:**
1. Set `WF_PlayerGuardReputation` to 25
2. Start quest, complete Stage 0
3. **Stage 1:** Verify "Report to Guards" IS available
4. Select it and verify `WF_MerchantDilemma_ChoseLawful` is set
5. Complete remaining stages

### Test 4: World Flag Persistence

1. Complete quest via any path
2. Verify appropriate world flag is set
3. Exit play mode, re-enter
4. Verify world flag persists (if using save system)
5. Verify other quests can read the flag

---

## Design Notes

### Why Stage-Based Branching?

Stage-based branching (vs. graph-based) offers:
1. **Clear progression** - Designers see quest as linear stages with branches
2. **Easy validation** - Can verify all stages are reachable
3. **Simple serialization** - Stage indices work well with Unity
4. **Familiar pattern** - Similar to Bethesda/BioWare quest design

### World Flag Best Practices

1. **Use descriptive names** - `WF_QuestName_ChoiceDescription`
2. **Set on choice, not completion** - Flag should reflect decision, not outcome
3. **Document consequences** - Future quests should reference which flags they check
4. **Consider mutual exclusivity** - Only one path flag should be true per quest

### UI Recommendations

For the choice presentation (Stage 1):
1. Show all choices in a modal/overlay
2. Clearly indicate locked choices and why
3. Show choice descriptions/consequences
4. Confirm before committing to choice

---

## Localization Keys

| Asset | Key | Example Text |
|-------|-----|--------------|
| Quest | quest_merchant_dilemma_name | "The Merchant's Dilemma" |
| Quest | quest_merchant_dilemma_desc | "Help a merchant recover stolen goods. How you do it is up to you." |
| Stage 1 | stage_the_choice_journal | "The merchant has told me about the bandits. I must decide how to proceed." |
| Choice 1 | choice_combat_text | "Confront the Bandits" |
| Choice 1 | choice_combat_tooltip | "Fight the bandits directly. Violent but effective." |
| Choice 2 | choice_diplomacy_text | "Negotiate with Bandits" |
| Choice 2 | choice_diplomacy_tooltip | "Try to reason with the bandit leader. May require payment." |
| Choice 3 | choice_lawful_text | "Report to Guards" |
| Choice 3 | choice_lawful_tooltip | "Inform the city guards. Requires their trust." |
| Task 1 | task_talk_merchant_name | "Meet the Merchant" |
| Task 2 | task_defeat_bandits_name | "Defeat the Bandits" |
| Task 3 | task_negotiate_name | "Negotiate with Bandits" |
| Task 4 | task_report_guards_name | "Report to Guards" |
| Task 5 | task_return_merchant_name | "Return to Merchant" |
