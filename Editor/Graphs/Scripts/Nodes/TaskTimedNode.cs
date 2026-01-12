using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;

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
    /// </remarks>
    [Serializable]
    public class TaskTimedNode : TaskTypedNode<TaskTimed_SO>
    {
        private const string PORT_TIME_LIMIT = "TimeLimitInput";
        private const string PORT_FAIL_ON_EXPIRE = "FailOnExpireInput";

        /// <inheritdoc/>
        public override string TaskTypeName => "Timed";

        /// <summary>
        /// Time limit in seconds (Define mode only).
        /// </summary>
        public float TimeLimit => GraphTraversalUtility.ResolveDataPort<float>(this, PORT_TIME_LIMIT, 120f);

        /// <summary>
        /// Whether expiring fails the entire quest (Define mode only).
        /// </summary>
        public bool FailQuestOnExpire => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_FAIL_ON_EXPIRE, false);

        /// <summary>
        /// Resolves the time limit from the appropriate source.
        /// </summary>
        public float ResolveTimeLimit()
        {
            if (IsAssetMode && TaskAsset is TaskTimed_SO timedTask)
                return timedTask.TimeLimit;
            return TimeLimit;
        }

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
        protected override bool ValidateTypeSpecificFields() => TimeLimit >= 1f;

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            data.timeLimit = ResolveTimeLimit();
            data.failQuestOnExpire = FailQuestOnExpire;
        }
    }
}
