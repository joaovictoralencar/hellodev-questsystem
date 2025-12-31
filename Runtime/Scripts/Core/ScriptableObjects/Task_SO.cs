using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Tasks;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// The abstract base class for all task data ScriptableObjects.
    /// Defines the common data fields for tasks and a contract for creating a runtime Task instance.
    /// </summary>
    public abstract partial class Task_SO : RuntimeScriptableObject
    {
        #region Identity

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration", SdfIconType.Gear, Order = 1), TitleGroup("Tabs/Configuration/Identity"), PropertyOrder(0), Required("Dev Name is required.")]
#else
        [Header("Identity")]
#endif
        [SerializeField, Tooltip("Internal name for developers, used for identification in code.")]
        private string devName;

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration"), TitleGroup("Tabs/Configuration/Identity"), PropertyOrder(1), ReadOnly, DisplayAsString]
#endif
        [SerializeField, Tooltip("A unique, permanent identifier for this task. Auto-generated.")]
        private string taskId;

        #endregion

        #region Display

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration"), TitleGroup("Tabs/Configuration/Display"), PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [SerializeField, Tooltip("The localized display name of the task.")]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration"), TitleGroup("Tabs/Configuration/Display"), PropertyOrder(11)]
#endif
        [SerializeField, Tooltip("The localized description of the task.")]
        private LocalizedString taskDescription;

        #endregion

        #region Conditions

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration"), TitleGroup("Tabs/Configuration/Conditions"), PropertyOrder(20), ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Conditions that complete this task when met. Should be event-driven.", InfoMessageType.Info)]
#else
        [Header("Conditions")]
#endif
        [SerializeField, Tooltip("The list of conditions that, when met, will complete this task.")]
        private List<Condition_SO> conditions;

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration"), TitleGroup("Tabs/Configuration/Conditions"), PropertyOrder(21), ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Conditions that fail this task when met.", InfoMessageType.Warning, nameof(HasFailureConditions))]
#endif
        [SerializeField, Tooltip("The list of conditions that, when all met, will cause this task to fail.")]
        private List<Condition_SO> failureConditions;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the developer-friendly name of the task.
        /// </summary>
        public string DevName => devName;

        /// <summary>
        /// Gets the unique, permanent identifier for this task.
        /// </summary>
        public Guid TaskId => string.IsNullOrEmpty(taskId) ? Guid.Empty : Guid.Parse(taskId);

        /// <summary>
        /// Gets the localized display name of the task.
        /// </summary>
        public LocalizedString DisplayName => displayName;

        /// <summary>
        /// Gets the localized description of the task.
        /// </summary>
        public LocalizedString TaskDescription => taskDescription;

        /// <summary>
        /// Gets the list of conditions that must be met to complete the task.
        /// </summary>
        public List<Condition_SO> Conditions => conditions;

        /// <summary>
        /// Gets the list of conditions that must be met to fail the task.
        /// </summary>
        public List<Condition_SO> FailureConditions => failureConditions;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Creates and returns a new runtime instance of this task.
        /// </summary>
        public abstract TaskRuntime GetRuntimeTask();

        /// <summary>
        /// Sets up localized variables for the task UI display.
        /// </summary>
        public abstract void SetupTaskLocalizedVariables(LocalizeStringEvent taskNameText, TaskRuntime task);

        #endregion

        #region Validation

        /// <summary>
        /// Called when the script is loaded or a value is changed in the Inspector.
        /// Ensures the task has a unique ID and a default dev name.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                GenerateNewGuid();
            }

            if (string.IsNullOrWhiteSpace(devName))
            {
                devName = name;
            }
        }

#if ODIN_INSPECTOR
        private bool HasFailureConditions()
        {
            return failureConditions != null && failureConditions.Count > 0;
        }
#endif

        #endregion

        #region Editor Buttons

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration"), TitleGroup("Tabs/Configuration/Identity"), Button("Generate New ID", ButtonSizes.Medium), PropertyOrder(2)]
        private void GenerateNewGuid()
        {
            taskId = Guid.NewGuid().ToString();
        }
#else
        private void GenerateNewGuid()
        {
            taskId = Guid.NewGuid().ToString();
        }
#endif

        #endregion

        #region Unity Callbacks

        protected override void OnScriptableObjectReset()
        {
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is Task_SO other)
            {
                return TaskId == other.TaskId;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }

        #endregion
    }
}
