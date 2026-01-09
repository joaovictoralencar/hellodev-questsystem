using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
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
    /// - Define Mode: Shows inline options for stage data + task group ports
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
        private const string OPT_STAGE_INDEX = "StageIndex";
        private const string OPT_STAGE_NAME = "StageName";
        private const string OPT_JOURNAL_ENTRY = "JournalEntry";
        private const string OPT_STAGE_ICON = "StageIcon";
        private const string OPT_IS_TERMINAL = "IsTerminal";
        private const string OPT_IS_OPTIONAL = "IsOptional";
        private const string OPT_IS_HIDDEN = "IsHidden";
        private const string OPT_HAS_PLAYER_CHOICES = "HasPlayerChoices";
        private const string OPT_TASK_GROUP_COUNT = "TaskGroupCount";

        #endregion

        #region Port Names

        // Asset Mode
        private const string PORT_STAGE_SUBGRAPH = "StageSubgraphInput";

        // Define Mode - Dynamic ports
        private const string PORT_TASK_GROUP = "TaskGroupInput";

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use an existing Stage Subgraph (true) or define inline (false).
        /// </summary>
        public bool UseStageSubgraph => GetOptionValue<bool>(OPT_USE_STAGE_SUBGRAPH);

        /// <summary>
        /// Unique index for this stage.
        /// </summary>
        public int StageIndex => GetOptionValue<int>(OPT_STAGE_INDEX);

        /// <summary>
        /// Developer-friendly name for this stage.
        /// </summary>
        public string StageName => GetOptionValue<string>(OPT_STAGE_NAME);

        /// <summary>
        /// Localized journal entry text.
        /// </summary>
        public LocalizedString JournalEntry => GetOptionValue<LocalizedString>(OPT_JOURNAL_ENTRY);

        /// <summary>
        /// Optional icon for this stage.
        /// </summary>
        public Sprite StageIcon => GetOptionValue<Sprite>(OPT_STAGE_ICON);

        /// <summary>
        /// If true, completing this stage ends the quest.
        /// </summary>
        public bool IsTerminal => GetOptionValue<bool>(OPT_IS_TERMINAL);

        /// <summary>
        /// If true, this stage can be skipped.
        /// </summary>
        public bool IsOptional => GetOptionValue<bool>(OPT_IS_OPTIONAL);

        /// <summary>
        /// If true, this stage is not shown in the UI.
        /// </summary>
        public bool IsHidden => GetOptionValue<bool>(OPT_IS_HIDDEN);

        /// <summary>
        /// If true, adds a Choices output port for player branching.
        /// </summary>
        public bool HasPlayerChoices => GetOptionValue<bool>(OPT_HAS_PLAYER_CHOICES);

        /// <summary>
        /// Number of task group ports to show (Define mode).
        /// </summary>
        public int TaskGroupCount => GetOptionValue<int>(OPT_TASK_GROUP_COUNT);

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

            // Stage index is always needed for ordering
            context.AddOption<int>(OPT_STAGE_INDEX)
                .WithDisplayName("Stage Index")
                .WithDefaultValue(0)
                .WithTooltip("Unique index for this stage (use gaps of 10: 0, 10, 20...)")
                .Delayed();

            // Only show these options in Define mode
            if (!UseStageSubgraph)
            {
                context.AddOption<string>(OPT_STAGE_NAME)
                    .WithDisplayName("Stage Name")
                    .WithDefaultValue("New Stage")
                    .WithTooltip("Developer-friendly name for this stage")
                    .Delayed();

                // Display options (Inspector only)
                context.AddOption<LocalizedString>(OPT_JOURNAL_ENTRY)
                    .WithDisplayName("Journal Entry")
                    .WithTooltip("Localized text shown in the quest journal")
                    .ShowInInspectorOnly();

                context.AddOption<Sprite>(OPT_STAGE_ICON)
                    .WithDisplayName("Stage Icon")
                    .WithTooltip("Optional icon for this stage")
                    .ShowInInspectorOnly();

                // Flag options
                context.AddOption<bool>(OPT_IS_TERMINAL)
                    .WithDisplayName("Is Terminal")
                    .WithDefaultValue(false)
                    .WithTooltip("If true, completing this stage ends the quest");

                context.AddOption<bool>(OPT_IS_OPTIONAL)
                    .WithDisplayName("Is Optional")
                    .WithDefaultValue(false)
                    .WithTooltip("If true, this stage can be skipped")
                    .ShowInInspectorOnly();

                context.AddOption<bool>(OPT_IS_HIDDEN)
                    .WithDisplayName("Is Hidden")
                    .WithDefaultValue(false)
                    .WithTooltip("If true, this stage is not shown in the UI")
                    .ShowInInspectorOnly();

                context.AddOption<bool>(OPT_HAS_PLAYER_CHOICES)
                    .WithDisplayName("Has Player Choices")
                    .WithDefaultValue(false)
                    .WithTooltip("If true, adds a Choices output port for branching");

                context.AddOption<int>(OPT_TASK_GROUP_COUNT)
                    .WithDisplayName("Task Group Count")
                    .WithDefaultValue(1)
                    .WithTooltip("Number of task group ports to show");
            }
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Flow input: From previous stage or start node
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

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
                // Define Mode: Show task group ports and flow outputs

                // Dynamic task group ports - connect to TaskGroupContextNodes or TaskGroupGraph subgraphs
                for (int i = 0; i < TaskGroupCount; i++)
                {
                    context.AddInputPort<TaskFlow>(PORT_TASK_GROUP + i)
                        .WithDisplayName($"Task Group {i + 1}")
                        .WithConnectorUI(PortConnectorUI.Arrowhead)
                        .Build();
                }

                AddFlowOutputPorts(context);
            }
        }

        /// <summary>
        /// Adds the standard flow output ports based on stage settings.
        /// </summary>
        private void AddFlowOutputPorts(IPortDefinitionContext context)
        {
            // Terminal stages have no flow output ports
            if (IsTerminal)
                return;

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

        #region Helpers

        /// <summary>
        /// Gets a task group subgraph from the dynamic port by index.
        /// </summary>
        public TaskGroupGraph GetTaskGroupGraph(int index)
        {
            if (index < 0 || index >= TaskGroupCount)
                return null;
            return GraphTraversalUtility.ResolveDataPort<TaskGroupGraph>(this, PORT_TASK_GROUP + index, null);
        }

        #endregion
    }
}
