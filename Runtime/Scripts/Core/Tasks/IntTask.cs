using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A concrete runtime task that tracks an integer counter.
    /// This task type is used for objectives like "kill X enemies" or "collect Y items".
    /// </summary>
    public class IntTask : Task
    {
        private int _currentCount;

        /// <summary>
        /// Gets the required number of counts to complete this task.
        /// </summary>
        public int RequiredCount => (TaskData as IntTask_SO)?.RequiredCount ?? 0;

        /// <summary>
        /// Gets the current number of counts for this task.
        /// </summary>
        public int CurrentCount => _currentCount;

        /// <summary>
        /// Initializes a new instance of the IntTask class.
        /// </summary>
        /// <param name="taskData">The ScriptableObject containing the task's data.</param>
        public IntTask(IntTask_SO taskData) : base(taskData)
        {
            _currentCount = taskData.CurrentCount;
        }

        /// <summary>
        /// Increments the task's counter by one and checks for completion.
        /// This method should be called externally by game systems when the relevant event occurs.
        /// </summary>
        /// <param name="targetId">The ID of the event that caused the increment (e.g., "goblin").</param>
        public bool IncrementCount()
        {
            if (CurrentState != TaskState.InProgress || _currentCount >= RequiredCount)
            {
                return false;
            }

            _currentCount++;

            // Fire the progress event for UI to listen to.
            OnTaskUpdated?.Invoke(this);

            QuestLogger.Log($"Task '{DevName}' progress updated: {_currentCount}/{RequiredCount}.");

            if (_currentCount >= RequiredCount)
            {
                CompleteTask();
            }
            
            return true;
        }

        public override bool OnIncrementStep()
        {
            return IncrementCount();
        }

        /// <summary>
        /// Resets the task's state and counter to its initial values.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _currentCount = 0;
            OnTaskUpdated?.Invoke(this);
        }
    }
}