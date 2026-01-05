using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that references a Quest in a QuestLine graph.
    /// Used in QuestLineGraph to embed reusable quest definitions.
    /// </summary>
    /// <remarks>
    /// This node supports two modes:
    /// 1. GraphAsset: Reference a QuestGraph subgraph (recommended for graph-based workflow)
    /// 2. ExistingAsset: Reference an existing Quest_SO (for legacy assets or manual creation)
    ///
    /// Use this when:
    /// - Building quest chains in a QuestLine
    /// - Reusing a quest across multiple questlines
    /// - Maintaining modular quest definitions
    /// </remarks>
    [Serializable]
    public class QuestRefNode : QuestBaseNode
    {
        /// <summary>
        /// Determines whether the node references an existing Quest_SO asset
        /// or a QuestGraph that will be converted to a Quest_SO.
        /// </summary>
        public enum ReferenceType
        {
            /// <summary>Reference a QuestGraph (subgraph) - recommended approach.</summary>
            GraphAsset,
            /// <summary>Reference an existing Quest_SO asset directly.</summary>
            ExistingAsset
        }

        #region Option Names

        private const string OPT_REFERENCE_TYPE = "ReferenceType";
        private const string OPT_QUEST_ASSET = "QuestAsset";
        private const string OPT_QUEST_GRAPH = "QuestGraph";
        private const string OPT_IS_OPTIONAL = "IsOptional";
        private const string OPT_QUEST_ORDER = "QuestOrderOverride";

        #endregion

        #region Properties

        public ReferenceType RefType => GetOptionValue<ReferenceType>(OPT_REFERENCE_TYPE);
        public Quest_SO QuestAsset => GetOptionValue<Quest_SO>(OPT_QUEST_ASSET);
        public QuestGraph QuestGraphAsset => GetOptionValue<QuestGraph>(OPT_QUEST_GRAPH);
        public bool IsOptional => GetOptionValue<bool>(OPT_IS_OPTIONAL);

        public int EffectiveQuestOrder
        {
            get
            {
                var order = GetOptionValue<int>(OPT_QUEST_ORDER);
                if (order >= 0)
                    return order;
                // Default to graph position order (determined during conversion)
                return -1;
            }
        }

        public string DisplayName
        {
            get
            {
                string prefix = IsOptional ? "[Optional] " : "";
                if (RefType == ReferenceType.ExistingAsset && QuestAsset != null)
                    return prefix + QuestAsset.DevName;
                if (RefType == ReferenceType.GraphAsset && QuestGraphAsset != null)
                    return prefix + QuestGraphAsset.DevName;
                return "[Quest] Empty Reference";
            }
        }

        public string QuestId
        {
            get
            {
                if (RefType == ReferenceType.ExistingAsset && QuestAsset != null)
                    return QuestAsset.QuestId.ToString();
                if (RefType == ReferenceType.GraphAsset && QuestGraphAsset != null)
                    return QuestGraphAsset.QuestId;
                return string.Empty;
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<ReferenceType>(OPT_REFERENCE_TYPE)
                .WithDisplayName("Reference Type")
                .WithDefaultValue(ReferenceType.GraphAsset)
                .WithTooltip("GraphAsset (recommended) or ExistingAsset for legacy Quest_SO");

            context.AddOption<QuestGraph>(OPT_QUEST_GRAPH)
                .WithDisplayName("Quest Graph")
                .WithTooltip("Reference to a QuestGraph subgraph (when using GraphAsset mode)");

            context.AddOption<Quest_SO>(OPT_QUEST_ASSET)
                .WithDisplayName("Quest Asset")
                .WithTooltip("Reference to an existing Quest_SO (when using ExistingAsset mode)")
                .ShowInInspectorOnly();

            context.AddOption<bool>(OPT_IS_OPTIONAL)
                .WithDisplayName("Is Optional")
                .WithDefaultValue(false)
                .WithTooltip("If true, this quest can be skipped in the questline");

            context.AddOption<int>(OPT_QUEST_ORDER)
                .WithDisplayName("Order Override")
                .WithDefaultValue(-1)
                .WithTooltip("Override quest order (-1 = use graph position)")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous quest in chain
            context.AddInputPort<QuestFlow>("In")
                .WithDisplayName("After")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Output: To next quest in chain
            context.AddOutputPort<QuestFlow>("Out")
                .WithDisplayName("Before")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Success flow - where to go when quest completes
            context.AddOutputPort<QuestFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Failure flow - where to go if quest fails
            context.AddOutputPort<QuestFlow>("Else")
                .WithDisplayName("Else")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
