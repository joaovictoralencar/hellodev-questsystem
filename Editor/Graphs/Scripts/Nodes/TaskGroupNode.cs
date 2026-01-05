using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a TaskGroup within a Stage.
    /// Can reference a TaskGroupGraph subgraph for reusability.
    /// </summary>
    [Serializable]
    public class TaskGroupNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_GROUP_NAME = "GroupName";
        private const string OPT_EXECUTION_MODE = "ExecutionMode";
        private const string OPT_REQUIRED_COUNT = "RequiredCount";
        private const string OPT_SUBGRAPH = "Subgraph";

        #endregion

        #region Properties

        public string GroupName => GetOptionValue<string>(OPT_GROUP_NAME);
        public TaskExecutionMode ExecutionMode => GetOptionValue<TaskExecutionMode>(OPT_EXECUTION_MODE);
        public int RequiredCount => GetOptionValue<int>(OPT_REQUIRED_COUNT);
        public TaskGroupGraph Subgraph => GetOptionValue<TaskGroupGraph>(OPT_SUBGRAPH);

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_GROUP_NAME)
                .WithDisplayName("Group Name")
                .WithDefaultValue("Task Group")
                .WithTooltip("Developer-friendly name for this task group")
                .Delayed();

            context.AddOption<TaskExecutionMode>(OPT_EXECUTION_MODE)
                .WithDisplayName("Execution Mode")
                .WithDefaultValue(TaskExecutionMode.Sequential)
                .WithTooltip("How tasks are executed:\n" +
                    "• Sequential: Chain tasks with Then→In (flow)\n" +
                    "• Parallel/AnyOrder: Connect ALL tasks directly from Tasks port (tree/fork)\n" +
                    "• OptionalXofY: Same as Parallel, set RequiredCount");

            context.AddOption<int>(OPT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .WithDefaultValue(1)
                .WithTooltip("For OptionalXofY mode: number of tasks that must complete")
                .ShowInInspectorOnly();

            context.AddOption<TaskGroupGraph>(OPT_SUBGRAPH)
                .WithDisplayName("Subgraph")
                .WithTooltip("Optional reference to a reusable TaskGroup subgraph")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From Stage")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Tasks output - connect to inline TaskNodes (alternative to subgraph)
            context.AddOutputPort<TaskFlow>("Tasks")
                .WithDisplayName("Tasks")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Success flow - all tasks complete
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Failure flow - group failed
            context.AddOutputPort<StageFlow>("Else")
                .WithDisplayName("Else")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
