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
        /// Initializes the UI quest section with the given list of quests.
        /// This method sets up the section's name and background color based on the first quest's type,
        /// and instantiates a UI quest item for each quest in the list.
        /// </summary>
        /// <param name="quests">The list of quests to set up the UI section with.</param>
        /// <param name="onQuestSelected">Callback to be called when a quest is selected.</param>
        public void Setup(List<Quest> quests, Action<Quest> onQuestSelected)
        {
            // Get the quest type of the first quest in the list
            QuestType = quests.First().QuestData.QuestType;

            // Set the section name and background color based on the quest type
            QuestSectionName.StringReference = QuestType.DisplayName;
            QuestSectionBackground.color = QuestType.Color;

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