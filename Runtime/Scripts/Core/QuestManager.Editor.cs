using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// Partial class containing Odin Inspector editor functionality for QuestManager.
    /// This separates editor/debug concerns from core runtime logic.
    /// </summary>
    public partial class QuestManager
    {
        #region Runtime State Display (Odin)

#if ODIN_INSPECTOR
        [TitleGroup("Runtime State")]
        [PropertyOrder(50)]
        [ShowInInspector, ReadOnly]
        [InfoBox("Runtime state is only visible during Play mode.", InfoMessageType.Info, nameof(IsNotPlaying))]
        private string StatusSummary => Application.isPlaying
            ? $"Active: {ActiveQuestCount} | Completed: {CompletedQuestCount} | Failed: {FailedQuestCount}"
            : "Not in Play mode";

        [TitleGroup("Runtime State")]
        [PropertyOrder(51)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithActiveQuests))]
        private List<string> ActiveQuestNames => _activeQuests.Values
            .Select(q => $"{q.QuestData.DevName} ({q.CurrentState})")
            .ToList() ?? new List<string>();

        [TitleGroup("Runtime State")]
        [PropertyOrder(52)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithCompletedQuests))]
        private List<string> CompletedQuestNames => _completedQuests.Values
            .Select(q => q.QuestData.DevName)
            .ToList() ?? new List<string>();

        [TitleGroup("Runtime State")]
        [PropertyOrder(53)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithFailedQuests))]
        private List<string> FailedQuestNames => _failedQuests.Values
            .Select(q => q.QuestData.DevName)
            .ToList() ?? new List<string>();

        private bool IsNotPlaying => !Application.isPlaying;
        private bool IsPlayingWithActiveQuests => Application.isPlaying && _activeQuests.Count > 0;
        private bool IsPlayingWithCompletedQuests => Application.isPlaying && _completedQuests.Count > 0;
        private bool IsPlayingWithFailedQuests => Application.isPlaying && _failedQuests.Count > 0;

        private string GetDatabaseInfoMessage()
        {
            if (questsDatabase == null || questsDatabase.Count == 0)
                return "No quests in database. Add Quest_SO assets to enable quests.";
            int validCount = questsDatabase.Count(q => q != null);
            return $"{validCount} quest(s) in database.";
        }

        private string GetQuestLineDatabaseInfoMessage()
        {
            if (questLinesDatabase == null || questLinesDatabase.Count == 0)
                return "No questlines in database. Add QuestLine_SO assets to enable questlines.";
            int validCount = questLinesDatabase.Count(l => l != null);
            return $"{validCount} questline(s) in database.";
        }

        [TitleGroup("QuestLine Runtime State")]
        [PropertyOrder(55)]
        [ShowInInspector, ReadOnly]
        [ShowIf(nameof(IsPlayingWithActiveQuestLines))]
        private string QuestLineStatusSummary => Application.isPlaying
            ? $"Active: {ActiveQuestLineCount} | Completed: {CompletedQuestLineCount}"
            : "Not in Play mode";

        [TitleGroup("QuestLine Runtime State")]
        [PropertyOrder(56)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithActiveQuestLines))]
        private List<string> ActiveQuestLineNames => _activeQuestLines.Values
            .Select(l => $"{l.Data.DevName} ({l.Progress:P0})")
            .ToList() ?? new List<string>();

        [TitleGroup("QuestLine Runtime State")]
        [PropertyOrder(57)]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = true)]
        [ShowIf(nameof(IsPlayingWithCompletedQuestLines))]
        private List<string> CompletedQuestLineNames => _completedQuestLines.Values
            .Select(l => l.Data.DevName)
            .ToList() ?? new List<string>();

        private bool IsPlayingWithActiveQuestLines => Application.isPlaying && _activeQuestLines.Count > 0;
        private bool IsPlayingWithCompletedQuestLines => Application.isPlaying && _completedQuestLines.Count > 0;
#endif

        #endregion

        #region Debug Actions (Odin)

#if ODIN_INSPECTOR
        [TitleGroup("Debug Actions")]
        [PropertyOrder(60)]
        [Button("Complete All Active Quests", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithActiveQuests))]
        private void DebugCompleteAllQuests()
        {
            var questIds = _activeQuests.Keys.ToList();
            foreach (var questId in questIds)
            {
                var quest = _activeQuests[questId];
                quest.ForceComplete();
            }
            Debug.Log("[QuestManager] All active quests completed.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(61)]
        [Button("Fail All Active Quests", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithActiveQuests))]
        private void DebugFailAllQuests()
        {
            var questIds = _activeQuests.Keys.ToList();
            foreach (var questId in questIds)
            {
                var quest = _activeQuests[questId];
                quest.FailQuest();
            }
            Debug.Log("[QuestManager] All active quests failed.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(62)]
        [Button("Increment Current Task (All Quests)", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithActiveQuests))]
        private void DebugIncrementAllCurrentTasks()
        {
            foreach (var quest in _activeQuests.Values)
            {
                quest.IncrementCurrentTask();
            }
            Debug.Log("[QuestManager] Incremented current task for all active quests.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(63)]
        [Button("Restart All Failed Quests", ButtonSizes.Medium)]
        [EnableIf(nameof(IsPlayingWithFailedQuests))]
        private void DebugRestartFailedQuests()
        {
            var failedQuestData = _failedQuests.Values.Select(q => q.QuestData).ToList();
            _failedQuests.Clear();
            foreach (var questData in failedQuestData)
            {
                AddQuest(questData, forceStart: true);
            }
            Debug.Log("[QuestManager] All failed quests restarted.");
        }

        [TitleGroup("Debug Actions")]
        [PropertyOrder(64)]
        [Button("Log State to Console", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        private void DebugLogState()
        {
            Debug.Log($"[QuestManager] === Current State ===");
            Debug.Log($"Active Quests ({ActiveQuestCount}):");
            foreach (var quest in _activeQuests.Values)
            {
                var currentTask = quest.CurrentTask;
                Debug.Log($"  - {quest.QuestData.DevName}: {quest.CurrentState} | Current Task: {currentTask?.DevName ?? "None"} | Progress: {quest.CurrentProgress:P0}");
            }
            Debug.Log($"Completed Quests ({CompletedQuestCount}):");
            foreach (var quest in _completedQuests.Values)
            {
                Debug.Log($"  - {quest.QuestData.DevName}");
            }
            Debug.Log($"Failed Quests ({FailedQuestCount}):");
            foreach (var quest in _failedQuests.Values)
            {
                Debug.Log($"  - {quest.QuestData.DevName}");
            }
        }

        private IEnumerable<Quest_SO> GetAvailableQuests()
        {
            return questsDatabase.Where(q => q != null) ?? Enumerable.Empty<Quest_SO>();
        }
#endif

        #endregion

        #region Runtime Actions (Odin Buttons)

#if ODIN_INSPECTOR
        [TitleGroup("Runtime Actions")]
        [Button("Add Quest")]
        [PropertyOrder(70)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        private void DebugAddQuest(
            [ValueDropdown(nameof(GetAvailableQuests))]
            Quest_SO questData)
        {
            AddQuest(questData, forceStart: true);
        }

        [TitleGroup("Runtime Actions")]
        [Button("Increment Task Step")]
        [PropertyOrder(71)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        private void DebugIncrementTaskStep(
            [ValueDropdown(nameof(GetAvailableQuests))]
            Quest_SO questData)
        {
            var quest = GetActiveQuest(questData);
            quest?.IncrementCurrentTask();
        }
#endif

        #endregion
    }
}
