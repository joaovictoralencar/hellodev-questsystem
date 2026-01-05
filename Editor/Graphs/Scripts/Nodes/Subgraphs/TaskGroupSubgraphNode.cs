using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that references a TaskGroupGraph subgraph.
    /// Used in StageGraph to embed reusable task group definitions.
    /// </summary>
    /// <remarks>
    /// This is the most commonly reused subgraph type. Examples:
    /// - "Kill 10 Goblins" task group used in multiple quests
    /// - "Collect Evidence" pattern reused across investigation stages
    /// - "Talk to NPC" interaction shared between quests
    ///
    /// Optional overrides allow customization without modifying the subgraph:
    /// - Override group name for context-specific display
    /// - Override execution mode for different completion logic
    /// - Override required count for X of Y variations
    /// </remarks>
    [Serializable]
    public class TaskGroupSubgraphNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_TASKGROUP_SUBGRAPH = "TaskGroupSubgraph";
        private const string OPT_OVERRIDE_GROUP_NAME = "OverrideGroupName";
        private const string OPT_USE_OVERRIDE_MODE = "UseOverrideExecutionMode";
        private const string OPT_OVERRIDE_EXECUTION_MODE = "OverrideExecutionMode";
        private const string OPT_OVERRIDE_REQUIRED_COUNT = "OverrideRequiredCount";

        #endregion

        #region Properties

        public TaskGroupGraph TaskGroupSubgraph => GetOptionValue<TaskGroupGraph>(OPT_TASKGROUP_SUBGRAPH);

        public string EffectiveGroupName
        {
            get
            {
                var overrideName = GetOptionValue<string>(OPT_OVERRIDE_GROUP_NAME);
                if (!string.IsNullOrEmpty(overrideName))
                    return overrideName;
                return TaskGroupSubgraph?.GroupName ?? "Task Group";
            }
        }

        public TaskExecutionMode EffectiveExecutionMode
        {
            get
            {
                if (GetOptionValue<bool>(OPT_USE_OVERRIDE_MODE))
                    return GetOptionValue<TaskExecutionMode>(OPT_OVERRIDE_EXECUTION_MODE);
                return TaskGroupSubgraph?.ExecutionMode ?? TaskExecutionMode.Sequential;
            }
        }

        public int EffectiveRequiredCount
        {
            get
            {
                var overrideCount = GetOptionValue<int>(OPT_OVERRIDE_REQUIRED_COUNT);
                if (overrideCount >= 0)
                    return overrideCount;
                return TaskGroupSubgraph?.RequiredCount ?? 1;
            }
        }

        public string DisplayName => TaskGroupSubgraph != null
            ? $"[TaskGroup] {EffectiveGroupName}"
            : "[TaskGroup] Empty Reference";

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<TaskGroupGraph>(OPT_TASKGROUP_SUBGRAPH)
                .WithDisplayName("TaskGroup Subgraph")
                .WithTooltip("Reference to a reusable TaskGroupGraph asset");

            context.AddOption<string>(OPT_OVERRIDE_GROUP_NAME)
                .WithDisplayName("Override Group Name")
                .WithTooltip("Override the subgraph's group name (empty = use subgraph value)")
                .ShowInInspectorOnly();

            context.AddOption<bool>(OPT_USE_OVERRIDE_MODE)
                .WithDisplayName("Use Override Execution Mode")
                .WithDefaultValue(false)
                .WithTooltip("If true, uses the override execution mode instead of subgraph value")
                .ShowInInspectorOnly();

            context.AddOption<TaskExecutionMode>(OPT_OVERRIDE_EXECUTION_MODE)
                .WithDisplayName("Override Execution Mode")
                .WithDefaultValue(TaskExecutionMode.Sequential)
                .WithTooltip("Override execution mode (only used if 'Use Override' is checked)")
                .ShowInInspectorOnly();

            context.AddOption<int>(OPT_OVERRIDE_REQUIRED_COUNT)
                .WithDisplayName("Override Required Count")
                .WithDefaultValue(-1)
                .WithTooltip("Override required count for OptionalXofY mode (-1 = use subgraph value)")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage (or previous TaskGroup in sequence)
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From Stage")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Tasks output - connect to inline TaskNodes (alternative to subgraph)
            context.AddOutputPort<TaskFlow>("Tasks")
                .WithDisplayName("Tasks")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Success flow - all tasks in group completed
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
