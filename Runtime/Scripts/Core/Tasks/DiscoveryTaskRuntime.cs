using System.Collections.Generic;
using System.Linq;
using HelloDev.IDs;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// A runtime task that requires discovering/examining specific items or clues.
    /// Used for objectives like "Examine 3 clues" or "Find all the witnesses".
    /// </summary>
    public class DiscoveryTaskRuntime : TaskRuntime
    {
        public override float Progress => RequiredDiscoveries == 0 ? 1f : (float)DiscoveredCount / RequiredDiscoveries;

        private readonly HashSet<ID_SO> _discoveredItems = new();

        /// <summary>
        /// Gets the list of discoverable item IDs for this task.
        /// </summary>
        public List<ID_SO> DiscoverableItems => (Data as TaskDiscovery_SO)?.DiscoverableItems;

        /// <summary>
        /// Gets the number of discoveries required to complete the task.
        /// </summary>
        public int RequiredDiscoveries => (Data as TaskDiscovery_SO)?.RequiredDiscoveries ?? 0;

        /// <summary>
        /// Gets the set of already discovered items.
        /// </summary>
        public IReadOnlyCollection<ID_SO> DiscoveredItems => _discoveredItems;

        /// <summary>
        /// Gets the current number of discovered items.
        /// </summary>
        public int DiscoveredCount => _discoveredItems.Count;

        /// <summary>
        /// Initializes a new instance of the DiscoveryTask class.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        public DiscoveryTaskRuntime(TaskDiscovery_SO data) : base(data)
        {
        }

        /// <summary>
        /// Called when the player discovers/examines an item.
        /// If the item is in the discoverable list and hasn't been discovered yet, it's added.
        /// </summary>
        /// <param name="itemId">The ID of the discovered item.</param>
        /// <returns>True if this was a valid new discovery, false otherwise.</returns>
        public bool OnItemDiscovered(ID_SO itemId)
        {
            if (CurrentState != TaskState.InProgress) return false;
            if (itemId == null) return false;

            // Check if this item is in the discoverable list
            if (DiscoverableItems == null || !DiscoverableItems.Contains(itemId)) return false;

            // Check if already discovered
            if (_discoveredItems.Contains(itemId)) return false;

            _discoveredItems.Add(itemId);
            QuestLogger.Log($"Task '{DevName}' - Discovered '{itemId.DevName}'. Progress: {DiscoveredCount}/{RequiredDiscoveries}");
            OnTaskUpdated?.SafeInvoke(this);
            return true;
        }

        /// <summary>
        /// Checks if a specific item has been discovered.
        /// </summary>
        /// <param name="itemId">The item ID to check.</param>
        /// <returns>True if the item has been discovered.</returns>
        public bool HasDiscovered(ID_SO itemId)
        {
            return _discoveredItems.Contains(itemId);
        }

        public override void ForceCompleteState()
        {
            // Add all required items as discovered
            if (DiscoverableItems != null)
            {
                foreach (var item in DiscoverableItems.Take(RequiredDiscoveries))
                {
                    _discoveredItems.Add(item);
                }
            }
        }

        public override bool OnIncrementStep()
        {
            // For discovery tasks, incrementing discovers the next undiscovered item
            if (CurrentState != TaskState.InProgress) return false;
            if (DiscoverableItems == null || _discoveredItems.Count >= RequiredDiscoveries) return false;

            var nextUndiscovered = DiscoverableItems.FirstOrDefault(item => !_discoveredItems.Contains(item));
            if (nextUndiscovered != null)
            {
                _discoveredItems.Add(nextUndiscovered);
                QuestLogger.Log($"Task '{DevName}' - Manually discovered '{nextUndiscovered.DevName}'. Progress: {DiscoveredCount}/{RequiredDiscoveries}");
                return true;
            }

            return false;
        }

        public override bool OnDecrementStep()
        {
            // For discovery tasks, decrementing removes the last discovered item
            if (CurrentState != TaskState.InProgress) return false;
            if (_discoveredItems.Count == 0) return false;

            var lastDiscovered = _discoveredItems.LastOrDefault();
            if (lastDiscovered != null)
            {
                _discoveredItems.Remove(lastDiscovered);
                QuestLogger.Log($"Task '{DevName}' - Removed discovery '{lastDiscovered.DevName}'. Progress: {DiscoveredCount}/{RequiredDiscoveries}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the task's state and clears all discoveries.
        /// </summary>
        public override void ResetTask()
        {
            base.ResetTask();
            _discoveredItems.Clear();
            OnTaskUpdated?.SafeInvoke(this);
        }

        protected override void CheckCompletion(TaskRuntime task)
        {
            if (_discoveredItems.Count >= RequiredDiscoveries)
            {
                CompleteTask();
            }
        }
    }
}
