using System;
using HelloDev.Conditions;
using UnityEngine;

namespace HelloDev.QuestSystem.Conditions
{
    /// <summary>
    /// Abstract base class for event-driven conditions that subscribe to QuestManager events.
    /// Provides common infrastructure for callback management and subscription lifecycle.
    /// </summary>
    /// <remarks>
    /// Subclasses implement:
    /// - SubscribeToManagerEvents() - which QuestManager events to listen to
    /// - UnsubscribeFromManagerEvents() - cleanup of those events
    /// - EvaluateCondition() - the actual evaluation logic (called by Evaluate())
    /// - GetConditionDisplayName() - for debug logging
    /// </remarks>
    public abstract class EventDrivenQuestCondition_SO : Condition_SO, IConditionEventDriven
    {
        #region Private Fields

        /// <summary>
        /// Multicast delegate for all registered callbacks.
        /// </summary>
        protected Action OnConditionMetCallback;

        /// <summary>
        /// Number of active subscribers.
        /// </summary>
        private int _subscriberCount;

        /// <summary>
        /// Whether we're subscribed to QuestManager events.
        /// </summary>
        private bool _isSubscribedToEvents;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if currently subscribed to QuestManager events.
        /// </summary>
        protected bool IsSubscribedToEvents => _isSubscribedToEvents;

        #endregion

        #region Abstract Members

        /// <summary>
        /// Subscribes to the appropriate QuestManager events.
        /// Called when the first subscriber registers.
        /// </summary>
        protected abstract void SubscribeToManagerEvents();

        /// <summary>
        /// Unsubscribes from the QuestManager events.
        /// Called when the last subscriber unregisters or on cleanup.
        /// </summary>
        protected abstract void UnsubscribeFromManagerEvents();

        /// <summary>
        /// Performs the actual condition evaluation logic.
        /// </summary>
        /// <returns>True if the condition is met (before IsInverted is applied).</returns>
        protected abstract bool EvaluateCondition();

        /// <summary>
        /// Gets the display name for debug logging.
        /// </summary>
        protected abstract string GetConditionDisplayName();

        #endregion

        #region ICondition Implementation

        /// <summary>
        /// Evaluates the condition, applying IsInverted if set.
        /// </summary>
        public sealed override bool Evaluate()
        {
            bool result = EvaluateCondition();
            return IsInverted ? !result : result;
        }

        #endregion

        #region IConditionEventDriven Implementation

        /// <summary>
        /// Subscribes to events to be notified when the condition state may have changed.
        /// Multiple subscribers can register callbacks.
        /// </summary>
        public void SubscribeToEvent(Action onConditionMet)
        {
            if (onConditionMet == null) return;

            OnConditionMetCallback += onConditionMet;
            _subscriberCount++;

            if (!_isSubscribedToEvents)
            {
                if (QuestManager.Instance != null)
                {
                    SubscribeToManagerEvents();
                    _isSubscribedToEvents = true;
                }
                else
                {
                    Debug.LogWarning($"[{GetConditionDisplayName()}] Cannot subscribe - QuestManager.Instance is null.");
                }
            }
        }

        /// <summary>
        /// Unsubscribes a specific callback.
        /// </summary>
        public void UnsubscribeFromEvent(Action callback)
        {
            if (callback == null) return;

            OnConditionMetCallback -= callback;
            _subscriberCount = Math.Max(0, _subscriberCount - 1);

            if (_subscriberCount == 0 && _isSubscribedToEvents)
            {
                if (QuestManager.Instance != null)
                {
                    UnsubscribeFromManagerEvents();
                }
                _isSubscribedToEvents = false;
            }
        }

        /// <summary>
        /// Forces the condition callback to fire. For debugging purposes.
        /// </summary>
        public void ForceFulfillCondition()
        {
            OnConditionMetCallback?.Invoke();
        }

        #endregion

        #region Protected Helpers

        /// <summary>
        /// Called by subclasses when a manager event fires for the tracked entity.
        /// Evaluates the condition and fires callback if met.
        /// </summary>
        protected void OnTrackedEntityChanged()
        {
            if (Evaluate())
            {
                OnConditionMetCallback?.Invoke();
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void OnScriptableObjectReset()
        {
            ClearAllSubscriptions();
        }

        protected virtual void OnDestroy()
        {
            ClearAllSubscriptions();
        }

        /// <summary>
        /// Clears all subscriptions and resets state.
        /// </summary>
        private void ClearAllSubscriptions()
        {
            if (_isSubscribedToEvents)
            {
                if (QuestManager.Instance != null)
                {
                    UnsubscribeFromManagerEvents();
                }
                _isSubscribedToEvents = false;
            }

            OnConditionMetCallback = null;
            _subscriberCount = 0;
        }

        #endregion
    }
}
