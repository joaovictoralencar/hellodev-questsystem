# Quest Dependency Diagram

Visual representation of quest chains and relationships in the BasicQuestExample.

## Master Quest Flow

```
                              ┌─────────────────────────────────────────┐
                              │            QUEST DEPENDENCIES           │
                              └─────────────────────────────────────────┘

    ┌───────────────────────────────────────────────────────────────────────────────┐
    │                          THE GOBLIN THREAT (Main QuestLine)                    │
    │                                                                               │
    │    ┌───────────────────┐                                                      │
    │    │   GOBLIN'S BANE   │                                                      │
    │    │    (Main, Lvl 5)  │                                                      │
    │    │                   │                                                      │
    │    │ Prerequisites:    │                                                      │
    │    │  • Level >= 5     │                                                      │
    │    │  • In Village     │                                                      │
    │    └─────────┬─────────┘                                                      │
    │              │                                                                │
    │              │ completes                                                      │
    │              ▼                                                                │
    │    ┌─────────────────────────────────────────────┐                           │
    │    │           THE GOBLIN CONSPIRACY              │◄─────────────────────┐   │
    │    │              (Main, Lvl 6)                   │                      │   │
    │    │                                              │                      │   │
    │    │  Prerequisites: (OR logic)                   │                      │   │
    │    │   • Goblin's Bane completed        ──────────┘                      │   │
    │    │   • OR The Bandit's Employer completed ─────────────────────────────│───┘
    │    │                                              │                      │
    │    │  Features:                                   │                      │
    │    │   • TimedTask (ritual, 120s)                │                      │
    │    │   • Multi-path narrative convergence         │                      │
    │    └─────────────────────────────────────────────┘                      │
    └───────────────────────────────────────────────────────────────────────────────┘
                                                                               │
    ┌───────────────────────────────────────────────────────────────────────────────┐
    │                       THE MERCHANT TROUBLES (Side QuestLine)                   │
    │                                                                               │
    │    ┌─────────────────────────┐         ┌──────────────────────┐              │
    │    │ MERCHANT'S STOLEN GOODS │         │ THE MERCHANT'S       │              │
    │    │   (Secondary, Lvl 3)    │         │    DILEMMA           │              │
    │    │                         │         │ (Secondary, Lvl 5)   │              │
    │    │ Prerequisites:          │         │                      │              │
    │    │  • None                 │         │ Prerequisites:       │              │
    │    └───────────┬─────────────┘         │  • In Market         │              │
    │                │                        │                      │              │
    │                │ completes              │ Features:            │              │
    │                ▼                        │  • 3-way branching   │              │
    │    ┌─────────────────────────┐         │  • World flags       │              │
    │    │  THE BANDIT'S EMPLOYER  │         │  • Gated choice      │              │
    │    │   (Secondary, Lvl 4)    │         └──────────────────────┘              │
    │    │                         │                                               │
    │    │ Prerequisites:          │                                               │
    │    │  • Merchant's Stolen    │                                               │
    │    │    Goods completed      │                                               │
    │    └───────────┬─────────────┘                                               │
    │                │                                                             │
    │                │ completes                                                   │
    │                └──────────────────────────────────────────────────────────────┘
```

## QuestLine Summary

### TheGoblinThreat (Main Story)
| Quest | Level | Type | Prerequisites | Rewards |
|-------|-------|------|---------------|---------|
| Goblin's Bane | 5 | Main | Level >= 5, In Village | 1500 XP |
| The Goblin Conspiracy | 6 | Main | Goblin's Bane OR Bandit's Employer | 2000 XP, 1500 Gold |

### TheMerchantTroubles (Side Story)
| Quest | Level | Type | Prerequisites | Rewards |
|-------|-------|------|---------------|---------|
| Merchant's Stolen Goods | 3 | Secondary | None | 500 XP |
| The Bandit's Employer | 4 | Secondary | Merchant's Stolen Goods completed | 1000 Gold, 750 XP |
| The Merchant's Dilemma | 5 | Secondary | In Market | 500 Gold, 750 XP |

## Branching Quest: The Merchant's Dilemma

```
    Stage 0: Introduction
    ┌────────────────────────────────────┐
    │  Task: Talk to the Merchant        │
    └─────────────────┬──────────────────┘
                      │
                      ▼
    Stage 1: The Choice (Player Choice)
    ┌────────────────────────────────────────────────────────────────┐
    │                                                                │
    │   ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐   │
    │   │   COMBAT     │  │  DIPLOMACY   │  │     LAWFUL        │   │
    │   │  (always)    │  │  (always)    │  │ (Guard Rep >= 20) │   │
    │   └──────┬───────┘  └──────┬───────┘  └────────┬──────────┘   │
    │          │                 │                    │              │
    │     Sets Flag:        Sets Flag:           Sets Flag:         │
    │     ChoseCombat       ChoseDiplomacy       ChoseLawful        │
    └──────────┼─────────────────┼────────────────────┼─────────────┘
               │                 │                    │
               ▼                 ▼                    ▼
    ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
    │  Stage 10    │  │  Stage 20    │  │    Stage 30      │
    │ Defeat       │  │ Negotiate    │  │ Report to        │
    │ Bandits      │  │ with Bandits │  │ Guards           │
    └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘
           │                 │                    │
           └────────────────┬────────────────────┘
                            ▼
    ┌────────────────────────────────────┐
    │      Stage 100: Resolution         │
    │  Task: Return to Merchant          │
    │        (Terminal Stage)            │
    └────────────────────────────────────┘
```

## Convergence Points

The system demonstrates **narrative convergence** where separate storylines merge:

```
    Path A: Goblin Storyline           Path B: Merchant Storyline

    Goblin's Bane (Lvl 5)              Merchant's Stolen Goods (Lvl 3)
            │                                     │
            │                                     ▼
            │                          The Bandit's Employer (Lvl 4)
            │                                     │
            └──────────────┬──────────────────────┘
                           │
                           ▼
                  The Goblin Conspiracy (Lvl 6)
                    (Quest Chain Finale)
```

Both paths reveal the same villain (the cult) and lead to the same climactic quest.

## Condition Types Used

| Condition Type | Purpose | Example |
|---------------|---------|---------|
| ConditionInt_SO | Level checks, counters | `Level >= 5` |
| ConditionID_SO | Location presence | `In Village`, `In Market` |
| ConditionQuestState_SO | Quest completion | `Merchant's Stolen Goods == Completed` |
| CompositeCondition_SO | OR logic for multiple paths | `Bandit's Employer OR Goblin's Bane` |
| WorldFlagInt_SO | World state gating | `Guard Reputation >= 20` |

## World Flags Set

| Quest | Flag | Possible Values |
|-------|------|-----------------|
| The Merchant's Dilemma | `WF_MerchantDilemma_ChoseCombat` | true/false |
| The Merchant's Dilemma | `WF_MerchantDilemma_ChoseDiplomacy` | true/false |
| The Merchant's Dilemma | `WF_MerchantDilemma_ChoseLawful` | true/false |

## Related Documentation

- [BasicQuestExample README](../BasicQuestExample/README.md) - Setup and usage
- [Event Integration Guide](../BasicQuestExample/Docs/EventIntegrationGuide.md) - Game event wiring
- [Quest Graph Editor Guide](quest-graph-editor-guide.md) - Visual quest editing
- [Choice Patterns](choice-patterns.md) - Branching design patterns
