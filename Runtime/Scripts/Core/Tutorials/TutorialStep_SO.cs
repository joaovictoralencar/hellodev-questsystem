using System;
using System.Collections.Generic;
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
        [SerializeField, Tooltip("Optional condition that triggers step completion when satisfied. Used for simple steps without substeps.")]
        private Condition_SO completionCondition;

#if ODIN_INSPECTOR
        [TitleGroup("Multi-Step"), PropertyOrder(30)]
#else
        [Header("Multi-Step")]
#endif
        [SerializeField, Tooltip("Optional substeps for multi-step tutorials. When present, all substeps must be completed.")]
        private List<TutorialSubstep_SO> substeps = new();

#if ODIN_INSPECTOR
        [TitleGroup("Count-Based"), PropertyOrder(40)]
#else
        [Header("Count-Based")]
#endif
        [SerializeField, Tooltip("Number of times the condition must be satisfied. Set to 0 or 1 for single completion.")]
        private int requiredCount = 1;

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
        /// </summary>gra
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
        /// Gets the list of substeps for multi-step tutorials.
        /// </summary>
        public IReadOnlyList<TutorialSubstep_SO> Substeps => substeps;

        /// <summary>
        /// Gets whether this step has substeps.
        /// </summary>
        public bool HasSubsteps => substeps != null && substeps.Count > 0;

        /// <summary>
        /// Gets the number of substeps.
        /// </summary>
        public int SubstepCount => substeps?.Count ?? 0;

        /// <summary>
        /// Gets the required count for count-based steps.
        /// </summary>
        public int RequiredCount => requiredCount > 0 ? requiredCount : 1;

        /// <summary>
        /// Gets whether this is a count-based step (requires multiple completions).
        /// </summary>
        public bool IsCountBased => requiredCount > 1;

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
        
#if ODIN_INSPECTOR
        [Button]
#endif
        public void GenerateNewGuid()
        {
            stepId = Guid.NewGuid().ToString();
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
