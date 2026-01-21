using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Utility class for restoring quest system state from snapshots.
    /// Used by QuestSnapshotProvider to restore saved game state.
    /// </summary>
    public static class SnapshotRestorer
    {
        /// <summary>
        /// Restores world flags from snapshot data.
        /// Requires a WorldFlagLocator_SO to set runtime values.
        /// </summary>
        /// <param name="flagSnapshots">The world flag snapshots to restore.</param>
        /// <param name="allFlags">All available world flag assets.</param>
        /// <param name="flagLocator">The flag locator to use for restoring values.</param>
        public static void RestoreWorldFlags(List<WorldFlagSnapshot> flagSnapshots, List<WorldFlagBase_SO> allFlags, WorldFlagLocator_SO flagLocator)
        {
            if (flagLocator == null || !flagLocator.IsAvailable)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "WorldFlagLocator not available. Cannot restore world flags.");
                return;
            }

            int restored = 0;
            int notFound = 0;

            foreach (var snapshot in flagSnapshots)
            {
                var flag = allFlags.Find(f => f != null && f.FlagId == snapshot.FlagGuid);
                if (flag == null)
                {
                    QuestLogger.LogVerbose(LogSubsystem.Save, $"World flag not found: {snapshot.FlagGuid}");
                    notFound++;
                    continue;
                }

                // Ensure flag is registered
                var manager = flagLocator.Manager;
                manager.RegisterFlag(flag);

                // Set value via typed runtime
                switch (flag)
                {
                    case WorldFlagBool_SO boolFlag:
                        manager.GetBoolFlag(boolFlag)?.SetValue(snapshot.BoolValue);
                        QuestLogger.LogVerbose(LogSubsystem.Save, $"Restored bool flag '{boolFlag.FlagName}' = {snapshot.BoolValue}");
                        break;

                    case WorldFlagInt_SO intFlag:
                        manager.GetIntFlag(intFlag)?.SetValue(snapshot.IntValue);
                        QuestLogger.LogVerbose(LogSubsystem.Save, $"Restored int flag '{intFlag.FlagName}' = {snapshot.IntValue}");
                        break;
                }
                restored++;
            }

            // Summary log only
            if (notFound > 0)
            {
                QuestLogger.Log(LogSubsystem.Save, $"Restored {restored} world flags ({notFound} not found)");
            }
            else
            {
                QuestLogger.Log(LogSubsystem.Save, $"Restored {restored} world flags");
            }
        }

        /// <summary>
        /// Restores quests from snapshot data.
        /// Uses a safe restoration order: task states first, then events.
        /// </summary>
        /// <param name="questSnapshots">The quest snapshots to restore.</param>
        /// <param name="targetState">The target state for these quests.</param>
        /// <param name="questManager">The quest manager instance.</param>
        /// <param name="findQuestByGuid">Function to find Quest_SO by GUID.</param>
        public static void RestoreQuests(
            List<QuestSnapshot> questSnapshots,
            QuestState targetState,
            QuestManager questManager,
            System.Func<string, Quest_SO> findQuestByGuid)
        {
            int restored = 0;
            int notFound = 0;
            int totalTasks = 0;

            foreach (var snapshot in questSnapshots)
            {
                // Find the Quest_SO by GUID
                var questData = findQuestByGuid(snapshot.QuestGuid);
                if (questData == null)
                {
                    QuestLogger.LogVerbose(LogSubsystem.Save, $"Quest not found: {snapshot.QuestGuid}");
                    notFound++;
                    continue;
                }

                // Get the captured state from the snapshot
                var capturedState = (QuestState)snapshot.State;

                // Add quest for restore (will be NotStarted initially, no events subscribed)
                // skipAutoStart and skipEventSubscription prevent any automatic behavior during restore
                questManager.AddQuestForRestore(questData, skipAutoStart: true, skipEventSubscription: true);

                // Get the runtime quest
                var quest = questManager.GetActiveQuest(questData);
                if (quest == null) continue;

                QuestLogger.LogVerbose(LogSubsystem.Save, $"Restoring quest '{questData.DevName}': capturedState={capturedState}");

                // STEP 1: Restore branch decisions first (needed for any state)
                foreach (var decision in snapshot.BranchDecisions)
                {
                    quest.BranchDecisions[decision.Key] = decision.Value;
                }

                // STEP 2: Restore all task states and progress BEFORE any events fire
                // This is critical - tasks must have correct state before event subscriptions
                RestoreTaskStates(quest, snapshot.Tasks);

                // STEP 3: Restore stage and group states (without triggering transitions)
                RestoreStageAndGroupStates(quest, snapshot);

                // STEP 4: Set quest state and current stage (without events)
                quest.RestoreQuestState(capturedState, snapshot.CurrentStageIndex);

                // STEP 5: Handle terminal states or resume
                switch (targetState)
                {
                    case QuestState.InProgress:
                        // Resume the quest - this subscribes to events
                        quest.ResumeQuest();
                        break;

                    case QuestState.Completed:
                        // Quest is already complete, no need to subscribe to events
                        // Just ensure all tasks are marked complete
                        foreach (var task in quest.Tasks)
                        {
                            if (task.CurrentState != TaskState.Completed)
                            {
                                task.RestoreState(TaskState.Completed);
                            }
                        }
                        // Move quest from active registry to completed registry
                        questManager.QuestRegistry.MoveToCompleted(quest.QuestId);
                        break;

                    case QuestState.Failed:
                        // Quest is already failed, no need to subscribe to events
                        // Move quest from active registry to failed registry
                        questManager.QuestRegistry.MoveToFailed(quest.QuestId);
                        break;
                }

                restored++;
                totalTasks += snapshot.Tasks.Count;
            }

            // Summary log based on target state
            if (restored > 0)
            {
                string stateLabel = targetState switch
                {
                    QuestState.InProgress => "in-progress",
                    QuestState.Completed => "completed",
                    QuestState.Failed => "failed",
                    _ => targetState.ToString().ToLower()
                };
                QuestLogger.Log(LogSubsystem.Save, $"Restored {restored} {stateLabel} quests ({totalTasks} tasks)");
            }
        }

        /// <summary>
        /// Restores stage and group states for a quest without triggering transitions.
        /// </summary>
        private static void RestoreStageAndGroupStates(QuestRuntime quest, QuestSnapshot snapshot)
        {
            // Determine which stages should be in which state
            foreach (var stage in quest.Stages)
            {
                if (stage.StageIndex < snapshot.CurrentStageIndex)
                {
                    // Stages before the current one are completed
                    stage.RestoreStageState(StageState.Completed, stage.TaskGroups.Count - 1);

                    // All groups in completed stages are completed
                    foreach (var group in stage.TaskGroups)
                    {
                        group.RestoreGroupState(TaskGroupState.Completed);
                    }
                }
                else if (stage.StageIndex == snapshot.CurrentStageIndex)
                {
                    // Current stage - need to figure out the current group
                    int currentGroupIndex = DetermineCurrentGroupIndex(stage, snapshot.Tasks);
                    stage.RestoreStageState(StageState.InProgress, currentGroupIndex);

                    // Set group states
                    for (int i = 0; i < stage.TaskGroups.Count; i++)
                    {
                        var group = stage.TaskGroups[i];
                        if (i < currentGroupIndex)
                        {
                            group.RestoreGroupState(TaskGroupState.Completed);
                        }
                        else if (i == currentGroupIndex)
                        {
                            group.RestoreGroupState(TaskGroupState.InProgress);
                        }
                        // Groups after current remain NotStarted
                    }
                }
                // Stages after current remain NotReached
            }
        }

        /// <summary>
        /// Determines the current group index based on task states.
        /// </summary>
        private static int DetermineCurrentGroupIndex(QuestStageRuntime stage, List<TaskSnapshot> taskSnapshots)
        {
            // Find the first group that has InProgress or NotStarted tasks
            for (int i = 0; i < stage.TaskGroups.Count; i++)
            {
                var group = stage.TaskGroups[i];
                bool hasInProgressOrNotStarted = false;
                bool allCompleted = true;

                foreach (var task in group.Tasks)
                {
                    var taskSnapshot = taskSnapshots.Find(t => t.TaskGuid == task.Data.TaskId.ToString());
                    if (taskSnapshot != null)
                    {
                        var taskState = (TaskState)taskSnapshot.State;
                        if (taskState == TaskState.InProgress || taskState == TaskState.NotStarted)
                        {
                            hasInProgressOrNotStarted = true;
                        }
                        if (taskState != TaskState.Completed)
                        {
                            allCompleted = false;
                        }
                    }
                }

                if (hasInProgressOrNotStarted || !allCompleted)
                {
                    return i;
                }
            }

            // All groups completed, return last index
            return stage.TaskGroups.Count - 1;
        }

        /// <summary>
        /// Restores task states for a quest.
        /// Uses RestoreState to set state directly without triggering events.
        /// </summary>
        /// <param name="quest">The quest runtime to restore tasks for.</param>
        /// <param name="taskSnapshots">The task snapshots to restore.</param>
        public static void RestoreTaskStates(QuestRuntime quest, List<TaskSnapshot> taskSnapshots)
        {
            // First, check for duplicate task GUIDs which would cause incorrect restoration
            var allTasks = quest.Tasks;
            var taskGuidCounts = new Dictionary<string, List<string>>();
            foreach (var task in allTasks)
            {
                var guid = task.Data.TaskId.ToString();
                if (!taskGuidCounts.ContainsKey(guid))
                {
                    taskGuidCounts[guid] = new List<string>();
                }
                taskGuidCounts[guid].Add(task.DevName);
            }

            // Warn about duplicates (this is a real error, keep as warning)
            foreach (var kvp in taskGuidCounts)
            {
                if (kvp.Value.Count > 1)
                {
                    QuestLogger.LogWarning(LogSubsystem.Save, $"DUPLICATE TASK GUID! '{kvp.Key}' shared by: {string.Join(", ", kvp.Value)}");
                }
            }

            foreach (TaskSnapshot taskSnapshot in taskSnapshots)
            {
                TaskRuntime task = allTasks.FirstOrDefault(t => t.Data.TaskId.ToString() == taskSnapshot.TaskGuid);
                if (task == null)
                {
                    QuestLogger.LogVerbose(LogSubsystem.Save, $"Task not found: {taskSnapshot.TaskGuid}");
                    continue;
                }

                TaskState targetState = (TaskState)taskSnapshot.State;

                // Individual task logs are verbose only
                QuestLogger.LogVerbose(LogSubsystem.Save, $"Task '{task.DevName}': {targetState}, progress={taskSnapshot.ProgressData.IntValue}");

                // Restore type-specific progress using polymorphism (no events fired)
                task.RestoreProgress(taskSnapshot.ProgressData);

                // Restore task state directly without triggering events
                // Events will be subscribed later via ResumeTask() for InProgress tasks
                task.RestoreState(targetState);
            }
        }

        /// <summary>
        /// Restores questlines from snapshot data.
        /// </summary>
        /// <param name="lineSnapshots">The questline snapshots to restore.</param>
        /// <param name="questManager">The quest manager instance.</param>
        /// <param name="findQuestLineByGuid">Function to find QuestLine_SO by GUID.</param>
        public static void RestoreQuestLines(
            List<QuestLineSnapshot> lineSnapshots,
            QuestManager questManager,
            System.Func<string, QuestLine_SO> findQuestLineByGuid)
        {
            int restored = 0;

            foreach (var snapshot in lineSnapshots)
            {
                var questLineData = findQuestLineByGuid(snapshot.QuestLineGuid);
                if (questLineData == null)
                {
                    QuestLogger.LogVerbose(LogSubsystem.Save, $"QuestLine not found: {snapshot.QuestLineGuid}");
                    continue;
                }

                // Add questline
                questManager.AddQuestLine(questLineData);

                // Get the runtime questline
                var questLine = questManager.GetQuestLine(questLineData);
                if (questLine == null) continue;

                // Restore state
                var state = (QuestLines.QuestLineState)snapshot.State;
                questLine.RestoreState(state, snapshot.HasStarted);
                restored++;
            }

            if (restored > 0)
            {
                QuestLogger.Log(LogSubsystem.Save, $"Restored {restored} questlines");
            }
        }

        // Tutorial restore is now handled by TutorialManager.RestoreSnapshot()
    }
}
