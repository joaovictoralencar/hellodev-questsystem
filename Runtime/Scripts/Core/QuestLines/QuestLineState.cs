namespace HelloDev.QuestSystem.QuestLines
{
    /// <summary>
    /// Represents the possible states of a QuestLine.
    /// </summary>
    public enum QuestLineState
    {
        /// <summary>
        /// QuestLine is locked. Prerequisite line not completed.
        /// </summary>
        Locked,

        /// <summary>
        /// QuestLine is available and can be started.
        /// </summary>
        Available,

        /// <summary>
        /// QuestLine is in progress. At least one quest has been started.
        /// </summary>
        InProgress,

        /// <summary>
        /// QuestLine is completed. All quests have been completed.
        /// </summary>
        Completed,

        /// <summary>
        /// QuestLine has failed. A quest failed and the line is non-recoverable.
        /// </summary>
        Failed
    }
}
