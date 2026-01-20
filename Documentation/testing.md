# Quest System Manual Test Scenarios

*Last Updated: 2026-01-14*

These tests use the debug buttons in UI_QuestDetails (requires Odin Inspector for quick action buttons).

**Debug Script Location:** `BasicQuestExample/Scripts/UI/Quests/UI_QuestDetails.cs`

## Debug Button Reference

| Button | Task Type | Action |
|--------|-----------|--------|
| Increment Task | IntTask | +1 to counter |
| Decrement Task | IntTask | -1 from counter |
| Complete Current Task | All | Force complete |
| Fail Current Task | All | Force fail |
| Reset Current Task | All | Reset to NotStarted |
| Invoke Event Task | All | Trigger conditions |
| Complete Current Quest | Quest | Complete all tasks |
| Fail Current Quest | Quest | Fail quest |
| Reset Current Quest | Quest | Restart quest |
| Trigger Location Reached | LocationTask | Mark location reached |
| Add 30 Seconds | TimedTask | Add time to timer |
| Expire Timer | TimedTask | Force timer to 0 |
| Complete Timed Objective | TimedTask | Complete before expiry |
| Discover Next Item | DiscoveryTask | Discover one item |

---

## Test 1: Complete → Restart → Complete
1. Start a quest and select it in UI
2. Use "Increment Task" repeatedly until task completes
3. Continue until all tasks complete → Quest moves to "Completed" section
4. Use "Reset Current Quest" → Quest returns to active section
5. Verify progress resets to 0%
6. Complete the quest again
7. **Pass:** Quest completes successfully both times

## Test 2: Fail → Restart → Complete
1. Start a quest and select it
2. Use "Fail Current Quest"
3. Verify quest shows "Failed" state
4. Use "Reset Current Quest"
5. Verify quest returns to "InProgress"
6. Complete all tasks
7. **Pass:** Quest completes after restart

## Test 3: Increment → Decrement → Complete (IntTask)
1. Start a quest with IntTask (e.g., "Kill 5 Goblins")
2. Use "Increment Task" 3 times → Should show 3/5
3. Use "Decrement Task" 1 time → Should show 2/5
4. Use "Increment Task" 3 times → Should complete at 5/5
5. **Pass:** Counter updates correctly, task completes at required count

## Test 4: Task Fail → Task Reset
1. Start a quest with multiple tasks
2. Complete first task
3. Select second task (InProgress)
4. Use "Fail Current Task"
5. Use "Reset Current Task"
6. Complete the task
7. **Pass:** Task resets and quest continues normally

## Test 5: LocationTask
1. Create quest with TaskLocation_SO
2. Select the LocationTask
3. Use "Trigger Location Reached"
4. **Pass:** Task completes, progress shows 100%

## Test 6: TimedTask
1. Create quest with TaskTimed_SO (60 seconds)
2. Select the TimedTask
3. Use "Add 30 Seconds" → Timer increases
4. Use "Expire Timer" → Task fails
5. Reset and use "Complete Timed Objective"
6. **Pass:** Task completes when objective done, fails when timer expires

## Test 7: DiscoveryTask
1. Create quest with TaskDiscovery_SO (3 items required)
2. Select the DiscoveryTask
3. Use "Discover Next Item" 3 times
4. **Pass:** Progress shows 1/3, 2/3, 3/3, then completes

## Test 8: Multiple Quests Independence
1. Start Quest A and Quest B
2. Progress Quest A to 50%
3. Complete Quest B
4. Verify Quest A still at 50%
5. **Pass:** Each quest tracks independently

## Test 9: UI Refresh After Restart
1. Complete a quest
2. Use "Reset Current Quest"
3. Verify quest moves to active section
4. Close and reopen Quest UI
5. **Pass:** State persists correctly

## Test 10: Memory Leak Check
1. Select Quest A, then Quest B, repeat 10 times
2. Complete and restart quests multiple times
3. Exit Play Mode
4. **Pass:** No errors in console, no "already subscribed" warnings

---

## Example Quests (Validated 2025-12-21)

### Goblin's Bane (Main Quest)
**Location:** `BasicQuestExample/ScriptableObjects/Quests/Goblin's Bane/`

| Property | Value |
|----------|-------|
| Type | Main Quest |
| Level | 5 |
| Tasks | 6 (Discovery, Location, Int, Bool, Timed, Location) |
| Rewards | 1500 XP |

**Conditions:**
- Start: AtLeastLevel5, PlayerInVillage
- Quest Failure: VillageAttacked
- Global Task Failure: PlayerDeath
- Task 2 Failure: GoblinScoutAlert (stealth)
- Task 3 Failure: GoblinsEscaped (>=3)

### The Merchant's Stolen Goods (Secondary Quest)
**Location:** `BasicQuestExample/ScriptableObjects/Quests/Merchant's Stolen Goods/`

| Property | Value |
|----------|-------|
| Type | Secondary Quest |
| Level | 3 |
| Tasks | 6 (Bool, Discovery, Location, Int, String, Location) |
| Rewards | 750 Gold, 500 XP |

**Task Types Demonstrated:**
1. TalkToMerchant (BoolTask) - Dialogue trigger
2. SearchCrimeScene (DiscoveryTask) - 2 clues
3. FollowTheTrail (LocationTask) - BanditCamp
4. RecoverGoods (IntTask) - 3 crates
5. InterrogateBanditLeader (StringTask) - "Scarface"
6. ReturnToMerchant (LocationTask) - Market

---

## Stage Transition Tests

### Test 11: Stage Progression
**Quest:** The Merchant's Dilemma (branching quest)

1. Start quest and complete Stage 0 tasks
2. Verify stage transition fires (OnStageChanged)
3. Check journal updates to new stage entry
4. **Pass:** Stage advances to choice stage

### Test 12: PlayerChoice Transitions
**Quest:** The Merchant's Dilemma

1. Complete tasks until choice stage reached
2. Verify OnChoicesAvailable event fires
3. Check all choices display in UI
4. Select "Combat" path
5. Verify OnChoiceMade event fires
6. Verify world flag modifications applied
7. **Pass:** Stage transitions to Combat path (Stage 10)

### Test 13: Conditional Choice Gating
1. Set REPUTATION flag to low value
2. Start choice stage quest
3. Verify "Lawful" choice is disabled (requires high rep)
4. Increase REPUTATION flag
5. Verify "Lawful" choice becomes available
6. **Pass:** Condition-based choice gating works

---

## World Flags Tests

### Test 14: WorldFlagBool_SO
**Location:** `com.hellodev.conditions/Runtime/Scripts/WorldFlags/`

1. Create WorldFlagBool_SO with default = false
2. Enter Play Mode
3. Call `flag.SetValue(true)`
4. Verify `flag.Value` returns true
5. Call `flag.Toggle()`
6. Verify `flag.Value` returns false
7. Exit Play Mode, re-enter
8. Verify flag reset to default (false)
9. **Pass:** Boolean flag works, resets on play mode

### Test 15: WorldFlagInt_SO with Min/Max
1. Create WorldFlagInt_SO (default=0, min=-10, max=10)
2. Call `flag.Add(5)` → should be 5
3. Call `flag.Add(10)` → should be 10 (clamped to max)
4. Call `flag.Subtract(25)` → should be -10 (clamped to min)
5. **Pass:** Integer flag clamps correctly

### Test 16: WorldFlagModification in Transitions
1. Start quest with WorldFlagModification on choice
2. Select the choice
3. Verify flag was modified
4. **Pass:** Stage transitions apply world flag changes

---

## Save/Load Tests

### Test 17: Save Quest Progress
**Location:** `Runtime/Scripts/Core/SaveLoad/`

1. Start quest and progress to 50%
2. Call `saveManager.SaveAsync("test_slot")`
3. Verify save file created
4. Exit Play Mode
5. **Pass:** Save operation completes without errors

### Test 18: Load Quest Progress
1. Start fresh Play Mode
2. Call `saveManager.LoadAsync("test_slot")`
3. Verify quest state restored (50% progress)
4. Verify correct stage loaded
5. **Pass:** Load restores exact state

### Test 19: Save/Load Branch Decisions
1. Start branching quest
2. Make choice (e.g., "Combat" path)
3. Progress through chosen branch
4. Save game
5. Exit and reload
6. Verify BranchDecisions dictionary restored
7. Verify correct stage active
8. **Pass:** Branch decisions persist

### Test 20: Save/Load World Flags
1. Modify world flags during gameplay
2. Save game
3. Reset flags to defaults
4. Load game
5. Verify world flags restored to saved values
6. **Pass:** World flags persist with save

### Test 21: Save Slot Metadata
1. Save to "slot_1"
2. Get slot metadata
3. Verify timestamp is current
4. Verify play time tracked
5. **Pass:** Metadata stored correctly

### Test 22: Delete Save Slot
1. Save to "temp_slot"
2. Verify file exists
3. Call `saveManager.DeleteSlotAsync("temp_slot")`
4. Verify file deleted
5. **Pass:** Slot deletion works

---

## QuestLine Tests

### Test 23: QuestLine Progression
**Location:** `Runtime/Scripts/Core/QuestLines/`

1. Add QuestLine with 3 quests
2. Complete first quest
3. Verify QuestLine progress updates
4. Complete all quests
5. Verify OnQuestLineCompleted fires
6. **Pass:** QuestLine tracks completion

### Test 24: Prerequisite QuestLines
1. Create QuestLine B with QuestLine A as prerequisite
2. Verify QuestLine B is Locked
3. Complete QuestLine A
4. Verify QuestLine B becomes Available
5. **Pass:** Prerequisite chaining works

---

## Integration Tests

### Test 25: Full Quest Lifecycle with All Features
1. Start quest with stages
2. Progress through first stage
3. Make branching choice
4. Verify world flags modified
5. Complete remaining tasks
6. Save game
7. Exit and reload
8. Verify state restored exactly
9. Complete quest
10. Verify rewards distributed
11. Verify QuestLine updated
12. **Pass:** All systems work together

---

## TransitionNode Tests (Graph Editor)

### Test 26: TransitionNode OnGroupsComplete
**Quest:** Graph with TransitionNode using OnGroupsComplete trigger

1. Create quest graph with Stage 0 → TransitionNode → Stage 10
2. Set TransitionNode trigger to OnGroupsComplete
3. Add task group to Stage 0
4. Start quest, complete Stage 0's tasks
5. Verify transition fires and advances to Stage 10
6. **Pass:** OnGroupsComplete trigger works correctly

### Test 27: TransitionNode OnConditionsMet (Conditional Skip)
**Quest:** Graph with conditional skip path

1. Create quest graph:
   - Stage 0 → TransitionNode (Priority 10, OnConditionsMet) → Stage 20
   - Stage 0 → Then → Stage 10 → Then → Stage 20
2. Add condition `HasCompletedTutorialBefore` to TransitionNode
3. Set world flag `HasCompletedTutorialBefore = false`
4. Start quest → Should proceed to Stage 10 (normal path)
5. Reset quest, set `HasCompletedTutorialBefore = true`
6. Start quest → Should skip directly to Stage 20
7. **Pass:** Conditional skip path works with priority

### Test 28: TransitionNode Priority Ordering
**Quest:** Multiple TransitionNodes targeting same stage

1. Create quest graph:
   - Stage 0 → TransitionNode A (Priority 5, ConditionA) → Stage 10
   - Stage 0 → TransitionNode B (Priority 10, ConditionB) → Stage 20
   - Stage 0 → Then → Stage 30
2. Set both ConditionA and ConditionB to true
3. Start quest and complete Stage 0
4. Verify Stage 20 is reached (TransitionNode B has higher priority)
5. Reset, set only ConditionA to true
6. Verify Stage 10 is reached
7. Reset, set both conditions to false
8. Verify Stage 30 is reached (default Then path)
9. **Pass:** Priority ordering works correctly

### Test 29: Multiple Sources to Same Stage (Port Multi-Capacity)
**Quest:** Multiple paths converging on one stage

1. Create quest graph:
   - Stage 0 → Then → Stage 10
   - Stage 0 → TransitionNode → Stage 10 (same target)
   - Stage 5 → Then → Stage 10 (another source)
2. Verify graph validates without errors
3. Start from Stage 0, complete normally → reaches Stage 10
4. Test TransitionNode path → reaches Stage 10
5. **Pass:** StageNode In port accepts multiple connections

### Test 30: TransitionNode Manual Trigger
**Quest:** Graph with manual transition

1. Create quest graph with TransitionNode using Manual trigger
2. Start quest at Stage 0
3. Complete Stage 0's tasks
4. Verify transition does NOT fire automatically
5. Trigger transition manually via code/event
6. Verify stage advances
7. **Pass:** Manual trigger requires explicit activation
