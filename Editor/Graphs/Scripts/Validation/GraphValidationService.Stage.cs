using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// StageGraph validation methods.
    /// </summary>
    public partial class GraphValidationService
    {
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
    }
}
