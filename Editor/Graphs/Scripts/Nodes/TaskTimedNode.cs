using System;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task node for timed tasks (TaskTimed_SO).
    /// Must be completed within a time limit.
    /// </summary>
    /// <remarks>
    /// Timed tasks have a countdown timer.
    /// Can optionally fail the entire quest when time expires.
    /// Use for objectives like "Escape Within 2 Minutes" or "Defuse the Bomb".
    ///
    /// Supports two modes controlled by "Use Task Asset" checkbox:
    /// - Asset Mode (checked): Shows Task Asset input port
    /// - Define Mode (unchecked): Shows inline data ports + dynamic condition ports
    /// </remarks>
    [Serializable]
    public class TaskTimedNode : TaskBaseNode
    {
        #region Option Names

        private const string OPT_USE_TASK_ASSET = "UseTaskAsset";
        private const string OPT_TRIGGER_CONDITION_COUNT = "TriggerConditionCount";
        private const string OPT_FAILURE_CONDITION_COUNT = "FailureConditionCount";

        #endregion

        #region Port Names

        private const string PORT_TASK_ASSET = "TaskAssetInput";
        private const string PORT_DEV_NAME = "DevNameInput";
        private const string PORT_DISPLAY_NAME = "DisplayNameInput";
        private const string PORT_DESCRIPTION = "DescriptionInput";
        private const string PORT_TIME_LIMIT = "TimeLimitInput";
        private const string PORT_FAIL_ON_EXPIRE = "FailOnExpireInput";
        private const string PORT_TRIGGER_CONDITION = "TriggerConditionInput";
        private const string PORT_FAILURE_CONDITION = "FailureConditionInput";

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use an existing Task Asset (true) or define inline (false).
        /// </summary>
        public bool UseTaskAsset => GetOptionValue<bool>(OPT_USE_TASK_ASSET);

        /// <summary>
        /// Number of trigger condition ports to show.
        /// </summary>
        public int TriggerConditionCount => GetOptionValue<int>(OPT_TRIGGER_CONDITION_COUNT);

        /// <summary>
        /// Number of failure condition ports to show.
        /// </summary>
        public int FailureConditionCount => GetOptionValue<int>(OPT_FAILURE_CONDITION_COUNT);

        /// <inheritdoc/>
        public override Task_SO TaskAsset
        {
            get
            {
                if (!UseTaskAsset)
                    return null;
                return GraphTraversalUtility.ResolveDataPort<TaskTimed_SO>(this, PORT_TASK_ASSET, null);
            }
        }

        /// <inheritdoc/>
        public override string TaskTypeName => "Timed";

        /// <summary>
        /// Time limit in seconds from port (Define mode only).
        /// </summary>
        public float TimeLimit => GraphTraversalUtility.ResolveDataPort<float>(this, PORT_TIME_LIMIT, 120f);

        /// <summary>
        /// Whether expiring fails the entire quest from port (Define mode only).
        /// </summary>
        public bool FailQuestOnExpire => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_FAIL_ON_EXPIRE, false);

        /// <summary>
        /// The dev name from port (Define mode only).
        /// </summary>
        public string PortDevName => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_DEV_NAME, "New Task");

        /// <summary>
        /// Resolves the time limit from the appropriate source.
        /// </summary>
        public float ResolveTimeLimit()
        {
            if (IsAssetMode && TaskAsset is TaskTimed_SO timedTask)
            {
                return timedTask.TimeLimit;
            }
            return TimeLimit;
        }

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
                    return !string.IsNullOrWhiteSpace(PortDevName) && TimeLimit >= 1f;
                }
            }
        }

        #endregion

        #region Port Definition

        /// <inheritdoc/>
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Flow ports
            context.AddInputPort<TaskFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<TaskFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            if (UseTaskAsset)
            {
                // Asset Mode: Show Task Asset input port only
                context.AddInputPort<TaskTimed_SO>(PORT_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .Build();
            }
            else
            {
                // Define Mode: Show inline data ports
                context.AddInputPort<string>(PORT_DEV_NAME)
                    .WithDisplayName("Dev Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DISPLAY_NAME)
                    .WithDisplayName("Display Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DESCRIPTION)
                    .WithDisplayName("Description")
                    .Build();

                context.AddInputPort<float>(PORT_TIME_LIMIT)
                    .WithDisplayName("Time Limit (s)")
                    .Build();

                context.AddInputPort<bool>(PORT_FAIL_ON_EXPIRE)
                    .WithDisplayName("Fail Quest On Expire")
                    .Build();

                // Dynamic trigger condition ports
                for (int i = 0; i < TriggerConditionCount; i++)
                {
                    context.AddInputPort<Condition_SO>(PORT_TRIGGER_CONDITION + i)
                        .WithDisplayName($"Trigger Condition {i + 1}")
                        .Build();
                }

                // Dynamic failure condition ports
                for (int i = 0; i < FailureConditionCount; i++)
                {
                    context.AddInputPort<Condition_SO>(PORT_FAILURE_CONDITION + i)
                        .WithDisplayName($"Fail Condition {i + 1}")
                        .Build();
                }
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(OPT_USE_TASK_ASSET)
                .WithDisplayName("Use Task Asset")
                .WithDefaultValue(false)
                .WithTooltip("Check to use an existing TaskTimed_SO asset.\nUncheck to define task inline.");

            // Only show count options in Define mode
            if (!UseTaskAsset)
            {
                context.AddOption<int>(OPT_TRIGGER_CONDITION_COUNT)
                    .WithDisplayName("Trigger Condition Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of trigger condition ports to show.");

                context.AddOption<int>(OPT_FAILURE_CONDITION_COUNT)
                    .WithDisplayName("Failure Condition Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of failure condition ports to show.");
            }
        }

        /// <inheritdoc/>
        protected override void DefineAssetModeOptions(IOptionDefinitionContext context) { }

        /// <inheritdoc/>
        protected override void OnDefineTypeSpecificOptions(IOptionDefinitionContext context) { }

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            data.timeLimit = ResolveTimeLimit();
            data.failQuestOnExpire = FailQuestOnExpire;
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            return InlineData.CreateTaskAsset<TaskTimed_SO>();
        }

        #endregion
    }
}
