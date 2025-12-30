using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample
{
    /// <summary>
    /// UI component representing a quest section that groups quests by type.
    /// Acts as a visual container with optional expand/collapse functionality.
    /// Quest items within this section participate in the main navigation flow.
    /// </summary>
    public class UI_QuestSection : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Section Display")]
        [SerializeField] private LocalizeStringEvent sectionNameText;
        [SerializeField] private Image sectionBackground;

        [Header("Quest Items")]
        [SerializeField] private UI_QuestItem questItemPrefab;
        [SerializeField] private RectTransform questItemHolder;

        [Header("Expand/Collapse (Optional)")]
        [SerializeField] private UIToggle expandToggle;
        [SerializeField] private GameObject contentContainer;

        #endregion

        #region Private Fields

        private readonly Dictionary<QuestRuntime, UI_QuestItem> _questItems = new();
        private ToggleGroup _questToggleGroup;
        private bool _isExpanded = true;

        #endregion

        #region Public Properties

        public QuestType_SO QuestType { get; private set; }
        public IReadOnlyDictionary<QuestRuntime, UI_QuestItem> QuestItems => _questItems;
        public int QuestCount => _questItems.Count;
        public bool IsExpanded => _isExpanded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Subscribe to expand/collapse if toggle exists
            if (expandToggle != null)
            {
                expandToggle.OnToggleOn.AddListener(HandleExpand);
                expandToggle.OnToggleOff.AddListener(HandleCollapse);
            }
        }

        private void OnDestroy()
        {
            if (expandToggle != null)
            {
                expandToggle.OnToggleOn.RemoveListener(HandleExpand);
                expandToggle.OnToggleOff.RemoveListener(HandleCollapse);
            }
        }

        #endregion

        #region Public Methods - Setup

        /// <summary>
        /// Configures the section with a quest type and applies visual styling.
        /// </summary>
        public void Setup(QuestType_SO questType)
        {
            if (questType == null) return;

            QuestType = questType;
            gameObject.name = $"Section_{questType.name}";

            if (sectionNameText != null)
            {
                sectionNameText.StringReference = questType.DisplayName;
                sectionNameText.RefreshString();
            }

            if (sectionBackground != null)
                sectionBackground.color = questType.Color;
        }

        /// <summary>
        /// Sets the toggle group that all quest items in this section will use.
        /// This should be a shared group across all sections for proper selection behavior.
        /// </summary>
        public void SetQuestToggleGroup(ToggleGroup toggleGroup)
        {
            _questToggleGroup = toggleGroup;

            // Update existing items
            foreach (var questItem in _questItems.Values)
            {
                questItem.SetToggleGroup(toggleGroup);
            }
        }

        /// <summary>
        /// Creates UI items for a list of quests.
        /// </summary>
        public void SpawnQuestItems(List<QuestRuntime> quests, Action<QuestRuntime> onQuestSelected)
        {
            if (quests == null) return;

            Debug.Log($"[UI_QuestSection] SpawnQuestItems called with {quests.Count} quests for section '{QuestType?.name ?? "null"}'");

            foreach (QuestRuntime quest in quests)
            {
                Debug.Log($"[UI_QuestSection]   Creating item for '{quest.QuestData.DevName}': State={quest.CurrentState}");
                CreateQuestItem(quest, onQuestSelected);
            }
        }

        #endregion

        #region Public Methods - Quest Management

        /// <summary>
        /// Adds a single quest to this section.
        /// </summary>
        public void AddQuest(QuestRuntime quest, Action<QuestRuntime> onQuestSelected)
        {
            if (quest?.QuestData == null) return;

            // Auto-setup if this is the first quest
            if (QuestType == null && quest.QuestData.QuestType != null)
                Setup(quest.QuestData.QuestType);

            CreateQuestItem(quest, onQuestSelected);
        }

        /// <summary>
        /// Removes a quest from this section.
        /// </summary>
        public void RemoveQuest(QuestRuntime quest)
        {
            if (quest == null || !_questItems.TryGetValue(quest, out UI_QuestItem questItem))
                return;

            _questItems.Remove(quest);
            if (questItem != null)
                Destroy(questItem.gameObject);
        }

        /// <summary>
        /// Clears all quests from this section.
        /// </summary>
        public void ClearAllQuests()
        {
            foreach (UI_QuestItem questItem in _questItems.Values)
            {
                if (questItem != null)
                    Destroy(questItem.gameObject);
            }
            _questItems.Clear();
        }

        /// <summary>
        /// Gets the first quest in this section.
        /// </summary>
        public QuestRuntime GetFirstQuest()
        {
            return _questItems.Values.FirstOrDefault()?.Quest;
        }

        /// <summary>
        /// Gets the first quest item UI component in this section.
        /// </summary>
        public UI_QuestItem GetFirstQuestItem()
        {
            return _questItems.Values.FirstOrDefault();
        }

        /// <summary>
        /// Checks if this section contains the specified quest.
        /// </summary>
        public bool ContainsQuest(QuestRuntime quest)
        {
            return _questItems.ContainsKey(quest);
        }

        /// <summary>
        /// Gets all quests in this section.
        /// </summary>
        public IEnumerable<QuestRuntime> GetAllQuests()
        {
            return _questItems.Keys;
        }

        /// <summary>
        /// Tries to get the UI item for a specific quest.
        /// </summary>
        public bool TryGetQuestItem(QuestRuntime quest, out UI_QuestItem questItem)
        {
            return _questItems.TryGetValue(quest, out questItem);
        }

        #endregion

        #region Public Methods - Navigation

        /// <summary>
        /// Selects the first quest in this section.
        /// </summary>
        public void SelectFirstQuest()
        {
            var firstItem = _questItems.Values.FirstOrDefault();
            firstItem?.SelectQuest();
        }

        /// <summary>
        /// Expands the section to show quest items.
        /// </summary>
        public void Expand()
        {
            if (contentContainer != null)
                contentContainer.SetActive(true);

            if (expandToggle != null)
                expandToggle.SetIsOn(true);

            _isExpanded = true;
        }

        /// <summary>
        /// Collapses the section to hide quest items.
        /// </summary>
        public void Collapse()
        {
            if (contentContainer != null)
                contentContainer.SetActive(false);

            if (expandToggle != null)
                expandToggle.SetIsOn(false);

            _isExpanded = false;
        }

        #endregion

        #region Private Methods

        private UI_QuestItem CreateQuestItem(QuestRuntime quest, Action<QuestRuntime> onQuestSelected)
        {
            // Return existing item if already created
            if (_questItems.TryGetValue(quest, out UI_QuestItem existing))
                return existing;

            UI_QuestItem questItem = Instantiate(questItemPrefab, questItemHolder);
            questItem.Setup(quest, onQuestSelected);

            // Use shared toggle group for proper selection across all sections
            if (_questToggleGroup != null)
                questItem.SetToggleGroup(_questToggleGroup);

            if (QuestType != null)
                questItem.SetupMarkerColour(QuestType.Color);

            _questItems.Add(quest, questItem);
            return questItem;
        }

        private void HandleExpand()
        {
            if (contentContainer != null)
                contentContainer.SetActive(true);
            _isExpanded = true;
        }

        private void HandleCollapse()
        {
            if (contentContainer != null)
                contentContainer.SetActive(false);
            _isExpanded = false;
        }

        #endregion

        #region Legacy Compatibility

        /// <summary>
        /// Legacy method - use SetQuestToggleGroup instead.
        /// </summary>
        [Obsolete("Use SetQuestToggleGroup instead")]
        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            SetQuestToggleGroup(toggleGroup);
        }

        /// <summary>
        /// Legacy method - use SpawnQuestItems instead.
        /// </summary>
        [Obsolete("Use SpawnQuestItems instead")]
        public void SpawnQuestsItems(List<QuestRuntime> quests, Action<QuestRuntime> onQuestSelected)
        {
            SpawnQuestItems(quests, onQuestSelected);
        }

        /// <summary>
        /// Legacy method - use SelectFirstQuest instead.
        /// </summary>
        [Obsolete("Use SelectFirstQuest instead")]
        public void Select()
        {
            SelectFirstQuest();
        }

        #endregion
    }
}