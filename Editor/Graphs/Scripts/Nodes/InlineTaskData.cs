using System;
using System.Collections.Generic;
using System.Reflection;
using HelloDev.Conditions;
using HelloDev.QuestSystem;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using IVariable = UnityEngine.Localization.SmartFormat.PersistentVariables.IVariable;

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
        /// <param name="persistentTaskId">Optional persistent task ID. If provided, uses this ID instead of generating a new one.</param>
        /// <returns>A new Task_SO instance with data from this inline definition.</returns>
        public TTask CreateTaskAsset<TTask>(string persistentTaskId = null) where TTask : Task_SO
        {
            var task = ScriptableObject.CreateInstance<TTask>();
            SetFieldsViaSerializedObject(task, persistentTaskId);
            return task;
        }

        // Track pending LocalizedString copies that need smart variables applied after serialization
        [NonSerialized]
        private List<(string propertyPath, LocalizedString source)> _pendingLocalizedStringCopies = new();

        private void SetFieldsViaSerializedObject(Task_SO task, string persistentTaskId = null)
        {
            // Clear any pending copies from previous calls
            _pendingLocalizedStringCopies.Clear();

            var so = new SerializedObject(task);

            // Common fields
            so.FindProperty("devName").stringValue = devName;
            // Use persistent ID if provided, otherwise generate a new one (for backward compatibility)
            so.FindProperty("taskId").stringValue = !string.IsNullOrEmpty(persistentTaskId)
                ? persistentTaskId
                : Guid.NewGuid().ToString();

            // Localization: Copy LocalizedString references
            CopyLocalizedString(so, so.FindProperty("displayName"), displayName);
            CopyLocalizedString(so, so.FindProperty("taskDescription"), taskDescription);

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

            // Apply smart/local variables after serialization is applied
            ApplyPendingSmartVariables(so);
        }

        private void CopyLocalizedString(SerializedObject so, SerializedProperty prop, LocalizedString source)
        {
            if (prop == null || source == null || source.IsEmpty)
                return;

            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef != null && source.TableReference.TableCollectionNameGuid != Guid.Empty)
            {
                var tableCollectionName = tableRef.FindPropertyRelative("m_TableCollectionName");
                if (tableCollectionName != null)
                {
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

            // Store for later application of smart variables after ApplyModifiedProperties
            _pendingLocalizedStringCopies.Add((prop.propertyPath, source));
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

        #region LocalizedString Smart Variables

        // Use lazy initialization to ensure reflection happens at the right time
        private static FieldInfo _variablesSourceField;
        private static FieldInfo _variablesDictField;
        private static bool _reflectionInitialized;

        // Reset reflection on domain reload
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetReflectionOnDomainReload()
        {
            _reflectionInitialized = false;
            _variablesSourceField = null;
            _variablesDictField = null;
        }

        private static FieldInfo VariablesSourceField
        {
            get
            {
                EnsureReflectionInitialized();
                return _variablesSourceField;
            }
        }

        private static FieldInfo VariablesDictField
        {
            get
            {
                EnsureReflectionInitialized();
                return _variablesDictField;
            }
        }

        private static void EnsureReflectionInitialized()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            // Try new Unity Localization field names first (m_LocalVariables), then fall back to old (m_Variables)
            _variablesSourceField = typeof(LocalizedString).GetField("m_LocalVariables", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_variablesSourceField == null)
                _variablesSourceField = typeof(LocalizedString).GetField("m_Variables", BindingFlags.Instance | BindingFlags.NonPublic);

            // For PersistentVariablesSource, try m_Variables (old) - new versions may not use this class the same way
            _variablesDictField = typeof(PersistentVariablesSource).GetField("m_Variables", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Applies smart variables to LocalizedStrings after serialization.
        /// Must be called after so.ApplyModifiedPropertiesWithoutUndo().
        /// </summary>
        private void ApplyPendingSmartVariables(SerializedObject so)
        {
            so.Update();

            foreach (var (propertyPath, source) in _pendingLocalizedStringCopies)
            {
                // Get the target LocalizedString via reflection (search base classes too)
                var fieldInfo = GetFieldIncludingBaseClasses(so.targetObject?.GetType(), propertyPath);
                if (fieldInfo?.GetValue(so.targetObject) is LocalizedString target)
                {
                    CopySmartVariables(target, source);
                }
            }
            _pendingLocalizedStringCopies.Clear();

            EditorUtility.SetDirty(so.targetObject);
        }

        /// <summary>
        /// Copies smart/local variables from source to target LocalizedString.
        /// Handles both old (PersistentVariablesSource) and new (List) Unity Localization APIs.
        /// </summary>
        private static void CopySmartVariables(LocalizedString target, LocalizedString source)
        {
            if (target == null || source == null || VariablesSourceField == null)
                return;

            var srcValue = VariablesSourceField.GetValue(source);
            if (srcValue == null)
                return;

            // Handle new Unity Localization API: m_LocalVariables is a List<LocalVariable>
            if (srcValue is System.Collections.IList srcList)
            {
                if (srcList.Count == 0)
                    return;

                var dstValue = VariablesSourceField.GetValue(target);
                var dstList = dstValue as System.Collections.IList
                    ?? Activator.CreateInstance(srcValue.GetType()) as System.Collections.IList;

                if (dstList == null)
                    return;

                dstList.Clear();
                foreach (var item in srcList)
                {
                    if (item == null) continue;
                    var clone = CloneLocalVariable(item);
                    if (clone != null)
                        dstList.Add(clone);
                }

                VariablesSourceField.SetValue(target, dstList);
                return;
            }

            // Handle old Unity Localization API: m_Variables is a PersistentVariablesSource
            if (srcValue is PersistentVariablesSource srcSource && VariablesDictField != null)
            {
                var srcDict = VariablesDictField.GetValue(srcSource) as Dictionary<string, IVariable>;
                if (srcDict == null || srcDict.Count == 0)
                    return;

                var formatter = LocalizationSettings.StringDatabase?.SmartFormatter;
                if (formatter == null)
                    return;

                var dstSource = new PersistentVariablesSource(formatter);
                var dstDict = new Dictionary<string, IVariable>();

                foreach (var pair in srcDict)
                {
                    if (pair.Value == null) continue;
                    var clone = CloneVariable(pair.Value);
                    if (clone != null)
                        dstDict[pair.Key] = clone;
                }

                VariablesDictField.SetValue(dstSource, dstDict);
                VariablesSourceField.SetValue(target, dstSource);
            }
        }

        /// <summary>
        /// Clones a LocalVariable object (new Unity Localization API).
        /// Traverses the full type hierarchy to ensure all fields are copied.
        /// </summary>
        private static object CloneLocalVariable(object source)
        {
            if (source == null) return null;

            var type = source.GetType();
            var clone = Activator.CreateInstance(type);

            // Traverse the type hierarchy to get all fields including base classes
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                foreach (var field in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsInitOnly) continue;
                    try
                    {
                        var value = field.GetValue(source);
                        // Deep clone IVariable values
                        if (value is IVariable variable)
                            value = CloneVariable(variable);
                        field.SetValue(clone, value);
                    }
                    catch { /* Ignore non-copyable fields */ }
                }
                currentType = currentType.BaseType;
            }

            return clone;
        }

        /// <summary>
        /// Clones an IVariable instance via reflection.
        /// Traverses the full type hierarchy to ensure base class fields (like m_Value) are copied.
        /// </summary>
        private static IVariable CloneVariable(IVariable source)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var clone = Activator.CreateInstance(type);

            // Traverse the type hierarchy to get all fields including base classes
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                foreach (var field in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsInitOnly)
                        continue;

                    try
                    {
                        field.SetValue(clone, field.GetValue(source));
                    }
                    catch { /* Ignore non-copyable fields */ }
                }
                currentType = currentType.BaseType;
            }

            return clone as IVariable;
        }

        /// <summary>
        /// Gets a field by name, searching through the type hierarchy (base classes).
        /// </summary>
        private static FieldInfo GetFieldIncludingBaseClasses(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
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
