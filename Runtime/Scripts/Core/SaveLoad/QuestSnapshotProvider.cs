using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using HelloDev.Saving;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Provides snapshot capture and restore functionality for the quest system.
    /// Implements ISaveableSystem for integration with the unified save system.
    /// Created and owned by QuestManager - not a MonoBehaviour.
    /// </summary>
    public class QuestSnapshotProvider : ISaveableSystem
    {
        #region Fields

        private readonly QuestManager _questManager;
        private readonly WorldFlagLocator_SO _worldFlagLocator;
        private readonly Func<List<WorldFlagBase_SO>> _getWorldFlags;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new QuestSnapshotProvider.
        /// </summary>
        /// <param name="questManager">The QuestManager to capture/restore state from.</param>
        /// <param name="worldFlagLocator">The locator for accessing world flag runtime values.</param>
        /// <param name="getWorldFlags">Function that returns all world flags to save.</param>
        public QuestSnapshotProvider(
            QuestManager questManager,
            WorldFlagLocator_SO worldFlagLocator,
            Func<List<WorldFlagBase_SO>> getWorldFlags)
        {
            _questManager = questManager ?? throw new ArgumentNullException(nameof(questManager));
            _worldFlagLocator = worldFlagLocator;
            _getWorldFlags = getWorldFlags ?? (() => new List<WorldFlagBase_SO>());
        }

        #endregion

        #region ISaveableSystem Implementation

        /// <inheritdoc />
        public string SystemKey => "quests";

        /// <inheritdoc />
        public int SavePriority => 100;

        /// <inheritdoc />
        public Type SnapshotType => typeof(QuestSystemSnapshot);

        /// <inheritdoc />
        public object CaptureSnapshot()
        {
            return CaptureQuestSnapshot();
        }

        /// <inheritdoc />
        public bool RestoreSnapshot(object snapshot)
        {
            if (snapshot is QuestSystemSnapshot questSnapshot)
            {
                return RestoreQuestSnapshot(questSnapshot);
            }

            QuestLogger.LogWarning(LogSubsystem.Save, $"Invalid snapshot type: {snapshot?.GetType().Name ?? "null"}");
            return false;
        }

        /// <inheritdoc />
        public void OnBeforeSave()
        {
            // Nothing to do before save
        }

        /// <inheritdoc />
        public void OnAfterSave(bool success)
        {
            // Nothing to do after save
        }

        /// <inheritdoc />
        public void OnBeforeLoad()
        {
            // Nothing to do before load
        }

        /// <inheritdoc />
        public void OnAfterLoad(bool success)
        {
            // Nothing to do after load
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Captures the current state of the quest system.
        /// </summary>
        /// <param name="force">If true, captures even during unsafe operations (may produce inconsistent snapshot).</param>
        /// <returns>A snapshot of the current quest system state, or null if capture is unsafe and not forced.</returns>
        public QuestSystemSnapshot CaptureQuestSnapshot(bool force = false)
        {
            if (_questManager == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Save, "QuestManager not available, snapshot empty");
                return new QuestSystemSnapshot { Version = 1, Timestamp = DateTime.UtcNow.ToString("O") };
            }

            // Check if it's safe to capture
            if (!force && !_questManager.IsSafeForSaveLoad)
            {
                if (_questManager.IsProcessingEvents)
                {
                    QuestLogger.LogWarning(LogSubsystem.Save, "Cannot capture snapshot while processing events. Skipping save.");
                }
                else if (_questManager.IsAnyQuestTransitioning)
                {
                    QuestLogger.LogWarning(LogSubsystem.Save, "Cannot capture snapshot during stage transition. Skipping save.");
                }
                return null;
            }

            var worldFlags = _getWorldFlags();

            var snapshot = SnapshotCapturer.CaptureFullSnapshot(
                _questManager.GetActiveQuests(),
                _questManager.GetCompletedQuests(),
                _questManager.GetFailedQuests(),
                _questManager.GetActiveQuestLines(),
                _questManager.GetCompletedQuestLines(),
                worldFlags,
                _worldFlagLocator
            );

            snapshot.Version = 1;

            QuestLogger.Log(LogSubsystem.Save, $"Captured: <b>{snapshot.ActiveQuests.Count}</b> active, <b>{snapshot.CompletedQuests.Count}</b> completed, <b>{snapshot.FailedQuests.Count}</b> failed");

            return snapshot;
        }

        /// <summary>
        /// Restores the quest system state from a snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore.</param>
        /// <returns>True if restoration was successful.</returns>
        public bool RestoreQuestSnapshot(QuestSystemSnapshot snapshot)
        {
            if (snapshot == null)
            {
                QuestLogger.LogError(LogSubsystem.Save, "Cannot restore null snapshot");
                return false;
            }

            if (_questManager == null)
            {
                QuestLogger.LogError(LogSubsystem.Save, "QuestManager not available");
                return false;
            }

            // Prevent loading while events are being processed to avoid invalid state
            if (_questManager.IsProcessingEvents)
            {
                QuestLogger.LogError(LogSubsystem.Save, "Cannot restore snapshot while quest events are being processed. " +
                    "Defer the load until event processing completes.");
                return false;
            }

            QuestLogger.Log(LogSubsystem.Save, $"Restoring snapshot from '{snapshot.Timestamp}'...");

            try
            {
                // Clear current state and reinitialize
                _questManager.ShutdownManager();
                _questManager.InitializeManager(_questManager.QuestsDatabase.ToList(), isRestore: true);

                var worldFlags = _getWorldFlags();

                // Restore world flags first (quests may depend on them)
                SnapshotRestorer.RestoreWorldFlags(snapshot.WorldFlags, worldFlags, _worldFlagLocator);

                // Restore quests
                SnapshotRestorer.RestoreQuests(snapshot.ActiveQuests, QuestState.InProgress, _questManager, FindQuestByGuid);
                SnapshotRestorer.RestoreQuests(snapshot.CompletedQuests, QuestState.Completed, _questManager, FindQuestByGuid);
                SnapshotRestorer.RestoreQuests(snapshot.FailedQuests, QuestState.Failed, _questManager, FindQuestByGuid);

                // Restore questlines
                SnapshotRestorer.RestoreQuestLines(snapshot.ActiveQuestLines, _questManager, FindQuestLineByGuid);
                SnapshotRestorer.RestoreQuestLines(snapshot.CompletedQuestLines, _questManager, FindQuestLineByGuid);

                // Re-subscribe NotStarted quests to their start condition events
                // This must happen AFTER all quests are restored to prevent events from triggering auto-start during restore
                _questManager.ResubscribeNotStartedQuestsToEvents();

                // Evaluate database quests not in save data - they may now qualify to start
                // (e.g., a quest whose start condition depends on a completed quest or world flag)
                _questManager.EvaluateUnstartedDatabaseQuests();
                
                QuestLogger.Log(LogSubsystem.Save, $"Load from <b>'{snapshot.Timestamp}'</b> {true}");
                return true;
            }
            catch (Exception ex)
            {
                QuestLogger.LogError(LogSubsystem.Save, $"Restore failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates a snapshot before restoration. Use this to check if a save file
        /// is compatible with the current game version.
        /// </summary>
        /// <param name="snapshot">The snapshot to validate.</param>
        /// <returns>Validation result with any issues found.</returns>
        public SnapshotValidationResult ValidateSnapshot(QuestSystemSnapshot snapshot)
        {
            if (_questManager == null)
            {
                var result = new SnapshotValidationResult();
                result.AddCritical("QuestManager", "QuestManager not available.");
                return result;
            }

            return SnapshotValidator.Validate(
                snapshot,
                FindQuestByGuid,
                FindQuestLineByGuid,
                _getWorldFlags()
            );
        }

        #endregion

        #region Private Methods

        private Quest_SO FindQuestByGuid(string guidString)
        {
            if (!Guid.TryParse(guidString, out var guid)) return null;
            return _questManager?.QuestsDatabase.FirstOrDefault(q => q.QuestId == guid);
        }

        private QuestLine_SO FindQuestLineByGuid(string guidString)
        {
            if (!Guid.TryParse(guidString, out var guid)) return null;
            return _questManager?.QuestLinesDatabase.FirstOrDefault(l => l.QuestLineId == guid);
        }

        #endregion
    }
}
