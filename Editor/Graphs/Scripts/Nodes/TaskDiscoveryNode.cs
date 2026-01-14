using System;
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
    /// Discovery tasks use the standard Conditions from Task_SO base class.
    /// Each condition represents one discoverable item/clue.
    /// RequiredDiscoveries specifies how many conditions must be fulfilled.
    /// If RequiredDiscoveries is 0, all conditions must be fulfilled.
    ///
    /// Use for objectives like "Discover 3 of 5 Hidden Locations" or "Find All Artifacts".
    /// </remarks>
    [Serializable]
    public class TaskDiscoveryNode : TaskTypedNode<TaskDiscovery_SO>
    {
        private const string PORT_REQUIRED_DISCOVERIES = "RequiredDiscoveriesInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Discovery";

        /// <summary>
        /// Number of discoveries required to complete the task.
        /// If 0, all trigger conditions must be fulfilled.
        /// </summary>
        public int RequiredDiscoveries => GraphTraversalUtility.ResolveDataPort<int>(this, PORT_REQUIRED_DISCOVERIES, 0);

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
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<int>(PORT_REQUIRED_DISCOVERIES)
                .WithDisplayName("Required Discoveries")
                .Build();
        }

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            data.requiredDiscoveries = ResolveRequiredDiscoveries();
        }
    }
}
