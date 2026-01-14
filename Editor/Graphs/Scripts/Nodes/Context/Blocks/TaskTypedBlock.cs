using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

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
        public new string DevName
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
            var task = ScriptableObject.CreateInstance<T>();
            var so = new SerializedObject(task);

            // Common fields
            so.FindProperty("devName").stringValue = DevName;
            so.FindProperty("taskId").stringValue = Guid.NewGuid().ToString();

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

        /// <summary>
        /// Copies a LocalizedString to a SerializedProperty.
        /// </summary>
        private static void CopyLocalizedStringToProperty(SerializedProperty prop, LocalizedString source)
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
