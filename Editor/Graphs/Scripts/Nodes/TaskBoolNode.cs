using System;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task node for boolean tasks (TaskBool_SO).
    /// Completes when conditions are met once.
    /// </summary>
    /// <remarks>
    /// Bool tasks are the simplest task type - they have no type-specific fields.
    /// Use for simple objectives like "Talk to NPC" or "Enter Area".
    /// </remarks>
    [Serializable]
    public class TaskBoolNode : TaskTypedNode<TaskBool_SO>
    {
        /// <inheritdoc/>
        public override string TaskTypeName => "Bool";

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            // Bool tasks have no type-specific data
        }
    }
}
