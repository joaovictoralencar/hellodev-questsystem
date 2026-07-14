using System;
using UnityEngine.Events;

namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents a single trackable objective - the fundamental unit of progress.
    /// <para>
    /// An objective is something the player must accomplish. It has a state,
    /// tracks progress from 0.0 to 1.0, and fires events when its state changes.
    /// </para>
    /// <para>
    /// Implemented by: TaskRuntime (and all task subtypes like IntTaskRuntime, BoolTaskRuntime, etc.)
    /// </para>
    /// </summary>
    public interface IObjective
    {
        #region Identity

        /// <summary>
        /// Gets the unique identifier for this objective.
        /// </summary>
        Guid Id { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of the objective.
        /// </summary>
        State State { get; }

        /// <summary>
        /// Gets the progress of this objective as a value from 0.0 (not started) to 1.0 (complete).
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// Gets whether this objective has been completed successfully.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Gets whether this objective has failed.
        /// </summary>
        bool IsFailed { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Starts the objective, transitioning it from NotStarted to InProgress.
        /// </summary>
        void Start();

        /// <summary>
        /// Completes the objective successfully.
        /// </summary>
        void Complete();

        /// <summary>
        /// Fails the objective.
        /// </summary>
        void Fail();

        /// <summary>
        /// Resets the objective to its initial state.
        /// </summary>
        void Reset();

        #endregion

        #region Events

        /// <summary>
        /// Fired when the objective starts (transitions to InProgress).
        /// </summary>
        UnityEvent<IObjective> Started { get; set; }

        /// <summary>
        /// Fired when the objective's progress changes.
        /// </summary>
        UnityEvent<IObjective> ProgressChanged { get; set; }

        /// <summary>
        /// Fired when the objective is completed successfully.
        /// </summary>
        UnityEvent<IObjective> Completed { get; set; }

        /// <summary>
        /// Fired when the objective fails.
        /// </summary>
        UnityEvent<IObjective> Failed { get; set; }
        
        /// <summary>
        /// Fired when the objective is Updated.
        /// </summary>
        UnityEvent<IObjective> Updated { get; set; }

        #endregion
    }
}
