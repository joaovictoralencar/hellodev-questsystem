using System;
using HelloDev.Conditions;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for string-based tasks (TaskString_SO).
    /// Completes when a specific string value is matched.
    /// </summary>
    /// <remarks>
    /// Use for objectives like "Enter Password" or "Say the Magic Word".
    ///
    /// Supports two modes controlled by "Use Task Asset" checkbox:
    /// - Asset Mode (checked): Reference an existing TaskString_SO asset
    /// - Define Mode (unchecked): Define task inline with ports + dynamic condition ports
    /// </remarks>
    [Serializable]
    public class TaskStringBlock : TaskBlockBase
    {
        #region Option Names

        private const string OPT_USE_TASK_ASSET = "UseTaskAsset";
        private const string OPT_STRING_TASK_ASSET = "StringTaskAsset";
        private const string OPT_TRIGGER_CONDITION_COUNT = "TriggerConditionCount";
        private const string OPT_FAILURE_CONDITION_COUNT = "FailureConditionCount";

        #endregion

        #region Port Names

        private const string PORT_TASK_ASSET = "TaskAssetInput";
        private const string PORT_DEV_NAME = "DevNameInput";
        private const string PORT_DISPLAY_NAME = "DisplayNameInput";
        private const string PORT_DESCRIPTION = "DescriptionInput";
        private const string PORT_TARGET_VALUE = "TargetValueInput";
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
        public override Task_SO TaskAsset => UseTaskAsset
            ? GetOptionValue<TaskString_SO>(OPT_STRING_TASK_ASSET)
            : null;

        /// <inheritdoc/>
        public override string TaskTypeName => "String";

        /// <inheritdoc/>
        public override bool HasValidTask
        {
            get
            {
                if (IsAssetMode)
                    return TaskAsset != null;
                var devName = GetOptionValue<string>(OPT_DEV_NAME);
                return !string.IsNullOrWhiteSpace(devName);
            }
        }

        #endregion

        #region Implementation

        /// <inheritdoc/>
        protected override string GetAssetOptionName() => OPT_STRING_TASK_ASSET;

        /// <inheritdoc/>
        protected override void DefineAssetModeOption(IOptionDefinitionContext context)
        {
            context.AddOption<bool>(OPT_USE_TASK_ASSET)
                .WithDisplayName("Use Task Asset")
                .WithDefaultValue(false)
                .WithTooltip("Check to use an existing TaskString_SO asset.\nUncheck to define task inline.");

            if (UseTaskAsset)
            {
                context.AddOption<TaskString_SO>(OPT_STRING_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .WithTooltip("Reference to a TaskString_SO asset");
            }
        }

        /// <inheritdoc/>
        protected override void OnDefineTypeSpecificOptions(IOptionDefinitionContext context)
        {
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
        protected override void AddTypeSpecificPorts(IPortDefinitionContext context)
        {
            if (UseTaskAsset)
            {
                context.AddInputPort<TaskString_SO>(PORT_TASK_ASSET)
                    .WithDisplayName("Task Asset")
                    .Build();
            }
            else
            {
                context.AddInputPort<string>(PORT_DEV_NAME)
                    .WithDisplayName("Dev Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DISPLAY_NAME)
                    .WithDisplayName("Display Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DESCRIPTION)
                    .WithDisplayName("Description")
                    .Build();

                context.AddInputPort<string>(PORT_TARGET_VALUE)
                    .WithDisplayName("Target Value")
                    .Build();

                for (int i = 0; i < TriggerConditionCount; i++)
                {
                    context.AddInputPort<Condition_SO>(PORT_TRIGGER_CONDITION + i)
                        .WithDisplayName($"Trigger Condition {i + 1}")
                        .Build();
                }

                for (int i = 0; i < FailureConditionCount; i++)
                {
                    context.AddInputPort<Condition_SO>(PORT_FAILURE_CONDITION + i)
                        .WithDisplayName($"Fail Condition {i + 1}")
                        .Build();
                }
            }
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            var task = UnityEngine.ScriptableObject.CreateInstance<TaskString_SO>();
            return task;
        }

        #endregion
    }
}
