using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// Service for validating quest graphs and reporting issues.
    /// </summary>
    public class GraphValidationService
    {
        #region QuestGraph Validation

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

            // Rule: No duplicate stage indices
            ValidateDuplicateStageIndices(stageNodes, stageSubgraphNodes, results, graph);

            // Rule: Stage indices should use gaps
            ValidateStageIndexGaps(stageNodes, stageSubgraphNodes, results, graph);

            // Validate individual stage nodes
            foreach (var stageNode in stageNodes)
            {
                ValidateStageNode(stageNode, results, graph);
            }

            // Validate choice nodes
            var choiceNodes = nodes.OfType<ChoiceNode>().ToList();
            foreach (var choiceNode in choiceNodes)
            {
                ValidateChoiceNode(choiceNode, results, graph);
            }

            // Validate task group context nodes
            var taskGroupContextNodes = nodes.OfType<TaskGroupContextNode>().ToList();
            foreach (var contextNode in taskGroupContextNodes)
            {
                ValidateTaskGroupContextNode(contextNode, results, graph);
            }

            // Validate reward context nodes
            var rewardContextNodes = nodes.OfType<RewardContextNode>().ToList();
            foreach (var rewardNode in rewardContextNodes)
            {
                ValidateRewardContextNode(rewardNode, results, graph);
            }

            // Validate switch nodes
            var switchNodes = nodes.OfType<SwitchNode>().ToList();
            foreach (var switchNode in switchNodes)
            {
                ValidateSwitchNode(switchNode, results, graph);
            }

            // Validate condition gate nodes
            var conditionGateNodes = nodes.OfType<ConditionGateNode>().ToList();
            foreach (var gateNode in conditionGateNodes)
            {
                ValidateConditionGateNode(gateNode, results, graph);
            }

            return results;
        }

        private void ValidateStageNode(StageNode node, List<ValidationResult> results, Graph graph)
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

        private void ValidateChoiceNode(ChoiceNode node, List<ValidationResult> results, Graph graph)
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

        private void ValidateTaskGroupContextNode(TaskGroupContextNode node, List<ValidationResult> results, Graph graph)
        {
            // Get blocks from context node
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

        private void ValidateRewardContextNode(RewardContextNode node, List<ValidationResult> results, Graph graph)
        {
            // Get blocks from context node
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

        private void ValidateSwitchNode(SwitchNode node, List<ValidationResult> results, Graph graph)
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

        private void ValidateConditionGateNode(ConditionGateNode node, List<ValidationResult> results, Graph graph)
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

        #endregion

        #region QuestLineGraph Validation

        /// <summary>
        /// Validates a QuestLineGraph and returns all issues found.
        /// </summary>
        public List<ValidationResult> ValidateQuestLineGraph(QuestLineGraph graph)
        {
            var results = new List<ValidationResult>();

            if (graph == null)
            {
                results.Add(ValidationResult.Error("Graph is null"));
                return results;
            }

            var nodes = graph.GetNodes().ToList();

            // Rule: Exactly one start node
            ValidateStartNode<QuestLineStartNode>(nodes, "QuestLineStartNode", results, graph);

            // Get quest reference nodes
            var questRefNodes = nodes.OfType<QuestRefNode>().ToList();

            // Rule: At least one quest reference
            if (questRefNodes.Count == 0)
            {
                results.Add(ValidationResult.Error(
                    "QuestLine must have at least one quest reference",
                    graph: graph));
            }

            // Validate each quest reference
            foreach (var questRef in questRefNodes)
            {
                ValidateQuestRefNode(questRef, results, graph);
            }

            return results;
        }

        private void ValidateQuestRefNode(QuestRefNode node, List<ValidationResult> results, Graph graph)
        {
            // Check for empty references
            bool hasAsset = node.QuestAsset != null;
            bool hasGraph = node.QuestGraphAsset != null;

            if (!hasAsset && !hasGraph)
            {
                results.Add(ValidationResult.Warning(
                    "Quest reference is empty (no Quest_SO or QuestGraph assigned)",
                    node, graph));
            }
        }

        #endregion

        #region StageGraph Validation

        /// <summary>
        /// Validates a StageGraph and returns all issues found.
        /// </summary>
        public List<ValidationResult> ValidateStageGraph(StageGraph graph)
        {
            var results = new List<ValidationResult>();

            if (graph == null)
            {
                results.Add(ValidationResult.Error("Graph is null"));
                return results;
            }

            // Stage index validation
            if (graph.StageIndex < 0)
            {
                results.Add(ValidationResult.Warning(
                    $"Stage '{graph.StageName}' has negative index: {graph.StageIndex}",
                    graph: graph));
            }

            var nodes = graph.GetNodes().ToList();

            // Check for task groups (context nodes or subgraphs)
            var taskGroupContextNodes = nodes.OfType<TaskGroupContextNode>().ToList();
            var taskGroupSubgraphNodes = nodes.OfType<ISubgraphNode>()
                .Where(n => n.GetSubgraph() is TaskGroupGraph)
                .ToList();

            if (taskGroupContextNodes.Count == 0 && taskGroupSubgraphNodes.Count == 0)
            {
                results.Add(ValidationResult.Warning(
                    $"Stage '{graph.StageName}' has no task groups",
                    graph: graph));
            }

            return results;
        }

        #endregion

        #region TaskGroupGraph Validation

        /// <summary>
        /// Validates a TaskGroupGraph and returns all issues found.
        /// </summary>
        public List<ValidationResult> ValidateTaskGroupGraph(TaskGroupGraph graph)
        {
            var results = new List<ValidationResult>();

            if (graph == null)
            {
                results.Add(ValidationResult.Error("Graph is null"));
                return results;
            }

            var nodes = graph.GetNodes().ToList();

            // Rule: At least one task
            var taskNodes = nodes.OfType<TaskBaseNode>().ToList();
            if (taskNodes.Count == 0)
            {
                results.Add(ValidationResult.Error(
                    $"TaskGroup '{graph.GroupName}' must have at least one task",
                    graph: graph));
            }

            // Rule: OptionalXofY validation
            if (graph.ExecutionMode == TaskGroups.TaskExecutionMode.OptionalXofY)
            {
                if (graph.RequiredCount > taskNodes.Count)
                {
                    results.Add(ValidationResult.Error(
                        $"TaskGroup '{graph.GroupName}' requires {graph.RequiredCount} tasks but only has {taskNodes.Count}",
                        graph: graph));
                }

                if (graph.RequiredCount < 1)
                {
                    results.Add(ValidationResult.Error(
                        $"TaskGroup '{graph.GroupName}' uses OptionalXofY but requiredCount is {graph.RequiredCount}",
                        graph: graph));
                }
            }

            // Validate each task
            foreach (var taskNode in taskNodes)
            {
                if (!taskNode.HasValidTask)
                {
                    results.Add(ValidationResult.Warning(
                        $"Task node '{taskNode.DevName}' has no valid task configuration",
                        taskNode, graph));
                }
            }

            return results;
        }

        #endregion

        #region Helper Methods

        private void ValidateStartNode<TStartNode>(
            List<INode> nodes,
            string nodeTypeName,
            List<ValidationResult> results,
            Graph graph) where TStartNode : INode
        {
            var startNodes = nodes.OfType<TStartNode>().ToList();

            if (startNodes.Count == 0)
            {
                results.Add(ValidationResult.Error(
                    $"Graph must have a {nodeTypeName}",
                    graph: graph));
            }
            else if (startNodes.Count > 1)
            {
                foreach (var node in startNodes.Skip(1))
                {
                    results.Add(ValidationResult.Error(
                        $"Only one {nodeTypeName} is allowed",
                        node, graph));
                }
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

        private bool HasOutputConnection(INode node, string portName)
        {
            try
            {
                var port = node.GetOutputPortByName(portName);
                return port != null && port.isConnected;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Returns true if the graph has no errors (warnings are OK).
        /// </summary>
        public bool IsValid(List<ValidationResult> results)
        {
            return !results.Any(r => r.Severity == ValidationSeverity.Error);
        }

        /// <summary>
        /// Gets only error results.
        /// </summary>
        public List<ValidationResult> GetErrors(List<ValidationResult> results)
        {
            return results.Where(r => r.Severity == ValidationSeverity.Error).ToList();
        }

        /// <summary>
        /// Gets only warning results.
        /// </summary>
        public List<ValidationResult> GetWarnings(List<ValidationResult> results)
        {
            return results.Where(r => r.Severity == ValidationSeverity.Warning).ToList();
        }

        #endregion
    }
}
