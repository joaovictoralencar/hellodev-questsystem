using System;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Generic base class for typed task nodes.
    /// Consolidates all common functionality shared across TaskBoolNode, TaskIntNode,
    /// TaskStringNode, TaskLocationNode, TaskDiscoveryNode, and TaskTimedNode.
    /// </summary>
    /// <typeparam name="TTaskSO">The specific Task_SO subclass (e.g., TaskBool_SO, TaskInt_SO).</typeparam>
    /// <remarks>
    /// Supports two modes controlled by "Use Task Asset" checkbox:
    /// - Asset Mode (checked): Shows Task Asset input port
    /// - Define Mode (unchecked): Shows inline data ports + dynamic condition ports
    ///
    /// Concrete implementations only need to override:
    /// - TaskTypeName (e.g., "Bool", "Int", "Timed")
    /// - DefineTypeSpecificPorts() for extra ports (e.g., RequiredCount, TimeLimit)
    /// - DefineTypeSpecificOptions() for extra options (e.g., RequiredDiscoveries)
    /// - PopulateTypeSpecificData() to fill InlineTaskData
    /// - ValidateTypeSpecificFields() for extra validation
    /// </remarks>
    [Serializable]
    public abstract class TaskTypedNode<TTaskSO> : TaskBaseNode where TTaskSO : Task_SO
    {
        #region Option Names

        protected const string OPT_USE_TASK_ASSET = "UseTaskAsset";
        protected const string OPT_TRIGGER_CONDITION_COUNT = "TriggerConditionCount";
        protected const string OPT_FAILURE_CONDITION_COUNT = "FailureConditionCount";

        #endregion

        #region Port Names

        protected const string PORT_TASK_ASSET = "TaskAssetInput";
        protected const string PORT_DEV_NAME = "DevNameInput";
        protected const string PORT_DISPLAY_NAME = "DisplayNameInput";
        protected const string PORT_DESCRIPTION = "DescriptionInput";
        protected const string PORT_TRIGGER_CONDITION = "TriggerConditionInput";
        protected const string PORT_FAILURE_CONDITION = "FailureConditionInput";

        #endregion

        #region Common Properties

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
                return GraphTraversalUtility.ResolveDataPort<TTaskSO>(this, PORT_TASK_ASSET, null);
            }
        }

        /// <summary>
        /// The dev name from port (Define mode only).
        /// </summary>
        public string PortDevName => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_DEV_NAME, "New Task");

        /// <inheritdoc/>
        public sealed override bool HasValidTask
        {
            get
            {
                if (IsAssetMode)
                    return TaskAsset != null;

                if (string.IsNullOrWhiteSpace(PortDevName))
                    return false;

                return ValidateTypeSpecificFields();
            }
        }

        #endregion

        #region Port Definition

        /// <inheritdoc/>
        protected sealed override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Flow ports
            context.AddInputPort<TaskFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<TaskFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            if (UseTaskAsset)
            {
                // Asset Mode: Show Task Asset input port only
                context.AddInputPort<TTaskSO>(PORT_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .Build();
            }
            else
            {
                // Define Mode: Show inline data ports
                context.AddInputPort<string>(PORT_DEV_NAME)
                    .WithDisplayName("Dev Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DISPLAY_NAME)
                    .WithDisplayName("Display Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DESCRIPTION)
                    .WithDisplayName("Description")
                    .Build();

                // Type-specific ports (e.g., RequiredCount, TimeLimit)
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
        }

        /// <summary>
        /// Override to define type-specific ports (e.g., RequiredCount for Int tasks).
        /// Called after common ports but before condition ports in Define mode.
        /// </summary>
        protected virtual void DefineTypeSpecificPorts(IPortDefinitionContext context) { }

        #endregion

        #region Option Definition

        protected sealed override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(OPT_USE_TASK_ASSET)
                .WithDisplayName("Use Task Asset")
                .WithDefaultValue(false)
                .WithTooltip($"Check to use an existing {typeof(TTaskSO).Name} asset.\nUncheck to define task inline.");

            // Only show count options in Define mode
            if (!UseTaskAsset)
            {
                // Type-specific options (e.g., RequiredDiscoveries for Discovery tasks)
                DefineTypeSpecificOptions(context);

                context.AddOption<int>(OPT_TRIGGER_CONDITION_COUNT)
                    .WithDisplayName("Trigger Condition Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of trigger condition ports to show.");

                context.AddOption<int>(OPT_FAILURE_CONDITION_COUNT)
                    .WithDisplayName("Failure Condition Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of failure condition ports to show.");
            }
        }

        /// <summary>
        /// Override to define type-specific options (e.g., RequiredDiscoveries for Discovery tasks).
        /// Called before trigger/failure condition count options in Define mode.
        /// </summary>
        protected virtual void DefineTypeSpecificOptions(IOptionDefinitionContext context) { }

        /// <inheritdoc/>
        protected sealed override void DefineAssetModeOptions(IOptionDefinitionContext context) { }

        /// <inheritdoc/>
        protected sealed override void OnDefineTypeSpecificOptions(IOptionDefinitionContext context) { }

        #endregion

        #region Validation

        /// <summary>
        /// Override to add type-specific validation (e.g., RequiredCount >= 1 for Int tasks).
        /// Called when validating HasValidTask in Define mode.
        /// </summary>
        /// <returns>True if type-specific fields are valid.</returns>
        protected virtual bool ValidateTypeSpecificFields() => true;

        #endregion

        #region Task Creation

        /// <inheritdoc/>
        public sealed override Task_SO CreateTaskAsset()
        {
            return InlineData.CreateTaskAsset<TTaskSO>();
        }

        #endregion
    }
}
