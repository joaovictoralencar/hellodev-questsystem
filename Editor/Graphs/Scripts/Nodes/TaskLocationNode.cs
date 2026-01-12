using System;
using HelloDev.QuestSystem.ScriptableObjects;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task node for location-based tasks (TaskLocation_SO).
    /// Completes when the player enters a specific location.
    /// </summary>
    /// <remarks>
    /// Location tasks have no type-specific fields beyond conditions.
    /// The location is defined through conditions (typically ConditionLocation_SO).
    /// Use for objectives like "Go to the Castle" or "Enter the Cave".
    /// </remarks>
    [Serializable]
    public class TaskLocationNode : TaskTypedNode<TaskLocation_SO>
    {
        /// <inheritdoc/>
        public override string TaskTypeName => "Location";

        /// <inheritdoc/>
        protected override void PopulateTypeSpecificData(InlineTaskData data)
        {
            // Location tasks have no type-specific data
        }
    }
}
