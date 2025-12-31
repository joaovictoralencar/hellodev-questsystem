using HelloDev.Conditions;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A concrete runtime task that tracks an integer counter.
    /// Uses event-driven conditions: each condition fulfillment increments the counter.
    /// Task completes when counter reaches requiredCount.
    /// </summary>
    public class IntTaskRuntime : TaskRuntime
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
        /// Initializes a new instance of the IntTaskRuntime class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public IntTaskRuntime(TaskInt_SO data) : base(data)
        {
            _currentCount = data.CurrentCount;
        }

        /// <summary>
        /// Override to make conditions increment counter instead of completing task.
        /// </summary>
        protected override void SubscribeToEvents()
        {
            // Subscribe conditions to increment instead of complete
            if (Data.Conditions != null)
            {
                foreach (var condition in Data.Conditions)
                {
                    if (condition is IConditionEventDriven eventCondition)
                    {
                        eventCondition.SubscribeToEvent(OnConditionFulfilled);
                    }
                }
            }

            // Subscribe failure conditions normally (they still fail the task)
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
        /// Called when a condition is fulfilled - increments the counter.
        /// </summary>
        private void OnConditionFulfilled()
        {
            if (IncrementCount())
            {
                OnTaskUpdated.SafeInvoke(this);
            }
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
            if (IncrementCount())
            {
                OnTaskUpdated.SafeInvoke(this);
                return true;
            }
            return false;
        }

        public override bool OnDecrementStep()
        {
            if (DecrementCount())
            {
                OnTaskUpdated.SafeInvoke(this);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets the task's state and counter to its initial values.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _currentCount = 0;
            OnTaskUpdated.SafeInvoke(this);
        }

        protected override void CheckCompletion(TaskRuntime task)
        {
            if (_currentCount >= RequiredCount)
            {
                CompleteTask();
            }
        }

        #region Save/Load

        /// <inheritdoc />
        public override void CaptureProgress(TaskProgressData progressData)
        {
            progressData.IntValue = _currentCount;
        }

        /// <inheritdoc />
        public override void RestoreProgress(TaskProgressData progressData)
        {
            _currentCount = progressData.IntValue;
            // Note: Do NOT fire OnTaskUpdated here - it triggers CheckCompletion
            // which can auto-complete the task before state is fully restored
        }

        #endregion
    }
}