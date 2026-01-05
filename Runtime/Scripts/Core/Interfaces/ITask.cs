using System;
using HelloDev.QuestSystem.Tasks;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Contract for task operations. Enables mocking in tests and alternative task implementations.
    /// Consumers can depend on this interface instead of the concrete TaskRuntime class.
    /// </summary>
    /// <remarks>
    /// This interface extracts the public API of TaskRuntime to enable:
    /// - Unit testing with mock implementations
    /// - Dependency injection patterns
    /// - Future alternative task implementations
    /// </remarks>
    public interface ITask
    {
        #region Identity

        /// <summary>
        /// Gets the unique identifier for this task.
        /// </summary>
        Guid TaskId { get; }

        /// <summary>
        /// Gets the developer-friendly name for internal identification.
        /// </summary>
        string DevName { get; }

        /// <summary>
        /// Gets the localized display name for UI.
        /// </summary>
        LocalizedString DisplayName { get; }

        /// <summary>
        /// Gets the localized description of the task.
        /// </summary>
        LocalizedString Description { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of the task (NotStarted, InProgress, Completed, Failed).
        /// </summary>
        TaskState CurrentState { get; }

        /// <summary>
        /// Gets the progress of the task (0-1).
        /// </summary>
        float Progress { get; }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Starts the task, changing its state to InProgress.
        /// </summary>
        void StartTask();

        /// <summary>
        /// Marks the task as completed.
        /// </summary>
        void CompleteTask();

        /// <summary>
        /// Marks the task as failed.
        /// </summary>
        void FailTask();

        /// <summary>
        /// Resets the task to its initial NotStarted state.
        /// </summary>
        void ResetTask();

        /// <summary>
        /// Forces the task parameters to a completed state.
        /// </summary>
        void ForceCompleteState();

        #endregion

        #region Progress Methods

        /// <summary>
        /// Increments the task's step/progress.
        /// </summary>
        void IncrementStep();

        /// <summary>
        /// Decrements the task's step/progress.
        /// </summary>
        void DecrementStep();

        #endregion
    }
}
