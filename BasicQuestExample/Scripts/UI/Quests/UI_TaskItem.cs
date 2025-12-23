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
    [RequireComponent(typeof(UIToggle))]
    public class UI_TaskItem : MonoBehaviour
    {
        [SerializeField] private UIToggle Toggle;
        [SerializeField] private LocalizeStringEvent TaskNameText;
        [SerializeField] private GameObject TaskCheck;
        [SerializeField] private Image selectedBackground;

        [Header("Status style")] [SerializeField]
        private Colour_SO NotCompletedColour;

        [SerializeField] private Colour_SO CompletedColour;
        [SerializeField] private Colour_SO FailedColour;
        [SerializeField] private TextStyleUpdater TextStyleUpdater;
        private TaskRuntime _task;
        private Action<TaskRuntime> OnTaskSelected;
        private UnityAction<TaskRuntime> _onTaskStartedCallback;

        public TaskRuntime Task => _task;

        private void Awake()
        {
            if (Toggle == null) TryGetComponent(out Toggle);
            Toggle.OnToggleOn.AddListener(OnSelect);
            Toggle.NormalStateEvent.AddListener(OnDeselect);
            Toggle.HighlightedStateEvent.AddListener(Select);
        }

        private void OnDestroy()
        {
            UnSubscribeToEvents();
            Tween.StopAll(selectedBackground);
        }

        public void Setup(TaskRuntime task, Action<TaskRuntime> onTaskSelected)
        {
            gameObject.name = task.Data.DevName;
            _task = task;
            // Disable component to prevent auto-format before variables are set up
            TaskNameText.enabled = false;
            TaskNameText.StringReference = task.Data.DisplayName;
            task.Data.SetupTaskLocalizedVariables(TaskNameText, task);
            TaskNameText.enabled = true;
            TaskNameText.RefreshString();
            switch (task.CurrentState)
            {
                case TaskState.NotStarted:
                    OnTaskNotStarted();
                    break;
                case TaskState.InProgress:
                    OnTaskInProgress();
                    break;
                case TaskState.Completed:
                    OnTaskCompleted(task);
                    break;
                case TaskState.Failed:
                    OnTaskFailed(task);
                    break;
            }
            OnTaskSelected = onTaskSelected;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _task.OnTaskUpdated.SafeSubscribe(OnTaskUpdated);
            _task.OnTaskCompleted.SafeSubscribe(OnTaskCompleted);
            _task.OnTaskFailed.SafeSubscribe(OnTaskFailed);
        }
        private void OnTaskUpdated(TaskRuntime task)
        {
            task.Data.SetupTaskLocalizedVariables(TaskNameText, task);
        }

        private void UnSubscribeToEvents()
        {
            if (_task == null) return;
            _task.OnTaskUpdated.SafeUnsubscribe(OnTaskUpdated);
            _task.OnTaskCompleted.SafeUnsubscribe(OnTaskCompleted);
            _task.OnTaskFailed.SafeUnsubscribe(OnTaskFailed);
            if (_onTaskStartedCallback != null)
            {
                _task.OnTaskStarted.SafeUnsubscribe(_onTaskStartedCallback);
                _onTaskStartedCallback = null;
            }
        }


        private void OnTaskCompleted(TaskRuntime task)
        {
            gameObject.SetActive(true);
            TaskCheck.SetActive(true);
            TextStyleUpdater.TextColourSO = CompletedColour;
        }

        private void OnTaskFailed(TaskRuntime task)
        {
            gameObject.SetActive(true);
            TaskCheck.SetActive(false);
            TextStyleUpdater.TextColourSO = FailedColour;
        }

        private void OnTaskInProgress()
        {
            gameObject.SetActive(true);
            TaskCheck.SetActive(false);
            TextStyleUpdater.TextColourSO = NotCompletedColour;
            if (_onTaskStartedCallback != null)
            {
                _task.OnTaskStarted.SafeUnsubscribe(_onTaskStartedCallback);
                _onTaskStartedCallback = null;
            }
        }

        private void OnTaskNotStarted()
        {
            gameObject.SetActive(false);
            _onTaskStartedCallback = _ => OnTaskInProgress();
            _task.OnTaskStarted.SafeSubscribe(_onTaskStartedCallback);
        }

        public void Select()
        {
            if (Toggle.IsOn) return;
            OnTaskSelected.Invoke(_task);
            Toggle.SetIsOn(true);
        }

        private void OnSelect()
        {
            selectedBackground.enabled = true;
            if (selectedBackground.fillAmount < 1f)
                Tween.UIFillAmount(selectedBackground, 1f, 0.35f, Ease.OutBack);
            Toggle.Toggle.Select();
        }

        public void OnDeselect()
        {
            if (Toggle.IsOn) return;
            selectedBackground.enabled = false;
            if (selectedBackground.fillAmount > 0f)
                Tween.UIFillAmount(selectedBackground, 0f, 0.2f, Ease.InBack);
        }

        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            Toggle.Toggle.group = toggleGroup;
        }
    }
}