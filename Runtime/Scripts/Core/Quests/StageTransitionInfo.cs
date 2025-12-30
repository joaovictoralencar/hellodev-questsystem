namespace HelloDev.QuestSystem.Quests
{
    /// <summary>
    /// Contains information about a stage transition.
    /// Used by OnStageTransition event for clear, named parameters.
    /// </summary>
    public readonly struct StageTransitionInfo
    {
        /// <summary>
        /// The stage index before the transition (-1 if no previous stage).
        /// </summary>
        public int PreviousStageIndex { get; }

        /// <summary>
        /// The stage index after the transition.
        /// </summary>
        public int NewStageIndex { get; }

        public StageTransitionInfo(int previousStageIndex, int newStageIndex)
        {
            PreviousStageIndex = previousStageIndex;
            NewStageIndex = newStageIndex;
        }

        public override string ToString() => $"Stage {PreviousStageIndex} → {NewStageIndex}";
    }
}
