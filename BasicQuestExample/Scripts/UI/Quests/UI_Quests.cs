using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    /// <summary>
    /// Main UI controller for the quest journal.
    /// Manages quest sections, selection state, and coordinates between
    /// the quest list and quest details panels.
    /// </summary>
    public class UI_Quests : UIContainer
    {
        #region Serialized Fields

        [Header("Display Settings")]
        [SerializeField] private bool showEmptySections = true;

        [Header("Quest Sections")]
        [SerializeField] private RectTransform questSectionHolder;
        [SerializeField] private UI_QuestSection questSectionPrefab;
        [SerializeField] private QuestType_SO completedQuestType;

        [Header("Quest Selection")]
        [SerializeField] private ToggleGroup questItemToggleGroup;

        [Header("Quest Details")]
        [SerializeField] private UI_QuestDetails questDetails;

        [Header("Navigation")]
        [SerializeField] private UIButton questBackButton;

        #endregion

        #region Private Fields

        private readonly Dictionary<QuestType_SO, UI_QuestSection> _sections = new();
        private readonly List<UI_QuestItem> _allQuestItems = new();
        private QuestRuntime _selectedQuest;
        private int _selectedIndex = -1;
        private GameObject _lastValidSelection;

        #endregion

        #region Public Properties

        public QuestRuntime SelectedQuest => _selectedQuest;
        public UI_QuestDetails QuestDetails => questDetails;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            onStartShow.AddListener(SetupQuestUI);
            onStartHide.AddListener(ClearSelectionTracking);

            if (questBackButton != null)
                questBackButton.OnClick.AddListener(HandleBackButton);

            // Prevent deselection when clicking on already-selected quest
            if (questItemToggleGroup != null)
                questItemToggleGroup.allowSwitchOff = false;
        }

        private void ClearSelectionTracking()
        {
            _lastValidSelection = null;
        }

        private void LateUpdate()
        {
            // Maintain selection for controller navigation
            MaintainSelection();
        }

        private void MaintainSelection()
        {
            if (EventSystem.current == null) return;

            var currentSelection = EventSystem.current.currentSelectedGameObject;

            // Track valid selections within our UI
            if (currentSelection != null && IsOurSelection(currentSelection))
            {
                _lastValidSelection = currentSelection;
            }
            // Restore selection if lost (null or outside our UI)
            else if (_lastValidSelection != null && _lastValidSelection.activeInHierarchy)
            {
                // Only restore if nothing is selected or selection is outside quest UI
                if (currentSelection == null || !IsOurSelection(currentSelection))
                {
                    EventSystem.current.SetSelectedGameObject(_lastValidSelection);
                }
            }
        }

        private bool IsOurSelection(GameObject obj)
        {
            if (obj == null) return false;

            // Check if selection is a quest item, task item, or debug button within our UI
            return obj.GetComponentInParent<UI_Quests>() == this ||
                   obj.GetComponentInParent<UI_QuestDetails>() == questDetails;
        }

        protected override void Start()
        {
            base.Start();
            SubscribeToQuestEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromQuestEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Selects a specific quest and updates the UI.
        /// </summary>
        public void SelectQuest(QuestRuntime quest)
        {
            if (quest == null || _selectedQuest == quest) return;

            _selectedQuest = quest;
            _selectedIndex = _allQuestItems.FindIndex(item => item.Quest == quest);

            // Update details panel
            questDetails?.Setup(quest);

            // Update quest item selection
            foreach (var item in _allQuestItems)
            {
                item.SetToggleIsOn(item.Quest == quest);
            }
        }

        /// <summary>
        /// Navigates to the next quest in the list.
        /// </summary>
        public void SelectNextQuest()
        {
            if (_allQuestItems.Count == 0) return;

            _selectedIndex = (_selectedIndex + 1) % _allQuestItems.Count;
            SelectQuestAtIndex(_selectedIndex);
        }

        /// <summary>
        /// Navigates to the previous quest in the list.
        /// </summary>
        public void SelectPreviousQuest()
        {
            if (_allQuestItems.Count == 0) return;

            _selectedIndex = _selectedIndex <= 0 ? _allQuestItems.Count - 1 : _selectedIndex - 1;
            SelectQuestAtIndex(_selectedIndex);
        }

        /// <summary>
        /// Gets all quest items across all sections.
        /// </summary>
        public IReadOnlyList<UI_QuestItem> GetAllQuestItems() => _allQuestItems;

        #endregion

        #region Private Methods - Setup

        private void SetupQuestUI()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("[UI_Quests] QuestManager.Instance is null, cannot setup quest UI.");
                return;
            }

            Debug.Log("[UI_Quests] SetupQuestUI called (v2 - with debug logs)");

            // Debug: Log all quests from GetActiveQuests
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            Debug.Log($"[UI_Quests] GetActiveQuests returned {activeQuests.Count} quests:");
            foreach (var q in activeQuests)
            {
                Debug.Log($"[UI_Quests]   - '{q.QuestData.DevName}': State={q.CurrentState}");
            }

            ClearQuestSections();
            _sections.Clear();
            _allQuestItems.Clear();

            if (showEmptySections)
                CreateEmptySections();

            CreateActiveSections();
            CreateCompletedSection();

            // Build flat list for navigation
            RebuildQuestItemList();

            // Select first quest
            SelectFirstAvailableQuest();
        }

        private void CreateEmptySections()
        {
            var allQuestTypes = QuestManager.Instance.QuestsDatabase
                .Where(q => q != null && q.QuestType != null)
                .Select(q => q.QuestType)
                .Distinct()
                .Where(qt => qt != completedQuestType);

            foreach (var questType in allQuestTypes)
            {
                if (questType == null) continue;

                var quests = GetActiveQuestsOfType(questType);
                var section = CreateSection(questType);
                section.SpawnQuestItems(quests, HandleQuestSelected);
            }
        }

        private void CreateActiveSections()
        {
            var groupedQuests = QuestManager.Instance.GetActiveQuests()
                .Where(q => q.QuestData?.QuestType != null && q.QuestData.QuestType != completedQuestType)
                .GroupBy(q => q.QuestData.QuestType);

            Debug.Log($"[UI_Quests] CreateActiveSections: Processing {groupedQuests.Count()} quest type groups");

            foreach (var group in groupedQuests)
            {
                Debug.Log($"[UI_Quests] Group '{group.Key.name}': {group.Count()} quests");
                foreach (var q in group)
                {
                    Debug.Log($"[UI_Quests]   - '{q.QuestData.DevName}': State={q.CurrentState}");
                }

                var section = GetOrCreateSection(group.Key);
                section.SpawnQuestItems(group.ToList(), HandleQuestSelected);
            }
        }

        private void CreateCompletedSection()
        {
            if (completedQuestType == null) return;

            var completedQuests = QuestManager.Instance.GetCompletedQuests().ToList();
            if (!showEmptySections && completedQuests.Count == 0) return;

            var section = GetOrCreateSection(completedQuestType);
            section.SpawnQuestItems(completedQuests, HandleQuestSelected);
        }

        private UI_QuestSection CreateSection(QuestType_SO questType)
        {
            var section = Instantiate(questSectionPrefab, questSectionHolder);
            section.Setup(questType);
            section.SetQuestToggleGroup(questItemToggleGroup);
            _sections.TryAdd(questType, section);
            return section;
        }

        private UI_QuestSection GetOrCreateSection(QuestType_SO questType)
        {
            if (_sections.TryGetValue(questType, out var existing))
                return existing;

            return CreateSection(questType);
        }

        private void ClearQuestSections()
        {
            questSectionHolder?.DestroyAllChildren();
        }

        private void RebuildQuestItemList()
        {
            _allQuestItems.Clear();

            foreach (var section in _sections.Values)
            {
                _allQuestItems.AddRange(section.QuestItems.Values);
            }
        }

        #endregion

        #region Private Methods - Selection

        private void HandleQuestSelected(QuestRuntime quest)
        {
            SelectQuest(quest);
        }

        private void SelectQuestAtIndex(int index)
        {
            if (index < 0 || index >= _allQuestItems.Count) return;

            var item = _allQuestItems[index];
            item.SelectQuest();
            SelectQuest(item.Quest);
        }

        private void SelectFirstAvailableQuest()
        {
            // Prefer non-completed quests
            var firstActiveSection = _sections.Values
                .FirstOrDefault(s => s.QuestType != completedQuestType && s.QuestCount > 0);

            if (firstActiveSection != null)
            {
                firstActiveSection.SelectFirstQuest();
                return;
            }

            // Fall back to completed section
            var completedSection = _sections.GetValueOrDefault(completedQuestType);
            completedSection?.SelectFirstQuest();
        }

        private QuestRuntime FindNextSelectableQuest()
        {
            // Prioritize active quests
            foreach (var section in _sections.Values)
            {
                if (section.QuestType == completedQuestType) continue;
                var quest = section.GetFirstQuest();
                if (quest != null) return quest;
            }

            // Fall back to completed
            return _sections.GetValueOrDefault(completedQuestType)?.GetFirstQuest();
        }

        #endregion

        #region Private Methods - Navigation

        private void HandleBackButton()
        {
            // If quest details has focus, return focus to quest list
            if (_selectedIndex >= 0 && _selectedIndex < _allQuestItems.Count)
            {
                var item = _allQuestItems[_selectedIndex];
                EventSystem.current?.SetSelectedGameObject(item.Toggle.Toggle.gameObject);
            }
        }

        #endregion

        #region Private Methods - Quest Events

        private void SubscribeToQuestEvents()
        {
            if (QuestManager.Instance == null) return;

            QuestManager.Instance.QuestStarted.SafeSubscribe(HandleQuestStarted);
            QuestManager.Instance.QuestCompleted.SafeSubscribe(HandleQuestCompleted);
            QuestManager.Instance.QuestRestarted.SafeSubscribe(HandleQuestRestarted);
            QuestManager.Instance.QuestFailed.SafeSubscribe(HandleQuestFailed);
            QuestManager.Instance.QuestRemoved.SafeSubscribe(HandleQuestRemoved);
        }

        private void UnsubscribeFromQuestEvents()
        {
            if (QuestManager.Instance == null) return;

            QuestManager.Instance.QuestStarted.SafeUnsubscribe(HandleQuestStarted);
            QuestManager.Instance.QuestCompleted.SafeUnsubscribe(HandleQuestCompleted);
            QuestManager.Instance.QuestRestarted.SafeUnsubscribe(HandleQuestRestarted);
            QuestManager.Instance.QuestFailed.SafeUnsubscribe(HandleQuestFailed);
            QuestManager.Instance.QuestRemoved.SafeUnsubscribe(HandleQuestRemoved);
        }

        private void HandleQuestStarted(QuestRuntime quest)
        {
            if (quest?.QuestData?.QuestType == null) return;

            var section = GetOrCreateSection(quest.QuestData.QuestType);
            section.AddQuest(quest, HandleQuestSelected);

            RebuildQuestItemList();

            // Auto-select if nothing selected
            if (_selectedQuest == null)
                SelectQuest(quest);
        }

        private void HandleQuestCompleted(QuestRuntime quest)
        {
            if (quest?.QuestData?.QuestType == null || completedQuestType == null) return;

            // Remove from original section
            var originalType = quest.QuestData.QuestType;
            if (_sections.TryGetValue(originalType, out var originalSection))
                originalSection.RemoveQuest(quest);

            // Add to completed section
            var completedSection = GetOrCreateSection(completedQuestType);
            completedSection.AddQuest(quest, HandleQuestSelected);

            RebuildQuestItemList();

            // Update selection if this was selected
            if (_selectedQuest == quest)
            {
                var nextQuest = FindNextSelectableQuest();
                SelectQuest(nextQuest ?? quest);
            }
        }

        private void HandleQuestRestarted(QuestRuntime quest)
        {
            if (quest?.QuestData?.QuestType == null) return;

            // Remove from completed section
            if (_sections.TryGetValue(completedQuestType, out var completedSection))
                completedSection.RemoveQuest(quest);

            // Add back to original section
            var section = GetOrCreateSection(quest.QuestData.QuestType);
            section.AddQuest(quest, HandleQuestSelected);

            RebuildQuestItemList();
            SelectQuest(quest);
        }

        private void HandleQuestFailed(QuestRuntime quest)
        {
            if (quest?.QuestData?.QuestType == null) return;

            // Refresh details if this is the selected quest
            if (_selectedQuest == quest)
                questDetails?.Setup(quest);
        }

        private void HandleQuestRemoved(QuestRuntime quest)
        {
            if (quest?.QuestData?.QuestType == null) return;

            // Remove from section
            if (_sections.TryGetValue(quest.QuestData.QuestType, out var section))
                section.RemoveQuest(quest);

            RebuildQuestItemList();

            // Update selection if this was selected
            if (_selectedQuest == quest)
            {
                _selectedQuest = null;
                var nextQuest = FindNextSelectableQuest();
                if (nextQuest != null)
                    SelectQuest(nextQuest);
            }
        }

        #endregion

        #region Private Methods - Helpers

        private List<QuestRuntime> GetActiveQuestsOfType(QuestType_SO questType)
        {
            return QuestManager.Instance.GetActiveQuests()
                .Where(q => q.QuestData?.QuestType == questType)
                .ToList();
        }

        #endregion
    }
}