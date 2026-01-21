using UnityEngine;

namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Abstract base class for effects that can be applied when a stage transition occurs.
    /// Extend this class to create custom transition effects (world flags, achievements, etc.).
    /// </summary>
    public abstract class TransitionEffect_SO : ScriptableObject
    {
        /// <summary>
        /// Applies this effect. Called when the transition is selected/executed.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Gets whether this effect is properly configured and can be applied.
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// Gets a human-readable description of this effect for debugging/editor display.
        /// </summary>
        public virtual string Description => name;
    }
}
