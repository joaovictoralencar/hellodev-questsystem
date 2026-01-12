using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point node for a TaskGroupGraph. Every TaskGroupGraph should have exactly one.
    /// </summary>
    [Serializable]
    public class TaskGroupStartNode : QuestBaseNode
    {
        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Start nodes only have output - no input
            context.AddOutputPort<TaskFlow>("FirstTask")
                .WithDisplayName("First Task")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
