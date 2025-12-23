using System;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Conditions
{
    /// <summary>
    /// An event-driven condition that checks the state of a quest.
    /// Used for quest chains where Quest B requires Quest A to be in a specific state.
    /// Subscribes to QuestManager events to automatically re-evaluate when quest states change.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/Quest System/Conditions/Quest State Condition")]
    public class ConditionQuestState_SO : Condition_SO, IConditionEventDriven
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Quest Reference")]
        [Required("A quest reference is required for this condition to work.")]
#else
        [Header("Quest Reference")]
#endif
        [Tooltip("The quest whose state will be checked.")]
        [SerializeField]
        private Quest_SO questToCheck;

#if ODIN_INSPECTOR
        [TitleGroup("Condition Settings")]
#else
        [Header("Condition Settings")]
#endif
        [Tooltip("The target state to compare against.")]
        [SerializeField]
        private QuestState targetState = QuestState.Completed;

#if ODIN_INSPECTOR
        [TitleGroup("Condition Settings")]
#endif
        [Tooltip("How to compare the quest's current state with the target state.")]
        [SerializeField]
        private QuestStateComparison comparisonType = QuestStateComparison.Equals;

        #endregion

        #region Private Fields

        private Action _onConditionMet;
        private bool _isSubscribed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the quest being checked by this condition.
        /// </summary>
        public Quest_SO QuestToCheck => questToCheck;

        /// <summary>
        /// Gets the target state this condition is checking for.
        /// </summary>
        public QuestState TargetState => targetState;

        /// <summary>
        /// Gets the comparison type used for evaluation.
        /// </summary>
        public QuestStateComparison ComparisonType => comparisonType;

        #endregion

        #region ICondition Implementation

        /// <summary>
        /// Evaluates the condition by checking the quest's current state against the target state.
        /// </summary>
        /// <returns>True if the condition is met, respecting IsInverted.</returns>
        public override bool Evaluate()
        {
            if (questToCheck == null)
            {
                Debug.LogWarning($"[ConditionQuestState_SO] Quest reference is null on '{name}'.");
                return IsInverted;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning($"[ConditionQuestState_SO] QuestManager.Instance is null.");
                return IsInverted;
            }

            QuestState currentState = GetQuestCurrentState();
            bool result = EvaluateComparison(currentState, targetState);

            return IsInverted ? !result : result;
        }

        #endregion

        #region IConditionEventDriven Implementation

        /// <summary>
        /// Subscribes to QuestManager events to be notified when quest states change.
        /// </summary>
        /// <param name="onConditionMet">Callback to invoke when the condition becomes true.</param>
        public void SubscribeToEvent(Action onConditionMet)
        {
            if (_isSubscribed) return;

            _onConditionMet = onConditionMet;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStarted.AddListener(OnQuestStateChanged);
                QuestManager.Instance.QuestCompleted.AddListener(OnQuestStateChanged);
                QuestManager.Instance.QuestFailed.AddListener(OnQuestStateChanged);
                QuestManager.Instance.QuestRestarted.AddListener(OnQuestStateChanged);
                QuestManager.Instance.QuestAdded.AddListener(OnQuestStateChanged);
                _isSubscribed = true;
            }
            else
            {
                Debug.LogWarning($"[ConditionQuestState_SO] Cannot subscribe - QuestManager.Instance is null.");
            }
        }

        /// <summary>
        /// Unsubscribes from QuestManager events.
        /// </summary>
        public void UnsubscribeFromEvent()
        {
            if (!_isSubscribed) return;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStarted.RemoveListener(OnQuestStateChanged);
                QuestManager.Instance.QuestCompleted.RemoveListener(OnQuestStateChanged);
                QuestManager.Instance.QuestFailed.RemoveListener(OnQuestStateChanged);
                QuestManager.Instance.QuestRestarted.RemoveListener(OnQuestStateChanged);
                QuestManager.Instance.QuestAdded.RemoveListener(OnQuestStateChanged);
            }

            _onConditionMet = null;
            _isSubscribed = false;
        }

        /// <summary>
        /// Forces the condition to be fulfilled. For debugging purposes.
        /// Note: This doesn't actually change quest state, just fires the callback if condition would be met.
        /// </summary>
        public void ForceFulfillCondition()
        {
            _onConditionMet?.Invoke();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the current state of the referenced quest from QuestManager.
        /// </summary>
        private QuestState GetQuestCurrentState()
        {
            if (QuestManager.Instance == null || questToCheck == null)
            {
                return QuestState.NotStarted;
            }

            if (QuestManager.Instance.IsQuestCompleted(questToCheck))
            {
                return QuestState.Completed;
            }

            if (QuestManager.Instance.IsQuestFailed(questToCheck))
            {
                return QuestState.Failed;
            }

            if (QuestManager.Instance.IsQuestActive(questToCheck))
            {
                return QuestState.InProgress;
            }

            return QuestState.NotStarted;
        }

        /// <summary>
        /// Evaluates the comparison between current state and target state.
        /// </summary>
        private bool EvaluateComparison(QuestState currentState, QuestState target)
        {
            return comparisonType switch
            {
                QuestStateComparison.Equals => currentState == target,
                QuestStateComparison.NotEquals => currentState != target,
                _ => currentState == target
            };
        }

        /// <summary>
        /// Called when any quest's state changes. Checks if it's the quest we're tracking.
        /// </summary>
        private void OnQuestStateChanged(QuestRuntime quest)
        {
            if (quest == null || questToCheck == null) return;

            // Only process if this event is for the quest we're tracking
            if (quest.QuestData != questToCheck) return;

            // Evaluate and fire callback if condition is now met
            if (Evaluate())
            {
                _onConditionMet?.Invoke();
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void OnScriptableObjectReset()
        {
            UnsubscribeFromEvent();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvent();
        }

        #endregion

        #region Editor Helpers

#if ODIN_INSPECTOR && UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Evaluate Now")]
        [PropertyOrder(100)]
        private void DebugEvaluate()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[ConditionQuestState_SO] Can only evaluate during Play mode.");
                return;
            }

            QuestState currentState = GetQuestCurrentState();
            bool result = Evaluate();
            Debug.Log($"[ConditionQuestState_SO] '{name}': Quest '{questToCheck?.name}' is {currentState}, target is {targetState} ({comparisonType}). Result: {result}");
        }

        [TitleGroup("Debug")]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        [PropertyOrder(101)]
        private string CurrentQuestState => Application.isPlaying && questToCheck != null
            ? GetQuestCurrentState().ToString()
            : "N/A";
#endif

        #endregion
    }

    /// <summary>
    /// Comparison types for quest state conditions.
    /// </summary>
    public enum QuestStateComparison
    {
        /// <summary>Quest must be in the exact target state.</summary>
        Equals,
        /// <summary>Quest must NOT be in the target state.</summary>
        NotEquals
    }
}
