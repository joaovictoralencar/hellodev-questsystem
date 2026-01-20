using System;
using HelloDev.Conditions;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Tutorials
{
    /// <summary>
    /// ScriptableObject that defines a single step in a tutorial.
    /// Each step represents one instruction or action the player must complete.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialStep", menuName = "HelloDev/Quest System/Tutorials/Tutorial Step")]
    public class TutorialStep_SO : RuntimeScriptableObject
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Identity"), PropertyOrder(0)]
#else
        [Header("Identity")]
#endif
        [SerializeField, Tooltip("Internal name for developers.")]
        private string devName;

#if ODIN_INSPECTOR
        [TitleGroup("Identity"), PropertyOrder(1), ReadOnly, DisplayAsString]
#endif
        [SerializeField, Tooltip("A unique, permanent identifier for this step.")]
        private string stepId;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [SerializeField, Tooltip("The localized instruction text shown to the player.")]
        private LocalizedString instruction;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(11)]
#endif
        [SerializeField, Tooltip("Optional sprite/icon for this step.")]
        private Sprite stepIcon;

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(20)]
#else
        [Header("Behavior")]
#endif
        [SerializeField, Tooltip("If true, this step auto-completes after the duration.")]
        private bool isTimedStep;

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(21), ShowIf(nameof(isTimedStep))]
#endif
        [SerializeField, Tooltip("Duration in seconds before auto-completing (if timed).")]
        private float duration = 3f;

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(22)]
#endif
        [SerializeField, Tooltip("If true, player can skip this step.")]
        private bool canSkip = true;

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(23)]
#endif
        [SerializeField, Tooltip("Optional condition that triggers step completion when satisfied.")]
        private Condition_SO completionCondition;

#if ODIN_INSPECTOR
        [TitleGroup("UI Highlight"), PropertyOrder(30)]
#else
        [Header("UI Highlight")]
#endif
        [SerializeField, Tooltip("Optional UI element tag to highlight during this step.")]
        private string highlightTarget;

#if ODIN_INSPECTOR
        [TitleGroup("UI Highlight"), PropertyOrder(31)]
#endif
        [SerializeField, Tooltip("Position offset for the tutorial popup relative to the highlight target.")]
        private Vector2 popupOffset;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the developer-friendly name of the step.
        /// </summary>
        public string DevName => string.IsNullOrEmpty(devName) ? name : devName;

        /// <summary>
        /// Gets the unique identifier for this step.
        /// </summary>
        public Guid StepId => string.IsNullOrEmpty(stepId) ? Guid.Empty : Guid.Parse(stepId);

        /// <summary>
        /// Gets the localized instruction text.
        /// </summary>
        public LocalizedString Instruction => instruction;

        /// <summary>
        /// Gets the optional icon for this step.
        /// </summary>
        public Sprite StepIcon => stepIcon;

        /// <summary>
        /// Gets whether this step is time-based.
        /// </summary>
        public bool IsTimedStep => isTimedStep;

        /// <summary>
        /// Gets the duration for timed steps.
        /// </summary>
        public float Duration => duration;

        /// <summary>
        /// Gets whether the player can skip this step.
        /// </summary>
        public bool CanSkip => canSkip;

        /// <summary>
        /// Gets the optional condition that triggers step completion.
        /// </summary>
        public Condition_SO CompletionCondition => completionCondition;

        /// <summary>
        /// Gets the UI element tag to highlight.
        /// </summary>
        public string HighlightTarget => highlightTarget;

        /// <summary>
        /// Gets the popup position offset.
        /// </summary>
        public Vector2 PopupOffset => popupOffset;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and returns a new runtime instance of this tutorial step.
        /// </summary>
        public TutorialStepRuntime GetRuntimeStep()
        {
            return new TutorialStepRuntime(this);
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(stepId))
            {
                stepId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }
        }

        #endregion

        #region Unity Callbacks

        protected override void OnScriptableObjectReset()
        {
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is TutorialStep_SO other)
            {
                return StepId == other.StepId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return StepId.GetHashCode();
        }

        #endregion
    }
}
