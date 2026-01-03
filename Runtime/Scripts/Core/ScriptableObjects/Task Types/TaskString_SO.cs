using HelloDev.QuestSystem.Tasks;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A concrete ScriptableObject for a task that tracks a string value.
    /// This can be used for tasks like "Enter the correct password" or "Find the secret code".
    /// </summary>
    [CreateAssetMenu(fileName = "TaskString_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/String Task")]
    public class TaskString_SO : Task_SO
    {
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [TitleGroup("Tabs/Configuration/Task Settings")]
        [PropertyOrder(5)]
        [Required("Target value is required.")]
#else
        [Header("String Task")]
#endif
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

        public override void SetupTaskLocalizedVariables(LocalizedString localizedString, TaskRuntime task)
        {
            // String tasks don't require any localized variables
        }
    }
}
