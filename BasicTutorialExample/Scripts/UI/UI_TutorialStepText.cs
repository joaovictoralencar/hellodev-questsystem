using System;
using HelloDev.UI.Default;
using HelloDev.Utils;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicTutorialExample.UI
{
    /// <summary>
    /// State of a tutorial step text item.
    /// </summary>
    public enum TutorialStepTextState
    {
        Pending,
        Active,
        Completed
    }

    /// <summary>
    /// A spawnable text component for displaying a single tutorial instruction line.
    /// Supports localization, state-based coloring via Colour_SO, and checkmark for completed state.
    /// Mimics UI_TaskItem pattern without navigation.
    /// </summary>
    public class UI_TutorialStepText : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Text")]
        [PropertyOrder(0)]
#else
        [Header("Text")]
#endif
        [SerializeField, Tooltip("Primary localized text display.")]
        private LocalizeStringEvent localizedText;

#if ODIN_INSPECTOR
        [TitleGroup("Text")]
        [PropertyOrder(1)]
#endif
        [SerializeField, Tooltip("Fallback TextMeshPro when no localization is available.")]
        private TextMeshProUGUI fallbackText;

#if ODIN_INSPECTOR
        [TitleGroup("Visuals")]
        [PropertyOrder(10)]
#else
        [Header("Visuals")]
#endif
        [SerializeField, Tooltip("Checkmark fill Image that toggles visibility and gets tinted.")]
        private Image checkmarkFill;

#if ODIN_INSPECTOR
        [TitleGroup("Visuals")]
        [PropertyOrder(11)]
#endif
        [SerializeField, Tooltip("Text style updater for applying colors.")]
        private TextStyleUpdater textStyleUpdater;

#if ODIN_INSPECTOR
        [TitleGroup("State Colors")]
        [PropertyOrder(20)]
#else
        [Header("State Colors")]
#endif
        [SerializeField, Tooltip("Color for the current/active instruction.")]
        private Colour_SO activeColour;

#if ODIN_INSPECTOR
        [TitleGroup("State Colors")]
        [PropertyOrder(21)]
#endif
        [SerializeField, Tooltip("Color for completed instructions.")]
        private Colour_SO completedColour;

#if ODIN_INSPECTOR
        [TitleGroup("State Colors")]
        [PropertyOrder(22)]
#endif
        [SerializeField, Tooltip("Color for upcoming/pending instructions.")]
        private Colour_SO pendingColour;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(30)]
#else
        [Header("Animation")]
#endif
        [SerializeField, Tooltip("Duration of fade animations.")]
        private float fadeDuration = 0.25f;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(31)]
#endif
        [SerializeField, Tooltip("Use unscaled time for animations.")]
        private bool useUnscaledTime = true;

        #endregion

        #region Private Fields

        private TutorialStepTextState _currentState = TutorialStepTextState.Pending;
        private CanvasGroup _canvasGroup;
        private string _counterSuffix = "";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current state of this text item.
        /// </summary>
        public TutorialStepTextState CurrentState => _currentState;

        /// <summary>
        /// Gets the primary TextMeshProUGUI component (from LocalizeStringEvent or fallback).
        /// </summary>
        public TextMeshProUGUI TextMesh
        {
            get
            {
                if (localizedText != null)
                {
                    var textMesh = localizedText.GetComponent<TextMeshProUGUI>();
                    if (textMesh != null) return textMesh;
                }
                return fallbackText;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Initialize checkmark fill to hidden
            if (checkmarkFill != null)
                checkmarkFill.enabled = false;
        }

        private void OnDestroy()
        {
            CleanupTweens();
        }

        #endregion

        #region Public Methods - Set Instruction

        /// <summary>
        /// Sets the instruction from a LocalizedString.
        /// </summary>
        /// <param name="instruction">The localized instruction to display.</param>
        public void SetInstruction(LocalizedString instruction)
        {
            _counterSuffix = "";

            if (instruction != null && !instruction.IsEmpty && localizedText != null)
            {
                localizedText.StringReference = instruction;
                localizedText.RefreshString();
                localizedText.gameObject.SetActive(true);

                if (fallbackText != null && fallbackText.gameObject != localizedText.gameObject)
                    fallbackText.gameObject.SetActive(false);
            }
            else if (fallbackText != null)
            {
                fallbackText.text = "";
                fallbackText.gameObject.SetActive(true);

                if (localizedText != null && localizedText.gameObject != fallbackText.gameObject)
                    localizedText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets the instruction from a plain string (fallback).
        /// </summary>
        /// <param name="text">The text to display.</param>
        public void SetInstruction(string text)
        {
            _counterSuffix = "";

            if (fallbackText != null)
            {
                fallbackText.text = text ?? "";
                fallbackText.gameObject.SetActive(true);
            }

            if (localizedText != null && localizedText.gameObject != fallbackText?.gameObject)
                localizedText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the instruction from a LocalizedString with a counter suffix.
        /// </summary>
        /// <param name="instruction">The localized instruction to display.</param>
        /// <param name="current">Current progress count.</param>
        /// <param name="total">Total count.</param>
        public void SetInstruction(LocalizedString instruction, int current, int total)
        {
            _counterSuffix = $" ({current}/{total})";

            if (instruction != null && !instruction.IsEmpty && localizedText != null)
            {
                // Subscribe to string changed to append counter
                localizedText.OnUpdateString.RemoveListener(AppendCounterToLocalizedString);
                localizedText.OnUpdateString.AddListener(AppendCounterToLocalizedString);
                localizedText.StringReference = instruction;
                localizedText.RefreshString();
                localizedText.gameObject.SetActive(true);

                if (fallbackText != null && fallbackText.gameObject != localizedText.gameObject)
                    fallbackText.gameObject.SetActive(false);
            }
            else if (fallbackText != null)
            {
                fallbackText.text = _counterSuffix;
                fallbackText.gameObject.SetActive(true);

                if (localizedText != null && localizedText.gameObject != fallbackText.gameObject)
                    localizedText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets the instruction from a plain string with a counter suffix.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="current">Current progress count.</param>
        /// <param name="total">Total count.</param>
        public void SetInstruction(string text, int current, int total)
        {
            _counterSuffix = $" ({current}/{total})";

            if (fallbackText != null)
            {
                fallbackText.text = (text ?? "") + _counterSuffix;
                fallbackText.gameObject.SetActive(true);
            }

            if (localizedText != null && localizedText.gameObject != fallbackText?.gameObject)
                localizedText.gameObject.SetActive(false);
        }

        #endregion

        #region Public Methods - State

        /// <summary>
        /// Updates the visual state of this text item.
        /// </summary>
        /// <param name="state">The new state to apply.</param>
        public void SetState(TutorialStepTextState state)
        {
            _currentState = state;
            ApplyStateVisuals();
        }

        #endregion

        #region Public Methods - Animation

        /// <summary>
        /// Animates the text item into view.
        /// </summary>
        public void AnimateIn()
        {
            if (_canvasGroup == null) return;

            CleanupTweens();
            _canvasGroup.alpha = 0f;
            Tween.Alpha(_canvasGroup, 1f, fadeDuration, Ease.OutQuad, useUnscaledTime: useUnscaledTime);
        }

        /// <summary>
        /// Animates the text item out of view.
        /// </summary>
        /// <param name="onComplete">Callback invoked when animation completes.</param>
        public void AnimateOut(Action onComplete = null)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            CleanupTweens();
            Tween.Alpha(_canvasGroup, 0f, fadeDuration, Ease.InQuad, useUnscaledTime: useUnscaledTime)
                .OnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region Public Methods - Clear

        /// <summary>
        /// Clears all content from this text item.
        /// </summary>
        public void Clear()
        {
            _counterSuffix = "";

            if (localizedText != null)
            {
                localizedText.OnUpdateString.RemoveListener(AppendCounterToLocalizedString);
            }

            if (fallbackText != null)
            {
                fallbackText.text = "";
            }

            if (checkmarkFill != null)
                checkmarkFill.enabled = false;
        }

        #endregion

        #region Private Methods

        private void ApplyStateVisuals()
        {
            Colour_SO targetColour = _currentState switch
            {
                TutorialStepTextState.Active => activeColour,
                TutorialStepTextState.Completed => completedColour,
                TutorialStepTextState.Pending => pendingColour,
                _ => activeColour
            };

            // Apply color via TextStyleUpdater (like UI_TaskItem)
            if (textStyleUpdater != null && targetColour != null)
            {
                textStyleUpdater.TextColourSO = targetColour;
            }

            // Show/hide and tint checkmark fill based on state
            if (checkmarkFill != null)
            {
                bool isCompleted = _currentState == TutorialStepTextState.Completed;
                checkmarkFill.enabled = isCompleted;

                // Tint checkmark fill with the completed color
                if (isCompleted && completedColour != null)
                {
                    checkmarkFill.color = completedColour.Colour;
                }
            }
        }

        private void AppendCounterToLocalizedString(string localizedValue)
        {
            var textMesh = TextMesh;
            if (textMesh != null && !string.IsNullOrEmpty(_counterSuffix))
            {
                textMesh.text = localizedValue + _counterSuffix;
            }
        }

        private void CleanupTweens()
        {
            if (_canvasGroup != null)
                Tween.StopAll(_canvasGroup);
        }

        #endregion
    }
}
