using HelloDev.IDs;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject for a task that requires the player to reach a specific location.
    /// Used for objectives like "Go to the goblin camp" or "Return to the village".
    /// </summary>
    [CreateAssetMenu(fileName = "TaskLocation_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Location Task")]
    public class TaskLocation_SO : Task_SO
    {
        [Header("Location Task")]
        [Tooltip("The target location the player must reach.")]
        [SerializeField]
        private ID_SO targetLocation;

        /// <summary>
        /// Gets the target location for this task.
        /// </summary>
        public ID_SO TargetLocation => targetLocation;

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

            if (task is not LocationTaskRuntime locationTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not a LocationTask.");
                return;
            }

            // Location tasks can use the target location's display name in localization
            // The smart string can reference {target} to get the location name
            var stringReference = taskNameText.StringReference;
            if (stringReference == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: StringReference is null.");
                return;
            }

            // Add location name if target exists
            if (targetLocation != null && targetLocation.DisplayName != null)
            {
                if (!stringReference.TryGetValue("target", out var _))
                {
                    stringReference.Add("target", targetLocation.DisplayName);
                }
            }

            // Refresh the localized string so UI updates immediately
            taskNameText.RefreshString();
        }
    }
}
