using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;

namespace HelloDev.QuestSystem.Internal
{
    /// <summary>
    /// Internal implementation of questline data storage.
    /// Manages the dictionaries of available, active, and completed questlines.
    /// </summary>
    internal class QuestLineRegistry : IQuestLineRegistry
    {
        #region Private Fields

        private readonly Dictionary<Guid, QuestLine_SO> _availableQuestLinesData = new();
        private readonly Dictionary<Guid, QuestLineRuntime> _activeQuestLines = new();
        private readonly Dictionary<Guid, QuestLineRuntime> _completedQuestLines = new();

        #endregion

        #region Properties

        public int ActiveCount => _activeQuestLines.Count;
        public int CompletedCount => _completedQuestLines.Count;
        public int DatabaseCount => _availableQuestLinesData.Count;

        #endregion

        #region Database Operations

        public void InitializeDatabase(IEnumerable<QuestLine_SO> questLineData)
        {
            _availableQuestLinesData.Clear();

            if (questLineData == null) return;

            foreach (QuestLine_SO data in questLineData)
            {
                if (data == null)
                {
                    QuestLogger.LogWarning("QuestLineRegistry: Null questline found in database, skipping.");
                    continue;
                }

                if (!_availableQuestLinesData.TryAdd(data.QuestLineId, data))
                {
                    QuestLogger.LogWarning($"QuestLineRegistry: Duplicate questline ID found for '{data.DevName}', skipping.");
                }
            }

            QuestLogger.Log($"QuestLineRegistry: Initialized with {_availableQuestLinesData.Count} questlines.");
        }

        public void ClearRuntimeState()
        {
            _activeQuestLines.Clear();
            _completedQuestLines.Clear();
        }

        public bool IsInDatabase(Guid questLineId)
        {
            return _availableQuestLinesData.ContainsKey(questLineId);
        }

        #endregion

        #region Active QuestLines

        public bool AddActive(QuestLineRuntime questLine)
        {
            if (questLine == null) return false;
            return _activeQuestLines.TryAdd(questLine.QuestLineId, questLine);
        }

        public bool RemoveActive(Guid questLineId)
        {
            return _activeQuestLines.Remove(questLineId);
        }

        public QuestLineRuntime GetActive(Guid questLineId)
        {
            _activeQuestLines.TryGetValue(questLineId, out QuestLineRuntime questLine);
            return questLine;
        }

        public bool IsActive(Guid questLineId)
        {
            return _activeQuestLines.ContainsKey(questLineId);
        }

        public IReadOnlyCollection<QuestLineRuntime> GetAllActive()
        {
            return _activeQuestLines.Values.ToList().AsReadOnly();
        }

        #endregion

        #region Completed QuestLines

        public bool AddCompleted(QuestLineRuntime questLine)
        {
            if (questLine == null) return false;
            return _completedQuestLines.TryAdd(questLine.QuestLineId, questLine);
        }

        public QuestLineRuntime GetCompleted(Guid questLineId)
        {
            _completedQuestLines.TryGetValue(questLineId, out QuestLineRuntime questLine);
            return questLine;
        }

        public bool IsCompleted(Guid questLineId)
        {
            return _completedQuestLines.ContainsKey(questLineId);
        }

        public IReadOnlyCollection<QuestLineRuntime> GetAllCompleted()
        {
            return _completedQuestLines.Values.ToList().AsReadOnly();
        }

        #endregion

        #region Movement Between States

        public bool MoveToCompleted(Guid questLineId)
        {
            if (!_activeQuestLines.TryGetValue(questLineId, out QuestLineRuntime questLine))
                return false;

            _activeQuestLines.Remove(questLineId);
            return _completedQuestLines.TryAdd(questLineId, questLine);
        }

        #endregion

        #region Internal Access (for QuestManager.Editor)

        /// <summary>
        /// Gets all active questlines as enumerable. Used internally for editor display.
        /// </summary>
        internal IEnumerable<QuestLineRuntime> ActiveQuestLinesEnumerable => _activeQuestLines.Values;

        /// <summary>
        /// Gets all completed questlines as enumerable. Used internally for editor display.
        /// </summary>
        internal IEnumerable<QuestLineRuntime> CompletedQuestLinesEnumerable => _completedQuestLines.Values;

        #endregion
    }
}
