using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
using HelloDev.QuestSystem.Stages;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a player choice that branches the quest.
    /// Connects to a Stage's choice output and leads to target stages.
    /// </summary>
    /// <remarks>
    /// Supports multiple output targets with conditional routing.
    /// Set "Output Count" > 1 for conditional branching after choice selection.
    /// First matching condition determines the output path.
    /// </remarks>
    [Serializable]
    public class ChoiceNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_CHOICE_ID = "ChoiceId";
        private const string OPT_PRIORITY = "Priority";
        private const string OPT_CHOICE_TEXT = "ChoiceText";
        private const string OPT_CHOICE_TOOLTIP = "ChoiceTooltip";
        private const string OPT_CHOICE_ICON = "ChoiceIcon";
        private const string OPT_CONDITIONS = "Conditions";
        private const string OPT_EFFECTS = "EffectsOnSelect";
        private const string OPT_OUTPUT_COUNT = "OutputCount";
        private const string OPT_OUTPUT_CONDITIONS = "OutputConditions";

        #endregion

        #region Constants

        private const int MIN_OUTPUTS = 1;
        private const int MAX_OUTPUTS = 4;

        #endregion

        #region Properties

        public string ChoiceId => GetOptionValue<string>(OPT_CHOICE_ID);
        public int Priority => GetOptionValue<int>(OPT_PRIORITY);
        public LocalizedString ChoiceText => GetOptionValue<LocalizedString>(OPT_CHOICE_TEXT);
        public LocalizedString ChoiceTooltip => GetOptionValue<LocalizedString>(OPT_CHOICE_TOOLTIP);
        public Sprite ChoiceIcon => GetOptionValue<Sprite>(OPT_CHOICE_ICON);
        public List<Condition_SO> Conditions => GetOptionValue<List<Condition_SO>>(OPT_CONDITIONS) ?? new List<Condition_SO>();
        public List<TransitionEffect_SO> EffectsOnSelect => GetOptionValue<List<TransitionEffect_SO>>(OPT_EFFECTS) ?? new List<TransitionEffect_SO>();

        /// <summary>
        /// Number of output paths for conditional routing.
        /// </summary>
        public int OutputCount
        {
            get
            {
                var count = GetOptionValue<int>(OPT_OUTPUT_COUNT);
                return Math.Clamp(count, MIN_OUTPUTS, MAX_OUTPUTS);
            }
        }

        /// <summary>
        /// Conditions for each output path (when OutputCount > 1).
        /// </summary>
        public List<Condition_SO> OutputConditions => GetOptionValue<List<Condition_SO>>(OPT_OUTPUT_CONDITIONS) ?? new List<Condition_SO>();

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Identity options
            context.AddOption<string>(OPT_CHOICE_ID)
                .WithDisplayName("Choice ID")
                .WithDefaultValue(Guid.NewGuid().ToString().Substring(0, 8))
                .WithTooltip("Unique identifier for this choice")
                .Delayed();

            context.AddOption<int>(OPT_PRIORITY)
                .WithDisplayName("Priority")
                .WithDefaultValue(0)
                .WithTooltip("Display order priority (higher = shown first)");

            // Display options
            context.AddOption<LocalizedString>(OPT_CHOICE_TEXT)
                .WithDisplayName("Choice Text")
                .WithTooltip("Localized text shown to the player");

            context.AddOption<LocalizedString>(OPT_CHOICE_TOOLTIP)
                .WithDisplayName("Tooltip")
                .WithTooltip("Localized tooltip shown on hover")
                .ShowInInspectorOnly();

            context.AddOption<Sprite>(OPT_CHOICE_ICON)
                .WithDisplayName("Icon")
                .WithTooltip("Optional icon for this choice")
                .ShowInInspectorOnly();

            // Conditions & Consequences
            context.AddOption<List<Condition_SO>>(OPT_CONDITIONS)
                .WithDisplayName("Availability Conditions")
                .WithTooltip("Conditions required for this choice to be available")
                .ShowInInspectorOnly();

            context.AddOption<List<TransitionEffect_SO>>(OPT_EFFECTS)
                .WithDisplayName("Effects On Select")
                .WithTooltip("Effects to apply when this transition is executed (e.g., world flags)")
                .ShowInInspectorOnly();

            // Dynamic output configuration
            context.AddOption<int>(OPT_OUTPUT_COUNT)
                .WithDisplayName("Output Count")
                .WithDefaultValue(MIN_OUTPUTS)
                .WithTooltip($"Number of output paths ({MIN_OUTPUTS}-{MAX_OUTPUTS}). Use >1 for conditional routing after selection.")
                .Delayed();

            context.AddOption<List<Condition_SO>>(OPT_OUTPUT_CONDITIONS)
                .WithDisplayName("Output Conditions")
                .WithTooltip("Conditions for each output path (evaluated in order)")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage's choice port
            context.AddInputPort<ChoiceFlow>("In")
                .WithDisplayName("From Choice")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Dynamic output ports based on output count
            // Note: Must handle null option during node scanning (before options are committed)
            var outputCount = MIN_OUTPUTS;
            var outputCountOption = GetNodeOptionByName(OPT_OUTPUT_COUNT);
            if (outputCountOption != null && outputCountOption.TryGetValue<int>(out var count))
            {
                outputCount = Math.Clamp(count, MIN_OUTPUTS, MAX_OUTPUTS);
            }

            if (outputCount == 1)
            {
                // Single output - simple case
                context.AddOutputPort<StageFlow>("Target")
                    .WithDisplayName("To Stage")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
            else
            {
                // Multiple outputs with conditions
                for (int i = 0; i < outputCount; i++)
                {
                    context.AddOutputPort<StageFlow>($"Target{i}")
                        .WithDisplayName($"Path {i + 1}")
                        .WithConnectorUI(PortConnectorUI.Arrowhead)
                        .Build();
                }

                // Default output when no conditions match
                context.AddOutputPort<StageFlow>("Default")
                    .WithDisplayName("Default")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the condition for a specific output path index.
        /// </summary>
        /// <param name="outputIndex">Zero-based output index.</param>
        /// <returns>The condition, or null if not set.</returns>
        public Condition_SO GetOutputCondition(int outputIndex)
        {
            var conditions = OutputConditions;
            if (outputIndex >= 0 && outputIndex < conditions.Count)
            {
                return conditions[outputIndex];
            }
            return null;
        }

        #endregion
    }
}
