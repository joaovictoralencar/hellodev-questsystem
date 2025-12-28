using System;
using PrimeTween;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    /// <summary>
    /// UI component representing a single task item in the quest details panel.
    /// Handles task selection, state visualization, and controller navigation.
    /// </summary>
    [RequireComponent(typeof(UIToggle))]
    public class UI_TaskItem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Components")]
        [SerializeField] private UIToggle toggle;
        [SerializeField] private LocalizeStringEvent taskNameText;
        [SerializeField] private GameObject taskCheckmark;
        [SerializeField] private Image selectedBackground;

        [Header("Status Colors")]
        [SerializeField] private Colour_SO inProgressColour;
        [SerializeField] private Colour_SO completedColour;
        [SerializeField] private Colour_SO failedColour;
        [SerializeField] private TextStyleUpdater textStyleUpdater;

        #endregion

        #region Private Fields

        private TaskRuntime _task;
        private Action<TaskRuntime> _onTaskSelectedCallback;
        private UnityAction<TaskRuntime> _onTaskStartedHandler;
        private bool _isInitialized;

        #endregion

        #region Public Properties

        public TaskRuntime Task => _task;
        public UIToggle Toggle => toggle;
        public bool IsSelected => toggle != null && toggle.IsOn;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (toggle == null) TryGetComponent(out toggle);

            // Subscribe to toggle events
            toggle.OnToggleOn.AddListener(HandleToggleOn);
            toggle.OnShowVisualFeedback.AddListener(ShowSelectionVisual);
            toggle.OnHideVisualFeedback.AddListener(HideSelectionVisual);
        }

        private void OnDestroy()
        {
            UnsubscribeFromTaskEvents();
            if (selectedBackground != null)
                Tween.StopAll(selectedBackground);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the task item with task data and selection callback.
        /// </summary>
        public void Setup(TaskRuntime task, Action<TaskRuntime> onTaskSelected)
        {
            if (task == null) return;

            if (_isInitialized)
                UnsubscribeFromTaskEvents();

            _task = task;
            _onTaskSelectedCallback = onTaskSelected;
            gameObject.name = $"TaskItem_{task.Data.DevName}";

            SetupLocalizedText();
            ApplyStateVisuals(task.CurrentState);
            SubscribeToTaskEvents();

            _isInitialized = true;
        }

        /// <summary>
        /// Programmatically selects this task item.
        /// </summary>
        public void SelectTask()
        {
            if (_task == null || toggle == null) return;

            toggle.SetIsOn(true);
            toggle.Toggle.Select();
        }

        /// <summary>
        /// Sets the toggle group for mutual exclusion.
        /// </summary>
        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            if (toggle?.Toggle != null)
                toggle.Toggle.group = toggleGroup;
        }

        #endregion

        #region Private Methods - Selection

        private void HandleToggleOn()
        {
            _onTaskSelectedCallback?.Invoke(_task);
        }

        private void ShowSelectionVisual()
        {
            if (selectedBackground == null) return;

            selectedBackground.enabled = true;
            if (selectedBackground.fillAmount < 1f)
                Tween.UIFillAmount(selectedBackground, 1f, 0.25f, Ease.OutCubic);
        }

        private void HideSelectionVisual()
        {
            if (selectedBackground == null || selectedBackground.fillAmount <= 0f) return;

            Tween.UIFillAmount(selectedBackground, 0f, 0.15f, Ease.InCubic)
                .OnComplete(() => selectedBackground.enabled = false);
        }

        #endregion

        #region Private Methods - Task State

        private void HandleTaskUpdated(TaskRuntime task)
        {
            if (task?.Data != null && taskNameText != null)
                task.Data.SetupTaskLocalizedVariables(taskNameText, task);
        }

        private void HandleTaskCompleted(TaskRuntime task)
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(true);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = completedColour;
            toggle?.SetInteractable(true);
        }

        private void HandleTaskFailed(TaskRuntime task)
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(false);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = failedColour;
            toggle?.SetInteractable(true);
        }

        private void HandleTaskInProgress()
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(false);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = inProgressColour;
            toggle?.SetInteractable(true);

            if (_onTaskStartedHandler != null && _task != null)
            {
                _task.OnTaskStarted.SafeUnsubscribe(_onTaskStartedHandler);
                _onTaskStartedHandler = null;
            }
        }

        private void HandleTaskNotStarted()
        {
            gameObject.SetActive(false);

            _onTaskStartedHandler = _ => HandleTaskInProgress();
            _task.OnTaskStarted.SafeSubscribe(_onTaskStartedHandler);
        }

        #endregion

        #region Private Methods - Setup

        private void SetupLocalizedText()
        {
            if (taskNameText == null || _task?.Data == null) return;

            taskNameText.enabled = false;
            taskNameText.StringReference = _task.Data.DisplayName;
            _task.Data.SetupTaskLocalizedVariables(taskNameText, _task);
            taskNameText.enabled = true;
            taskNameText.RefreshString();
        }

        private void ApplyStateVisuals(TaskState state)
        {
            switch (state)
            {
                case TaskState.NotStarted:
                    HandleTaskNotStarted();
                    break;
                case TaskState.InProgress:
                    HandleTaskInProgress();
                    break;
                case TaskState.Completed:
                    HandleTaskCompleted(_task);
                    break;
                case TaskState.Failed:
                    HandleTaskFailed(_task);
                    break;
            }
        }

        private void SubscribeToTaskEvents()
        {
            if (_task == null) return;

            _task.OnTaskUpdated.SafeSubscribe(HandleTaskUpdated);
            _task.OnTaskCompleted.SafeSubscribe(HandleTaskCompleted);
            _task.OnTaskFailed.SafeSubscribe(HandleTaskFailed);
        }

        private void UnsubscribeFromTaskEvents()
        {
            if (_task == null) return;

            _task.OnTaskUpdated.SafeUnsubscribe(HandleTaskUpdated);
            _task.OnTaskCompleted.SafeUnsubscribe(HandleTaskCompleted);
            _task.OnTaskFailed.SafeUnsubscribe(HandleTaskFailed);

            if (_onTaskStartedHandler != null)
            {
                _task.OnTaskStarted.SafeUnsubscribe(_onTaskStartedHandler);
                _onTaskStartedHandler = null;
            }
        }

        #endregion
    }
}
