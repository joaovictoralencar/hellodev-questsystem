using System.Collections.Generic;
using System.Linq;
using PrimeTween;
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
    public partial class UI_QuestDetails : MonoBehaviour
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

            OnSetupDebug();
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
                int currentIndex = quest.CurrentStageIndex + 1;
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

            if (targetTask == null) return;

            var taskItem = _taskItems.FirstOrDefault(item => item.Task == targetTask);
            if (taskItem != null)
            {
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

            OnTaskSelectedDebug();
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

            // Animate text appearance
            if (taskDescriptionTextMesh != null)
                Tween.Alpha(taskDescriptionTextMesh, 0f, 1f, 0.25f, Ease.OutQuad);
        }

        #endregion

        #region Private Methods - Events

        private void SubscribeToQuestEvents(QuestRuntime quest)
        {
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

            // Update progress display
            if (progressionText != null)
                progressionText.text = $"{QuestUtils.GetPercentage(_currentQuest.CurrentProgress)}%";

            // Update stage info (stage progress may have changed)
            UpdateStageInfo(_currentQuest);

            // Handle new in-progress tasks (for parallel groups)
            // Skip during stage transitions to avoid race conditions
            if (!_isTransitioningStage)
                AddNewInProgressTasks();

            OnTaskUpdatedDebug();
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
            if (choicesHolder == null || choiceButtonPrefab == null) return;

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

                // For UI-based choices (Option B), buttons are always interactable
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
            _currentQuest.SelectChoice(choice, bypassConditions: true);
        }

        /// <summary>
        /// Checks if the current stage has choices and displays them.
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

        #region Debug Hooks (Partial Methods)

        /// <summary>Called after Setup completes. Override in debug partial class.</summary>
        partial void OnSetupDebug();

        /// <summary>Called when a task is selected. Override in debug partial class.</summary>
        partial void OnTaskSelectedDebug();

        /// <summary>Called when a task is updated. Override in debug partial class.</summary>
        partial void OnTaskUpdatedDebug();

        #endregion
    }
}
