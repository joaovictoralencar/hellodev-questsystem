using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for timed tasks (TaskTimed_SO).
    /// Must be completed within a time limit.
    /// </summary>
    /// <remarks>
    /// Use for objectives like "Escape Within 2 Minutes" or "Defuse the Bomb".
    /// Type-specific: TimeLimit and FailOnExpire ports.
    /// </remarks>
    [Serializable]
    public class TaskTimedBlock : TaskTypedBlock<TaskTimed_SO>
    {
        private const string PORT_TIME_LIMIT = "TimeLimitInput";
        private const string PORT_FAIL_ON_EXPIRE = "FailOnExpireInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Timed";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "TimedTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskTimed_SO";

        /// <summary>
        /// Gets the time limit from the port.
        /// </summary>
        public float TimeLimit => GraphTraversalUtility.ResolveDataPort<float>(this, PORT_TIME_LIMIT, 60f);

        /// <summary>
        /// Gets whether the quest should fail when time expires.
        /// </summary>
        public bool FailQuestOnExpire => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_FAIL_ON_EXPIRE, false);

        /// <inheritdoc/>
        protected override void DefineTypeSpecificPorts(IPortDefinitionContext context)
        {
            context.AddInputPort<float>(PORT_TIME_LIMIT)
                .WithDisplayName("Time Limit (s)")
                .Build();

            context.AddInputPort<bool>(PORT_FAIL_ON_EXPIRE)
                .WithDisplayName("Fail Quest On Expire")
                .Build();
        }

        /// <inheritdoc/>
        protected override bool ValidateTypeSpecificFields()
        {
            return TimeLimit >= 1f;
        }

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            return ScriptableObject.CreateInstance<TaskTimed_SO>();
        }
    }
}
