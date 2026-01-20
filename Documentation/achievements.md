# Achievement System

*Last Updated: 2026-01-18*

## Overview

The Achievement System provides a framework for tracking player accomplishments. Built on the same Objective interfaces as the Quest System, it offers:

- **Multiple Achievement Types** - Binary, Progressive, and Hidden achievements
- **Condition-Based Unlocking** - Automatic unlocking via event-driven conditions
- **Progress Tracking** - Track incremental progress toward goals
- **Category Organization** - Group achievements by category
- **Points System** - Assign point values for gamification
- **Save/Load Integration** - Persist progress across sessions

---

## Architecture

```
Achievement_SO (ScriptableObject - configuration)

AchievementRuntime (runtime state and tracking)

AchievementManager (singleton - lifecycle management)
```

### Interface Implementation

The Achievement System implements the Objective interfaces:

| Class | Implements | Role |
|-------|------------|------|
| `AchievementRuntime` | `IObjective`, `IObjectiveGroup` | Single trackable achievement |

---

## Achievement Types

### Binary
Simple yes/no achievements. Either locked or unlocked.

```
Example: "First Blood" - Defeat your first enemy
```

### Progressive
Track progress toward a target value. Shows progress bar.

```
Example: "Monster Hunter" - Defeat 100 enemies (shows 47/100)
```

### Hidden
Not visible in achievement list until unlocked. Good for spoiler-sensitive content.

```
Example: "Secret Ending" - Discover the hidden finale
```

---

## ScriptableObjects

### Achievement_SO

The achievement configuration asset.

**Create:** `Create > HelloDev > Quest System > Achievements > Achievement`

| Field | Type | Description |
|-------|------|-------------|
| `devName` | string | Developer-friendly name |
| `achievementId` | Guid | Auto-generated unique identifier |
| `achievementType` | AchievementType | Binary, Progressive, or Hidden |
| `displayName` | LocalizedString | Localized title |
| `achievementDescription` | LocalizedString | Localized description |
| `hiddenDescription` | LocalizedString | Text shown when hidden (before unlock) |
| `lockedIcon` | Sprite | Icon shown when locked |
| `unlockedIcon` | Sprite | Icon shown when unlocked |
| `targetValue` | int | Target for progressive achievements |
| `startValue` | int | Starting value for progressive achievements |
| `unlockCondition` | Condition_SO | Condition that auto-unlocks |
| `points` | int | Points awarded when unlocked |
| `category` | string | Category for organization |

---

## AchievementManager API

### Getting Achievements

```csharp
// Get by ID
AchievementRuntime achievement = AchievementManager.Instance.GetAchievement(achievementGuid);

// Get by ScriptableObject
AchievementRuntime achievement = AchievementManager.Instance.GetAchievement(myAchievement_SO);

// Get all
IReadOnlyCollection<AchievementRuntime> all = AchievementManager.Instance.AllAchievements;

// Get unlocked
IReadOnlyCollection<AchievementRuntime> unlocked = AchievementManager.Instance.UnlockedAchievements;

// Get by category
IReadOnlyList<AchievementRuntime> combat = AchievementManager.Instance.GetAchievementsByCategory("Combat");
```

### Unlocking Achievements

```csharp
// Manual unlock by ID
bool success = AchievementManager.Instance.UnlockAchievement(achievementGuid);

// Manual unlock by ScriptableObject
bool success = AchievementManager.Instance.UnlockAchievement(myAchievement_SO);
```

### Updating Progress

```csharp
// Increment progress (for progressive achievements)
AchievementManager.Instance.IncrementProgress(achievementGuid, 1);

// Set progress directly
AchievementManager.Instance.SetProgress(achievementGuid, 50);
```

### Querying State

```csharp
// Check if unlocked
bool isUnlocked = AchievementManager.Instance.IsAchievementUnlocked(achievementGuid);

// Get stats
int totalPoints = AchievementManager.Instance.TotalPointsEarned;
int possiblePoints = AchievementManager.Instance.TotalPointsPossible;
float percentage = AchievementManager.Instance.UnlockPercentage; // 0.0 to 1.0
```

### Events

```csharp
AchievementManager.Instance.OnAchievementUnlocked.AddListener(HandleUnlocked);
AchievementManager.Instance.OnAchievementProgressChanged.AddListener(HandleProgress);

void HandleUnlocked(AchievementRuntime achievement)
{
    // Show unlock popup
    ShowAchievementPopup(achievement.Data.DisplayName, achievement.Data.UnlockedIcon);
}

void HandleProgress(AchievementRuntime achievement)
{
    // Update progress UI
    Debug.Log($"{achievement.Data.DevName}: {achievement.CurrentValue}/{achievement.Data.TargetValue}");
}
```

---

## Example Scene Setup

### 1. Create AchievementManager

1. Create empty GameObject: `AchievementManager`
2. Add `AchievementManager` component
3. Enable `Auto Start Tracking` (recommended)
4. Populate `Achievement Database` with all achievements

### 2. Create Achievement Assets

**Example: Binary Achievement**

1. `Create > HelloDev > Quest System > Achievements > Achievement`
2. Name: `Achievement_FirstBlood`
3. Configure:
   - achievementType: Binary
   - displayName: "First Blood"
   - achievementDescription: "Defeat your first enemy"
   - unlockCondition: ConditionEventDriven_SO (listens to OnEnemyKilled)
   - points: 10
   - category: "Combat"

**Example: Progressive Achievement**

1. `Create > HelloDev > Quest System > Achievements > Achievement`
2. Name: `Achievement_MonsterHunter`
3. Configure:
   - achievementType: Progressive
   - displayName: "Monster Hunter"
   - achievementDescription: "Defeat 100 enemies"
   - targetValue: 100
   - unlockCondition: ConditionEventDriven_SO (increments on OnEnemyKilled)
   - points: 50
   - category: "Combat"

**Example: Hidden Achievement**

1. `Create > HelloDev > Quest System > Achievements > Achievement`
2. Name: `Achievement_SecretEnding`
3. Configure:
   - achievementType: Hidden
   - displayName: "Secret Ending"
   - achievementDescription: "Discovered the hidden finale"
   - hiddenDescription: "???"
   - points: 100
   - category: "Story"

### 3. Create Achievement UI

```
Canvas
├── AchievementListPanel
│   └── AchievementScrollView
│       └── Content (Vertical Layout Group)
│           └── AchievementEntry (prefab instances)
│
└── UnlockPopup (hidden by default)
    ├── PopupBackground
    ├── AchievementIcon (Image)
    ├── AchievementTitle (TextMeshPro)
    └── PointsText (TextMeshPro)
```

### 4. Create UI Controller

```csharp
public class AchievementUIController : MonoBehaviour
{
    [SerializeField] private GameObject unlockPopup;
    [SerializeField] private Image popupIcon;
    [SerializeField] private TMP_Text popupTitle;
    [SerializeField] private TMP_Text popupPoints;
    [SerializeField] private float popupDuration = 3f;

    [SerializeField] private Transform listContent;
    [SerializeField] private AchievementEntryUI entryPrefab;

    private void Start()
    {
        unlockPopup.SetActive(false);

        AchievementManager.Instance.OnAchievementUnlocked.AddListener(ShowUnlockPopup);

        RefreshList();
    }

    private void ShowUnlockPopup(AchievementRuntime achievement)
    {
        popupIcon.sprite = achievement.Data.UnlockedIcon;
        popupTitle.text = achievement.Data.DisplayName.GetLocalizedString();
        popupPoints.text = $"+{achievement.Data.Points} pts";

        unlockPopup.SetActive(true);
        StartCoroutine(HidePopupAfterDelay());
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupDuration);
        unlockPopup.SetActive(false);
    }

    private void RefreshList()
    {
        // Clear existing entries
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        // Create entry for each achievement
        foreach (var achievement in AchievementManager.Instance.AllAchievements)
        {
            // Skip hidden achievements that aren't unlocked
            if (achievement.Data.IsHidden && !achievement.IsUnlocked)
                continue;

            var entry = Instantiate(entryPrefab, listContent);
            entry.Setup(achievement);
        }
    }
}
```

### 5. Achievement Entry Prefab

```csharp
public class AchievementEntryUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject completedOverlay;

    public void Setup(AchievementRuntime achievement)
    {
        var data = achievement.Data;

        // Icon
        icon.sprite = achievement.IsUnlocked ? data.UnlockedIcon : data.LockedIcon;

        // Title
        titleText.text = data.DisplayName.GetLocalizedString();

        // Description
        descriptionText.text = data.AchievementDescription.GetLocalizedString();

        // Progress (for progressive achievements)
        bool isProgressive = data.AchievementType == AchievementType.Progressive;
        progressBar.gameObject.SetActive(isProgressive && !achievement.IsUnlocked);

        if (isProgressive)
        {
            progressBar.value = achievement.Progress;
            progressText.text = $"{achievement.CurrentValue}/{data.TargetValue}";
        }

        // Completed state
        completedOverlay.SetActive(achievement.IsUnlocked);
    }
}
```

---

## Automatic Unlocking with Conditions

The most powerful feature is condition-based automatic unlocking. Use `ConditionEventDriven_SO` to listen for game events.

### Example: Kill Counter Achievement

1. Create `GameEvent_ID_SO` for enemy kills:
   - Name: `Event_OnEnemyKilled`

2. Create `ConditionEventDriven_SO`:
   - Name: `Condition_EnemyKilled`
   - Target Event: Event_OnEnemyKilled

3. In your combat code:
```csharp
public void OnEnemyDied(Enemy enemy)
{
    onEnemyKilled.Raise(enemy.EnemyId);
}
```

4. Configure Achievement:
   - unlockCondition: Condition_EnemyKilled
   - For progressive: The condition fires increment progress
   - For binary: First condition fire unlocks

---

## Save/Load Integration

```csharp
// Saving
List<AchievementManager.AchievementSaveData> saveData = AchievementManager.Instance.GetSaveData();
// Serialize saveData to your save file

// Loading
AchievementManager.Instance.RestoreFromSaveData(loadedSaveData);
```

### Save Data Structure

```csharp
public class AchievementSaveData
{
    public string AchievementId;   // Guid as string
    public bool IsUnlocked;         // Current unlock state
    public int CurrentValue;        // Progress value
    public string UnlockTime;       // ISO 8601 timestamp
}
```

---

## Best Practices

1. **Use Categories** - Organize achievements into meaningful groups (Combat, Exploration, Story)
2. **Balance Points** - Harder achievements should award more points
3. **Use Hidden Sparingly** - Only for genuine spoilers
4. **Provide Visual Feedback** - Always show unlock popups
5. **Include Progress** - Progressive achievements should show current/target
6. **Test Conditions** - Ensure unlock conditions fire at the right time
7. **Consider Difficulty** - Mix easy and hard achievements for player motivation

---

## Statistics Example

```csharp
public class AchievementStatsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Slider overallProgress;

    private void Update()
    {
        var manager = AchievementManager.Instance;

        statsText.text = $"Achievements: {manager.UnlockedAchievements.Count}/{manager.AllAchievements.Count}\n" +
                         $"Points: {manager.TotalPointsEarned}/{manager.TotalPointsPossible}";

        overallProgress.value = manager.UnlockPercentage;
    }
}
```

---

## Related Documentation

- [Extensible Architecture Design](extensible-architecture-design.md) - Interface design and architecture
- [Overview](overview.md) - Quest System overview
- [Event Integration](Tutorials/tutorial-event-integration.md) - Using conditions with events
- [Tutorial System](tutorials.md) - Related guided experience system
