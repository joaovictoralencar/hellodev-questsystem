using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
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
    /// Port design (per documentation standards):
    /// - GroupName: PORT (critical identity, shown in title)
    /// - ExecutionMode: OPTION (mode selector)
    /// - RequiredCount: OPTION (only relevant for OptionalXofY mode)
    /// - FailOnAnyTaskFailure: OPTION + ShowInInspectorOnly (rare runtime flag)
    /// - Then: OUTPUT PORT (StageFlow, connects to StageNode.TaskGroupsInput)
    ///
    /// Use ISubgraphNode referencing TaskGroupGraph for reusable task groups.
    /// Use TaskGroupContextNode for inline task editing within a quest.
    /// </remarks>
    [Serializable]
    public class TaskGroupContextNode : ContextNode
    {
        #region Option Names

        private const string OPT_EXECUTION_MODE = "ExecutionMode";
        private const string OPT_REQUIRED_COUNT = "RequiredCount";
        private const string OPT_FAIL_ON_ANY_TASK_FAILURE = "FailOnAnyTaskFailure";

        #endregion

        #region Port Names

        private const string PORT_GROUP_NAME = "GroupNameInput";

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
        public string GroupName => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_GROUP_NAME, "Task Group");

        /// <summary>
        /// How tasks in this group are executed.
        /// </summary>
        public TaskExecutionMode ExecutionMode => GetOptionValue<TaskExecutionMode>(OPT_EXECUTION_MODE);

        /// <summary>
        /// For OptionalXofY mode: number of tasks that must complete.
        /// </summary>
        public int RequiredCount => GetOptionValue<int>(OPT_REQUIRED_COUNT);

        /// <summary>
        /// If true, the entire group fails if any task fails.
        /// </summary>
        public bool FailOnAnyTaskFailure => GetOptionValue<bool>(OPT_FAIL_ON_ANY_TASK_FAILURE);

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Mode selector - affects behavior
            context.AddOption<TaskExecutionMode>(OPT_EXECUTION_MODE)
                .WithDisplayName("Execution Mode")
                .WithDefaultValue(TaskExecutionMode.Sequential)
                .WithTooltip("How tasks are executed:\n" +
                    "• Sequential: Tasks must be completed in order (top to bottom)\n" +
                    "• Parallel: All tasks active at once, complete in any order\n" +
                    "• AnyOrder: Same as Parallel\n" +
                    "• OptionalXofY: Only RequiredCount tasks need to complete");

            // Only show RequiredCount when OptionalXofY mode is selected
            if (ExecutionMode == TaskExecutionMode.OptionalXofY)
            {
                context.AddOption<int>(OPT_REQUIRED_COUNT)
                    .WithDisplayName("Required Count")
                    .WithDefaultValue(1)
                    .WithTooltip("Number of tasks that must complete");
            }

            // Rare runtime flag - hide in inspector only
            context.AddOption<bool>(OPT_FAIL_ON_ANY_TASK_FAILURE)
                .WithDisplayName("Fail On Any Task Failure")
                .WithDefaultValue(false)
                .WithTooltip("If true, the entire group fails if any task fails")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Critical identity - PORT (visible on node + Node Properties)
            context.AddInputPort<string>(PORT_GROUP_NAME)
                .WithDisplayName("Group Name")
                .Build();

            // Success flow - connects to StageNode.TaskGroupsInput
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
