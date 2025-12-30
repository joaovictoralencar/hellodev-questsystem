using System;
using System.Collections.Generic;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Utility class for capturing quest system state into snapshots.
    /// Extracted from QuestSaveManager for single responsibility.
    /// </summary>
    public static class SnapshotCapturer
    {
        /// <summary>
        /// Captures the complete state of the quest system.
        /// </summary>
        /// <param name="activeQuests">Active quests to capture.</param>
        /// <param name="completedQuests">Completed quests to capture.</param>
        /// <param name="failedQuests">Failed quests to capture.</param>
        /// <param name="activeQuestLines">Active questlines to capture.</param>
        /// <param name="completedQuestLines">Completed questlines to capture.</param>
        /// <param name="worldFlags">World flags to capture.</param>
        /// <param name="flagLocator">Optional flag locator for runtime values.</param>
        /// <returns>A complete snapshot of the quest system.</returns>
        public static QuestSystemSnapshot CaptureFullSnapshot(
            IEnumerable<QuestRuntime> activeQuests,
            IEnumerable<QuestRuntime> completedQuests,
            IEnumerable<QuestRuntime> failedQuests,
            IEnumerable<QuestLineRuntime> activeQuestLines,
            IEnumerable<QuestLineRuntime> completedQuestLines,
            IEnumerable<WorldFlagBase_SO> worldFlags,
            WorldFlagLocator_SO flagLocator = null)
        {
            var snapshot = new QuestSystemSnapshot
            {
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            // Capture active quests
            foreach (var quest in activeQuests)
            {
                snapshot.ActiveQuests.Add(CaptureQuest(quest));
            }

            // Capture completed quests
            foreach (var quest in completedQuests)
            {
                snapshot.CompletedQuests.Add(CaptureQuest(quest));
            }

            // Capture failed quests
            foreach (var quest in failedQuests)
            {
                snapshot.FailedQuests.Add(CaptureQuest(quest));
            }

            // Capture questlines
            foreach (var line in activeQuestLines)
            {
                snapshot.ActiveQuestLines.Add(CaptureQuestLine(line));
            }

            foreach (var line in completedQuestLines)
            {
                snapshot.CompletedQuestLines.Add(CaptureQuestLine(line));
            }

            // Capture world flags
            int flagsCaptured = 0;
            foreach (var flag in worldFlags)
            {
                if (flag != null)
                {
                    snapshot.WorldFlags.Add(CaptureWorldFlag(flag, flagLocator));
                    flagsCaptured++;
                }
            }

            if (flagsCaptured > 0)
            {
                QuestLogger.Log($"[SnapshotCapturer] Captured {flagsCaptured} world flags.");
            }

            return snapshot;
        }

        /// <summary>
        /// Captures a single quest's state.
        /// </summary>
        public static QuestSnapshot CaptureQuest(QuestRuntime quest)
        {
            var snapshot = new QuestSnapshot
            {
                QuestGuid = quest.QuestData.QuestId.ToString(),
                State = (int)quest.CurrentState,
                CurrentStageIndex = quest.CurrentStageIndex
            };

            // Capture branch decisions
            foreach (var kvp in quest.BranchDecisions)
            {
                snapshot.BranchDecisions.Add(new BranchDecisionEntry(kvp.Key, kvp.Value));
            }

            // Capture all tasks
            foreach (var task in quest.Tasks)
            {
                snapshot.Tasks.Add(CaptureTask(task));
            }

            return snapshot;
        }

        /// <summary>
        /// Captures a single task's state.
        /// </summary>
        public static TaskSnapshot CaptureTask(TaskRuntime task)
        {
            var snapshot = new TaskSnapshot
            {
                TaskGuid = task.Data.TaskId.ToString(),
                State = (int)task.CurrentState,
                TaskType = task.GetType().Name,
                ProgressData = new TaskProgressData()
            };

            // Capture type-specific data using polymorphism
            task.CaptureProgress(snapshot.ProgressData);

            return snapshot;
        }

        /// <summary>
        /// Captures a single questline's state.
        /// </summary>
        public static QuestLineSnapshot CaptureQuestLine(QuestLineRuntime line)
        {
            return new QuestLineSnapshot
            {
                QuestLineGuid = line.QuestLineId.ToString(),
                State = (int)line.CurrentState,
                CompletedQuestsCount = line.CompletedQuestCount,
                HasStarted = line.HasStarted
            };
        }

        /// <summary>
        /// Captures a single world flag's state.
        /// Requires a WorldFlagLocator_SO to get runtime values.
        /// </summary>
        /// <param name="flag">The flag to capture.</param>
        /// <param name="flagLocator">Optional flag locator for runtime values. If null, uses default values.</param>
        public static WorldFlagSnapshot CaptureWorldFlag(WorldFlagBase_SO flag, WorldFlagLocator_SO flagLocator = null)
        {
            var snapshot = new WorldFlagSnapshot
            {
                FlagGuid = flag.FlagId
            };

            // Get runtime values from flag locator
            if (flagLocator == null || !flagLocator.IsAvailable)
            {
                // Fallback to default values if locator not available
                switch (flag)
                {
                    case WorldFlagBool_SO boolFlag:
                        snapshot.IsBoolFlag = true;
                        snapshot.BoolValue = boolFlag.DefaultValue;
                        break;

                    case WorldFlagInt_SO intFlag:
                        snapshot.IsBoolFlag = false;
                        snapshot.IntValue = intFlag.DefaultValue;
                        break;
                }
                return snapshot;
            }

            // Get runtime values from locator
            switch (flag)
            {
                case WorldFlagBool_SO boolFlag:
                    snapshot.IsBoolFlag = true;
                    var boolRuntime = flagLocator.GetBoolFlag(boolFlag);
                    snapshot.BoolValue = boolRuntime?.Value ?? boolFlag.DefaultValue;
                    break;

                case WorldFlagInt_SO intFlag:
                    snapshot.IsBoolFlag = false;
                    var intRuntime = flagLocator.GetIntFlag(intFlag);
                    snapshot.IntValue = intRuntime?.Value ?? intFlag.DefaultValue;
                    break;
            }

            return snapshot;
        }
    }
}
