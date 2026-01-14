using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Events;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Context node that contains reward blocks as a visual container.
    /// Provides rewards for QuestNode via the context pattern.
    /// </summary>
    /// <remarks>
    /// Flow design:
    /// - RewardContextNode.Then (output) → QuestNode.Rewards (input)
    /// - No input port - this node is a source of rewards, not part of a flow chain
    ///
    /// Benefits of the context pattern:
    /// - Rewards can be reordered via drag-and-drop inside the context
    /// - Visual grouping of related rewards
    /// - Can add multiple reward types (XP, Gold, Items) as separate blocks
    ///
    /// Use for quest completion rewards (granted when the quest completes successfully).
    /// </remarks>
    [Serializable]
    public class RewardContextNode : ContextNode
    {
        #region Option Names

        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_ON_REWARDS_GRANTED = "OnRewardsGranted";
        private const string OPT_GRANT_MODE = "GrantMode";

        #endregion

        #region Enums

        /// <summary>
        /// How rewards are granted.
        /// </summary>
        public enum RewardGrantMode
        {
            /// <summary>
            /// Grant all rewards at once.
            /// </summary>
            All,

            /// <summary>
            /// Player chooses one reward from the list.
            /// </summary>
            ChooseOne,

            /// <summary>
            /// Grant a random reward from the list.
            /// </summary>
            Random
        }

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
        /// Developer-friendly name for this reward node.
        /// </summary>
        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);

        /// <summary>
        /// Optional event fired when rewards are granted.
        /// </summary>
        public GameEventVoid_SO OnRewardsGranted => GetOptionValue<GameEventVoid_SO>(OPT_ON_REWARDS_GRANTED);

        /// <summary>
        /// How rewards in this context are granted.
        /// </summary>
        public RewardGrantMode GrantMode => GetOptionValue<RewardGrantMode>(OPT_GRANT_MODE);

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_NODE_NAME)
                .WithDisplayName("Node Name")
                .WithDefaultValue("Rewards")
                .WithTooltip("Developer-friendly name for this reward node")
                .Delayed();

            context.AddOption<RewardGrantMode>(OPT_GRANT_MODE)
                .WithDisplayName("Grant Mode")
                .WithDefaultValue(RewardGrantMode.All)
                .WithTooltip("How rewards are granted:\n" +
                    "• All: Grant all rewards\n" +
                    "• ChooseOne: Player picks one reward\n" +
                    "• Random: Random reward is granted");

            context.AddOption<GameEventVoid_SO>(OPT_ON_REWARDS_GRANTED)
                .WithDisplayName("On Rewards Granted")
                .WithTooltip("Optional event fired when rewards are granted");
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Output: Connects to QuestNode's Rewards input
            context.AddOutputPort<RewardFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
