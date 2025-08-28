using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem
{
    public class BoolTask : Task
    {
        public BoolTask(Task_SO taskData) : base(taskData)
        {
        }

        public override float Progress { get; }
        public override bool OnIncrementStep()
        {
            throw new System.NotImplementedException();
        }

        public override bool OnDecrementStep()
        {
            throw new System.NotImplementedException();
        }

        protected override void CheckCompletion(Task task)
        {
            throw new System.NotImplementedException();
        }
    }
}
