using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Utility class for validating quest system snapshots before restoration.
    /// Extracted from QuestSaveManager for single responsibility.
    /// </summary>
    public static class SnapshotValidator
    {
        /// <summary>
        /// Validates a complete snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to validate.</param>
        /// <param name="findQuestByGuid">Function to find Quest_SO by GUID.</param>
        /// <param name="findQuestLineByGuid">Function to find QuestLine_SO by GUID.</param>
        /// <param name="allWorldFlags">All registered world flags.</param>
        /// <returns>Validation result with any issues found.</returns>
        public static SnapshotValidationResult Validate(
            QuestSystemSnapshot snapshot,
            Func<string, Quest_SO> findQuestByGuid,
            Func<string, QuestLine_SO> findQuestLineByGuid,
            List<WorldFlagBase_SO> allWorldFlags)
        {
            var result = new SnapshotValidationResult();

            if (snapshot == null)
            {
                result.AddCritical("Snapshot", "Snapshot is null.");
                return result;
            }

            // Validate version
            if (snapshot.Version > 1)
            {
                result.AddWarning("Version", $"Snapshot version {snapshot.Version} is newer than supported version 1.");
            }

            // Validate quests
            ValidateQuests(snapshot.ActiveQuests, "Active", result, findQuestByGuid);
            ValidateQuests(snapshot.CompletedQuests, "Completed", result, findQuestByGuid);
            ValidateQuests(snapshot.FailedQuests, "Failed", result, findQuestByGuid);

            // Validate questlines
            ValidateQuestLines(snapshot.ActiveQuestLines, result, findQuestLineByGuid);
            ValidateQuestLines(snapshot.CompletedQuestLines, result, findQuestLineByGuid);

            // Validate world flags
            ValidateWorldFlags(snapshot.WorldFlags, result, allWorldFlags);

            return result;
        }

        /// <summary>
        /// Validates a list of quest snapshots.
        /// </summary>
        public static void ValidateQuests(
            List<QuestSnapshot> quests,
            string category,
            SnapshotValidationResult result,
            Func<string, Quest_SO> findQuestByGuid)
        {
            foreach (var questSnapshot in quests)
            {
                var questData = findQuestByGuid(questSnapshot.QuestGuid);
                if (questData == null)
                {
                    result.AddError("Quest", $"{category} quest not found in database.", questSnapshot.QuestGuid);
                    continue;
                }

                // Validate tasks
                foreach (var taskSnapshot in questSnapshot.Tasks)
                {
                    bool taskFound = questData.AllTasks.Any(t => t.TaskId.ToString() == taskSnapshot.TaskGuid);
                    if (!taskFound)
                    {
                        result.AddWarning("Task", $"Task not found in quest '{questData.name}'.", taskSnapshot.TaskGuid);
                    }
                }

                // Validate stage index
                if (questSnapshot.CurrentStageIndex >= questData.Stages.Count)
                {
                    result.AddWarning("Stage", $"Stage index {questSnapshot.CurrentStageIndex} out of range for quest.", questData.name);
                }
            }
        }

        /// <summary>
        /// Validates a list of questline snapshots.
        /// </summary>
        public static void ValidateQuestLines(
            List<QuestLineSnapshot> questLines,
            SnapshotValidationResult result,
            Func<string, QuestLine_SO> findQuestLineByGuid)
        {
            foreach (var lineSnapshot in questLines)
            {
                var lineData = findQuestLineByGuid(lineSnapshot.QuestLineGuid);
                if (lineData == null)
                {
                    result.AddError("QuestLine", "QuestLine not found in database.", lineSnapshot.QuestLineGuid);
                }
            }
        }

        /// <summary>
        /// Validates a list of world flag snapshots.
        /// </summary>
        public static void ValidateWorldFlags(
            List<WorldFlagSnapshot> flags,
            SnapshotValidationResult result,
            List<WorldFlagBase_SO> allFlags)
        {
            foreach (var flagSnapshot in flags)
            {
                var flag = allFlags.Find(f => f != null && f.FlagId == flagSnapshot.FlagGuid);
                if (flag == null)
                {
                    result.AddWarning("WorldFlag", "World flag not found in registry.", flagSnapshot.FlagGuid);
                }
            }

            if (allFlags.Count == 0 && flags.Count > 0)
            {
                result.AddWarning("WorldFlag", $"No world flags registered but snapshot contains {flags.Count} flags. Register flags with SetWorldFlagRegistry() or RegisterWorldFlag().");
            }
        }
    }
}
