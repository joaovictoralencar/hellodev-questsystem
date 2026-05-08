using System;
using HelloDev.Logging;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.Utils;
using HelloDev.Saving;

namespace HelloDev.QuestSystem.Tutorials.SaveLoad
{
    /// <summary>
    /// Provides snapshot capture and restore functionality for the tutorial system.
    /// Implements ISaveableSystem for integration with the unified save system.
    /// Created and owned by TutorialManager - not a MonoBehaviour.
    /// </summary>
    public class TutorialSnapshotProvider : ISaveableSystem
    {
        #region Fields

        private readonly TutorialManager _tutorialManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TutorialSnapshotProvider.
        /// </summary>
        /// <param name="tutorialManager">The TutorialManager to capture/restore state from.</param>
        public TutorialSnapshotProvider(TutorialManager tutorialManager)
        {
            _tutorialManager = tutorialManager ?? throw new ArgumentNullException(nameof(tutorialManager));
        }

        #endregion

        #region ISaveableSystem Implementation

        /// <inheritdoc />
        public string SystemKey => "tutorials";

        /// <inheritdoc />
        public int SavePriority => 110;

        /// <inheritdoc />
        public Type SnapshotType => typeof(TutorialSystemSnapshot);

        /// <inheritdoc />
        public object CaptureSnapshot()
        {
            if (_tutorialManager == null)
            { 
                Logger.LogWarning("Tutorial", "TutorialManager not available, cannot capture snapshot.");
                return null;
            }

            TutorialSystemSnapshot tutorialSystemSnapshot = _tutorialManager.CaptureSnapshot();
            Logger.Log("Save",$"Saved tutorial snapshot: {tutorialSystemSnapshot}");
            return tutorialSystemSnapshot;
        }

        /// <inheritdoc />
        public bool RestoreSnapshot(object snapshot)
        {
            if (_tutorialManager == null)
            {
                Logger.LogWarning("Tutorial", "TutorialManager not available, cannot restore snapshot.");
                return false;
            }

            if (snapshot is TutorialSystemSnapshot tutorialSnapshot)
            {
                try
                {
                    _tutorialManager.RestoreSnapshot(tutorialSnapshot);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError("Tutorial", $"Failed to restore tutorial snapshot: {ex.Message}");
                    return false;
                }
            }

            Logger.LogWarning("Tutorial", $"Invalid snapshot type: {snapshot?.GetType().Name ?? "null"}");
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
    }
}
