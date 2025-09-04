using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A concrete runtime task that tracks an integer counter.
    /// This task type is used for objectives like "kill X enemies" or "collect Y items".
    /// </summary>
    public class IntTask : Task
    {
        public override float Progress => RequiredCount == 0 ? 1 : (float)_currentCount / RequiredCount;

        private int _currentCount;

        /// <summary>
        /// Gets the required number of counts to complete this task.
        /// </summary>
        public int RequiredCount => (Data as TaskInt_SO)?.RequiredCount ?? 0;

        /// <summary>
        /// Gets the current number of counts for this task.
        /// </summary>
        public int CurrentCount => _currentCount;

        /// <summary>
        /// Initializes a new instance of the IntTask class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public IntTask(TaskInt_SO data) : base(data)
        {
            _currentCount = data.CurrentCount;
        }

        /// <summary>
        /// Increments the task's counter by one and checks for completion.
        /// This method should be called externally by game systems when the relevant event occurs.
        /// </summary>
        private bool IncrementCount()
        {
            if (CurrentState != TaskState.InProgress || _currentCount >= RequiredCount)
            {
                return false;
            }

            _currentCount++;
            QuestLogger.Log($"Task '{DevName}' progress updated: {_currentCount}/{RequiredCount}.");
            return true;
        }

        /// <summary>
        /// Increments the task's counter by one and checks for completion.
        /// This method should be called externally by game systems when the relevant event occurs.
        /// </summary>
        private bool DecrementCount()
        {
            if (CurrentState != TaskState.InProgress || _currentCount >= RequiredCount || _currentCount == 0)
            {
                return false;
            }

            _currentCount--;
            return true;
        }

        public override void ForceCompleteState()
        {
            _currentCount = RequiredCount;
        }

        public override bool OnIncrementStep()
        {
            return IncrementCount();
        }

        public override bool OnDecrementStep()
        {
            return DecrementCount();
        }

        /// <summary>
        /// Resets the task's state and counter to its initial values.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _currentCount = 0;
            OnTaskUpdated?.SafeInvoke(this);
        }

        protected override void CheckCompletion(Task task)
        {
            if (_currentCount >= RequiredCount)
            {
                CompleteTask();
            }
        }
    }
}