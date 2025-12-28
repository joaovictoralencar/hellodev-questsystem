using System;
using System.Linq;
using PrimeTween;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.UI.Default;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample
{
    /// <summary>
    /// UI component representing a single quest item within a quest section.
    /// Handles display of quest information, status indicators, and selection.
    /// </summary>
    [RequireComponent(typeof(UIToggle))]
    public class UI_QuestItem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Quest Information")]
        [SerializeField] private LocalizeStringEvent questNameText;
        [SerializeField] private LocalizeStringEvent questLocationText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI progressionText;

        [Header("Toggle")]
        [SerializeField] private UIToggle toggle;

        [Header("Status Indicators")]
        [SerializeField] private Image questColourMarker;
        [SerializeField] private RectTransform questStatusIndicatorHolder;
        [SerializeField] private RectTransform completedIndicatorPrefab;
        [SerializeField] private RectTransform failedIndicatorPrefab;

        [Header("Status Text")]
        [SerializeField] private RectTransform questStatusHolder;
        [SerializeField] private LocalizeStringEvent nextTaskTextPrefab;
        [SerializeField] private RectTransform completedTextPrefab;
        [SerializeField] private RectTransform failedTextPrefab;

        [Header("Selection Visuals")]
        [SerializeField] private Image selectableImage;
        [SerializeField] private Color selectedStateColour;

        #endregion

        #region Private Fields

        private Action<QuestRuntime> _onQuestSelectedCallback;
        private QuestRuntime _quest;
        private Color _originalColor;
        private bool _isInitialized;

        #endregion

        #region Public Properties

        public QuestRuntime Quest => _quest;
        public UIToggle Toggle => toggle;
        public bool IsSelected => toggle != null && toggle.IsOn;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (toggle == null) TryGetComponent(out toggle);
            if (selectableImage != null)
                _originalColor = selectableImage.color;

            // Toggle events for selection
            toggle.OnToggleOn.AddListener(HandleToggleOn);
            toggle.OnToggleOff.AddListener(HandleToggleOff);
        }

        private void OnDestroy()
        {
            Tween.StopAll(transform);
            UnsubscribeFromQuestEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the quest item with quest data and selection callback.
        /// </summary>
        public void Setup(QuestRuntime newQuest, Action<QuestRuntime> onQuestSelectedCallback)
        {
            if (newQuest?.QuestData == null) return;

            // Clean up previous quest if re-using
            if (_isInitialized)
                UnsubscribeFromQuestEvents();

            _quest = newQuest;
            _onQuestSelectedCallback = onQuestSelectedCallback;
            gameObject.name = $"QuestItem_{newQuest.QuestData.DevName}";

            SetupQuestDisplay();
            SetupQuestStateVisuals();
            SubscribeToQuestEvents();

            _isInitialized = true;
        }

        /// <summary>
        /// Programmatically selects this quest item and sets EventSystem focus.
        /// </summary>
        public void SelectQuest()
        {
            if (_quest == null || toggle == null) return;

            toggle.SetIsOn(true);
            toggle.Toggle.Select();
        }

        /// <summary>
        /// Sets the toggle group for mutual exclusion across all quest items.
        /// </summary>
        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            if (toggle?.Toggle != null)
                toggle.Toggle.group = toggleGroup;
        }

        /// <summary>
        /// Sets the color of the quest type marker.
        /// </summary>
        public void SetupMarkerColour(Color questTypeColor)
        {
            if (questColourMarker != null)
                questColourMarker.color = questTypeColor;
        }

        /// <summary>
        /// Sets the toggle state without triggering selection callback.
        /// Used for synchronizing toggle states from parent container.
        /// </summary>
        public void SetToggleIsOn(bool isOn)
        {
            if (toggle != null)
                toggle.SetIsOn(isOn);
        }

        #endregion

        #region Private Methods - Toggle Handlers

        private void HandleToggleOn()
        {
            // Notify parent of selection
            _onQuestSelectedCallback?.Invoke(_quest);

            // Apply selected visuals
            if (selectableImage != null)
                selectableImage.color = selectedStateColour;

            // Scale animation for emphasis
            Tween.Scale(transform, 1.035f, 0.2f, Ease.OutBack);
        }

        private void HandleToggleOff()
        {
            // Revert to original visuals
            if (selectableImage != null)
                selectableImage.color = _originalColor;

            // Scale back to normal
            if (transform.localScale.x > 1f)
                Tween.Scale(transform, 1f, 0.15f, Ease.InBack);
        }

        #endregion

        #region Private Methods - Quest State

        private void HandleQuestUpdated(QuestRuntime updatedQuest)
        {
            if (progressionText != null)
                progressionText.text = $"{QuestUtils.GetPercentage(updatedQuest.CurrentProgress)}%";
        }

        private void HandleQuestCompleted(QuestRuntime questData)
        {
            ClearStatusElements();
            CreateStatusIndicator(completedIndicatorPrefab);
            CreateStatusText(completedTextPrefab);

            // Unsubscribe since we're done tracking this state
            questData.OnQuestCompleted.SafeUnsubscribe(HandleQuestCompleted);
        }

        private void HandleQuestFailed(QuestRuntime questData)
        {
            ClearStatusElements();
            CreateStatusIndicator(failedIndicatorPrefab);
            CreateStatusText(failedTextPrefab);
        }

        private void HandleQuestInProgress(QuestRuntime questData)
        {
            ClearStatusText();

            // Subscribe to completion
            questData.OnQuestCompleted.SafeSubscribe(HandleQuestCompleted);

            // Show next active task
            TaskRuntime nextTask = questData.Tasks.FirstOrDefault(t => t.CurrentState == TaskState.InProgress);
            if (nextTask != null)
                DisplayNextTask(nextTask);
        }

        #endregion

        #region Private Methods - Setup

        private void SetupQuestDisplay()
        {
            if (questNameText != null)
            {
                questNameText.StringReference = _quest.QuestData.DisplayName;
                questNameText.RefreshString();
            }

            if (questLocationText != null)
            {
                questLocationText.StringReference = _quest.QuestData.QuestLocation;
                questLocationText.RefreshString();
            }

            if (levelText != null)
                levelText.text = _quest.QuestData.RecommendedLevel.ToString();

            if (progressionText != null)
                progressionText.text = $"{QuestUtils.GetPercentage(_quest.CurrentProgress)}%";
        }

        private void SetupQuestStateVisuals()
        {
            switch (_quest.CurrentState)
            {
                case QuestState.InProgress:
                    HandleQuestInProgress(_quest);
                    break;
                case QuestState.Completed:
                    HandleQuestCompleted(_quest);
                    break;
                case QuestState.Failed:
                    HandleQuestFailed(_quest);
                    break;
            }
        }

        private void SubscribeToQuestEvents()
        {
            if (_quest == null) return;
            _quest.OnQuestUpdated.SafeSubscribe(HandleQuestUpdated);
        }

        private void UnsubscribeFromQuestEvents()
        {
            if (_quest == null) return;
            _quest.OnQuestUpdated.SafeUnsubscribe(HandleQuestUpdated);
            _quest.OnQuestCompleted.SafeUnsubscribe(HandleQuestCompleted);
        }

        #endregion

        #region Private Methods - UI Helpers

        private void DisplayNextTask(TaskRuntime task)
        {
            if (nextTaskTextPrefab == null || questStatusHolder == null) return;

            LocalizeStringEvent nextTaskText = Instantiate(nextTaskTextPrefab, questStatusHolder);
            nextTaskText.StringReference = task.DisplayName;
            task.Data.SetupTaskLocalizedVariables(nextTaskText, task);
        }

        private void CreateStatusIndicator(RectTransform prefab)
        {
            if (prefab != null && questStatusIndicatorHolder != null)
                Instantiate(prefab, questStatusIndicatorHolder);
        }

        private void CreateStatusText(RectTransform prefab)
        {
            if (prefab != null && questStatusHolder != null)
                Instantiate(prefab, questStatusHolder);
        }

        private void ClearStatusElements()
        {
            ClearStatusIndicators();
            ClearStatusText();
        }

        private void ClearStatusIndicators()
        {
            if (questStatusIndicatorHolder != null)
                questStatusIndicatorHolder.DestroyAllChildren();
        }

        private void ClearStatusText()
        {
            // Keep first child (base element), remove additional status text
            if (questStatusHolder != null && questStatusHolder.childCount > 1)
                Destroy(questStatusHolder.GetChild(1).gameObject);
        }

        #endregion
    }
}