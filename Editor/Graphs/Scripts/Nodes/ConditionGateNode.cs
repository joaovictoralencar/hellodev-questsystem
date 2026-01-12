using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that branches flow based on condition evaluation.
    /// Unlike ChoiceNode (player decision), this is automatic branching based on game state.
    /// </summary>
    /// <remarks>
    /// Use this for:
    /// - Gating content based on prerequisites (reputation, items, quest state)
    /// - Creating different paths based on world flags
    /// - Conditional stage transitions without player choice
    ///
    /// The condition is evaluated when the flow reaches this node:
    /// - If evaluation returns true → flow continues via "Then" port
    ///
    /// Supports multiple conditions with different evaluation modes:
    /// - All: All conditions must be true (AND)
    /// - Any: At least one condition must be true (OR)
    /// - None: No conditions can be true (NOR)
    /// - XOfY: X conditions must be true out of Y total
    /// </remarks>
    [Serializable]
    public class ConditionGateNode : QuestBaseNode
    {
        #region Enums

        /// <summary>
        /// How multiple conditions are combined for evaluation.
        /// </summary>
        public enum ConditionMode
        {
            /// <summary>
            /// All conditions must be true (logical AND).
            /// </summary>
            All,

            /// <summary>
            /// At least one condition must be true (logical OR).
            /// </summary>
            Any,

            /// <summary>
            /// No conditions can be true (logical NOR).
            /// </summary>
            None,

            /// <summary>
            /// A specific number of conditions must be true.
            /// Configure with RequiredCount option.
            /// </summary>
            XOfY
        }

        #endregion

        #region Option Names

        private const string OPT_CONDITIONS = "Conditions";
        private const string OPT_CONDITION_MODE = "ConditionMode";
        private const string OPT_REQUIRED_COUNT = "RequiredCount";
        private const string OPT_GATE_NAME = "GateName";
        private const string OPT_INVERT_RESULT = "InvertResult";

        #endregion

        #region Properties

        /// <summary>
        /// The conditions to evaluate for branching.
        /// </summary>
        public List<Condition_SO> Conditions => GetOptionValue<List<Condition_SO>>(OPT_CONDITIONS) ?? new List<Condition_SO>();

        /// <summary>
        /// How multiple conditions are combined.
        /// </summary>
        public ConditionMode Mode => GetOptionValue<ConditionMode>(OPT_CONDITION_MODE);

        /// <summary>
        /// For XOfY mode: number of conditions that must be true.
        /// </summary>
        public int RequiredCount => GetOptionValue<int>(OPT_REQUIRED_COUNT);

        /// <summary>
        /// Developer-friendly name for this gate (for graph readability).
        /// </summary>
        public string GateName => GetOptionValue<string>(OPT_GATE_NAME);

        /// <summary>
        /// If true, inverts the condition result (Then becomes Else and vice versa).
        /// </summary>
        public bool InvertResult => GetOptionValue<bool>(OPT_INVERT_RESULT);

        /// <summary>
        /// Display name shown on the node.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(GateName))
                    return $"[Gate] {GateName}";

                var conditions = Conditions;
                if (conditions.Count == 0)
                    return "[Gate] No Conditions";

                if (conditions.Count == 1 && conditions[0] != null)
                    return $"[Gate] {conditions[0].name}";

                return $"[Gate] {Mode} ({conditions.Count})";
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<List<Condition_SO>>(OPT_CONDITIONS)
                .WithDisplayName("Conditions")
                .WithTooltip("The conditions to evaluate for branching");

            context.AddOption<ConditionMode>(OPT_CONDITION_MODE)
                .WithDisplayName("Mode")
                .WithDefaultValue(ConditionMode.All)
                .WithTooltip("How multiple conditions are combined:\n" +
                    "• All: All must be true (AND)\n" +
                    "• Any: At least one must be true (OR)\n" +
                    "• None: No conditions can be true (NOR)\n" +
                    "• XOfY: Specific count must be true");

            context.AddOption<int>(OPT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .WithDefaultValue(1)
                .WithTooltip("For XOfY mode: how many conditions must be true");

            context.AddOption<string>(OPT_GATE_NAME)
                .WithDisplayName("Gate Name")
                .WithDefaultValue("")
                .WithTooltip("Optional developer-friendly name for this gate")
                .Delayed();

            context.AddOption<bool>(OPT_INVERT_RESULT)
                .WithDisplayName("Invert Result")
                .WithDefaultValue(false)
                .WithTooltip("If true, swaps Then and Else behavior");
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

            // Success path - condition evaluates to true
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then (True)")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion

        #region Evaluation Helpers

        /// <summary>
        /// Gets the mode description for display purposes.
        /// </summary>
        public string GetModeDescription()
        {
            return Mode switch
            {
                ConditionMode.All => "All conditions must be true",
                ConditionMode.Any => "At least one condition must be true",
                ConditionMode.None => "No conditions can be true",
                ConditionMode.XOfY => $"{RequiredCount} of {Conditions.Count} conditions must be true",
                _ => "Unknown mode"
            };
        }

        #endregion
    }
}
