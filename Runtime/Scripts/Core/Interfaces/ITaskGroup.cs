using System;
using System.Collections.Generic;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Contract for a task group – a collection of tasks that act as a single objective.
    /// Inherits core objective and group behaviour from <see cref="IObjectiveGroup"/>.
    /// </summary>
    public interface ITaskGroup : IObjectiveGroup
    {
        /// <summary>
        /// Gets the human‑readable name of this task group (for UI and debug).
        /// </summary>
        string GroupName { get; }

        /// <summary>
        /// Gets all runtime tasks in this group (strongly typed).
        /// </summary>
        IReadOnlyList<TaskRuntime> Tasks { get; }

        /// <summary>
        /// Gets a task by its unique identifier.
        /// </summary>
        TaskRuntime GetTask(Guid taskId);

        /// <summary>
        /// Checks whether the group’s completion criteria have been met.
        /// </summary>
        bool CheckCompletion();

        /// <summary>
        /// Checks whether completion of this group has become impossible
        /// (for example, too many tasks have failed).
        /// </summary>
        bool IsCompletionImpossible();
    }
}