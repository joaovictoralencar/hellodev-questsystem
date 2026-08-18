using System;
using HelloDev.Objectives;
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
        private UnityAction<IObjective> _onTaskStartedHandler;
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
            toggle.OnValueChanged.AddListener(HandleToggleValueChanged);
        }

        private void OnDestroy()
        {
            if (toggle != null)
                toggle.OnValueChanged.RemoveListener(HandleToggleValueChanged);
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

            // Initialize selection visual to hidden state
            if (selectedBackground != null)
            {
                Tween.StopAll(selectedBackground);
                selectedBackground.fillAmount = 0f;
                selectedBackground.enabled = false;
            }

            SetupLocalizedText();
            ApplyStateVisuals(task.State);
            SubscribeToTaskEvents();

            _isInitialized = true;
        }

        /// <summary>
        /// Programmatically selects this task item.
        /// </summary>
        public void SelectTask()
        {
            if (_task == null || toggle == null) return;

            toggle.IsOn = true;
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

        private void HandleToggleValueChanged(bool value)
        {
            if (value)
                HandleToggleOn();
            else
                HandleToggleOff();
        }
        
        private void HandleToggleOff()
        {
            HideSelectionVisual();
            _onTaskSelectedCallback?.Invoke(_task);
        }

        private void HandleToggleOn()
        {
            ShowSelectionVisual();
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

        private void HandleTaskUpdated(IObjective task)
        {
            var taskRuntime = task as TaskRuntime;
            
            if (taskRuntime?.Data == null || taskNameText?.StringReference == null) return;

            // Update variables on the existing StringReference and refresh
            taskRuntime.Data.SetupTaskLocalizedVariables(taskNameText.StringReference, taskRuntime);
            taskNameText.RefreshString();
        }

        private void HandleTaskCompleted(IObjective task)
        {
            gameObject.SetActive(true);
            if (taskCheckmark != null) taskCheckmark.SetActive(true);
            if (textStyleUpdater != null) textStyleUpdater.TextColourSO = completedColour;
            toggle?.SetInteractable(true);
        }

        private void HandleTaskFailed(IObjective task)
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
                _task.Started.SafeUnsubscribe(_onTaskStartedHandler);
                _onTaskStartedHandler = null;
            }
        }

        private void HandleTaskNotStarted()
        {
            gameObject.SetActive(false);

            _onTaskStartedHandler = _ => HandleTaskInProgress();
            _task.Started.SafeSubscribe(_onTaskStartedHandler);
        }

        #endregion

        #region Private Methods - Setup

        private void SetupLocalizedText()
        {
            if (taskNameText == null || _task?.Data?.DisplayName == null) return;

            // Set up variables on the original DisplayName BEFORE assigning
            // The variables persist on the LocalizedString instance
            _task.Data.SetupTaskLocalizedVariables(_task.Data.DisplayName, _task);

            // Now assign - the refresh will have the variables already
            taskNameText.StringReference = _task.Data.DisplayName;
        }

        private void ApplyStateVisuals(State state)
        {
            switch (state)
            {
                case State.NotStarted:
                    HandleTaskNotStarted();
                    break;
                case State.InProgress:
                    HandleTaskInProgress();
                    break;
                case State.Completed:
                    HandleTaskCompleted(_task);
                    break;
                case State.Failed:
                    HandleTaskFailed(_task);
                    break;
            }
        }

        private void SubscribeToTaskEvents()
        {
            if (_task == null) return;

            _task.Updated.SafeSubscribe(HandleTaskUpdated);
            _task.Completed.SafeSubscribe(HandleTaskCompleted);
            _task.Failed.SafeSubscribe(HandleTaskFailed);
        }

        private void UnsubscribeFromTaskEvents()
        {
            if (_task == null) return;

            _task.Updated.SafeUnsubscribe(HandleTaskUpdated);
            _task.Completed.SafeUnsubscribe(HandleTaskCompleted);
            _task.Failed.SafeUnsubscribe(HandleTaskFailed);

            if (_onTaskStartedHandler != null)
            {
                _task.Started.SafeUnsubscribe(_onTaskStartedHandler);
                _onTaskStartedHandler = null;
            }
        }

        #endregion
    }
}
