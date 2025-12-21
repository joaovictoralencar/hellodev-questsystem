using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    /// <summary>
    /// Main UI controller for displaying quest sections and managing quest selection.
    /// Handles the creation and management of quest sections grouped by quest type,
    /// and provides quest details display functionality.
    /// </summary>
    public class UI_Quests : UIContainer
    {
        #region Serialized Fields

        [Header("Display Settings")] [SerializeField] [Tooltip("Show quest sections even if they have no active quests")]
        private bool showEmptySections = true;

        [Header("Quest Section Management")] [SerializeField] [Tooltip("Parent container for all quest sections")]
        private RectTransform questSectionHolder;

        [SerializeField] [Tooltip("Prefab used to create individual quest sections")]
        private UI_QuestSection questSectionPrefab;

        [SerializeField] [Tooltip("Toggle group to ensure only one quest section is selected at a time")]
        private ToggleGroup questSectionToggleGroup;

        [SerializeField] [Tooltip("Quest type to be used for completed quests")]
        private QuestType_SO completedQuestType;

        [Header("Quest Details Display")] [SerializeField] [Tooltip("UI component that displays detailed information about the selected quest")]
        private UI_QuestDetails questDetails;

        #endregion

        #region Private Fields

        /// <summary>
        /// Dictionary mapping quest types to their corresponding UI sections
        /// </summary>
        private readonly Dictionary<QuestType_SO, UI_QuestSection> questSections = new();

        /// <summary>
        /// Currently selected quest for detailed display
        /// </summary>
        private Quest selectedQuest;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            onStartShow.AddListener(SetupQuestUI);
            base.Awake();

            ClearQuestSections();
        }

        protected override void Start()
        {
            base.Start();
            SubscribeToQuestEvents();
        }

        #endregion

        #region UI Setup and Management

        /// <summary>
        /// Sets up the quest UI by creating sections and populating them with quests.
        /// This method is called when the UI is shown and handles both empty and populated sections
        /// based on the showEmptySections setting.
        /// </summary>
        private void SetupQuestUI()
        {
            ClearQuestSections();
            questSections.Clear();

            if (showEmptySections)
            {
                CreateEmptyQuestSections();
            }

            CreateActiveQuestSections();
            CreateCompletedQuestSection();

            SelectFirstAvailableQuest();
        }

        /// <summary>
        /// Creates quest sections for all available quest types, even if they have no active quests.
        /// Useful for showing the complete quest structure to players.
        /// </summary>
        private void CreateEmptyQuestSections()
        {
            var allQuestTypes = GetAllQuestTypes();

            foreach (var questType in allQuestTypes)
            {
                if (questType == completedQuestType) continue;

                var activeQuests = GetActiveQuestsOfType(questType);
                var questSection = CreateQuestSection();

                questSection.Setup(questType);
                questSection.SpawnQuestsItems(activeQuests, OnQuestSelected);
                questSections.TryAdd(questType, questSection);
            }
        }

        /// <summary>
        /// Creates quest sections only for quest types that have active quests.
        /// More compact display that only shows relevant content.
        /// </summary>
        private void CreateActiveQuestSections()
        {
            var groupedQuests = GroupActiveQuestsByType();

            foreach (var questGroup in groupedQuests)
            {
                QuestType_SO questType = questGroup.Key;
                if (questType == completedQuestType) continue;

                List<Quest> questGroupValue = questGroup.Value;
                AddQuestToSection(questType, questGroupValue);
            }
        }

        private void AddQuestToSection(QuestType_SO questType, List<Quest> questList)
        {
            UI_QuestSection questSection = GetOrCreateQuestSection(questType);
            questSection.Setup(questType);
            questSection.SpawnQuestsItems(questList, OnQuestSelected);
            questSections.TryAdd(questType, questSection);
        }

        /// <summary>
        /// Creates the completed quest section and populates it with completed quests.
        /// </summary>
        private void CreateCompletedQuestSection()
        {
            if (completedQuestType == null) return;

            var completedQuests = GetCompletedQuests();

            if (!showEmptySections && completedQuests.Count == 0) return;

            var completedSection = GetOrCreateQuestSection(completedQuestType);
            completedSection.Setup(completedQuestType);
            completedSection.SpawnQuestsItems(completedQuests, OnQuestSelected);
            questSections.TryAdd(completedQuestType, completedSection);
        }

        /// <summary>
        /// Creates and configures a new quest section UI element.
        /// </summary>
        /// <returns>Configured UI_QuestSection instance</returns>
        private UI_QuestSection CreateQuestSection()
        {
            var questSection = Instantiate(questSectionPrefab, questSectionHolder);
            questSection.SetToggleGroup(questSectionToggleGroup);
            return questSection;
        }

        /// <summary>
        /// Removes all child objects from the quest section holder.
        /// </summary>
        private void ClearQuestSections()
        {
            if (questSectionHolder != null)
            {
                questSectionHolder.DestroyAllChildren();
            }
        }

        #endregion

        #region Quest Management

        /// <summary>
        /// Handles the event when a new quest is started.
        /// Creates or updates the appropriate quest section and adds the new quest.
        /// </summary>
        /// <param name="quest">The newly started quest</param>
        private void OnQuestStarted(Quest quest)
        {
            if (quest?.QuestData?.QuestType == null) return;

            var questType = quest.QuestData.QuestType;
            var questSection = GetOrCreateQuestSection(questType);

            questSection.AddQuest(quest, OnQuestSelected);

            if (selectedQuest == null)
            {
                OnQuestSelected(quest);
            }
        }

        /// <summary>
        /// Handles the event when a quest is completed.
        /// Removes the quest from its current section and moves it to the completed quests section.
        /// </summary>
        /// <param name="quest">The completed quest</param>
        private void OnQuestCompleted(Quest quest)
        {
            if (quest?.QuestData?.QuestType == null || completedQuestType == null) return;

            QuestType_SO originalQuestType = quest.QuestData.QuestType;

            RemoveQuestFromSection(quest, originalQuestType);
            AddQuestToCompletedSection(quest);

            HandleCompletedQuestSelection(quest);
        }


        private void OnQuestRestarted(Quest quest)
        {
            if (quest?.QuestData?.QuestType == null) return;

            QuestType_SO originalQuestType = quest.QuestData.QuestType;

            //try to remove the quest from the completed section
            RemoveQuestFromSection(quest, completedQuestType);
            AddQuestToSection(originalQuestType, new List<Quest>() { quest });
            OnQuestSelected(quest);
        }

        /// <summary>
        /// Handles quest selection and updates the details display.
        /// </summary>
        /// <param name="quest">The selected quest</param>
        private void OnQuestSelected(Quest quest)
        {
            if (selectedQuest == quest) return;

            selectedQuest = quest;

            if (questDetails != null)
            {
                questDetails.Setup(quest);
            }

            foreach (UI_QuestSection section in questSections.Values)
            {
                foreach (UI_QuestItem questItem in section.QuestItems.Values)
                {
                    questItem.SetToggleIsOn(Equals(questItem.Quest, quest));
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets or creates a quest section for the specified quest type.
        /// </summary>
        /// <param name="questType">The quest type to get or create a section for</param>
        /// <returns>The quest section for the specified type</returns>
        private UI_QuestSection GetOrCreateQuestSection(QuestType_SO questType)
        {
            if (questSections.TryGetValue(questType, out var existingSection))
            {
                return existingSection;
            }

            UI_QuestSection newSection = CreateQuestSection();
            questSections.Add(questType, newSection);
            return newSection;
        }

        /// <summary>
        /// Removes a quest from its current section.
        /// </summary>
        /// <param name="quest">The quest to remove</param>
        /// <param name="questType">The quest type section to remove from</param>
        private void RemoveQuestFromSection(Quest quest, QuestType_SO questType)
        {
            if (questSections.TryGetValue(questType, out UI_QuestSection questSection))
            {
                questSection.RemoveQuest(quest);
            }
        }

        /// <summary>
        /// Adds a quest to the completed quests section.
        /// Creates the section if it doesn't exist.
        /// </summary>
        /// <param name="quest">The completed quest to add</param>
        private void AddQuestToCompletedSection(Quest quest)
        {
            var completedSection = GetOrCreateQuestSection(completedQuestType);

            if (!questSections.ContainsKey(completedQuestType))
            {
                completedSection.Setup(completedQuestType);
                questSections.Add(completedQuestType, completedSection);
            }

            completedSection.AddQuest(quest, OnQuestSelected);
        }

        /// <summary>
        /// Handles quest selection logic when a quest is completed.
        /// If the completed quest was selected, attempts to select another quest.
        /// </summary>
        /// <param name="completedQuest">The quest that was just completed</param>
        private void HandleCompletedQuestSelection(Quest completedQuest)
        {
            if (selectedQuest != completedQuest) return;

            var nextQuest = FindNextSelectableQuest();
            if (nextQuest != null)
            {
                OnQuestSelected(nextQuest);
            }
            else
            {
                OnQuestSelected(completedQuest);
            }
        }

        /// <summary>
        /// Finds the next available quest to select when the current selection is completed.
        /// Prioritizes active quests over completed ones.
        /// </summary>
        /// <returns>The next quest to select, or null if none available</returns>
        private Quest FindNextSelectableQuest()
        {
            foreach (var section in questSections.Values)
            {
                var firstQuest = section.GetFirstQuest();
                if (firstQuest != null && section != questSections.GetValueOrDefault(completedQuestType))
                {
                    return firstQuest;
                }
            }

            return questSections.GetValueOrDefault(completedQuestType)?.GetFirstQuest();
        }

        /// <summary>
        /// Gets all available quest types from the quest database.
        /// </summary>
        /// <returns>Collection of all quest types</returns>
        private IEnumerable<QuestType_SO> GetAllQuestTypes()
        {
            return QuestManager.Instance.QuestsDatabase
                .Select(quest => quest.QuestType)
                .Distinct();
        }

        /// <summary>
        /// Gets active quests of a specific type.
        /// </summary>
        /// <param name="questType">The quest type to filter by</param>
        /// <returns>List of active quests of the specified type</returns>
        private List<Quest> GetActiveQuestsOfType(QuestType_SO questType)
        {
            return QuestManager.Instance.GetActiveQuests()
                .Where(quest => quest.QuestData.QuestType == questType)
                .ToList();
        }

        /// <summary>
        /// Gets all completed quests from the quest manager.
        /// </summary>
        /// <returns>List of completed quests</returns>
        private List<Quest> GetCompletedQuests()
        {
            return QuestManager.Instance.GetCompletedQuests().ToList();
        }

        /// <summary>
        /// Groups all active quests by their quest type.
        /// </summary>
        /// <returns>Dictionary mapping quest types to their active quests</returns>
        private Dictionary<QuestType_SO, List<Quest>> GroupActiveQuestsByType()
        {
            var groupedQuests = new Dictionary<QuestType_SO, List<Quest>>();

            foreach (var quest in QuestManager.Instance.GetActiveQuests())
            {
                var questType = quest.QuestData.QuestType;

                if (!groupedQuests.ContainsKey(questType))
                {
                    groupedQuests[questType] = new List<Quest>();
                }

                groupedQuests[questType].Add(quest);
            }

            return groupedQuests;
        }

        /// <summary>
        /// Subscribes to relevant quest manager events.
        /// </summary>
        private void SubscribeToQuestEvents()
        {
            if (QuestManager.Instance == null) return;
            QuestManager.Instance.QuestStarted.SafeSubscribe(OnQuestStarted);
            QuestManager.Instance.QuestCompleted.SafeSubscribe(OnQuestCompleted);
            QuestManager.Instance.QuestRestarted.SafeSubscribe(OnQuestRestarted);
        }

        /// <summary>
        /// Automatically selects the first available quest if any exist.
        /// </summary>
        private void SelectFirstAvailableQuest()
        {
            UI_QuestSection firstSection = questSections.Values.FirstOrDefault();
            firstSection?.Select();
        }

        #endregion
    }
}