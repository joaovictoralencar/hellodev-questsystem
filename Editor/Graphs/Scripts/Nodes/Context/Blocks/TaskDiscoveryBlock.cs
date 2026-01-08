using System;
using HelloDev.IDs;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

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
    /// Type-specific: RequiredDiscoveries option, dynamic Discovery ID ports.
    /// </remarks>
    [Serializable]
    public class TaskDiscoveryBlock : TaskTypedBlock<TaskDiscovery_SO>
    {
        private const string OPT_REQUIRED_DISCOVERIES = "RequiredDiscoveries";
        private const string PORT_DISCOVERY = "DiscoveryInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Discovery";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "DiscoveryTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskDiscovery_SO";

        /// <summary>
        /// Number of discoveries required. Controls how many Discovery ID ports are shown.
        /// </summary>
        public int RequiredDiscoveries => GetOptionValue<int>(OPT_REQUIRED_DISCOVERIES);

        /// <inheritdoc/>
        protected override void DefineTypeSpecificOptions(IOptionDefinitionContext context)
        {
            context.AddOption<int>(OPT_REQUIRED_DISCOVERIES)
                .WithDisplayName("Required Discoveries")
                .WithDefaultValue(0)
                .WithTooltip("Number of discoveries required. Controls how many Discovery ID ports appear.");
        }

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            for (int i = 0; i < RequiredDiscoveries; i++)
            {
                context.AddInputPort<ID_SO>(PORT_DISCOVERY + i)
                    .WithDisplayName($"Discovery ID {i + 1}")
                    .Build();
            }
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            return ScriptableObject.CreateInstance<TaskDiscovery_SO>();
        }
    }
}
