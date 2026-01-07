# Rewards

## Overview

The reward system provides extensible quest completion rewards. Rewards are defined as ScriptableObjects and referenced in quests with amounts.

## Files

| File | Class | Purpose |
|------|-------|---------|
| `QuestRewardType_SO.cs` | `QuestRewardType_SO`, `RewardInstance` | Base reward type and instance struct |
| `ExperienceQuestRewardType_SO.cs` | `ExperienceQuestRewardType_SO` | Example: XP reward |

## QuestRewardType_SO

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/ScriptableObjects/QuestRewardType_SO.cs`
**Inherits:** `ScriptableObject`

Abstract base class for all reward types.

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `Icon` | `Sprite` | Reward icon for UI |
| `Name` | `LocalizedString` | Localized reward name |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `RewardIcon` | `Sprite` | Icon accessor |
| `RewardName` | `LocalizedString` | Name accessor |

### Abstract Methods

#### `GiveReward(int amount)`
Override to implement reward granting logic.

```csharp
public abstract void GiveReward(int amount);
```

---

## RewardInstance

**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/ScriptableObjects/QuestRewardType_SO.cs`

Struct combining a reward type with an amount.

```csharp
[Serializable]
public struct RewardInstance
{
    public QuestRewardType_SO RewardType;
    public int Amount;
}
```

### Usage in Quest_SO

```csharp
// Quest_SO has:
[SerializeField] private List<RewardInstance> rewards;

// Accessed via:
public List<RewardInstance> Rewards => rewards;
```

---

## ExperienceQuestRewardType_SO

**Path:** `Assets/com.hellodev.questsystem/BasicQuestExample/Scripts/Rewards/ExperienceQuestRewardType_SO.cs`
**Inherits:** `QuestRewardType_SO`
**Create Menu:** `HelloDev/Quest System/Rewards/Experience Quest RewardType`

Example implementation for experience point rewards.

### Implementation

```csharp
public override void GiveReward(int amount)
{
    Debug.Log($"Added {amount} experience to the player!");
    // TODO: Connect to actual experience system
}
```

## Creating Custom Reward Types

### Step 1: Create the ScriptableObject
```csharp
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GoldReward",
    menuName = "HelloDev/Quest System/Rewards/Gold Reward")]
public class GoldQuestRewardType_SO : QuestRewardType_SO
{
    public override void GiveReward(int amount)
    {
        // Connect to your currency system
        CurrencyManager.Instance.AddGold(amount);
        Debug.Log($"Awarded {amount} gold!");
    }
}
```

### Step 2: Create Asset
1. Right-click in Project window
2. Create > HelloDev/Quest System/Rewards/Gold Reward
3. Configure Icon and Name

### Step 3: Add to Quest
1. Open Quest_SO asset
2. Add entry to Rewards list
3. Set RewardType reference
4. Set Amount

## Granting Rewards

**Note:** The current implementation does NOT automatically grant rewards on quest completion. You must implement this manually.

### Manual Reward Distribution
```csharp
public class QuestRewardHandler : MonoBehaviour
{
    private void OnEnable()
    {
        QuestManager.Instance.QuestCompleted.AddListener(GrantRewards);
    }

    private void OnDisable()
    {
        QuestManager.Instance.QuestCompleted.RemoveListener(GrantRewards);
    }

    private void GrantRewards(Quest quest)
    {
        foreach (var reward in quest.QuestData.Rewards)
        {
            reward.RewardType.GiveReward(reward.Amount);
        }
    }
}
```

## UI Integration

### Displaying Rewards in Quest UI
```csharp
public class RewardDisplay : MonoBehaviour
{
    [SerializeField] private Transform rewardContainer;
    [SerializeField] private GameObject rewardItemPrefab;

    public void ShowRewards(Quest quest)
    {
        // Clear existing
        rewardContainer.DestroyAllChildren();

        // Create reward items
        foreach (var reward in quest.QuestData.Rewards)
        {
            var item = Instantiate(rewardItemPrefab, rewardContainer);
            var display = item.GetComponent<RewardItemDisplay>();

            display.SetIcon(reward.RewardType.RewardIcon);
            display.SetName(reward.RewardType.RewardName);
            display.SetAmount(reward.Amount);
        }
    }
}
```

### BasicQuestExample UI Components

The BasicQuestExample includes:
- `UI_QuestRewards` - Container for reward display
- `UI_QuestRewardItem` - Individual reward item

```csharp
// UI_QuestRewards.Setup(Quest quest)
// - Clears previous rewards
// - Shows "No Rewards" if empty
// - Spawns UI_QuestRewardItem for each reward

// UI_QuestRewardItem.Setup(RewardInstance reward)
// - Shows reward icon
// - Shows localized name
// - Shows amount (if > 1)
```

## Common Reward Types

| Type | Use Case |
|------|----------|
| Experience | Level progression |
| Currency | Gold, gems, credits |
| Items | Inventory items |
| Reputation | Faction standing |
| Unlocks | New areas, abilities |
| Achievements | Badges, titles |

### Example Implementations

```csharp
// Item Reward
public class ItemQuestRewardType_SO : QuestRewardType_SO
{
    [SerializeField] private ID_SO itemId;

    public override void GiveReward(int amount)
    {
        Inventory.Instance.AddItem(itemId, amount);
    }
}

// Reputation Reward
public class ReputationQuestRewardType_SO : QuestRewardType_SO
{
    [SerializeField] private ID_SO factionId;

    public override void GiveReward(int amount)
    {
        FactionManager.Instance.AddReputation(factionId, amount);
    }
}
```

## Architecture Notes

- Rewards are ScriptableObjects for easy configuration
- RewardInstance struct allows same reward type with different amounts
- GiveReward() is abstract - implement for each reward type
- UI components read rewards from Quest_SO.Rewards list
- **Reward distribution is NOT automatic** - must be implemented
