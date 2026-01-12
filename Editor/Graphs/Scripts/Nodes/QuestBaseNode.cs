using System;
using HelloDev.Conditions;
using HelloDev.Conditions.WorldFlags;
using HelloDev.Events;
using HelloDev.IDs;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Base class for all Quest Graph nodes.
    /// Provides common port definitions and utilities.
    /// </summary>
    /// <remarks>
    /// Also registers custom ScriptableObject types for use as Variables/Constants
    /// in the Blackboard by defining typed data ports.
    /// </remarks>
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

        #region Type Registration Ports

        /// <summary>
        /// Registers custom ScriptableObject types for Blackboard Variables/Constants.
        /// Call this in OnDefinePorts to make these types available.
        /// </summary>
        /// <remarks>
        /// Graph Toolkit automatically discovers supported variable types by scanning
        /// the data types used in node ports. By adding typed ports here, we enable
        /// these types to be used as Variables and Constants in the Blackboard.
        /// </remarks>
        protected void RegisterCustomVariableTypes(IPortDefinitionContext context)
        {
            // Quest System types
            context.AddInputPort<Quest_SO>("_Quest").WithDisplayName("").Build();
            context.AddInputPort<QuestLine_SO>("_QuestLine").WithDisplayName("").Build();
            context.AddInputPort<Task_SO>("_Task").WithDisplayName("").Build();
            context.AddInputPort<QuestType_SO>("_QuestType").WithDisplayName("").Build();
            context.AddInputPort<QuestRewardType_SO>("_RewardType").WithDisplayName("").Build();

            // ID System types
            context.AddInputPort<ID_SO>("_ID").WithDisplayName("").Build();

            // Condition System types
            context.AddInputPort<Condition_SO>("_Condition").WithDisplayName("").Build();
            context.AddInputPort<WorldFlagBase_SO>("_WorldFlag").WithDisplayName("").Build();

            // Event System types
            context.AddInputPort<GameEventBase_SO>("_GameEvent").WithDisplayName("").Build();

            // Unity types (commonly used)
            context.AddInputPort<Sprite>("_Sprite").WithDisplayName("").Build();
            context.AddInputPort<LocalizedString>("_LocalizedString").WithDisplayName("").Build();
        }

        #endregion
    }
}
