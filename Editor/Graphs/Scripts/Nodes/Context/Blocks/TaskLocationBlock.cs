using System;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Task block for location-based tasks (TaskLocation_SO).
    /// Completes when the player enters a specific location.
    /// </summary>
    /// <remarks>
    /// Location tasks have no type-specific fields beyond conditions.
    /// The location is defined through conditions (typically ConditionLocation_SO).
    /// Use for objectives like "Go to the Castle" or "Enter the Cave".
    /// </remarks>
    [Serializable]
    public class TaskLocationBlock : TaskTypedBlock<TaskLocation_SO>
    {
        /// <inheritdoc/>
        public override string TaskTypeName => "Location";

        /// <inheritdoc/>
        protected override string TaskAssetOptionName => "LocationTaskAsset";

        /// <inheritdoc/>
        protected override string TaskAssetTooltipType => "TaskLocation_SO";

        /// <inheritdoc/>
        public override Task_SO CreateTaskAsset()
        {
            return ScriptableObject.CreateInstance<TaskLocation_SO>();
        }
    }
}
