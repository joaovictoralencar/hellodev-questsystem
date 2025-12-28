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

            // Subscribe to toggle events - OnToggleOn fires when toggle becomes active
            toggle.OnToggleOn.AddListener(HandleToggleOn);
            toggle.OnToggleOff.AddListener(HandleToggleOff);
            // Highlighted fires when controller/mouse hovers
            toggle.HighlightedStateEvent.AddListener(HandleHighlighted);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
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

            // Clean up previous task if re-using this item
            if (_isInitialized)
                UnsubscribeFromEvents();

            _task = task;
            _onTaskSelectedCallback = onTaskSelected;
            gameObject.name = $"TaskItem_{task.Data.DevName}";

            SetupLocalizedText();
            ApplyStateVisuals(task.CurrentState);
            SubscribeToEvents();

            _isInitialized = true;
        }

        /// <summary>
        /// Programmatically selects this task item.
        /// Use this for initial selection or navigation.
        /// </summary>
        public void SelectTask()
        {
            if (_task == null || toggle == null) return;

            toggle.SetIsOn(true);
            toggle.Toggle.Select(); // Set EventSystem focus for controller
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

        #region Private Methods - Event Handlers

        private void HandleToggleOn()
        {
            // Invoke callback to notify parent that this task was selected
            _onTaskSelectedCallback?.Invoke(_task);

            // Animate selection background
            if (selectedBackground != null)
            {
                selectedBackground.enabled = true;
                Tween.UIFillAmount(selectedBackground, 1f, 0.25f, Ease.OutCubic);
            }
        }

        private void HandleToggleOff()
        {
            // Animate deselection
            if (selectedBackground != null)
            {
                Tween.UIFillAmount(selectedBackground, 0f, 0.15f, Ease.InCubic)
                    .OnComplete(() => selectedBackground.enabled = false);
            }
        }

        private void HandleHighlighted()
        {
            // When highlighted via controller navigation but not yet selected,
            // we can optionally auto-select for smoother navigation
            // For now, just ensure visual feedback without changing selection
        }

        private void HandleTaskUpdated(TaskRuntime task)
        {
            // Refresh localized text with updated values (e.g., progress counters)
            if (task?.Data != null && taskNameText != null)
                task.Data.SetupTaskLocalizedVariables(taskNameText, task);
        }

        private void HandleTaskCompleted(TaskRuntime task)
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(true);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = completedColour;
        }

        private void HandleTaskFailed(TaskRuntime task)
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(false);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = failedColour;
        }

        private void HandleTaskInProgress()
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(false);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = inProgressColour;

            // Clear the started handler since we're now in progress
            if (_onTaskStartedHandler != null && _task != null)
            {
                _task.OnTaskStarted.SafeUnsubscribe(_onTaskStartedHandler);
                _onTaskStartedHandler = null;
            }
        }

        private void HandleTaskNotStarted()
        {
            // Hide until task starts
            gameObject.SetActive(false);

            // Subscribe to start event to show when task begins
            _onTaskStartedHandler = _ => HandleTaskInProgress();
            _task.OnTaskStarted.SafeSubscribe(_onTaskStartedHandler);
        }

        #endregion

        #region Private Methods - Setup

        private void SetupLocalizedText()
        {
            if (taskNameText == null || _task?.Data == null) return;

            // Disable to prevent format errors before variables are set
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

        private void SubscribeToEvents()
        {
            if (_task == null) return;

            _task.OnTaskUpdated.SafeSubscribe(HandleTaskUpdated);
            _task.OnTaskCompleted.SafeSubscribe(HandleTaskCompleted);
            _task.OnTaskFailed.SafeSubscribe(HandleTaskFailed);
        }

        private void UnsubscribeFromEvents()
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