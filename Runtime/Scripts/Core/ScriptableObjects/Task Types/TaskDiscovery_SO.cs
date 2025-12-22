using System.Collections.Generic;
using HelloDev.IDs;
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
    /// Used for objectives like "Examine 3 clues" or "Find all the witnesses".
    /// </summary>
    [CreateAssetMenu(fileName = "TaskDiscovery_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Discovery Task")]
    public class TaskDiscovery_SO : Task_SO
    {
        [Header("Discovery Task")]
        [Tooltip("The list of discoverable items/clues for this task.")]
        [SerializeField]
#if ODIN_INSPECTOR
        [ListDrawerSettings(ShowFoldout = true)]
#endif
        private List<ID_SO> discoverableItems = new();

        [Tooltip("The number of items that must be discovered to complete the task. If 0, all items must be discovered.")]
        [SerializeField]
        [Min(0)]
        private int requiredDiscoveries = 0;

        /// <summary>
        /// Gets the list of discoverable items.
        /// </summary>
        public List<ID_SO> DiscoverableItems => discoverableItems;

        /// <summary>
        /// Gets the number of discoveries required. Returns the total count if set to 0.
        /// </summary>
        public int RequiredDiscoveries => requiredDiscoveries > 0 ? requiredDiscoveries : discoverableItems.Count;

        private void OnValidate()
        {
            // Ensure required discoveries doesn't exceed discoverable items
            if (requiredDiscoveries > discoverableItems.Count)
            {
                requiredDiscoveries = discoverableItems.Count;
            }
        }

        public override Task GetRuntimeTask()
        {
            return new DiscoveryTask(this);
        }

        protected override void OnScriptableObjectReset()
        {
            base.OnScriptableObjectReset();
        }

        public override void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, Task task)
        {
            if (taskNameText == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: taskNameText is null.");
                return;
            }

            if (task is not DiscoveryTask discoveryTask)
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
