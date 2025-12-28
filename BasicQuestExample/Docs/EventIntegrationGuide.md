# Quest System Event Integration Guide

This guide documents all events that game code must raise for the quest system to function correctly.

## Overview

The quest system uses **event-driven conditions**. When your game code raises an event, the quest system:
1. Evaluates conditions listening to that event
2. If a condition is met, checks if ALL conditions for that quest are now true
3. If yes, the quest starts/completes/fails automatically

**Key insight**: You only need to raise events - the quest system handles the rest.

---

## Event Design Philosophy

### Generic Events with ID Parameters

The quest system uses **generic events** with ID_SO parameters rather than specific events per entity type. This approach:

- **Reduces event proliferation** - One `OnMonsterKilled` event instead of `OnGoblinKilled`, `OnOrcKilled`, `OnSkeletonKilled`, etc.
- **Enables designer flexibility** - Create new monster types without code changes
- **Simplifies integration** - Game code raises the same event, just with different IDs

**Example:**
```csharp
// Generic approach (preferred)
OnMonsterKilled.Raise(goblinId);      // For goblins
OnMonsterKilled.Raise(orcId);         // For orcs
OnMonsterKilled.Raise(skeletonId);    // For skeletons

// vs. Specific approach (avoided)
OnGoblinKilled.Raise(count);
OnOrcKilled.Raise(count);
OnSkeletonKilled.Raise(count);
```

---

## Event Reference

### Player Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnPlayerLevelUp` | `GameEventInt_SO` | Player gains a level | `OnPlayerLevelUp.Raise(newLevel)` |
| `OnPlayerDeath` | `GameEventBool_SO` | Player dies | `OnPlayerDeath.Raise(true)` |
| `OnFindLocation` | `GameEventID_SO` | Player enters a location | `OnFindLocation.Raise(locationId)` |

### Combat Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnMonsterKilled` | `GameEventID_SO` | Any monster is killed | `OnMonsterKilled.Raise(monsterId)` |
| `OnBossDefeated` | `GameEventID_SO` | Boss is killed | `OnBossDefeated.Raise(bossId)` |
| `OnGoblinEscaped` | `GameEventInt_SO` | Goblin escapes (increment) | `OnGoblinEscaped.Raise(totalEscaped)` |

### Dialogue Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnNPCDialogue` | `GameEventID_SO` | Player finishes dialogue with NPC | `OnNPCDialogue.Raise(npcId)` |
| `OnInterrogation` | `GameEventString_SO` | Interrogation answer given | `OnInterrogation.Raise("scarface")` |

### Item Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnItemCollected` | `GameEventID_SO` | Any item is collected/recovered | `OnItemCollected.Raise(itemId)` |
| `OnItemDiscovered` | `GameEventID_SO` | Item is discovered/found | `OnItemDiscovered.Raise(itemId)` |
| `OnItemDestroyed` | `GameEventID_SO` | Item is destroyed | `OnItemDestroyed.Raise(itemId)` |
| `OnGoodsDestroyed` | `GameEventInt_SO` | Goods destroyed (count) | `OnGoodsDestroyed.Raise(totalDestroyed)` |

### Stealth/Alert Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnEnemyAlert` | `GameEventID_SO` | Enemy spots player | `OnEnemyAlert.Raise(enemyId)` |

### NPC Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnNPCKilled` | `GameEventID_SO` | Any NPC is killed | `OnNPCKilled.Raise(npcId)` |

### World Events

| Event | Type | When to Raise | Example |
|-------|------|---------------|---------|
| `OnLocationAttacked` | `GameEventID_SO` | Location is attacked | `OnLocationAttacked.Raise(locationId)` |

---

## Quest-Specific Event Requirements

### Goblin's Bane

**Start Conditions (ALL must be true):**
1. Player level >= 5 - Triggered by `OnPlayerLevelUp.Raise(level)`
2. Player in Village - Triggered by `OnFindLocation.Raise(villageId)`

**Failure Condition:**
- Village attacked - Triggered by `OnLocationAttacked.Raise(villageId)`

**Global Task Failure:**
- Player dies - Triggered by `OnPlayerDeath.Raise(true)`

**Task Completions:**
| Task | Event to Raise |
|------|----------------|
| Investigate Attacks | `OnItemDiscovered.Raise(clueId)` for each clue |
| Track Goblin Camp | `OnFindLocation.Raise(campId)` |
| Kill Goblins | `OnMonsterKilled.Raise(goblinId)` for each kill |
| Find Goblin Campsite | `OnFindLocation.Raise(campsiteId)` |
| Defeat Chief | `OnBossDefeated.Raise(chiefId)` |
| Return to Village | `OnFindLocation.Raise(villageId)` |

### Merchant's Stolen Goods

**Start Conditions:** None (starts when added to QuestManager)

**Failure Condition:**
- Merchant killed - Triggered by `OnNPCKilled.Raise(merchantId)`

**Task Completions:**
| Task | Event to Raise |
|------|----------------|
| Talk to Merchant | `OnNPCDialogue.Raise(merchantId)` |
| Search Crime Scene | `OnItemDiscovered.Raise(clueId)` for each clue |
| Follow Trail | `OnFindLocation.Raise(banditCampId)` |
| Recover Goods | `OnItemCollected.Raise(goodsId)` for each crate |
| Interrogate Leader | `OnInterrogation.Raise("scarface")` |
| Return to Merchant | `OnFindLocation.Raise(marketId)` |

### The Bandit's Employer

**Start Conditions:**
- Merchant's Stolen Goods completed - Uses `ConditionQuestState_SO` (auto-triggered)

### The Goblin Conspiracy

**Start Conditions:**
- Bandit's Employer OR Goblin's Bane completed - Uses composite condition (auto-triggered)

---

## Integration Code Examples

### MonoBehaviour: Location Trigger

```csharp
public class LocationTrigger : MonoBehaviour
{
    [SerializeField] private ID_SO locationId;
    [SerializeField] private GameEventID_SO onFindLocation;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onFindLocation.Raise(locationId);
        }
    }
}
```

### MonoBehaviour: Monster Kill Handler

```csharp
public class MonsterKillHandler : MonoBehaviour
{
    [SerializeField] private ID_SO monsterId;
    [SerializeField] private GameEventID_SO onMonsterKilled;

    public void OnDeath()
    {
        // Raise generic event with this monster's specific ID
        onMonsterKilled.Raise(monsterId);
    }
}
```

### MonoBehaviour: Item Collection

```csharp
public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private ID_SO itemId;
    [SerializeField] private GameEventID_SO onItemCollected;

    public void Collect()
    {
        onItemCollected.Raise(itemId);
        Destroy(gameObject);
    }
}
```

### MonoBehaviour: Level Up Handler

```csharp
public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private GameEventInt_SO onPlayerLevelUp;

    private int currentLevel = 1;

    public void GainExperience(int xp)
    {
        // ... XP logic ...
        if (shouldLevelUp)
        {
            currentLevel++;
            onPlayerLevelUp.Raise(currentLevel);
        }
    }
}
```

### MonoBehaviour: NPC Dialogue

```csharp
public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private ID_SO npcId;
    [SerializeField] private GameEventID_SO onNPCDialogue;
    [SerializeField] private GameEventBool_SO onDialogueComplete;

    public void OnDialogueFinished()
    {
        // Raise specific NPC event for "Talk to X" tasks
        onNPCDialogue.Raise(npcId);

        // Also raise generic dialogue complete for simpler tasks
        onDialogueComplete.Raise(true);
    }
}
```

### MonoBehaviour: Interrogation Handler

```csharp
public class InterrogationHandler : MonoBehaviour
{
    [SerializeField] private GameEventString_SO onInterrogation;

    public void OnAnswerGiven(string answer)
    {
        // Raise with the answer/name given
        onInterrogation.Raise(answer);
    }
}
```

---

## Debugging Tips

1. **Quest not starting?**
   - Check that ALL start conditions are event-driven
   - Verify events are being raised with correct values
   - Use the Quest_SO Validation tab to check for issues

2. **Task not completing?**
   - Verify the task's condition is listening to the correct event
   - Check the comparison type (Equals, GreaterThanOrEqual, etc.)
   - Ensure the ID passed to Raise() matches what the condition expects

3. **Condition evaluates correctly but quest doesn't start?**
   - Remember: When one condition fires, ALL conditions are re-evaluated
   - If condition A is met at time T1 and condition B at time T2, both must evaluate true at time T2
   - For location-based conditions, re-raise the location event when relevant

4. **Kill count not incrementing?**
   - With generic events, you need a counter in your game code
   - The condition compares against the kill count, not individual kills
   - Use ConditionInt_SO with "GreaterThanOrEqual" for "Kill X monsters" tasks

---

## Event Asset Locations

```
BasicQuestExample/ScriptableObjects/Events/
├── SO_GameEvent_OnPlayerLevelUp.asset      (GameEventInt_SO)
├── SO_GameEvent_OnPlayerDeath.asset        (GameEventBool_SO)
├── SO_GameEvent_OnFindLocation.asset       (GameEventID_SO)
├── SO_GameEvent_OnBossDefeated.asset       (GameEventID_SO)
├── SO_GameEvent_OnItemDiscovered.asset     (GameEventID_SO)
├── SO_GameEvent_OnGoblinEscaped.asset      (GameEventInt_SO)
├── SO_GameEvent_OnGoodsDestroyed.asset     (GameEventInt_SO)
├── SO_GameEvent_OnMonsterKilled.asset      (GameEventID_SO)   [v2.0.0]
├── SO_GameEvent_OnItemCollected.asset      (GameEventID_SO)   [v2.0.0]
├── SO_GameEvent_OnNPCDialogue.asset        (GameEventID_SO)   [v2.0.0]
├── SO_GameEvent_OnNPCKilled.asset          (GameEventID_SO)   [v2.0.0]
├── SO_GameEvent_OnEnemyAlert.asset         (GameEventID_SO)   [v2.0.0]
├── SO_GameEvent_OnLocationAttacked.asset   (GameEventID_SO)   [v2.0.0]
├── SO_GameEvent_OnItemDestroyed.asset      (GameEventID_SO)   [v2.0.0]
└── SO_GameEvent_OnInterrogation.asset      (GameEventString_SO) [v2.0.0]
```

---

## New Generic Events (v2.0.0)

### OnMonsterKilled
- **Type:** `GameEventID_SO`
- **Purpose:** Track when any monster type is killed
- **Usage:** `OnMonsterKilled.Raise(monsterId)`
- **Note:** Use the monster's ID_SO (e.g., GoblinId, OrcId, SkeletonId)

### OnItemCollected
- **Type:** `GameEventID_SO`
- **Purpose:** Track when any item is collected/recovered
- **Usage:** `OnItemCollected.Raise(itemId)`
- **Note:** Use the item's ID_SO for tracking specific items

### OnNPCDialogue
- **Type:** `GameEventID_SO`
- **Purpose:** Track which NPC the player talked to (NPC-specific)
- **Usage:** `OnNPCDialogue.Raise(npcId)`
- **Note:** Allows multiple "Talk to X" tasks with different NPC IDs

### OnNPCKilled
- **Type:** `GameEventID_SO`
- **Purpose:** Track when any NPC is killed
- **Usage:** `OnNPCKilled.Raise(npcId)`
- **Note:** Use for "protect this NPC" fail conditions (e.g., merchant killed)

### OnEnemyAlert
- **Type:** `GameEventID_SO`
- **Purpose:** Track when an enemy spots the player (stealth)
- **Usage:** `OnEnemyAlert.Raise(enemyId)`
- **Note:** Use for stealth fail conditions (e.g., goblin scout alert, bandit spotted)

### OnLocationAttacked
- **Type:** `GameEventID_SO`
- **Purpose:** Track when a location is attacked
- **Usage:** `OnLocationAttacked.Raise(locationId)`
- **Note:** Use for "protect this location" fail conditions (e.g., village attacked)

### OnItemDestroyed
- **Type:** `GameEventID_SO`
- **Purpose:** Track when an item is destroyed
- **Usage:** `OnItemDestroyed.Raise(itemId)`
- **Note:** Use for "protect these goods" fail conditions

### OnInterrogation
- **Type:** `GameEventString_SO`
- **Purpose:** Track interrogation answers for StringTask
- **Usage:** `OnInterrogation.Raise("scarface")`

---

## Condition Wiring Examples

### For "Kill 5 Goblins" Task

**Condition Setup (ConditionID_SO):**
- Event: `OnMonsterKilled`
- Target ID: `GoblinId`
- Comparison: `Equals`

**Game Code:**
```csharp
// Each goblin raises the event on death
public class Goblin : Monster
{
    [SerializeField] private ID_SO goblinId;
    [SerializeField] private GameEventID_SO onMonsterKilled;

    protected override void OnDeath()
    {
        onMonsterKilled.Raise(goblinId);
        base.OnDeath();
    }
}
```

**Note:** For counting kills, use ConditionInt_SO with a kill counter in your game code, then raise a GameEventInt_SO with the total count.

### For "Recover 3 Crates" Task

**Condition Setup (ConditionInt_SO):**
- Event: `OnItemCollected` (counting variant, or use separate counter event)
- Required Value: `3`
- Comparison: `GreaterThanOrEqual`

**Game Code:**
```csharp
public class StolenGoods : MonoBehaviour
{
    [SerializeField] private ID_SO goodsId;
    [SerializeField] private GameEventID_SO onItemCollected;
    [SerializeField] private GameEventInt_SO onGoodsRecoveredCount; // For counting

    private static int recoveredCount = 0;

    public void Collect()
    {
        onItemCollected.Raise(goodsId);
        recoveredCount++;
        onGoodsRecoveredCount.Raise(recoveredCount);
        Destroy(gameObject);
    }
}
```
