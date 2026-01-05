using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that references a StageGraph subgraph.
    /// Used in QuestGraph to embed reusable stage definitions.
    /// </summary>
    /// <remarks>
    /// Subgraph nodes provide flow-through connections while encapsulating
    /// the stage's internal logic (task groups, transitions) in a separate asset.
    ///
    /// Use this when:
    /// - A stage pattern is reused across multiple quests
    /// - You want to keep the main quest graph clean
    /// - Multiple designers work on different stages
    /// </remarks>
    [Serializable]
    public class StageSubgraphNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_STAGE_SUBGRAPH = "StageSubgraph";
        private const string OPT_OVERRIDE_STAGE_INDEX = "OverrideStageIndex";
        private const string OPT_OVERRIDE_STAGE_NAME = "OverrideStageName";

        #endregion

        #region Properties

        public StageGraph StageSubgraph => GetOptionValue<StageGraph>(OPT_STAGE_SUBGRAPH);

        public int EffectiveStageIndex
        {
            get
            {
                var overrideIndex = GetOptionValue<int>(OPT_OVERRIDE_STAGE_INDEX);
                if (overrideIndex >= 0)
                    return overrideIndex;
                return StageSubgraph?.StageIndex ?? 0;
            }
        }

        public string EffectiveStageName
        {
            get
            {
                var overrideName = GetOptionValue<string>(OPT_OVERRIDE_STAGE_NAME);
                if (!string.IsNullOrEmpty(overrideName))
                    return overrideName;
                return StageSubgraph?.StageName ?? "Empty Stage";
            }
        }

        public string DisplayName => StageSubgraph != null
            ? $"[Stage] {EffectiveStageName}"
            : "[Stage] Empty Reference";

        public bool IsTerminal => StageSubgraph?.IsTerminal ?? false;
        public bool IsOptional => StageSubgraph?.IsOptional ?? false;
        public bool IsHidden => StageSubgraph?.IsHidden ?? false;

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<StageGraph>(OPT_STAGE_SUBGRAPH)
                .WithDisplayName("Stage Subgraph")
                .WithTooltip("Reference to a reusable StageGraph asset");

            context.AddOption<int>(OPT_OVERRIDE_STAGE_INDEX)
                .WithDisplayName("Override Stage Index")
                .WithDefaultValue(-1)
                .WithTooltip("Override the subgraph's stage index (-1 = use subgraph value)")
                .ShowInInspectorOnly();

            context.AddOption<string>(OPT_OVERRIDE_STAGE_NAME)
                .WithDisplayName("Override Stage Name")
                .WithTooltip("Override the subgraph's stage name (empty = use subgraph value)")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous stage or quest start
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // TaskGroups output - connect to inline TaskGroupNodes
            context.AddOutputPort<StageFlow>("TaskGroups")
                .WithDisplayName("Task Groups")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Only add flow output ports if not terminal
            if (!IsTerminal)
            {
                // Success flow - where to go when stage completes
                context.AddOutputPort<StageFlow>("Then")
                    .WithDisplayName("Then")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                // Failure flow - where to go if stage fails
                context.AddOutputPort<StageFlow>("Else")
                    .WithDisplayName("Else")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion
    }
}
