using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Converters
{
    /// <summary>
    /// Utility class for traversing and extracting data from quest graphs.
    /// Based on the Visual Novel Director sample pattern.
    /// </summary>
    public static class GraphTraversalUtility
    {
        /// <summary>
        /// Gets the node connected to a specific output port.
        /// </summary>
        /// <param name="currentNode">The node containing the output port.</param>
        /// <param name="portName">The name of the output port.</param>
        /// <returns>The connected node, or null if not connected.</returns>
        public static INode GetNextNode(INode currentNode, string portName)
        {
            if (currentNode == null)
                return null;

            try
            {
                var outputPort = currentNode.GetOutputPortByName(portName);
                if (outputPort == null || !outputPort.isConnected)
                    return null;

                var nextNodePort = outputPort.firstConnectedPort;
                return nextNodePort?.GetNode();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all nodes connected to a specific output port.
        /// Useful for ports that can have multiple connections.
        /// </summary>
        /// <param name="currentNode">The node containing the output port.</param>
        /// <param name="portName">The name of the output port.</param>
        /// <returns>List of connected nodes.</returns>
        public static List<INode> GetAllConnectedNodes(INode currentNode, string portName)
        {
            var connectedNodes = new List<INode>();

            if (currentNode == null)
                return connectedNodes;

            try
            {
                var outputPort = currentNode.GetOutputPortByName(portName);
                if (outputPort == null || !outputPort.isConnected)
                    return connectedNodes;

                // Graph Toolkit 0.4.0 requires output parameter
                var connectedPorts = new List<IPort>();
                outputPort.GetConnectedPorts(connectedPorts);

                foreach (var port in connectedPorts)
                {
                    var node = port.GetNode();
                    if (node != null)
                    {
                        connectedNodes.Add(node);
                    }
                }
            }
            catch
            {
                // Port access failed
            }

            return connectedNodes;
        }

        /// <summary>
        /// Gets the value of an input port on a node.
        /// Value is obtained from (in priority order):
        /// 1. Connections to the port (variable nodes, constant nodes)
        /// 2. Embedded value on the port
        /// 3. Default value of the port
        /// </summary>
        /// <typeparam name="T">The type of value expected.</typeparam>
        /// <param name="port">The input port to read from.</param>
        /// <returns>The port value.</returns>
        public static T GetInputPortValue<T>(IPort port)
        {
            T value = default;

            if (port == null)
                return value;

            try
            {
                // If port is connected to another node, get value from connection
                if (port.isConnected)
                {
                    var connectedNode = port.firstConnectedPort?.GetNode();

                    switch (connectedNode)
                    {
                        case IVariableNode variableNode:
                            variableNode.variable.TryGetDefaultValue<T>(out value);
                            return value;
                        case IConstantNode constantNode:
                            constantNode.TryGetValue<T>(out value);
                            return value;
                    }
                }

                // If port has embedded value, return it
                // Otherwise, return the default value of the port
                port.TryGetValue(out value);
            }
            catch
            {
                // Port access failed, return default
            }

            return value;
        }

        /// <summary>
        /// Gets the value of an input port by name.
        /// </summary>
        /// <typeparam name="T">The type of value expected.</typeparam>
        /// <param name="node">The node containing the port.</param>
        /// <param name="portName">The name of the input port.</param>
        /// <returns>The port value.</returns>
        public static T GetInputPortValue<T>(INode node, string portName)
        {
            if (node == null)
                return default;

            try
            {
                var port = node.GetInputPortByName(portName);
                return GetInputPortValue<T>(port);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Checks if an output port is connected.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="portName">The name of the output port.</param>
        /// <returns>True if connected.</returns>
        public static bool IsOutputConnected(INode node, string portName)
        {
            if (node == null)
                return false;

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

        /// <summary>
        /// Gets the stage index from a connected stage node.
        /// Used for determining transition targets.
        /// </summary>
        /// <param name="node">The source node (e.g., StageNode, ChoiceNode).</param>
        /// <param name="portName">The output port name.</param>
        /// <returns>The target stage index, or -1 if not found.</returns>
        public static int GetConnectedStageIndex(INode node, string portName)
        {
            var connectedNode = GetNextNode(node, portName);

            if (connectedNode == null)
                return -1;

            return connectedNode switch
            {
                Nodes.StageNode stageNode => stageNode.StageIndex,
                ISubgraphNode subgraphNode when subgraphNode.GetSubgraph() is StageGraph stageGraph => stageGraph.StageIndex,
                _ => -1
            };
        }

        /// <summary>
        /// Collects all nodes of a specific type from the graph, sorted by a key selector.
        /// </summary>
        /// <typeparam name="TNode">The node type to collect.</typeparam>
        /// <typeparam name="TKey">The key type for sorting.</typeparam>
        /// <param name="graph">The graph to search.</param>
        /// <param name="keySelector">Function to extract the sort key.</param>
        /// <returns>Sorted list of nodes.</returns>
        public static List<TNode> CollectNodesSorted<TNode, TKey>(
            Graph graph,
            System.Func<TNode, TKey> keySelector) where TNode : INode
        {
            return graph.GetNodes()
                .OfType<TNode>()
                .OrderBy(keySelector)
                .ToList();
        }

        /// <summary>
        /// Builds a lookup table from stage index to stage node.
        /// </summary>
        /// <param name="graph">The quest graph.</param>
        /// <returns>Dictionary mapping stage index to stage node.</returns>
        public static Dictionary<int, INode> BuildStageIndexLookup(QuestGraph graph)
        {
            var lookup = new Dictionary<int, INode>();
            var nodes = graph.GetNodes().ToList();

            foreach (var node in nodes)
            {
                int? index = node switch
                {
                    Nodes.StageNode stageNode => stageNode.StageIndex,
                    ISubgraphNode subgraphNode when subgraphNode.GetSubgraph() is StageGraph sg => sg.StageIndex,
                    _ => null
                };

                if (index.HasValue && !lookup.ContainsKey(index.Value))
                {
                    lookup[index.Value] = node;
                }
            }

            return lookup;
        }

        #region Data Port Resolution

        /// <summary>
        /// Resolves a data value from an input port by name.
        /// Checks port connections first (Variable/Constant nodes), then embedded value.
        /// Pattern from TextureMaker sample.
        /// </summary>
        /// <typeparam name="T">The type of value expected.</typeparam>
        /// <param name="node">The node containing the port.</param>
        /// <param name="portName">The name of the input port.</param>
        /// <param name="fallback">Value to return if resolution fails.</param>
        /// <returns>The resolved value.</returns>
        public static T ResolveDataPort<T>(INode node, string portName, T fallback = default)
        {
            if (node == null)
                return fallback;

            try
            {
                var port = node.GetInputPortByName(portName);
                return ResolveDataPort(port, fallback);
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// Resolves a data value from an input port.
        /// Priority: Connected source (Variable/Constant) > Embedded value > Fallback.
        /// </summary>
        /// <typeparam name="T">The type of value expected.</typeparam>
        /// <param name="port">The input port to resolve.</param>
        /// <param name="fallback">Value to return if resolution fails.</param>
        /// <returns>The resolved value.</returns>
        public static T ResolveDataPort<T>(IPort port, T fallback = default)
        {
            if (port == null)
                return fallback;

            try
            {
                var sourcePort = port.firstConnectedPort;

                switch (sourcePort?.GetNode())
                {
                    case IConstantNode constantNode:
                        constantNode.TryGetValue(out T constantValue);
                        return constantValue;

                    case IVariableNode variableNode:
                        variableNode.variable.TryGetDefaultValue(out T variableValue);
                        return variableValue;

                    case null:
                        // Not connected: use embedded port value
                        if (port.TryGetValue(out T embeddedValue))
                            return embeddedValue;
                        return fallback;
                }
            }
            catch
            {
                // Port access failed
            }

            return fallback;
        }

        #endregion
    }
}
