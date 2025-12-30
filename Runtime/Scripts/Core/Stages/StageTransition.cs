using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using HelloDev.Conditions.WorldFlags;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Defines how to transition from one stage to another.
    /// Transitions can be triggered by completing tasks, meeting conditions, manually via API,
    /// or by player choice (branching).
    /// </summary>
    [Serializable]
    public class StageTransition
    {
#if ODIN_INSPECTOR
        [BoxGroup("Transition"), PropertyOrder(0), LabelText("Target Stage")]
#endif
        [SerializeField, Tooltip("The stage index to transition to when this transition is triggered.")]
        private int targetStageIndex;

#if ODIN_INSPECTOR
        [BoxGroup("Transition"), PropertyOrder(1)]
#endif
        [SerializeField, Tooltip("What triggers this transition.")]
        private TransitionTrigger trigger = TransitionTrigger.OnGroupsComplete;

#if ODIN_INSPECTOR
        [BoxGroup("Transition"), PropertyOrder(2), ShowIf(nameof(HasConditions)), ListDrawerSettings(ShowFoldout = true)]
#endif
        [SerializeField, Tooltip("Conditions that must be met for this transition.")]
        private List<Condition_SO> conditions = new();

#if ODIN_INSPECTOR
        [BoxGroup("Transition"), PropertyOrder(3)]
#endif
        [SerializeField, Tooltip("Optional label for this transition (useful for branching choices).")]
        private string transitionLabel;

#if ODIN_INSPECTOR
        [BoxGroup("Transition"), PropertyOrder(4)]
#endif
        [SerializeField, Tooltip("Priority when multiple transitions are valid. Higher priority is evaluated first.")]
        private int priority;

        #region Player Choice Fields

#if ODIN_INSPECTOR
        [BoxGroup("Player Choice"), PropertyOrder(10), OnValueChanged(nameof(OnIsPlayerChoiceChanged))]
#endif
        [SerializeField, Tooltip("When true, this transition requires player selection (shows in choice UI).")]
        private bool isPlayerChoice;

#if ODIN_INSPECTOR
        [BoxGroup("Player Choice"), PropertyOrder(11), ShowIf(nameof(isPlayerChoice))]
#endif
        [SerializeField, Tooltip("Unique identifier for this choice (used for save/load and tracking).")]
        private string choiceId;

#if ODIN_INSPECTOR
        [BoxGroup("Player Choice"), PropertyOrder(12), ShowIf(nameof(isPlayerChoice))]
#endif
        [SerializeField, Tooltip("Localized text displayed to the player for this choice.")]
        private LocalizedString choiceText;

#if ODIN_INSPECTOR
        [BoxGroup("Player Choice"), PropertyOrder(13), ShowIf(nameof(isPlayerChoice)), PreviewField(50)]
#endif
        [SerializeField, Tooltip("Optional icon displayed with this choice.")]
        private Sprite choiceIcon;

#if ODIN_INSPECTOR
        [BoxGroup("Player Choice"), PropertyOrder(14), ShowIf(nameof(isPlayerChoice))]
#endif
        [SerializeField, Tooltip("Tooltip or additional description for this choice.")]
        private LocalizedString choiceTooltip;

        #endregion

        #region Consequences (World State)

#if ODIN_INSPECTOR
        [BoxGroup("Consequences"), PropertyOrder(20), ShowIf(nameof(isPlayerChoice)), ListDrawerSettings(ShowFoldout = true)]
#endif
        [SerializeField, Tooltip("World flags to modify when this choice is selected.")]
        private List<WorldFlagModification> worldFlagsOnSelect = new();

        #endregion

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

        /// <summary>
        /// Returns true if this transition requires player selection.
        /// Player choice transitions are shown in UI for the player to select.
        /// </summary>
        public bool IsPlayerChoice => isPlayerChoice;

        /// <summary>
        /// Gets the unique identifier for this choice.
        /// Used for save/load and decision tracking.
        /// Falls back to a generated ID if not set.
        /// </summary>
        public string ChoiceId => string.IsNullOrEmpty(choiceId)
            ? $"choice_to_{targetStageIndex}"
            : choiceId;

        /// <summary>
        /// Gets the localized text to display for this choice.
        /// </summary>
        public LocalizedString ChoiceText => choiceText;

        /// <summary>
        /// Gets the optional icon for this choice.
        /// </summary>
        public Sprite ChoiceIcon => choiceIcon;

        /// <summary>
        /// Gets the optional tooltip/description for this choice.
        /// </summary>
        public LocalizedString ChoiceTooltip => choiceTooltip;

        /// <summary>
        /// Returns true if this choice is currently available (all conditions met).
        /// </summary>
        public bool IsChoiceAvailable => !isPlayerChoice || EvaluateConditions();

        /// <summary>
        /// Gets the world flag modifications to apply when this choice is selected.
        /// </summary>
        public List<WorldFlagModification> WorldFlagsOnSelect => worldFlagsOnSelect;

        /// <summary>
        /// Returns true if this transition has world flag modifications.
        /// </summary>
        public bool HasWorldFlagModifications => worldFlagsOnSelect != null && worldFlagsOnSelect.Count > 0;

        #endregion

        #region Methods

        /// <summary>
        /// Applies all world flag modifications for this transition.
        /// Called when the choice is selected.
        /// </summary>
        public void ApplyWorldFlagModifications()
        {
            if (worldFlagsOnSelect == null || worldFlagsOnSelect.Count == 0)
                return;

            foreach (var modification in worldFlagsOnSelect)
            {
                if (modification != null && modification.IsValid)
                {
                    modification.Apply();
                }
            }
        }

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

        #region Editor Helpers

#if ODIN_INSPECTOR && UNITY_EDITOR
        private void OnIsPlayerChoiceChanged()
        {
            // Auto-set trigger to PlayerChoice when isPlayerChoice is enabled
            if (isPlayerChoice && trigger != TransitionTrigger.PlayerChoice)
            {
                trigger = TransitionTrigger.PlayerChoice;
            }
            else if (!isPlayerChoice && trigger == TransitionTrigger.PlayerChoice)
            {
                trigger = TransitionTrigger.OnGroupsComplete;
            }

            // Auto-generate choice ID if empty
            if (isPlayerChoice && string.IsNullOrEmpty(choiceId))
            {
                choiceId = System.Guid.NewGuid().ToString().Substring(0, 8);
            }
        }
#endif

        #endregion
    }
}
