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
    /// ScriptableObject that defines a substep within a multi-step tutorial step.
    /// Substeps allow grouping related actions (e.g., "Move Forward", "Move Left") under one step.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialSubstep", menuName = "HelloDev/Quest System/Tutorials/Tutorial Substep")]
    public class TutorialSubstep_SO : RuntimeScriptableObject
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
        [SerializeField, Tooltip("A unique, permanent identifier for this substep.")]
        private string substepId;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [SerializeField, Tooltip("The localized instruction text shown to the player.")]
        private LocalizedString instruction;

#if ODIN_INSPECTOR
        [TitleGroup("Behavior"), PropertyOrder(20)]
#else
        [Header("Behavior")]
#endif
        [SerializeField, Tooltip("Condition that triggers substep completion when satisfied.")]
        private Condition_SO completionCondition;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the developer-friendly name of the substep.
        /// </summary>
        public string DevName => string.IsNullOrEmpty(devName) ? name : devName;

        /// <summary>
        /// Gets the unique identifier for this substep.
        /// </summary>
        public Guid SubstepId => string.IsNullOrEmpty(substepId) ? Guid.Empty : Guid.Parse(substepId);

        /// <summary>
        /// Gets the localized instruction text.
        /// </summary>
        public LocalizedString Instruction => instruction;

        /// <summary>
        /// Gets the condition that triggers substep completion.
        /// </summary>
        public Condition_SO CompletionCondition => completionCondition;

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(substepId))
            {
                substepId = Guid.NewGuid().ToString();
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
            if (obj is TutorialSubstep_SO other)
            {
                return SubstepId == other.SubstepId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return SubstepId.GetHashCode();
        }

        #endregion
    }
}
