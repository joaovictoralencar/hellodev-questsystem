using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// Analyzes graph reachability to find unreachable nodes and cycles.
    /// </summary>
    public class GraphReachabilityAnalyzer
    {
        /// <summary>
        /// Result of a reachability analysis.
        /// </summary>
        public class ReachabilityResult
        {
            /// <summary>
            /// All nodes that are reachable from the start node.
            /// </summary>
            public HashSet<INode> ReachableNodes { get; } = new HashSet<INode>();

            /// <summary>
            /// All nodes that are NOT reachable from the start node.
            /// </summary>
            public HashSet<INode> UnreachableNodes { get; } = new HashSet<INode>();

            /// <summary>
            /// True if a cycle was detected during traversal.
            /// </summary>
            public bool HasCycle { get; set; }

            /// <summary>
            /// Nodes involved in cycles (if any).
            /// </summary>
            public List<INode> CycleNodes { get; } = new List<INode>();
        }

        /// <summary>
        /// Analyzes reachability in a QuestGraph starting from QuestStartNode.
        /// </summary>
        public ReachabilityResult AnalyzeQuestGraph(QuestGraph graph)
        {
            var result = new ReachabilityResult();
            var allNodes = graph.GetNodes().ToHashSet();

            // Find start node
            var startNode = allNodes.OfType<QuestStartNode>().FirstOrDefault();
            if (startNode == null)
            {
                // All nodes are unreachable if no start
                result.UnreachableNodes.UnionWith(allNodes);
                return result;
            }

            // BFS from start node
            var visited = new HashSet<INode>();
            var visiting = new HashSet<INode>(); // For cycle detection
            var queue = new Queue<INode>();

            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);
                result.ReachableNodes.Add(current);

                // Get all connected nodes via output ports
                var connectedNodes = GetConnectedOutputNodes(current);

                foreach (var connected in connectedNodes)
                {
                    if (!visited.Contains(connected))
                    {
                        queue.Enqueue(connected);
                    }
                }
            }

            // Find unreachable nodes
            result.UnreachableNodes.UnionWith(allNodes.Except(result.ReachableNodes));

            // Check for cycles using DFS
            DetectCycles(startNode, new HashSet<INode>(), new HashSet<INode>(), result);

            return result;
        }

        /// <summary>
        /// Analyzes reachability in a QuestLineGraph starting from QuestLineStartNode.
        /// </summary>
        public ReachabilityResult AnalyzeQuestLineGraph(QuestLineGraph graph)
        {
            var result = new ReachabilityResult();
            var allNodes = graph.GetNodes().ToHashSet();

            // Find start node
            var startNode = allNodes.OfType<QuestLineStartNode>().FirstOrDefault();
            if (startNode == null)
            {
                result.UnreachableNodes.UnionWith(allNodes);
                return result;
            }

            // BFS from start node
            var visited = new HashSet<INode>();
            var queue = new Queue<INode>();

            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);
                result.ReachableNodes.Add(current);

                var connectedNodes = GetConnectedOutputNodes(current);

                foreach (var connected in connectedNodes)
                {
                    if (!visited.Contains(connected))
                    {
                        queue.Enqueue(connected);
                    }
                }
            }

            result.UnreachableNodes.UnionWith(allNodes.Except(result.ReachableNodes));

            return result;
        }

        /// <summary>
        /// Gets all nodes connected to the output ports of the given node.
        /// </summary>
        private List<INode> GetConnectedOutputNodes(INode node)
        {
            var connectedNodes = new List<INode>();

            try
            {
                var outputPorts = node.GetOutputPorts();

                foreach (var port in outputPorts)
                {
                    if (port.isConnected)
                    {
                        // Get all connected input ports (Graph Toolkit 0.4.0 API)
                        var connectedPorts = new List<IPort>();
                        port.GetConnectedPorts(connectedPorts);

                        foreach (var connectedPort in connectedPorts)
                        {
                            var connectedNode = connectedPort.GetNode();
                            if (connectedNode != null)
                            {
                                connectedNodes.Add(connectedNode);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Port access may fail for some node types
            }

            return connectedNodes;
        }

        /// <summary>
        /// Detects cycles using DFS with coloring.
        /// </summary>
        private bool DetectCycles(
            INode node,
            HashSet<INode> visiting,
            HashSet<INode> visited,
            ReachabilityResult result)
        {
            if (visited.Contains(node))
                return false;

            if (visiting.Contains(node))
            {
                // Cycle detected!
                result.HasCycle = true;
                result.CycleNodes.Add(node);
                return true;
            }

            visiting.Add(node);

            var connectedNodes = GetConnectedOutputNodes(node);

            foreach (var connected in connectedNodes)
            {
                if (DetectCycles(connected, visiting, visited, result))
                {
                    // Track nodes in the cycle path
                    if (!result.CycleNodes.Contains(node))
                    {
                        result.CycleNodes.Add(node);
                    }
                }
            }

            visiting.Remove(node);
            visited.Add(node);

            return false;
        }

        /// <summary>
        /// Validates reachability and returns validation results.
        /// </summary>
        public List<ValidationResult> ValidateReachability(QuestGraph graph)
        {
            var results = new List<ValidationResult>();
            var analysis = AnalyzeQuestGraph(graph);

            // Report unreachable nodes
            foreach (var node in analysis.UnreachableNodes)
            {
                // Skip start nodes - they're entry points
                if (node is QuestStartNode)
                    continue;

                string nodeName = GetNodeDisplayName(node);
                results.Add(ValidationResult.Warning(
                    $"Node '{nodeName}' is not reachable from the start node",
                    node, graph));
            }

            // Report cycles
            if (analysis.HasCycle)
            {
                var cycleNodeNames = analysis.CycleNodes.Select(GetNodeDisplayName);
                results.Add(ValidationResult.Warning(
                    $"Cycle detected involving nodes: {string.Join(", ", cycleNodeNames)}",
                    analysis.CycleNodes.FirstOrDefault(), graph));
            }

            return results;
        }

        /// <summary>
        /// Gets a display-friendly name for a node.
        /// </summary>
        private string GetNodeDisplayName(INode node)
        {
            return node switch
            {
                StageNode stageNode => $"Stage[{stageNode.StageIndex}]: {stageNode.StageName}",
                ChoiceNode choice => $"Choice: {choice.ChoiceId}",
                TaskGroupNode taskGroup => $"TaskGroup: {taskGroup.GroupName}",
                TaskNode task => $"Task: {task.DevName}",
                QuestStartNode _ => "QuestStartNode",
                QuestLineStartNode _ => "QuestLineStartNode",
                QuestRefNode questRef => $"QuestRef: {questRef.DisplayName}",
                ISubgraphNode subgraph when subgraph.GetSubgraph() is StageGraph sg => $"StageSubgraph[{sg.StageIndex}]: {sg.StageName}",
                ISubgraphNode subgraph when subgraph.GetSubgraph() is TaskGroupGraph tg => $"TaskGroupSubgraph: {tg.GroupName}",
                _ => node.GetType().Name
            };
        }
    }
}
