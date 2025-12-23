using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A runtime task with a time limit. The task fails if time runs out before completion.
    /// Used for objectives like "Defeat the boss within 2 minutes".
    /// </summary>
    public class TimedTask : Task
    {
        public override float Progress => IsCompleted ? 1f : 0f;

        private float _remainingTime;
        private bool _isCompleted;

        /// <summary>
        /// Gets the original time limit for this task in seconds.
        /// </summary>
        public float TimeLimit => (Data as TaskTimed_SO)?.TimeLimit ?? 0f;

        /// <summary>
        /// Gets the remaining time in seconds.
        /// </summary>
        public float RemainingTime => _remainingTime;

        /// <summary>
        /// Gets whether the timer has expired.
        /// </summary>
        public bool IsExpired => _remainingTime <= 0f && CurrentState == TaskState.InProgress;

        /// <summary>
        /// Gets whether the task objective has been completed (before time expired).
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// Gets the progress of time remaining as a value from 0 to 1.
        /// </summary>
        public float TimeProgress => TimeLimit > 0 ? _remainingTime / TimeLimit : 0f;

        /// <summary>
        /// Initializes a new instance of the TimedTask class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public TimedTask(TaskTimed_SO data) : base(data)
        {
            _remainingTime = data.TimeLimit;
            _isCompleted = false;
        }

        /// <summary>
        /// Updates the timer. Should be called every frame while the task is in progress.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last frame.</param>
        public void UpdateTimer(float deltaTime)
        {
            if (CurrentState != TaskState.InProgress || _isCompleted) return;

            _remainingTime -= deltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                QuestLogger.Log($"Task '{DevName}' - Timer expired!");
                FailTask();
            }
        }

        /// <summary>
        /// Adds time to the remaining timer.
        /// </summary>
        /// <param name="seconds">The amount of seconds to add.</param>
        public void AddTime(float seconds)
        {
            if (CurrentState != TaskState.InProgress) return;

            _remainingTime += seconds;
            QuestLogger.Log($"Task '{DevName}' - Added {seconds}s to timer. New remaining: {_remainingTime}s");
            OnTaskUpdated.SafeInvoke(this);
        }

        /// <summary>
        /// Marks the timed objective as completed (e.g., boss defeated).
        /// The task will complete successfully.
        /// </summary>
        public void MarkObjectiveComplete()
        {
            if (CurrentState != TaskState.InProgress || _isCompleted) return;

            _isCompleted = true;
            QuestLogger.Log($"Task '{DevName}' - Objective completed with {_remainingTime}s remaining!");
            OnTaskUpdated.SafeInvoke(this);
        }

        public override void ForceCompleteState()
        {
            _isCompleted = true;
        }

        public override bool OnIncrementStep()
        {
            // For timed tasks, increment marks the objective complete
            if (CurrentState != TaskState.InProgress || _isCompleted) return false;

            _isCompleted = true;
            QuestLogger.Log($"Task '{DevName}' manually marked as completed.");
            return true;
        }

        public override bool OnDecrementStep()
        {
            // Cannot decrement a timed task completion
            return false;
        }

        /// <summary>
        /// Resets the task's state and timer to initial values.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _remainingTime = TimeLimit;
            _isCompleted = false;
            OnTaskUpdated.SafeInvoke(this);
        }

        protected override void CheckCompletion(Task task)
        {
            if (_isCompleted)
            {
                CompleteTask();
            }
        }
    }
}
