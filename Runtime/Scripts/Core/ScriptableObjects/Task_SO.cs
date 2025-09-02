using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using UnityEngine;
using UnityEngine.Localization;

using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// The abstract base class for all task data ScriptableObjects.
    /// Defines the common data fields for tasks and a contract for creating a runtime Task instance.
    /// </summary>
    public abstract class   Task_SO : RuntimeScriptableObject
    {
        [Header("Core Info")]
        [Tooltip("Internal name for developers, used for identification in code.")]
        [SerializeField]
        private string devName;

        [Tooltip("A unique, permanent identifier for this task. Auto-generated.")]
        [SerializeField, ReadOnly]
        private string taskId;

        [Header("Content")]
        [Tooltip("The localized display name of the task.")]
        [SerializeField]
        private LocalizedString displayName;

        [Tooltip("The localized description of the task.")]
        [SerializeField]
        private LocalizedString taskDescription;
        
        [Header("Conditions")]
        [SerializeField] private List<Condition_SO> conditions;

        [Tooltip("The list of conditions that, when all met, will cause this task to fail.")]
        [SerializeField]
        private List<Condition_SO> failureConditions;

        /// <summary>
        /// Gets the developer-friendly name of the task.
        /// </summary>
        public string DevName => devName;

        /// <summary>
        /// Gets the unique, permanent identifier for this task.
        /// </summary>
        public Guid TaskId => Guid.Parse(taskId);

        /// <summary>
        /// Gets the localized display name of the task.
        /// </summary>
        public LocalizedString DisplayName => displayName;

        /// <summary>
        /// Gets the localized description of the task.
        /// </summary>
        public LocalizedString TaskDescription => taskDescription;
        
        /// <summary>
        /// Gets the list of conditions that must be met to fail the task.
        /// </summary>
        public List<Condition_SO> Conditions => conditions;

        /// <summary>
        /// Gets the list of conditions that must be met to fail the task.
        /// </summary>
        public List<Condition_SO> FailureConditions => failureConditions;

        /// <summary>
        /// Creates and returns a new runtime instance of this task.
        /// </summary>
        public abstract Task GetRuntimeTask();

        /// <summary>
        /// Called when the script is loaded or a value is changed in the Inspector.
        /// Ensures the task has a unique ID and a default dev name.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                taskId = Guid.NewGuid().ToString();
            }
            
            if (string.IsNullOrWhiteSpace(devName))
            {
                devName = name;
            }
        }

        protected override void OnScriptableObjectReset()
        {
        }

        public abstract void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, Task task);
    }
}