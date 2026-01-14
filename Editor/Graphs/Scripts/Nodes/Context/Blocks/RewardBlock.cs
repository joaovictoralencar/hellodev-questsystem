using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Block node representing a single reward inside a RewardContextNode.
    /// Pairs a QuestRewardType_SO with an amount.
    /// </summary>
    /// <remarks>
    /// Add multiple RewardBlocks to a RewardContextNode to define quest rewards.
    /// Both RewardType and Amount are input ports that can receive connections
    /// from Variables/Constants or have embedded values.
    /// </remarks>
    [UseWithContext(typeof(RewardContextNode))]
    [Serializable]
    public class RewardBlock : BlockNode
    {
        #region Option Names

        private const string OPT_DESCRIPTION = "Description";

        #endregion

        #region Port Names

        private const string PORT_REWARD_TYPE = "RewardTypeInput";
        private const string PORT_AMOUNT = "AmountInput";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the value of a node option by name.
        /// </summary>
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
        /// The type of reward to grant.
        /// </summary>
        public QuestRewardType_SO RewardType => GraphTraversalUtility.ResolveDataPort<QuestRewardType_SO>(this, PORT_REWARD_TYPE, null);

        /// <summary>
        /// The amount of this reward to grant.
        /// </summary>
        public int Amount => GraphTraversalUtility.ResolveDataPort<int>(this, PORT_AMOUNT, 1);

        /// <summary>
        /// Optional description override for this reward instance.
        /// </summary>
        public string Description => GetOptionValue<string>(OPT_DESCRIPTION);

        /// <summary>
        /// Whether this reward block has valid configuration.
        /// </summary>
        public bool IsValid => RewardType != null && Amount > 0;

        /// <summary>
        /// Creates a RewardInstance from this block's configuration.
        /// </summary>
        public RewardInstance ToRewardInstance() => new RewardInstance
        {
            RewardType = RewardType,
            Amount = Amount
        };

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_DESCRIPTION)
                .WithDisplayName("Description")
                .WithDefaultValue("")
                .WithTooltip("Optional description override for UI display")
                .Delayed();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Reward type input port - can connect to Variable/Constant
            context.AddInputPort<QuestRewardType_SO>(PORT_REWARD_TYPE)
                .WithDisplayName("Reward Type")
                .Build();

            // Amount input port - can connect to Variable/Constant
            context.AddInputPort<int>(PORT_AMOUNT)
                .WithDisplayName("Amount")
                .Build();
        }

        #endregion
    }
}
