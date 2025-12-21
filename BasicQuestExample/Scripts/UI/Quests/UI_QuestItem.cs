using System;
using System.Linq;
using DG.Tweening;
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
    /// Handles the display of quest information, status indicators, and user interactions.
    /// </summary>
    [RequireComponent(typeof(UIToggle))]
    public class UI_QuestItem : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Quest Information Display")]
        [SerializeField] 
        [Tooltip("Localized text component for displaying the quest name")]
        private LocalizeStringEvent questNameText;
        
        [SerializeField] 
        [Tooltip("Localized text component for displaying the quest location")]
        private LocalizeStringEvent questLocationText;
        
        [SerializeField] 
        [Tooltip("Text component for displaying the recommended level")]
        private TextMeshProUGUI levelText;
        
        [SerializeField] 
        [Tooltip("Text component for displaying quest completion percentage")]
        private TextMeshProUGUI progressionText;
        
        [Header("Toggle"), SerializeField, Tooltip("The toggle component for quest selection")]
        private UIToggle toggle;

        [Header("Quest Status Indicators")] [SerializeField] [Tooltip("The vertical marker for the quest colour on the left")]
        private Image questColourMarker;
        
        [SerializeField] 
        [Tooltip("Container for quest status indicator icons")]
        private RectTransform questStatusIndicatorHolder;
        
        [Space(10)]
        [SerializeField] 
        [Tooltip("Prefab for updated quest indicator")]
        private RectTransform updatedIndicatorPrefab;
        
        [SerializeField] 
        [Tooltip("Prefab for completed quest indicator")]
        private RectTransform completedIndicatorPrefab;
        
        [SerializeField] 
        [Tooltip("Prefab for failed quest indicator")]
        private RectTransform failedIndicatorPrefab;

        [Header("Quest Status Text")]
        [SerializeField] 
        [Tooltip("Container for quest status text elements")]
        private RectTransform questStatusHolder;
        
        [Space(10)]
        [SerializeField] 
        [Tooltip("Prefab for displaying next task text")]
        private LocalizeStringEvent nextTaskTextPrefab;
        
        [SerializeField] 
        [Tooltip("Prefab for completed quest text")]
        private RectTransform completedTextPrefab;
        
        [SerializeField] 
        [Tooltip("Prefab for failed quest text")]
        private RectTransform failedTextPrefab;

        [Header("UI selectable")]
        [SerializeField] private Image selectableImage;
        [SerializeField] private Color selectedStateColour;
        
        #endregion

        #region Private Fields
        
        /// <summary>
        /// Callback invoked when this quest item is selected
        /// </summary>
        private Action<Quest> onQuestSelected;
        
        /// <summary>
        /// The quest data associated with this UI item
        /// </summary>
        private Quest quest;

        private Color originalColor;
        
        #endregion

        #region Public Properties
        
        /// <summary>
        /// Gets the quest associated with this UI item
        /// </summary>
        public Quest Quest => quest;
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (toggle == null) TryGetComponent(out toggle);
            toggle.OnToggleOn.AddListener(SelectQuest);
            toggle.HighlightedStateEvent.AddListener(Select);
            toggle.NormalStateEvent.AddListener(OnDeselect);
            originalColor = selectableImage.color;
        }

        private void OnDestroy()
        {
            UnsubscribeFromQuestEvents();
        }
        
        #endregion

        #region Public Setup Methods
        
        /// <summary>
        /// Sets up the quest item with quest data and selection callback.
        /// Configures the display based on the current quest state.
        /// </summary>
        /// <param name="newQuest">The quest to display</param>
        /// <param name="onQuestSelectedCallback">Callback invoked when quest is selected</param>
        public void Setup(Quest newQuest, Action<Quest> onQuestSelectedCallback)
        {
            if (newQuest?.QuestData == null) return;

            this.quest = newQuest;
            onQuestSelected = onQuestSelectedCallback;
            
            SetupQuestInformation();
            SetupQuestStateDisplay();
            SubscribeToQuestEvents();
        }
        
        #endregion

        #region Quest State Management

        /// <summary>
        /// Sets up the color of the quest marker based on the quest type.
        /// </summary>
        /// <param name="questTypeColor"></param>
        public void SetupMarkerColour(Color questTypeColor)
        {
            questColourMarker.color = questTypeColor;
        }

        /// <summary>
        /// Handles the display when quest is in progress.
        /// Shows the next active task and subscribes to completion events.
        /// </summary>
        /// <param name="questData">The quest in progress</param>
        private void OnQuestInProgress(Quest questData)
        {
            ClearStatusText();
            questData.OnQuestCompleted.SafeSubscribe(OnQuestCompleted);
            
            Task nextActiveTask = GetNextActiveTask(questData);
            if (nextActiveTask != null)
            {
                DisplayNextTask(nextActiveTask);
            }
        }

        /// <summary>
        /// Handles the display when quest is completed.
        /// Shows completion indicators and unsubscribes from events.
        /// </summary>
        /// <param name="questData">The completed quest</param>
        private void OnQuestCompleted(Quest questData)
        {
            ClearStatusText();
            ClearStatusIndicators();
            
            questData.OnQuestCompleted.Unsubscribe(OnQuestCompleted);
            
            CreateStatusIndicator(completedIndicatorPrefab);
            CreateStatusText(completedTextPrefab);
        }

        /// <summary>
        /// Handles the display when quest has failed.
        /// Shows failure indicators and clears previous status.
        /// </summary>
        /// <param name="questData">The failed quest</param>
        private void OnQuestFailed(Quest questData)
        {
            ClearStatusText();
            ClearStatusIndicators();
            
            CreateStatusIndicator(failedIndicatorPrefab);
            CreateStatusText(failedTextPrefab);
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Handles quest update events to refresh the progress display.
        /// </summary>
        /// <param name="updatedQuest">The quest that was updated</param>
        private void OnQuestUpdated(Quest updatedQuest)
        {
            if (progressionText != null)
            {
                progressionText.text = $"{QuestUtils.GetPercentage(updatedQuest.CurrentProgress)}%";
            }
        }
        
        #endregion

        #region Selection Handlers
        
        /// <summary>
        /// Selects this quest item.
        /// </summary>
        public void Select()
        {
            if (toggle.IsOn) return;
            toggle?.SetIsOn(true);
            SelectQuest();
        }

        private void OnSelect()
        {
            transform.DOScale(1.035f, .25f).From(1).SetEase(Ease.OutBack);
            selectableImage.color = selectedStateColour;
            toggle.Toggle.Select();
        }

        public void OnDeselect()
        {
            if (toggle.IsOn) return;
            selectableImage.color = originalColor;
            transform.DOScale(1, .15f).SetEase(Ease.InBack);
        }
        
        #endregion

        #region Private Helper Methods
        
        /// <summary>
        /// Sets up the basic quest information display elements.
        /// </summary>
        private void SetupQuestInformation()
        {
            if (questNameText != null)
            {
                questNameText.StringReference = quest.QuestData.DisplayName;
            }
            
            if (questLocationText != null)
            {
                questLocationText.StringReference = quest.QuestData.QuestLocation;
            }
            
            if (levelText != null)
            {
                levelText.text = quest.QuestData.RecommendedLevel.ToString();
            }
            
            if (progressionText != null)
            {
                progressionText.text = $"{QuestUtils.GetPercentage(quest.CurrentProgress)}%";
            }
        }

        /// <summary>
        /// Configures the display based on the current quest state.
        /// </summary>
        private void SetupQuestStateDisplay()
        {
            switch (quest.CurrentState)
            {
                case QuestState.NotStarted:
                    break;
                case QuestState.InProgress:
                    OnQuestInProgress(quest);
                    break;
                case QuestState.Completed:
                    OnQuestCompleted(quest);
                    break;
                case QuestState.Failed:
                    OnQuestFailed(quest);
                    break;
            }
        }

        /// <summary>
        /// Subscribes to relevant quest events for updates.
        /// </summary>
        private void SubscribeToQuestEvents()
        {
            if (quest?.OnQuestUpdated != null)
            {
                quest.OnQuestUpdated.SafeSubscribe(OnQuestUpdated);
            }
        }

        /// <summary>
        /// Unsubscribes from quest events to prevent memory leaks.
        /// </summary>
        private void UnsubscribeFromQuestEvents()
        {
            if (quest?.OnQuestUpdated != null)
            {
                quest.OnQuestUpdated.Unsubscribe(OnQuestUpdated);
            }
        }

        /// <summary>
        /// Finds the next active task in the quest.
        /// </summary>
        /// <param name="questData">The quest to search</param>
        /// <returns>The next active task, or null if none found</returns>
        private Task GetNextActiveTask(Quest questData)
        {
            return questData.Tasks.FirstOrDefault(task => task.CurrentState == TaskState.InProgress);
        }

        /// <summary>
        /// Displays information about the next active task.
        /// </summary>
        /// <param name="task">The task to display</param>
        private void DisplayNextTask(Task task)
        {
            if (nextTaskTextPrefab == null || questStatusHolder == null) return;

            LocalizeStringEvent nextTaskText = Instantiate(nextTaskTextPrefab, questStatusHolder);
            nextTaskText.StringReference = task.DisplayName;
            task.Data.SetupTaskLocalizedVariables(nextTaskText, task);
        }

        /// <summary>
        /// Creates a status indicator using the specified prefab.
        /// </summary>
        /// <param name="indicatorPrefab">The prefab to instantiate</param>
        private void CreateStatusIndicator(RectTransform indicatorPrefab)
        {
            if (indicatorPrefab != null && questStatusIndicatorHolder != null)
            {
                Instantiate(indicatorPrefab, questStatusIndicatorHolder);
            }
        }

        /// <summary>
        /// Creates status text using the specified prefab.
        /// </summary>
        /// <param name="textPrefab">The text prefab to instantiate</param>
        private void CreateStatusText(RectTransform textPrefab)
        {
            if (textPrefab != null && questStatusHolder != null)
            {
                Instantiate(textPrefab, questStatusHolder);
            }
        }

        /// <summary>
        /// Removes all status indicator elements.
        /// </summary>
        private void ClearStatusIndicators()
        {
            if (questStatusIndicatorHolder != null)
            {
                questStatusIndicatorHolder.DestroyAllChildren();
            }
        }

        /// <summary>
        /// Removes status text elements (keeping the first child as base element).
        /// </summary>
        private void ClearStatusText()
        {
            if (questStatusHolder != null && questStatusHolder.childCount > 1)
            {
                Destroy(questStatusHolder.GetChild(1).gameObject);
            }
        }

        /// <summary>
        /// Invokes the quest selection callback.
        /// </summary>
        private void SelectQuest()
        {
            onQuestSelected?.Invoke(quest);
            OnSelect();
            toggle.Toggle.Select();
        }
        
        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            toggle.Toggle.group = toggleGroup;
        }
        #endregion

        public void SetToggleIsOn(bool isOn)
        {
            toggle.SetIsOn(isOn);
        }
    }
}