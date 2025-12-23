namespace HelloDev.QuestSystem.TaskGroups
{
    /// <summary>
    /// Defines how tasks within a group are executed.
    /// </summary>
    public enum TaskExecutionMode
    {
        /// <summary>
        /// Tasks must be completed in order. Next task starts only when current completes.
        /// This is the default behavior matching existing quest functionality.
        /// </summary>
        Sequential,

        /// <summary>
        /// All tasks start immediately and can be completed in any order.
        /// Group completes when ALL tasks are completed.
        /// </summary>
        Parallel,

        /// <summary>
        /// All tasks are available from the start and can be completed in any order.
        /// Similar to Parallel but semantically indicates player choice in order.
        /// Group completes when ALL tasks are completed.
        /// </summary>
        AnyOrder,

        /// <summary>
        /// Complete X of Y tasks. Not all tasks need to be completed.
        /// Uses RequiredCount to specify minimum required completions.
        /// </summary>
        OptionalXofY
    }
}
