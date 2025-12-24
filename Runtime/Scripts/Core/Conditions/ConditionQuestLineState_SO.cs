using System;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Conditions
{
    /// <summary>
    /// An event-driven condition that checks the state of a questline.
    /// Used for prerequisites where content unlocks after completing a questline.
    /// Subscribes to QuestManager events to automatically re-evaluate when questline states change.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/Quest System/Conditions/Quest Line State Condition")]
    public class ConditionQuestLineState_SO : Condition_SO, IConditionEventDriven
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("QuestLine Reference")]
        [Required("A questline reference is required for this condition to work.")]
#else
        [Header("QuestLine Reference")]
#endif
        [Tooltip("The questline whose state will be checked.")]
        [SerializeField]
        private QuestLine_SO questLineToCheck;

#if ODIN_INSPECTOR
        [TitleGroup("Condition Settings")]
#else
        [Header("Condition Settings")]
#endif
        [Tooltip("The target state to compare against.")]
        [SerializeField]
        private QuestLineState targetState = QuestLineState.Completed;

#if ODIN_INSPECTOR
        [TitleGroup("Condition Settings")]
#endif
        [Tooltip("How to compare the questline's current state with the target state.")]
        [SerializeField]
        private QuestLineStateComparison comparisonType = QuestLineStateComparison.Equals;

        #endregion

        #region Private Fields

        private Action _onConditionMet;
        private bool _isSubscribed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the questline being checked by this condition.
        /// </summary>
        public QuestLine_SO QuestLineToCheck => questLineToCheck;

        /// <summary>
        /// Gets the target state this condition is checking for.
        /// </summary>
        public QuestLineState TargetState => targetState;

        /// <summary>
        /// Gets the comparison type used for evaluation.
        /// </summary>
        public QuestLineStateComparison ComparisonType => comparisonType;

        #endregion

        #region ICondition Implementation

        /// <summary>
        /// Evaluates the condition by checking the questline's current state against the target state.
        /// </summary>
        /// <returns>True if the condition is met, respecting IsInverted.</returns>
        public override bool Evaluate()
        {
            if (questLineToCheck == null)
            {
                Debug.LogWarning($"[ConditionQuestLineState_SO] QuestLine reference is null on '{name}'.");
                return IsInverted;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning($"[ConditionQuestLineState_SO] QuestManager.Instance is null.");
                return IsInverted;
            }

            QuestLineState currentState = GetQuestLineCurrentState();
            bool result = EvaluateComparison(currentState, targetState);

            return IsInverted ? !result : result;
        }

        #endregion

        #region IConditionEventDriven Implementation

        /// <summary>
        /// Subscribes to QuestManager events to be notified when questline states change.
        /// </summary>
        /// <param name="onConditionMet">Callback to invoke when the condition becomes true.</param>
        public void SubscribeToEvent(Action onConditionMet)
        {
            if (_isSubscribed) return;

            _onConditionMet = onConditionMet;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestLineStarted.AddListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineCompleted.AddListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineUpdated.AddListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineFailed.AddListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineAdded.AddListener(OnQuestLineStateChanged);
                _isSubscribed = true;
            }
            else
            {
                Debug.LogWarning($"[ConditionQuestLineState_SO] Cannot subscribe - QuestManager.Instance is null.");
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
                QuestManager.Instance.QuestLineStarted.RemoveListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineCompleted.RemoveListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineUpdated.RemoveListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineFailed.RemoveListener(OnQuestLineStateChanged);
                QuestManager.Instance.QuestLineAdded.RemoveListener(OnQuestLineStateChanged);
            }

            _onConditionMet = null;
            _isSubscribed = false;
        }

        /// <summary>
        /// Forces the condition to be fulfilled. For debugging purposes.
        /// Note: This doesn't actually change questline state, just fires the callback if condition would be met.
        /// </summary>
        public void ForceFulfillCondition()
        {
            _onConditionMet?.Invoke();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the current state of the referenced questline from QuestManager.
        /// </summary>
        private QuestLineState GetQuestLineCurrentState()
        {
            if (QuestManager.Instance == null || questLineToCheck == null)
            {
                return QuestLineState.Locked;
            }

            if (QuestManager.Instance.IsQuestLineCompleted(questLineToCheck))
            {
                return QuestLineState.Completed;
            }

            if (QuestManager.Instance.IsQuestLineActive(questLineToCheck))
            {
                var line = QuestManager.Instance.GetQuestLine(questLineToCheck);
                if (line != null)
                {
                    return line.CurrentState;
                }
                return QuestLineState.InProgress;
            }

            // Check if it would be available (prerequisite met)
            if (questLineToCheck.PrerequisiteLine == null ||
                QuestManager.Instance.IsQuestLineCompleted(questLineToCheck.PrerequisiteLine))
            {
                return QuestLineState.Available;
            }

            return QuestLineState.Locked;
        }

        /// <summary>
        /// Evaluates the comparison between current state and target state.
        /// </summary>
        private bool EvaluateComparison(QuestLineState currentState, QuestLineState target)
        {
            return comparisonType switch
            {
                QuestLineStateComparison.Equals => currentState == target,
                QuestLineStateComparison.NotEquals => currentState != target,
                _ => currentState == target
            };
        }

        /// <summary>
        /// Called when any questline's state changes. Checks if it's the questline we're tracking.
        /// </summary>
        private void OnQuestLineStateChanged(QuestLineRuntime line)
        {
            if (line == null || questLineToCheck == null) return;

            // Only process if this event is for the questline we're tracking
            if (line.Data != questLineToCheck) return;

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
                Debug.Log("[ConditionQuestLineState_SO] Can only evaluate during Play mode.");
                return;
            }

            QuestLineState currentState = GetQuestLineCurrentState();
            bool result = Evaluate();
            Debug.Log($"[ConditionQuestLineState_SO] '{name}': QuestLine '{questLineToCheck?.name}' is {currentState}, target is {targetState} ({comparisonType}). Result: {result}");
        }

        [TitleGroup("Debug")]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying")]
        [PropertyOrder(101)]
        private string CurrentQuestLineState => Application.isPlaying && questLineToCheck != null
            ? GetQuestLineCurrentState().ToString()
            : "N/A";
#endif

        #endregion
    }

    /// <summary>
    /// Comparison types for questline state conditions.
    /// </summary>
    public enum QuestLineStateComparison
    {
        /// <summary>QuestLine must be in the exact target state.</summary>
        Equals,
        /// <summary>QuestLine must NOT be in the target state.</summary>
        NotEquals
    }
}
