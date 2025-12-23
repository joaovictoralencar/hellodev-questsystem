using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject for a task with a time limit.
    /// The task fails if time runs out before the objective is completed.
    /// Used for objectives like "Defeat the boss within 2 minutes".
    /// </summary>
    [CreateAssetMenu(fileName = "TaskTimed_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Timed Task")]
    public class TaskTimed_SO : Task_SO
    {
        [Header("Timed Task")]
        [Tooltip("The time limit in seconds.")]
        [SerializeField]
        [Min(1f)]
        private float timeLimit = 120f;

        [Tooltip("If true, the timer fails the entire quest. If false, only the task fails.")]
        [SerializeField]
        private bool failQuestOnExpire = false;

        /// <summary>
        /// Gets the time limit in seconds.
        /// </summary>
        public float TimeLimit => timeLimit;

        /// <summary>
        /// Gets whether the quest should fail when the timer expires.
        /// </summary>
        public bool FailQuestOnExpire => failQuestOnExpire;

        public override TaskRuntime GetRuntimeTask()
        {
            return new TimedTaskRuntime(this);
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

            if (task is not TimedTaskRuntime timedTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not a TimedTask.");
                return;
            }

            LocalizedString stringReference = taskNameText.StringReference;
            if (stringReference == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: StringReference is null.");
                return;
            }

            // Format remaining time as minutes:seconds
            int minutes = (int)(timedTask.RemainingTime / 60);
            int seconds = (int)(timedTask.RemainingTime % 60);
            string timeString = $"{minutes}:{seconds:D2}";

            // Add or update "remaining" variable for remaining time
            if (!stringReference.TryGetValue("remaining", out IVariable remainingVariable))
            {
                stringReference.Add("remaining", new StringVariable { Value = timeString });
            }
            else
            {
                if (remainingVariable is StringVariable existingRemaining)
                    existingRemaining.Value = timeString;
            }

            // Add or update "time" variable (alias for remaining time - used in some localization strings)
            if (!stringReference.TryGetValue("time", out IVariable timeVariable))
            {
                stringReference.Add("time", new StringVariable { Value = timeString });
            }
            else
            {
                if (timeVariable is StringVariable existingTime)
                    existingTime.Value = timeString;
            }

            // Add or update "limit" variable for total time limit
            int limitMinutes = (int)(timeLimit / 60);
            int limitSeconds = (int)(timeLimit % 60);
            string limitString = $"{limitMinutes}:{limitSeconds:D2}";

            if (!stringReference.TryGetValue("limit", out IVariable limitVariable))
            {
                stringReference.Add("limit", new StringVariable { Value = limitString });
            }
            else
            {
                if (limitVariable is StringVariable existingLimit)
                    existingLimit.Value = limitString;
            }

            // Refresh the localized string so UI updates immediately
            taskNameText.RefreshString();
        }
    }
}
