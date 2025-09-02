using System;
using System.Linq;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    public class UI_QuestDetails : MonoBehaviour
    {
        [SerializeField] private LocalizeStringEvent QuestNameText;
        [SerializeField] private LocalizeStringEvent QuestDescriptionText;
        [SerializeField] private LocalizeStringEvent QuestLocationText;
        [SerializeField] private TextMeshProUGUI LevelText;
        [SerializeField] private TextMeshProUGUI ProgressionText;
        [Header("Rewards")]
        [SerializeField] private UI_QuestRewards RewardsUI;
        [Header("Tasks")] [SerializeField] private UI_TaskItem TaskItemPrefab;
        [SerializeField] private RectTransform TasksHolder;
        [SerializeField] private LocalizeStringEvent TaskDescriptionText;

        private Quest _quest;
        private void OnDestroy()
        {
            if (_quest == null) return;
            _quest.OnQuestUpdated.SafeSubscribe(OnQuestUpdated);
        }

        public void Setup(Quest quest)
        {
            _quest = quest;

            QuestNameText.StringReference = quest.QuestData.DisplayName;
            QuestDescriptionText.StringReference = quest.QuestData.QuestDescription;
            QuestLocationText.StringReference = quest.QuestData.QuestLocation;
            LevelText.text = quest.QuestData.RecommendedLevel.ToString();
            ProgressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";
            
             
            //Select next in progress task
            Task nextTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            
            TasksHolder.DestroyAllChildren();
            foreach (Task task in quest.Tasks)
            {
                if (task.CurrentState == TaskState.NotStarted) continue;
                UI_TaskItem taskItem = Instantiate(TaskItemPrefab, TasksHolder);
                taskItem.Setup(task, OnTaskSelected);
                if (task == nextTask)
                {
                    taskItem.Select();
                }
            }
            
            //Rewards
            RewardsUI.Setup(quest);
            quest.OnAnyTaskUpdated.SafeSubscribe(OnQuestUpdated);
            quest.OnQuestUpdated.SafeSubscribe(OnQuestUpdated);
        }

        private void OnQuestUpdated(Quest quest)
        {
            ProgressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";
            //Select next in progress task
            Task nextTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            if (nextTask == null) return;
            UI_TaskItem taskItem = Instantiate(TaskItemPrefab, TasksHolder);
            taskItem.Setup(nextTask, OnTaskSelected);
        }

        private void OnTaskSelected(Task task)
        {
            TaskDescriptionText.StringReference = task.Description;
        }
    }
}