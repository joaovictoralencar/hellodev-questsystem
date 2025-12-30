using System;
using System.Collections.Generic;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Complete snapshot of the quest system state for save/load functionality.
    /// Captures all active, completed, and failed quests along with their progress.
    /// </summary>
    [Serializable]
    public class QuestSystemSnapshot
    {
        /// <summary>
        /// Version of the snapshot format for future compatibility.
        /// </summary>
        public int Version = 1;

        /// <summary>
        /// UTC timestamp when the snapshot was captured.
        /// </summary>
        public string Timestamp;

        /// <summary>
        /// All active quest snapshots.
        /// </summary>
        public List<QuestSnapshot> ActiveQuests = new();

        /// <summary>
        /// All completed quest snapshots.
        /// </summary>
        public List<QuestSnapshot> CompletedQuests = new();

        /// <summary>
        /// All failed quest snapshots.
        /// </summary>
        public List<QuestSnapshot> FailedQuests = new();

        /// <summary>
        /// All active questline snapshots.
        /// </summary>
        public List<QuestLineSnapshot> ActiveQuestLines = new();

        /// <summary>
        /// All completed questline snapshots.
        /// </summary>
        public List<QuestLineSnapshot> CompletedQuestLines = new();

        /// <summary>
        /// All world flag snapshots.
        /// </summary>
        public List<WorldFlagSnapshot> WorldFlags = new();
    }

    /// <summary>
    /// Snapshot of a single quest's state and progress.
    /// </summary>
    [Serializable]
    public class QuestSnapshot
    {
        /// <summary>
        /// The GUID of the Quest_SO asset.
        /// </summary>
        public string QuestGuid;

        /// <summary>
        /// The current state of the quest.
        /// </summary>
        public int State; // QuestState as int

        /// <summary>
        /// The current stage index.
        /// </summary>
        public int CurrentStageIndex;

        /// <summary>
        /// Branch decisions made during this quest.
        /// Key: branch point ID, Value: chosen option ID.
        /// </summary>
        public List<BranchDecisionEntry> BranchDecisions = new();

        /// <summary>
        /// All task snapshots for this quest.
        /// </summary>
        public List<TaskSnapshot> Tasks = new();
    }

    /// <summary>
    /// Key-value pair for branch decisions (needed because Dictionary isn't directly serializable).
    /// </summary>
    [Serializable]
    public class BranchDecisionEntry
    {
        public string Key;
        public string Value;

        public BranchDecisionEntry() { }

        public BranchDecisionEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Snapshot of a single task's state and progress.
    /// </summary>
    [Serializable]
    public class TaskSnapshot
    {
        /// <summary>
        /// The GUID of the Task_SO asset.
        /// </summary>
        public string TaskGuid;

        /// <summary>
        /// The current state of the task.
        /// </summary>
        public int State; // TaskState as int

        /// <summary>
        /// The type of task for polymorphic restoration.
        /// </summary>
        public string TaskType;

        /// <summary>
        /// Type-specific data serialized as JSON or simple values.
        /// </summary>
        public TaskProgressData ProgressData = new();
    }

    /// <summary>
    /// Type-specific task progress data.
    /// Uses nullable fields to support different task types.
    /// </summary>
    [Serializable]
    public class TaskProgressData
    {
        /// <summary>
        /// For IntTaskRuntime: current count.
        /// </summary>
        public int IntValue;

        /// <summary>
        /// For TimedTaskRuntime: remaining time in seconds.
        /// </summary>
        public float FloatValue;

        /// <summary>
        /// For BoolTaskRuntime, LocationTaskRuntime, TimedTaskRuntime: completion flag.
        /// </summary>
        public bool BoolValue;

        /// <summary>
        /// For StringTaskRuntime: collected string value.
        /// </summary>
        public string StringValue;

        /// <summary>
        /// For DiscoveryTaskRuntime: names of fulfilled conditions (legacy).
        /// </summary>
        public List<string> FulfilledConditionGuids = new();

        /// <summary>
        /// For DiscoveryTaskRuntime: indices of fulfilled conditions.
        /// </summary>
        public List<int> FulfilledConditionIndices = new();
    }

    /// <summary>
    /// Snapshot of a questline's state.
    /// </summary>
    [Serializable]
    public class QuestLineSnapshot
    {
        /// <summary>
        /// The GUID of the QuestLine_SO asset.
        /// </summary>
        public string QuestLineGuid;

        /// <summary>
        /// The current state of the questline.
        /// </summary>
        public int State; // QuestLineState as int

        /// <summary>
        /// Number of completed quests in this line.
        /// </summary>
        public int CompletedQuestsCount;

        /// <summary>
        /// Whether the questline has been started.
        /// </summary>
        public bool HasStarted;
    }

    /// <summary>
    /// Snapshot of a world flag's value.
    /// </summary>
    [Serializable]
    public class WorldFlagSnapshot
    {
        /// <summary>
        /// The GUID or asset path of the WorldFlag_SO.
        /// </summary>
        public string FlagGuid;

        /// <summary>
        /// Whether this is a boolean flag (true) or integer flag (false).
        /// </summary>
        public bool IsBoolFlag;

        /// <summary>
        /// The boolean value (for WorldFlagBool_SO).
        /// </summary>
        public bool BoolValue;

        /// <summary>
        /// The integer value (for WorldFlagInt_SO).
        /// </summary>
        public int IntValue;
    }
}
