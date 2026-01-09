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
    }
}
