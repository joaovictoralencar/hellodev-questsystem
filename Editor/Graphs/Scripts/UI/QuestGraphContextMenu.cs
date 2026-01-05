using System.Linq;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.UI
{
    /// <summary>
    /// Provides context menu actions for the Quest Graph Editor.
    /// These actions appear when right-clicking in the graph view.
    /// </summary>
    /// <remarks>
    /// Note: Graph Toolkit may require specific integration for context menus.
    /// This class provides the logic; actual menu integration depends on
    /// how Graph Toolkit exposes context menu customization.
    /// </remarks>
    public static class QuestGraphContextMenu
    {
        #region Stage Actions

        /// <summary>
        /// Suggests the next available stage index.
        /// </summary>
        public static int GetNextStageIndex(QuestGraph graph)
        {
            if (graph == null) return 0;

            var maxIndex = graph.GetNodes()
                .OfType<StageNode>()
                .Select(s => s.StageIndex)
                .DefaultIfEmpty(-10)
                .Max();

            return maxIndex + 10;
        }

        /// <summary>
        /// Logs the next available stage index for debugging.
        /// </summary>
        public static void LogNextStageIndex(QuestGraph graph)
        {
            if (graph == null) return;

            var nextIndex = GetNextStageIndex(graph);
            Debug.Log($"[QuestGraphContextMenu] Next available stage index: {nextIndex}");
        }

        #endregion

        #region Validation Actions

        /// <summary>
        /// Validates the current graph and logs results.
        /// </summary>
        public static void ValidateGraph(Graph graph)
        {
            if (graph == null) return;

            var validationService = new GraphValidationService();
            var graphType = graph.GetType();

            if (graphType == typeof(QuestGraph))
            {
                var questGraph = (QuestGraph)(object)graph;
                var results = validationService.ValidateQuestGraph(questGraph);

                var reachabilityAnalyzer = new GraphReachabilityAnalyzer();
                results.AddRange(reachabilityAnalyzer.ValidateReachability(questGraph));

                LogValidationResults("Quest Graph", results);
            }
            else if (graphType == typeof(QuestLineGraph))
            {
                var questLineGraph = (QuestLineGraph)(object)graph;
                var results = validationService.ValidateQuestLineGraph(questLineGraph);
                LogValidationResults("QuestLine Graph", results);
            }
            else if (graphType == typeof(StageGraph))
            {
                var stageGraph = (StageGraph)(object)graph;
                var results = validationService.ValidateStageGraph(stageGraph);
                LogValidationResults("Stage Graph", results);
            }
            else if (graphType == typeof(TaskGroupGraph))
            {
                var taskGroupGraph = (TaskGroupGraph)(object)graph;
                var results = validationService.ValidateTaskGroupGraph(taskGroupGraph);
                LogValidationResults("TaskGroup Graph", results);
            }
        }

        private static void LogValidationResults(string graphType, System.Collections.Generic.List<ValidationResult> results)
        {
            if (results.Count == 0)
            {
                Debug.Log($"[{graphType}] Validation passed - no issues found.");
                return;
            }

            var errors = results.Count(r => r.Severity == ValidationSeverity.Error);
            var warnings = results.Count(r => r.Severity == ValidationSeverity.Warning);

            Debug.Log($"[{graphType}] Validation: {errors} error(s), {warnings} warning(s)");

            foreach (var result in results)
            {
                switch (result.Severity)
                {
                    case ValidationSeverity.Error:
                        Debug.LogError($"[{graphType}] ERROR: {result.Message}");
                        break;
                    case ValidationSeverity.Warning:
                        Debug.LogWarning($"[{graphType}] WARNING: {result.Message}");
                        break;
                    case ValidationSeverity.Info:
                        Debug.Log($"[{graphType}] INFO: {result.Message}");
                        break;
                }
            }
        }

        #endregion

        #region Helper Actions

        /// <summary>
        /// Highlights unreachable nodes in the graph.
        /// </summary>
        public static void HighlightUnreachableNodes(QuestGraph graph)
        {
            if (graph == null) return;

            var analyzer = new GraphReachabilityAnalyzer();
            var result = analyzer.AnalyzeQuestGraph(graph);

            if (result.UnreachableNodes.Count == 0)
            {
                Debug.Log("[QuestGraphContextMenu] All nodes are reachable from start.");
            }
            else
            {
                Debug.LogWarning($"[QuestGraphContextMenu] Found {result.UnreachableNodes.Count} unreachable node(s):");
                foreach (var node in result.UnreachableNodes)
                {
                    Debug.LogWarning($"  - {node.GetType().Name}");
                }
            }
        }

        #endregion

        #region Graph Statistics

        /// <summary>
        /// Shows statistics about the current graph.
        /// </summary>
        public static void ShowGraphStatistics(Graph graph)
        {
            if (graph == null) return;

            var nodes = graph.GetNodes().ToList();
            var nodesByType = nodes.GroupBy(n => n.GetType().Name)
                .Select(g => $"  {g.Key}: {g.Count()}")
                .ToList();

            var message = $"Graph Statistics:\n" +
                          $"Total Nodes: {nodes.Count}\n" +
                          $"Node Types:\n{string.Join("\n", nodesByType)}";

            if (graph.GetType() == typeof(QuestGraph))
            {
                var stages = nodes.OfType<StageNode>().ToList();
                var terminalCount = stages.Count(s => s.IsTerminal);
                var choiceCount = nodes.OfType<ChoiceNode>().Count();

                message += $"\n\nQuest-Specific:\n" +
                           $"  Stages: {stages.Count} ({terminalCount} terminal)\n" +
                           $"  Choices: {choiceCount}";
            }

            Debug.Log(message);
            EditorUtility.DisplayDialog("Graph Statistics", message, "OK");
        }

        #endregion
    }
}
