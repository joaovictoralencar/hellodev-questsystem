using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

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
#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [TitleGroup("Tabs/Configuration/Task Settings")]
        [PropertyOrder(5)]
        [SuffixLabel("seconds")]
        [Min(1f)]
#else
        [Header("Timed Task")]
#endif
        [Tooltip("The time limit in seconds.")]
        [SerializeField]
        private float timeLimit = 120f;

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [TitleGroup("Tabs/Configuration/Task Settings")]
        [PropertyOrder(6)]
        [InfoBox("If enabled, the timer failing will fail the entire quest, not just this task.", InfoMessageType.Warning, nameof(failQuestOnExpire))]
#endif
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

        public override void SetupTaskLocalizedVariables(LocalizedString localizedString, TaskRuntime task)
        {
            if (localizedString == null)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: localizedString is null.");
                return;
            }

            if (task is not TimedTaskRuntime timedTask)
            {
                QuestLogger.LogError("SetupTaskLocalizedVariables: task is not a TimedTask.");
                return;
            }

            // Format remaining time as minutes:seconds
            int minutes = (int)(timedTask.RemainingTime / 60);
            int seconds = (int)(timedTask.RemainingTime % 60);
            string timeString = $"{minutes}:{seconds:D2}";

            // Add or update "remaining" variable for remaining time
            if (!localizedString.TryGetValue("remaining", out IVariable remainingVariable))
            {
                localizedString.Add("remaining", new StringVariable { Value = timeString });
            }
            else
            {
                if (remainingVariable is StringVariable existingRemaining)
                    existingRemaining.Value = timeString;
            }

            // Add or update "time" variable (alias for remaining time - used in some localization strings)
            if (!localizedString.TryGetValue("time", out IVariable timeVariable))
            {
                localizedString.Add("time", new StringVariable { Value = timeString });
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

            if (!localizedString.TryGetValue("limit", out IVariable limitVariable))
            {
                localizedString.Add("limit", new StringVariable { Value = limitString });
            }
            else
            {
                if (limitVariable is StringVariable existingLimit)
                    existingLimit.Value = limitString;
            }

            // Add "current" and "required" for compatibility with common localization patterns
            // For timed tasks: current = remaining time, required = time limit
            if (!localizedString.TryGetValue("current", out IVariable currentVariable))
            {
                localizedString.Add("current", new StringVariable { Value = timeString });
            }
            else
            {
                if (currentVariable is StringVariable existingCurrent)
                    existingCurrent.Value = timeString;
            }

            if (!localizedString.TryGetValue("required", out IVariable requiredVariable))
            {
                localizedString.Add("required", new StringVariable { Value = limitString });
            }
            else
            {
                if (requiredVariable is StringVariable existingRequired)
                    existingRequired.Value = limitString;
            }
        }
    }
}
