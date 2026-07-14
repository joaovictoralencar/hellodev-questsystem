using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents a group of objectives that collectively behave as a single objective.
    /// <para>
    /// An objective group contains multiple <see cref="IObjective"/> children and defines
    /// how they are executed (sequential, parallel, any order, X of Y). The group itself
    /// is an <see cref="IObjective"/>—its state and progress derive from its children.
    /// </para>
    /// <para>
    /// Implemented by: TaskGroupRuntime, AchievementRuntime
    /// </para>
    /// </summary>
    public interface IObjectiveGroup : IObjective
    {
        #region Children

        /// <summary>
        /// Gets the list of child objectives in this group.
        /// </summary>
        IReadOnlyList<IObjective> Objectives { get; }

        /// <summary>
        /// Gets the execution mode that governs how children are completed.
        /// </summary>
        ObjectiveExecutionMode ExecutionMode { get; }

        /// <summary>
        /// Gets the number of child objectives that must be completed to finish the group.
        /// Relevant mainly for <see cref="ObjectiveExecutionMode.OptionalXOfY"/>.
        /// </summary>
        int RequiredCount { get; }

        /// <summary>
        /// Gets the number of child objectives that have been completed.
        /// </summary>
        int CompletedCount { get; }

        #endregion

        #region Child events

        /// <summary>
        /// Fired when an individual objective within the group is completed.
        /// </summary>
        UnityEvent<IObjectiveGroup, IObjective> OnObjectiveCompleted { get; set; }

        #endregion
    }
}