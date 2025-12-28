using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;

namespace HelloDev.QuestSystem.Internal
{
    /// <summary>
    /// Internal implementation of quest data storage.
    /// Manages the dictionaries of available, active, completed, and failed quests.
    /// </summary>
    internal class QuestRegistry : IQuestRegistry
    {
        #region Private Fields

        private readonly Dictionary<Guid, Quest_SO> _availableQuestsData = new();
        private readonly Dictionary<Guid, QuestRuntime> _activeQuests = new();
        private readonly Dictionary<Guid, QuestRuntime> _completedQuests = new();
        private readonly Dictionary<Guid, QuestRuntime> _failedQuests = new();

        #endregion

        #region Properties

        public int ActiveCount => _activeQuests.Count;
        public int CompletedCount => _completedQuests.Count;
        public int FailedCount => _failedQuests.Count;
        public int DatabaseCount => _availableQuestsData.Count;

        #endregion

        #region Database Operations

        public void InitializeDatabase(IEnumerable<Quest_SO> questData)
        {
            _availableQuestsData.Clear();

            if (questData == null) return;

            foreach (Quest_SO data in questData)
            {
                if (data == null)
                {
                    QuestLogger.LogWarning("QuestRegistry: Null quest found in database, skipping.");
                    continue;
                }

                if (!_availableQuestsData.TryAdd(data.QuestId, data))
                {
                    QuestLogger.LogWarning($"QuestRegistry: Duplicate quest ID found for '{data.DevName}', skipping.");
                }
            }

            QuestLogger.Log($"QuestRegistry: Initialized with {_availableQuestsData.Count} quests.");
        }

        public void ClearRuntimeState()
        {
            _activeQuests.Clear();
            _completedQuests.Clear();
            _failedQuests.Clear();
        }

        public bool IsInDatabase(Guid questId)
        {
            return _availableQuestsData.ContainsKey(questId);
        }

        public Quest_SO GetFromDatabase(Guid questId)
        {
            _availableQuestsData.TryGetValue(questId, out Quest_SO data);
            return data;
        }

        #endregion

        #region Active Quests

        public bool AddActive(QuestRuntime quest)
        {
            if (quest == null) return false;
            return _activeQuests.TryAdd(quest.QuestId, quest);
        }

        public bool RemoveActive(Guid questId)
        {
            return _activeQuests.Remove(questId);
        }

        public QuestRuntime GetActive(Guid questId)
        {
            _activeQuests.TryGetValue(questId, out QuestRuntime quest);
            return quest;
        }

        public bool IsActive(Guid questId)
        {
            return _activeQuests.ContainsKey(questId);
        }

        public IReadOnlyCollection<QuestRuntime> GetAllActive()
        {
            return _activeQuests.Values.ToList().AsReadOnly();
        }

        #endregion

        #region Completed Quests

        public bool AddCompleted(QuestRuntime quest)
        {
            if (quest == null) return false;
            return _completedQuests.TryAdd(quest.QuestId, quest);
        }

        public bool RemoveCompleted(Guid questId)
        {
            return _completedQuests.Remove(questId);
        }

        public QuestRuntime GetCompleted(Guid questId)
        {
            _completedQuests.TryGetValue(questId, out QuestRuntime quest);
            return quest;
        }

        public bool IsCompleted(Guid questId)
        {
            return _completedQuests.ContainsKey(questId);
        }

        public IReadOnlyCollection<QuestRuntime> GetAllCompleted()
        {
            return _completedQuests.Values.ToList().AsReadOnly();
        }

        #endregion

        #region Failed Quests

        public bool AddFailed(QuestRuntime quest)
        {
            if (quest == null) return false;
            return _failedQuests.TryAdd(quest.QuestId, quest);
        }

        public bool RemoveFailed(Guid questId)
        {
            return _failedQuests.Remove(questId);
        }

        public QuestRuntime GetFailed(Guid questId)
        {
            _failedQuests.TryGetValue(questId, out QuestRuntime quest);
            return quest;
        }

        public bool IsFailed(Guid questId)
        {
            return _failedQuests.ContainsKey(questId);
        }

        public IReadOnlyCollection<QuestRuntime> GetAllFailed()
        {
            return _failedQuests.Values.ToList().AsReadOnly();
        }

        #endregion

        #region Movement Between States

        public bool MoveToCompleted(Guid questId)
        {
            if (!_activeQuests.TryGetValue(questId, out QuestRuntime quest))
                return false;

            _activeQuests.Remove(questId);
            return _completedQuests.TryAdd(questId, quest);
        }

        public bool MoveToFailed(Guid questId)
        {
            if (!_activeQuests.TryGetValue(questId, out QuestRuntime quest))
                return false;

            _activeQuests.Remove(questId);
            return _failedQuests.TryAdd(questId, quest);
        }

        public bool MoveFromCompletedToActive(Guid questId)
        {
            if (!_completedQuests.TryGetValue(questId, out QuestRuntime quest))
                return false;

            _completedQuests.Remove(questId);
            return _activeQuests.TryAdd(questId, quest);
        }

        public bool MoveFromFailedToActive(Guid questId)
        {
            if (!_failedQuests.TryGetValue(questId, out QuestRuntime quest))
                return false;

            _failedQuests.Remove(questId);
            return _activeQuests.TryAdd(questId, quest);
        }

        #endregion

        #region Internal Access (for QuestManager.Editor)

        /// <summary>
        /// Gets all active quests as a list. Used internally for editor display.
        /// </summary>
        internal IEnumerable<QuestRuntime> ActiveQuestsEnumerable => _activeQuests.Values;

        /// <summary>
        /// Gets all completed quests as a list. Used internally for editor display.
        /// </summary>
        internal IEnumerable<QuestRuntime> CompletedQuestsEnumerable => _completedQuests.Values;

        /// <summary>
        /// Gets all failed quests as a list. Used internally for editor display.
        /// </summary>
        internal IEnumerable<QuestRuntime> FailedQuestsEnumerable => _failedQuests.Values;

        #endregion
    }
}
