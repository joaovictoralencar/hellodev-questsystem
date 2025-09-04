using UnityEngine;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    public class StringTask : Task
    {
        private string _currentValue;

        public StringTask(Task_SO taskData) : base(taskData)
        {
        }

        public override float Progress => CurrentState == TaskState.Completed ? 1 : 0;
        public override void ForceCompleteState()
        {
            //Todo current value = target value
        }

        public override bool OnIncrementStep()
        {
            CompleteTask();
            return true;
        }

        public override bool OnDecrementStep()
        {
            return true;
        }

        protected override void CheckCompletion(Task task)
        {
        }
    }
}
