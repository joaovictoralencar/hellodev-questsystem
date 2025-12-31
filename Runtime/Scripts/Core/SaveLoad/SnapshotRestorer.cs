using System.Collections.Generic;
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
    /// Extracted from QuestSaveManager for single responsibility.
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
                QuestLogger.LogWarning("[SnapshotRestorer] WorldFlagLocator not available. Cannot restore world flags.");
                return;
            }

            QuestLogger.Log($"[SnapshotRestorer] Restoring {flagSnapshots.Count} world flags...");
            int restored = 0;
            int notFound = 0;

            foreach (var snapshot in flagSnapshots)
            {
                var flag = allFlags.Find(f => f != null && f.FlagId == snapshot.FlagGuid);
                if (flag == null)
                {
                    QuestLogger.LogWarning($"[SnapshotRestorer] World flag not found: {snapshot.FlagGuid}");
                    notFound++;
                    continue;
                }

                // Ensure flag is registered
                flagLocator.RegisterFlag(flag);

                // Set value via locator
                switch (flag)
                {
                    case WorldFlagBool_SO boolFlag:
                        flagLocator.SetBoolValue(boolFlag, snapshot.BoolValue);
                        QuestLogger.Log($"[SnapshotRestorer] Restored bool flag '{boolFlag.FlagName}' = {snapshot.BoolValue}");
                        break;

                    case WorldFlagInt_SO intFlag:
                        flagLocator.SetIntValue(intFlag, snapshot.IntValue);
                        QuestLogger.Log($"[SnapshotRestorer] Restored int flag '{intFlag.FlagName}' = {snapshot.IntValue}");
                        break;
                }
                restored++;
            }

            QuestLogger.Log($"[SnapshotRestorer] World flags restore complete: {restored} restored, {notFound} not found.");
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
            QuestLogger.Log($"[SnapshotRestorer] RestoreQuests called with {questSnapshots.Count} snapshots, targetState={targetState}");

            foreach (var snapshot in questSnapshots)
            {
                // Find the Quest_SO by GUID
                var questData = findQuestByGuid(snapshot.QuestGuid);
                if (questData == null)
                {
                    QuestLogger.LogWarning($"[SnapshotRestorer] Quest not found: {snapshot.QuestGuid}");
                    continue;
                }

                // Get the captured state from the snapshot
                var capturedState = (QuestState)snapshot.State;

                // Add quest (will be NotStarted initially, no events subscribed)
                // skipAutoStart and skipEventSubscription prevent any automatic behavior
                questManager.AddQuest(questData, forceStart: false, skipAutoStart: true, skipEventSubscription: true);

                // Get the runtime quest
                var quest = questManager.GetActiveQuest(questData);
                if (quest == null) continue;

                QuestLogger.Log($"[SnapshotRestorer] Restoring quest '{questData.DevName}': capturedState={capturedState}, targetState={targetState}");

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
                        break;

                    case QuestState.Failed:
                        // Quest is already failed, no need to subscribe to events
                        break;
                }
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

            // Warn about duplicates
            foreach (var kvp in taskGuidCounts)
            {
                if (kvp.Value.Count > 1)
                {
                    QuestLogger.LogWarning($"[SnapshotRestorer] DUPLICATE TASK GUID DETECTED! GUID '{kvp.Key}' is shared by tasks: {string.Join(", ", kvp.Value)}. This will cause incorrect save/load behavior!");
                }
            }

            QuestLogger.Log($"[SnapshotRestorer] RestoreTaskStates: {taskSnapshots.Count} snapshots, {allTasks.Count} tasks in quest");

            foreach (var taskSnapshot in taskSnapshots)
            {
                var task = allTasks.Find(t => t.Data.TaskId.ToString() == taskSnapshot.TaskGuid);
                if (task == null)
                {
                    QuestLogger.LogWarning($"[SnapshotRestorer] Task not found for GUID '{taskSnapshot.TaskGuid}'");
                    continue;
                }

                var targetState = (TaskState)taskSnapshot.State;

                QuestLogger.Log($"[SnapshotRestorer] Restoring task '{task.DevName}' (GUID: {taskSnapshot.TaskGuid.Substring(0, 8)}...): " +
                               $"targetState={targetState}, intProgress={taskSnapshot.ProgressData.IntValue}");

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
            foreach (var snapshot in lineSnapshots)
            {
                var questLineData = findQuestLineByGuid(snapshot.QuestLineGuid);
                if (questLineData == null)
                {
                    QuestLogger.LogWarning($"[SnapshotRestorer] QuestLine not found: {snapshot.QuestLineGuid}");
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
            }
        }
    }
}
