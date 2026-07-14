using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Achievements
{
    /// <summary>
    /// Central manager for all achievements. Handles achievement lifecycle, tracking, and events.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        #region Singleton

        private static AchievementManager _instance;

        /// <summary>
        /// Gets the singleton instance of the AchievementManager.
        /// </summary>
        public static AchievementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AchievementManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[AchievementManager] No AchievementManager found in scene.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Achievement Database"), ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
#else
        [Header("Achievement Database")]
#endif
        [SerializeField, Tooltip("All available achievements.")]
        private List<Achievement_SO> achievementDatabase = new();

#if ODIN_INSPECTOR
        [TitleGroup("Configuration"), ToggleLeft]
#else
        [Header("Configuration")]
#endif
        [SerializeField, Tooltip("If true, debug messages will be logged.")]
        private bool enableDebugLogging = true;

#if ODIN_INSPECTOR
        [TitleGroup("Configuration"), ToggleLeft]
#endif
        [SerializeField, Tooltip("If true, achievements start tracking automatically on initialization.")]
        private bool autoStartTracking = true;

        #endregion

        #region Events

        /// <summary>
        /// Fired when any achievement is unlocked.
        /// </summary>
        public UnityEvent<AchievementRuntime> OnAchievementUnlocked = new();

        /// <summary>
        /// Fired when any achievement's progress changes.
        /// </summary>
        public UnityEvent<AchievementRuntime> OnAchievementProgressChanged = new();

        #endregion

        #region Private Fields

        private readonly Dictionary<Guid, AchievementRuntime> _achievements = new();
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Gets all achievements.
        /// </summary>
        public IReadOnlyCollection<AchievementRuntime> AllAchievements => _achievements.Values;

        /// <summary>
        /// Gets all unlocked achievements.
        /// </summary>
        public IReadOnlyCollection<AchievementRuntime> UnlockedAchievements =>
            _achievements.Values.Where(a => a.IsComplete).ToList();

        /// <summary>
        /// Gets the total achievement points earned.
        /// </summary>
        public int TotalPointsEarned =>
            _achievements.Values.Where(a => a.IsComplete).Sum(a => a.Data.Points);

        /// <summary>
        /// Gets the total possible achievement points.
        /// </summary>
        public int TotalPointsPossible =>
            achievementDatabase.Sum(a => a.Points);

        /// <summary>
        /// Gets the unlock percentage.
        /// </summary>
        public float UnlockPercentage =>
            achievementDatabase.Count > 0
                ? (float)UnlockedAchievements.Count / achievementDatabase.Count
                : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            // Unsubscribe from all achievement events
            foreach (var achievement in _achievements.Values)
            {
                achievement.Completed.SafeUnsubscribe(HandleAchievementUnlocked);
                achievement.Updated.SafeUnsubscribe(HandleProgressUpdated);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            QuestLogger.IsLoggingEnabled = enableDebugLogging;

            // Create runtime instances for all achievements
            foreach (var achievementData in achievementDatabase)
            {
                if (achievementData == null) continue;

                var runtime = achievementData.GetRuntimeAchievement();
                _achievements[achievementData.AchievementId] = runtime;

                // Subscribe to events
                runtime.Completed.SafeSubscribe(HandleAchievementUnlocked);
                runtime.Updated.SafeSubscribe(HandleProgressUpdated);

                // Auto-start tracking if enabled
                if (autoStartTracking)
                {
                    runtime.StartTracking();
                }
            }

            QuestLogger.Log(LogSubsystem.Achievement, $"AchievementManager initialized with {_achievements.Count} achievements.");
            _isInitialized = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an achievement by its ID.
        /// </summary>
        /// <param name="achievementId">The achievement ID.</param>
        /// <returns>The runtime achievement, or null if not found.</returns>
        public AchievementRuntime GetAchievement(Guid achievementId)
        {
            return _achievements.TryGetValue(achievementId, out var achievement) ? achievement : null;
        }

        /// <summary>
        /// Gets an achievement by its ScriptableObject.
        /// </summary>
        /// <param name="achievementData">The achievement ScriptableObject.</param>
        /// <returns>The runtime achievement, or null if not found.</returns>
        public AchievementRuntime GetAchievement(Achievement_SO achievementData)
        {
            return achievementData != null ? GetAchievement(achievementData.AchievementId) : null;
        }

        /// <summary>
        /// Unlocks an achievement by its ID.
        /// </summary>
        /// <param name="achievementId">The achievement ID.</param>
        /// <returns>True if unlocked, false if not found or already unlocked.</returns>
        public bool UnlockAchievement(Guid achievementId)
        {
            var achievement = GetAchievement(achievementId);
            if (achievement == null) return false;
            if (achievement.IsComplete) return false;

            achievement.Unlock();
            return true;
        }

        /// <summary>
        /// Unlocks an achievement by its ScriptableObject.
        /// </summary>
        /// <param name="achievementData">The achievement ScriptableObject.</param>
        /// <returns>True if unlocked.</returns>
        public bool UnlockAchievement(Achievement_SO achievementData)
        {
            return achievementData != null && UnlockAchievement(achievementData.AchievementId);
        }

        /// <summary>
        /// Increments progress on a progressive achievement.
        /// </summary>
        /// <param name="achievementId">The achievement ID.</param>
        /// <param name="amount">Amount to add.</param>
        public void IncrementProgress(Guid achievementId, int amount = 1)
        {
            var achievement = GetAchievement(achievementId);
            achievement?.IncrementProgress(amount);
        }

        /// <summary>
        /// Sets progress on a progressive achievement.
        /// </summary>
        /// <param name="achievementId">The achievement ID.</param>
        /// <param name="value">The value to set.</param>
        public void SetProgress(Guid achievementId, int value)
        {
            var achievement = GetAchievement(achievementId);
            achievement?.SetProgress(value);
        }

        /// <summary>
        /// Gets achievements by category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>List of achievements in the category.</returns>
        public IReadOnlyList<AchievementRuntime> GetAchievementsByCategory(string category)
        {
            return _achievements.Values
                .Where(a => string.Equals(a.Data.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Checks if an achievement is unlocked.
        /// </summary>
        /// <param name="achievementId">The achievement ID.</param>
        /// <returns>True if unlocked.</returns>
        public bool IsAchievementUnlocked(Guid achievementId)
        {
            var achievement = GetAchievement(achievementId);
            return achievement?.IsComplete ?? false;
        }

        /// <summary>
        /// Resets all achievement progress.
        /// </summary>
        public void ResetAllProgress()
        {
            foreach (var achievement in _achievements.Values)
            {
                achievement.ResetProgress();
            }

            // Restart tracking if auto-start is enabled
            if (autoStartTracking)
            {
                foreach (var achievement in _achievements.Values)
                {
                    achievement.StartTracking();
                }
            }

            QuestLogger.Log(LogSubsystem.Achievement, "All achievement progress reset.");
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Data structure for saving achievement state.
        /// </summary>
        [Serializable]
        public class AchievementSaveData
        {
            public string AchievementId;
            public bool IsUnlocked;
            public int CurrentValue;
            public string UnlockTime; // ISO 8601 format
        }

        /// <summary>
        /// Gets save data for all achievements.
        /// </summary>
        /// <returns>List of achievement save data.</returns>
        public List<AchievementSaveData> GetSaveData()
        {
            return _achievements.Values.Select(a => new AchievementSaveData
            {
                AchievementId = a.Id.ToString(),
                IsUnlocked = a.IsComplete,
                CurrentValue = a.CurrentValue,
                UnlockTime = a.UnlockTime?.ToString("O")
            }).ToList();
        }

        /// <summary>
        /// Restores achievement state from save data.
        /// </summary>
        /// <param name="saveData">The save data to restore.</param>
        public void RestoreFromSaveData(List<AchievementSaveData> saveData)
        {
            foreach (var data in saveData)
            {
                if (!Guid.TryParse(data.AchievementId, out var id)) continue;

                var achievement = GetAchievement(id);
                if (achievement == null) continue;

                DateTime? unlockTime = null;
                if (!string.IsNullOrEmpty(data.UnlockTime) &&
                    DateTime.TryParse(data.UnlockTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                {
                    unlockTime = parsed;
                }

                achievement.RestoreState(data.IsUnlocked, data.CurrentValue, unlockTime);
            }

            QuestLogger.Log(LogSubsystem.Achievement, $"Restored {saveData.Count} achievement states.");
        }

        #endregion

        #region Event Handlers

        private void HandleAchievementUnlocked(IObjective achievement)
        {
            OnAchievementUnlocked?.Invoke(achievement as AchievementRuntime);
        }

        private void HandleProgressUpdated(IObjective achievement)
        {
            OnAchievementProgressChanged?.Invoke(achievement as AchievementRuntime);
        }

        #endregion

        #region Editor Support

#if ODIN_INSPECTOR && UNITY_EDITOR
        [TitleGroup("Debug"), Button("Unlock First Achievement"), PropertyOrder(100)]
        private void DebugUnlockFirst()
        {
            if (_achievements.Count > 0)
            {
                var first = _achievements.Values.FirstOrDefault(a => !a.IsComplete);
                first?.Unlock();
            }
        }

        [TitleGroup("Debug"), Button("Increment First Progressive"), PropertyOrder(101)]
        private void DebugIncrementFirst()
        {
            var progressive = _achievements.Values
                .FirstOrDefault(a => a.Data.AchievementType == AchievementType.Progressive && !a.IsComplete);
            progressive?.IncrementProgress();
        }

        [TitleGroup("Debug"), Button("Reset All Progress"), PropertyOrder(102)]
        private void DebugResetProgress()
        {
            ResetAllProgress();
        }

        [TitleGroup("Debug"), ShowInInspector, ReadOnly, PropertyOrder(103)]
        private string DebugStats => $"Unlocked: {UnlockedAchievements?.Count ?? 0}/{_achievements.Count} | Points: {TotalPointsEarned}/{TotalPointsPossible}";
#endif

        #endregion
    }
}
