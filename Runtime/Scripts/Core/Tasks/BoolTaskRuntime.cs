using System.Linq;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A concrete runtime task that tracks a boolean state.
    /// This task type is used for simple objectives like "talk to NPC" or "trigger an event".
    /// Completes when all conditions are met or when manually incremented.
    /// </summary>
    public class BoolTaskRuntime : TaskRuntime
    {
        /// <summary>
        /// Gets the progress of this task. Returns 1 if completed, 0 otherwise.
        /// </summary>
        public override float Progress => CurrentState == TaskState.Completed ? 1 : 0;

        /// <summary>
        /// Gets whether the task is completed. Used for save/load.
        /// </summary>
        public bool IsCompleted => CurrentState == TaskState.Completed;

        /// <summary>
        /// Initializes a new instance of the BoolTaskRuntime class.
        /// </summary>
        /// <param name="taskData">The ScriptableObject containing the task's data.</param>
        public BoolTaskRuntime(Task_SO taskData) : base(taskData)
        {
        }

        /// <summary>
        /// Forces the task into a completed state. No additional state changes needed for bool tasks.
        /// </summary>
        public override void ForceCompleteState()
        {
        }

        /// <summary>
        /// Increments the task, which for a bool task means completing it immediately.
        /// </summary>
        /// <returns>True if the task was successfully completed, false if not in progress.</returns>
        public override bool OnIncrementStep()
        {
            if (CurrentState != TaskState.InProgress) return false;
            CompleteTask();
            return true;
        }

        /// <summary>
        /// Decrements the task. Bool tasks cannot be decremented, so this always returns true without action.
        /// </summary>
        /// <returns>Always returns true.</returns>
        public override bool OnDecrementStep()
        {
            return true;
        }

        /// <summary>
        /// Checks if all conditions are met and completes the task if so.
        /// Called automatically when subscribed condition events fire.
        /// </summary>
        /// <param name="task">The task being checked (this instance).</param>
        protected override void CheckCompletion(TaskRuntime task)
        {
            if (task.Data.Conditions.All(condition => condition.Evaluate()))
            {
                CompleteTask();
            }
        }

        #region Save/Load

        /// <inheritdoc />
        public override void CaptureProgress(TaskProgressData progressData)
        {
            progressData.BoolValue = IsCompleted;
        }

        /// <inheritdoc />
        public override void RestoreProgress(TaskProgressData progressData)
        {
            // Bool task state is restored via task state, not internal value
            // If saved as completed, the task will be completed via CompleteTask()
        }

        #endregion
    }
}
