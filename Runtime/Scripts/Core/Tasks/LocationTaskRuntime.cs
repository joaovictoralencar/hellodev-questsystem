using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A runtime task that completes when the player enters a specific location.
    /// Uses event-driven conditions: task completes when any condition is fulfilled.
    /// Used for objectives like "Go to the goblin camp" or "Return to the village".
    /// </summary>
    public class LocationTaskRuntime : TaskRuntime
    {
        public override float Progress => HasReached ? 1f : 0f;

        private bool _hasReached;

        /// <summary>
        /// Gets whether the player has reached the target location.
        /// </summary>
        public bool HasReached => _hasReached;

        /// <summary>
        /// Initializes a new instance of the LocationTaskRuntime class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public LocationTaskRuntime(TaskLocation_SO data) : base(data)
        {
            _hasReached = false;
        }

        public override void ForceCompleteState()
        {
            _hasReached = true;
        }

        public override bool OnIncrementStep()
        {
            // Location task completes on increment
            if (CurrentState != TaskState.InProgress || _hasReached) return false;

            _hasReached = true;
            QuestLogger.Log($"Task '{DevName}' manually marked as reached.");
            return true;
        }

        public override bool OnDecrementStep()
        {
            // Cannot decrement a location task - it's binary (reached or not)
            return false;
        }

        /// <summary>
        /// Resets the task's state to its initial values.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _hasReached = false;
            OnTaskUpdated.SafeInvoke(this);
        }

        protected override void CheckCompletion(TaskRuntime task)
        {
            if (_hasReached)
            {
                CompleteTask();
            }
        }

        #region Save/Load

        /// <inheritdoc />
        public override void CaptureProgress(TaskProgressData progressData)
        {
            progressData.BoolValue = _hasReached;
        }

        /// <inheritdoc />
        public override void RestoreProgress(TaskProgressData progressData)
        {
            _hasReached = progressData.BoolValue;
        }

        #endregion
    }
}
