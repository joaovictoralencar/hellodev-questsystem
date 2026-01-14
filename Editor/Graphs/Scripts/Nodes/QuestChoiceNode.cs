using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a branching point between quests in a QuestLine.
    /// Routes to different quests based on conditions or quest outcomes.
    /// </summary>
    /// <remarks>
    /// Use this node after a QuestNode to branch the questline:
    /// - Quest1 -> QuestChoiceNode -> Quest2 (Path A)
    ///                             -> Quest3 (Path B, skips Quest2)
    ///
    /// Supports multiple output targets with conditional routing.
    /// Set "Output Count" > 1 for conditional branching.
    /// First matching condition determines the output path.
    /// </remarks>
    [Serializable]
    public class QuestChoiceNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_CHOICE_ID = "ChoiceId";
        private const string OPT_CHOICE_NAME = "ChoiceName";
        private const string OPT_CONDITIONS = "Conditions";
        private const string OPT_WORLD_FLAGS = "WorldFlagsOnSelect";
        private const string OPT_OUTPUT_COUNT = "OutputCount";
        private const string OPT_OUTPUT_CONDITIONS = "OutputConditions";

        #endregion

        #region Constants

        private const int MIN_OUTPUTS = 1;
        private const int MAX_OUTPUTS = 4;

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for this quest branch point.
        /// </summary>
        public string ChoiceId => GetOptionValue<string>(OPT_CHOICE_ID);

        /// <summary>
        /// Developer-friendly name for this branch point.
        /// </summary>
        public string ChoiceName => GetOptionValue<string>(OPT_CHOICE_NAME);

        /// <summary>
        /// Conditions required for this branch to be evaluated.
        /// </summary>
        public List<Condition_SO> Conditions => GetOptionValue<List<Condition_SO>>(OPT_CONDITIONS) ?? new List<Condition_SO>();

        /// <summary>
        /// World flags to set when this branch is taken.
        /// </summary>
        public List<WorldFlagModification> WorldFlagsOnSelect => GetOptionValue<List<WorldFlagModification>>(OPT_WORLD_FLAGS) ?? new List<WorldFlagModification>();

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

        /// <summary>
        /// Display name for this node.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(ChoiceName))
                    return $"[Branch] {ChoiceName}";
                return "[Quest Branch]";
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Identity options
            context.AddOption<string>(OPT_CHOICE_ID)
                .WithDisplayName("Branch ID")
                .WithDefaultValue(Guid.NewGuid().ToString().Substring(0, 8))
                .WithTooltip("Unique identifier for this quest branch point")
                .Delayed();

            context.AddOption<string>(OPT_CHOICE_NAME)
                .WithDisplayName("Branch Name")
                .WithDefaultValue("Quest Branch")
                .WithTooltip("Developer-friendly name for this branch point")
                .Delayed();

            // Conditions & Consequences
            context.AddOption<List<Condition_SO>>(OPT_CONDITIONS)
                .WithDisplayName("Branch Conditions")
                .WithTooltip("Conditions required for this branch to be evaluated")
                .ShowInInspectorOnly();

            context.AddOption<List<WorldFlagModification>>(OPT_WORLD_FLAGS)
                .WithDisplayName("World Flags On Branch")
                .WithTooltip("World flags to set when this branch is taken")
                .ShowInInspectorOnly();

            // Dynamic output configuration
            context.AddOption<int>(OPT_OUTPUT_COUNT)
                .WithDisplayName("Output Count")
                .WithDefaultValue(MIN_OUTPUTS)
                .WithTooltip($"Number of output paths ({MIN_OUTPUTS}-{MAX_OUTPUTS}). Use >1 for conditional routing.")
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
            // Input: From QuestNode's Then port
            context.AddInputPort<QuestFlow>("In")
                .WithDisplayName("From Quest")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Dynamic output ports based on output count
            var outputCount = MIN_OUTPUTS;
            var outputCountOption = GetNodeOptionByName(OPT_OUTPUT_COUNT);
            if (outputCountOption != null && outputCountOption.TryGetValue<int>(out var count))
            {
                outputCount = Math.Clamp(count, MIN_OUTPUTS, MAX_OUTPUTS);
            }

            if (outputCount == 1)
            {
                // Single output - simple case
                context.AddOutputPort<QuestFlow>("Target")
                    .WithDisplayName("To Quest")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
            else
            {
                // Multiple outputs with conditions
                for (int i = 0; i < outputCount; i++)
                {
                    context.AddOutputPort<QuestFlow>($"Target{i}")
                        .WithDisplayName($"Path {i + 1}")
                        .WithConnectorUI(PortConnectorUI.Arrowhead)
                        .Build();
                }

                // Default output when no conditions match
                context.AddOutputPort<QuestFlow>("Default")
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
