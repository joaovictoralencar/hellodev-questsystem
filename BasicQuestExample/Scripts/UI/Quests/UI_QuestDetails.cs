using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using Sirenix.OdinInspector;
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
        [Header("Rewards")] [SerializeField] private UI_QuestRewards RewardsUI;
        [Header("Tasks")] [SerializeField] private UI_TaskItem TaskItemPrefab;
        [SerializeField] private RectTransform TasksHolder;
        [SerializeField] private LocalizeStringEvent TaskDescriptionText;
        [SerializeField] private TextMeshProUGUI TaskDescriptionTextMesh;

        private Quest _quest;
        private Task _currentTask;
        private List<UI_TaskItem> _taskUiItems = new();

        #region Debug

#if UNITY_EDITOR
        [FoldoutGroup("Debug"), Title("Tasks"), SerializeField]
        private BaseButton CompleteCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton FailCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton InvokeEventTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton IncrementCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton DecrementCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton ResetCurrentTaskButton;

        [FoldoutGroup("Debug"), Title("Quests"), SerializeField]
        private BaseButton CompleteCurrentQuestButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton FailCurrentQuestButton;

        [FoldoutGroup("Debug"), SerializeField]
        private BaseButton ResetCurrentQuestButton;
#endif

        #endregion

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
            _taskUiItems.Clear();
            foreach (Task task in quest.Tasks)
            {
                if (task.CurrentState == TaskState.NotStarted) continue;
                UI_TaskItem taskItem = Instantiate(TaskItemPrefab, TasksHolder);
                _taskUiItems.Add(taskItem);
                taskItem.Setup(task, OnTaskSelected);
                if (task == nextTask)
                {
                    taskItem.Select();
                }
            }

            //Rewards
            RewardsUI.Setup(quest);
            quest.OnAnyTaskUpdated.SafeSubscribe(OnQuestUpdated);

            //Debug buttons setup
            CompleteCurrentQuestButton.OnClick.SafeSubscribe(DebugCompleteQuest);
            FailCurrentQuestButton.OnClick.SafeSubscribe(DebugFailQuest);
            ResetCurrentQuestButton.OnClick.SafeSubscribe(DebugResetQuest);
        }

        private void DebugFailQuest()
        {
            _quest.FailQuest();
        }

        private void DebugResetQuest()
        {
            _quest.ResetQuest();
        }

        private void DebugCompleteQuest()
        {
            foreach (Task task in _quest.Tasks)
            {
                task.CompleteTask();
            }
        }

        private void OnQuestUpdated(Quest quest)
        {
            ProgressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";
            //Select next in progress task
            Task nextTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            if (nextTask == null) return;
            //Checks if task is already spawned
            UI_TaskItem taskItem = _taskUiItems.FirstOrDefault(t => t.Task == nextTask);
            if (taskItem == null)
            {
                taskItem = Instantiate(TaskItemPrefab, TasksHolder);
                _taskUiItems.Add(taskItem);
            }
            taskItem.Setup(nextTask, OnTaskSelected);
        }

        private void OnTaskSelected(Task task)
        {
            _currentTask = task;
            TaskDescriptionText.StringReference = task.Description;
            TaskDescriptionTextMesh.DOFade(1, .35f).From(0).SetEase(Ease.OutQuad);

            //Debug buttons setup
#if UNITY_EDITOR
            CompleteCurrentTaskButton.OnClick.SafeSubscribe(DebugCompleteTask);
            FailCurrentTaskButton.OnClick.SafeSubscribe(DebugFailTask);
            ResetCurrentTaskButton.OnClick.SafeSubscribe(DebugResetTask);
            IncrementCurrentTaskButton.OnClick.SafeSubscribe(DebugIncrementTask);
            DecrementCurrentTaskButton.OnClick.SafeSubscribe(DebugDecrementTask);
            InvokeEventTaskButton.OnClick.SafeSubscribe(DebugEventTask);
#endif
        }

        #region Debug callbacks

#if UNITY_EDITOR
        private void DebugDecrementTask()
        {
            _currentTask.DecrementStep();
        }

        private void DebugIncrementTask()
        {
            _currentTask.IncrementStep();
        }

        private void DebugResetTask()
        {
            _currentTask.ResetTask();
        }

        private void DebugFailTask()
        {
            _currentTask.FailTask();
        }

        private void DebugCompleteTask()
        {
            _currentTask.CompleteTask();
        }

        private void DebugEventTask()
        {
            foreach (Condition_SO condition in _currentTask.Data.Conditions)
            {
                if (condition is not IConditionEventDriven conditionEventDriven) continue;
                conditionEventDriven.ForceFulfillCondition();
            }
        }
#endif

        #endregion
    }
}