using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
using HelloDev.QuestSystem.Stages;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Defines a stage transition with full configuration options.
    /// Sits between stages to control how and when transitions occur.
    /// </summary>
    /// <remarks>
    /// Use TransitionNode when you need:
    /// - Conditional transitions (OnConditionsMet trigger)
    /// - Manual API-triggered transitions
    /// - Priority ordering for multiple transitions
    /// - Transition labels for debugging
    ///
    /// For simple linear progression, you can connect Stage→Stage directly
    /// (creates implicit OnGroupsComplete transition with priority 0).
    ///
    /// For player choices, use ChoiceNode instead.
    ///
    /// Flow: Stage.Then → TransitionNode.In ... TransitionNode.To → Stage.In
    /// </remarks>
    [Serializable]
    public class TransitionNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_TRIGGER = "Trigger";
        private const string OPT_PRIORITY = "Priority";
        private const string OPT_LABEL = "Label";

        #endregion

        #region Port Names

        private const string PORT_IN = "In";
        private const string PORT_TO = "To";
        private const string PORT_CONDITIONS = "ConditionsInput";

        #endregion

        #region Properties

        /// <summary>
        /// What triggers this transition.
        /// </summary>
        /// <remarks>
        /// - OnGroupsComplete: When all task groups in the source stage complete
        /// - OnConditionsMet: When connected conditions evaluate to true
        /// - Manual: Only via API call (QuestRuntime.TriggerManualTransition)
        /// </remarks>
        public TransitionTrigger Trigger => GetOptionValue<TransitionTrigger>(OPT_TRIGGER);

        /// <summary>
        /// Priority when multiple transitions from the same stage are valid.
        /// Higher priority transitions are evaluated first.
        /// </summary>
        public int Priority => GetOptionValue<int>(OPT_PRIORITY);

        /// <summary>
        /// Optional label for debugging and identification.
        /// </summary>
        public string Label => GetOptionValue<string>(OPT_LABEL);

        /// <summary>
        /// Gets the target stage index this transition leads to.
        /// Returns -1 if not connected to a valid stage.
        /// </summary>
        public int TargetStageIndex => GraphTraversalUtility.GetConnectedStageIndex(this, PORT_TO);

        /// <summary>
        /// Whether this transition has a valid target stage connected.
        /// </summary>
        public bool HasValidTarget => TargetStageIndex >= 0;

        /// <summary>
        /// Display name for this node in the graph.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var triggerName = Trigger switch
                {
                    TransitionTrigger.OnGroupsComplete => "Complete",
                    TransitionTrigger.OnConditionsMet => "Conditional",
                    TransitionTrigger.Manual => "Manual",
                    TransitionTrigger.PlayerChoice => "Choice", // Shouldn't be used, but handle gracefully
                    _ => "Transition"
                };

                if (!string.IsNullOrEmpty(Label))
                    return $"{triggerName}: {Label}";

                if (HasValidTarget)
                    return $"{triggerName} → Stage {TargetStageIndex}";

                return triggerName;
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<TransitionTrigger>(OPT_TRIGGER)
                .WithDisplayName("Trigger")
                .WithDefaultValue(TransitionTrigger.OnGroupsComplete)
                .WithTooltip("What triggers this transition:\n" +
                    "• OnGroupsComplete: When all task groups complete\n" +
                    "• OnConditionsMet: When conditions are met (connect ConditionContext)\n" +
                    "• Manual: Only via API call");

            context.AddOption<int>(OPT_PRIORITY)
                .WithDisplayName("Priority")
                .WithDefaultValue(0)
                .WithTooltip("Higher priority transitions are evaluated first when multiple are valid");

            context.AddOption<string>(OPT_LABEL)
                .WithDisplayName("Label")
                .WithDefaultValue("")
                .WithTooltip("Optional label for debugging and identification")
                .Delayed();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage's "Then" port
            context.AddInputPort<StageFlow>(PORT_IN)
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Conditions input: Connect to ConditionContextNode for transition conditions
            context.AddInputPort<ConditionFlow>(PORT_CONDITIONS)
                .WithDisplayName("Conditions")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Output: To target Stage
            context.AddOutputPort<StageFlow>(PORT_TO)
                .WithDisplayName("To")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
