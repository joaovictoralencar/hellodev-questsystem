namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Defines when a stage transition should be triggered.
    /// </summary>
    public enum TransitionTrigger
    {
        /// <summary>
        /// Transition when all task groups in the stage are completed.
        /// This is the default behavior for linear quest progression.
        /// </summary>
        OnGroupsComplete,

        /// <summary>
        /// Transition immediately when the specified conditions are met.
        /// Useful for conditional branches or early exits.
        /// </summary>
        OnConditionsMet,

        /// <summary>
        /// Transition only via explicit API call (e.g., from dialogue, event, or external system).
        /// Useful for story-driven branching controlled by the narrative system.
        /// </summary>
        Manual
    }
}
