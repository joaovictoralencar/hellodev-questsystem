using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task node for integer-based tasks (TaskInt_SO).
    /// Tracks progress toward a required count.
    /// </summary>
    /// <remarks>
    /// Int tasks require conditions to be met multiple times.
    /// Use for objectives like "Kill 10 Goblins" or "Collect 5 Items".
    /// </remarks>
    [Serializable]
    public class TaskIntNode : TaskTypedNode<TaskInt_SO>
    {
        private const string PORT_REQUIRED_COUNT = "RequiredCountInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Int";

        /// <summary>
        /// The required count from port (Define mode only).
        /// </summary>
        public int RequiredCount => GraphTraversalUtility.ResolveDataPort<int>(this, PORT_REQUIRED_COUNT, 1);

        /// <summary>
        /// Resolves the required count from the appropriate source.
        /// </summary>
        public int ResolveRequiredCount()
        {
            if (IsAssetMode && TaskAsset is TaskInt_SO intTask)
                return intTask.RequiredCount;
            return RequiredCount;
        }

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<int>(PORT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .Build();
        }

        /// <inheritdoc/>
        protected override bool ValidateTypeSpecificFields() => RequiredCount >= 1;

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            data.requiredCount = ResolveRequiredCount();
        }
    }
}
