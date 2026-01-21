using HelloDev.Conditions.WorldFlags;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Stages.Effects
{
    /// <summary>
    /// Transition effect that modifies a world flag when the transition is executed.
    /// Allows setting boolean or integer world flags as consequences of stage transitions.
    /// </summary>
    [CreateAssetMenu(menuName = "HelloDev/Quest System/Effects/World Flag Effect", fileName = "SO_Effect_WorldFlag")]
    public class WorldFlagTransitionEffect_SO : TransitionEffect_SO
    {
#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(0), Required]
#endif
        [SerializeField, Tooltip("The locator that provides access to the world flag manager.")]
        private WorldFlagLocator_SO flagLocator;

#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(1)]
#endif
        [SerializeField, Tooltip("True for boolean flag, false for integer flag.")]
        private bool isBoolFlag = true;

#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(2), ShowIf(nameof(isBoolFlag)), Required]
#endif
        [SerializeField, Tooltip("The boolean flag to modify.")]
        private WorldFlagBool_SO boolFlag;

#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(3), ShowIf(nameof(isBoolFlag))]
#endif
        [SerializeField, Tooltip("The value to set the boolean flag to.")]
        private bool boolValue = true;

#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(4), HideIf(nameof(isBoolFlag)), Required]
#endif
        [SerializeField, Tooltip("The integer flag to modify.")]
        private WorldFlagInt_SO intFlag;

#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(5), HideIf(nameof(isBoolFlag))]
#endif
        [SerializeField, Tooltip("The operation to perform on the integer flag.")]
        private IntFlagOperation intOperation = IntFlagOperation.Set;

#if ODIN_INSPECTOR
        [BoxGroup("World Flag"), PropertyOrder(6), HideIf(nameof(isBoolFlag))]
#endif
        [SerializeField, Tooltip("The value for the integer operation.")]
        private int intValue;

        /// <summary>
        /// Gets whether this effect is properly configured.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                if (flagLocator == null)
                    return false;

                if (isBoolFlag)
                    return boolFlag != null;
                else
                    return intFlag != null;
            }
        }

        /// <summary>
        /// Gets a description of this effect.
        /// </summary>
        public override string Description
        {
            get
            {
                if (isBoolFlag && boolFlag != null)
                    return $"Set {boolFlag.FlagName} = {boolValue}";
                else if (!isBoolFlag && intFlag != null)
                    return $"{intOperation} {intFlag.FlagName} {intValue}";
                else
                    return "Invalid World Flag Effect";
            }
        }

        /// <summary>
        /// Applies the world flag modification.
        /// </summary>
        public override void Apply()
        {
            if (flagLocator == null || !flagLocator.IsAvailable)
            {
                Debug.LogWarning($"[WorldFlagTransitionEffect] Cannot apply - flagLocator not available.");
                return;
            }

            if (isBoolFlag)
            {
                ApplyBoolFlag();
            }
            else
            {
                ApplyIntFlag();
            }
        }

        private void ApplyBoolFlag()
        {
            if (boolFlag == null)
            {
                Debug.LogWarning("[WorldFlagTransitionEffect] Bool flag is null.");
                return;
            }

            var runtimeFlag = flagLocator.Manager.GetBoolFlag(boolFlag);
            if (runtimeFlag == null)
            {
                Debug.LogWarning($"[WorldFlagTransitionEffect] Runtime flag not found for {boolFlag.FlagName}");
                return;
            }

            runtimeFlag.SetValue(boolValue);
        }

        private void ApplyIntFlag()
        {
            if (intFlag == null)
            {
                Debug.LogWarning("[WorldFlagTransitionEffect] Int flag is null.");
                return;
            }

            var runtimeFlag = flagLocator.Manager.GetIntFlag(intFlag);
            if (runtimeFlag == null)
            {
                Debug.LogWarning($"[WorldFlagTransitionEffect] Runtime flag not found for {intFlag.FlagName}");
                return;
            }

            switch (intOperation)
            {
                case IntFlagOperation.Set:
                    runtimeFlag.SetValue(intValue);
                    break;
                case IntFlagOperation.Add:
                    runtimeFlag.SetValue(runtimeFlag.Value + intValue);
                    break;
                case IntFlagOperation.Subtract:
                    runtimeFlag.SetValue(runtimeFlag.Value - intValue);
                    break;
                case IntFlagOperation.Multiply:
                    runtimeFlag.SetValue(runtimeFlag.Value * intValue);
                    break;
            }
        }
    }

    /// <summary>
    /// Operations that can be performed on integer world flags.
    /// </summary>
    public enum IntFlagOperation
    {
        Set,
        Add,
        Subtract,
        Multiply
    }
}
