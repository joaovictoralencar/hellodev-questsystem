# Basic Quest Example

A sample implementation demonstrating the HelloDev Quest System. Includes a complete UI, debug tools, and example quest (Goblin's Bane).

## Features

- Complete Quest UI with sections, items, and details
- Task list with progress tracking
- Reward display
- Debug buttons for testing (requires Odin Inspector for quick actions)

## Structure

```
BasicQuestExample/
├── Prefabs/              ← UI prefabs
├── Scenes/               ← Example scene
├── Scripts/
│   ├── Conditions/       ← Example conditions
│   ├── GameEvents/       ← Example game events
│   ├── Rewards/          ← Example reward types
│   └── UI/               ← Quest UI components
│       └── Quests/       ← Quest-specific UI
└── ScriptableObjects/
    ├── Conditions/       ← Condition instances
    ├── Events/           ← Event instances
    ├── IDs/              ← ID_SO instances
    │   └── Locations/    ← Location IDs
    ├── Quests/           ← Quest assets
    │   └── Goblin's Bane/  ← Example quest + tasks
    ├── QuestType/        ← Quest categories
    └── Rewards/          ← Reward instances
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

Debug buttons are available in `UI_QuestDetails` Inspector (Play Mode only).

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

Available types:
- **Int Task** - Counter-based (kill X, collect Y)
- **Bool Task** - Binary state (find item, talk to NPC)
- **String Task** - Text matching
- **Location Task** - Reach location (uses ID_SO)
- **Timed Task** - Time limit
- **Discovery Task** - Find items (uses List<ID_SO>)

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

| Task Type | Variables |
|-----------|-----------|
| IntTask | `{current}`, `{required}`, `{target}` |
| DiscoveryTask | `{current}`, `{required}` |
| TimedTask | `{remaining}`, `{limit}` |
| LocationTask | `{target}` |

Example: `"Kill {current}/{required} goblins"`

## Dependencies

- HelloDev.QuestSystem
- HelloDev.UI
- HelloDev.Utils
- Unity Localization
- PrimeTween
- Odin Inspector (optional, for debug buttons)
