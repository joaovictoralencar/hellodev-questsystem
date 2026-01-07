using System;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a Quest Stage - a discrete phase of quest progression.
    /// Stages can be terminal (quest ends), optional, or hidden.
    /// Connect ChoiceNodes to the Choice output for player branching.
    /// </summary>
    [Serializable]
    public class StageNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_STAGE_INDEX = "StageIndex";
        private const string OPT_STAGE_NAME = "StageName";
        private const string OPT_JOURNAL_ENTRY = "JournalEntry";
        private const string OPT_STAGE_ICON = "StageIcon";
        private const string OPT_IS_TERMINAL = "IsTerminal";
        private const string OPT_IS_OPTIONAL = "IsOptional";
        private const string OPT_IS_HIDDEN = "IsHidden";
        private const string OPT_HAS_PLAYER_CHOICES = "HasPlayerChoices";

        #endregion

        #region Properties

        public int StageIndex => GetOptionValue<int>(OPT_STAGE_INDEX);
        public string StageName => GetOptionValue<string>(OPT_STAGE_NAME);
        public LocalizedString JournalEntry => GetOptionValue<LocalizedString>(OPT_JOURNAL_ENTRY);
        public Sprite StageIcon => GetOptionValue<Sprite>(OPT_STAGE_ICON);
        public bool IsTerminal => GetOptionValue<bool>(OPT_IS_TERMINAL);
        public bool IsOptional => GetOptionValue<bool>(OPT_IS_OPTIONAL);
        public bool IsHidden => GetOptionValue<bool>(OPT_IS_HIDDEN);
        public bool HasPlayerChoices => GetOptionValue<bool>(OPT_HAS_PLAYER_CHOICES);

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Identity options
            context.AddOption<int>(OPT_STAGE_INDEX)
                .WithDisplayName("Stage Index")
                .WithDefaultValue(0)
                .WithTooltip("Unique index for this stage (use gaps of 10: 0, 10, 20...)")
                .Delayed();

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
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous stage or start node
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // TaskGroups output - connect to TaskGroupContextNodes
            context.AddOutputPort<StageFlow>("TaskGroups")
                .WithDisplayName("Task Groups")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

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
            // Designer connects ChoiceNodes to this port
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
