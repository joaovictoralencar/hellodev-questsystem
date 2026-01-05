# Quest Choice Patterns

This document describes different patterns for implementing player choices in quests.

## Pattern Overview

There are two main patterns for handling branching quests with player choices:

| Pattern | Description | Best For |
|---------|-------------|----------|
| **Option B: UI Choices** | Explicit choice buttons in quest panel | Visual novels, dialogue-heavy games |
| **Option C: AAA-Style** | Choices triggered by gameplay actions | Action RPGs, immersive sims |

## Option B: UI-Based Choices (Current Implementation)

### How It Works

1. Quest stage has no task groups, only player choices
2. UI detects the choice stage and spawns choice buttons
3. Player clicks a button to select their path
4. Quest transitions to the chosen branch

### Quest Structure
```
Quest: "The Merchant's Dilemma"
├── Stage 0: "Introduction"
│   └── TaskGroup: "Meet the Merchant"
│       └── Task: "TalkToMerchant"
├── Stage 1: "The Choice" (no task groups)
│   ├── Choice: "Confront Bandits" → Stage 2
│   ├── Choice: "Negotiate" → Stage 3
│   └── Choice: "Report to Guards" → Stage 4
├── Stage 2: "Combat Path"
│   └── TaskGroup: "Fight"
│       └── Task: "DefeatBandits"
...
```

### Pros
- Clear, explicit choices
- Easy to implement and understand
- Works well for dialogue-driven games

### Cons
- Breaks immersion (choices in UI, not world)
- Not how AAA games typically handle it
- Requires dedicated UI for choices

---

## Option C: AAA-Style Choices (Recommended)

This pattern matches how games like The Witcher 3, Skyrim, and Baldur's Gate 3 handle quest choices.

### Core Principle

**Choices happen through gameplay, not UI buttons.**

The quest journal shows an objective that leads to a choice moment. The actual choice is made through gameplay actions (dialogue, location visits, interactions). The journal then updates to reflect the chosen path.

### How It Works

1. Quest stage has a "decision task" describing the situation
2. Task has multiple completion conditions, one per choice
3. Each condition is triggered by a gameplay action
4. When any condition is met, the corresponding choice is selected automatically

### Quest Structure
```
Quest: "The Merchant's Dilemma"
├── Stage 0: "Introduction"
│   └── TaskGroup: "Meet the Merchant"
│       └── Task: "TalkToMerchant"
├── Stage 1: "The Decision"
│   └── TaskGroup: "Handle the Situation"
│       └── Task: "DecideHowToHandle" (decision task)
│           ├── Condition A: "TalkedToBanditLeader" → triggers combat
│           ├── Condition B: "UsedPersuadeOnBandit" → triggers negotiate
│           └── Condition C: "TalkedToGuardCaptain" → triggers guards
├── Stage 2: "Combat Path"
│   └── TaskGroup: "Fight"
│       └── Task: "DefeatBandits"
...
```

### Implementation

#### 1. Create a Decision Task

Instead of a choice-only stage, create a task that describes the decision:
- **Display Name**: "Decide how to handle the bandits"
- **Description**: "You've discovered the bandit hideout. You could confront them, try to negotiate, or report to the guards."

#### 2. Add Multiple Completion Conditions

Each condition represents a different player action:

```csharp
// Condition A: Combat path
ConditionID_SO with targetValue = "BanditLeaderConfronted"

// Condition B: Negotiate path
ConditionID_SO with targetValue = "BanditNegotiation"

// Condition C: Guards path
ConditionID_SO with targetValue = "GuardCaptainInformed"
```

#### 3. Wire Up Gameplay Triggers

Each condition is triggered by actual gameplay:
- **Combat**: Enter bandit camp with weapons drawn → raises event with "BanditLeaderConfronted"
- **Negotiate**: Dialogue option with bandit leader → raises event with "BanditNegotiation"
- **Guards**: Talk to guard captain → raises event with "GuardCaptainInformed"

#### 4. Configure Stage Transitions

The choice transitions use conditions that match the task conditions:
```
Stage "The Decision":
  Transitions:
  - To Stage 2 (Combat) when: ConditionID == "BanditLeaderConfronted"
  - To Stage 3 (Negotiate) when: ConditionID == "BanditNegotiation"
  - To Stage 4 (Guards) when: ConditionID == "GuardCaptainInformed"
```

### Pros
- Immersive - choices feel like natural gameplay
- Matches AAA RPG conventions
- No special UI needed
- Supports organic discovery of options

### Cons
- More complex setup
- Requires careful condition design
- Less obvious to players what their options are

---

## Hybrid Approach

For maximum flexibility, combine both patterns:

1. **Show decision task in journal**: "Decide how to handle the bandits"
2. **List options in task description**: Describe the available approaches
3. **Allow gameplay triggers**: Player can trigger choice through world interaction
4. **Fallback UI buttons**: If player opens quest panel, show explicit choice buttons

This gives players multiple ways to make their choice.

---

## Migration Guide: Converting UI Choices to AAA-Style

### Before (Option B)
```
Stage: "The Choice"
  TaskGroups: none
  PlayerChoices:
    - ChoiceId: "combat"
    - ChoiceId: "negotiate"
    - ChoiceId: "guards"
```

### After (Option C)
```
Stage: "The Decision"
  TaskGroups:
    - TaskGroup: "Handle the Situation"
        Tasks:
          - Task: "DecideHowToHandle"
              Conditions:
                - ConditionID("BanditCombat")
                - ConditionID("BanditNegotiate")
                - ConditionID("ReportToGuards")
  Transitions:
    - OnCondition: ConditionID("BanditCombat") → Stage 2
    - OnCondition: ConditionID("BanditNegotiate") → Stage 3
    - OnCondition: ConditionID("ReportToGuards") → Stage 4
```

### Key Changes

1. **Add a task group** with a decision task
2. **Task has multiple conditions** (one per choice)
3. **Transitions use OnCondition** instead of PlayerChoice
4. **Gameplay triggers conditions** instead of UI buttons

---

## Best Practices

1. **Make choices meaningful**: Each path should have different consequences
2. **Provide context**: Task description should hint at available options
3. **Support exploration**: Let players discover options naturally
4. **Allow changes**: Consider if players should be able to reconsider before committing
5. **Log decisions**: Track which path was chosen for narrative purposes

## See Also

- [Tasks Documentation](tasks.md)
- [Stages and Transitions](stages.md)
- [Conditions System](conditions.md)
