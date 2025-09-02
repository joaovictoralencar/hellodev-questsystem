using System;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample
{
    public class UI_QuestItem : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        [SerializeField] private LocalizeStringEvent questNameText;
        [SerializeField] private LocalizeStringEvent questLocationText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI progressionText;

        [Header("Quest Status")] [SerializeField]
        private RectTransform questStatusIndicatorHolder;

        [Space(10)] [SerializeField] private RectTransform updatedIndicatorPrefab;
        [SerializeField] private RectTransform completedIndicatorPrefab;
        [SerializeField] private RectTransform failedIndicatorPrefab;

        [Header("Text Prefabs")] [SerializeField]
        private RectTransform questStatusHolder;

        [Space(10)] [SerializeField] private LocalizeStringEvent NextTaskTextPrefab;
        [SerializeField] private RectTransform completedTextPrefab;
        [SerializeField] private RectTransform failedTextPrefab;

        private Action<Quest> OnQuestSelected;
        private Quest _quest;

        private void OnDestroy()
        {
            if (_quest == null) return;
            _quest.OnQuestUpdated.Unsubscribe(OnQuestUpdated);
        }

        private void OnQuestUpdated(Quest quest)
        {
            progressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";
        }

        public void Setup(Quest quest, Action<Quest> onQuestSelected)
        {
            questNameText.StringReference = quest.QuestData.DisplayName;
            _quest = quest;
            switch (quest.CurrentState)
            {
                case QuestState.NotStarted:
                    break;
                case QuestState.InProgress:
                    OnQuesInProgress(quest);
                    break;
                case QuestState.Completed:
                    OnQuestCompleted(quest);
                    break;
                case QuestState.Failed:
                    OnQuestFailed(quest);
                    break;
            }

            questLocationText.StringReference = quest.QuestData.QuestLocation;
            levelText.text = quest.QuestData.RecommendedLevel.ToString();
            progressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";
            OnQuestSelected = onQuestSelected;
            _quest.OnQuestUpdated.SafeSubscribe(OnQuestUpdated);
        }


        private void OnQuestFailed(Quest quest)
        {
            DestroyTaskText();
            questStatusIndicatorHolder.DestroyAllChildren();
            RectTransform failedIndicator = Instantiate(failedIndicatorPrefab, questStatusIndicatorHolder);
            RectTransform failedText = Instantiate(failedTextPrefab, questStatusHolder);
        }

        private void OnQuesInProgress(Quest quest)
        {
            DestroyTaskText();
            quest.OnQuestCompleted.SafeSubscribe(OnQuestCompleted);
            Task nextNotCompletedTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            if (nextNotCompletedTask == null) return;
            LocalizeStringEvent nextTaskText = Instantiate(NextTaskTextPrefab, questStatusHolder);
            nextTaskText.StringReference = nextNotCompletedTask.DisplayName;
            nextNotCompletedTask.Data.SetupTaskLocalizedVariables(nextTaskText, nextNotCompletedTask);
        }

        private void OnQuestCompleted(Quest quest)
        {
            DestroyTaskText();
            questStatusIndicatorHolder.DestroyAllChildren();
            quest.OnQuestCompleted.Unsubscribe(OnQuestCompleted);
            RectTransform completedIndicator = Instantiate(completedIndicatorPrefab, questStatusIndicatorHolder);
            RectTransform completedText = Instantiate(completedTextPrefab, questStatusHolder);
        }

        private void DestroyTaskText()
        {
            if (questStatusHolder.childCount > 1) Destroy(questStatusHolder.GetChild(1).gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            Select(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Select(eventData);
        }

        private void Select(BaseEventData eventData)
        {
            OnQuestSelected?.Invoke(_quest);
        }
    }
}