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
    public class UI_Quests : UIContainer
    {
        [Header("Prefabs")] [SerializeField] private RectTransform QuestSectionHolder;
        [SerializeField] private UI_QuestSection QuestSectionPrefab;
        [SerializeField] private ToggleGroup QuestSectionToggleGroup;

        [Header("Quest Details")] [SerializeField]
        private UI_QuestDetails questDetails;

        Dictionary<QuestType_SO, UI_QuestSection> _questSections = new();
        
        private Quest _selectedQuest;

        protected override void Awake()
        {
            onStartShow.AddListener(Setup);
            base.Awake();
            // Clear the QuestSectionHolder
            QuestSectionHolder.DestroyAllChildren();
        }
        
        /// <summary>
        /// Sets up the UI_QUESTS component.
        /// This function is called when the UI_QUESTS component is shown.
        /// It populates the QuestSectionHolder with a UI_QuestSection for each QuestType in the QuestManager.
        /// Each UI_QuestSection is populated with the Quests associated with that QuestType.
        /// </summary>
        private void Setup()
        {
            // Clear the QuestSectionHolder
            QuestSectionHolder.DestroyAllChildren();

            // Group the quests by QuestType
            Dictionary<QuestType_SO, List<Quest>> groupedQuests = new Dictionary<QuestType_SO, List<Quest>>();
            foreach (Quest quest in QuestManager.Instance.ActiveQuests.Values)
            {
                if (!groupedQuests.ContainsKey(quest.QuestData.QuestType))
                {
                    groupedQuests.Add(quest.QuestData.QuestType, new List<Quest>());
                }

                groupedQuests[quest.QuestData.QuestType].Add(quest);
            }

            // Create a UI_QuestSection for each QuestType and populate it with the Quests
            foreach (KeyValuePair<QuestType_SO, List<Quest>> keyValuePair in groupedQuests)
            {
                UI_QuestSection questSection = Instantiate(QuestSectionPrefab, QuestSectionHolder);
                questSection.Setup(keyValuePair.Value, OnQuestSelected);
                questSection.SetToggleGroup(QuestSectionToggleGroup);
                _questSections.TryAdd(keyValuePair.Key, questSection);
            }

            QuestManager.Instance.QuestStarted.SafeSubscribe(OnQuestStarted);
            
            //Select first quest
            if (_questSections.Count > 0)
            {
                _questSections.First().Value.Select();
            }
        }

        private void OnQuestStarted(Quest quest)
        {
            QuestType_SO questType = quest.QuestData.QuestType;
            UI_QuestSection questSection;
            if (_questSections.TryGetValue(questType, out UI_QuestSection section))
                questSection = section;
            else
            {
                questSection = Instantiate(QuestSectionPrefab, QuestSectionHolder);
                questSection.SetToggleGroup(QuestSectionToggleGroup);
            }

            questSection.AddQuest(quest, OnQuestSelected);
            if (_selectedQuest == null)
            {
                OnQuestSelected(quest);
            }
        }

        private void OnQuestSelected(Quest quest)
        {
            if (_selectedQuest == quest)
                return;
            _selectedQuest = quest;
            questDetails.Setup(quest);
        }
    }
}