using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// Abstract ScriptableObject defining a reward type.
    /// Contains display information and granting logic.
    /// </summary>
    /// <remarks>
    /// Create concrete subclasses for different reward categories:
    /// - ExperienceRewardType_SO for XP rewards
    /// - CurrencyRewardType_SO for gold/gems
    /// - ItemRewardType_SO for inventory items
    ///
    /// Each subclass implements GiveReward() to handle the actual granting.
    /// The ScriptableObject stores configuration (icon, name) and knows
    /// how to grant itself - keeping data and behavior together per reward type.
    /// </remarks>
    public abstract class QuestRewardType_SO : ScriptableObject
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
        [PreviewField(50)]
#endif
        [SerializeField]
        [Tooltip("Icon displayed in UI for this reward")]
        private Sprite icon;

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
#endif
        [SerializeField]
        [Tooltip("Localized name displayed in UI")]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [BoxGroup("Display")]
        [TextArea(2, 4)]
#endif
        [SerializeField]
        [Tooltip("Optional description of this reward type")]
        private string description;

#if ODIN_INSPECTOR
        [BoxGroup("Metadata")]
#endif
        [SerializeField]
        [Tooltip("Category for filtering/grouping rewards")]
        private string category;

        #endregion

        #region Properties

        /// <summary>
        /// Icon displayed in UI for this reward.
        /// </summary>
        public Sprite RewardIcon => icon;

        /// <summary>
        /// Localized display name for UI.
        /// </summary>
        public LocalizedString RewardName => displayName;

        /// <summary>
        /// Optional description of this reward type.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Category for filtering/grouping rewards.
        /// </summary>
        public string Category => category;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Grants this reward to the player.
        /// Override in concrete subclasses to implement reward-specific logic.
        /// </summary>
        /// <param name="amount">The amount of this reward to grant.</param>
        public abstract void GiveReward(int amount);

        #endregion
    }

    /// <summary>
    /// Pairs a reward type with an amount for granting.
    /// </summary>
    [Serializable]
    public struct RewardInstance
    {
        [Tooltip("The type of reward to grant")]
        public QuestRewardType_SO RewardType;

        [Tooltip("Amount of this reward to grant")]
        public int Amount;

        /// <summary>
        /// Gets a description of this reward for display.
        /// </summary>
        public string DisplayText => RewardType != null
            ? $"{RewardType.name} x{Amount}"
            : "(empty)";

        /// <summary>
        /// Returns true if this instance has a valid reward type.
        /// </summary>
        public bool IsValid => RewardType != null && Amount > 0;
    }
}