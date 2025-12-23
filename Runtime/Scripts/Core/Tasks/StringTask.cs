using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// Runtime task for string-based objectives. Completes when the current value matches the target value.
    /// </summary>
    public class StringTask : TaskRuntime
    {
        private string _currentValue = string.Empty;
        private readonly TaskString_SO _stringTaskData;

        /// <summary>
        /// Gets the current string value.
        /// </summary>
        public string CurrentValue => _currentValue;

        /// <summary>
        /// Gets the target string value that must be matched.
        /// </summary>
        public string TargetValue => _stringTaskData?.TargetValue ?? string.Empty;

        public StringTask(Task_SO taskData) : base(taskData)
        {
            _stringTaskData = taskData as TaskString_SO;
        }

        public override float Progress => CurrentState == TaskState.Completed ? 1f : 0f;

        public override void ForceCompleteState()
        {
            _currentValue = TargetValue;
            CompleteTask();
        }

        /// <summary>
        /// Sets the current string value and checks for completion.
        /// </summary>
        /// <param name="value">The new string value.</param>
        public void SetValue(string value)
        {
            _currentValue = value ?? string.Empty;
            CheckCompletion(this);
        }

        public override bool OnIncrementStep()
        {
            // For string tasks, increment completes the task directly
            CompleteTask();
            return true;
        }

        public override bool OnDecrementStep()
        {
            // String tasks don't support decrement - reset the value instead
            _currentValue = string.Empty;
            return true;
        }

        protected override void CheckCompletion(TaskRuntime task)
        {
            if (CurrentState != TaskState.InProgress) return;

            // Compare current value with target value (case-sensitive)
            if (_currentValue == TargetValue)
            {
                CompleteTask();
            }
        }
    }
}
