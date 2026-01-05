using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point node for a StageGraph. Every StageGraph should have exactly one.
    /// </summary>
    [Serializable]
    public class StageStartNode : QuestBaseNode
    {
        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Start nodes only have output - no input
            context.AddOutputPort<StageFlow>("FirstTaskGroup")
                .WithDisplayName("First Task Group")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
