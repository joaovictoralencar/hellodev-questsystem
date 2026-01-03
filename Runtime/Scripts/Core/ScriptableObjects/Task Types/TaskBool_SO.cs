using UnityEngine;
using HelloDev.QuestSystem.Tasks;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A concrete ScriptableObject for a task that tracks a generic boolean value.
    /// </summary>
    [CreateAssetMenu(fileName = "Task Bool", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Bool Task")]
    public class TaskBool_SO : Task_SO
    {
        public override TaskRuntime GetRuntimeTask()
        {
            return new BoolTaskRuntime(this);
        }

        protected override void OnScriptableObjectReset()
        {
            base.OnScriptableObjectReset();
        }

        public override void SetupTaskLocalizedVariables(LocalizedString localizedString, TaskRuntime task)
        {
            // Bool tasks don't require any localized variables
        }
    }
}