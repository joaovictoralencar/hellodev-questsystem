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
    }
}
