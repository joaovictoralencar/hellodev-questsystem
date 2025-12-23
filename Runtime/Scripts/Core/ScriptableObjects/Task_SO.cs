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
    public abstract class Task_SO : RuntimeScriptableObject
    {
        #region Identity

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(0)]
        [Required("Dev Name is required for identification.")]
#else
        [Header("Identity")]
#endif
        [Tooltip("Internal name for developers, used for identification in code.")]
        [SerializeField]
        private string devName;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(1)]
        [ReadOnly]
        [DisplayAsString]
#endif
        [Tooltip("A unique, permanent identifier for this task. Auto-generated.")]
        [SerializeField]
        private string taskId;

        #endregion

        #region Display

#if ODIN_INSPECTOR
        [FoldoutGroup("Display", expanded: true)]
        [PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [Tooltip("The localized display name of the task.")]
        [SerializeField]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(11)]
#endif
        [Tooltip("The localized description of the task.")]
        [SerializeField]
        private LocalizedString taskDescription;

        #endregion

        #region Conditions

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(20)]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Conditions that complete this task when met. Should be event-driven.", InfoMessageType.Info)]
#else
        [Header("Conditions")]
#endif
        [Tooltip("The list of conditions that, when met, will complete this task.")]
        [SerializeField]
        private List<Condition_SO> conditions;

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(21)]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Conditions that fail this task when met.", InfoMessageType.Warning, nameof(HasFailureConditions))]
#endif
        [Tooltip("The list of conditions that, when all met, will cause this task to fail.")]
        [SerializeField]
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
        [ButtonGroup("Actions")]
        [Button("Generate New ID", ButtonSizes.Medium)]
        [PropertyOrder(100)]
#endif
        private void GenerateNewGuid()
        {
            taskId = Guid.NewGuid().ToString();
        }

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
