using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A runtime task that requires discovering/examining specific items or clues.
    /// Uses event-driven conditions: each condition can only be fulfilled once (duplicate-protected).
    /// Task completes when requiredDiscoveries conditions are fulfilled.
    /// </summary>
    public class DiscoveryTaskRuntime : TaskRuntime
    {
        public override float Progress => RequiredDiscoveries == 0 ? 1f : (float)DiscoveredCount / RequiredDiscoveries;

        private readonly HashSet<Condition_SO> _fulfilledConditions = new();

        /// <summary>
        /// Gets the number of discoveries required to complete the task.
        /// </summary>
        public int RequiredDiscoveries => (Data as TaskDiscovery_SO)?.RequiredDiscoveries ?? 0;

        /// <summary>
        /// Gets the current number of fulfilled conditions (discoveries).
        /// </summary>
        public int DiscoveredCount => _fulfilledConditions.Count;

        /// <summary>
        /// Gets the set of fulfilled conditions. Used for save/load.
        /// </summary>
        public IReadOnlyCollection<Condition_SO> FulfilledConditions => _fulfilledConditions;

        /// <summary>
        /// Initializes a new instance of the DiscoveryTask class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public DiscoveryTaskRuntime(TaskDiscovery_SO data) : base(data)
        {
        }

        /// <summary>
        /// Override to make conditions increment with duplicate protection.
        /// </summary>
        protected override void SubscribeToEvents()
        {
            // Subscribe conditions with duplicate protection
            if (Data.Conditions != null)
            {
                foreach (var condition in Data.Conditions)
                {
                    if (condition is IConditionEventDriven eventCondition)
                    {
                        // Create a closure to track which condition was fulfilled
                        var capturedCondition = condition;
                        eventCondition.SubscribeToEvent(() => OnConditionFulfilled(capturedCondition));
                    }
                }
            }

            // Subscribe failure conditions normally
            if (Data.FailureConditions != null)
            {
                foreach (var condition in Data.FailureConditions)
                {
                    if (condition is IConditionEventDriven eventCondition)
                    {
                        eventCondition.SubscribeToEvent(FailTask);
                    }
                }
            }

            OnTaskUpdated.AddListener(CheckCompletion);
        }

        /// <summary>
        /// Called when a condition is fulfilled - adds to fulfilled set (duplicate-protected).
        /// </summary>
        private void OnConditionFulfilled(Condition_SO condition)
        {
            if (CurrentState != TaskState.InProgress) return;
            if (_fulfilledConditions.Contains(condition)) return; // Duplicate protection

            _fulfilledConditions.Add(condition);
            QuestLogger.Log($"Task '{DevName}' - Condition fulfilled. Progress: {DiscoveredCount}/{RequiredDiscoveries}");
            OnTaskUpdated?.SafeInvoke(this);
        }

        public override void ForceCompleteState()
        {
            // Mark all required conditions as fulfilled
            if (Data.Conditions != null)
            {
                foreach (var condition in Data.Conditions.Take(RequiredDiscoveries))
                {
                    _fulfilledConditions.Add(condition);
                }
            }
        }

        public override bool OnIncrementStep()
        {
            // For discovery tasks, incrementing marks the next unfulfilled condition
            if (CurrentState != TaskState.InProgress) return false;
            if (Data.Conditions == null || _fulfilledConditions.Count >= RequiredDiscoveries) return false;

            var nextUnfulfilled = Data.Conditions.FirstOrDefault(c => !_fulfilledConditions.Contains(c));
            if (nextUnfulfilled != null)
            {
                _fulfilledConditions.Add(nextUnfulfilled);
                QuestLogger.Log($"Task '{DevName}' - Manually fulfilled condition. Progress: {DiscoveredCount}/{RequiredDiscoveries}");
                return true;
            }

            return false;
        }

        public override bool OnDecrementStep()
        {
            // For discovery tasks, decrementing removes the last fulfilled condition
            if (CurrentState != TaskState.InProgress) return false;
            if (_fulfilledConditions.Count == 0) return false;

            var lastFulfilled = _fulfilledConditions.LastOrDefault();
            if (lastFulfilled != null)
            {
                _fulfilledConditions.Remove(lastFulfilled);
                QuestLogger.Log($"Task '{DevName}' - Removed fulfillment. Progress: {DiscoveredCount}/{RequiredDiscoveries}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the task's state and clears all fulfilled conditions.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _fulfilledConditions.Clear();
            OnTaskUpdated?.SafeInvoke(this);
        }

        protected override void CheckCompletion(TaskRuntime task)
        {
            if (_fulfilledConditions.Count >= RequiredDiscoveries)
            {
                CompleteTask();
            }
        }

        #region Save/Load

        /// <inheritdoc />
        public override void CaptureProgress(TaskProgressData progressData)
        {
            // Store fulfilled condition indices for reliable lookup
            var allConditions = Data.Conditions;
            foreach (var condition in _fulfilledConditions)
            {
                int index = allConditions?.IndexOf(condition) ?? -1;
                if (index >= 0)
                {
                    progressData.FulfilledConditionIndices.Add(index);
                }
            }
        }

        /// <inheritdoc />
        public override void RestoreProgress(TaskProgressData progressData)
        {
            _fulfilledConditions.Clear();

            // Restore by index (preferred) or count (legacy)
            if (progressData.FulfilledConditionIndices.Count > 0)
            {
                // New format: restore specific conditions by index
                var allConditions = Data.Conditions;
                if (allConditions != null)
                {
                    foreach (int index in progressData.FulfilledConditionIndices)
                    {
                        if (index >= 0 && index < allConditions.Count)
                        {
                            _fulfilledConditions.Add(allConditions[index]);
                        }
                    }
                }
            }
            else if (progressData.FulfilledConditionGuids.Count > 0)
            {
                // Legacy format: restore by count
                var allConditions = Data.Conditions;
                if (allConditions != null)
                {
                    for (int i = 0; i < progressData.FulfilledConditionGuids.Count && i < allConditions.Count; i++)
                    {
                        _fulfilledConditions.Add(allConditions[i]);
                    }
                }
            }

            OnTaskUpdated.SafeInvoke(this);
        }

        #endregion
    }
}
