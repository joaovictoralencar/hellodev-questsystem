using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Contract for task group operations. Enables mocking in tests.
    /// Consumers can depend on this interface instead of the concrete TaskGroupRuntime class.
    /// </summary>
    /// <remarks>
    /// This interface extracts the public API of TaskGroupRuntime to enable:
    /// - Unit testing with mock implementations
    /// - Dependency injection patterns
    /// - Future alternative task group implementations
    /// </remarks>
    public interface ITaskGroup
    {
        #region Identity

        /// <summary>
        /// Gets the name of this task group.
        /// </summary>
        string GroupName { get; }

        /// <summary>
        /// Gets how tasks in this group are executed.
        /// </summary>
        TaskExecutionMode ExecutionMode { get; }

        /// <summary>
        /// For OptionalXofY mode: minimum number of tasks required to complete.
        /// </summary>
        int RequiredCount { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of this group.
        /// </summary>
        TaskGroupState CurrentState { get; }

        /// <summary>
        /// Gets the progress of this group (0-1).
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// Gets the number of completed tasks in this group.
        /// </summary>
        int CompletedTaskCount { get; }

        /// <summary>
        /// Gets the number of failed tasks in this group.
        /// </summary>
        int FailedTaskCount { get; }

        /// <summary>
        /// Gets the number of tasks still in progress or not started.
        /// </summary>
        int RemainingTaskCount { get; }

        #endregion

        #region Tasks

        /// <summary>
        /// Gets all runtime tasks in this group.
        /// </summary>
        IReadOnlyList<TaskRuntime> Tasks { get; }

        /// <summary>
        /// Gets all tasks that are currently in progress.
        /// </summary>
        IReadOnlyList<TaskRuntime> CurrentTasks { get; }

        /// <summary>
        /// Gets all tasks that are available to work on.
        /// </summary>
        IReadOnlyList<TaskRuntime> AvailableTasks { get; }

        /// <summary>
        /// Gets a task by its ID.
        /// </summary>
        TaskRuntime GetTask(Guid taskId);

        #endregion

        #region Lifecycle

        /// <summary>
        /// Starts this task group.
        /// </summary>
        void StartGroup();

        /// <summary>
        /// Completes the group successfully.
        /// </summary>
        void CompleteGroup();

        /// <summary>
        /// Fails the group.
        /// </summary>
        void FailGroup();

        /// <summary>
        /// Resets all tasks in the group to NotStarted state.
        /// </summary>
        void ResetGroup();

        #endregion

        #region Completion Checks

        /// <summary>
        /// Checks if the group completion criteria is met.
        /// </summary>
        bool CheckCompletion();

        /// <summary>
        /// Checks if completion has become impossible due to failures.
        /// </summary>
        bool IsCompletionImpossible();

        #endregion
    }
}
