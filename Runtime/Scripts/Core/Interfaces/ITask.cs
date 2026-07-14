using System;
using HelloDev.Objectives;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Contract for task operations. Inherits core objective state and lifecycle
    /// from <see cref="IObjective"/> to remove duplicate members.
    /// </summary>
    public interface ITask : IObjective
    {
        #region Identity

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

        #region Task‑specific lifecycle

        /// <summary>
        /// Forces the task parameters to a completed state (bypassing normal progression).
        /// </summary>
        void ForceCompleteState();

        #endregion

        #region Step‑based progress (task‑specific)

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