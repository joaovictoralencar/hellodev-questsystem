using System;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using DG.Tweening;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    [RequireComponent(typeof(Selectable))]
    public class UI_TaskItem : MonoBehaviour, ISelectHandler, IPointerEnterHandler, IDeselectHandler
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private LocalizeStringEvent TaskNameText;
        [SerializeField] private GameObject TaskCheck;
        [SerializeField] private Image selectedBackground;

        [Header("Status style")] [SerializeField]
        private Colour_SO NotCompletedColour;

        [SerializeField] private Colour_SO CompletedColour;
        [SerializeField] private Colour_SO FailedColour;
        [SerializeField] private TextStyleUpdater TextStyleUpdater;
        private Selectable Selectable;
        private Task _task;
        private Action<Task> OnTaskSelected;

        public Task Task => _task;

        private void OnDestroy()
        {
            UnSubscribeToEvents();
        }

        public void Setup(Task task, Action<Task> onTaskSelected)
        {
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

        public void Select()
        {
            selectable.Select();
        }

        private void OnTaskUpdated(Task task)
        {
            task.Data.SetupTaskLocalizedVariables(TaskNameText, task);
        }

        private void UnSubscribeToEvents()
        {
            if (_task == null) return;
            _task.OnTaskUpdated.Unsubscribe(OnTaskUpdated);
            _task.OnTaskCompleted.Unsubscribe(OnTaskCompleted);
            _task.OnTaskFailed.Unsubscribe(OnTaskFailed);
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
            _task.OnTaskStarted.Unsubscribe((t)=> OnTaskInProgress());
        }

        private void OnTaskNotStarted()
        {
            gameObject.SetActive(false);
            _task.OnTaskStarted.SafeSubscribe((t)=> OnTaskInProgress());
        }

        public void OnSelect(BaseEventData eventData)
        {
            Select(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Select(eventData);
        }

        private void Select(BaseEventData eventData)
        {
            if (EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            selectedBackground.enabled = true;
            selectedBackground.DOFillAmount(1, 0.35f).SetEase(Ease.OutBack);
            OnTaskSelected.Invoke(_task);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selectedBackground.enabled = false;
            selectedBackground.DOFillAmount(0, 0.2f).SetEase(Ease.InBack);
        }
    }
}