using System;
using HelloDev.IDs;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task node for discovery-based tasks (TaskDiscovery_SO).
    /// Tracks discovery of multiple items/locations/objectives.
    /// </summary>
    /// <remarks>
    /// Discovery tasks allow completing a subset of conditions.
    /// If requiredDiscoveries is 0, all conditions must be met.
    /// Use for objectives like "Discover 3 of 5 Hidden Locations" or "Find All Artifacts".
    /// </remarks>
    [Serializable]
    public class TaskDiscoveryNode : TaskTypedNode<TaskDiscovery_SO>
    {
        private const string OPT_REQUIRED_DISCOVERIES = "RequiredDiscoveries";
        private const string PORT_DISCOVERY = "DiscoveryInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Discovery";

        /// <summary>
        /// Number of discoveries required. Controls how many Discovery ID ports are shown.
        /// </summary>
        public int RequiredDiscoveries => GetOptionValue<int>(OPT_REQUIRED_DISCOVERIES);

        /// <summary>
        /// Resolves the required discoveries from the appropriate source.
        /// </summary>
        public int ResolveRequiredDiscoveries()
        {
            if (IsAssetMode && TaskAsset is TaskDiscovery_SO discoveryTask)
                return discoveryTask.RequiredDiscoveries;
            return RequiredDiscoveries;
        }

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
            // Dynamic Discovery ID ports based on RequiredDiscoveries option
            for (int i = 0; i < RequiredDiscoveries; i++)
            {
                context.AddInputPort<ID_SO>(PORT_DISCOVERY + i)
                    .WithDisplayName($"Discovery ID {i + 1}")
                    .Build();
            }
        }

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            data.requiredDiscoveries = ResolveRequiredDiscoveries();
        }
    }
}
