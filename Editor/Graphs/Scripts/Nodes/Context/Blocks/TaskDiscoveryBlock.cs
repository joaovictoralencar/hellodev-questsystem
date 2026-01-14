using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for discovery-based tasks (TaskDiscovery_SO).
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
    public class TaskDiscoveryBlock : TaskTypedBlock<TaskDiscovery_SO>
    {
        private const string PORT_REQUIRED_DISCOVERIES = "RequiredDiscoveriesInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Discovery";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "DiscoveryTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskDiscovery_SO";

        /// <summary>
        /// Number of discoveries required to complete the task.
        /// If 0, all trigger conditions must be fulfilled.
        /// </summary>
        public int RequiredDiscoveries => GraphTraversalUtility.ResolveDataPort<int>(this, PORT_REQUIRED_DISCOVERIES, 0);

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<int>(PORT_REQUIRED_DISCOVERIES)
                .WithDisplayName("Required Discoveries")
                .Build();
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            var task = CreateTaskAssetWithCommonFields<TaskDiscovery_SO>();

            // Set type-specific field
            var so = new SerializedObject(task);
            so.FindProperty("requiredDiscoveries").intValue = RequiredDiscoveries;
            so.ApplyModifiedPropertiesWithoutUndo();

            return task;
        }
    }
}
