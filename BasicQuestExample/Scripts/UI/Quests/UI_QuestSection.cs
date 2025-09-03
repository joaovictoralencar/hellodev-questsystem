using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample
{
    public class UI_QuestSection : MonoBehaviour
    {
        [SerializeField] private LocalizeStringEvent QuestSectionName;
        [SerializeField] private Image QuestSectionBackground;
        [SerializeField] private UI_QuestItem QuestItemPrefab;
        [SerializeField] private RectTransform QuestItemHolder;
        
        [Header("Toggle")] [SerializeField] BaseToggle toggle;
        public QuestType_SO QuestType { get; private set; }
        
        
        /// <summary>
        /// Sets up the quest section with the given quest type.
        /// Assigns the quest type to the section, sets the section name to the display name of the quest type,
        /// and sets the section background color to the color of the quest type.
        /// </summary>
        /// <param name="questType">The quest type for the section</param>
        public void Setup(QuestType_SO questType)
        {
            // Get the quest type of the first quest in the list
            QuestType = questType;

            // Set the section name and background color based on the quest type
            QuestSectionName.StringReference = QuestType.DisplayName;
            QuestSectionBackground.color = QuestType.Color;
        }
        public void SpawnQuestsItems(List<Quest> quests, Action<Quest> onQuestSelected)
        {
            if (quests.Count == 0) return;
            
            // Instantiate a UI quest item for each quest in the list
            foreach (Quest quest in quests)
            {
                UI_QuestItem questItem = Instantiate(QuestItemPrefab, QuestItemHolder);
                questItem.Setup(quest, onQuestSelected);
            }
        }
        public void AddQuest(Quest newQuest, Action<Quest> onQuestSelected)
        {
            // Get the quest type of the first quest in the list
            QuestType = newQuest.QuestData.QuestType;

            // Set the section name and background color based on the quest type
            QuestSectionName.StringReference = QuestType.DisplayName;
            QuestSectionBackground.color = QuestType.Color;

            // Instantiate a UI quest item for each quest in the list
                UI_QuestItem questItem = Instantiate(QuestItemPrefab, QuestItemHolder);
            questItem.Setup(newQuest, onQuestSelected);
        }
        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            toggle.Toggle.group = toggleGroup;
        }

        public void Select()
        {
            toggle.Toggle.isOn = true;
        }
    }
}