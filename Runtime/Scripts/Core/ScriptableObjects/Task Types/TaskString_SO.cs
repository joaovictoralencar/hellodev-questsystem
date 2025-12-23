using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A concrete ScriptableObject for a task that tracks a string value.
    /// This can be used for tasks like "Enter the correct password" or "Find the secret code".
    /// </summary>
    [CreateAssetMenu(fileName = "TaskString_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/String Task")]
    public class TaskString_SO : Task_SO
    {
        [Header("String Task")]
        [Tooltip("The target string value that must be matched to complete the task.")]
        [SerializeField]
        private string targetValue;

        /// <summary>
        /// Gets the target string value that must be matched.
        /// </summary>
        public string TargetValue => targetValue;

        public override TaskRuntime GetRuntimeTask()
        {
            return new StringTaskRuntime(this);
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

            if (task is not StringTaskRuntime stringTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not a StringTask.");
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
