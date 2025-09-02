using System;
using HelloDev.QuestSystem.Tasks;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    [RequireComponent(typeof(Selectable))]
    public class UI_TaskItem : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private LocalizeStringEvent TaskNameText;
        [SerializeField] private GameObject TaskCheck;

        [Header("Status style")] [SerializeField]
        private Colour_SO NotCompletedColour;

        [SerializeField] private Colour_SO CompletedColour;
        [SerializeField] private Colour_SO FailedColour;
        [SerializeField] private TextStyleUpdater TextStyleUpdater;
        private Selectable Selectable;
        private Task _task;
        private Action<Task> OnTaskSelected;

        private void OnDestroy()
        {
            UnSubscribeToEvents();
        }

        public void Setup(Task task, Action<Task> onTaskSelected)
        {
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

            _task = task;
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
        }

        private void OnTaskNotStarted()
        {
            gameObject.SetActive(false);
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
            OnTaskSelected.Invoke(_task);
        }
    }
}