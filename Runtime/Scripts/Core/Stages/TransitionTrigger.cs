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
        Manual,

        /// <summary>
        /// Transition requires player selection from available choices.
        /// The quest system fires OnChoicesAvailable; game code decides HOW to present choices.
        ///
        /// Common choice origins (handled by game code, not quest system):
        /// - UI Dialog: Traditional popup with choice buttons
        /// - Dialogue: NPC conversation responses ("I'll help you" vs "Not my problem")
        /// - Physical: Walking through different doors, interacting with objects
        /// - Combat: Killing vs sparing an enemy, choosing attack type
        /// - Implicit: Having the right item, meeting reputation threshold
        /// - Timed: Default choice after countdown expires
        ///
        /// Call QuestRuntime.SelectChoice() or SelectChoiceById() when player makes their selection.
        /// </summary>
        PlayerChoice
    }
}
