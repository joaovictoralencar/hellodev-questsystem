using System;
using System.Collections.Generic;

namespace HelloDev.Objectives
{
    /// <summary>
    /// Represents a mission - a sequence of stages with transitions.
    /// <para>
    /// A mission is the top-level container that groups stages together.
    /// It tracks overall progress and manages stage transitions.
    /// </para>
    /// <para>
    /// Implemented by: QuestRuntime, TutorialRuntime
    /// </para>
    /// </summary>
    public interface IMission
    {
        #region Identity

        /// <summary>
        /// Gets the unique identifier for this mission instance.
        /// </summary>
        Guid MissionId { get; }

        /// <summary>
        /// Gets the display name of this mission.
        /// </summary>
        string DisplayName { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of the mission.
        /// </summary>
        ObjectiveState State { get; }

        /// <summary>
        /// Gets the overall progress of this mission as a value from 0.0 to 1.0.
        /// </summary>
        float Progress { get; }

        #endregion

        #region Stages

        /// <summary>
        /// Gets all stages in this mission.
        /// </summary>
        IReadOnlyList<IStage> Stages { get; }

        /// <summary>
        /// Gets the currently active stage, or null if no stage is active.
        /// </summary>
        IStage CurrentStage { get; }

        /// <summary>
        /// Gets the index of the current stage, or -1 if no stage is active.
        /// </summary>
        int CurrentStageIndex { get; }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Starts the mission, entering the first stage.
        /// </summary>
        void Start();

        /// <summary>
        /// Completes the mission successfully.
        /// </summary>
        void Complete();

        /// <summary>
        /// Fails the mission.
        /// </summary>
        void Fail();

        /// <summary>
        /// Resets the mission to its initial state.
        /// </summary>
        void Reset();

        #endregion

        #region Events

        /// <summary>
        /// Fired when the mission starts.
        /// </summary>
        event Action<IMission> OnStarted;

        /// <summary>
        /// Fired when the mission's progress changes.
        /// </summary>
        event Action<IMission> OnProgressChanged;

        /// <summary>
        /// Fired when the mission is completed successfully.
        /// </summary>
        event Action<IMission> OnCompleted;

        /// <summary>
        /// Fired when the mission fails.
        /// </summary>
        event Action<IMission> OnFailed;

        /// <summary>
        /// Fired when a stage is entered.
        /// </summary>
        event Action<IMission, IStage> OnStageEntered;

        /// <summary>
        /// Fired when a stage is completed.
        /// </summary>
        event Action<IMission, IStage> OnStageCompleted;

        #endregion
    }
}
