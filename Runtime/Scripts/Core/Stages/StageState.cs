namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Represents the current state of a quest stage.
    /// </summary>
    public enum StageState
    {
        /// <summary>
        /// Stage has not been reached yet.
        /// </summary>
        NotReached,

        /// <summary>
        /// Stage is currently active and its task groups are being executed.
        /// </summary>
        InProgress,

        /// <summary>
        /// Stage has been completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Stage has failed (e.g., a task group failed).
        /// </summary>
        Failed,

        /// <summary>
        /// Stage was skipped (e.g., via branching to a different path).
        /// </summary>
        Skipped
    }
}
