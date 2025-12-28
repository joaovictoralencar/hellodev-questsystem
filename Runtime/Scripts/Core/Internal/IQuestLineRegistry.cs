using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.Internal
{
    /// <summary>
    /// Internal interface for questline data storage and retrieval.
    /// </summary>
    internal interface IQuestLineRegistry
    {
        #region Database Operations

        /// <summary>
        /// Initializes the available questlines database from QuestLine_SO list.
        /// </summary>
        void InitializeDatabase(IEnumerable<QuestLine_SO> questLineData);

        /// <summary>
        /// Clears all runtime state (active, completed questlines).
        /// </summary>
        void ClearRuntimeState();

        /// <summary>
        /// Checks if a questline exists in the available database.
        /// </summary>
        bool IsInDatabase(Guid questLineId);

        #endregion

        #region Active QuestLines

        /// <summary>
        /// Adds a questline to the active questlines.
        /// </summary>
        bool AddActive(QuestLineRuntime questLine);

        /// <summary>
        /// Removes a questline from the active questlines.
        /// </summary>
        bool RemoveActive(Guid questLineId);

        /// <summary>
        /// Gets an active questline by ID.
        /// </summary>
        QuestLineRuntime GetActive(Guid questLineId);

        /// <summary>
        /// Checks if a questline is currently active.
        /// </summary>
        bool IsActive(Guid questLineId);

        /// <summary>
        /// Gets all active questlines.
        /// </summary>
        IReadOnlyCollection<QuestLineRuntime> GetAllActive();

        /// <summary>
        /// Gets the count of active questlines.
        /// </summary>
        int ActiveCount { get; }

        #endregion

        #region Completed QuestLines

        /// <summary>
        /// Adds a questline to the completed questlines.
        /// </summary>
        bool AddCompleted(QuestLineRuntime questLine);

        /// <summary>
        /// Gets a completed questline by ID.
        /// </summary>
        QuestLineRuntime GetCompleted(Guid questLineId);

        /// <summary>
        /// Checks if a questline has been completed.
        /// </summary>
        bool IsCompleted(Guid questLineId);

        /// <summary>
        /// Gets all completed questlines.
        /// </summary>
        IReadOnlyCollection<QuestLineRuntime> GetAllCompleted();

        /// <summary>
        /// Gets the count of completed questlines.
        /// </summary>
        int CompletedCount { get; }

        #endregion

        #region Movement Between States

        /// <summary>
        /// Moves a questline from active to completed.
        /// </summary>
        bool MoveToCompleted(Guid questLineId);

        #endregion
    }
}
