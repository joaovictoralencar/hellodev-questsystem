using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Block node representing a single reward inside a RewardContextNode.
    /// Pairs a QuestRewardType_SO with an amount.
    /// </summary>
    /// <remarks>
    /// Add multiple RewardBlocks to a RewardContextNode to define quest rewards.
    /// The amount can be set directly or connected from a Variable/Constant.
    /// </remarks>
    [UseWithContext(typeof(RewardContextNode))]
    [Serializable]
    public class RewardBlock : BlockNode
    {
        #region Option Names

        private const string OPT_REWARD_TYPE = "RewardType";
        private const string OPT_AMOUNT = "Amount";
        private const string OPT_DESCRIPTION = "Description";

        #endregion

        #region Port Names

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
        public QuestRewardType_SO RewardType => GetOptionValue<QuestRewardType_SO>(OPT_REWARD_TYPE);

        /// <summary>
        /// The amount of this reward to grant.
        /// </summary>
        public int Amount => GetOptionValue<int>(OPT_AMOUNT);

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
            context.AddOption<QuestRewardType_SO>(OPT_REWARD_TYPE)
                .WithDisplayName("Reward Type")
                .WithTooltip("The type of reward to grant (XP, Gold, Items, etc.)");

            context.AddOption<int>(OPT_AMOUNT)
                .WithDisplayName("Amount")
                .WithDefaultValue(1)
                .WithTooltip("The amount of this reward to grant");

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
            // Data input port for dynamic amount from Variables/Constants
            context.AddInputPort<int>(PORT_AMOUNT)
                .WithDisplayName("Amount")
                .Build();
        }

        #endregion
    }
}
