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
    /// UI component that represents a quest section containing multiple quest items.
    /// Handles the display and management of quests grouped by quest type.
    /// </summary>
    public class UI_QuestSection : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Quest Section Display")]
        [SerializeField] 
        [Tooltip("Localized string component for the section name")]
        private LocalizeStringEvent questSectionName;
        
        [SerializeField] 
        [Tooltip("Background image component for the section")]
        private Image questSectionBackground;
        
        [Header("Quest Items Management")]
        [SerializeField] 
        [Tooltip("Prefab used to create individual quest items")]
        private UI_QuestItem questItemPrefab;
        
        [SerializeField] 
        [Tooltip("Parent container for quest item instances")]
        private RectTransform questItemHolder;
        
        [Header("Selection Toggle")]
        [SerializeField] 
        [Tooltip("Toggle component for section selection")]
        private BaseToggle toggle;
        
        #endregion

        #region Private Fields
        
        /// <summary>
        /// Dictionary mapping quests to their corresponding UI items for efficient lookup
        /// </summary>
        private readonly Dictionary<Quest, UI_QuestItem> questItems = new Dictionary<Quest, UI_QuestItem>();
        
        #endregion

        #region Public Properties
        
        /// <summary>
        /// The quest type associated with this section
        /// </summary>
        public QuestType_SO QuestType { get; private set; }
        
        #endregion

        #region Public Setup Methods
        
        /// <summary>
        /// Sets up the quest section with the given quest type.
        /// Configures the section appearance based on the quest type properties.
        /// </summary>
        /// <param name="questType">The quest type for this section</param>
        public void Setup(QuestType_SO questType)
        {
            if (questType == null) return;

            QuestType = questType;
            UpdateSectionAppearance();
        }

        /// <summary>
        /// Creates and displays UI items for multiple quests at once.
        /// Typically used during initial setup of the quest section.
        /// </summary>
        /// <param name="quests">List of quests to display</param>
        /// <param name="onQuestSelected">Callback invoked when a quest is selected</param>
        public void SpawnQuestsItems(List<Quest> quests, Action<Quest> onQuestSelected)
        {
            if (quests == null || quests.Count == 0) return;
            
            foreach (Quest quest in quests)
            {
                CreateQuestItem(quest, onQuestSelected);
            }
        }

        /// <summary>
        /// Adds a single quest to this section.
        /// Creates a new quest item and updates the section if this is the first quest.
        /// </summary>
        /// <param name="newQuest">The quest to add</param>
        /// <param name="onQuestSelected">Callback invoked when the quest is selected</param>
        public void AddQuest(Quest newQuest, Action<Quest> onQuestSelected)
        {
            if (newQuest?.QuestData?.QuestType == null) return;

            if (QuestType == null)
            {
                Setup(newQuest.QuestData.QuestType);
            }
            CreateQuestItem(newQuest, onQuestSelected);
            
            // Update existing quest item to handle quest section change
            UI_QuestItem questItem = questItems.Values.FirstOrDefault(x => x.Quest.QuestId == newQuest.QuestId);
            if (questItem != null)
            {
                questItem.SetupMarkerColour(QuestType.Color);
            }
        }

        /// <summary>
        /// Removes a quest from this section.
        /// Destroys the associated UI item and cleans up references.
        /// </summary>
        /// <param name="quest">The quest to remove</param>
        public void RemoveQuest(Quest quest)
        {
            if (quest == null || !questItems.ContainsKey(quest)) return;

            UI_QuestItem questItem = questItems[quest];
            questItems.Remove(quest);
            
            if (questItem != null)
            {
                DestroyImmediate(questItem.gameObject);
            }
        }

        /// <summary>
        /// Gets the first quest in this section.
        /// </summary>
        /// <returns>The first quest, or null if section is empty</returns>
        public Quest GetFirstQuest()
        {
            UI_QuestItem firstQuestItem = GetFirstQuestItem();
            return firstQuestItem?.Quest;
        }
        
        #endregion

        #region Toggle Management
        
        /// <summary>
        /// Assigns this section to a toggle group for mutually exclusive selection.
        /// </summary>
        /// <param name="toggleGroup">The toggle group to assign to</param>
        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            if (toggle?.Toggle != null)
            {
                toggle.Toggle.group = toggleGroup;
            }
        }

        /// <summary>
        /// Programmatically selects this quest section.
        /// </summary>
        public void Select()
        {
            if (toggle?.Toggle != null)
            {
                toggle.Toggle.isOn = true;
            }
        }
        
        #endregion

        #region Private Helper Methods
        
        /// <summary>
        /// Updates the visual appearance of the section based on the quest type.
        /// </summary>
        private void UpdateSectionAppearance()
        {
            if (QuestType == null) return;

            if (questSectionName != null)
            {
                questSectionName.StringReference = QuestType.DisplayName;
            }

            if (questSectionBackground != null)
            {
                questSectionBackground.color = QuestType.Color;
            }
        }

        /// <summary>
        /// Creates a new quest item UI component for the given quest.
        /// </summary>
        /// <param name="quest">The quest to create an item for</param>
        /// <param name="onQuestSelected">Callback for quest selection</param>
        /// <returns>The created quest item component</returns>
        private UI_QuestItem CreateQuestItem(Quest quest, Action<Quest> onQuestSelected)
        {
            if (questItems.ContainsKey(quest))
            {
                return questItems[quest];
            }

            UI_QuestItem questItem = Instantiate(questItemPrefab, questItemHolder);
            questItem.Setup(quest, onQuestSelected);
            questItem.SetupMarkerColour(QuestType.Color);
            questItems.Add(quest, questItem);
            
            return questItem;
        }

        /// <summary>
        /// Gets the first quest item in the section.
        /// </summary>
        /// <returns>The first quest item component, or null if none exist</returns>
        private UI_QuestItem GetFirstQuestItem()
        {
            return questItems.Values.FirstOrDefault();
        }
        
        #endregion

        #region Public Utility Methods
        
        /// <summary>
        /// Gets the total number of quests in this section.
        /// </summary>
        /// <returns>Number of quests in the section</returns>
        public int GetQuestCount()
        {
            return questItems.Count;
        }

        /// <summary>
        /// Checks if this section contains the specified quest.
        /// </summary>
        /// <param name="quest">The quest to check for</param>
        /// <returns>True if the quest exists in this section</returns>
        public bool ContainsQuest(Quest quest)
        {
            return questItems.ContainsKey(quest);
        }

        /// <summary>
        /// Gets all quests currently in this section.
        /// </summary>
        /// <returns>Collection of all quests in the section</returns>
        public IEnumerable<Quest> GetAllQuests()
        {
            return questItems.Keys;
        }

        /// <summary>
        /// Clears all quests from this section.
        /// </summary>
        public void ClearAllQuests()
        {
            foreach (UI_QuestItem questItem in questItems.Values)
            {
                if (questItem != null)
                {
                    DestroyImmediate(questItem.gameObject);
                }
            }
            
            questItems.Clear();
        }
        
        #endregion
    }
}