using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.UI.Default;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    [RequireComponent(typeof(ToggleGroup))]
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
        [SerializeField] private ToggleGroup ToggleGroup;
        [SerializeField] private LocalizeStringEvent TaskDescriptionText;
        [SerializeField] private TextMeshProUGUI TaskDescriptionTextMesh;

        private Quest _currentQuest;
        private Task _currentTask;
        private List<UI_TaskItem> _taskUiItems = new();

        #region Debug

#if UNITY_EDITOR
        [FoldoutGroup("Debug"), Title("Tasks"), SerializeField]
        private UIButton CompleteCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton FailCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton InvokeEventTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton IncrementCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton DecrementCurrentTaskButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton ResetCurrentTaskButton;

        [FoldoutGroup("Debug"), Title("Quests"), SerializeField]
        private UIButton CompleteCurrentQuestButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton FailCurrentQuestButton;

        [FoldoutGroup("Debug"), SerializeField]
        private UIButton ResetCurrentQuestButton;
#endif

        #endregion

        private void Awake()
        {
            if (ToggleGroup == null) TryGetComponent(out ToggleGroup);
        }

        public void Setup(Quest quest)
        {
            _currentQuest = quest;

            QuestNameText.StringReference = quest.QuestData.DisplayName;
            QuestDescriptionText.StringReference = quest.QuestData.QuestDescription;
            QuestLocationText.StringReference = quest.QuestData.QuestLocation;
            LevelText.text = quest.QuestData.RecommendedLevel.ToString();
            ProgressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";


            //Select next in progress task
            Task nextTask = quest.Tasks.FirstOrDefault(t => IsFirstValidTask(quest, t));

            bool IsFirstValidTask(Quest q, Task t)
            {
               return (q.CurrentState is QuestState.InProgress && t.CurrentState == TaskState.InProgress) ||
                      (q.CurrentState == QuestState.Completed  && t.CurrentState == TaskState.Completed)  ||
                      (q.CurrentState == QuestState.Failed     && t.CurrentState == TaskState.Failed);
            }
            
            TasksHolder.DestroyAllChildren();
            _taskUiItems.Clear();
            foreach (Task task in quest.Tasks)
            {
                if (task.CurrentState == TaskState.NotStarted) continue;
                UI_TaskItem taskItem = Instantiate(TaskItemPrefab, TasksHolder);
                _taskUiItems.Add(taskItem);
                taskItem.Setup(task, OnTaskSelected);
                taskItem.SetToggleGroup(ToggleGroup);
                if (task == nextTask)
                {
                    taskItem.Select();
                }
            }

            //Rewards
            RewardsUI.Setup(quest);
            quest.OnAnyTaskUpdated.SafeSubscribe(OnTaskUpdated);
            quest.OnAnyTaskCompleted.SafeSubscribe(OnTaskUpdated);

#if UNITY_EDITOR
            //Debug buttons setup
            CompleteCurrentQuestButton.OnClick.SafeSubscribe(DebugCompleteQuest);
            FailCurrentQuestButton.OnClick.SafeSubscribe(DebugFailQuest);
            ResetCurrentQuestButton.OnClick.SafeSubscribe(DebugResetQuest);
            UpdateDebugButtons();
#endif
        }

        private void DebugFailQuest()
        {
            _currentQuest.FailQuest();
        }

        private void DebugResetQuest()
        {
            _currentQuest.ResetQuest();
        }

        private void DebugCompleteQuest()
        {
            foreach (Task task in _currentQuest.Tasks)
            {
                task.CompleteTask();
            }
        }

        private void OnTaskUpdated(Task task)
        {
            ProgressionText.text = $"{QuestUtils.GetPercentage(_currentQuest.CurrentProgress)}%";
            //Select next in progress task
            SetupNextTask(_currentQuest);
            
            //Update debug buttons
            #if UNITY_EDITOR
            UpdateDebugButtons();
            #endif
        }
#if UNITY_EDITOR
        private void UpdateDebugButtons()
        {
            CompleteCurrentTaskButton. SetInteractable(_currentTask.CurrentState == TaskState.InProgress);
            IncrementCurrentTaskButton.SetInteractable(_currentTask.CurrentState == TaskState.InProgress);
            DecrementCurrentTaskButton.SetInteractable(_currentTask.CurrentState == TaskState.InProgress || _currentTask.CurrentState == TaskState.Completed);
            FailCurrentTaskButton.     SetInteractable(_currentTask.CurrentState == TaskState.InProgress || _currentTask.CurrentState == TaskState.Completed);
            ResetCurrentTaskButton.    SetInteractable(_currentTask.CurrentState != TaskState.NotStarted);
            
            CompleteCurrentQuestButton.SetInteractable(_currentQuest.CurrentState == QuestState.InProgress);
            FailCurrentQuestButton.    SetInteractable(_currentQuest.CurrentState == QuestState.InProgress);
            ResetCurrentQuestButton.   SetInteractable(_currentQuest.CurrentState != QuestState.NotStarted);
        }
#endif
        private void SetupNextTask(Quest quest)
        {
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
            taskItem.SetToggleGroup(ToggleGroup);
            //Update debug buttons
            #if UNITY_EDITOR
                UpdateDebugButtons();
            #endif
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
            
            //Update debug buttons
            UpdateDebugButtons();
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
            int index = _currentQuest.Tasks.IndexOf(_currentTask);
            for (int i = index; i < _currentQuest.Tasks.Count; i++)
            {
                _currentQuest.Tasks[i].ResetTask();
            }

            _currentTask.StartTask();
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