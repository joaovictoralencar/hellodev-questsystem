using UnityEngine;
using HelloDev.QuestSystem.Utils;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A concrete ScriptableObject for a task that tracks a generic integer counter.
    /// This can be used for tasks like "Kill X enemies" or "Collect Y items".
    /// </summary>
    [CreateAssetMenu(fileName = "IntTask_SO", menuName = "HelloDev/Quest System/Scriptable Objects/Tasks/Int Task")]
    public class IntTask_SO : Task_SO
    {
        [Header("Int Task")]
        [Tooltip("A unique string ID for the target of this task (e.g., 'goblin', 'gold_coin').")]
        [SerializeField]
        private string targetId;

        [Tooltip("The number of times the target event must occur to complete the task.")]
        [SerializeField]
        private int requiredCount;

        [Tooltip("The current progress of the task. Read-only in the editor, but serialized for saving.")]
        [ReadOnly, SerializeField]
        private int currentCount;

        /// <summary>
        /// Gets the unique string ID for the target of this task.
        /// </summary>
        public string TargetId => targetId;

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

        public override Task GetRuntimeTask()
        {
            return new IntTask(this);
        }
    }
}