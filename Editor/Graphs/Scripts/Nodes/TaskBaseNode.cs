using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Abstract base class for all task nodes.
    /// Provides common functionality for Asset/Define mode auto-detection.
    /// </summary>
    /// <remarks>
    /// Mode is auto-detected based on whether a Task Asset is assigned:
    /// - If Task Asset is set → uses the referenced asset (Asset mode)
    /// - If Task Asset is empty → uses inline data to create a new task (Define mode)
    ///
    /// Concrete implementations (TaskBoolNode, TaskIntNode, etc.) provide
    /// type-specific options and asset creation logic.
    /// </remarks>
    [Serializable]
    public abstract class TaskBaseNode : QuestBaseNode
    {
        #region Option Names

        protected const string OPT_TASK_ASSET = "TaskAsset";

        // Common inline task data options (used in Define mode)
        protected const string OPT_DEV_NAME = "DevName";
        protected const string OPT_DISPLAY_NAME = "DisplayName";
        protected const string OPT_TASK_DESCRIPTION = "TaskDescription";
        protected const string OPT_CONDITIONS = "Conditions";
        protected const string OPT_FAILURE_CONDITIONS = "FailureConditions";

        #endregion

        #region Properties

        /// <summary>
        /// The referenced task asset. If set, Asset mode is used.
        /// If null, Define mode is used (inline data creates a new task).
        /// </summary>
        public abstract Task_SO TaskAsset { get; }

        /// <summary>
        /// Whether this node uses Asset mode (auto-detected: TaskAsset is set).
        /// </summary>
        public bool IsAssetMode => TaskAsset != null;

        /// <summary>
        /// Whether this node uses Define mode (auto-detected: TaskAsset is null).
        /// </summary>
        public bool IsDefineMode => TaskAsset == null;

        /// <summary>
        /// Gets the inline task data constructed from options/ports.
        /// Override in subclasses to customize how data is populated.
        /// </summary>
        public virtual InlineTaskData InlineData
        {
            get
            {
                var data = CreateBaseInlineData();
                PopulateTypeSpecificData(data);
                return data;
            }
        }

        /// <summary>
        /// Dev name for display (works for both modes).
        /// </summary>
        public string DevName
        {
            get
            {
                if (IsAssetMode)
                {
                    return TaskAsset != null ? TaskAsset.DevName : "No Task Assigned";
                }
                else
                {
                    var name = GetOptionValue<string>(OPT_DEV_NAME);
                    return !string.IsNullOrEmpty(name) ? name : "Unnamed Task";
                }
            }
        }

        /// <summary>
        /// Task type name for display.
        /// </summary>
        public abstract string TaskTypeName { get; }

        /// <summary>
        /// Returns true if this node has a valid task configuration.
        /// </summary>
        public abstract bool HasValidTask { get; }

        #endregion

        #region Abstract Members

        /// <summary>
        /// Creates a Task_SO asset from the inline data.
        /// Called during graph export when in Define mode.
        /// </summary>
        /// <returns>A new Task_SO instance.</returns>
        public abstract Task_SO CreateTaskAsset();

        /// <summary>
        /// Defines type-specific options (e.g., RequiredCount for TaskIntNode).
        /// Called during OnDefineOptions after common options are defined.
        /// </summary>
        protected abstract void OnDefineTypeSpecificOptions(IOptionDefinitionContext context);

        /// <summary>
        /// Populates type-specific data in the InlineTaskData.
        /// </summary>
        protected abstract void PopulateTypeSpecificData(InlineTaskData data);

        /// <summary>
        /// Whether to show inline options (Define mode).
        /// Override to return false when using Task Asset mode.
        /// </summary>
        protected virtual bool ShowInlineOptions => true;

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Task asset picker (if set, uses Asset mode; if empty, uses Define mode)
            DefineAssetModeOptions(context);

            // Inline task options (used when Task Asset is empty)
            DefineCommonInlineOptions(context);

            // Type-specific options (implemented by concrete classes)
            OnDefineTypeSpecificOptions(context);
        }

        /// <summary>
        /// Defines options for Asset mode.
        /// Override in concrete classes to specify the correct Task_SO type.
        /// </summary>
        protected abstract void DefineAssetModeOptions(IOptionDefinitionContext context);

        /// <summary>
        /// Defines common inline task options shared by all task types.
        /// Override in subclasses to add conditional visibility.
        /// </summary>
        protected virtual void DefineCommonInlineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_DEV_NAME)
                .WithDisplayName("Dev Name")
                .WithDefaultValue("New Task")
                .WithTooltip("Internal name for developers")
                .ShowInInspectorOnly()
                .Delayed();

            context.AddOption<LocalizedString>(OPT_DISPLAY_NAME)
                .WithDisplayName("Display Name")
                .WithTooltip("Localized display name shown in UI")
                .ShowInInspectorOnly();

            context.AddOption<LocalizedString>(OPT_TASK_DESCRIPTION)
                .WithDisplayName("Description")
                .WithTooltip("Localized task description")
                .ShowInInspectorOnly();

            context.AddOption<ConditionList>(OPT_CONDITIONS)
                .WithDisplayName("Start Conditions")
                .WithDefaultValue(new ConditionList())
                .WithTooltip("Conditions that complete this task")
                .ShowInInspectorOnly();

            context.AddOption<ConditionList>(OPT_FAILURE_CONDITIONS)
                .WithDisplayName("Failure Conditions")
                .WithDefaultValue(new ConditionList())
                .WithTooltip("Conditions that fail this task")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From TaskGroup or previous Task
            context.AddInputPort<TaskFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Output: Next task in sequence
            context.AddOutputPort<TaskFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates base InlineTaskData with common fields populated.
        /// </summary>
        private InlineTaskData CreateBaseInlineData()
        {
            return new InlineTaskData
            {
                devName = GetOptionValue<string>(OPT_DEV_NAME) ?? "New Task",
                displayName = GetOptionValue<LocalizedString>(OPT_DISPLAY_NAME),
                taskDescription = GetOptionValue<LocalizedString>(OPT_TASK_DESCRIPTION),
                conditions = GetOptionValue<ConditionList>(OPT_CONDITIONS) ?? new ConditionList(),
                failureConditions = GetOptionValue<ConditionList>(OPT_FAILURE_CONDITIONS) ?? new ConditionList()
            };
        }

        #endregion
    }
}
