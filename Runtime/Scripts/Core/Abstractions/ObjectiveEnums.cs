namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents the current state of an objective, group, stage, or mission.
    /// </summary>
    public enum State
    {
        /// <summary>The objective has not been started yet.</summary>
        NotStarted,

        /// <summary>The objective is currently in progress.</summary>
        InProgress,

        /// <summary>The objective has been completed successfully.</summary>
        Completed,

        /// <summary>The objective has failed.</summary>
        Failed
    }

    /// <summary>
    /// Defines how objectives within a group are executed.
    /// </summary>
    public enum ObjectiveExecutionMode
    {
        /// <summary>Objectives must be completed one at a time, in order.</summary>
        Sequential,

        /// <summary>All objectives are active at once and can be completed in any order.</summary>
        Parallel,

        /// <summary>Objectives are completed one at a time, but player chooses the order.</summary>
        AnyOrder,

        /// <summary>Complete X of Y objectives (RequiredCount specifies how many).</summary>
        OptionalXOfY
    }
}
