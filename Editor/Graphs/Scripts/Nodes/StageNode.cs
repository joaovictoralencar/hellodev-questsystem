using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
using HelloDev.QuestSystem.QuestGraph.Editor.Utilities;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Node for defining or referencing a Quest Stage in a QuestGraph.
    /// Supports two modes: Asset Mode (reference StageGraph subgraph) or Define Mode (create inline).
    /// </summary>
    /// <remarks>
    /// Follows the TaskTypedNode pattern:
    /// - Boolean toggle "Use Stage Subgraph" controls which ports are shown
    /// - Asset Mode: Shows Stage Subgraph input port only
    /// - Define Mode: Shows inline options for stage data
    ///
    /// Port design:
    /// - All stage variables (name, index, journal entry, etc.) are INPUT ports
    /// - TaskGroups (StageFlow INPUT): Receives from TaskGroupContextNode.Then
    /// - Outputs are flow ports only:
    ///   - Then (StageFlow): Connects to next stage (if not terminal)
    ///   - Choices (ChoiceFlow): For player branching (if HasPlayerChoices)
    ///
    /// Stages are discrete phases of quest progression. They can be:
    /// - Terminal (quest ends when completed)
    /// - Optional (can be skipped)
    /// - Hidden (not shown in UI)
    /// - Have player choices (branching paths)
    /// </remarks>
    [Serializable]
    public class StageNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_USE_STAGE_SUBGRAPH = "UseStageSubgraph";
        private const string OPT_HAS_PLAYER_CHOICES = "HasPlayerChoices";

        #endregion

        #region Port Names

        // Asset Mode
        private const string PORT_STAGE_SUBGRAPH = "StageSubgraphInput";

        // Define Mode - Identity ports (visible on node)
        private const string PORT_STAGE_NAME = "StageNameInput";
        private const string PORT_JOURNAL_ENTRY = "JournalEntryInput";
        private const string PORT_STAGE_ICON = "StageIconInput";
        private const string PORT_IS_TERMINAL = "IsTerminalInput";
        private const string PORT_IS_OPTIONAL = "IsOptionalInput";
        private const string PORT_IS_HIDDEN = "IsHiddenInput";
        private const string PORT_STAGE_INDEX = "StageIndexInput";

        // Define Mode - Flow input port for task groups
        private const string PORT_TASK_GROUPS = "TaskGroupsInput";

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use an existing Stage Subgraph (true) or define inline (false).
        /// </summary>
        public bool UseStageSubgraph => GetOptionValue<bool>(OPT_USE_STAGE_SUBGRAPH);

        /// <summary>
        /// Unique index for this stage.
        /// </summary>
        public int StageIndex =>  GraphTraversalUtility.ResolveDataPort<int>(this, PORT_STAGE_INDEX);

        /// <summary>
        /// Developer-friendly name for this stage (Define mode).
        /// </summary>
        public string StageName => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_STAGE_NAME, "New Stage");

        /// <summary>
        /// Localized journal entry text (Define mode).
        /// </summary>
        public LocalizedString JournalEntry => GraphTraversalUtility.ResolveDataPort<LocalizedString>(this, PORT_JOURNAL_ENTRY, default);

        /// <summary>
        /// Optional icon for this stage (Define mode).
        /// </summary>
        public Sprite StageIcon => GraphTraversalUtility.ResolveDataPort<Sprite>(this, PORT_STAGE_ICON, null);

        /// <summary>
        /// If true, completing this stage ends the quest (Define mode).
        /// </summary>
        public bool IsTerminal => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_IS_TERMINAL, false);

        /// <summary>
        /// If true, this stage can be skipped (Define mode).
        /// </summary>
        public bool IsOptional => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_IS_OPTIONAL, false);

        /// <summary>
        /// If true, this stage is not shown in the UI (Define mode).
        /// </summary>
        public bool IsHidden => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_IS_HIDDEN, false);

        /// <summary>
        /// If true, adds a Choices output port for player branching.
        /// </summary>
        public bool HasPlayerChoices => GetOptionValue<bool>(OPT_HAS_PLAYER_CHOICES);

        /// <summary>
        /// The referenced StageGraph subgraph (Asset mode only).
        /// </summary>
        public StageGraph StageSubgraph
        {
            get
            {
                if (!UseStageSubgraph)
                    return null;
                return GraphTraversalUtility.ResolveDataPort<StageGraph>(this, PORT_STAGE_SUBGRAPH, null);
            }
        }

        /// <summary>
        /// Whether this node has a valid stage configuration.
        /// </summary>
        public bool HasValidStage
        {
            get
            {
                if (UseStageSubgraph)
                    return StageSubgraph != null;

                // Define mode: at least need a stage name
                return !string.IsNullOrWhiteSpace(StageName);
            }
        }

        /// <summary>
        /// Display name for this node.
        /// </summary>
        public string DisplayName
        {
            get
            {
                string prefix = $"[{StageIndex}] ";
                if (UseStageSubgraph && StageSubgraph != null)
                    return prefix + StageSubgraph.StageName;
                if (!UseStageSubgraph && !string.IsNullOrEmpty(StageName))
                    return prefix + StageName;
                return "[Stage] " + (UseStageSubgraph ? "No Subgraph" : "Unnamed");
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Mode toggle - switches between Asset/Define mode
            context.AddOption<bool>(OPT_USE_STAGE_SUBGRAPH)
                .WithDisplayName("Use Stage Subgraph")
                .WithDefaultValue(false)
                .WithTooltip("Check to use an existing StageGraph subgraph.\nUncheck to define stage inline.");

            // Only show these options in Define mode
            if (!UseStageSubgraph)
            {
                // HasPlayerChoices controls output ports, must stay as option
                context.AddOption<bool>(OPT_HAS_PLAYER_CHOICES)
                    .WithDisplayName("Has Player Choices")
                    .WithDefaultValue(false)
                    .WithTooltip("If true, adds a Choices output port for branching");
            }
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Flow input: From previous stage, start node, or TransitionNodes
            // Use SetMultiCapacity to allow multiple transitions to target the same stage
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build()
                .SetMultiCapacity();

            if (UseStageSubgraph)
            {
                // Asset Mode: Show Stage Subgraph input port only
                context.AddInputPort<StageGraph>(PORT_STAGE_SUBGRAPH)
                    .WithDisplayName("Stage Subgraph")
                    .Build();

                // Still need flow outputs for graph connections
                AddFlowOutputPorts(context);
            }
            else
            {
                // Define Mode: Show identity ports, task group ports, and flow outputs

                // Identity ports - visible on node and in Node Properties
                context.AddInputPort<string>(PORT_STAGE_NAME)
                    .WithDisplayName("Stage Name")
                    .Build();
                
                context.AddInputPort<int>(PORT_STAGE_INDEX)
                    .WithDisplayName("Stage Index")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_JOURNAL_ENTRY)
                    .WithDisplayName("Journal Entry")
                    .Build();

                context.AddInputPort<Sprite>(PORT_STAGE_ICON)
                    .WithDisplayName("Stage Icon")
                    .Build();

                context.AddInputPort<bool>(PORT_IS_TERMINAL)
                    .WithDisplayName("Is Terminal")
                    .Build();

                context.AddInputPort<bool>(PORT_IS_OPTIONAL)
                    .WithDisplayName("Is Optional")
                    .Build();

                context.AddInputPort<bool>(PORT_IS_HIDDEN)
                    .WithDisplayName("Is Hidden")
                    .Build();

                // TaskGroups flow input - receives from TaskGroupContextNode.Then
                context.AddInputPort<StageFlow>(PORT_TASK_GROUPS)
                    .WithDisplayName("Task Groups")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                AddFlowOutputPorts(context);
            }
        }

        /// <summary>
        /// Adds the standard flow output ports based on stage settings.
        /// </summary>
        private void AddFlowOutputPorts(IPortDefinitionContext context)
        {
            // Terminal stages have no Then output port (quest ends here)
            if (IsTerminal)
                return;

            // Success flow - where to go when stage completes
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // If stage has player choices, add a choice output port
            if (HasPlayerChoices)
            {
                context.AddOutputPort<ChoiceFlow>("Choices")
                    .WithDisplayName("Player Choices")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion
    }
}
