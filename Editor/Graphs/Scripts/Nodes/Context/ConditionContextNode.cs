using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Context node that contains condition blocks as a visual container.
    /// Provides trigger and failure conditions for QuestNode via the context pattern.
    /// </summary>
    /// <remarks>
    /// Flow design:
    /// - ConditionContextNode.Then (output) → QuestNode.TriggerConditions or FailConditions (input)
    /// - No input port - this node is a source of conditions, not part of a flow chain
    ///
    /// Benefits of the context pattern:
    /// - Conditions can be reordered via drag-and-drop
    /// - Each condition is a separate block for clear visual separation
    /// - Easier to add/remove individual conditions
    /// - Visual grouping with shared evaluation mode
    ///
    /// Use for quest trigger conditions (when the quest can start) or
    /// failure conditions (when the quest fails).
    /// </remarks>
    [Serializable]
    public class ConditionContextNode : ContextNode
    {
        #region Enums

        /// <summary>
        /// How multiple conditions are combined for evaluation.
        /// </summary>
        public enum ConditionMode
        {
            /// <summary>All conditions must be true (logical AND).</summary>
            All,
            /// <summary>At least one condition must be true (logical OR).</summary>
            Any,
            /// <summary>No conditions can be true (logical NOR).</summary>
            None,
            /// <summary>A specific number of conditions must be true.</summary>
            XOfY
        }

        #endregion

        #region Option Names

        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_CONDITION_MODE = "ConditionMode";
        private const string OPT_REQUIRED_COUNT = "RequiredCount";
        private const string OPT_INVERT_RESULT = "InvertResult";

        #endregion

        #region Helper Methods

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
        /// Developer-friendly name for this condition group.
        /// </summary>
        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);

        /// <summary>
        /// How conditions in this context are combined.
        /// </summary>
        public ConditionMode Mode => GetOptionValue<ConditionMode>(OPT_CONDITION_MODE);

        /// <summary>
        /// For XOfY mode: number of conditions that must be true.
        /// </summary>
        public int RequiredCount => GetOptionValue<int>(OPT_REQUIRED_COUNT);

        /// <summary>
        /// If true, inverts the final result (Then becomes Else and vice versa).
        /// </summary>
        public bool InvertResult => GetOptionValue<bool>(OPT_INVERT_RESULT);

        /// <summary>
        /// Whether to show RequiredCount option (only for XOfY mode).
        /// </summary>
        private bool ShowRequiredCount => Mode == ConditionMode.XOfY;

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_NODE_NAME)
                .WithDisplayName("Node Name")
                .WithDefaultValue("Conditions")
                .WithTooltip("Developer-friendly name for this condition group")
                .Delayed();

            context.AddOption<ConditionMode>(OPT_CONDITION_MODE)
                .WithDisplayName("Mode")
                .WithDefaultValue(ConditionMode.All)
                .WithTooltip("How conditions are combined:\n" +
                    "• All: All must be true (AND)\n" +
                    "• Any: At least one must be true (OR)\n" +
                    "• None: No conditions can be true (NOR)\n" +
                    "• XOfY: Specific count must be true");

            // Only show RequiredCount when Mode is XOfY (same pattern as StageNode)
            if (ShowRequiredCount)
            {
                context.AddOption<int>(OPT_REQUIRED_COUNT)
                    .WithDisplayName("Required Count")
                    .WithDefaultValue(1)
                    .WithTooltip("How many conditions must be true");
            }

            context.AddOption<bool>(OPT_INVERT_RESULT)
                .WithDisplayName("Invert Result")
                .WithDefaultValue(false)
                .WithTooltip("If true, swaps Then and Else behavior");
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Output: Connects to QuestNode's TriggerConditions or FailConditions input
            context.AddOutputPort<ConditionFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
