using System;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using DG.Tweening;

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
        private Task _task;
        private Action<Task> OnTaskSelected;

        public Task Task => _task;

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
            selectedBackground.DOKill();
        }

        public void Setup(Task task, Action<Task> onTaskSelected)
        {
            gameObject.name = task.Data.DevName;
            _task = task;
            TaskNameText.StringReference = task.Data.DisplayName;
            task.Data.SetupTaskLocalizedVariables(TaskNameText, task);
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
        private void OnTaskUpdated(Task task)
        {
            task.Data.SetupTaskLocalizedVariables(TaskNameText, task);
        }

        private void UnSubscribeToEvents()
        {
            if (_task == null) return;
            _task.OnTaskUpdated.SafeUnsubscribe(OnTaskUpdated);
            _task.OnTaskCompleted.SafeUnsubscribe(OnTaskCompleted);
            _task.OnTaskFailed.SafeUnsubscribe(OnTaskFailed);
        }


        private void OnTaskCompleted(Task task)
        {
            gameObject.SetActive(true);
            TaskCheck.SetActive(true);
            TextStyleUpdater.TextColourSO = CompletedColour;
        }

        private void OnTaskFailed(Task task)
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
            _task.OnTaskStarted.SafeUnsubscribe((t)=> OnTaskInProgress());
        }

        private void OnTaskNotStarted()
        {
            gameObject.SetActive(false);
            _task.OnTaskStarted.SafeSubscribe((t)=> OnTaskInProgress());
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
            selectedBackground.DOFillAmount(1, 0.35f).SetEase(Ease.OutBack);
            Toggle.Toggle.Select();
        }

        public void OnDeselect()
        {
            if (Toggle.IsOn) return;
            selectedBackground.enabled = false;
            selectedBackground.DOFillAmount(0, 0.2f).SetEase(Ease.InBack);
        }

        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            Toggle.Toggle.group = toggleGroup;
        }
    }
}