using HelloDev.IDs;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
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
    /// A concrete ScriptableObject for a task that tracks a generic integer counter.
    /// This can be used for tasks like "Kill X enemies" or "Collect Y items".
    /// </summary>
    [CreateAssetMenu(fileName = "TaskInt_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Int Task")]
    public class TaskInt_SO : Task_SO
    {
        [Header("Int Task")] [Tooltip("The ID_SO reference for the target of this task (e.g., Goblin ID, Gold Coin ID).")] [SerializeField]
        private ID_SO targetId;

        [Tooltip("The number of times the target event must occur to complete the task.")] [SerializeField]
        private int requiredCount;

        [Tooltip("The current progress of the task. Read-only in the editor, but serialized for saving.")]
        [SerializeField]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        private int currentCount;

        /// <summary>
        /// Gets the ID_SO reference for the target of this task.
        /// </summary>
        public ID_SO TargetId => targetId;
        /// <summary>
        /// Gets the required count to complete this task.
        /// </summary>
        public int RequiredCount => requiredCount;

        /// <summary>
        /// Gets or sets the current progress count of the task.
        /// </summary>
        public int CurrentCount
        {
            get => currentCount;
            set => currentCount = value;
        }

        public override TaskRuntime GetRuntimeTask()
        {
            return new IntTask(this);
        }

        protected override void OnScriptableObjectReset()
        {
            base.OnScriptableObjectReset();
            currentCount = 0;
        }

        public override void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, TaskRuntime task)
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

            LocalizedString stringReference = taskNameText.StringReference;
            if (stringReference == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: StringReference is null.");
                return;
            }

            // Ensure "current" variable exists
            if (!stringReference.TryGetValue("current", out IVariable currentVariable))
            {
                stringReference.Add("current", new IntVariable { Value = intTask.CurrentCount });
            }
            else
            {
                if (currentVariable is IntVariable existingCurrent)
                    existingCurrent.Value = intTask.CurrentCount;
            }

            // Ensure "target" variable exists
            if (!stringReference.TryGetValue("required", out IVariable requiredVariable))
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