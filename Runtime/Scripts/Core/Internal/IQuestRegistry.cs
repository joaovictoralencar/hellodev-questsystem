using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.Internal
{
    /// <summary>
    /// Internal interface for quest data storage and retrieval.
    /// Handles the dictionaries of active, completed, and failed quests.
    /// </summary>
    internal interface IQuestRegistry
    {
        #region Database Operations

        /// <summary>
        /// Initializes the available quests database from Quest_SO list.
        /// </summary>
        void InitializeDatabase(IEnumerable<Quest_SO> questData);

        /// <summary>
        /// Clears all runtime state (active, completed, failed quests).
        /// </summary>
        void ClearRuntimeState();

        /// <summary>
        /// Checks if a quest exists in the available database.
        /// </summary>
        bool IsInDatabase(Guid questId);

        /// <summary>
        /// Gets the Quest_SO from the database by ID.
        /// </summary>
        Quest_SO GetFromDatabase(Guid questId);

        #endregion

        #region Active Quests

        /// <summary>
        /// Adds a quest to the active quests.
        /// </summary>
        bool AddActive(QuestRuntime quest);

        /// <summary>
        /// Removes a quest from the active quests.
        /// </summary>
        bool RemoveActive(Guid questId);

        /// <summary>
        /// Gets an active quest by ID.
        /// </summary>
        QuestRuntime GetActive(Guid questId);

        /// <summary>
        /// Checks if a quest is currently active.
        /// </summary>
        bool IsActive(Guid questId);

        /// <summary>
        /// Gets all active quests.
        /// </summary>
        IReadOnlyCollection<QuestRuntime> GetAllActive();

        /// <summary>
        /// Gets the count of active quests.
        /// </summary>
        int ActiveCount { get; }

        #endregion

        #region Completed Quests

        /// <summary>
        /// Adds a quest to the completed quests.
        /// </summary>
        bool AddCompleted(QuestRuntime quest);

        /// <summary>
        /// Removes a quest from the completed quests.
        /// </summary>
        bool RemoveCompleted(Guid questId);

        /// <summary>
        /// Gets a completed quest by ID.
        /// </summary>
        QuestRuntime GetCompleted(Guid questId);

        /// <summary>
        /// Checks if a quest has been completed.
        /// </summary>
        bool IsCompleted(Guid questId);

        /// <summary>
        /// Gets all completed quests.
        /// </summary>
        IReadOnlyCollection<QuestRuntime> GetAllCompleted();

        /// <summary>
        /// Gets the count of completed quests.
        /// </summary>
        int CompletedCount { get; }

        #endregion

        #region Failed Quests

        /// <summary>
        /// Adds a quest to the failed quests.
        /// </summary>
        bool AddFailed(QuestRuntime quest);

        /// <summary>
        /// Removes a quest from the failed quests.
        /// </summary>
        bool RemoveFailed(Guid questId);

        /// <summary>
        /// Gets a failed quest by ID.
        /// </summary>
        QuestRuntime GetFailed(Guid questId);

        /// <summary>
        /// Checks if a quest has failed.
        /// </summary>
        bool IsFailed(Guid questId);

        /// <summary>
        /// Gets all failed quests.
        /// </summary>
        IReadOnlyCollection<QuestRuntime> GetAllFailed();

        /// <summary>
        /// Gets the count of failed quests.
        /// </summary>
        int FailedCount { get; }

        #endregion

        #region Movement Between States

        /// <summary>
        /// Moves a quest from active to completed.
        /// </summary>
        bool MoveToCompleted(Guid questId);

        /// <summary>
        /// Moves a quest from active to failed.
        /// </summary>
        bool MoveToFailed(Guid questId);

        /// <summary>
        /// Moves a quest from completed back to active.
        /// </summary>
        bool MoveFromCompletedToActive(Guid questId);

        /// <summary>
        /// Moves a quest from failed back to active.
        /// </summary>
        bool MoveFromFailedToActive(Guid questId);

        #endregion
    }
}
