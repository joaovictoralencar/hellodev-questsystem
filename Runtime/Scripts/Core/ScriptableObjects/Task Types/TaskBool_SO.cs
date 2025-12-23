using UnityEngine;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

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
            return new BoolTask(this);
        }

        protected override void OnScriptableObjectReset()
        {
            base.OnScriptableObjectReset();
        }

        public override void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, TaskRuntime task)
        {
            if (taskNameText == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: taskNameText is null.");
                return;
            }

            if (task is not BoolTask boolTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not an BoolTask.");
                return;
            }

            LocalizedString stringReference = taskNameText.StringReference;
            if (stringReference == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: StringReference is null.");
                return;
            }

            // Refresh the localized string so UI updates immediately
            taskNameText.RefreshString();
        }
    }
}