namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Type of change that occurred to quest data.
    /// Used by OnQuestDataChanged event for auto-save triggers.
    /// </summary>
    public enum QuestDataChangeType
    {
        /// <summary>A quest was added to tracking.</summary>
        QuestAdded,

        /// <summary>A quest was started.</summary>
        QuestStarted,

        /// <summary>A quest was completed.</summary>
        QuestCompleted,

        /// <summary>A quest failed.</summary>
        QuestFailed,

        /// <summary>A quest was restarted.</summary>
        QuestRestarted,

        /// <summary>Quest progress updated (task completed, etc.).</summary>
        QuestUpdated,

        /// <summary>A questline was added.</summary>
        QuestLineAdded,

        /// <summary>A questline was started.</summary>
        QuestLineStarted,

        /// <summary>A questline was completed.</summary>
        QuestLineCompleted,

        /// <summary>A questline failed.</summary>
        QuestLineFailed,

        /// <summary>A world flag value changed.</summary>
        WorldFlagChanged,

        /// <summary>A branch choice was made.</summary>
        BranchChoiceMade
    }
}
