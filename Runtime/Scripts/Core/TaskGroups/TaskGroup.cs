using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.TaskGroups
{
    /// <summary>
    /// Serializable data class representing a group of tasks within a quest.
    /// Used in Quest_SO for designer configuration.
    /// </summary>
    [Serializable]
    public class TaskGroup
    {
#if ODIN_INSPECTOR
        [BoxGroup("Group Settings")]
        [PropertyOrder(0)]
#endif
        [Tooltip("Optional name for this task group (e.g., 'Investigation Phase', 'Combat Tasks')")]
        [SerializeField]
        private string groupName = "Task Group";

#if ODIN_INSPECTOR
        [BoxGroup("Group Settings")]
        [PropertyOrder(1)]
#endif
        [Tooltip("How tasks in this group should be executed")]
        [SerializeField]
        private TaskExecutionMode executionMode = TaskExecutionMode.Sequential;

#if ODIN_INSPECTOR
        [BoxGroup("Group Settings")]
        [PropertyOrder(2)]
        [ShowIf(nameof(IsOptionalMode))]
        [MinValue(1)]
#endif
        [Tooltip("For OptionalXofY mode: minimum number of tasks required to complete the group")]
        [SerializeField]
        private int requiredCount = 1;

#if ODIN_INSPECTOR
        [BoxGroup("Tasks")]
        [PropertyOrder(10)]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
#endif
        [Tooltip("Tasks in this group")]
        [SerializeField]
        private List<Task_SO> tasks = new();

        #region Properties

        /// <summary>
        /// Optional name for this task group.
        /// </summary>
        public string GroupName => groupName;

        /// <summary>
        /// How tasks in this group should be executed.
        /// </summary>
        public TaskExecutionMode ExecutionMode => executionMode;

        /// <summary>
        /// For OptionalXofY mode: minimum number of tasks required to complete the group.
        /// </summary>
        public int RequiredCount => requiredCount;

        /// <summary>
        /// Tasks in this group.
        /// </summary>
        public List<Task_SO> Tasks => tasks;

        /// <summary>
        /// Returns true if this group has no tasks.
        /// </summary>
        public bool IsEmpty => tasks == null || tasks.Count == 0;

        /// <summary>
        /// Returns the total number of tasks in this group.
        /// </summary>
        public int TaskCount => tasks?.Count ?? 0;

        #endregion

#if ODIN_INSPECTOR
        private bool IsOptionalMode => executionMode == TaskExecutionMode.OptionalXofY;
#endif

        /// <summary>
        /// Creates a default sequential group from a flat task list.
        /// Used for migrating legacy quests that use a flat task list.
        /// </summary>
        /// <param name="taskList">The list of tasks to include in the group.</param>
        /// <param name="name">Optional name for the group.</param>
        /// <returns>A new TaskGroup configured for sequential execution.</returns>
        public static TaskGroup CreateSequentialGroup(List<Task_SO> taskList, string name = "Main Tasks")
        {
            return new TaskGroup
            {
                groupName = name,
                executionMode = TaskExecutionMode.Sequential,
                requiredCount = taskList?.Count ?? 0,
                tasks = taskList ?? new List<Task_SO>()
            };
        }

        /// <summary>
        /// Validates the task group configuration.
        /// </summary>
        /// <returns>List of validation warnings, empty if valid.</returns>
        public List<string> Validate()
        {
            var warnings = new List<string>();

            if (tasks == null || tasks.Count == 0)
            {
                warnings.Add($"Group '{groupName}' has no tasks.");
            }
            else
            {
                // Check for null entries
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i] == null)
                    {
                        warnings.Add($"Group '{groupName}' has null task at index {i}.");
                    }
                }

                // Check OptionalXofY requirements
                if (executionMode == TaskExecutionMode.OptionalXofY)
                {
                    if (requiredCount < 1)
                    {
                        warnings.Add($"Group '{groupName}' has RequiredCount < 1.");
                    }
                    if (requiredCount > tasks.Count)
                    {
                        warnings.Add($"Group '{groupName}' RequiredCount ({requiredCount}) exceeds task count ({tasks.Count}).");
                    }
                }
            }

            return warnings;
        }
    }
}
