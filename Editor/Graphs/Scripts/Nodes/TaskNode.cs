using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents an individual Task in a TaskGroup.
    /// References an existing Task_SO asset directly - no enum needed.
    /// The task type is determined by the concrete Task_SO subclass assigned.
    /// </summary>
    /// <remarks>
    /// Design: Task_SO assets are created separately using Unity's CreateAssetMenu.
    /// This node just references them, following the Open/Closed principle.
    /// Adding new task types requires no changes to this class.
    /// </remarks>
    [Serializable]
    public class TaskNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_TASK_ASSET = "TaskAsset";

        #endregion

        #region Properties

        public Task_SO TaskAsset => GetOptionValue<Task_SO>(OPT_TASK_ASSET);

        // Convenience properties that read from the referenced asset
        public string DevName => TaskAsset != null ? TaskAsset.DevName : "No Task Assigned";
        public string TaskTypeName => TaskAsset != null ? TaskAsset.GetType().Name : "None";

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<Task_SO>(OPT_TASK_ASSET)
                .WithDisplayName("Task Asset")
                .WithTooltip("Reference to a Task_SO asset (TaskInt_SO, TaskLocation_SO, etc.)");
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From TaskGroup or previous Task
            context.AddInputPort<TaskFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Output: Next task in sequence
            context.AddOutputPort<TaskFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
