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
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/Quest System/Conditions/Quest State Condition")]
    public class ConditionQuestState_SO : EventDrivenQuestCondition_SO
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

        #region EventDrivenQuestCondition_SO Implementation

        /// <inheritdoc/>
        protected override string GetConditionDisplayName() => "ConditionQuestState_SO";

        /// <inheritdoc/>
        protected override void SubscribeToManagerEvents()
        {
            QuestManager.Instance.QuestStarted.AddListener(OnQuestStateChanged);
            QuestManager.Instance.QuestCompleted.AddListener(OnQuestStateChanged);
            QuestManager.Instance.QuestFailed.AddListener(OnQuestStateChanged);
            QuestManager.Instance.QuestRestarted.AddListener(OnQuestStateChanged);
            QuestManager.Instance.QuestAdded.AddListener(OnQuestStateChanged);
        }

        /// <inheritdoc/>
        protected override void UnsubscribeFromManagerEvents()
        {
            QuestManager.Instance.QuestStarted.RemoveListener(OnQuestStateChanged);
            QuestManager.Instance.QuestCompleted.RemoveListener(OnQuestStateChanged);
            QuestManager.Instance.QuestFailed.RemoveListener(OnQuestStateChanged);
            QuestManager.Instance.QuestRestarted.RemoveListener(OnQuestStateChanged);
            QuestManager.Instance.QuestAdded.RemoveListener(OnQuestStateChanged);
        }

        /// <inheritdoc/>
        protected override bool EvaluateCondition()
        {
            if (questToCheck == null)
            {
                Debug.LogWarning($"[ConditionQuestState_SO] Quest reference is null on '{name}'.");
                return false;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning($"[ConditionQuestState_SO] QuestManager.Instance is null.");
                return false;
            }

            QuestState currentState = GetQuestCurrentState();
            return EvaluateComparison(currentState, targetState);
        }

        #endregion

        #region Private Methods

        private QuestState GetQuestCurrentState()
        {
            if (QuestManager.Instance == null || questToCheck == null)
                return QuestState.NotStarted;

            if (QuestManager.Instance.IsQuestCompleted(questToCheck))
                return QuestState.Completed;

            if (QuestManager.Instance.IsQuestFailed(questToCheck))
                return QuestState.Failed;

            if (QuestManager.Instance.IsQuestActive(questToCheck))
                return QuestState.InProgress;

            return QuestState.NotStarted;
        }

        private bool EvaluateComparison(QuestState currentState, QuestState target)
        {
            return comparisonType switch
            {
                QuestStateComparison.Equals => currentState == target,
                QuestStateComparison.NotEquals => currentState != target,
                _ => currentState == target
            };
        }

        private void OnQuestStateChanged(QuestRuntime quest)
        {
            if (quest == null || questToCheck == null) return;
            if (quest.QuestData != questToCheck) return;

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
