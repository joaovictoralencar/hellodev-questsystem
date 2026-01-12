using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for string-based tasks (TaskString_SO).
    /// Completes when a specific string value is matched.
    /// </summary>
    /// <remarks>
    /// Use for objectives like "Enter Password" or "Say the Magic Word".
    /// Type-specific: TargetValue port.
    /// </remarks>
    [Serializable]
    public class TaskStringBlock : TaskTypedBlock<TaskString_SO>
    {
        private const string PORT_TARGET_VALUE = "TargetValueInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "String";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "StringTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskString_SO";

        /// <summary>
        /// Gets the target value from the port.
        /// </summary>
        public string TargetValue => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_TARGET_VALUE, string.Empty);

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<string>(PORT_TARGET_VALUE)
                .WithDisplayName("Target Value")
                .Build();
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            return ScriptableObject.CreateInstance<TaskString_SO>();
        }
    }
}
