using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject for a task that requires discovering/examining specific items or clues.
    /// Uses event-driven conditions from the Conditions list. Each condition represents one discoverable item.
    /// Each condition can only be fulfilled once (duplicate-protected).
    /// </summary>
    [CreateAssetMenu(fileName = "TaskDiscovery_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Discovery Task")]
    public class TaskDiscovery_SO : Task_SO
    {
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [TitleGroup("Tabs/Configuration/Task Settings")]
        [PropertyOrder(5)]
        [Min(0)]
        [InfoBox("Each event-driven condition in the Conditions list represents one discoverable item. Each can only be fulfilled once. If 0, all conditions must be fulfilled.", InfoMessageType.Info)]
#else
        [Header("Discovery Task")]
#endif
        [Tooltip("The number of conditions that must be fulfilled to complete the task. If 0, all conditions must be fulfilled.")]
        [SerializeField]
        private int requiredDiscoveries = 0;

        /// <summary>
        /// Gets the number of discoveries required. Returns the conditions count if set to 0.
        /// </summary>
        public int RequiredDiscoveries => requiredDiscoveries > 0 ? requiredDiscoveries : (Conditions?.Count ?? 0);

        public override TaskRuntime GetRuntimeTask()
        {
            return new DiscoveryTaskRuntime(this);
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

            if (task is not DiscoveryTaskRuntime discoveryTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not a DiscoveryTask.");
                return;
            }

            LocalizedString stringReference = taskNameText.StringReference;
            if (stringReference == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: StringReference is null.");
                return;
            }

            // Add or update "current" variable for discovered count
            if (!stringReference.TryGetValue("current", out IVariable currentVariable))
            {
                stringReference.Add("current", new IntVariable { Value = discoveryTask.DiscoveredCount });
            }
            else
            {
                if (currentVariable is IntVariable existingCurrent)
                    existingCurrent.Value = discoveryTask.DiscoveredCount;
            }

            // Add or update "required" variable for required discoveries
            if (!stringReference.TryGetValue("required", out IVariable requiredVariable))
            {
                stringReference.Add("required", new IntVariable { Value = discoveryTask.RequiredDiscoveries });
            }
            else
            {
                if (requiredVariable is IntVariable existingRequired)
                    existingRequired.Value = discoveryTask.RequiredDiscoveries;
            }

            // Refresh the localized string so UI updates immediately
            taskNameText.RefreshString();
        }
    }
}
