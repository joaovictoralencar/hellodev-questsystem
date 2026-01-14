using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.QuestSystem;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Defines the task creation mode for task nodes.
    /// </summary>
    public enum TaskMode
    {
        /// <summary>Reference an existing Task_SO asset.</summary>
        Asset = 0,
        /// <summary>Define task data directly in the node.</summary>
        Define = 1
    }

    /// <summary>
    /// Defines the available task types for validation and creation.
    /// </summary>
    public enum TaskType
    {
        Bool,
        Int,
        String,
        Location,
        Discovery,
        Timed
    }

    /// <summary>
    /// Serializable container for inline task data.
    /// Holds all possible fields for any task type.
    /// Each task node type only exposes relevant fields.
    /// </summary>
    [Serializable]
    public class InlineTaskData
    {
        #region Common Fields (Task_SO base)

        [Tooltip("Internal name for developers, used for identification.")]
        public string devName = "New Task";

        [Tooltip("The localized display name of the task.")]
        public LocalizedString displayName;

        [Tooltip("The localized description of the task.")]
        public LocalizedString taskDescription;

        [Tooltip("Conditions that complete this task when met.")]
        public ConditionList conditions = new();

        [Tooltip("Conditions that fail this task when all are met.")]
        public ConditionList failureConditions = new();

        #endregion

        #region TaskInt_SO Fields

        [Min(1)]
        [Tooltip("The number of times a condition must be fulfilled to complete the task.")]
        public int requiredCount = 1;

        #endregion

        #region TaskString_SO Fields

        [Tooltip("The target string value that must be matched to complete the task.")]
        public string targetValue = "";

        #endregion

        #region TaskDiscovery_SO Fields

        [Min(0)]
        [Tooltip("The number of conditions to fulfill. If 0, all conditions must be fulfilled.")]
        public int requiredDiscoveries = 0;

        #endregion

        #region TaskTimed_SO Fields

        [Min(1f)]
        [Tooltip("The time limit in seconds.")]
        public float timeLimit = 120f;

        [Tooltip("If true, the timer failing will fail the entire quest, not just this task.")]
        public bool failQuestOnExpire = false;

        #endregion

        #region Asset Creation

        /// <summary>
        /// Creates a Task_SO instance from this inline data.
        /// Uses SerializedObject to set private fields.
        /// </summary>
        /// <typeparam name="TTask">The Task_SO subclass to create.</typeparam>
        /// <returns>A new Task_SO instance with data from this inline definition.</returns>
        public TTask CreateTaskAsset<TTask>() where TTask : Task_SO
        {
            var task = ScriptableObject.CreateInstance<TTask>();
            SetFieldsViaSerializedObject(task);
            return task;
        }

        private void SetFieldsViaSerializedObject(Task_SO task)
        {
            var so = new SerializedObject(task);

            // Common fields
            so.FindProperty("devName").stringValue = devName;
            so.FindProperty("taskId").stringValue = Guid.NewGuid().ToString();

            // Localization: Copy LocalizedString references
            CopyLocalizedString(so.FindProperty("displayName"), displayName);
            CopyLocalizedString(so.FindProperty("taskDescription"), taskDescription);

            // Conditions lists
            CopyConditionList(so.FindProperty("conditions"), conditions);
            CopyConditionList(so.FindProperty("failureConditions"), failureConditions);

            // Type-specific fields based on actual task type
            switch (task)
            {
                case TaskInt_SO:
                    so.FindProperty("requiredCount").intValue = requiredCount;
                    break;

                case TaskString_SO:
                    so.FindProperty("targetValue").stringValue = targetValue;
                    break;

                case TaskDiscovery_SO:
                    so.FindProperty("requiredDiscoveries").intValue = requiredDiscoveries;
                    break;

                case TaskTimed_SO:
                    so.FindProperty("timeLimit").floatValue = timeLimit;
                    so.FindProperty("failQuestOnExpire").boolValue = failQuestOnExpire;
                    break;

                // Bool and Location have no additional fields
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CopyLocalizedString(SerializedProperty prop, LocalizedString source)
        {
            if (prop == null || source == null || source.IsEmpty)
                return;

            // LocalizedString has m_TableReference and m_TableEntryReference
            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef != null && source.TableReference.TableCollectionNameGuid != Guid.Empty)
            {
                // Unity.Localization serializes as m_TableCollectionName with format "GUID:xxxxx"
                var tableCollectionName = tableRef.FindPropertyRelative("m_TableCollectionName");
                if (tableCollectionName != null)
                {
                    // Format: "GUID:" + GUID without dashes (e.g., "GUID:05b8775364730764ab5bf1891aa1cb86")
                    tableCollectionName.stringValue = "GUID:" + source.TableReference.TableCollectionNameGuid.ToString("N");
                }
            }

            if (entryRef != null && source.TableEntryReference.KeyId != 0)
            {
                var keyId = entryRef.FindPropertyRelative("m_KeyId");
                if (keyId != null)
                {
                    keyId.longValue = source.TableEntryReference.KeyId;
                }
            }
        }

        private void CopyConditionList(SerializedProperty prop, ConditionList sourceConditions)
        {
            if (prop == null)
                return;

            prop.ClearArray();

            if (sourceConditions == null || sourceConditions.Count == 0)
                return;

            foreach (var condition in sourceConditions)
            {
                if (condition != null)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = condition;
                }
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Checks if this inline data has valid configuration for the specified task type.
        /// </summary>
        /// <param name="taskType">The task type to validate for.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValidFor(TaskType taskType)
        {
            // Base validation: devName must not be empty
            if (string.IsNullOrWhiteSpace(devName))
                return false;

            // Type-specific validation
            return taskType switch
            {
                TaskType.Int => requiredCount >= 1,
                TaskType.Timed => timeLimit >= 1f,
                _ => true // Bool, String, Location, Discovery have no additional requirements
            };
        }

        /// <summary>Checks if valid for Bool task.</summary>
        public bool IsValidForBool() => IsValidFor(TaskType.Bool);

        /// <summary>Checks if valid for Int task.</summary>
        public bool IsValidForInt() => IsValidFor(TaskType.Int);

        /// <summary>Checks if valid for String task.</summary>
        public bool IsValidForString() => IsValidFor(TaskType.String);

        /// <summary>Checks if valid for Location task.</summary>
        public bool IsValidForLocation() => IsValidFor(TaskType.Location);

        /// <summary>Checks if valid for Discovery task.</summary>
        public bool IsValidForDiscovery() => IsValidFor(TaskType.Discovery);

        /// <summary>Checks if valid for Timed task.</summary>
        public bool IsValidForTimed() => IsValidFor(TaskType.Timed);

        #endregion
    }
}
