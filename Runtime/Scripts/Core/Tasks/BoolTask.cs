using System.Linq;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem
{
    public class BoolTask : Task
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

        protected override void CheckCompletion(Task task)
        {
            if (task.Data.Conditions.All(condition => condition.Evaluate()))
            {
                CompleteTask();
            }
        }
    }
}