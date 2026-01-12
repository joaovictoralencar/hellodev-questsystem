using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Events;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that grants rewards when reached in the quest flow.
    /// </summary>
    /// <remarks>
    /// Use this for:
    /// - Granting items upon quest completion
    /// - Awarding XP for reaching milestones
    /// - Giving currency rewards
    /// - Unlocking achievements or abilities
    ///
    /// The rewards are granted when the flow reaches this node, then
    /// continues to the next node via the "Then" port.
    ///
    /// Following the "Separate Data from Logic" principle:
    /// - RewardInstance stores WHAT rewards to grant (QuestRewardType_SO + amount)
    /// - IRewardable implementations decide HOW to grant them
    ///
    /// At runtime, iterate through Rewards and use IRewardable handlers:
    /// <code>
    /// foreach (var reward in rewardNode.Rewards)
    /// {
    ///     var handler = rewardHandlers.FirstOrDefault(h => h.CanHandle(reward.RewardType));
    ///     handler?.GiveReward(reward.RewardType, reward.Amount);
    /// }
    /// </code>
    /// </remarks>
    [Serializable]
    public class RewardNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_ON_REWARDS_GRANTED = "OnRewardsGranted";

        #endregion

        #region Serialized Data

        [SerializeField]
        [Tooltip("List of rewards to grant when this node is reached")]
        private List<RewardInstance> rewards = new();

        #endregion

        #region Properties

        /// <summary>
        /// Developer-friendly name for this node (for graph readability).
        /// </summary>
        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);

        /// <summary>
        /// Optional event fired when rewards are granted.
        /// </summary>
        public GameEventVoid_SO OnRewardsGranted => GetOptionValue<GameEventVoid_SO>(OPT_ON_REWARDS_GRANTED);

        /// <summary>
        /// The list of reward instances to grant.
        /// Each instance pairs a QuestRewardType_SO with an amount.
        /// </summary>
        public List<RewardInstance> Rewards => rewards;

        /// <summary>
        /// Display name shown on the node.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(NodeName))
                    return $"[Reward] {NodeName}";

                int validCount = 0;
                foreach (var r in rewards)
                {
                    if (r.IsValid) validCount++;
                }

                if (validCount == 0)
                    return "[Reward] Empty";

                if (validCount == 1)
                {
                    foreach (var r in rewards)
                    {
                        if (r.IsValid)
                            return $"[Reward] {r.DisplayText}";
                    }
                }

                return $"[Reward] {validCount} rewards";
            }
        }

        /// <summary>
        /// Returns true if this node has any valid rewards configured.
        /// </summary>
        public bool HasRewards
        {
            get
            {
                foreach (var r in rewards)
                {
                    if (r.IsValid) return true;
                }
                return false;
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_NODE_NAME)
                .WithDisplayName("Node Name")
                .WithDefaultValue("")
                .WithTooltip("Optional developer-friendly name for this node")
                .Delayed();

            context.AddOption<GameEventVoid_SO>(OPT_ON_REWARDS_GRANTED)
                .WithDisplayName("On Rewards Granted")
                .WithTooltip("Optional event fired when rewards are granted")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous node in the flow
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Continue flow after granting rewards
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
