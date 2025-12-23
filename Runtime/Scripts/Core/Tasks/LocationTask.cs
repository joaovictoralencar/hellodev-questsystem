using HelloDev.IDs;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A runtime task that completes when the player enters a specific location.
    /// Used for objectives like "Go to the goblin camp" or "Return to the village".
    /// </summary>
    public class LocationTask : Task
    {
        public override float Progress => HasReached ? 1f : 0f;

        private bool _hasReached;

        /// <summary>
        /// Gets the target location ID for this task.
        /// </summary>
        public ID_SO TargetLocation => (Data as TaskLocation_SO)?.TargetLocation;

        /// <summary>
        /// Gets whether the player has reached the target location.
        /// </summary>
        public bool HasReached => _hasReached;

        /// <summary>
        /// Initializes a new instance of the LocationTask class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public LocationTask(TaskLocation_SO data) : base(data)
        {
            _hasReached = false;
        }

        /// <summary>
        /// Called when the player enters a location.
        /// If the location matches the target, the task is completed.
        /// </summary>
        /// <param name="locationId">The ID of the location the player entered.</param>
        public void OnPlayerEnteredLocation(ID_SO locationId)
        {
            if (CurrentState != TaskState.InProgress) return;
            if (TargetLocation == null) return;

            if (locationId == TargetLocation)
            {
                _hasReached = true;
                QuestLogger.Log($"Task '{DevName}' - Player reached location '{locationId.DevName}'.");
                OnTaskUpdated.SafeInvoke(this);
            }
        }

        public override void ForceCompleteState()
        {
            _hasReached = true;
        }

        public override bool OnIncrementStep()
        {
            // Location task doesn't support increment
            // It's completed via OnPlayerEnteredLocation
            if (CurrentState != TaskState.InProgress || _hasReached) return false;

            _hasReached = true;
            QuestLogger.Log($"Task '{DevName}' manually marked as reached.");
            return true;
        }

        public override bool OnDecrementStep()
        {
            // Cannot decrement a location task
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

        protected override void CheckCompletion(Task task)
        {
            if (_hasReached)
            {
                CompleteTask();
            }
        }
    }
}
