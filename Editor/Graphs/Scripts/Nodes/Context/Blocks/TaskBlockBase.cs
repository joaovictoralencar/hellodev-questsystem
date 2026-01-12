using System;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Base class for all task blocks that can be placed inside a TaskGroupContextNode.
    /// </summary>
    /// <remarks>
    /// Task blocks are simplified versions of TaskNodes designed for inline editing
    /// within a context node. They support both Asset mode (reference existing Task_SO)
    /// and Define mode (create task inline).
    ///
    /// Each concrete block type (TaskIntBlock, TaskBoolBlock, etc.) handles
    /// type-specific configuration and uses the boolean toggle pattern for
    /// dynamic visibility (same as StageNode.HasPlayerChoices).
    /// </remarks>
    [UseWithContext(typeof(TaskGroupContextNode))]
    [Serializable]
    public abstract class TaskBlockBase : BlockNode
    {
        #region Option Names

        protected const string OPT_DEV_NAME = "DevName";
        protected const string OPT_DISPLAY_NAME = "DisplayName";
        protected const string OPT_TASK_DESCRIPTION = "TaskDescription";
        protected const string OPT_IS_OPTIONAL = "IsOptional";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the value of a node option by name.
        /// </summary>
        protected T GetOptionValue<T>(string optionName)
        {
            var option = GetNodeOptionByName(optionName);
            if (option != null && option.TryGetValue<T>(out var value))
                return value;
            return default;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The referenced task asset. If set, Asset mode is used.
        /// </summary>
        public abstract Task_SO TaskAsset { get; }

        /// <summary>
        /// Whether this block uses Asset mode (TaskAsset is set).
        /// </summary>
        public bool IsAssetMode => TaskAsset != null;

        /// <summary>
        /// Whether this block uses Define mode (inline data).
        /// </summary>
        public bool IsDefineMode => TaskAsset == null;

        /// <summary>
        /// Dev name for display (works for both modes).
        /// </summary>
        public string DevName
        {
            get
            {
                if (IsAssetMode)
                    return TaskAsset?.DevName ?? "No Task";
                return GetOptionValue<string>(OPT_DEV_NAME) ?? "Unnamed Task";
            }
        }

        /// <summary>
        /// Whether this task is optional (doesn't block group completion).
        /// </summary>
        public bool IsOptional => GetOptionValue<bool>(OPT_IS_OPTIONAL);

        /// <summary>
        /// Task type name for display.
        /// </summary>
        public abstract string TaskTypeName { get; }

        /// <summary>
        /// Returns true if this block has a valid task configuration.
        /// </summary>
        public abstract bool HasValidTask { get; }

        #endregion

        #region Abstract Members

        /// <summary>
        /// Creates a Task_SO asset from the inline data.
        /// Called during graph export when in Define mode.
        /// </summary>
        public abstract Task_SO CreateTaskAsset();

        /// <summary>
        /// Defines type-specific options (e.g., RequiredCount for TaskIntBlock).
        /// </summary>
        protected abstract void OnDefineTypeSpecificOptions(IOptionDefinitionContext context);

        /// <summary>
        /// Defines the asset mode toggle and picker options.
        /// Use boolean toggle pattern for dynamic visibility.
        /// </summary>
        protected abstract void DefineAssetModeOption(IOptionDefinitionContext context);

        /// <summary>
        /// Gets the option name used for the Task Asset field.
        /// </summary>
        protected abstract string GetAssetOptionName();

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Asset mode toggle and picker (handled by subclass with boolean pattern)
            DefineAssetModeOption(context);

            // Common inline options (subclass decides visibility via UseTaskAsset check)
            DefineCommonInlineOptions(context);

            // Type-specific options
            OnDefineTypeSpecificOptions(context);
        }

        /// <summary>
        /// Defines common inline options. Subclasses can check UseTaskAsset
        /// to conditionally skip these.
        /// </summary>
        protected virtual void DefineCommonInlineOptions(IOptionDefinitionContext context)
        {
            // By default, always define these. Subclasses override to add
            // conditional visibility based on UseTaskAsset boolean.
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Subclasses add conditional ports based on UseTaskAsset
            AddTypeSpecificPorts(context);
        }

        /// <summary>
        /// Override to add type-specific data ports.
        /// Use UseTaskAsset check for conditional visibility.
        /// </summary>
        protected virtual void AddTypeSpecificPorts(IPortDefinitionContext context)
        {
            // Default: no additional ports
        }

        #endregion
    }
}
