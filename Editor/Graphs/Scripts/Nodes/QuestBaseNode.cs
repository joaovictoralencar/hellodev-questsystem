using System;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Base class for all Quest Graph nodes.
    /// Provides common port definitions and utilities.
    /// </summary>
    [Serializable]
    public abstract class QuestBaseNode : Node
    {
        /// <summary>
        /// Default name for execution/flow ports.
        /// </summary>
        public const string FLOW_PORT_NAME = "Flow";

        #region Option Helpers

        /// <summary>
        /// Gets the value of a node option by name.
        /// Returns default(T) if the option doesn't exist or has no value.
        /// </summary>
        protected T GetOptionValue<T>(string optionName)
        {
            var option = GetNodeOptionByName(optionName);
            if (option != null && option.TryGetValue<T>(out var value))
                return value;
            return default;
        }

        #endregion

        /// <summary>
        /// Adds standard input and output flow ports for sequential execution.
        /// Use this for nodes that participate in a linear flow.
        /// </summary>
        /// <param name="context">The port definition context.</param>
        protected void AddFlowPorts(IPortDefinitionContext context)
        {
            context.AddInputPort(FLOW_PORT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort(FLOW_PORT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// Adds only an output flow port (for start nodes).
        /// </summary>
        /// <param name="context">The port definition context.</param>
        /// <param name="displayName">Optional display name for the port.</param>
        protected void AddOutputFlowPort(IPortDefinitionContext context, string displayName = "")
        {
            context.AddOutputPort(FLOW_PORT_NAME)
                .WithDisplayName(displayName)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        /// <summary>
        /// Adds only an input flow port (for terminal nodes).
        /// </summary>
        /// <param name="context">The port definition context.</param>
        /// <param name="displayName">Optional display name for the port.</param>
        protected void AddInputFlowPort(IPortDefinitionContext context, string displayName = "")
        {
            context.AddInputPort(FLOW_PORT_NAME)
                .WithDisplayName(displayName)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
