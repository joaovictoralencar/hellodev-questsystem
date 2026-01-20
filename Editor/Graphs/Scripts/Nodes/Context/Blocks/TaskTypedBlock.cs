using System;
using System.Collections.Generic;
using System.Reflection;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using Unity.GraphToolkit.Editor;
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
    /// Generic base class for typed task blocks that eliminates duplication.
    /// </summary>
    /// <typeparam name="TTaskSO">The concrete Task_SO type (e.g., TaskBool_SO, TaskInt_SO).</typeparam>
    /// <remarks>
    /// Provides common functionality for all task blocks:
    /// - Asset mode toggle and picker
    /// - Common inline options (trigger/failure condition counts)
    /// - Common inline ports (dev name, display name, description, conditions)
    /// - HasValidTask validation
    ///
    /// Subclasses only need to implement:
    /// - TaskTypeName (string like "Bool", "Int")
    /// - TaskAssetOptionName (like "BoolTaskAsset")
    /// - TaskAssetTooltipType (like "TaskBool_SO")
    /// - DefineTypeSpecificOptions (optional)
    /// - DefineTypeSpecificPorts (optional)
    /// - ValidateTypeSpecificFields (optional)
    /// - CreateTaskAsset
    /// </remarks>
    [Serializable]
    public abstract class TaskTypedBlock<TTaskSO> : TaskBlockBase where TTaskSO : Task_SO
    {
        #region Persistent Task ID

        /// <summary>
        /// Persistent task ID that stays constant across graph exports.
        /// This ensures save/load works correctly by maintaining stable GUIDs.
        /// </summary>
        [SerializeField]
        private string persistentTaskId;

        /// <summary>
        /// Gets the persistent task ID, generating one if needed.
        /// Once generated, this ID is serialized with the graph and remains stable.
        /// </summary>
        protected string PersistentTaskId
        {
            get
            {
                if (string.IsNullOrEmpty(persistentTaskId))
                {
                    persistentTaskId = Guid.NewGuid().ToString();
                }
                return persistentTaskId;
            }
        }

        #endregion

        #region Shared Option Names

        protected const string OPT_USE_TASK_ASSET = "UseTaskAsset";
        protected const string OPT_TRIGGER_CONDITION_COUNT = "TriggerConditionCount";
        protected const string OPT_FAILURE_CONDITION_COUNT = "FailureConditionCount";

        #endregion

        #region Shared Port Names

        protected const string PORT_TASK_ASSET = "TaskAssetInput";
        protected const string PORT_DEV_NAME = "DevNameInput";
        protected const string PORT_DISPLAY_NAME = "DisplayNameInput";
        protected const string PORT_DESCRIPTION = "DescriptionInput";
        protected const string PORT_TRIGGER_CONDITION = "TriggerConditionInput";
        protected const string PORT_FAILURE_CONDITION = "FailureConditionInput";

        #endregion

        #region Abstract Members

        /// <summary>
        /// Gets the option name for the task asset (e.g., "BoolTaskAsset").
        /// </summary>
        protected abstract string TaskAssetOptionName { get; }

        /// <summary>
        /// Gets the type name for tooltips (e.g., "TaskBool_SO").
        /// </summary>
        protected abstract string TaskAssetTooltipType { get; }

        #endregion

        #region Shared Properties

        /// <summary>
        /// Whether to use an existing Task Asset (true) or define inline (false).
        /// </summary>
        public bool UseTaskAsset => GetOptionValue<bool>(OPT_USE_TASK_ASSET);

        /// <summary>
        /// Number of trigger condition ports to show.
        /// </summary>
        public int TriggerConditionCount => GetOptionValue<int>(OPT_TRIGGER_CONDITION_COUNT);

        /// <summary>
        /// Number of failure condition ports to show.
        /// </summary>
        public int FailureConditionCount => GetOptionValue<int>(OPT_FAILURE_CONDITION_COUNT);

        /// <inheritdoc/>
        public sealed override Task_SO TaskAsset
        {
            get
            {
                if (!UseTaskAsset)
                    return null;

                // Check option first (embedded value in inspector)
                var optionAsset = GetOptionValue<TTaskSO>(TaskAssetOptionName);
                if (optionAsset != null)
                    return optionAsset;

                // Also check port (connected or embedded port value)
                return GraphTraversalUtility.ResolveDataPort<TTaskSO>(this, PORT_TASK_ASSET, null);
            }
        }

        /// <summary>
        /// Dev name for this task (Define mode).
        /// Reads from port in Define mode.
        /// </summary>
        public override string DevName
        {
            get
            {
                if (IsAssetMode)
                    return TaskAsset?.DevName ?? "No Task";
                return GraphTraversalUtility.ResolveDataPort<string>(this, PORT_DEV_NAME, "Unnamed Task");
            }
        }

        /// <inheritdoc/>
        public sealed override bool HasValidTask
        {
            get
            {
                if (IsAssetMode)
                    return TaskAsset != null;

                // Use DevName property which reads from port
                if (string.IsNullOrWhiteSpace(DevName) || DevName == "Unnamed Task")
                    return false;

                return ValidateTypeSpecificFields();
            }
        }

        #endregion

        #region TaskBlockBase Implementation

        /// <inheritdoc/>
        protected sealed override string GetAssetOptionName() => TaskAssetOptionName;

        /// <inheritdoc/>
        protected sealed override void DefineAssetModeOption(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(OPT_USE_TASK_ASSET)
                .WithDisplayName("Use Task Asset")
                .WithDefaultValue(false)
                .WithTooltip($"Check to use an existing {TaskAssetTooltipType} asset.\nUncheck to define task inline.");

            if (UseTaskAsset)
            {
                context.AddOption<TTaskSO>(TaskAssetOptionName)
                    .WithDisplayName("Task Asset")
                    .WithTooltip($"Reference to a {TaskAssetTooltipType} asset");
            }
        }

        /// <inheritdoc/>
        protected sealed override void OnDefineTypeSpecificOptions(IOptionDefinitionContext context)
        {
            if (UseTaskAsset) return;

            // Type-specific options first (like RequiredDiscoveries)
            DefineTypeSpecificOptions(context);

            // Common condition count options
            context.AddOption<int>(OPT_TRIGGER_CONDITION_COUNT)
                .WithDisplayName("Trigger Condition Count")
                .WithDefaultValue(0)
                .WithTooltip("Number of trigger condition ports to show.");

            context.AddOption<int>(OPT_FAILURE_CONDITION_COUNT)
                .WithDisplayName("Failure Condition Count")
                .WithDefaultValue(0)
                .WithTooltip("Number of failure condition ports to show.");
        }

        /// <inheritdoc/>
        protected sealed override void AddTypeSpecificPorts(IPortDefinitionContext context)
        {
            if (UseTaskAsset)
            {
                context.AddInputPort<TTaskSO>(PORT_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .Build();
                return;
            }

            // Common inline ports
            context.AddInputPort<string>(PORT_DEV_NAME)
                .WithDisplayName("Dev Name")
                .Build();

            context.AddInputPort<LocalizedString>(PORT_DISPLAY_NAME)
                .WithDisplayName("Display Name")
                .Build();

            context.AddInputPort<LocalizedString>(PORT_DESCRIPTION)
                .WithDisplayName("Description")
                .Build();

            // Type-specific ports (like RequiredCount, TimeLimit)
            DefineTypeSpecificPorts(context);

            // Dynamic trigger condition ports
            for (int i = 0; i < TriggerConditionCount; i++)
            {
                context.AddInputPort<Condition_SO>(PORT_TRIGGER_CONDITION + i)
                    .WithDisplayName($"Trigger Condition {i + 1}")
                    .Build();
            }

            // Dynamic failure condition ports
            for (int i = 0; i < FailureConditionCount; i++)
            {
                context.AddInputPort<Condition_SO>(PORT_FAILURE_CONDITION + i)
                    .WithDisplayName($"Fail Condition {i + 1}")
                    .Build();
            }
        }

        #endregion

        #region Virtual Methods for Subclass Customization

        /// <summary>
        /// Override to define type-specific options (e.g., RequiredDiscoveries for Discovery tasks).
        /// Called only in Define mode.
        /// </summary>
        protected virtual void DefineTypeSpecificOptions(IOptionDefinitionContext context) { }

        /// <summary>
        /// Override to define type-specific ports (e.g., RequiredCount for Int tasks).
        /// Called only in Define mode, after common ports.
        /// </summary>
        protected virtual void DefineTypeSpecificPorts(IPortDefinitionContext context) { }

        /// <summary>
        /// Override to add type-specific validation (e.g., RequiredCount >= 1).
        /// Called only in Define mode when DevName is valid.
        /// </summary>
        protected virtual bool ValidateTypeSpecificFields() => true;

        #endregion

        #region Task Asset Creation Helpers

        /// <summary>
        /// Creates a Task_SO instance and populates common fields from ports.
        /// Subclasses should call this, then add type-specific fields.
        /// </summary>
        /// <typeparam name="T">The concrete Task_SO type to create.</typeparam>
        /// <returns>A new Task_SO instance with common fields populated.</returns>
        protected T CreateTaskAssetWithCommonFields<T>() where T : Task_SO
        {
            // Clear any pending copies from previous calls
            _pendingLocalizedStringCopies.Clear();

            var task = ScriptableObject.CreateInstance<T>();
            var so = new SerializedObject(task);

            // Common fields
            so.FindProperty("devName").stringValue = DevName;
            so.FindProperty("taskId").stringValue = PersistentTaskId;

            // Localization: Copy LocalizedString references from ports
            var displayNameLS = GraphTraversalUtility.ResolveDataPort<LocalizedString>(this, PORT_DISPLAY_NAME, default);
            var descriptionLS = GraphTraversalUtility.ResolveDataPort<LocalizedString>(this, PORT_DESCRIPTION, default);

            CopyLocalizedStringToProperty(so.FindProperty("displayName"), displayNameLS);
            CopyLocalizedStringToProperty(so.FindProperty("taskDescription"), descriptionLS);

            // Conditions from dynamic ports
            var triggerConditions = CollectConditionsFromPorts(PORT_TRIGGER_CONDITION, TriggerConditionCount);
            var failConditions = CollectConditionsFromPorts(PORT_FAILURE_CONDITION, FailureConditionCount);

            CopyConditionListToProperty(so.FindProperty("conditions"), triggerConditions);
            CopyConditionListToProperty(so.FindProperty("failureConditions"), failConditions);

            so.ApplyModifiedPropertiesWithoutUndo();

            // Apply smart/local variables after serialization is applied
            ApplyPendingSmartVariables(so);

            return task;
        }

        /// <summary>
        /// Collects Condition_SO references from numbered ports.
        /// </summary>
        private List<Condition_SO> CollectConditionsFromPorts(string portPrefix, int count)
        {
            var conditions = new List<Condition_SO>();
            for (int i = 0; i < count; i++)
            {
                var condition = GraphTraversalUtility.ResolveDataPort<Condition_SO>(this, portPrefix + i, null);
                if (condition != null)
                {
                    conditions.Add(condition);
                }
            }
            return conditions;
        }

        #region LocalizedString Copy with Smart Variables

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
        /// Copies a LocalizedString to a SerializedProperty, including local/smart variables.
        /// </summary>
        private void CopyLocalizedStringToProperty(SerializedProperty prop, LocalizedString source)
        {
            if (prop == null || source == null)
                return;

            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef != null && source.TableReference.TableCollectionNameGuid != Guid.Empty)
            {
                // Unity.Localization serializes as m_TableCollectionName with format "GUID:xxxxx"
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

        // Track pending LocalizedString copies that need smart variables applied after serialization
        [NonSerialized]
        private List<(string propertyPath, LocalizedString source)> _pendingLocalizedStringCopies = new();

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

        /// <summary>
        /// Copies a list of Condition_SO to a SerializedProperty array.
        /// </summary>
        private static void CopyConditionListToProperty(SerializedProperty prop, List<Condition_SO> conditions)
        {
            if (prop == null)
                return;

            prop.ClearArray();

            if (conditions == null)
                return;

            foreach (var condition in conditions)
            {
                if (condition != null)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = condition;
                }
            }
        }

        #endregion
    }
}
