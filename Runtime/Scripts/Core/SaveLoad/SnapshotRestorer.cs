using System.Collections.Generic;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
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
        /// Requires a WorldFlagService_SO to set runtime values.
        /// </summary>
        /// <param name="flagSnapshots">The world flag snapshots to restore.</param>
        /// <param name="allFlags">All available world flag assets.</param>
        /// <param name="flagService">The flag service to use for restoring values.</param>
        public static void RestoreWorldFlags(List<WorldFlagSnapshot> flagSnapshots, List<WorldFlagBase_SO> allFlags, WorldFlagService_SO flagService)
        {
            if (flagService == null || !flagService.IsAvailable)
            {
                QuestLogger.LogWarning("[SnapshotRestorer] WorldFlagService not available. Cannot restore world flags.");
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
                flagService.RegisterFlag(flag);

                // Set value via service
                switch (flag)
                {
                    case WorldFlagBool_SO boolFlag:
                        flagService.SetBoolValue(boolFlag, snapshot.BoolValue);
                        QuestLogger.Log($"[SnapshotRestorer] Restored bool flag '{boolFlag.FlagName}' = {snapshot.BoolValue}");
                        break;

                    case WorldFlagInt_SO intFlag:
                        flagService.SetIntValue(intFlag, snapshot.IntValue);
                        QuestLogger.Log($"[SnapshotRestorer] Restored int flag '{intFlag.FlagName}' = {snapshot.IntValue}");
                        break;
                }
                restored++;
            }

            QuestLogger.Log($"[SnapshotRestorer] World flags restore complete: {restored} restored, {notFound} not found.");
        }

        /// <summary>
        /// Restores quests from snapshot data.
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

                // Get the captured state to determine if we should skip event subscription
                var capturedState = (QuestState)snapshot.State;

                // Add quest (will be NotStarted initially)
                // Use skipAutoStart to prevent quests from auto-starting during restore
                // Use skipEventSubscription for NotStarted quests to prevent events from triggering auto-start
                // Event subscription will be restored after all quests are loaded via ResubscribeNotStartedQuestsToEvents
                bool skipEvents = capturedState == QuestState.NotStarted;
                questManager.AddQuest(questData, forceStart: false, skipAutoStart: true, skipEventSubscription: skipEvents);

                // Get the runtime quest
                var quest = questManager.GetActiveQuest(questData);
                if (quest == null) continue;

                // Restore branch decisions (must happen before starting quest)
                foreach (var decision in snapshot.BranchDecisions)
                {
                    quest.BranchDecisions[decision.Key] = decision.Value;
                }

                QuestLogger.Log($"[SnapshotRestorer] Quest '{questData.DevName}': capturedState={capturedState}, targetState={targetState}, currentState={quest.CurrentState}");

                // For InProgress/Completed/Failed quests, we need to start them first
                // before we can set stage and restore task states
                bool needsStart = targetState == QuestState.InProgress && capturedState == QuestState.InProgress
                               || targetState == QuestState.Completed
                               || targetState == QuestState.Failed;

                QuestLogger.Log($"[SnapshotRestorer] Quest '{questData.DevName}': needsStart={needsStart}");

                if (needsStart && quest.CurrentState == QuestState.NotStarted)
                {
                    QuestLogger.Log($"[SnapshotRestorer] Starting quest '{questData.DevName}'");
                    quest.StartQuest();
                }
                else if (!needsStart)
                {
                    QuestLogger.Log($"[SnapshotRestorer] Quest '{questData.DevName}' kept as NotStarted (was saved as NotStarted)");
                }

                // Restore stage (only works if quest is InProgress)
                // Must happen AFTER StartQuest() but BEFORE restoring task states
                if (snapshot.CurrentStageIndex >= 0 && quest.CurrentState == QuestState.InProgress)
                {
                    quest.TrySetStage(snapshot.CurrentStageIndex);
                }

                // Restore task states
                RestoreTaskStates(quest, snapshot.Tasks);

                // Handle terminal states
                switch (targetState)
                {
                    case QuestState.Completed:
                        quest.ForceComplete();
                        break;

                    case QuestState.Failed:
                        quest.FailQuest();
                        break;
                }
            }
        }

        /// <summary>
        /// Restores task states for a quest.
        /// </summary>
        /// <param name="quest">The quest runtime to restore tasks for.</param>
        /// <param name="taskSnapshots">The task snapshots to restore.</param>
        public static void RestoreTaskStates(QuestRuntime quest, List<TaskSnapshot> taskSnapshots)
        {
            foreach (var taskSnapshot in taskSnapshots)
            {
                var task = quest.Tasks.Find(t => t.Data.TaskId.ToString() == taskSnapshot.TaskGuid);
                if (task == null) continue;

                // Restore type-specific progress using polymorphism
                task.RestoreProgress(taskSnapshot.ProgressData);

                // Restore task state (Completed/Failed)
                var targetState = (TaskState)taskSnapshot.State;
                if (targetState == TaskState.Completed && task.CurrentState == TaskState.InProgress)
                {
                    task.CompleteTask();
                }
                else if (targetState == TaskState.Failed && task.CurrentState == TaskState.InProgress)
                {
                    task.FailTask();
                }
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
