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
        
        [Header("Display Settings")]
        [SerializeField] 
        [Tooltip("Show quest sections even if they have no active quests")]
        private bool showEmptySections = true;
        
        [Header("Quest Section Management")]
        [SerializeField] 
        [Tooltip("Parent container for all quest sections")]
        private RectTransform questSectionHolder;
        
        [SerializeField] 
        [Tooltip("Prefab used to create individual quest sections")]
        private UI_QuestSection questSectionPrefab;
        
        [SerializeField] 
        [Tooltip("Toggle group to ensure only one quest section is selected at a time")]
        private ToggleGroup questSectionToggleGroup;

        [Header("Quest Details Display")]
        [SerializeField] 
        [Tooltip("UI component that displays detailed information about the selected quest")]
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
            // Subscribe to show event to setup the UI when displayed
            onStartShow.AddListener(SetupQuestUI);
            base.Awake();
            
            // Ensure clean state on initialization
            ClearQuestSections();
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
            // Clear existing content
            ClearQuestSections();
            questSections.Clear();

            // Create quest sections based on configuration
            if (showEmptySections)
            {
                CreateEmptyQuestSections();
            }
            
            CreateActiveQuestSections();

            // Subscribe to quest events
            SubscribeToQuestEvents();
            
            // Auto-select first available quest
            SelectFirstAvailableQuest();
        }

        /// <summary>
        /// Creates quest sections for all available quest types, even if they have no active quests.
        /// Useful for showing the complete quest structure to players.
        /// </summary>
        private void CreateEmptyQuestSections()
        {
            IEnumerable<QuestType_SO> allQuestTypes = GetAllQuestTypes();
            
            foreach (QuestType_SO questType in allQuestTypes)
            {
                List<Quest> activeQuests = GetActiveQuestsOfType(questType);
                UI_QuestSection questSection = CreateQuestSection();
                
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
            Dictionary<QuestType_SO, List<Quest>> groupedQuests = GroupActiveQuestsByType();
            
            foreach (KeyValuePair<QuestType_SO, List<Quest>> questGroup in groupedQuests)
            {
                QuestType_SO questType = questGroup.Key;
                UI_QuestSection questSection = GetOrCreateQuestSection(questType);
                questSection.Setup(questType);
                questSection.SpawnQuestsItems(questGroup.Value, OnQuestSelected);
                questSections.TryAdd(questType, questSection);
            }
        }

        /// <summary>
        /// Creates and configures a new quest section UI element.
        /// </summary>
        /// <returns>Configured UI_QuestSection instance</returns>
        private UI_QuestSection CreateQuestSection()
        {
            UI_QuestSection questSection = Instantiate(questSectionPrefab, questSectionHolder);
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

            QuestType_SO questType = quest.QuestData.QuestType;
            UI_QuestSection questSection = GetOrCreateQuestSection(questType);
            
            questSection.AddQuest(quest, OnQuestSelected);
            
            // Auto-select if no quest is currently selected
            if (selectedQuest == null)
            {
                OnQuestSelected(quest);
            }
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
            if (questSections.TryGetValue(questType, out UI_QuestSection existingSection))
            {
                return existingSection;
            }

            UI_QuestSection newSection = CreateQuestSection();
            questSections.Add(questType, newSection);
            return newSection;
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
            return QuestManager.Instance.ActiveQuests.Values
                .Where(quest => quest.QuestData.QuestType == questType)
                .ToList();
        }

        /// <summary>
        /// Groups all active quests by their quest type.
        /// </summary>
        /// <returns>Dictionary mapping quest types to their active quests</returns>
        private Dictionary<QuestType_SO, List<Quest>> GroupActiveQuestsByType()
        {
            Dictionary<QuestType_SO, List<Quest>> groupedQuests = new Dictionary<QuestType_SO, List<Quest>>();
            
            foreach (Quest quest in QuestManager.Instance.ActiveQuests.Values)
            {
                QuestType_SO questType = quest.QuestData.QuestType;
                
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
            if (QuestManager.Instance?.QuestStarted != null)
            {
                QuestManager.Instance.QuestStarted.SafeSubscribe(OnQuestStarted);
            }
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