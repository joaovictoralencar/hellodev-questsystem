using System;
using System.Collections.Generic;

namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents a group of objectives with execution mode logic.
    /// <para>
    /// An objective group contains multiple objectives and defines how they
    /// should be executed (sequential, parallel, any order, or X of Y).
    /// </para>
    /// <para>
    /// Implemented by: TaskGroupRuntime, AchievementRuntime
    /// </para>
    /// </summary>
    public interface IObjectiveGroup
    {
        #region Identity

        /// <summary>
        /// Gets the unique identifier for this group.
        /// </summary>
        string Id { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of the group.
        /// </summary>
        ObjectiveState State { get; }

        /// <summary>
        /// Gets the overall progress of this group as a value from 0.0 to 1.0.
        /// </summary>
        float Progress { get; }

        #endregion

        #region Objectives

        /// <summary>
        /// Gets the list of objectives in this group.
        /// </summary>
        IReadOnlyList<IObjective> Objectives { get; }

        /// <summary>
        /// Gets the execution mode for objectives in this group.
        /// </summary>
        ObjectiveExecutionMode ExecutionMode { get; }

        /// <summary>
        /// Gets the number of objectives required to complete this group.
        /// Used primarily for OptionalXOfY mode.
        /// </summary>
        int RequiredCount { get; }

        /// <summary>
        /// Gets the number of objectives that have been completed.
        /// </summary>
        int CompletedCount { get; }

        #endregion

        #region Events

        /// <summary>
        /// Fired when the group starts.
        /// </summary>
        event Action<IObjectiveGroup> OnStarted;

        /// <summary>
        /// Fired when the group's progress changes.
        /// </summary>
        event Action<IObjectiveGroup> OnProgressChanged;

        /// <summary>
        /// Fired when the group is completed.
        /// </summary>
        event Action<IObjectiveGroup> OnCompleted;

        /// <summary>
        /// Fired when the group fails.
        /// </summary>
        event Action<IObjectiveGroup> OnFailed;

        /// <summary>
        /// Fired when an individual objective within the group is completed.
        /// </summary>
        event Action<IObjectiveGroup, IObjective> OnObjectiveCompleted;

        #endregion
    }
}
