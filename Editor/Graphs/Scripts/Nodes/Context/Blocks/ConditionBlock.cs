using System;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Block node representing a single condition inside a ConditionContextNode.
    /// </summary>
    /// <remarks>
    /// Add multiple ConditionBlocks to a ConditionContextNode to define condition groups.
    /// Each block represents one condition that contributes to the overall evaluation.
    ///
    /// Supports two modes:
    /// - Asset Mode: Reference an existing Condition_SO asset
    /// - Define Mode: Define condition inline (future feature)
    /// </remarks>
    [UseWithContext(typeof(ConditionContextNode))]
    [Serializable]
    public class ConditionBlock : BlockNode
    {
        #region Option Names

        private const string OPT_USE_CONDITION_ASSET = "UseConditionAsset";
        private const string OPT_CONDITION_ASSET = "ConditionAsset";
        private const string OPT_BLOCK_NAME = "BlockName";
        private const string OPT_INVERT = "Invert";

        #endregion

        #region Port Names

        private const string PORT_CONDITION_ASSET = "ConditionAssetInput";

        #endregion

        #region Helper Methods

        protected T GetOptionValue<T>(string optionName)
        {
            var option = GetNodeOptionByName(optionName);
            if (option != null && option.TryGetValue<T>(out var value))
                return value;
            return default;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use an existing Condition Asset (true) or define inline (false).
        /// </summary>
        public bool UseConditionAsset => GetOptionValue<bool>(OPT_USE_CONDITION_ASSET);

        /// <summary>
        /// The condition asset to evaluate.
        /// </summary>
        public Condition_SO ConditionAsset => GetOptionValue<Condition_SO>(OPT_CONDITION_ASSET);

        /// <summary>
        /// Optional name for this block (for visual clarity).
        /// </summary>
        public string BlockName => GetOptionValue<string>(OPT_BLOCK_NAME);

        /// <summary>
        /// If true, inverts this specific condition's result.
        /// </summary>
        public bool Invert => GetOptionValue<bool>(OPT_INVERT);

        /// <summary>
        /// Display name for the block in the graph.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(BlockName))
                    return BlockName;
                if (ConditionAsset != null)
                    return ConditionAsset.name;
                return "Empty Condition";
            }
        }

        /// <summary>
        /// Whether this block has a valid condition.
        /// </summary>
        public bool IsValid => ConditionAsset != null;

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_BLOCK_NAME)
                .WithDisplayName("Name")
                .WithDefaultValue("")
                .WithTooltip("Optional display name for this condition")
                .Delayed();

            context.AddOption<bool>(OPT_USE_CONDITION_ASSET)
                .WithDisplayName("Use Asset")
                .WithDefaultValue(true)
                .WithTooltip("Use an existing Condition_SO asset");

            // Show condition picker when UseConditionAsset is true
            if (UseConditionAsset)
            {
                context.AddOption<Condition_SO>(OPT_CONDITION_ASSET)
                    .WithDisplayName("Condition")
                    .WithTooltip("The condition asset to evaluate");
            }

            context.AddOption<bool>(OPT_INVERT)
                .WithDisplayName("Invert")
                .WithDefaultValue(false)
                .WithTooltip("Invert this condition's result (NOT)");
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Show condition input port when UseConditionAsset is true
            // Allows connecting Variable/Constant with Condition_SO
            if (UseConditionAsset)
            {
                context.AddInputPort<Condition_SO>(PORT_CONDITION_ASSET)
                    .WithDisplayName("Condition")
                    .Build();
            }
        }

        #endregion
    }
}
