using System;
using System.Collections.Generic;

namespace HelloDev.QuestSystem.Tutorials.SaveLoad
{
    /// <summary>
    /// Complete snapshot of tutorial system state for save/load functionality.
    /// This is the top-level class used by TutorialManager.CaptureSnapshot/RestoreSnapshot.
    /// </summary>
    [Serializable]
    public class TutorialSystemSnapshot
    {
        /// <summary>
        /// Version of the snapshot format for future compatibility.
        /// </summary>
        public int Version = 1;

        /// <summary>
        /// IDs of all completed tutorials (for PlayOnce support).
        /// </summary>
        public List<string> CompletedTutorialIds = new();

        /// <summary>
        /// The currently active tutorial (if any).
        /// </summary>
        public TutorialSnapshot ActiveTutorial;

        /// <summary>
        /// Queued tutorials waiting to play.
        /// </summary>
        public List<TutorialSnapshot> QueuedTutorials = new();
    }

    /// <summary>
    /// Snapshot of a tutorial's state and progress.
    /// </summary>
    [Serializable]
    public class TutorialSnapshot
    {
        /// <summary>
        /// The GUID of the Tutorial_SO asset.
        /// </summary>
        public string TutorialGuid;

        /// <summary>
        /// The current state of the tutorial.
        /// </summary>
        public int State; // ObjectiveState as int

        /// <summary>
        /// The current step index.
        /// </summary>
        public int CurrentStepIndex;

        /// <summary>
        /// All step snapshots for this tutorial.
        /// </summary>
        public List<TutorialStepSnapshot> Steps = new();
    }

    /// <summary>
    /// Snapshot of a tutorial step's state and progress.
    /// </summary>
    [Serializable]
    public class TutorialStepSnapshot
    {
        /// <summary>
        /// The GUID of the TutorialStep_SO asset.
        /// </summary>
        public string StepGuid;

        /// <summary>
        /// The current state of the step.
        /// </summary>
        public int State; // ObjectiveState as int

        /// <summary>
        /// The elapsed time for timed steps.
        /// </summary>
        public float ElapsedTime;

        /// <summary>
        /// Current count for count-based steps (e.g., "killed 2/3 enemies").
        /// </summary>
        public int CurrentCount;

        /// <summary>
        /// List of completed substep GUIDs for multi-step tutorials.
        /// </summary>
        public List<string> CompletedSubstepIds = new();
    }
}
