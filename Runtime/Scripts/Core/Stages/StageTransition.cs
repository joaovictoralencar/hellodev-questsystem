using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Defines how to transition from one stage to another.
    /// Transitions can be triggered by completing tasks, meeting conditions, or manually via API.
    /// </summary>
    [Serializable]
    public class StageTransition
    {
#if ODIN_INSPECTOR
        [BoxGroup("Transition")]
        [PropertyOrder(0)]
        [LabelText("Target Stage")]
        [Tooltip("The stage index to transition to when this transition is triggered.")]
#endif
        [SerializeField]
        private int targetStageIndex;

#if ODIN_INSPECTOR
        [BoxGroup("Transition")]
        [PropertyOrder(1)]
        [Tooltip("What triggers this transition.")]
#endif
        [SerializeField]
        private TransitionTrigger trigger = TransitionTrigger.OnGroupsComplete;

#if ODIN_INSPECTOR
        [BoxGroup("Transition")]
        [PropertyOrder(2)]
        [ShowIf(nameof(HasConditions))]
        [ListDrawerSettings(ShowFoldout = true)]
        [Tooltip("Conditions that must be met for this transition (used with OnConditionsMet trigger, or as additional gate for other triggers).")]
#endif
        [SerializeField]
        private List<Condition_SO> conditions = new();

#if ODIN_INSPECTOR
        [BoxGroup("Transition")]
        [PropertyOrder(3)]
        [Tooltip("Optional label for this transition (useful for branching choices).")]
#endif
        [SerializeField]
        private string transitionLabel;

#if ODIN_INSPECTOR
        [BoxGroup("Transition")]
        [PropertyOrder(4)]
        [Tooltip("Priority when multiple transitions are valid. Higher priority is evaluated first.")]
#endif
        [SerializeField]
        private int priority;

        #region Properties

        /// <summary>
        /// Gets the index of the stage to transition to.
        /// </summary>
        public int TargetStageIndex => targetStageIndex;

        /// <summary>
        /// Gets the trigger type for this transition.
        /// </summary>
        public TransitionTrigger Trigger => trigger;

        /// <summary>
        /// Gets the conditions required for this transition.
        /// For OnConditionsMet trigger, these are monitored for fulfillment.
        /// For other triggers, these act as additional gates.
        /// </summary>
        public List<Condition_SO> Conditions => conditions;

        /// <summary>
        /// Gets the optional label for this transition.
        /// </summary>
        public string TransitionLabel => transitionLabel;

        /// <summary>
        /// Gets the priority of this transition.
        /// Higher priority transitions are evaluated first.
        /// </summary>
        public int Priority => priority;

        /// <summary>
        /// Returns true if this transition uses conditions.
        /// </summary>
        public bool HasConditions => conditions != null && conditions.Count > 0;

        #endregion

        #region Methods

        /// <summary>
        /// Evaluates all conditions for this transition.
        /// </summary>
        /// <returns>True if all conditions are met (or no conditions exist).</returns>
        public bool EvaluateConditions()
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (var condition in conditions)
            {
                if (condition != null && !condition.Evaluate())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the transition configuration.
        /// </summary>
        /// <param name="maxStageIndex">The maximum valid stage index.</param>
        /// <returns>List of validation warnings.</returns>
        public List<string> Validate(int maxStageIndex)
        {
            var warnings = new List<string>();

            if (targetStageIndex < 0)
            {
                warnings.Add($"Transition has invalid target stage index: {targetStageIndex}");
            }
            else if (targetStageIndex > maxStageIndex)
            {
                warnings.Add($"Transition target stage index ({targetStageIndex}) exceeds max stage index ({maxStageIndex})");
            }

            if (trigger == TransitionTrigger.OnConditionsMet && (conditions == null || conditions.Count == 0))
            {
                warnings.Add("Transition with OnConditionsMet trigger has no conditions defined");
            }

            if (conditions != null)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (conditions[i] == null)
                    {
                        warnings.Add($"Transition has null condition at index {i}");
                    }
                }
            }

            return warnings;
        }

        #endregion
    }
}
