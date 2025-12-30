# HelloDev Quest System

A data-driven quest system for Unity games. Designers assemble quests from reusable components; programmers extend the system with new task types and conditions.

## Features

### Core System
- **QuestManager** - Singleton managing quest and questline lifecycle, events, and state
- **QuestRuntime / Quest_SO** - Runtime quest instances with ScriptableObject data
- **QuestLineRuntime / QuestLine_SO** - Narrative grouping of related quests (story arcs)
- **TaskRuntime / Task_SO** - Abstract task system with typed implementations

### Task Types
- **IntTask / TaskInt_SO** - Counter-based tasks (collect 5 items, kill 10 enemies)
- **BoolTask / TaskBool_SO** - Boolean tasks (toggle a switch, trigger an event)
- **StringTask / TaskString_SO** - String matching tasks (enter password, find code)
- **LocationTask / TaskLocation_SO** - Location-based tasks (reach a waypoint)
- **TimedTask / TaskTimed_SO** - Timer-based tasks with countdown
- **DiscoveryTask / TaskDiscovery_SO** - Discovery tasks (find hidden items)

### Branching & Player Choices
- **Stage-based branching** - Quests can branch based on player choices or conditions
- **PlayerChoice transitions** - Present choices to players (UI, dialogue, physical actions, etc.)
- **Implicit choices** - Choices can be made through actions (buying items, entering areas, etc.)
- **Choice tracking** - All branch decisions are recorded for save/load and analytics
- **Event-driven** - OnChoicesAvailable, OnChoiceMade, OnChoiceAvailabilityChanged

### World State Flags
- **WorldFlagBool_SO** - Boolean flags for binary state (met_king, chose_evil_path)
- **WorldFlagInt_SO** - Integer flags for numeric state (reputation, kill_count)
- **ConditionWorldFlagBool_SO** - Check boolean flags in conditions
- **ConditionWorldFlagInt_SO** - Check integer flags with comparisons (>=, <, ==, etc.)

### Conditions
- Start conditions - Control when quests become available
- Failure conditions - Quest-level failure triggers
- Global task failure conditions - Fail current task when met
- Task conditions - Task completion triggers via event-driven conditions
- **ConditionQuestState_SO** - Quest chains (Quest B requires Quest A completed)
- **ConditionQuestLineState_SO** - QuestLine prerequisites (unlock content after completing a questline)
- **ConditionWorldFlagBool_SO** - Check boolean world state flags
- **ConditionWorldFlagInt_SO** - Check integer world state flags

### Quest Chains
- **Sequential chains** - Quest B starts only after Quest A completes
- **Branching paths** - Quest C requires Quest A OR Quest B (CompositeCondition with OR)
- **Locked content** - Quest D requires Quest A AND Quest B (CompositeCondition with AND)
- **Exclusive paths** - Quest E only available if Quest A NOT failed (IsInverted)

### QuestLines (Story Arcs)
A **QuestLine** is a narrative grouping of related quests that together tell a complete story. Unlike quest chains (execution dependencies), a QuestLine is a thematic container.

**AAA Examples:**
- Skyrim: "Companions Questline", "Thieves Guild Questline"
- Witcher 3: Story "threads" within narrative phases
- Cyberpunk 2077: Character arcs (Panam's arc, Judy's arc)

**Features:**
- Groups quests belonging to the same storyline
- Tracks overall progress across all contained quests (0-100%)
- Completion rewards when all quests in the line are done
- Optional prerequisite questlines (unlock Questline B after completing Questline A)
- Configurable failure behavior (fail line if any quest fails)
- Works alongside quest chains (not replacing them)

### Rewards
- **QuestRewardType_SO** - Abstract base for custom reward types
- **RewardInstance** - Pairs reward type with amount
- Auto-distribution on quest completion

### Events
All major state changes fire events for UI and game integration:
- QuestManager: `QuestAdded`, `QuestStarted`, `QuestCompleted`, `QuestFailed`, `QuestUpdated`
- QuestManager (QuestLines): `QuestLineAdded`, `QuestLineStarted`, `QuestLineCompleted`, `QuestLineFailed`, `QuestLineUpdated`
- Quest: `OnQuestUpdated`, `OnAnyTaskUpdated`, `OnAnyTaskCompleted`, `OnAnyTaskFailed`
- QuestLine: `OnQuestLineStarted`, `OnQuestLineCompleted`, `OnQuestLineUpdated`, `OnQuestInLineCompleted`
- Task: `OnTaskUpdated`, `OnTaskCompleted`, `OnTaskFailed`

### Event-Driven Conditions (Generic Event Pattern)

**CRITICAL: Events should be GENERIC, conditions should be SPECIFIC.**

The quest system uses a pattern where:
- **Events** are generic and reusable (e.g., `OnMonsterKilled`, `OnNPCDialogue`, `OnLocationAttacked`)
- **Conditions** hold the specific expected values (e.g., which monster ID, which NPC ID, which location ID)

**Why this pattern?**
- Reduces event proliferation - One `OnMonsterKilled` event instead of `OnGoblinKilled`, `OnOrcKilled`, `OnSkeletonKilled`
- Enables designer flexibility - Create new monster/NPC/location types without code changes
- Simplifies game code integration - Raise the same event with different IDs

**Correct approach:**
```
Event: OnMonsterKilled (GameEventID_SO) - generic, reusable
Condition: SO_Condition_Event_ID_GoblinKilled - references OnMonsterKilled + GoblinId
Condition: SO_Condition_Event_ID_OrcKilled - references OnMonsterKilled + OrcId
```

**Wrong approach:**
```
Event: OnGoblinKilled (GameEventBool_SO) - specific, not reusable
Event: OnOrcKilled (GameEventBool_SO) - specific, not reusable
```

**Standard generic events:**
| Event | Type | Purpose |
|-------|------|---------|
| `OnMonsterKilled` | `GameEventID_SO` | Any monster killed |
| `OnNPCDialogue` | `GameEventID_SO` | Dialogue with specific NPC |
| `OnNPCKilled` | `GameEventID_SO` | Any NPC killed |
| `OnEnemyAlert` | `GameEventID_SO` | Enemy spots player (stealth) |
| `OnLocationAttacked` | `GameEventID_SO` | Location under attack |
| `OnItemCollected` | `GameEventID_SO` | Item collected/recovered |
| `OnItemDestroyed` | `GameEventID_SO` | Item destroyed |
| `OnFindLocation` | `GameEventID_SO` | Player reaches location |
| `OnItemDiscovered` | `GameEventID_SO` | Item/clue discovered |

**Game Code Integration:**
```csharp
// Monster kill handler - uses generic event
public class MonsterKillHandler : MonoBehaviour
{
    [SerializeField] private ID_SO monsterId;  // Goblin, Orc, etc.
    [SerializeField] private GameEventID_SO onMonsterKilled;

    public void OnDeath()
    {
        onMonsterKilled.Raise(monsterId);  // Same event, different ID
    }
}
```

See `BasicQuestExample/Docs/EventIntegrationGuide.md` for complete integration documentation.

## Installation

### Via Package Manager (Local)
1. Open Unity Package Manager
2. Click "+" > "Add package from disk"
3. Navigate to this folder and select `package.json`

## Usage

### Creating a Quest (Designer)

1. Create > HelloDev > Quest System > Scriptable Objects > Quest
2. Set DevName and localized DisplayName/Description
3. Add Task_SO references to the tasks list
4. (Optional) Configure start/failure conditions
5. (Optional) Add rewards

### Creating Tasks

```
Create > HelloDev > Quest System > Scriptable Objects > Tasks > Int Task
Create > HelloDev > Quest System > Scriptable Objects > Tasks > Bool Task
Create > HelloDev > Quest System > Scriptable Objects > Tasks > String Task
```

### Using in Code

```csharp
using HelloDev.QuestSystem;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;

// Subscribe to quest events
QuestManager.Instance.QuestCompleted.AddListener(OnQuestCompleted);

// Get active quests
var activeQuests = QuestManager.Instance.GetActiveQuests();

// Get a specific quest and work with it
var quest = QuestManager.Instance.GetActiveQuest(questSO);
quest?.IncrementCurrentTask();  // Progress current task
quest?.CurrentTask?.IncrementStep();  // Same effect, more explicit

// Access quest properties
var tasks = quest.Tasks;
var currentTask = quest.CurrentTask;
var progress = quest.CurrentProgress;
```

### Creating Custom Task Types

```csharp
// 1. Create the runtime task
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.ScriptableObjects;

public class TimedTaskRuntime : TaskRuntime
{
    private readonly TaskTimed_SO _timedData;
    public float TimeRemaining { get; private set; }

    public TimedTaskRuntime(TaskTimed_SO data) : base(data)
    {
        _timedData = data;
        TimeRemaining = data.Duration;
    }

    public override float Progress => 1f - (TimeRemaining / _timedData.Duration);

    protected override void CheckCompletion(TaskRuntime task)
    {
        if (TimeRemaining <= 0) CompleteTask();
    }

    public override void ForceCompleteState() => TimeRemaining = 0;
    public override bool OnIncrementStep() { CompleteTask(); return true; }
    public override bool OnDecrementStep() { return false; }
}

// 2. Create the ScriptableObject data
[CreateAssetMenu(menuName = "HelloDev/Quest System/Tasks/Timed Task")]
public class TaskTimed_SO : Task_SO
{
    [SerializeField] private float duration = 60f;
    public float Duration => duration;

    public override TaskRuntime GetRuntimeTask() => new TimedTaskRuntime(this);

    public override void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, TaskRuntime task)
    {
        // Configure localization variables
        taskNameText.RefreshString();
    }
}
```

### Creating Custom Reward Types

```csharp
using HelloDev.QuestSystem.ScriptableObjects;

[CreateAssetMenu(menuName = "HelloDev/Quest System/Rewards/Gold Reward")]
public class GoldRewardType_SO : QuestRewardType_SO
{
    public override void GiveReward(int amount)
    {
        // Add gold to player inventory
        PlayerInventory.Instance.AddGold(amount);
        Debug.Log($"Received {amount} gold!");
    }
}
```

### Creating Quest Chains

Quest chains allow you to create prerequisites between quests. Use `ConditionQuestState_SO` to check if a quest is in a specific state.

**Sequential Chain (Quest B requires Quest A completed):**
1. Create > HelloDev > Quest System > Conditions > Quest State Condition
2. Set "Quest To Check" to Quest A
3. Set "Target State" to `Completed`
4. Set "Comparison Type" to `Equals`
5. Add this condition to Quest B's Start Conditions

**Branching Paths (Quest C requires Quest A OR Quest B):**
1. Create two `ConditionQuestState_SO` assets (one for each prerequisite quest)
2. Create a `CompositeCondition_SO` with `LogicType = OR`
3. Add both quest state conditions to the composite
4. Add the composite to Quest C's Start Conditions

**Locked Content (Quest D requires Quest A AND Quest B):**
1. Create two `ConditionQuestState_SO` assets
2. Create a `CompositeCondition_SO` with `LogicType = AND`
3. Add both conditions to the composite
4. Add the composite to Quest D's Start Conditions

**Exclusive Paths (Quest E only if Quest A NOT failed):**
1. Create a `ConditionQuestState_SO` for Quest A
2. Set "Target State" to `Failed`
3. Set "Comparison Type" to `Equals`
4. Enable "Is Inverted" on the condition
5. Add to Quest E's Start Conditions

**Available States to Check:**
- `NotStarted` - Quest has never been added
- `InProgress` - Quest is currently active
- `Completed` - Quest was completed successfully
- `Failed` - Quest was failed

```csharp
// Programmatic check
var questStateCondition = ScriptableObject.CreateInstance<ConditionQuestState_SO>();
// Configure via inspector, or check programmatically:
if (QuestManager.Instance.IsQuestCompleted(prerequisiteQuest))
{
    QuestManager.Instance.AddQuest(nextQuest, forceStart: true);
}
```

### Creating Branching Quests (Player Choices)

Branching quests allow players to make meaningful choices that affect quest progression and the game world.

**Stage-Based Quest Structure:**
1. Create > HelloDev > Quest System > Scriptable Objects > Quest
2. Enable stage-based structure in the quest inspector
3. Add stages with unique indices (0, 1, 10, 20, etc.)
4. Configure transitions between stages

**Player Choice Transitions:**
1. In a stage's transitions, set `Trigger` to `PlayerChoice`
2. Enable `IsPlayerChoice` flag
3. Set a unique `ChoiceId` (e.g., "combat_path", "diplomacy_path")
4. Add optional conditions to gate certain choices
5. Add `WorldFlagsOnSelect` to set world flags when the choice is made

**Conditional Choices (e.g., Reputation Gates):**
1. Create a `WorldFlagInt_SO` for the reputation (e.g., GuardReputation)
2. Create a `ConditionWorldFlagInt_SO` that checks reputation >= 20
3. Add the condition to the choice's transition conditions
4. The choice only appears if the condition is met

**World Consequences:**
Each choice can modify world flags when selected:
- Bool flags: Set true/false (e.g., "ChoseCombatPath = true")
- Int flags: Set, Add, or Subtract values (e.g., "Reputation += 10")

**Implicit Choices (Action-Based):**
Choices can be made through player actions instead of explicit menus:
1. Create a condition that triggers on player action (e.g., buying an item)
2. Add the condition to a choice transition
3. When the player performs the action, the choice auto-selects

**Example: The Merchant's Dilemma (see BasicQuestExample)**
```
Stage 0: Talk to Merchant
    → auto-transition to Stage 1

Stage 1: The Choice (presents 3 options)
    → [Combat] Confront Bandits → Stage 10 (sets ChoseCombat flag)
    → [Diplomacy] Negotiate → Stage 20 (sets ChoseDiplomacy flag)
    → [Lawful] Report to Guards → Stage 30 (requires Guard Rep >= 20, sets ChoseLawful flag)

Stage 10: Combat Path (defeat bandits) → Stage 100
Stage 20: Diplomacy Path (negotiate) → Stage 100
Stage 30: Lawful Path (report to guards) → Stage 100

Stage 100: Return to Merchant (resolution, terminal)
```

**Using Branching in Code:**
```csharp
// Get available choices at current stage
var choices = quest.GetAvailableChoices();
foreach (var choice in choices)
{
    Debug.Log($"Choice: {choice.TransitionLabel} (ID: {choice.ChoiceId})");
    if (!choice.AreConditionsMet())
        Debug.Log("  [Locked - conditions not met]");
}

// Select a choice
quest.SelectChoiceById("combat_path");

// Subscribe to choice events
quest.OnChoicesAvailable += (q, choices) => ShowChoiceUI(choices);
quest.OnChoiceMade += (q, choice) => Debug.Log($"Player chose: {choice.ChoiceId}");
```

### AAA Integration Patterns

The branching and world state systems are designed to support AAA-style quest patterns found in games like Skyrim, Witcher 3, Mass Effect, and Cyberpunk 2077.

**Pattern 1: Cross-Quest Consequences (Witcher 3 Style)**
Choices in one quest affect other quests:
```
Quest A: Choose to save or sacrifice the village
  → Sets WorldFlag: VillageSaved = true/false

Quest B: (starts later)
  → Start condition: ConditionWorldFlagBool_SO checks VillageSaved
  → If saved: Village welcomes you, new merchants available
  → If sacrificed: Village is ruins, hostile NPCs
```

**Pattern 2: Reputation Gates (Skyrim Style)**
Faction standing unlocks dialogue and quest options:
```
WorldFlagInt: GuardReputation (0-100)
Quest: The Merchant's Dilemma
  → "Report to Guards" choice requires GuardReputation >= 20
  → Choosing this path grants +10 reputation
```

**Pattern 3: Branching Narrative Paths (Mass Effect Style)**
Major story decisions tracked across the entire game:
```
WorldFlagBool: ChoseParagonPath, ChoseRenegadePath
WorldFlagInt: ParagonScore, RenegadeScore

Quest choices add to scores and set flags
Future quests check flags for dialogue variations
Ending quests check cumulative scores
```

**Pattern 4: Implicit Choices (Cyberpunk 2077 Style)**
Player actions make choices without explicit menus:
```
Stage 1: Approach the deal
  → [Buy drugs] triggered by purchasing illegal items
  → [Call police] triggered by phone call action
  → [Attack] triggered by combat initiation

Game systems raise events, quest conditions detect and auto-select choices
```

**Pattern 5: Dynamic Availability (Living World)**
World flags control which content is available:
```
WorldFlagBool: DragonDefeated
WorldFlagInt: GuildRank

Quest A: Only available if DragonDefeated = true
Quest B: Only available if GuildRank >= 5
Quest C: Only available if both conditions met
```

**Combining Patterns:**
All patterns work together - a single quest can:
- Check world flags to gate availability (Pattern 5)
- Present reputation-gated choices (Pattern 2)
- Set world flags that affect other quests (Pattern 1)
- Track choices for narrative endings (Pattern 3)
- Respond to player actions (Pattern 4)

### Save/Load System

The quest system includes a flexible save/load system that allows you to persist quest progress. The system uses an interface-based design so you can integrate with any save system (JSON files, cloud saves, Easy Save 3, etc.).

**Quick Start:**
```csharp
using HelloDev.QuestSystem.SaveLoad;

// Setup (typically in game initialization)
QuestSaveManager.Instance.SetProvider(new JsonFileSaveProvider());

// Save quest progress
await QuestSaveManager.Instance.SaveAsync("save_slot_1");

// Load quest progress
await QuestSaveManager.Instance.LoadAsync("save_slot_1");

// Check if save exists
bool exists = await QuestSaveManager.Instance.SaveExistsAsync("save_slot_1");

// Delete a save
await QuestSaveManager.Instance.DeleteSaveAsync("save_slot_1");

// List all saves
string[] slots = await QuestSaveManager.Instance.GetAllSaveSlotsAsync();
```

**Per-Slot Autosave:**

The save system supports slot-based autosave, where each save slot gets its own autosave file. When playing on slot 1, autosaves go to "autosave-1"; when playing on slot 2, autosaves go to "autosave-2".

1. Create > HelloDev > Services > Save Slot Service
2. Assign to SaveSystemSetup's `Slot Service` field
3. Set `Default Slot Index` (0 = first slot)

```csharp
// Programmatic slot management
slotService.SetActiveSlot(0);  // Autosaves now go to "autosave-0"
slotService.SetActiveSlot(2);  // Autosaves now go to "autosave-2"

// Get slot keys
string autoKey = slotService.CurrentAutosaveSlotKey;  // "autosave-2"
string saveKey = slotService.CurrentManualSlotKey;    // "save-2"

// When loading a different save
await saveService.LoadAsync("save-1");
slotService.SetActiveSlot(1);  // Future autosaves go to "autosave-1"
```

**SaveSlotService_SO Properties:**
| Property | Description |
|----------|-------------|
| `MaxSlots` | Maximum number of save slots (configurable) |
| `CurrentSlotIndex` | Currently active slot index (-1 if none) |
| `HasActiveSlot` | True if a slot is active |
| `CurrentAutosaveSlotKey` | Returns "autosave-X" for current slot |
| `CurrentManualSlotKey` | Returns "save-X" for current slot |

**Custom Save Provider:**
Implement `ISaveDataProvider` to integrate with your preferred save system:

```csharp
using HelloDev.QuestSystem.SaveLoad;

// Example: Easy Save 3 integration
public class ES3SaveProvider : ISaveDataProvider
{
    public Task<bool> SaveAsync(string slotKey, QuestSystemSnapshot snapshot)
    {
        ES3.Save(slotKey, snapshot);
        return Task.FromResult(true);
    }

    public Task<QuestSystemSnapshot> LoadAsync(string slotKey)
    {
        if (!ES3.KeyExists(slotKey)) return Task.FromResult<QuestSystemSnapshot>(null);
        return Task.FromResult(ES3.Load<QuestSystemSnapshot>(slotKey));
    }

    public Task<bool> ExistsAsync(string slotKey) => Task.FromResult(ES3.KeyExists(slotKey));
    public Task<bool> DeleteAsync(string slotKey) { ES3.DeleteKey(slotKey); return Task.FromResult(true); }
    public Task<SaveSlotMetadata> GetMetadataAsync(string slotKey) => Task.FromResult<SaveSlotMetadata>(null);
    public Task<string[]> GetAllSlotsAsync() => Task.FromResult(Array.Empty<string>());
}

// Use custom provider
QuestSaveManager.Instance.SetProvider(new ES3SaveProvider());
```

**What Gets Saved:**
- All quest states (active, completed, failed)
- Current stage and task progress
- Branch decisions (which choices were made)
- World flag values (boolean and integer)
- QuestLine progress

**World Flags:**
For world flags to be saved, register them with the save manager:
```csharp
// Option 1: Add to QuestSaveManager's worldFlagRegistry in the inspector

// Option 2: Register programmatically
QuestSaveManager.Instance.RegisterWorldFlag(myWorldFlag);
```

**Save Events:**
```csharp
QuestSaveManager.Instance.OnBeforeSave.AddListener(slotKey => Debug.Log($"Saving to {slotKey}..."));
QuestSaveManager.Instance.OnAfterSave.AddListener((slotKey, success) => Debug.Log($"Save {(success ? "succeeded" : "failed")}"));
QuestSaveManager.Instance.OnBeforeLoad.AddListener(slotKey => Debug.Log($"Loading from {slotKey}..."));
QuestSaveManager.Instance.OnAfterLoad.AddListener((slotKey, success) => Debug.Log($"Load {(success ? "succeeded" : "failed")}"));
```

**Manual Snapshots:**
For custom implementations, you can capture/restore snapshots directly:
```csharp
// Capture current state
QuestSystemSnapshot snapshot = QuestSaveManager.Instance.CaptureSnapshot();

// Serialize to JSON
string json = JsonUtility.ToJson(snapshot);

// Later: deserialize and restore
var loaded = JsonUtility.FromJson<QuestSystemSnapshot>(json);
QuestSaveManager.Instance.RestoreSnapshot(loaded);
```

### Creating QuestLines

QuestLines group related quests into narrative arcs. They work alongside quest chains (execution dependencies).

**Creating a QuestLine:**
1. Create > HelloDev > Quest System > Scriptable Objects > Quest Line
2. Set DevName and localized DisplayName/Description
3. Add Quest_SO references to the quests list (order matters for UI)
4. (Optional) Set a Prerequisite Line (another questline that must complete first)
5. (Optional) Add completion rewards

**Adding QuestLine Prerequisites:**
Use `ConditionQuestLineState_SO` to unlock content after completing a questline:
1. Create > HelloDev > Quest System > Conditions > Quest Line State Condition
2. Set "QuestLine To Check" to the prerequisite questline
3. Set "Target State" to `Completed`
4. Add this condition to a quest's Start Conditions

**Using QuestLines in Code:**
```csharp
using HelloDev.QuestSystem;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.ScriptableObjects;

// Add a questline to tracking
QuestManager.Instance.AddQuestLine(questLineSO);

// Subscribe to questline events
QuestManager.Instance.QuestLineCompleted.AddListener(OnQuestLineCompleted);

// Get active questlines
var activeLines = QuestManager.Instance.GetActiveQuestLines();

// Check progress
var line = QuestManager.Instance.GetQuestLine(questLineSO);
float progress = line.Progress;  // 0.0 to 1.0
int completed = line.CompletedQuestCount;
int total = line.TotalQuestCount;

// Check state
bool isComplete = QuestManager.Instance.IsQuestLineCompleted(questLineSO);
bool isActive = QuestManager.Instance.IsQuestLineActive(questLineSO);
```

**QuestLine vs Quest Chain:**
| Concept | Purpose | Mechanism |
|---------|---------|-----------|
| Quest Chain | Execution dependency | `ConditionQuestState_SO` in startConditions |
| QuestLine | Narrative grouping | `QuestLine_SO` containing multiple quests |

Both can be used together: a QuestLine can contain quests that have chain dependencies.

## API Reference

### QuestManager

The QuestManager is the entry point for all quest operations. It manages quest lifecycle and provides events for game integration.

#### Configuration Properties
| Property | Description |
|----------|-------------|
| `Instance` | Singleton instance |
| `QuestsDatabase` | Read-only access to the quest database |
| `QuestLinesDatabase` | Read-only access to the questline database |
| `ActiveQuestCount` | Number of currently active quests |
| `CompletedQuestCount` | Number of completed quests |
| `FailedQuestCount` | Number of failed quests |
| `ActiveQuestLineCount` | Number of currently active questlines |
| `CompletedQuestLineCount` | Number of completed questlines |
| `AllowMultipleActiveQuests` | Whether multiple quests can be active |
| `AllowReplayingCompletedQuests` | Whether completed quests can be replayed |
| `RequireQuestInDatabase` | Whether quests must be in database to be added |

#### Quest Lifecycle Methods
| Method | Description |
|--------|-------------|
| `AddQuest(Quest_SO, forceStart)` | Add and optionally start a quest |
| `FailQuest(Quest_SO)` | Fail a quest |
| `RemoveQuest(Quest_SO)` | Remove a quest from active quests |
| `RestartQuest(Quest_SO, forceStart)` | Restart a quest |

#### Query Methods
| Method | Description |
|--------|-------------|
| `GetActiveQuest(Quest_SO)` | Get active quest runtime instance |
| `GetActiveQuests()` | Get all active quests (read-only) |
| `GetCompletedQuests()` | Get all completed quests (read-only) |
| `GetFailedQuests()` | Get all failed quests (read-only) |
| `IsQuestActive(Quest_SO)` | Check if quest is active |
| `IsQuestCompleted(Quest_SO)` | Check if quest is completed |
| `IsQuestFailed(Quest_SO)` | Check if quest has failed |

#### QuestLine Lifecycle Methods
| Method | Description |
|--------|-------------|
| `AddQuestLine(QuestLine_SO)` | Add a questline to tracking |
| `GetQuestLine(QuestLine_SO)` | Get active or completed questline |
| `GetActiveQuestLines()` | Get all active questlines (read-only) |
| `GetCompletedQuestLines()` | Get all completed questlines (read-only) |
| `IsQuestLineActive(QuestLine_SO)` | Check if questline is active |
| `IsQuestLineCompleted(QuestLine_SO)` | Check if questline is completed |

#### Events
| Event | Description |
|-------|-------------|
| `QuestAdded` | Fired when a quest is added |
| `QuestStarted` | Fired when a quest starts |
| `QuestCompleted` | Fired when a quest completes |
| `QuestFailed` | Fired when a quest fails |
| `QuestUpdated` | Fired when quest progress changes |
| `QuestRemoved` | Fired when a quest is removed |
| `QuestRestarted` | Fired when a quest is restarted |
| `QuestLineAdded` | Fired when a questline is added |
| `QuestLineStarted` | Fired when a questline starts |
| `QuestLineCompleted` | Fired when a questline completes |
| `QuestLineFailed` | Fired when a questline fails |
| `QuestLineUpdated` | Fired when questline progress changes |

### QuestRuntime

The runtime representation of a quest. Access via `QuestManager.GetActiveQuest(questSO)`.

#### Properties
| Member | Description |
|--------|-------------|
| `QuestId` | Unique GUID |
| `QuestData` | Reference to Quest_SO |
| `CurrentState` | NotStarted, InProgress, Completed, Failed |
| `CurrentProgress` | 0-1 completion percentage |
| `Tasks` | List of all runtime tasks |
| `CurrentTask` | First in-progress task (null if none) |
| `CurrentTasks` | All in-progress tasks (for parallel groups) |
| `TaskGroups` | List of task groups |
| `CurrentGroup` | Currently active task group |

#### Methods
| Method | Description |
|--------|-------------|
| `StartQuest()` | Begin the quest |
| `CompleteQuest()` | Complete and distribute rewards |
| `FailQuest()` | Mark as failed |
| `ResetQuest()` | Reset and restart |
| `IncrementCurrentTask()` | Progress current task |
| `DecrementCurrentTask()` | Regress current task |
| `ForceComplete()` | Complete all remaining tasks |

#### Events
| Event | Description |
|-------|-------------|
| `OnQuestStarted` | Quest started |
| `OnQuestCompleted` | Quest completed |
| `OnQuestFailed` | Quest failed |
| `OnQuestRestarted` | Quest restarted |
| `OnQuestUpdated` | Progress changed |
| `OnAnyTaskStarted` | Any task started |
| `OnAnyTaskUpdated` | Any task updated |
| `OnAnyTaskCompleted` | Any task completed |
| `OnAnyTaskFailed` | Any task failed |

### TaskRuntime

The runtime representation of a task.

#### Properties
| Member | Description |
|--------|-------------|
| `TaskId` | Unique GUID |
| `Data` | Reference to Task_SO |
| `CurrentState` | NotStarted, InProgress, Completed, Failed |
| `Progress` | 0-1 completion percentage |

#### Methods
| Method | Description |
|--------|-------------|
| `StartTask()` | Begin the task |
| `IncrementStep()` | Progress the task |
| `DecrementStep()` | Regress the task |
| `CompleteTask()` | Force complete |
| `FailTask()` | Mark as failed |
| `ResetTask()` | Reset to initial state |

### ConditionQuestState_SO

An event-driven condition for creating quest chains. Checks if a quest is in a specific state.

#### Properties
| Property | Description |
|----------|-------------|
| `QuestToCheck` | The quest whose state will be checked |
| `TargetState` | The state to compare against (NotStarted, InProgress, Completed, Failed) |
| `ComparisonType` | How to compare: Equals or NotEquals |
| `IsInverted` | Inherited from Condition_SO - inverts the result |

#### Methods
| Method | Description |
|--------|-------------|
| `Evaluate()` | Returns true if condition is met |
| `SubscribeToEvent(Action)` | Subscribe to quest state changes |
| `UnsubscribeFromEvent()` | Unsubscribe from events |
| `ForceFulfillCondition()` | Debug: Force-trigger the callback |

#### Usage Example
```csharp
// The condition automatically subscribes to QuestManager events:
// - QuestStarted
// - QuestCompleted
// - QuestFailed
// - QuestRestarted
// - QuestAdded

// When the tracked quest changes state, the condition re-evaluates
// and fires the callback if the condition becomes true.
```

### QuestLineRuntime

The runtime representation of a questline. Access via `QuestManager.GetQuestLine(questLineSO)`.

#### Properties
| Member | Description |
|--------|-------------|
| `QuestLineId` | Unique GUID |
| `Data` | Reference to QuestLine_SO |
| `CurrentState` | Locked, Available, InProgress, Completed, Failed |
| `Progress` | 0-1 completion percentage |
| `CompletedQuestCount` | Number of completed quests in the line |
| `TotalQuestCount` | Total number of quests in the line |
| `IsComplete` | True if all quests are completed |
| `IsAvailable` | True if questline can be started |
| `IsInProgress` | True if at least one quest has started |
| `IsFailed` | True if questline has failed |
| `NextQuest` | Next incomplete quest in the line |
| `FirstQuest` | First quest in the line |

#### Events
| Event | Description |
|-------|-------------|
| `OnQuestLineStarted` | QuestLine started |
| `OnQuestLineCompleted` | QuestLine completed |
| `OnQuestLineUpdated` | Progress changed |
| `OnQuestLineFailed` | QuestLine failed |
| `OnQuestInLineCompleted` | A quest in the line completed |

### ConditionQuestLineState_SO

An event-driven condition for questline prerequisites. Checks if a questline is in a specific state.

#### Properties
| Property | Description |
|----------|-------------|
| `QuestLineToCheck` | The questline whose state will be checked |
| `TargetState` | The state to compare against (Locked, Available, InProgress, Completed, Failed) |
| `ComparisonType` | How to compare: Equals or NotEquals |
| `IsInverted` | Inherited from Condition_SO - inverts the result |

#### Methods
| Method | Description |
|--------|-------------|
| `Evaluate()` | Returns true if condition is met |
| `SubscribeToEvent(Action)` | Subscribe to questline state changes |
| `UnsubscribeFromEvent()` | Unsubscribe from events |
| `ForceFulfillCondition()` | Debug: Force-trigger the callback |

## Dependencies

### Required
- com.hellodev.utils (1.1.0+)
- com.hellodev.events (1.1.0+)
- com.hellodev.conditions (1.1.0+)
- com.hellodev.ids (1.1.0+)
- com.unity.localization

### Optional
- Odin Inspector (for enhanced inspectors)

## Changelog

### v3.2.0 (2025-12-29)
**Per-Slot Autosave:**
- Added `SaveSlotService_SO` for managing save slot selection
- Each save slot now gets its own autosave file (autosave-0, autosave-1, etc.)
- Configurable `maxSlots`, `manualSavePrefix`, and `autosavePrefix`
- `SaveSystemSetup` now supports optional slot service reference
- Added `defaultSlotIndex` to auto-activate a slot on start
- Slot changes fire `OnSlotChanged` event

**Bug Fixes:**
- Fixed task UI not updating after `RestoreProgress` (int tasks showed 0/2 instead of restored value)
- Fixed `OnDecrementStep` not notifying UI of changes
- Fixed `OnIncrementStep` not notifying UI when called directly (not via event conditions)

### v3.1.0 (2025-12-29)
**Save/Load System:**
- Added `QuestSaveManager` singleton for managing save/load operations
- Added `ISaveDataProvider` interface for custom save system integration
- Added `JsonFileSaveProvider` default implementation using JSON files
- Added `QuestSystemSnapshot` for capturing complete system state
- Captures quest states, task progress, stage positions, branch decisions
- Captures QuestLine progress
- Captures WorldFlag values (boolean and integer)
- Async API for non-blocking save/load operations
- Events: `OnBeforeSave`, `OnAfterSave`, `OnBeforeLoad`, `OnAfterLoad`
- Save slot metadata for UI display (timestamp, quest counts)

**New Classes:**
- `QuestSaveManager` - Main save/load manager
- `ISaveDataProvider` - Interface for custom save systems
- `JsonFileSaveProvider` - Default JSON file implementation
- `QuestSystemSnapshot` - Complete state snapshot
- `QuestSnapshot` - Individual quest state
- `TaskSnapshot` - Individual task state
- `QuestLineSnapshot` - QuestLine state
- `WorldFlagSnapshot` - World flag state
- `SaveSlotMetadata` - Save slot info for UI

**TaskRuntime Enhancements:**
- Added `BoolTaskRuntime.IsCompleted` property
- Added `DiscoveryTaskRuntime.FulfilledConditions` property

### v3.0.0 (2025-12-28)
**Stage-Based Branching Quests:**
- Added `QuestStage` for organizing quests into discrete stages
- Added `StageTransition` for defining how stages connect
- Added `TransitionTrigger` enum: OnComplete, OnFail, Conditional, PlayerChoice
- Stages support multiple task groups with parallel/sequential execution
- Non-sequential stage indices (0, 1, 10, 20, 100) for flexible quest design

**Player Choice System:**
- Added `isPlayerChoice` flag to mark transitions as explicit choices
- Added `choiceId` for unique choice identification
- Added `choiceText`, `choiceIcon`, `choiceTooltip` for UI presentation
- Choices can be gated by conditions (e.g., reputation requirements)
- Added `QuestRuntime.GetAvailableChoices()` - get valid choices at current stage
- Added `QuestRuntime.SelectChoice()` / `SelectChoiceById()` - make a choice
- Added `QuestRuntime.OnChoicesAvailable` event - fired when stage has choices
- Added `QuestRuntime.OnChoiceMade` event - fired when player selects choice
- Added `QuestRuntime.BranchDecisions` dictionary - tracks all choices made

**World State Flags:**
- Added `WorldFlagBool_SO` - boolean state flags (met_king, chose_evil_path)
- Added `WorldFlagInt_SO` - integer state flags (reputation, kill_count)
- Added `ConditionWorldFlagBool_SO` - check boolean flags in conditions
- Added `ConditionWorldFlagInt_SO` - check integer flags with comparisons
- Added `WorldFlagModification` - defines how to modify flags (set, add, subtract)
- Added `worldFlagsOnSelect` to `StageTransition` - apply modifications on choice

**Cross-Quest Consequences:**
- Choices in one quest can set world flags
- Other quests can check world flags in start/fail conditions
- Enables Witcher 3-style persistent world consequences

**Implicit Choices:**
- Choices can use conditions that trigger on player actions
- When condition is met, choice auto-selects (no UI needed)
- Supports action-driven narrative (buying items, entering areas)

**New Example Quest:**
- "The Merchant's Dilemma" - demonstrates all branching features
- 3 player choices with different paths
- Reputation-gated choice (requires Guard Reputation >= 20)
- World flag modifications on each choice

**Breaking Changes:**
- Quest_SO now uses stage-based structure (legacy quests auto-migrate)
- `TransitionTrigger` values: 0=OnComplete, 1=OnFail, 2=Conditional, 3=PlayerChoice

### v1.8.0 (2025-12-24)
**QuestLines (Story Arcs):**
- Added `QuestLine_SO` for grouping related quests into narrative arcs
- Added `QuestLineRuntime` for tracking progress across questline quests
- Added `QuestLineState` enum: Locked, Available, InProgress, Completed, Failed
- Added `ConditionQuestLineState_SO` for questline prerequisites
- QuestManager now tracks questlines with full event support
- Completion rewards when all quests in a line are done
- Prerequisite questlines (unlock Questline B after completing Questline A)
- Configurable failure behavior (fail line if any quest fails)

**New QuestManager API:**
- `AddQuestLine(QuestLine_SO)` - Add questline to tracking
- `GetQuestLine(QuestLine_SO)` - Get questline runtime
- `GetActiveQuestLines()` - Get all active questlines
- `GetCompletedQuestLines()` - Get all completed questlines
- `IsQuestLineActive(QuestLine_SO)` - Check if active
- `IsQuestLineCompleted(QuestLine_SO)` - Check if completed

**New Events:**
- `QuestLineAdded`, `QuestLineStarted`, `QuestLineCompleted`, `QuestLineFailed`, `QuestLineUpdated`

**Note:** A visual graph tool for designing quests and questlines is planned for a future release.

### v1.4.0 (2025-12-23)
**Quest Chains:**
- Added `ConditionQuestState_SO` for creating quest prerequisites
- Supports all quest states: NotStarted, InProgress, Completed, Failed
- Supports Equals/NotEquals comparison types
- Event-driven: automatically re-evaluates when quest states change
- Composable with `CompositeCondition_SO` for AND/OR logic
- Works with `IsInverted` for negative conditions (e.g., "not failed")

**Usage Examples:**
- Sequential chains: Quest B requires Quest A completed
- Branching paths: Quest C requires Quest A OR Quest B
- Locked content: Quest D requires Quest A AND Quest B
- Exclusive paths: Quest E only available if Quest A not failed

### v1.3.0 (2025-12-23)
**Architecture Cleanup:**
- Refactored QuestManager into partial class with editor code separated (`QuestManager.Editor.cs`)
- Reduced QuestManager from ~1050 lines to ~527 lines
- Simplified API: removed redundant overloads, kept only Quest_SO parameter versions
- Task operations moved from QuestManager to QuestRuntime

**New QuestRuntime Methods:**
- `IncrementCurrentTask()` - Progress current task
- `DecrementCurrentTask()` - Regress current task
- `ForceComplete()` - Complete all remaining tasks
- `CurrentTask` property - First in-progress task

**New Configuration:**
- `RequireQuestInDatabase` - Optional database validation (configurable)

**Removed Methods (use QuestRuntime instead):**
- `QuestManager.IncrementTaskStep()` -> `quest.IncrementCurrentTask()`
- `QuestManager.DecrementTaskStep()` -> `quest.DecrementCurrentTask()`
- `QuestManager.CompleteTask()` -> `quest.CurrentTask?.CompleteTask()`
- `QuestManager.FailTask()` -> `quest.CurrentTask?.FailTask()`
- `QuestManager.GetCurrentTask()` -> `quest.CurrentTask`
- `QuestManager.GetTasksForQuest()` -> `quest.Tasks`

**Migration Guide:**
```csharp
// Before (v1.2.0):
QuestManager.Instance.IncrementTaskStep(questSO);
QuestManager.Instance.GetCurrentTask(questSO);

// After (v1.3.0):
var quest = QuestManager.Instance.GetActiveQuest(questSO);
quest?.IncrementCurrentTask();
var task = quest?.CurrentTask;
```

### v1.2.0 (2025-12-21)
**QuestManager Improvements:**
- Consistent API with overloads for `Guid`, `Quest_SO`, and `Quest` across all methods
- Added `OnDestroy` cleanup to prevent memory leaks
- Added failed quests tracking (`_failedQuests`, `GetFailedQuests()`, `IsQuestFailed()`)
- Added `autoStartQuestsOnStart` configuration option
- All event handlers now use `SafeInvoke` consistently
- Renamed configuration properties to follow C# naming conventions
- Added `SubscribeToQuestEvents()` for cleaner code organization
- Added `GetActiveQuest(Quest_SO)` overload
- Added quest count properties (`ActiveQuestCount`, `CompletedQuestCount`, `FailedQuestCount`)

**Validation:**
- `Quest_SO.OnValidate()` now validates: empty tasks, null entries, duplicate tasks, non-event-driven start conditions, invalid rewards

**New Helper Methods:**
- `GetCurrentTask(Guid/Quest/Quest_SO)` - Get current in-progress task
- `GetTask(Guid questId, Guid taskId)` - Get specific task
- `IsQuestActive(Guid/Quest_SO)` - Check if quest is active
- `IsQuestFailed(Guid/Quest_SO)` - Check if quest has failed

**Tests:**
- Implemented comprehensive runtime tests (21 tests covering quest creation, lifecycle, task progression, events)
- Implemented editor tests (16 tests covering ScriptableObject creation, equality, runtime instances)

### v1.1.0 (2025-12-21)
**Bug Fixes:**
- Rewards now auto-distribute on quest completion
- `UnsubscribeFromQuestEvents()` now properly cleans up event subscriptions
- `GlobalTaskFailureConditions` now connected and functional
- `StringTask` fully implemented with `SetValue()` and comparison logic

**New Features:**
- Quest completion automatically calls `GiveReward()` on all configured rewards
- Global task failure conditions fail the current in-progress task when triggered

**Code Quality:**
- Added Odin conditionals to BasicQuestExample
- Fixed namespace issues in sample files

### v1.0.0
- Initial release

## License

MIT License
