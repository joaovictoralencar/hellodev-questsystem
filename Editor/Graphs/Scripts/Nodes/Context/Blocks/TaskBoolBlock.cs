using System;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for boolean-based tasks (TaskBool_SO).
    /// Completes when conditions are met once.
    /// </summary>
    /// <remarks>
    /// Use for simple objectives like "Talk to NPC" or "Enter Area".
    /// No type-specific fields - inherits all from TaskTypedBlock.
    /// </remarks>
    [Serializable]
    public class TaskBoolBlock : TaskTypedBlock<TaskBool_SO>
    {
        /// <inheritdoc/>
        public override string TaskTypeName => "Bool";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "BoolTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskBool_SO";

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            // TaskBool_SO has no type-specific fields
            return CreateTaskAssetWithCommonFields<TaskBool_SO>();
        }
    }
}
