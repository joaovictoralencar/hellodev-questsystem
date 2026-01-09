using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A switch node that routes flow based on conditions.
    /// Uses dynamic port count to allow multiple branches.
    /// </summary>
    /// <remarks>
    /// Use for conditional branching without player choices.
    /// Each branch has its own condition - first matching condition wins.
    /// If no conditions match, flow goes through the Default output.
    ///
    /// Example: Route quest based on player faction, level, or world state.
    /// </remarks>
    [Serializable]
    public class SwitchNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_BRANCH_COUNT = "BranchCount";
        private const string OPT_BRANCH_CONDITIONS = "BranchConditions";

        #endregion

        #region Constants

        private const int MIN_BRANCHES = 2;
        private const int MAX_BRANCHES = 8;

        #endregion

        #region Properties

        /// <summary>
        /// Developer-friendly name for this switch node.
        /// </summary>
        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);

        /// <summary>
        /// Number of conditional branches.
        /// </summary>
        public int BranchCount
        {
            get
            {
                var count = GetOptionValue<int>(OPT_BRANCH_COUNT);
                return Math.Clamp(count, MIN_BRANCHES, MAX_BRANCHES);
            }
        }

        /// <summary>
        /// Conditions for each branch. Index corresponds to branch number.
        /// </summary>
        public List<Condition_SO> BranchConditions => GetOptionValue<List<Condition_SO>>(OPT_BRANCH_CONDITIONS) ?? new List<Condition_SO>();

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_NODE_NAME)
                .WithDisplayName("Node Name")
                .WithDefaultValue("Switch")
                .WithTooltip("Developer-friendly name for this switch node")
                .Delayed();

            context.AddOption<int>(OPT_BRANCH_COUNT)
                .WithDisplayName("Branch Count")
                .WithDefaultValue(MIN_BRANCHES)
                .WithTooltip($"Number of conditional branches ({MIN_BRANCHES}-{MAX_BRANCHES})")
                .Delayed();

            context.AddOption<List<Condition_SO>>(OPT_BRANCH_CONDITIONS)
                .WithDisplayName("Branch Conditions")
                .WithTooltip("Conditions for each branch (evaluated in order)")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous node
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Dynamic output ports based on branch count
            // Note: Must handle null option during node scanning (before options are committed)
            var branchCount = MIN_BRANCHES;
            var portCountOption = GetNodeOptionByName(OPT_BRANCH_COUNT);
            if (portCountOption != null && portCountOption.TryGetValue<int>(out var count))
            {
                branchCount = Math.Clamp(count, MIN_BRANCHES, MAX_BRANCHES);
            }

            for (int i = 0; i < branchCount; i++)
            {
                context.AddOutputPort<StageFlow>($"Branch{i}")
                    .WithDisplayName($"Case {i + 1}")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }

            // Default output when no conditions match
            context.AddOutputPort<StageFlow>("Default")
                .WithDisplayName("Default")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the condition for a specific branch index.
        /// </summary>
        /// <param name="branchIndex">Zero-based branch index.</param>
        /// <returns>The condition, or null if not set.</returns>
        public Condition_SO GetBranchCondition(int branchIndex)
        {
            var conditions = BranchConditions;
            if (branchIndex >= 0 && branchIndex < conditions.Count)
            {
                return conditions[branchIndex];
            }
            return null;
        }

        #endregion
    }
}
