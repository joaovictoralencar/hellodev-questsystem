using System.Collections.Generic;
using System.Linq;
using PrimeTween;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.UI.Default;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static HelloDev.QuestSystem.Utils.QuestLogger;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    /// <summary>
    /// UI component for displaying quest details including tasks, rewards, and progress.
    /// Handles task navigation and selection within the quest details panel.
    /// </summary>
    [RequireComponent(typeof(ToggleGroup))]
    public class UI_QuestDetails : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(0)]
#else
        [Header("Quest Info")]
#endif
        [SerializeField] private LocalizeStringEvent questNameText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(1)]
#endif
        [SerializeField] private Image questImage;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(2)]
#endif
        [SerializeField] private LocalizeStringEvent questDescriptionText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(3)]
#endif
        [SerializeField] private LocalizeStringEvent questLocationText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(4)]
#endif
        [SerializeField] private TextMeshProUGUI levelText;

#if ODIN_INSPECTOR
        [TitleGroup("Quest Info")]
        [PropertyOrder(5)]
#endif
        [SerializeField] private TextMeshProUGUI progressionText;

#if ODIN_INSPECTOR
        [TitleGroup("Stage Info")]
        [PropertyOrder(6)]
#else
        [Header("Stage Info")]
#endif
        [SerializeField] private TextMeshProUGUI stageNameText;

#if ODIN_INSPECTOR
        [TitleGroup("Stage Info")]
        [PropertyOrder(7)]
#endif
        [SerializeField] private TextMeshProUGUI stageProgressText;

#if ODIN_INSPECTOR
        [TitleGroup("Rewards")]
        [PropertyOrder(10)]
        [Required("RewardsUI reference is required.")]
#else
        [Header("Rewards")]
#endif
        [SerializeField] private UI_QuestRewards rewardsUI;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(20)]
        [Required("TaskItemPrefab is required for spawning task items.")]
#else
        [Header("Tasks")]
#endif
        [SerializeField] private UI_TaskItem taskItemPrefab;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(21)]
        [Required]
#endif
        [SerializeField] private RectTransform tasksHolder;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(22)]
#endif
        [SerializeField] private ToggleGroup taskToggleGroup;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(23)]
#endif
        [SerializeField] private LocalizeStringEvent taskDescriptionText;

#if ODIN_INSPECTOR
        [TitleGroup("Tasks")]
        [PropertyOrder(24)]
#endif
        [SerializeField] private TextMeshProUGUI taskDescriptionTextMesh;

#if ODIN_INSPECTOR
        [TitleGroup("Choices")]
        [PropertyOrder(30)]
        [InfoBox("Choice buttons are displayed when a stage has player choices and no active tasks.")]
#else
        [Header("Choices")]
#endif
        [SerializeField] private RectTransform choicesHolder;

#if ODIN_INSPECTOR
        [TitleGroup("Choices")]
        [PropertyOrder(31)]
#endif
        [SerializeField] private UIButton choiceButtonPrefab;

#if ODIN_INSPECTOR
        [TitleGroup("Choices")]
        [PropertyOrder(32)]
#endif
        [SerializeField] private LocalizeStringEvent choiceHeaderText;

        #endregion

        #region Private Fields

        private QuestRuntime _currentQuest;
        private TaskRuntime _currentTask;
        private readonly List<UI_TaskItem> _taskItems = new();
        private readonly List<UIButton> _choiceButtons = new();
        private int _selectedTaskIndex = -1;
        private bool _isTransitioningStage;
        private bool _isShowingChoices;

        #endregion

        #region Public Properties

        public QuestRuntime CurrentQuest => _currentQuest;
        public TaskRuntime CurrentTask => _currentTask;
        public IReadOnlyList<UI_TaskItem> TaskItems => _taskItems;

        #endregion

#if ODIN_INSPECTOR
        #region Runtime State Display

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

        #endregion
#endif

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

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Location Task")]
        [PropertyOrder(80)]
        [Button("Trigger Location Reached", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is LocationTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickTriggerLocation()
        {
            // Location tasks now use conditions - trigger the first unfulfilled condition
            _currentTask?.IncrementStep();
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Timed Task")]
        [PropertyOrder(81)]
        [Button("Add 30 Seconds", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is TimedTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickAddTime()
        {
            if (_currentTask is TimedTaskRuntime timedTask)
            {
                timedTask.AddTime(30f);
            }
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Timed Task")]
        [PropertyOrder(82)]
        [Button("Expire Timer", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is TimedTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickExpireTimer()
        {
            if (_currentTask is TimedTaskRuntime timedTask)
            {
                timedTask.UpdateTimer(timedTask.RemainingTime + 1f);
            }
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Timed Task")]
        [PropertyOrder(83)]
        [Button("Complete Timed Objective", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is TimedTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickCompleteTimedObjective()
        {
            if (_currentTask is TimedTaskRuntime timedTask)
            {
                timedTask.MarkObjectiveComplete();
            }
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Discovery Task")]
        [PropertyOrder(84)]
        [Button("Discover Next Item", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is DiscoveryTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickDiscoverItem()
        {
            if (_currentTask is DiscoveryTaskRuntime discoveryTask)
            {
                discoveryTask.IncrementStep();
            }
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Player Choices")]
        [PropertyOrder(90)]
        [Button("Select Choice 1", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickSelectChoice1() => DebugSelectChoice(0);

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Player Choices")]
        [PropertyOrder(91)]
        [Button("Select Choice 2", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickSelectChoice2() => DebugSelectChoice(1);

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Player Choices")]
        [PropertyOrder(92)]
        [Button("Select Choice 3", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickSelectChoice3() => DebugSelectChoice(2);
#endif
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (taskToggleGroup == null) TryGetComponent(out taskToggleGroup);

            // Prevent deselection when clicking on already-selected task
            if (taskToggleGroup != null)
                taskToggleGroup.allowSwitchOff = false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromQuestEvents();
            ClearChoiceButtons();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up the quest details panel with the specified quest.
        /// </summary>
        public void Setup(QuestRuntime quest)
        {
            if (quest?.QuestData == null) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Setup for quest: {quest.QuestData.DevName}");

            // Unsubscribe from previous quest
            UnsubscribeFromQuestEvents();

            _currentQuest = quest;

            // Setup quest info
            SetupQuestInfo(quest);

            // Clear task list and choice buttons
            ClearTaskItems();
            ClearChoiceButtons();
            CreateTaskItems(quest);

            // Setup rewards
            rewardsUI?.Setup(quest);

            // Subscribe to quest events
            SubscribeToQuestEvents(quest);

            // Select initial task or show choices if no tasks
            SelectInitialTask(quest);
            CheckForPendingChoices();

#if UNITY_EDITOR
            SetupDebugButtons();
#endif
        }

        /// <summary>
        /// Selects the next task in the list.
        /// </summary>
        public void SelectNextTask()
        {
            if (_taskItems.Count == 0) return;

            _selectedTaskIndex = (_selectedTaskIndex + 1) % _taskItems.Count;
            SelectTaskAtIndex(_selectedTaskIndex);
        }

        /// <summary>
        /// Selects the previous task in the list.
        /// </summary>
        public void SelectPreviousTask()
        {
            if (_taskItems.Count == 0) return;

            _selectedTaskIndex = _selectedTaskIndex <= 0 ? _taskItems.Count - 1 : _selectedTaskIndex - 1;
            SelectTaskAtIndex(_selectedTaskIndex);
        }

        /// <summary>
        /// Sets focus to the first task for controller navigation.
        /// </summary>
        public void FocusFirstTask()
        {
            if (_taskItems.Count > 0)
            {
                var firstItem = _taskItems[0];
                EventSystem.current?.SetSelectedGameObject(firstItem.Toggle.Toggle.gameObject);
            }
        }

        #endregion

        #region Private Methods - Setup

        private void SetupQuestInfo(QuestRuntime quest)
        {
            if (questNameText != null)
            {
                questNameText.StringReference = quest.QuestData.DisplayName;
                questNameText.RefreshString();
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.StringReference = quest.QuestData.QuestDescription;
                questDescriptionText.RefreshString();
            }

            if (questLocationText != null)
            {
                questLocationText.StringReference = quest.QuestData.QuestLocation;
                questLocationText.RefreshString();
            }

            if (levelText != null)
                levelText.text = quest.QuestData.RecommendedLevel.ToString();

            if (progressionText != null)
                progressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";

            if (questImage != null && quest.QuestData.QuestSprite != null)
                questImage.sprite = quest.QuestData.QuestSprite;

            // Update stage info
            UpdateStageInfo(quest);
        }

        private void UpdateStageInfo(QuestRuntime quest)
        {
            if (quest?.CurrentStage == null)
            {
                if (stageNameText != null) stageNameText.text = "";
                if (stageProgressText != null) stageProgressText.text = "";
                return;
            }

            if (stageNameText != null)
                stageNameText.text = quest.CurrentStage.StageName;

            if (stageProgressText != null)
            {
                int currentIndex = quest.Stages.FindIndex(s => s == quest.CurrentStage) + 1;
                int totalStages = quest.Stages.Count;
                stageProgressText.text = $"Stage {currentIndex}/{totalStages}";
            }
        }

        private void CreateTaskItems(QuestRuntime quest)
        {
            int createdCount = 0;
            foreach (TaskRuntime task in quest.Tasks)
            {
                // Skip not-started tasks
                if (task.CurrentState == TaskState.NotStarted)
                    continue;

                var taskItem = Instantiate(taskItemPrefab, tasksHolder);
                taskItem.Setup(task, HandleTaskSelected);
                taskItem.SetToggleGroup(taskToggleGroup);
                _taskItems.Add(taskItem);
                createdCount++;
            }
            Log(LogSubsystem.UI, $"[UI_QuestDetails] Created {createdCount} task item(s) for quest: {quest.QuestData.DevName}");
        }

        private void ClearTaskItems()
        {
            if (_taskItems.Count > 0)
                Log(LogSubsystem.UI, $"[UI_QuestDetails] Clearing {_taskItems.Count} task item(s)");

            tasksHolder?.DestroyAllChildren();
            _taskItems.Clear();
            _selectedTaskIndex = -1;
        }

        private void SelectInitialTask(QuestRuntime quest)
        {
            // Find first valid task based on quest state
            TaskRuntime targetTask = quest.Tasks.FirstOrDefault(t => IsValidInitialTask(quest, t));

            if (targetTask == null)
            {
                LogVerbose(LogSubsystem.UI, "[UI_QuestDetails] No valid initial task found");
                return;
            }

            var taskItem = _taskItems.FirstOrDefault(item => item.Task == targetTask);
            if (taskItem != null)
            {
                LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] Selected initial task: {targetTask.DevName}");
                taskItem.SelectTask();
                _selectedTaskIndex = _taskItems.IndexOf(taskItem);
            }
        }

        private bool IsValidInitialTask(QuestRuntime quest, TaskRuntime task)
        {
            return quest.CurrentState switch
            {
                QuestState.InProgress => task.CurrentState == TaskState.InProgress,
                QuestState.Completed => task.CurrentState == TaskState.Completed,
                QuestState.Failed => task.CurrentState == TaskState.Failed,
                _ => false
            };
        }

        #endregion

        #region Private Methods - Selection

        private void HandleTaskSelected(TaskRuntime task)
        {
            _currentTask = task;
            _selectedTaskIndex = _taskItems.FindIndex(item => item.Task == task);

            // Update task description
            UpdateTaskDescription(task);

#if UNITY_EDITOR
            SetupTaskDebugButtons();
            UpdateDebugButtons();
#endif
        }

        private void SelectTaskAtIndex(int index)
        {
            if (index < 0 || index >= _taskItems.Count) return;

            var taskItem = _taskItems[index];
            taskItem.SelectTask();
        }

        private void UpdateTaskDescription(TaskRuntime task)
        {
            if (taskDescriptionText == null || task?.Data == null || task.Description == null) return;

            // Create a new LocalizedString with the same table/entry reference
            // This avoids modifying the shared ScriptableObject's TaskDescription
            var localizedString = new LocalizedString(
                task.Description.TableReference,
                task.Description.TableEntryReference);

            // Set up variables BEFORE assigning to StringReference
            // to avoid SmartFormat errors during auto-refresh
            task.Data.SetupTaskLocalizedVariables(localizedString, task);

            // Now assign - the refresh will have the variables already
            taskDescriptionText.StringReference = localizedString;

            // Log variables for debugging
            LogLocalizedVariables(taskDescriptionText, task.DevName);

            // Animate text appearance
            if (taskDescriptionTextMesh != null)
                Tween.Alpha(taskDescriptionTextMesh, 0f, 1f, 0.25f, Ease.OutQuad);
        }

        private void LogLocalizedVariables(LocalizeStringEvent localizeEvent, string context)
        {
            if (localizeEvent?.StringReference == null) return;

            var stringRef = localizeEvent.StringReference;
            var varNames = new System.Text.StringBuilder();
            int count = 0;

            // Check common variable names
            string[] commonVars = { "current", "required", "target", "time", "remaining", "object", "location" };
            foreach (var varName in commonVars)
            {
                if (stringRef.TryGetValue(varName, out var variable))
                {
                    if (varNames.Length > 0) varNames.Append(", ");
                    varNames.Append($"{varName}={variable}");
                    count++;
                }
            }

            if (count == 0)
            {
                LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] No localized variables set for: {context}");
            }
            else
            {
                LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] Localized variables for '{context}': {varNames}");
            }
        }

        #endregion

        #region Private Methods - Events

        private void SubscribeToQuestEvents(QuestRuntime quest)
        {
            LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] Subscribing to quest events: {quest.QuestData.DevName}");
            quest.OnAnyTaskStarted.SafeSubscribe(HandleTaskUpdated);
            quest.OnAnyTaskUpdated.SafeSubscribe(HandleTaskUpdated);
            quest.OnAnyTaskCompleted.SafeSubscribe(HandleTaskUpdated);
            quest.OnStageTransition.SafeSubscribe(HandleStageTransition);
            quest.OnChoicesAvailable.SafeSubscribe(HandleChoicesAvailable);
            quest.OnChoiceMade.SafeSubscribe(HandleChoiceMade);
        }

        private void UnsubscribeFromQuestEvents()
        {
            if (_currentQuest == null) return;

            LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] Unsubscribing from quest events: {_currentQuest.QuestData?.DevName}");
            _currentQuest.OnAnyTaskStarted.SafeUnsubscribe(HandleTaskUpdated);
            _currentQuest.OnAnyTaskUpdated.SafeUnsubscribe(HandleTaskUpdated);
            _currentQuest.OnAnyTaskCompleted.SafeUnsubscribe(HandleTaskUpdated);
            _currentQuest.OnStageTransition.SafeUnsubscribe(HandleStageTransition);
            _currentQuest.OnChoicesAvailable.SafeUnsubscribe(HandleChoicesAvailable);
            _currentQuest.OnChoiceMade.SafeUnsubscribe(HandleChoiceMade);
        }

        private void HandleStageTransition(QuestRuntime quest, StageTransitionInfo info)
        {
            if (quest != _currentQuest) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Stage transition: {info.PreviousStageIndex} → {info.NewStageIndex}");

            _isTransitioningStage = true;
            try
            {
                // Update stage display
                UpdateStageInfo(quest);

                // Clear previous stage's UI elements
                ClearTaskItems();
                ClearChoiceButtons();

                // Rebuild task list to show new stage's tasks
                CreateTaskItems(quest);
                SelectInitialTask(quest);

                // Check for choices in new stage (if no active tasks)
                CheckForPendingChoices();

                // Animate stage transition (optional visual feedback)
                if (stageNameText != null)
                    Tween.Alpha(stageNameText, 0f, 1f, 0.3f, Ease.OutQuad);
            }
            finally
            {
                _isTransitioningStage = false;
            }
        }

        private void HandleTaskUpdated(QuestRuntime quest, TaskRuntime task)
        {
            if (quest != _currentQuest) return;

            LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] Task updated: {task.DevName} ({task.CurrentState})");

            // Update progress display
            if (progressionText != null)
                progressionText.text = $"{QuestUtils.GetPercentage(_currentQuest.CurrentProgress)}%";

            // Update stage info (stage progress may have changed)
            UpdateStageInfo(_currentQuest);

            // Handle new in-progress tasks (for parallel groups)
            // Skip during stage transitions to avoid race conditions
            if (!_isTransitioningStage)
                AddNewInProgressTasks();

#if UNITY_EDITOR
            UpdateDebugButtons();
#endif
        }

        private void AddNewInProgressTasks()
        {
            if (_currentQuest == null) return;

            var inProgressTasks = _currentQuest.Tasks
                .Where(t => t.CurrentState == TaskState.InProgress)
                .ToList();

            bool addedNew = false;
            foreach (var task in inProgressTasks)
            {
                // Check if already displayed
                if (_taskItems.Any(item => item.Task == task))
                    continue;

                // Create new task item
                Log(LogSubsystem.UI, $"[UI_QuestDetails] Adding new task item: {task.DevName}");
                var taskItem = Instantiate(taskItemPrefab, tasksHolder);
                taskItem.Setup(task, HandleTaskSelected);
                taskItem.SetToggleGroup(taskToggleGroup);
                _taskItems.Add(taskItem);
                addedNew = true;
            }

            // Auto-select first new task if current task is no longer in progress
            if (addedNew && _currentTask?.CurrentState != TaskState.InProgress)
            {
                var firstInProgress = _taskItems.FirstOrDefault(item => item.Task.CurrentState == TaskState.InProgress);
                firstInProgress?.SelectTask();
            }
        }

        #endregion

        #region Private Methods - Choices

        private void HandleChoicesAvailable(QuestRuntime quest, List<StageTransition> choices)
        {
            if (quest != _currentQuest) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Choices available: {choices.Count} choice(s)");
            SpawnChoiceButtons(choices);
        }

        private void HandleChoiceMade(QuestRuntime quest, StageTransition choice)
        {
            if (quest != _currentQuest) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Choice made: {choice.ChoiceId}");
            ClearChoiceButtons();
        }

        private void SpawnChoiceButtons(List<StageTransition> choices)
        {
            if (choicesHolder == null || choiceButtonPrefab == null)
            {
                LogVerbose(LogSubsystem.UI, "[UI_QuestDetails] Cannot spawn choices - missing prefab or holder");
                return;
            }

            // Clear existing choice buttons
            ClearChoiceButtons();

            // Show choices holder
            choicesHolder.gameObject.SetActive(true);
            _isShowingChoices = true;

            // Update header text if available
            if (choiceHeaderText != null)
            {
                // Could be localized - for now just show generic text
                choiceHeaderText.gameObject.SetActive(true);
            }

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Spawning {choices.Count} choice button(s)");

            foreach (var choice in choices)
            {
                var button = Instantiate(choiceButtonPrefab, choicesHolder);
                button.name = $"ChoiceButton_{choice.ChoiceId}";

                // Setup button text - try to get localized text from choice
                var buttonText = button.GetComponentInChildren<LocalizeStringEvent>();
                if (buttonText != null && choice.ChoiceText != null && !choice.ChoiceText.IsEmpty)
                {
                    buttonText.StringReference = choice.ChoiceText;
                    buttonText.RefreshString();
                }
                else
                {
                    // Fallback to choice ID as text
                    var tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmpText != null)
                        tmpText.text = choice.ChoiceId;
                }

                // Setup button tooltip if available
                // TODO: Add tooltip support using choice.ChoiceTooltip

                // For UI-based choices (Option B), buttons are always interactable
                // Conditions on player choice transitions are for gameplay triggers (Option C),
                // not for gating UI button availability
                button.SetInteractable(true);

                // Capture choice for lambda
                var capturedChoice = choice;
                button.OnClick.AddListener(() => OnChoiceButtonClicked(capturedChoice));

                _choiceButtons.Add(button);
            }

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Created {_choiceButtons.Count} choice button(s)");
        }

        private void ClearChoiceButtons()
        {
            if (_choiceButtons.Count == 0) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Clearing {_choiceButtons.Count} choice button(s)");

            foreach (var button in _choiceButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _choiceButtons.Clear();

            // Hide choices holder
            if (choicesHolder != null)
                choicesHolder.gameObject.SetActive(false);

            if (choiceHeaderText != null)
                choiceHeaderText.gameObject.SetActive(false);

            _isShowingChoices = false;
        }

        private void OnChoiceButtonClicked(StageTransition choice)
        {
            if (_currentQuest == null) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Choice button clicked: {choice.ChoiceId}");

            // Bypass conditions for UI-based choices (Option B)
            // Conditions are for gameplay triggers (Option C), not UI availability
            bool success = _currentQuest.SelectChoice(choice, bypassConditions: true);
            if (!success)
            {
                LogVerbose(LogSubsystem.UI, $"[UI_QuestDetails] Failed to select choice: {choice.ChoiceId}");
            }
        }

        /// <summary>
        /// Checks if the current stage has choices and displays them.
        /// Called after setup and stage transitions.
        /// </summary>
        private void CheckForPendingChoices()
        {
            if (_currentQuest?.CurrentStage == null) return;

            var choices = _currentQuest.CurrentStage.Data.GetAllPlayerChoices();
            if (choices.Count == 0) return;

            Log(LogSubsystem.UI, $"[UI_QuestDetails] Stage has {choices.Count} pending choice(s)");
            SpawnChoiceButtons(choices);
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR
        private void SetupDebugButtons()
        {
            CompleteCurrentQuestButton?.OnClick.SafeSubscribe(DebugCompleteQuest);
            FailCurrentQuestButton?.OnClick.SafeSubscribe(DebugFailQuest);
            ResetCurrentQuestButton?.OnClick.SafeSubscribe(DebugResetQuest);
            UpdateDebugButtons();
        }

        private void SetupTaskDebugButtons()
        {
            CompleteCurrentTaskButton?.OnClick.SafeSubscribe(DebugCompleteTask);
            FailCurrentTaskButton?.OnClick.SafeSubscribe(DebugFailTask);
            ResetCurrentTaskButton?.OnClick.SafeSubscribe(DebugResetTask);
            IncrementCurrentTaskButton?.OnClick.SafeSubscribe(DebugIncrementTask);
            DecrementCurrentTaskButton?.OnClick.SafeSubscribe(DebugDecrementTask);
            InvokeEventTaskButton?.OnClick.SafeSubscribe(DebugEventTask);
        }

        private void UpdateDebugButtons()
        {
            if (_currentTask != null)
            {
                bool isInProgress = _currentTask.CurrentState == TaskState.InProgress;
                bool isCompleted = _currentTask.CurrentState == TaskState.Completed;

                CompleteCurrentTaskButton?.SetInteractable(isInProgress);
                IncrementCurrentTaskButton?.SetInteractable(isInProgress);
                DecrementCurrentTaskButton?.SetInteractable(isInProgress || isCompleted);
                FailCurrentTaskButton?.SetInteractable(isInProgress || isCompleted);
                ResetCurrentTaskButton?.SetInteractable(_currentTask.CurrentState != TaskState.NotStarted);
            }

            if (_currentQuest != null)
            {
                bool isInProgress = _currentQuest.CurrentState == QuestState.InProgress;

                CompleteCurrentQuestButton?.SetInteractable(isInProgress);
                FailCurrentQuestButton?.SetInteractable(isInProgress);
                ResetCurrentQuestButton?.SetInteractable(_currentQuest.CurrentState != QuestState.NotStarted);
            }
        }

        private void DebugCompleteQuest()
        {
            if (_currentQuest == null) return;
            foreach (TaskRuntime task in _currentQuest.Tasks)
                task.CompleteTask();
        }

        private void DebugFailQuest()
        {
            _currentQuest?.FailQuest();
        }

        private void DebugResetQuest()
        {
            if (_currentQuest == null) return;
            QuestManager.Instance.RestartQuest(_currentQuest.QuestData);
        }

        private void DebugCompleteTask()
        {
            _currentTask?.CompleteTask();
        }

        private void DebugFailTask()
        {
            _currentTask?.FailTask();
        }

        private void DebugResetTask()
        {
            if (_currentQuest == null || _currentTask == null) return;

            int index = _currentQuest.Tasks.IndexOf(_currentTask);
            for (int i = index; i < _currentQuest.Tasks.Count; i++)
                _currentQuest.Tasks[i].ResetTask();

            _currentTask.StartTask();
        }

        private void DebugIncrementTask()
        {
            _currentTask?.IncrementStep();
        }

        private void DebugDecrementTask()
        {
            _currentTask?.DecrementStep();
        }

        private void DebugEventTask()
        {
            if (_currentTask == null || _currentTask.CurrentState != TaskState.InProgress)
            {
                Log(LogSubsystem.UI, $"[DebugEventTask] Skipped - task is null or not in progress. Task: {_currentTask?.DevName}, State: {_currentTask?.CurrentState}");
                return;
            }

            Log(LogSubsystem.UI, $"[DebugEventTask] Called for task: {_currentTask.DevName} ({_currentTask.GetType().Name})");

            // Try to find an event-driven condition to fulfill
            if (_currentTask.Data?.Conditions != null)
            {
                int conditionCount = _currentTask.Data.Conditions.Count;
                Log(LogSubsystem.UI, $"[DebugEventTask] Task has {conditionCount} condition(s)");

                // For discovery tasks, check the task's fulfilled conditions set instead of Evaluate()
                var discoveryTask = _currentTask as DiscoveryTaskRuntime;

                // First pass: find unfulfilled conditions (for multi-condition tasks)
                foreach (Condition_SO condition in _currentTask.Data.Conditions)
                {
                    if (condition is not IConditionEventDriven conditionEventDriven)
                    {
                        Log(LogSubsystem.UI, $"[DebugEventTask] Condition '{condition.name}' is not event-driven, skipping");
                        continue;
                    }

                    // For discovery tasks, check if condition is already in the fulfilled set
                    // For other tasks, use Evaluate()
                    bool isAlreadyFulfilled = discoveryTask != null
                        ? discoveryTask.FulfilledConditions.Contains(condition)
                        : condition.Evaluate();

                    Log(LogSubsystem.UI, $"[DebugEventTask] Condition '{condition.name}' fulfilled={isAlreadyFulfilled}");

                    if (isAlreadyFulfilled) continue; // Skip already fulfilled

                    Log(LogSubsystem.UI, $"[DebugEventTask] Calling ForceFulfillCondition() on '{condition.name}'");
                    conditionEventDriven.ForceFulfillCondition();
                    return;
                }

                // Second pass: if all conditions are fulfilled but task still needs progress,
                // re-trigger the first event-driven condition (for repeatable tasks like "collect 3 items")
                // Note: Discovery tasks have duplicate protection, so this won't work for them
                if (discoveryTask == null)
                {
                    Log(LogSubsystem.UI, $"[DebugEventTask] All conditions fulfilled, checking if task needs more progress...");
                    foreach (Condition_SO condition in _currentTask.Data.Conditions)
                    {
                        if (condition is not IConditionEventDriven conditionEventDriven) continue;

                        Log(LogSubsystem.UI, $"[DebugEventTask] Re-triggering condition '{condition.name}' for repeatable task");
                        conditionEventDriven.ForceFulfillCondition();
                        return;
                    }
                }
                else
                {
                    Log(LogSubsystem.UI, $"[DebugEventTask] All {discoveryTask.DiscoveredCount}/{discoveryTask.RequiredDiscoveries} conditions fulfilled for discovery task");
                }
            }
            else
            {
                Log(LogSubsystem.UI, "[DebugEventTask] Task has no conditions");
            }

            // No event-driven conditions found - fall back to increment step
            Log(LogSubsystem.UI, "[DebugEventTask] Falling back to IncrementStep()");
            _currentTask.IncrementStep();
        }

        private void DebugSelectChoice(int choiceIndex)
        {
            if (_currentQuest == null || _currentQuest.CurrentState != QuestState.InProgress)
            {
                Log(LogSubsystem.UI, "[DebugSelectChoice] No active quest");
                return;
            }

            var currentStage = _currentQuest.CurrentStage;
            if (currentStage == null)
            {
                Log(LogSubsystem.UI, "[DebugSelectChoice] No current stage");
                return;
            }

            var choices = currentStage.Data.GetAllPlayerChoices();
            if (choices == null || choices.Count == 0)
            {
                Log(LogSubsystem.UI, "[DebugSelectChoice] No choices available in current stage");
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                Log(LogSubsystem.UI, $"[DebugSelectChoice] Choice index {choiceIndex} out of range (0-{choices.Count - 1})");
                return;
            }

            var choice = choices[choiceIndex];
            Log(LogSubsystem.UI, $"[DebugSelectChoice] Selecting choice '{choice.ChoiceId}' (index {choiceIndex})");

            // Bypass conditions for debug/UI-based choices
            bool success = _currentQuest.SelectChoice(choice, bypassConditions: true);
            Log(LogSubsystem.UI, $"[DebugSelectChoice] SelectChoice result: {success}");
        }
#endif

        #endregion
    }
}
