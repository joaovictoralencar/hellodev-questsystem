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
    /// <summary>
    /// UI component for displaying quest details including tasks, rewards, and progress.
    /// </summary>
    [RequireComponent(typeof(ToggleGroup))]
    public class UI_QuestDetails : MonoBehaviour
    {
        #region UI References

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(0)]
#else
        [Header("Quest Info")]
#endif
        [SerializeField] private LocalizeStringEvent QuestNameText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(1)]
#endif
        [SerializeField] private LocalizeStringEvent QuestDescriptionText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(2)]
#endif
        [SerializeField] private LocalizeStringEvent QuestLocationText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(3)]
#endif
        [SerializeField] private TextMeshProUGUI LevelText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(4)]
#endif
        [SerializeField] private TextMeshProUGUI ProgressionText;

#if ODIN_INSPECTOR
        [TitleGroup("Rewards")]
        [PropertyOrder(10)]
        [Required("RewardsUI reference is required.")]
#else
        [Header("Rewards")]
#endif
        [SerializeField] private UI_QuestRewards RewardsUI;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(20)]
        [Required("TaskItemPrefab is required for spawning task items.")]
#else
        [Header("Tasks")]
#endif
        [SerializeField] private UI_TaskItem TaskItemPrefab;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(21)]
        [Required]
#endif
        [SerializeField] private RectTransform TasksHolder;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(22)]
#endif
        [SerializeField] private ToggleGroup ToggleGroup;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(23)]
#endif
        [SerializeField] private LocalizeStringEvent TaskDescriptionText;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(24)]
#endif
        [SerializeField] private TextMeshProUGUI TaskDescriptionTextMesh;

        #endregion

        #region Runtime State

        private Quest _currentQuest;
        private Task _currentTask;
        private List<UI_TaskItem> _taskUiItems = new();

#if ODIN_INSPECTOR
        [TitleGroup("Runtime State")]
        [PropertyOrder(40)]
        [ShowInInspector, ReadOnly]
        [InfoBox("Runtime state is only visible during Play mode.", InfoMessageType.Info, "@!UnityEngine.Application.isPlaying")]
        private string CurrentQuestName => _currentQuest?.QuestData?.DevName ?? "None";

        [TitleGroup("Runtime State")]
        [PropertyOrder(41)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentQuest != null")]
        private string CurrentQuestState => _currentQuest?.CurrentState.ToString() ?? "N/A";

        [TitleGroup("Runtime State")]
        [PropertyOrder(42)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentQuest != null")]
        [ProgressBar(0, 1, ColorGetter = nameof(GetProgressColor))]
        private float CurrentQuestProgress => _currentQuest?.CurrentProgress ?? 0f;

        [TitleGroup("Runtime State")]
        [PropertyOrder(43)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentTask != null")]
        private string CurrentTaskName => _currentTask?.DevName ?? "None";

        [TitleGroup("Runtime State")]
        [PropertyOrder(44)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentTask != null")]
        private string CurrentTaskState => _currentTask?.CurrentState.ToString() ?? "N/A";

        private Color GetProgressColor()
        {
            if (_currentQuest == null) return Color.gray;
            return _currentQuest.CurrentState switch
            {
                QuestState.Completed => Color.green,
                QuestState.Failed => Color.red,
                QuestState.InProgress => new Color(0.2f, 0.6f, 1f),
                _ => Color.gray
            };
        }
#endif

        #endregion

        #region Debug Buttons

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [FoldoutGroup("Debug", expanded: false)]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(50)]
#else
        [Header("Debug - Tasks")]
#endif
        [SerializeField] private UIButton CompleteCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(51)]
#endif
        [SerializeField] private UIButton FailCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(52)]
#endif
        [SerializeField] private UIButton InvokeEventTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(53)]
#endif
        [SerializeField] private UIButton IncrementCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(54)]
#endif
        [SerializeField] private UIButton DecrementCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(55)]
#endif
        [SerializeField] private UIButton ResetCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quest Actions")]
        [PropertyOrder(60)]
#else
        [Header("Debug - Quests")]
#endif
        [SerializeField] private UIButton CompleteCurrentQuestButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quest Actions")]
        [PropertyOrder(61)]
#endif
        [SerializeField] private UIButton FailCurrentQuestButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quest Actions")]
        [PropertyOrder(62)]
#endif
        [SerializeField] private UIButton ResetCurrentQuestButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(70)]
        [Button("Complete Current Task", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask != null && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickCompleteTask() => _currentTask?.CompleteTask();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(71)]
        [Button("Fail Current Task", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask != null && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickFailTask() => _currentTask?.FailTask();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(72)]
        [Button("Increment Task", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask != null && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickIncrementTask() => _currentTask?.IncrementStep();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(73)]
        [Button("Complete Current Quest", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickCompleteQuest() => DebugCompleteQuest();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(74)]
        [Button("Fail Current Quest", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickFailQuest() => _currentQuest?.FailQuest();
#endif
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (ToggleGroup == null) TryGetComponent(out ToggleGroup);
        }

        #endregion

        #region Public Methods

        public void Setup(Quest quest)
        {
            _currentQuest = quest;

            QuestNameText.StringReference = quest.QuestData.DisplayName;
            QuestDescriptionText.StringReference = quest.QuestData.QuestDescription;
            QuestLocationText.StringReference = quest.QuestData.QuestLocation;
            LevelText.text = quest.QuestData.RecommendedLevel.ToString();
            ProgressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";

            // Select next in progress task
            Task nextTask = quest.Tasks.FirstOrDefault(t => IsFirstValidTask(quest, t));

            bool IsFirstValidTask(Quest q, Task t)
            {
                return (q.CurrentState is QuestState.InProgress && t.CurrentState == TaskState.InProgress) ||
                       (q.CurrentState == QuestState.Completed && t.CurrentState == TaskState.Completed) ||
                       (q.CurrentState == QuestState.Failed && t.CurrentState == TaskState.Failed);
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

            // Rewards
            RewardsUI.Setup(quest);
            quest.OnAnyTaskUpdated.SafeSubscribe(OnTaskUpdated);
            quest.OnAnyTaskCompleted.SafeSubscribe(OnTaskUpdated);

#if UNITY_EDITOR
            // Debug buttons setup
            CompleteCurrentQuestButton.OnClick.SafeSubscribe(DebugCompleteQuest);
            FailCurrentQuestButton.OnClick.SafeSubscribe(DebugFailQuest);
            ResetCurrentQuestButton.OnClick.SafeSubscribe(DebugResetQuest);
            UpdateDebugButtons();
#endif
        }

        #endregion

        #region Private Methods

        private void OnTaskUpdated(Task task)
        {
            ProgressionText.text = $"{QuestUtils.GetPercentage(_currentQuest.CurrentProgress)}%";
            SetupNextTask(_currentQuest);

#if UNITY_EDITOR
            UpdateDebugButtons();
#endif
        }

        private void SetupNextTask(Quest quest)
        {
            Task nextTask = quest.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            if (nextTask == null) return;

            UI_TaskItem taskItem = _taskUiItems.FirstOrDefault(t => t.Task == nextTask);
            if (taskItem == null)
            {
                taskItem = Instantiate(TaskItemPrefab, TasksHolder);
                _taskUiItems.Add(taskItem);
            }

            taskItem.Setup(nextTask, OnTaskSelected);
            taskItem.SetToggleGroup(ToggleGroup);

#if UNITY_EDITOR
            UpdateDebugButtons();
#endif
        }

        private void OnTaskSelected(Task task)
        {
            _currentTask = task;
            TaskDescriptionText.StringReference = task.Description;
            TaskDescriptionTextMesh.DOFade(1, .35f).From(0).SetEase(Ease.OutQuad);

#if UNITY_EDITOR
            CompleteCurrentTaskButton.OnClick.SafeSubscribe(DebugCompleteTask);
            FailCurrentTaskButton.OnClick.SafeSubscribe(DebugFailTask);
            ResetCurrentTaskButton.OnClick.SafeSubscribe(DebugResetTask);
            IncrementCurrentTaskButton.OnClick.SafeSubscribe(DebugIncrementTask);
            DecrementCurrentTaskButton.OnClick.SafeSubscribe(DebugDecrementTask);
            InvokeEventTaskButton.OnClick.SafeSubscribe(DebugEventTask);
            UpdateDebugButtons();
#endif
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR
        private void UpdateDebugButtons()
        {
            if (_currentTask != null)
            {
                CompleteCurrentTaskButton.SetInteractable(_currentTask.CurrentState == TaskState.InProgress);
                IncrementCurrentTaskButton.SetInteractable(_currentTask.CurrentState == TaskState.InProgress);
                DecrementCurrentTaskButton.SetInteractable(_currentTask.CurrentState == TaskState.InProgress || _currentTask.CurrentState == TaskState.Completed);
                FailCurrentTaskButton.SetInteractable(_currentTask.CurrentState == TaskState.InProgress || _currentTask.CurrentState == TaskState.Completed);
                ResetCurrentTaskButton.SetInteractable(_currentTask.CurrentState != TaskState.NotStarted);
            }

            if (_currentQuest != null)
            {
                CompleteCurrentQuestButton.SetInteractable(_currentQuest.CurrentState == QuestState.InProgress);
                FailCurrentQuestButton.SetInteractable(_currentQuest.CurrentState == QuestState.InProgress);
                ResetCurrentQuestButton.SetInteractable(_currentQuest.CurrentState != QuestState.NotStarted);
            }
        }

        private void DebugFailQuest()
        {
            _currentQuest?.FailQuest();
        }

        private void DebugResetQuest()
        {
            _currentQuest?.ResetQuest();
        }

        private void DebugCompleteQuest()
        {
            if (_currentQuest == null) return;
            foreach (Task task in _currentQuest.Tasks)
            {
                task.CompleteTask();
            }
        }

        private void DebugDecrementTask()
        {
            _currentTask?.DecrementStep();
        }

        private void DebugIncrementTask()
        {
            _currentTask?.IncrementStep();
        }

        private void DebugResetTask()
        {
            if (_currentQuest == null || _currentTask == null) return;
            int index = _currentQuest.Tasks.IndexOf(_currentTask);
            for (int i = index; i < _currentQuest.Tasks.Count; i++)
            {
                _currentQuest.Tasks[i].ResetTask();
            }
            _currentTask.StartTask();
        }

        private void DebugFailTask()
        {
            _currentTask?.FailTask();
        }

        private void DebugCompleteTask()
        {
            _currentTask?.CompleteTask();
        }

        private void DebugEventTask()
        {
            if (_currentTask?.Data?.Conditions == null) return;
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
