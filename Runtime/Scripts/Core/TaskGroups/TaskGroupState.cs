namespace HelloDev.QuestSystem.TaskGroups
{
    /// <summary>
    /// Represents the current state of a task group at runtime.
    /// </summary>
    public enum TaskGroupState
    {
        /// <summary>
        /// Group has not been started yet.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Group is currently active with tasks in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// Group has been successfully completed (all required tasks done).
        /// </summary>
        Completed,

        /// <summary>
        /// Group has failed (completion became impossible).
        /// </summary>
        Failed
    }
}
