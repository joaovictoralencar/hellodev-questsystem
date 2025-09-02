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
