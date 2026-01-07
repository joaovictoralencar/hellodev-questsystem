using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Legacy task node that supports any task type.
    /// DEPRECATED: Use type-specific nodes (TaskBoolNode, TaskIntNode, etc.) for new graphs.
    /// </summary>
    /// <remarks>
    /// This class is kept for backward compatibility with existing graph files.
    /// New graphs should use:
    /// - TaskBoolNode for boolean tasks
    /// - TaskIntNode for count-based tasks
    /// - TaskStringNode for string-match tasks
    /// - TaskLocationNode for location tasks
    /// - TaskDiscoveryNode for discovery tasks
    /// - TaskTimedNode for timed tasks
    /// </remarks>
    [Obsolete("Use type-specific nodes (TaskBoolNode, TaskIntNode, etc.) instead.")]
    [Serializable]
    public class TaskNode : TaskBaseNode
    {
        #region Option Names

        private const string OPT_LEGACY_TASK_ASSET = "TaskAsset";
        private const string OPT_TASK_TYPE = "TaskType";

        // Type-specific inline options
        private const string OPT_REQUIRED_COUNT = "RequiredCount";
        private const string OPT_TARGET_VALUE = "TargetValue";
        private const string OPT_REQUIRED_DISCOVERIES = "RequiredDiscoveries";
        private const string OPT_TIME_LIMIT = "TimeLimit";
        private const string OPT_FAIL_QUEST_ON_EXPIRE = "FailQuestOnExpire";

        #endregion

        #region Task Type Enum (Legacy)

        /// <summary>
        /// Legacy enum for selecting task type in the unified TaskNode.
        /// </summary>
        public enum LegacyTaskType
        {
            Bool = 0,
            Int = 1,
            String = 2,
            Location = 3,
            Discovery = 4,
            Timed = 5
        }

        #endregion

        #region Properties

        /// <summary>
        /// The type of task (legacy enum selection).
        /// </summary>
        public LegacyTaskType SelectedTaskType => GetOptionValue<LegacyTaskType>(OPT_TASK_TYPE);

        /// <inheritdoc/>
        public override Task_SO TaskAsset => GetOptionValue<Task_SO>(OPT_LEGACY_TASK_ASSET);

        /// <inheritdoc/>
        public override string TaskTypeName => SelectedTaskType.ToString();

        /// <summary>
        /// Required count for Int tasks.
        /// </summary>
        public int RequiredCount => GetOptionValue<int>(OPT_REQUIRED_COUNT);

        /// <summary>
        /// Target value for String tasks.
        /// </summary>
        public string TargetValue => GetOptionValue<string>(OPT_TARGET_VALUE) ?? "";

        /// <summary>
        /// Required discoveries for Discovery tasks.
        /// </summary>
        public int RequiredDiscoveries => GetOptionValue<int>(OPT_REQUIRED_DISCOVERIES);

        /// <summary>
        /// Time limit for Timed tasks.
        /// </summary>
        public float TimeLimit => GetOptionValue<float>(OPT_TIME_LIMIT);

        /// <summary>
        /// Whether timer expiry fails the quest for Timed tasks.
        /// </summary>
        public bool FailQuestOnExpire => GetOptionValue<bool>(OPT_FAIL_QUEST_ON_EXPIRE);

        /// <inheritdoc/>
        public override bool HasValidTask
        {
            get
            {
                if (IsAssetMode)
                {
                    return TaskAsset != null;
                }
                else
                {
                    var devName = GetOptionValue<string>(OPT_DEV_NAME);
                    if (string.IsNullOrWhiteSpace(devName))
                        return false;

                    switch (SelectedTaskType)
                    {
                        case LegacyTaskType.Int:
                            return RequiredCount >= 1;
                        case LegacyTaskType.Timed:
                            return TimeLimit >= 1f;
                        default:
                            return true;
                    }
                }
            }
        }

        #endregion

        #region Abstract Implementation

        /// <inheritdoc/>
        protected override void DefineAssetModeOptions(IOptionDefinitionContext context)
        {
            context.AddOption<Task_SO>(OPT_LEGACY_TASK_ASSET)
                .WithDisplayName("Task Asset")
                .WithTooltip("Reference to a Task_SO asset (Asset mode only)");
        }

        /// <inheritdoc/>
        protected override void OnDefineTypeSpecificOptions(IOptionDefinitionContext context)
        {
            // Task type selector for Define mode
            context.AddOption<LegacyTaskType>(OPT_TASK_TYPE)
                .WithDisplayName("Task Type")
                .WithDefaultValue(LegacyTaskType.Bool)
                .WithTooltip("The type of task to create (Define mode only)")
                .ShowInInspectorOnly();

            // Int task options
            context.AddOption<int>(OPT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .WithDefaultValue(1)
                .WithTooltip("Number of times conditions must be fulfilled (Int task only)")
                .ShowInInspectorOnly();

            // String task options
            context.AddOption<string>(OPT_TARGET_VALUE)
                .WithDisplayName("Target Value")
                .WithDefaultValue("")
                .WithTooltip("The target string value to match (String task only)")
                .ShowInInspectorOnly()
                .Delayed();

            // Discovery task options
            context.AddOption<int>(OPT_REQUIRED_DISCOVERIES)
                .WithDisplayName("Required Discoveries")
                .WithDefaultValue(0)
                .WithTooltip("Number of conditions to fulfill. 0 = all (Discovery task only)")
                .ShowInInspectorOnly();

            // Timed task options
            context.AddOption<float>(OPT_TIME_LIMIT)
                .WithDisplayName("Time Limit (seconds)")
                .WithDefaultValue(120f)
                .WithTooltip("Time limit in seconds (Timed task only)")
                .ShowInInspectorOnly();

            context.AddOption<bool>(OPT_FAIL_QUEST_ON_EXPIRE)
                .WithDisplayName("Fail Quest On Expire")
                .WithDefaultValue(false)
                .WithTooltip("If true, timer expiring fails the entire quest (Timed task only)")
                .ShowInInspectorOnly();
        }

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            switch (SelectedTaskType)
            {
                case LegacyTaskType.Int:
                    data.requiredCount = RequiredCount;
                    break;
                case LegacyTaskType.String:
                    data.targetValue = TargetValue;
                    break;
                case LegacyTaskType.Discovery:
                    data.requiredDiscoveries = RequiredDiscoveries;
                    break;
                case LegacyTaskType.Timed:
                    data.timeLimit = TimeLimit;
                    data.failQuestOnExpire = FailQuestOnExpire;
                    break;
            }
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            var data = InlineData;

            return SelectedTaskType switch
            {
                LegacyTaskType.Bool => data.CreateTaskAsset<TaskBool_SO>(),
                LegacyTaskType.Int => data.CreateTaskAsset<TaskInt_SO>(),
                LegacyTaskType.String => data.CreateTaskAsset<TaskString_SO>(),
                LegacyTaskType.Location => data.CreateTaskAsset<TaskLocation_SO>(),
                LegacyTaskType.Discovery => data.CreateTaskAsset<TaskDiscovery_SO>(),
                LegacyTaskType.Timed => data.CreateTaskAsset<TaskTimed_SO>(),
                _ => data.CreateTaskAsset<TaskBool_SO>()
            };
        }

        #endregion
    }
}
