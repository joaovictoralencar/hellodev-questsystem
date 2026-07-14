using System;
using System.Collections.Generic;

namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents a stage or phase within a mission.
    /// <para>
    /// A stage contains objective groups and defines a distinct phase of progress.
    /// Stages can be terminal (end points), optional, or hidden from the player.
    /// </para>
    /// <para>
    /// Implemented by: QuestStageRuntime, TutorialStepRuntime
    /// </para>
    /// </summary>
    public interface IStage
    {
        #region Identity

        /// <summary>
        /// Gets the index of this stage within its parent mission.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the unique identifier for this stage.
        /// </summary>
        string Name { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of the stage.
        /// </summary>
        State State { get; }

        /// <summary>
        /// Gets the progress of this stage as a value from 0.0 to 1.0.
        /// </summary>
        float Progress { get; }

        #endregion

        #region Content

        /// <summary>
        /// Gets the objective groups contained in this stage.
        /// Note: Some stage implementations (like tutorial steps) may return an empty list.
        /// </summary>
        IReadOnlyList<IObjectiveGroup> ObjectiveGroups { get; }

        #endregion

        #region Stage Properties

        /// <summary>
        /// Gets whether this stage is a terminal (end) stage.
        /// Terminal stages complete the mission when reached.
        /// </summary>
        bool IsTerminal { get; }

        /// <summary>
        /// Gets whether this stage is optional.
        /// Optional stages can be skipped without affecting mission completion.
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// Gets whether this stage is hidden from the player.
        /// Hidden stages don't appear in UI but still execute.
        /// </summary>
        bool IsHidden { get; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when the stage is entered (becomes active).
        /// </summary>
        event Action<IStage> OnEntered;

        /// <summary>
        /// Fired when the stage's progress changes.
        /// </summary>
        event Action<IStage> OnProgressChanged;

        /// <summary>
        /// Fired when the stage is completed.
        /// </summary>
        event Action<IStage> OnCompleted;

        /// <summary>
        /// Fired when the stage fails.
        /// </summary>
        event Action<IStage> OnFailed;

        /// <summary>
        /// Fired when the stage is exited (no longer active).
        /// </summary>
        event Action<IStage> OnExited;

        #endregion
    }
}
