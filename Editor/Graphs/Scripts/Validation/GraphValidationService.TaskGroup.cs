using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// TaskGroupGraph validation methods.
    /// </summary>
    public partial class GraphValidationService
    {
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
    }
}
