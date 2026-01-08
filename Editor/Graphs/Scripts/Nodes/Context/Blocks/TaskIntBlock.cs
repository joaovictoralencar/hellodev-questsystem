using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for integer-based tasks (TaskInt_SO).
    /// Tracks progress toward a required count.
    /// </summary>
    /// <remarks>
    /// Use for objectives like "Kill 10 Goblins" or "Collect 5 Items".
    /// Type-specific: RequiredCount port.
    /// </remarks>
    [Serializable]
    public class TaskIntBlock : TaskTypedBlock<TaskInt_SO>
    {
        private const string PORT_REQUIRED_COUNT = "RequiredCountInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Int";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "IntTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskInt_SO";

        /// <summary>
        /// Gets the required count from the port.
        /// </summary>
        public int RequiredCount => GraphTraversalUtility.ResolveDataPort<int>(this, PORT_REQUIRED_COUNT, 1);

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<int>(PORT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .Build();
        }

        /// <inheritdoc/>
        protected override bool ValidateTypeSpecificFields()
        {
            return RequiredCount >= 1;
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            return ScriptableObject.CreateInstance<TaskInt_SO>();
        }
    }
}
