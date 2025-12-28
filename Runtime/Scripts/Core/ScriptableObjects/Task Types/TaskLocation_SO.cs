using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Localization.Components;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject for a task that requires the player to reach a specific location.
    /// Uses an event-driven condition from the Conditions list. The condition's targetValue is the location ID.
    /// </summary>
    [CreateAssetMenu(fileName = "TaskLocation_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Location Task")]
    public class TaskLocation_SO : Task_SO
    {
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [TitleGroup("Tabs/Configuration/Task Settings")]
        [PropertyOrder(5)]
        [InfoBox("Add one event-driven condition to the Conditions list with targetValue set to the target location ID. Task completes when condition is fulfilled.", InfoMessageType.Info)]
        [DisplayAsString]
        [ShowInInspector]
        [HideLabel]
#endif
        private string _infoPlaceholder => "Uses event-driven condition from Conditions list";

        public override TaskRuntime GetRuntimeTask()
        {
            return new LocationTaskRuntime(this);
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

            // Location task localization is handled via the condition's targetValue
            // The display name comes from the condition's target ID
            taskNameText.RefreshString();
        }
    }
}
