using System;
using HelloDev.Conditions;
using HelloDev.IDs;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for discovery-based tasks (TaskDiscovery_SO).
    /// Tracks discovery of multiple items/locations/objectives.
    /// </summary>
    /// <remarks>
    /// Discovery tasks allow completing a subset of conditions.
    /// If requiredDiscoveries is 0, all conditions must be met.
    /// Use for objectives like "Discover 3 of 5 Hidden Locations" or "Find All Artifacts".
    ///
    /// Supports two modes controlled by "Use Task Asset" checkbox:
    /// - Asset Mode (checked): Reference an existing TaskDiscovery_SO asset
    /// - Define Mode (unchecked): Define task inline with ports + dynamic condition/discovery ports
    /// </remarks>
    [Serializable]
    public class TaskDiscoveryBlock : TaskBlockBase
    {
        #region Option Names

        private const string OPT_USE_TASK_ASSET = "UseTaskAsset";
        private const string OPT_DISCOVERY_TASK_ASSET = "DiscoveryTaskAsset";
        private const string OPT_REQUIRED_DISCOVERIES = "RequiredDiscoveries";
        private const string OPT_TRIGGER_CONDITION_COUNT = "TriggerConditionCount";
        private const string OPT_FAILURE_CONDITION_COUNT = "FailureConditionCount";

        #endregion

        #region Port Names

        private const string PORT_TASK_ASSET = "TaskAssetInput";
        private const string PORT_DEV_NAME = "DevNameInput";
        private const string PORT_DISPLAY_NAME = "DisplayNameInput";
        private const string PORT_DESCRIPTION = "DescriptionInput";
        private const string PORT_DISCOVERY = "DiscoveryInput";
        private const string PORT_TRIGGER_CONDITION = "TriggerConditionInput";
        private const string PORT_FAILURE_CONDITION = "FailureConditionInput";

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use an existing Task Asset (true) or define inline (false).
        /// </summary>
        public bool UseTaskAsset => GetOptionValue<bool>(OPT_USE_TASK_ASSET);

        /// <summary>
        /// Number of discoveries required. Controls how many Discovery ID ports are shown.
        /// </summary>
        public int RequiredDiscoveries => GetOptionValue<int>(OPT_REQUIRED_DISCOVERIES);

        /// <summary>
        /// Number of trigger condition ports to show.
        /// </summary>
        public int TriggerConditionCount => GetOptionValue<int>(OPT_TRIGGER_CONDITION_COUNT);

        /// <summary>
        /// Number of failure condition ports to show.
        /// </summary>
        public int FailureConditionCount => GetOptionValue<int>(OPT_FAILURE_CONDITION_COUNT);

        /// <inheritdoc/>
        public override Task_SO TaskAsset => UseTaskAsset
            ? GetOptionValue<TaskDiscovery_SO>(OPT_DISCOVERY_TASK_ASSET)
            : null;

        /// <inheritdoc/>
        public override string TaskTypeName => "Discovery";

        /// <inheritdoc/>
        public override bool HasValidTask
        {
            get
            {
                if (IsAssetMode)
                    return TaskAsset != null;
                var devName = GetOptionValue<string>(OPT_DEV_NAME);
                return !string.IsNullOrWhiteSpace(devName);
            }
        }

        #endregion

        #region Implementation

        /// <inheritdoc/>
        protected override string GetAssetOptionName() => OPT_DISCOVERY_TASK_ASSET;

        /// <inheritdoc/>
        protected override void DefineAssetModeOption(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(OPT_USE_TASK_ASSET)
                .WithDisplayName("Use Task Asset")
                .WithDefaultValue(false)
                .WithTooltip("Check to use an existing TaskDiscovery_SO asset.\nUncheck to define task inline.");

            if (UseTaskAsset)
            {
                context.AddOption<TaskDiscovery_SO>(OPT_DISCOVERY_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .WithTooltip("Reference to a TaskDiscovery_SO asset");
            }
        }

        /// <inheritdoc/>
        protected override void OnDefineTypeSpecificOptions(IOptionDefinitionContext context)
        {
            if (!UseTaskAsset)
            {
                context.AddOption<int>(OPT_REQUIRED_DISCOVERIES)
                    .WithDisplayName("Required Discoveries")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of discoveries required. Controls how many Discovery ID ports appear.");

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

        /// <inheritdoc/>
        protected override void AddTypeSpecificPorts(IPortDefinitionContext context)
        {
            if (UseTaskAsset)
            {
                context.AddInputPort<TaskDiscovery_SO>(PORT_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .Build();
            }
            else
            {
                context.AddInputPort<string>(PORT_DEV_NAME)
                    .WithDisplayName("Dev Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DISPLAY_NAME)
                    .WithDisplayName("Display Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DESCRIPTION)
                    .WithDisplayName("Description")
                    .Build();

                // Dynamic Discovery ID ports
                for (int i = 0; i < RequiredDiscoveries; i++)
                {
                    context.AddInputPort<ID_SO>(PORT_DISCOVERY + i)
                        .WithDisplayName($"Discovery ID {i + 1}")
                        .Build();
                }

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

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            var task = UnityEngine.ScriptableObject.CreateInstance<TaskDiscovery_SO>();
            return task;
        }

        #endregion
    }
}
