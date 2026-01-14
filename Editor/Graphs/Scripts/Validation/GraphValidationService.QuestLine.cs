using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// QuestLineGraph validation methods.
    /// </summary>
    public partial class GraphValidationService
    {
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

            // Get quest nodes
            var questNodes = nodes.OfType<QuestNode>().ToList();

            // Get quest subgraph nodes (native subgraph functionality)
            var questSubgraphs = nodes.OfType<ISubgraphNode>()
                .Where(n => n.GetSubgraph() is QuestGraph)
                .ToList();

            // Rule: At least one quest reference (either QuestNode or QuestGraph subgraph)
            if (questNodes.Count == 0 && questSubgraphs.Count == 0)
            {
                results.Add(ValidationResult.Warning(
                    "QuestLine has no quests. Add QuestNode nodes or embed QuestGraph subgraphs.",
                    graph: graph));
            }

            // Validate each quest node
            foreach (var questNode in questNodes)
            {
                ValidateQuestNode(questNode, results, graph);
            }

            // Get and validate quest choice nodes
            var questChoiceNodes = nodes.OfType<QuestChoiceNode>().ToList();
            foreach (var choiceNode in questChoiceNodes)
            {
                ValidateQuestChoiceNode(choiceNode, results, graph);
            }

            return results;
        }

        private void ValidateQuestNode(QuestNode node, List<ValidationResult> results, Graph graph)
        {
            // Check for empty references
            if (!node.HasValidQuest)
            {
                results.Add(ValidationResult.Warning(
                    $"Quest node has no Quest Asset assigned",
                    node, graph));
            }
        }

        private void ValidateQuestChoiceNode(QuestChoiceNode node, List<ValidationResult> results, Graph graph)
        {
            // Check for empty choice ID
            if (string.IsNullOrWhiteSpace(node.ChoiceId))
            {
                results.Add(ValidationResult.Warning(
                    $"Quest branch node '{node.ChoiceName}' has no Branch ID",
                    node, graph));
            }

            // Check that at least one output is connected
            var outputCount = node.OutputCount;
            bool hasAnyConnection = false;

            if (outputCount == 1)
            {
                var targetPort = node.GetOutputPortByName("Target");
                hasAnyConnection = targetPort != null && targetPort.isConnected;
            }
            else
            {
                for (int i = 0; i < outputCount; i++)
                {
                    var targetPort = node.GetOutputPortByName($"Target{i}");
                    if (targetPort != null && targetPort.isConnected)
                    {
                        hasAnyConnection = true;
                        break;
                    }
                }

                // Also check Default port
                var defaultPort = node.GetOutputPortByName("Default");
                if (defaultPort != null && defaultPort.isConnected)
                {
                    hasAnyConnection = true;
                }
            }

            if (!hasAnyConnection)
            {
                results.Add(ValidationResult.Warning(
                    $"Quest branch node '{node.DisplayName}' has no connected outputs",
                    node, graph));
            }

            // Check output conditions for multi-output mode
            if (outputCount > 1)
            {
                var conditions = node.OutputConditions;
                if (conditions.Count < outputCount)
                {
                    results.Add(ValidationResult.Info(
                        $"Quest branch node '{node.DisplayName}' has {outputCount} outputs but only {conditions.Count} conditions configured. Missing conditions will use Default path.",
                        node, graph));
                }
            }
        }
    }
}
