using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.QuestLines
{
    /// <summary>
    /// Runtime representation of a QuestLine. Tracks progress across all quests
    /// in the line and fires events when state changes.
    /// </summary>
    public class QuestLineRuntime
    {
        #region Events

        /// <summary>Fired when the questline starts (first quest in line starts).</summary>
        public UnityEvent<QuestLineRuntime> OnQuestLineStarted = new();

        /// <summary>Fired when questline progress changes (quest completes/fails).</summary>
        public UnityEvent<QuestLineRuntime> OnQuestLineUpdated = new();

        /// <summary>Fired when all quests in the line are completed.</summary>
        public UnityEvent<QuestLineRuntime> OnQuestLineCompleted = new();

        /// <summary>Fired when any quest in the line completes.</summary>
        public UnityEvent<QuestLineRuntime, QuestRuntime> OnQuestInLineCompleted = new();

        /// <summary>Fired when the questline fails (a quest fails and line is non-recoverable).</summary>
        public UnityEvent<QuestLineRuntime> OnQuestLineFailed = new();

        #endregion

        #region Properties

        /// <summary>Gets the unique identifier for this questline.</summary>
        public Guid QuestLineId { get; }

        /// <summary>Gets the ScriptableObject data for this questline.</summary>
        public QuestLine_SO Data { get; }

        /// <summary>Gets the current state of this questline.</summary>
        public QuestLineState CurrentState { get; private set; }

        /// <summary>Gets the progress (0.0 to 1.0) of this questline.</summary>
        public float Progress => CalculateProgress();

        /// <summary>Gets the number of completed quests in this line.</summary>
        public int CompletedQuestCount => GetCompletedCount();

        /// <summary>Gets the total number of quests in this line.</summary>
        public int TotalQuestCount => Data.QuestCount;

        /// <summary>Returns true if the questline is complete.</summary>
        public bool IsComplete => CurrentState == QuestLineState.Completed;

        /// <summary>Returns true if the questline is available to start.</summary>
        public bool IsAvailable => CurrentState == QuestLineState.Available;

        /// <summary>Returns true if the questline is in progress.</summary>
        public bool IsInProgress => CurrentState == QuestLineState.InProgress;

        /// <summary>Returns true if the questline has failed.</summary>
        public bool IsFailed => CurrentState == QuestLineState.Failed;

        /// <summary>Gets the next incomplete quest in the line, or null if all are complete.</summary>
        public Quest_SO NextQuest => GetNextIncompleteQuest();

        /// <summary>Gets the first quest in the line, or null if empty.</summary>
        public Quest_SO FirstQuest => Data.QuestCount > 0 ? Data.Quests[0] : null;

        #endregion

        #region Private Fields

        private HashSet<Guid> _completedQuestIds = new();
        private bool _hasStarted;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new QuestLineRuntime from the given data.
        /// </summary>
        /// <param name="data">The QuestLine_SO configuration.</param>
        public QuestLineRuntime(QuestLine_SO data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            QuestLineId = data.QuestLineId;
            CurrentState = QuestLineState.Available;
            _hasStarted = false;
        }

        #endregion

        #region State Management

        /// <summary>
        /// Checks and updates the questline state based on current quest progress.
        /// Called by QuestManager when any quest completes.
        /// </summary>
        public void CheckProgress()
        {
            if (CurrentState == QuestLineState.Completed || CurrentState == QuestLineState.Failed)
                return;

            int completedCount = GetCompletedCount();

            // Check if we've started
            if (!_hasStarted && completedCount > 0)
            {
                _hasStarted = true;
                CurrentState = QuestLineState.InProgress;
                OnQuestLineStarted.SafeInvoke(this);
            }

            // Check if all quests are complete
            if (completedCount >= TotalQuestCount && TotalQuestCount > 0)
            {
                CurrentState = QuestLineState.Completed;
                OnQuestLineCompleted.SafeInvoke(this);
            }
            else
            {
                OnQuestLineUpdated.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Notifies the questline that a quest within it has completed.
        /// </summary>
        /// <param name="quest">The quest that completed.</param>
        public void NotifyQuestCompleted(QuestRuntime quest)
        {
            if (quest == null) return;

            // Track completion
            _completedQuestIds.Add(quest.QuestId);

            // Fire event
            OnQuestInLineCompleted.SafeInvoke(this, quest);

            // Check overall progress
            CheckProgress();
        }

        /// <summary>
        /// Notifies the questline that a quest within it has failed.
        /// </summary>
        /// <param name="quest">The quest that failed.</param>
        public void NotifyQuestFailed(QuestRuntime quest)
        {
            if (quest == null) return;

            // Check if failure should fail the entire line
            if (Data.FailOnAnyQuestFailed)
            {
                CurrentState = QuestLineState.Failed;
                OnQuestLineFailed.SafeInvoke(this);
            }
            else
            {
                OnQuestLineUpdated.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Checks if the prerequisite line (if any) is completed.
        /// </summary>
        /// <returns>True if available, false if locked.</returns>
        public bool CheckPrerequisite()
        {
            if (Data.PrerequisiteLine == null)
                return true;

            if (QuestManager.Instance == null)
                return false;

            return QuestManager.Instance.IsQuestLineCompleted(Data.PrerequisiteLine);
        }

        /// <summary>
        /// Distributes completion rewards when the entire questline is completed.
        /// </summary>
        public void DistributeCompletionRewards()
        {
            if (Data.CompletionRewards == null) return;

            foreach (var reward in Data.CompletionRewards)
            {
                reward.RewardType?.GiveReward(reward.Amount);
            }
        }

        /// <summary>
        /// Resets the questline to its initial state.
        /// </summary>
        public void Reset()
        {
            _completedQuestIds.Clear();
            _hasStarted = false;
            CurrentState = CheckPrerequisite() ? QuestLineState.Available : QuestLineState.Locked;
        }

        /// <summary>
        /// Restores the questline state from a saved snapshot.
        /// Used for save/load functionality.
        /// </summary>
        /// <param name="state">The state to restore to.</param>
        /// <param name="hasStarted">Whether the questline has been started.</param>
        public void RestoreState(QuestLineState state, bool hasStarted)
        {
            CurrentState = state;
            _hasStarted = hasStarted;
        }

        /// <summary>
        /// Gets whether this questline has been started. Used for save/load.
        /// </summary>
        public bool HasStarted => _hasStarted;

        #endregion

        #region Private Helpers

        private float CalculateProgress()
        {
            if (TotalQuestCount == 0) return 1f;
            return (float)GetCompletedCount() / TotalQuestCount;
        }

        private int GetCompletedCount()
        {
            if (QuestManager.Instance == null) return _completedQuestIds.Count;

            int count = 0;
            foreach (var quest in Data.Quests)
            {
                if (quest != null && QuestManager.Instance.IsQuestCompleted(quest))
                    count++;
            }
            return count;
        }

        private Quest_SO GetNextIncompleteQuest()
        {
            if (QuestManager.Instance == null) return null;

            foreach (var quest in Data.Quests)
            {
                if (quest != null && !QuestManager.Instance.IsQuestCompleted(quest))
                    return quest;
            }
            return null;
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is QuestLineRuntime other)
            {
                return QuestLineId == other.QuestLineId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return QuestLineId.GetHashCode();
        }

        #endregion
    }
}
