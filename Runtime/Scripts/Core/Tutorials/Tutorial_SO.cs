using System;
using System.Collections.Generic;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Tutorials
{
    /// <summary>
    /// ScriptableObject that defines a complete tutorial sequence.
    /// A tutorial is a guided experience with sequential steps.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorial", menuName = "HelloDev/Quest System/Tutorials/Tutorial")]
    public class Tutorial_SO : RuntimeScriptableObject
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
        [SerializeField, Tooltip("A unique, permanent identifier for this tutorial.")]
        private string tutorialId;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [SerializeField, Tooltip("The localized display name of the tutorial.")]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(11)]
#endif
        [SerializeField, Tooltip("The localized description of the tutorial.")]
        private LocalizedString tutorialDescription;

#if ODIN_INSPECTOR
        [TitleGroup("Steps"), PropertyOrder(20), ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
#else
        [Header("Steps")]
#endif
        [SerializeField, Tooltip("The ordered list of tutorial steps.")]
        private List<TutorialStep_SO> steps = new();

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(30)]
#else
        [Header("Behavior")]
#endif
        [SerializeField, Tooltip("If true, this tutorial only plays once per save.")]
        private bool playOnce = true;

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(31)]
#endif
        [SerializeField, Tooltip("If true, player can skip the entire tutorial.")]
        private bool canSkip = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the developer-friendly name of the tutorial.
        /// </summary>
        public string DevName => string.IsNullOrEmpty(devName) ? name : devName;

        /// <summary>
        /// Gets the unique identifier for this tutorial.
        /// </summary>
        public Guid TutorialId => string.IsNullOrEmpty(tutorialId) ? Guid.Empty : Guid.Parse(tutorialId);

        /// <summary>
        /// Gets the localized display name.
        /// </summary>
        public LocalizedString DisplayName => displayName;

        /// <summary>
        /// Gets the localized description.
        /// </summary>
        public LocalizedString TutorialDescription => tutorialDescription;

        /// <summary>
        /// Gets the list of tutorial steps.
        /// </summary>
        public IReadOnlyList<TutorialStep_SO> Steps => steps;

        /// <summary>
        /// Gets whether this tutorial should only play once.
        /// </summary>
        public bool PlayOnce => playOnce;

        /// <summary>
        /// Gets whether the player can skip this tutorial.
        /// </summary>
        public bool CanSkip => canSkip;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and returns a new runtime instance of this tutorial.
        /// </summary>
        public TutorialRuntime GetRuntimeTutorial()
        {
            return new TutorialRuntime(this);
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(tutorialId))
            {
                tutorialId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }

            // Validate steps
            if (steps != null)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i] == null)
                    {
                        Debug.LogWarning($"[Tutorial_SO] '{DevName}': Step at index {i} is null.", this);
                    }
                }
            }
            
            HashSet<string> guids = new HashSet<string>();
            foreach (TutorialStep_SO step in Steps)
            {
                if (string.IsNullOrEmpty(step.StepId.ToString()))
                {
                    step.GenerateNewGuid();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                else if (guids.Contains(step.StepId.ToString()))
                {
                    Debug.LogError($"Duplicate GUID detected in tutorial '{DevName}' for step '{step.DevName}'!");
                }
                guids.Add(step.StepId.ToString());
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
            if (obj is Tutorial_SO other)
            {
                return TutorialId == other.TutorialId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return TutorialId.GetHashCode();
        }

        #endregion
    }
}
