using System;
using HelloDev.Conditions;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Achievements
{
    /// <summary>
    /// Defines the type of achievement tracking.
    /// </summary>
    public enum AchievementType
    {
        /// <summary>Binary achievement - either completed or not.</summary>
        Binary,
        /// <summary>Progressive achievement - tracks progress toward a target.</summary>
        Progressive,
        /// <summary>Hidden achievement - not shown until unlocked.</summary>
        Hidden
    }

    /// <summary>
    /// ScriptableObject that defines an achievement.
    /// Achievements are single objectives that track progress toward a goal.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAchievement", menuName = "HelloDev/Quest System/Achievements/Achievement")]
    public class Achievement_SO : RuntimeScriptableObject
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
        [SerializeField, Tooltip("A unique, permanent identifier for this achievement.")]
        private string achievementId;

#if ODIN_INSPECTOR
        [TitleGroup("Identity"), PropertyOrder(2)]
#endif
        [SerializeField, Tooltip("The type of achievement.")]
        private AchievementType achievementType = AchievementType.Binary;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [SerializeField, Tooltip("The localized display name.")]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(11)]
#endif
        [SerializeField, Tooltip("The localized description.")]
        private LocalizedString achievementDescription;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(12)] 
#endif
        [SerializeField, Tooltip("The localized description shown when hidden (before unlock).")]
        private LocalizedString hiddenDescription;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(13), PreviewField(60, Alignment = ObjectFieldAlignment.Left)]
#endif
        [SerializeField, Tooltip("Icon shown when locked.")]
        private Sprite lockedIcon;

#if ODIN_INSPECTOR
        [TitleGroup("Display"), PropertyOrder(14), PreviewField(60, Alignment = ObjectFieldAlignment.Left)]
#endif
        [SerializeField, Tooltip("Icon shown when unlocked.")]
        private Sprite unlockedIcon;

#if ODIN_INSPECTOR
        [TitleGroup("Progress"), PropertyOrder(20), ShowIf("@achievementType == AchievementType.Progressive")]
#else
        [Header("Progress")]
#endif
        [SerializeField, Tooltip("Target value for progressive achievements.")]
        private int targetValue = 1;

#if ODIN_INSPECTOR
        [TitleGroup("Progress"), PropertyOrder(21), ShowIf("@achievementType == AchievementType.Progressive")]
#endif
        [SerializeField, Tooltip("Starting value for progressive achievements.")]
        private int startValue;

#if ODIN_INSPECTOR
        [TitleGroup("Unlock Condition"), PropertyOrder(30)]
#else
        [Header("Unlock Condition")]
#endif
        [SerializeField, Tooltip("Optional condition that unlocks this achievement automatically.")]
        private Condition_SO unlockCondition;

#if ODIN_INSPECTOR
        [TitleGroup("Rewards"), PropertyOrder(40)]
#else
        [Header("Rewards")]
#endif
        [SerializeField, Tooltip("Points awarded when this achievement is unlocked.")]
        private int points = 10;

#if ODIN_INSPECTOR
        [TitleGroup("Rewards"), PropertyOrder(41)]
#endif
        [SerializeField, Tooltip("Optional category/tier for organization.")]
        private string category;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the developer-friendly name.
        /// </summary>
        public string DevName => string.IsNullOrEmpty(devName) ? name : devName;

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public Guid AchievementId => string.IsNullOrEmpty(achievementId) ? Guid.Empty : Guid.Parse(achievementId);

        /// <summary>
        /// Gets the achievement type.
        /// </summary>
        public AchievementType AchievementType => achievementType;

        /// <summary>
        /// Gets the localized display name.
        /// </summary>
        public LocalizedString DisplayName => displayName;

        /// <summary>
        /// Gets the localized description.
        /// </summary>
        public LocalizedString AchievementDescription => achievementDescription;

        /// <summary>
        /// Gets the hidden description.
        /// </summary>
        public LocalizedString HiddenDescription => hiddenDescription;

        /// <summary>
        /// Gets the locked icon.
        /// </summary>
        public Sprite LockedIcon => lockedIcon;

        /// <summary>
        /// Gets the unlocked icon.
        /// </summary>
        public Sprite UnlockedIcon => unlockedIcon;

        /// <summary>
        /// Gets the target value for progressive achievements.
        /// </summary>
        public int TargetValue => targetValue;

        /// <summary>
        /// Gets the starting value.
        /// </summary>
        public int StartValue => startValue;

        /// <summary>
        /// Gets the unlock condition.
        /// </summary>
        public Condition_SO UnlockCondition => unlockCondition;

        /// <summary>
        /// Gets the points awarded.
        /// </summary>
        public int Points => points;

        /// <summary>
        /// Gets the category.
        /// </summary>
        public string Category => category;

        /// <summary>
        /// Gets whether this is a hidden achievement.
        /// </summary>
        public bool IsHidden => achievementType == AchievementType.Hidden;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and returns a new runtime instance.
        /// </summary>
        public AchievementRuntime GetRuntimeAchievement()
        {
            return new AchievementRuntime(this);
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(achievementId))
            {
                achievementId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }

            if (achievementType == AchievementType.Progressive && targetValue <= 0)
            {
                Debug.LogWarning($"[Achievement_SO] '{DevName}': Progressive achievement has invalid target value.", this);
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
            if (obj is Achievement_SO other)
            {
                return AchievementId == other.AchievementId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return AchievementId.GetHashCode();
        }

        #endregion
    }
}
