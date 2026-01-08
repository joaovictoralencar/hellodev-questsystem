using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// QuestGraph validation methods.
    /// </summary>
    public partial class GraphValidationService
    {
        /// <summary>
        /// Validates a QuestGraph and returns all issues found.
        /// </summary>
        public List<ValidationResult> ValidateQuestGraph(QuestGraph graph)
        {
            var results = new List<ValidationResult>();

            if (graph == null)
            {
                results.Add(ValidationResult.Error("Graph is null"));
                return results;
            }

            var nodes = graph.GetNodes().ToList();

            // Rule: Exactly one start node
            ValidateStartNode<QuestStartNode>(nodes, "QuestStartNode", results, graph);

            // Get stage nodes for further validation
            var stageNodes = nodes.OfType<StageNode>().ToList();
            var stageSubgraphNodes = nodes.OfType<ISubgraphNode>()
                .Where(n => n.GetSubgraph() is StageGraph)
                .ToList();
            var allStageNodes = stageNodes.Cast<INode>().Concat(stageSubgraphNodes).ToList();

            // Rule: At least one stage
            if (allStageNodes.Count == 0)
            {
                results.Add(ValidationResult.Error(
                    "Quest must have at least one stage (StageNode or Stage subgraph)",
                    graph: graph));
            }

            // Rule: At least one terminal stage
            ValidateTerminalStages(stageNodes, stageSubgraphNodes, allStageNodes, results, graph);

            // Rule: No duplicate stage indices
            ValidateDuplicateStageIndices(stageNodes, stageSubgraphNodes, results, graph);

            // Rule: Stage indices should use gaps
            ValidateStageIndexGaps(stageNodes, stageSubgraphNodes, results, graph);

            // Validate individual nodes
            ValidateStageNodes(stageNodes, results, graph);
            ValidateChoiceNodes(nodes.OfType<ChoiceNode>().ToList(), results, graph);
            ValidateTaskGroupContextNodes(nodes.OfType<TaskGroupContextNode>().ToList(), results, graph);
            ValidateRewardContextNodes(nodes.OfType<RewardContextNode>().ToList(), results, graph);
            ValidateSwitchNodes(nodes.OfType<SwitchNode>().ToList(), results, graph);
            ValidateConditionGateNodes(nodes.OfType<ConditionGateNode>().ToList(), results, graph);

            return results;
        }

        #region Stage Validation

        private void ValidateTerminalStages(
            List<StageNode> stageNodes,
            List<ISubgraphNode> stageSubgraphNodes,
            List<INode> allStageNodes,
            List<ValidationResult> results,
            Graph graph)
        {
            var terminalStages = stageNodes.Where(s => s.IsTerminal).ToList();
            var terminalSubgraphs = stageSubgraphNodes
                .Where(s => (s.GetSubgraph() as StageGraph)?.IsTerminal == true)
                .ToList();

            if (terminalStages.Count == 0 && terminalSubgraphs.Count == 0 && allStageNodes.Count > 0)
            {
                results.Add(ValidationResult.Error(
                    "Quest must have at least one terminal stage",
                    graph: graph));
            }
        }

        private void ValidateDuplicateStageIndices(
            List<StageNode> stageNodes,
            List<ISubgraphNode> stageSubgraphNodes,
            List<ValidationResult> results,
            Graph graph)
        {
            var allIndices = stageNodes.Select(s => (s.StageIndex, (INode)s))
                .Concat(stageSubgraphNodes.Select(s => ((s.GetSubgraph() as StageGraph)?.StageIndex ?? 0, (INode)s)))
                .ToList();

            var duplicates = allIndices
                .GroupBy(x => x.Item1)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicates)
            {
                foreach (var (index, node) in group)
                {
                    results.Add(ValidationResult.Error(
                        $"Duplicate stage index: {index}",
                        node, graph));
                }
            }
        }

        private void ValidateStageIndexGaps(
            List<StageNode> stageNodes,
            List<ISubgraphNode> stageSubgraphNodes,
            List<ValidationResult> results,
            Graph graph)
        {
            var indices = stageNodes.Select(s => s.StageIndex)
                .Concat(stageSubgraphNodes.Select(s => (s.GetSubgraph() as StageGraph)?.StageIndex ?? 0))
                .OrderBy(i => i)
                .ToList();

            for (int i = 1; i < indices.Count; i++)
            {
                if (indices[i] - indices[i - 1] == 1)
                {
                    results.Add(ValidationResult.Warning(
                        $"Consider using gaps between stage indices (e.g., 0, 10, 20) for easier insertion. " +
                        $"Found consecutive indices: {indices[i - 1]}, {indices[i]}",
                        graph: graph));
                    break; // Only warn once
                }
            }
        }

        private void ValidateStageNodes(List<StageNode> stageNodes, List<ValidationResult> results, Graph graph)
        {
            foreach (var node in stageNodes)
            {
                // Non-terminal stages should have at least one output connection
                if (!node.IsTerminal)
                {
                    bool hasThenConnection = HasOutputConnection(node, "Then");
                    bool hasElseConnection = HasOutputConnection(node, "Else");
                    bool hasChoicesConnection = HasOutputConnection(node, "Choices");

                    if (!hasThenConnection && !hasElseConnection && !hasChoicesConnection)
                    {
                        results.Add(ValidationResult.Error(
                            $"Non-terminal stage '{node.StageName}' has no output connections",
                            node, graph));
                    }
                }

                // Stages with player choices should have choice connections
                if (node.HasPlayerChoices && !HasOutputConnection(node, "Choices"))
                {
                    results.Add(ValidationResult.Warning(
                        $"Stage '{node.StageName}' has player choices enabled but no choice connections",
                        node, graph));
                }
            }
        }

        #endregion

        #region Choice Validation

        private void ValidateChoiceNodes(List<ChoiceNode> choiceNodes, List<ValidationResult> results, Graph graph)
        {
            foreach (var node in choiceNodes)
            {
                var outputCount = node.OutputCount;

                if (outputCount == 1)
                {
                    // Single output mode - check Target connection
                    if (!HasOutputConnection(node, "Target"))
                    {
                        results.Add(ValidationResult.Error(
                            $"Choice '{node.ChoiceId}' has no target stage connection",
                            node, graph));
                    }
                }
                else
                {
                    // Multiple output mode - check at least one path is connected
                    bool hasAnyOutput = false;
                    for (int i = 0; i < outputCount; i++)
                    {
                        if (HasOutputConnection(node, $"Target{i}"))
                        {
                            hasAnyOutput = true;
                            break;
                        }
                    }

                    if (!hasAnyOutput && !HasOutputConnection(node, "Default"))
                    {
                        results.Add(ValidationResult.Error(
                            $"Choice '{node.ChoiceId}' has no output connections",
                            node, graph));
                    }

                    // Check output conditions are defined
                    var outputConditions = node.OutputConditions;
                    if (outputConditions.Count == 0 || outputConditions.All(c => c == null))
                    {
                        results.Add(ValidationResult.Warning(
                            $"Choice '{node.ChoiceId}' has multiple outputs but no conditions defined",
                            node, graph));
                    }
                }

                // Warn on empty choice text
                if (node.ChoiceText == null || node.ChoiceText.IsEmpty)
                {
                    results.Add(ValidationResult.Warning(
                        $"Choice '{node.ChoiceId}' has no choice text",
                        node, graph));
                }
            }
        }

        #endregion

        #region Context Node Validation

        private void ValidateTaskGroupContextNodes(List<TaskGroupContextNode> contextNodes, List<ValidationResult> results, Graph graph)
        {
            foreach (var node in contextNodes)
            {
                var taskBlocks = node.blockNodes.OfType<TaskBlockBase>().ToList();

                // Context should have at least one task block
                if (taskBlocks.Count == 0)
                {
                    results.Add(ValidationResult.Warning(
                        $"TaskGroupContext '{node.GroupName}' has no task blocks",
                        node, graph));
                }

                // Validate each task block
                foreach (var taskBlock in taskBlocks)
                {
                    if (!taskBlock.HasValidTask)
                    {
                        results.Add(ValidationResult.Warning(
                            $"Task block '{taskBlock.TaskTypeName}' in '{node.GroupName}' has no valid task configuration",
                            node, graph));
                    }
                }

                // OptionalXofY: requiredCount must be valid
                if (node.ExecutionMode == TaskGroups.TaskExecutionMode.OptionalXofY)
                {
                    if (node.RequiredCount < 1)
                    {
                        results.Add(ValidationResult.Error(
                            $"TaskGroupContext '{node.GroupName}' uses OptionalXofY but requiredCount is {node.RequiredCount}",
                            node, graph));
                    }

                    if (node.RequiredCount > taskBlocks.Count)
                    {
                        results.Add(ValidationResult.Error(
                            $"TaskGroupContext '{node.GroupName}' requires {node.RequiredCount} tasks but only has {taskBlocks.Count}",
                            node, graph));
                    }
                }
            }
        }

        private void ValidateRewardContextNodes(List<RewardContextNode> rewardNodes, List<ValidationResult> results, Graph graph)
        {
            foreach (var node in rewardNodes)
            {
                var rewardBlocks = node.blockNodes.OfType<RewardBlock>().ToList();

                // Context should have at least one reward block
                if (rewardBlocks.Count == 0)
                {
                    results.Add(ValidationResult.Warning(
                        $"RewardContext '{node.NodeName}' has no reward blocks",
                        node, graph));
                }

                // Validate each reward block
                foreach (var rewardBlock in rewardBlocks)
                {
                    if (!rewardBlock.IsValid)
                    {
                        results.Add(ValidationResult.Warning(
                            $"Reward block in '{node.NodeName}' has invalid configuration (missing type or amount <= 0)",
                            node, graph));
                    }
                }

                // ChooseOne mode should have multiple rewards
                if (node.GrantMode == RewardContextNode.RewardGrantMode.ChooseOne && rewardBlocks.Count < 2)
                {
                    results.Add(ValidationResult.Warning(
                        $"RewardContext '{node.NodeName}' uses ChooseOne mode but has fewer than 2 rewards",
                        node, graph));
                }
            }
        }

        #endregion

        #region Flow Control Validation

        private void ValidateSwitchNodes(List<SwitchNode> switchNodes, List<ValidationResult> results, Graph graph)
        {
            foreach (var node in switchNodes)
            {
                var branchCount = node.BranchCount;
                var conditions = node.BranchConditions;

                // Check if conditions are defined
                if (conditions.Count == 0 || conditions.All(c => c == null))
                {
                    results.Add(ValidationResult.Warning(
                        $"Switch node '{node.NodeName}' has no conditions defined",
                        node, graph));
                }

                // Check if all branches have output connections
                bool hasAnyOutput = false;
                int disconnectedBranches = 0;

                for (int i = 0; i < branchCount; i++)
                {
                    if (HasOutputConnection(node, $"Branch{i}"))
                    {
                        hasAnyOutput = true;
                    }
                    else
                    {
                        disconnectedBranches++;
                    }
                }

                if (!hasAnyOutput && !HasOutputConnection(node, "Default"))
                {
                    results.Add(ValidationResult.Error(
                        $"Switch node '{node.NodeName}' has no output connections",
                        node, graph));
                }
                else if (disconnectedBranches > 0)
                {
                    results.Add(ValidationResult.Warning(
                        $"Switch node '{node.NodeName}' has {disconnectedBranches} disconnected branch(es)",
                        node, graph));
                }
            }
        }

        private void ValidateConditionGateNodes(List<ConditionGateNode> gateNodes, List<ValidationResult> results, Graph graph)
        {
            foreach (var node in gateNodes)
            {
                var conditions = node.Conditions;
                var mode = node.Mode;

                // Check if conditions are defined
                if (conditions.Count == 0 || conditions.All(c => c == null))
                {
                    results.Add(ValidationResult.Warning(
                        $"Condition gate '{node.GateName}' has no conditions defined",
                        node, graph));
                }

                // XOfY mode validation
                if (mode == ConditionGateNode.ConditionMode.XOfY)
                {
                    if (node.RequiredCount < 1)
                    {
                        results.Add(ValidationResult.Error(
                            $"Condition gate '{node.GateName}' uses XOfY mode but RequiredCount is {node.RequiredCount}",
                            node, graph));
                    }

                    int validConditions = conditions.Count(c => c != null);
                    if (node.RequiredCount > validConditions)
                    {
                        results.Add(ValidationResult.Error(
                            $"Condition gate '{node.GateName}' requires {node.RequiredCount} conditions but only {validConditions} are defined",
                            node, graph));
                    }
                }

                // Check output connections
                if (!HasOutputConnection(node, "Then") && !HasOutputConnection(node, "Else"))
                {
                    results.Add(ValidationResult.Warning(
                        $"Condition gate '{node.GateName}' has no output connections",
                        node, graph));
                }
            }
        }

        #endregion
    }
}
