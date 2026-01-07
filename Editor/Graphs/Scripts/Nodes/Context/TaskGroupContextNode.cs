using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Context node that contains task blocks as a visual container.
    /// Provides inline task editing via draggable blocks.
    /// </summary>
    /// <remarks>
    /// Benefits of the Context/Block pattern:
    /// - Tasks can be reordered via drag-and-drop inside the context
    /// - Shared settings apply to all contained task blocks
    /// - Type constraints prevent adding incompatible blocks
    /// - Cleaner visual grouping than separate nodes with wires
    ///
    /// Use ISubgraphNode referencing TaskGroupGraph for reusable task groups.
    /// Use TaskGroupContextNode for inline task editing within a quest.
    /// </remarks>
    [Serializable]
    public class TaskGroupContextNode : ContextNode
    {
        #region Option Names

        private const string OPT_GROUP_NAME = "GroupName";
        private const string OPT_EXECUTION_MODE = "ExecutionMode";
        private const string OPT_REQUIRED_COUNT = "RequiredCount";
        private const string OPT_FAIL_ON_ANY_TASK_FAILURE = "FailOnAnyTaskFailure";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the value of a node option by name.
        /// </summary>
        protected T GetOptionValue<T>(string optionName)
        {
            var option = GetNodeOptionByName(optionName);
            if (option != null && option.TryGetValue<T>(out var value))
                return value;
            return default;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Developer-friendly name for this task group.
        /// </summary>
        public string GroupName => GetOptionValue<string>(OPT_GROUP_NAME);

        /// <summary>
        /// How tasks in this group are executed.
        /// </summary>
        public TaskExecutionMode ExecutionMode => GetOptionValue<TaskExecutionMode>(OPT_EXECUTION_MODE);

        /// <summary>
        /// For OptionalXofY mode: number of tasks that must complete.
        /// </summary>
        public int RequiredCount => GetOptionValue<int>(OPT_REQUIRED_COUNT);

        /// <summary>
        /// If true, the group fails if any task fails.
        /// </summary>
        public bool FailOnAnyTaskFailure => GetOptionValue<bool>(OPT_FAIL_ON_ANY_TASK_FAILURE);

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
                    "• Sequential: Tasks must be completed in order (top to bottom)\n" +
                    "• Parallel: All tasks active at once, complete in any order\n" +
                    "• AnyOrder: Same as Parallel\n" +
                    "• OptionalXofY: Only RequiredCount tasks need to complete");

            context.AddOption<int>(OPT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .WithDefaultValue(1)
                .WithTooltip("For OptionalXofY mode: number of tasks that must complete");

            context.AddOption<bool>(OPT_FAIL_ON_ANY_TASK_FAILURE)
                .WithDisplayName("Fail On Any Task Failure")
                .WithDefaultValue(false)
                .WithTooltip("If true, the entire group fails if any task fails");
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

            // Success flow - all required tasks complete
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Failure flow - group failed
            context.AddOutputPort<StageFlow>("Else")
                .WithDisplayName("Else (Failed)")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
