using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that sets world flags when reached in the quest flow.
    /// </summary>
    /// <remarks>
    /// Use this for:
    /// - Recording player choices (e.g., "spared_merchant = true")
    /// - Updating reputation values (e.g., "bandit_reputation += 10")
    /// - Setting story state (e.g., "act_two_started = true")
    /// - Unlocking content based on progression
    ///
    /// The flags are modified when the flow reaches this node, then
    /// continues to the next node via the "Then" port.
    ///
    /// Note: This node uses WorldFlagModification which supports both
    /// boolean flags (set true/false) and integer flags (set/add/subtract).
    /// </remarks>
    [Serializable]
    public class WorldFlagSetNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_FLAG_LOCATOR = "FlagLocator";

        #endregion

        #region Serialized Data

        [SerializeField]
        private List<WorldFlagModification> modifications = new();

        #endregion

        #region Properties

        /// <summary>
        /// Developer-friendly name for this node (for graph readability).
        /// </summary>
        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);

        /// <summary>
        /// The locator used to access world flag runtime values.
        /// </summary>
        public WorldFlagLocator_SO FlagLocator => GetOptionValue<WorldFlagLocator_SO>(OPT_FLAG_LOCATOR);

        /// <summary>
        /// The list of flag modifications to apply.
        /// </summary>
        public List<WorldFlagModification> Modifications => modifications;

        /// <summary>
        /// Display name shown on the node.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(NodeName))
                    return $"[Flags] {NodeName}";

                if (modifications.Count == 0)
                    return "[Flags] Empty";

                if (modifications.Count == 1 && modifications[0].IsValid)
                    return $"[Flags] {modifications[0].Description}";

                return $"[Flags] {modifications.Count} changes";
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

            context.AddOption<WorldFlagLocator_SO>(OPT_FLAG_LOCATOR)
                .WithDisplayName("Flag Locator")
                .WithTooltip("The WorldFlagLocator that provides access to flag runtime values");
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

            // Continue flow after setting flags
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
