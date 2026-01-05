# QuestLine Integration Design Document

## Overview

A **QuestLine** is a narrative grouping of related quests that together tell a complete story. Unlike quest chains (which define execution dependencies via `ConditionQuestState_SO`), a QuestLine is a thematic container that:

- Groups quests that belong to the same storyline
- Tracks overall progress across all contained quests
- Provides UI organization (journal sections, achievement tracking)
- Works alongside existing chain dependencies (not replacing them)

**AAA Examples:**
- Skyrim: "Companions Questline", "Thieves Guild Questline"
- Witcher 3: Story "threads" within narrative phases
- Cyberpunk 2077: Character arcs (Panam's arc, Judy's arc)

---

## Architecture Alignment

### Following HelloDev Patterns

| Pattern | QuestLine Implementation |
|---------|-------------------------|
| **Data/Runtime Split** | `QuestLine_SO` (config) + `QuestLineRuntime` (mutable state) |
| **Event-Driven** | Events for progress, completion, unlock |
| **Condition-Gated** | `ConditionQuestLineState_SO` for prerequisites |
| **Designer-Friendly** | Visual inspector with Odin, Quick Actions |

### Namespace & Location

```
Runtime/Scripts/Core/
├── QuestLines/
│   ├── QuestLineState.cs              → HelloDev.QuestSystem.QuestLines
│   └── QuestLineRuntime.cs            → HelloDev.QuestSystem.QuestLines
├── ScriptableObjects/
│   └── QuestLine_SO.cs                → HelloDev.QuestSystem.ScriptableObjects
├── Conditions/
│   └── ConditionQuestLineState_SO.cs  → HelloDev.QuestSystem.Conditions
└── QuestManager.cs                    → (extended with QuestLine support)
```

---

## Data Model

### QuestLine_SO (ScriptableObject - Designer-Configured)

```csharp
[CreateAssetMenu(menuName = "HelloDev/Quest System/Quest Line")]
public class QuestLine_SO : RuntimeScriptableObject
{
    #region Identity
    [SerializeField] private string devName;
    [SerializeField] private string questLineId;  // GUID
    #endregion

    #region Display
    [SerializeField] private LocalizedString displayName;
    [SerializeField] private LocalizedString description;
    [SerializeField] private Sprite icon;
    #endregion

    #region Configuration
    [SerializeField] private List<Quest_SO> quests;           // Ordered list
    [SerializeField] private bool requireSequentialCompletion; // Must complete in order?
    [SerializeField] private QuestLine_SO prerequisiteLine;    // Optional: unlock after another line
    #endregion

    #region Rewards
    [SerializeField] private List<RewardInstance> completionRewards; // Bonus for completing entire line
    #endregion

    #region Properties
    public string DevName => devName;
    public Guid QuestLineId => Guid.Parse(questLineId);
    public LocalizedString DisplayName => displayName;
    public LocalizedString Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<Quest_SO> Quests => quests;
    public bool RequireSequentialCompletion => requireSequentialCompletion;
    public QuestLine_SO PrerequisiteLine => prerequisiteLine;
    public List<RewardInstance> CompletionRewards => completionRewards;
    public int QuestCount => quests?.Count ?? 0;
    #endregion

    #region Factory
    public QuestLineRuntime GetRuntimeQuestLine() => new QuestLineRuntime(this);
    #endregion
}
```

### QuestLineState (Enum)

```csharp
namespace HelloDev.QuestSystem.QuestLines
{
    public enum QuestLineState
    {
        Locked,      // Prerequisite line not completed
        Available,   // Can be started
        InProgress,  // At least one quest started
        Completed,   // All quests completed
        Failed       // Optional: if any quest is failed and non-recoverable
    }
}
```

### QuestLineRuntime (Runtime - Mutable State)

```csharp
namespace HelloDev.QuestSystem.QuestLines
{
    public class QuestLineRuntime
    {
        #region Events
        public UnityEvent<QuestLineRuntime> OnQuestLineStarted = new();
        public UnityEvent<QuestLineRuntime> OnQuestLineUpdated = new();
        public UnityEvent<QuestLineRuntime> OnQuestLineCompleted = new();
        public UnityEvent<QuestLineRuntime, QuestRuntime> OnQuestInLineCompleted = new();
        #endregion

        #region Properties
        public Guid QuestLineId { get; }
        public QuestLine_SO Data { get; }
        public QuestLineState CurrentState { get; private set; }

        // Progress: 0.0 to 1.0
        public float Progress => CalculateProgress();

        // Counts
        public int CompletedQuestCount => GetCompletedCount();
        public int TotalQuestCount => Data.QuestCount;

        // Query
        public bool IsComplete => CurrentState == QuestLineState.Completed;
        public bool IsAvailable => CurrentState == QuestLineState.Available;
        public Quest_SO NextQuest => GetNextIncompleteQuest();
        #endregion

        #region Constructor
        public QuestLineRuntime(QuestLine_SO data)
        {
            Data = data;
            QuestLineId = data.QuestLineId;
            CurrentState = QuestLineState.Available;
        }
        #endregion

        #region State Management
        public void CheckProgress()
        {
            // Called by QuestManager when any quest completes
            // Updates state and fires events
        }

        public void DistributeCompletionRewards()
        {
            // Called when all quests complete
            foreach (var reward in Data.CompletionRewards)
            {
                reward.RewardType?.GiveReward(reward.Amount);
            }
        }
        #endregion

        #region Private Helpers
        private float CalculateProgress()
        {
            if (Data.QuestCount == 0) return 1f;
            return (float)GetCompletedCount() / Data.QuestCount;
        }

        private int GetCompletedCount()
        {
            int count = 0;
            foreach (var quest in Data.Quests)
            {
                if (QuestManager.Instance?.IsQuestCompleted(quest) == true)
                    count++;
            }
            return count;
        }

        private Quest_SO GetNextIncompleteQuest()
        {
            foreach (var quest in Data.Quests)
            {
                if (QuestManager.Instance?.IsQuestCompleted(quest) != true)
                    return quest;
            }
            return null;
        }
        #endregion
    }
}
```

---

## QuestManager Integration

### Option A: Extend QuestManager (Recommended)

Add questline tracking to the existing QuestManager singleton:

```csharp
public partial class QuestManager : MonoBehaviour
{
    #region QuestLine Fields
    [SerializeField] private List<QuestLine_SO> questLinesDatabase = new();
    private Dictionary<Guid, QuestLineRuntime> _activeQuestLines = new();
    private Dictionary<Guid, QuestLineRuntime> _completedQuestLines = new();
    #endregion

    #region QuestLine Events
    public UnityEvent<QuestLineRuntime> QuestLineStarted = new();
    public UnityEvent<QuestLineRuntime> QuestLineUpdated = new();
    public UnityEvent<QuestLineRuntime> QuestLineCompleted = new();
    #endregion

    #region QuestLine Lifecycle
    public bool AddQuestLine(QuestLine_SO lineData)
    {
        // Create runtime, add to tracking, fire event
    }

    public QuestLineRuntime GetQuestLine(QuestLine_SO lineData)
    {
        // Return active or completed questline
    }

    public bool IsQuestLineCompleted(QuestLine_SO lineData)
    {
        return _completedQuestLines.ContainsKey(lineData.QuestLineId);
    }

    public IReadOnlyList<QuestLineRuntime> GetActiveQuestLines()
    {
        return _activeQuestLines.Values.ToList().AsReadOnly();
    }
    #endregion

    #region QuestLine Progress Tracking
    private void OnQuestCompleted_UpdateQuestLines(QuestRuntime quest)
    {
        // Check all active questlines to see if this quest belongs to any
        // Update progress, check for line completion
        foreach (var line in _activeQuestLines.Values)
        {
            if (line.Data.Quests.Contains(quest.QuestData))
            {
                line.CheckProgress();
                QuestLineUpdated.SafeInvoke(line);

                if (line.IsComplete)
                {
                    CompleteQuestLine(line);
                }
            }
        }
    }

    private void CompleteQuestLine(QuestLineRuntime line)
    {
        _activeQuestLines.Remove(line.QuestLineId);
        _completedQuestLines.Add(line.QuestLineId, line);
        line.DistributeCompletionRewards();
        QuestLineCompleted.SafeInvoke(line);
    }
    #endregion
}
```

### Option B: Separate QuestLineManager

If you want complete separation:

```csharp
public class QuestLineManager : SingletonBase<QuestLineManager>
{
    // Same structure as Option A, but references QuestManager for quest state
}
```

**Recommendation:** Option A is cleaner and follows the existing pattern where QuestManager is the central hub.

---

## Condition Integration

### ConditionQuestLineState_SO

For prerequisites like "Thieves Guild Questline must be completed":

```csharp
[CreateAssetMenu(menuName = "HelloDev/Quest System/Conditions/Quest Line State Condition")]
public class ConditionQuestLineState_SO : Condition_SO, IConditionEventDriven
{
    [SerializeField] private QuestLine_SO questLineToCheck;
    [SerializeField] private QuestLineState targetState = QuestLineState.Completed;
    [SerializeField] private QuestStateComparison comparisonType = QuestStateComparison.Equals;

    public override bool Evaluate()
    {
        if (questLineToCheck == null || QuestManager.Instance == null)
            return IsInverted;

        var currentState = GetCurrentState();
        bool result = comparisonType == QuestStateComparison.Equals
            ? currentState == targetState
            : currentState != targetState;

        return IsInverted ? !result : result;
    }

    public void SubscribeToEvent(Action onConditionMet)
    {
        QuestManager.Instance?.QuestLineCompleted.AddListener(OnQuestLineStateChanged);
        QuestManager.Instance?.QuestLineStarted.AddListener(OnQuestLineStateChanged);
        // Store callback, check on state change
    }

    // ... rest of IConditionEventDriven implementation
}
```

---

## Relationship with Quest Chains

QuestLines and Quest Chains are **complementary, not conflicting**:

| Concept | Purpose | Mechanism |
|---------|---------|-----------|
| **Quest Chain** | Execution dependency | `ConditionQuestState_SO` in startConditions |
| **Quest Line** | Narrative grouping | `QuestLine_SO` containing multiple quests |

### Example: Thieves Guild Questline

```
QuestLine: "Thieves Guild"
├── Quest 1: "A Chance Arrangement" (no prerequisites)
├── Quest 2: "Taking Care of Business" (requires Quest 1 completed)
├── Quest 3: "Loud and Clear" (requires Quest 2 completed)
├── Quest 4: "Dampened Spirits" (requires Quest 3 completed)
└── Quest 5: "Under New Management" (requires Quest 4 completed)
```

- The **QuestLine_SO** groups all 5 quests
- Each quest has **ConditionQuestState_SO** pointing to its predecessor
- The QuestLine tracks overall progress (0/5, 1/5, 2/5...)
- Completion rewards trigger when all 5 are done

---

## UI Integration

### Quest Journal Structure

```
┌─────────────────────────────────────────────────┐
│ QUEST JOURNAL                                    │
├─────────────────────────────────────────────────┤
│ ▼ Main Story                    [3/5 Complete]  │
│   ├── [✓] Prologue                              │
│   ├── [✓] The Investigation                     │
│   ├── [→] The Conspiracy        ← In Progress   │
│   ├── [ ] The Confrontation                     │
│   └── [ ] Epilogue                              │
│                                                  │
│ ▼ Thieves Guild                 [2/5 Complete]  │
│   ├── [✓] A Chance Arrangement                  │
│   ├── [→] Taking Care of Business               │
│   └── ...                                        │
│                                                  │
│ ► Companions                    [0/6 Complete]  │
│ ► Misc Quests                   [4/12 Complete] │
└─────────────────────────────────────────────────┘
```

### UI_QuestLineList.cs

```csharp
public class UI_QuestLineList : MonoBehaviour
{
    [SerializeField] private Transform questLineContainer;
    [SerializeField] private UI_QuestLineItem questLineItemPrefab;

    private void OnEnable()
    {
        QuestManager.Instance.QuestLineUpdated.AddListener(RefreshUI);
        RefreshUI();
    }

    private void RefreshUI()
    {
        // Clear and rebuild questline list
        foreach (var line in QuestManager.Instance.GetActiveQuestLines())
        {
            var item = Instantiate(questLineItemPrefab, questLineContainer);
            item.Setup(line);
        }
    }
}
```

---

## Inspector UX (Odin)

### QuestLine_SO.Odin.cs

Following the Quest_SO pattern with tabs:

```csharp
#if ODIN_INSPECTOR
public partial class QuestLine_SO
{
    [TabGroup("Tabs", "Overview")]
    [OnInspectorGUI("DrawQuestLineOverview")]
    private string _overviewPlaceholder => "";

    [TabGroup("Tabs", "Quests")]
    [OnInspectorGUI("DrawQuestsSection")]
    private string _questsPlaceholder => "";

    [TabGroup("Tabs", "Validation")]
    [OnInspectorGUI("DrawValidationSection")]
    private string _validationPlaceholder => "";

    // Overview shows:
    // - Icon, name, description
    // - Progress bar (X/Y quests)
    // - Total rewards preview
    // - Prerequisite line (if any)

    // Quests section shows:
    // - Ordered list of quests
    // - Each quest shows: icon, name, chain status
    // - "Add Quest" button
    // - Drag to reorder

    // Validation checks:
    // - No duplicate quests
    // - No circular questline prerequisites
    // - All quests exist
    // - Localization configured
}
#endif
```

---

## Implementation Plan

### Phase 1: Core Data Model
1. Create `QuestLineState.cs` enum
2. Create `QuestLine_SO.cs` ScriptableObject
3. Create `QuestLineRuntime.cs` runtime class

### Phase 2: QuestManager Integration
1. Add questline fields to QuestManager
2. Add questline events
3. Add questline lifecycle methods
4. Hook into QuestCompleted to update lines

### Phase 3: Condition System
1. Create `ConditionQuestLineState_SO.cs`
2. Add to Create menu

### Phase 4: Inspector UX
1. Create `QuestLine_SO.Odin.cs` partial class
2. Build Overview, Quests, Validation tabs
3. Add Quick Actions (Add Quest, Reorder)

### Phase 5: UI Integration
1. Create `UI_QuestLineItem.cs`
2. Create `UI_QuestLineList.cs`
3. Integrate with existing quest journal

### Phase 6: Documentation
1. Update README.md
2. Update CLAUDE.md
3. Create example questlines

---

## File Changes Summary

### New Files
| File | Purpose |
|------|---------|
| `Runtime/Scripts/Core/QuestLines/QuestLineState.cs` | State enum |
| `Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs` | Runtime class |
| `Runtime/Scripts/Core/ScriptableObjects/QuestLine_SO.cs` | Data container |
| `Runtime/Scripts/Core/ScriptableObjects/QuestLine_SO.Odin.cs` | Inspector UI |
| `Runtime/Scripts/Core/Conditions/ConditionQuestLineState_SO.cs` | Condition |
| `BasicQuestExample/Scripts/UI/QuestLines/UI_QuestLineItem.cs` | UI component |
| `BasicQuestExample/Scripts/UI/QuestLines/UI_QuestLineList.cs` | UI list |

### Modified Files
| File | Changes |
|------|---------|
| `QuestManager.cs` | Add questline tracking, events, methods |
| `package.json` | Version bump to 1.8.0 |
| `README.md` | Document QuestLine feature |

---

## Open Questions

1. **Should QuestLines auto-start their first quest?**
   - Option A: Auto-add first quest when line becomes available
   - Option B: Manual: designer controls via startConditions on first quest
   - **Recommendation:** Option B (more flexible, matches existing pattern)

2. **Can a quest belong to multiple questlines?**
   - Skyrim: No (each quest belongs to one line)
   - WoW: Yes (quest can contribute to multiple achievements/campaigns)
   - **Recommendation:** Allow it, but warn in validation

3. **What happens if a quest in the line is failed?**
   - Option A: QuestLine can't complete (strict)
   - Option B: Failed quests count as "done" for progress
   - Option C: QuestLine has its own Failed state
   - **Recommendation:** Option C with configurable behavior

---

## Version

This document describes **QuestLine feature for com.hellodev.questsystem v1.8.0**.
