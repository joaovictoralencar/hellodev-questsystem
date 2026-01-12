using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task node for string-based tasks (TaskString_SO).
    /// Completes when a specific string value is matched.
    /// </summary>
    /// <remarks>
    /// String tasks track a string value toward a target.
    /// Use for objectives like "Enter Password" or "Say the Magic Word".
    /// </remarks>
    [Serializable]
    public class TaskStringNode : TaskTypedNode<TaskString_SO>
    {
        private const string PORT_TARGET_VALUE = "TargetValueInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "String";

        /// <summary>
        /// The target string value for task completion (Define mode only).
        /// </summary>
        public string TargetValue => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_TARGET_VALUE, "");

        /// <summary>
        /// Resolves the target value from the appropriate source.
        /// </summary>
        public string ResolveTargetValue()
        {
            if (IsAssetMode && TaskAsset is TaskString_SO stringTask)
                return stringTask.TargetValue;
            return TargetValue;
        }

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<string>(PORT_TARGET_VALUE)
                .WithDisplayName("Target Value")
                .Build();
        }

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            data.targetValue = ResolveTargetValue();
        }
    }
}
