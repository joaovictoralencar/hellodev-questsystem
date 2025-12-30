# Basic Quest Example

A sample implementation demonstrating the HelloDev Quest System. Includes a complete UI, debug tools, and example quests.

## Getting Started

### 1. Set Up the Example in Your Scene

The BasicQuestExample provides reusable components and assets. To use them:

1. Create a new scene or use an existing one
2. Add a QuestManager component to a GameObject
3. Add the example quests from `ScriptableObjects/Quests/` to the QuestManager's database
4. Add the Quest UI prefab from `Prefabs/` to your Canvas

### 2. Enter Play Mode

The example will:
- Initialize the QuestManager with example quests
- Display the Quest UI panel
- Allow you to interact with quests via debug tools

### 3. Test the Quest System

1. Select a quest in the UI
2. Use the debug buttons in the Inspector (requires Odin Inspector)
3. Watch quest states update in real-time

## Features

- Complete Quest UI with sections, items, and details
- Task list with progress tracking
- Reward display
- Debug buttons for testing (requires Odin Inspector for quick actions)
- Example quests demonstrating different task types
- Localization setup

## Structure

```
BasicQuestExample/
├── Docs/                 ← Documentation (EventIntegrationGuide.md)
├── Fonts/                ← Font assets (Source Sans 3)
├── Prefabs/              ← UI prefabs
├── Scripts/
│   ├── Conditions/       ← Example condition types (ConditionID_SO)
│   ├── GameEvents/       ← Example game event types (GameEventID_SO)
│   ├── Rewards/          ← Example reward types
│   ├── UI/               ← Quest UI components
│   └── SaveSystemSetup.cs ← Save system configuration component
├── ScriptableObjects/
│   ├── Conditions/       ← Condition instances
│   │   ├── Branching/    ← Branching quest conditions
│   │   └── QuestChains/  ← Quest chain conditions
│   ├── Events/           ← Event instances
│   ├── IDs/              ← ID_SO instances (Enemies, NPCs, Locations)
│   ├── Quests/           ← Quest assets
│   │   └── Goblin's Bane/ ← Example quest + tasks
│   ├── QuestLines/       ← QuestLine assets
│   ├── QuestType/        ← Quest categories
│   ├── Rewards/          ← Reward instances
│   ├── SaveLoad/         ← Save system configuration assets
│   └── WorldFlags/       ← World flag instances
├── Settings/             ← Import presets
└── Textures/             ← Sprite textures
```

## UI Components

| Component | Description |
|-----------|-------------|
| `UI_Quests` | Main quest panel with sections |
| `UI_QuestSection` | Category section (Main, Secondary, etc.) |
| `UI_QuestItem` | Individual quest in list |
| `UI_QuestDetails` | Selected quest details + tasks |
| `UI_TaskItem` | Individual task in details |
| `UI_QuestRewards` | Reward display |

## Debug Tools

Debug buttons are available in `UI_QuestDetails` Inspector (Play Mode only, requires Odin Inspector).

### Task Actions

| Button | Action |
|--------|--------|
| Complete Current Task | Force complete selected task |
| Fail Current Task | Force fail selected task |
| Increment Task | +1 for IntTask counters |
| Decrement Task | -1 for IntTask counters |
| Reset Current Task | Reset task to NotStarted |
| Invoke Event Task | Trigger task conditions |

### Quest Actions

| Button | Action |
|--------|--------|
| Complete Current Quest | Complete all tasks |
| Fail Current Quest | Force fail quest |
| Reset Current Quest | Restart quest from beginning |

### Task-Type Specific (Odin Quick Actions)

| Button | Task Type | Action |
|--------|-----------|--------|
| Trigger Location Reached | LocationTask | Mark location as reached |
| Add 30 Seconds | TimedTask | Add time to timer |
| Expire Timer | TimedTask | Force timer expiration |
| Complete Timed Objective | TimedTask | Mark objective complete |
| Discover Next Item | DiscoveryTask | Discover next item |

## Test Scenarios

### Test 1: Complete → Restart → Complete
1. Start a quest and select it in UI
2. Use "Increment Task" repeatedly until task completes
3. Continue until quest completes → Moves to "Completed" section
4. Use "Reset Current Quest" → Returns to active section
5. Verify progress resets to 0%
6. Complete again
7. **Pass:** Quest completes both times without errors

### Test 2: Fail → Restart → Complete
1. Start a quest
2. Use "Fail Current Quest"
3. Verify failed state in UI
4. Use "Reset Current Quest"
5. Complete all tasks
6. **Pass:** Quest completes after restart

### Test 3: Increment/Decrement (IntTask)
1. Select an IntTask (e.g., "Kill 5 Goblins")
2. "Increment Task" 3x → Shows 3/5
3. "Decrement Task" 1x → Shows 2/5
4. "Increment Task" 3x → Completes at 5/5
5. **Pass:** Counter works correctly

### Test 4: LocationTask
1. Select a LocationTask
2. Use "Trigger Location Reached"
3. **Pass:** Task completes immediately

### Test 5: TimedTask
1. Select a TimedTask
2. "Add 30 Seconds" → Timer increases
3. "Expire Timer" → Task fails
4. Reset, then "Complete Timed Objective"
5. **Pass:** Both expiry and completion work

### Test 6: DiscoveryTask
1. Select a DiscoveryTask (3 items)
2. "Discover Next Item" 3x
3. **Pass:** Progress shows 1/3, 2/3, 3/3, then completes

### Test 7: Memory Leak Check
1. Switch between quests 10+ times
2. Complete/restart quests
3. Exit Play Mode
4. **Pass:** No console errors

## Creating New Quests

### 1. Create Task Assets

```
Right-click > Create > HelloDev > Quest System > Scriptable Objects > Tasks > [Type]
```

**Available types:**
| Type | Description | Use Case |
|------|-------------|----------|
| Int Task | Counter-based | Kill X, Collect Y |
| Bool Task | Binary state | Find item, Talk to NPC |
| String Task | Text matching | Enter password |
| Location Task | Reach location (uses ID_SO) | Go to waypoint |
| Timed Task | Time limit | Survive, Escape |
| Discovery Task | Find items (uses List\<ID_SO\>) | Find all clues |

### 2. Create Quest Asset

```
Right-click > Create > HelloDev > Quest System > Scriptable Objects > Quest
```

Configure:
- Add tasks to the Tasks list
- Set Quest Type for categorization
- Add Start Conditions (optional)
- Add Failure Conditions (optional)
- Add Rewards (optional)

### 3. Add to QuestManager

Reference the Quest_SO in QuestManager's database.

## Localization

Tasks require localized strings for:
- `displayName` - Task title shown in UI
- `taskDescription` - Detailed description

Add entries in your StringTable and link via Inspector.

### Smart String Variables

| Task Type | Variables | Example |
|-----------|-----------|---------|
| IntTask | `{current}`, `{required}`, `{target}` | "Kill {current}/{required} goblins" |
| DiscoveryTask | `{current}`, `{required}` | "Found {current}/{required} items" |
| TimedTask | `{remaining}`, `{limit}` | "{remaining}s remaining" |
| LocationTask | `{target}` | "Go to {target}" |

## Dependencies

- HelloDev.QuestSystem
- HelloDev.UI
- HelloDev.Utils
- Unity Localization
- PrimeTween
- Odin Inspector (optional, for debug buttons)
