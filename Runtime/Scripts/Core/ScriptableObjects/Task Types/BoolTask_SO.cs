using UnityEngine;
using HelloDev.QuestSystem.Utils;
using HelloDev.QuestSystem.Tasks;
using HelloDev.Utils;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A concrete ScriptableObject for a task that tracks a generic boolean value.    /// </summary>
    [CreateAssetMenu(fileName = "BoolTask_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Bool Task")]
    public class BoolTask_SO : Task_SO
    {
        [Header("Bool Task")] [Tooltip("A unique string ID for the target of this task (e.g., 'goblin', 'gold_coin').")] [SerializeField]
        private string targetId;
        
        /// <summary>
        /// Gets the unique string ID for the target of this task.
        /// </summary>
        public string TargetId => targetId;
        
        public override Task GetRuntimeTask()
        {
            return new BoolTask(this);
        }

        protected override void Reset()
        {
            base.Reset();
        }

        public override void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, Task task)
        {
            if (taskNameText == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: taskNameText is null.");
                return;
            }

            if (task is not IntTask intTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not an IntTask.");
                return;
            }

            var stringReference = taskNameText.StringReference;
            if (stringReference == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: StringReference is null.");
                return;
            }

            // Ensure "current" variable exists
            if (!stringReference.TryGetValue("current", out var currentVariable))
            {
                stringReference.Add("current", new IntVariable { Value = intTask.CurrentCount });
            }
            else
            {
                if (currentVariable is IntVariable existingCurrent)
                    existingCurrent.Value = intTask.CurrentCount;
            }

            // Ensure "target" variable exists
            if (!stringReference.TryGetValue("required", out var requiredVariable))
            {
                stringReference.Add("required", new IntVariable { Value = intTask.RequiredCount });
            }
            else
            {
                if (requiredVariable is IntVariable existingTarget)
                    existingTarget.Value = intTask.RequiredCount;
            }

            // Refresh the localized string so UI updates immediately
            taskNameText.RefreshString();
        }
    }
}