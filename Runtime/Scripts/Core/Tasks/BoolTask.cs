using System.Linq;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.Tasks
{
    public class BoolTask : TaskRuntime
    {
        public BoolTask(Task_SO taskData) : base(taskData)
        {
        }

        public override float Progress => CurrentState == TaskState.Completed ? 1 : 0;

        public override void ForceCompleteState()
        {
        }

        public override bool OnIncrementStep()
        {
            if (CurrentState != TaskState.InProgress) return false;
            CompleteTask();
            return true;
        }

        public override bool OnDecrementStep()
        {
            return true;
        }

        protected override void CheckCompletion(TaskRuntime task)
        {
            if (task.Data.Conditions.All(condition => condition.Evaluate()))
            {
                CompleteTask();
            }
        }
    }
}