using System;
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
    /// - If Evaluate() returns true → flow continues via "Then" port
    /// - If Evaluate() returns false → flow continues via "Else" port
    /// </remarks>
    [Serializable]
    public class ConditionGateNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_CONDITION = "Condition";
        private const string OPT_GATE_NAME = "GateName";
        private const string OPT_INVERT_RESULT = "InvertResult";

        #endregion

        #region Properties

        /// <summary>
        /// The condition to evaluate for branching.
        /// </summary>
        public Condition_SO Condition => GetOptionValue<Condition_SO>(OPT_CONDITION);

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
        public string DisplayName => !string.IsNullOrEmpty(GateName)
            ? $"[Gate] {GateName}"
            : Condition != null
                ? $"[Gate] {Condition.name}"
                : "[Gate] No Condition";

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<Condition_SO>(OPT_CONDITION)
                .WithDisplayName("Condition")
                .WithTooltip("The condition to evaluate. True → Then, False → Else");

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

            // Failure path - condition evaluates to false
            context.AddOutputPort<StageFlow>("Else")
                .WithDisplayName("Else (False)")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
