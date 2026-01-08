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
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/Quest System/Conditions/Quest Line State Condition")]
    public class ConditionQuestLineState_SO : EventDrivenQuestCondition_SO
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

        #region EventDrivenQuestCondition_SO Implementation

        /// <inheritdoc/>
        protected override string GetConditionDisplayName() => "ConditionQuestLineState_SO";

        /// <inheritdoc/>
        protected override void SubscribeToManagerEvents()
        {
            QuestManager.Instance.QuestLineStarted.AddListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineCompleted.AddListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineUpdated.AddListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineFailed.AddListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineAdded.AddListener(OnQuestLineStateChanged);
        }

        /// <inheritdoc/>
        protected override void UnsubscribeFromManagerEvents()
        {
            QuestManager.Instance.QuestLineStarted.RemoveListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineCompleted.RemoveListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineUpdated.RemoveListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineFailed.RemoveListener(OnQuestLineStateChanged);
            QuestManager.Instance.QuestLineAdded.RemoveListener(OnQuestLineStateChanged);
        }

        /// <inheritdoc/>
        protected override bool EvaluateCondition()
        {
            if (questLineToCheck == null)
            {
                Debug.LogWarning($"[ConditionQuestLineState_SO] QuestLine reference is null on '{name}'.");
                return false;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning($"[ConditionQuestLineState_SO] QuestManager.Instance is null.");
                return false;
            }

            QuestLineState currentState = GetQuestLineCurrentState();
            return EvaluateComparison(currentState, targetState);
        }

        #endregion

        #region Private Methods

        private QuestLineState GetQuestLineCurrentState()
        {
            if (QuestManager.Instance == null || questLineToCheck == null)
                return QuestLineState.Locked;

            if (QuestManager.Instance.IsQuestLineCompleted(questLineToCheck))
                return QuestLineState.Completed;

            if (QuestManager.Instance.IsQuestLineActive(questLineToCheck))
            {
                var line = QuestManager.Instance.GetQuestLine(questLineToCheck);
                if (line != null)
                    return line.CurrentState;
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

        private bool EvaluateComparison(QuestLineState currentState, QuestLineState target)
        {
            return comparisonType switch
            {
                QuestLineStateComparison.Equals => currentState == target,
                QuestLineStateComparison.NotEquals => currentState != target,
                _ => currentState == target
            };
        }

        private void OnQuestLineStateChanged(QuestLineRuntime line)
        {
            if (line == null || questLineToCheck == null) return;
            if (line.Data != questLineToCheck) return;

            OnTrackedEntityChanged();
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
