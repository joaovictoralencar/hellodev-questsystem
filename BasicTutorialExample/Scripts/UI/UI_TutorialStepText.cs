using System;
using HelloDev.Input;
using HelloDev.UI.Default;
using HelloDev.Utils;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
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
    /// Supports localization, state-based coloring via Colour_SO, checkmark for completed state,
    /// and automatic device-specific input sprite replacement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses <see cref="InputSpriteUtility"/> to replace generic sprite tags (e.g., &lt;sprite name=buttonSouth&gt;)
    /// with device-specific sprite names based on the current input device.
    /// </para>
    /// <para>
    /// Automatically updates when the player switches between keyboard/mouse and gamepad.
    /// </para>
    /// </remarks>
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
#else
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
#else
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
#else
#endif
        [SerializeField, Tooltip("Color for completed instructions.")]
        private Colour_SO completedColour;

#if ODIN_INSPECTOR
        [TitleGroup("State Colors")]
        [PropertyOrder(22)]
#else
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
#else
#endif
        [SerializeField, Tooltip("Use unscaled time for animations.")]
        private bool useUnscaledTime = true;

#if ODIN_INSPECTOR
        [TitleGroup("Input Icons")]
        [PropertyOrder(40)]
#else
        [Header("Input Icons")]
#endif
        [SerializeField, Tooltip("Icon provider for resolving device-specific input sprites.")]
        private InputIconProvider_SO iconProvider;

        #endregion

        #region Private Fields

        private TutorialStepTextState _currentState = TutorialStepTextState.Pending;
        private CanvasGroup _canvasGroup;
        private LocalizedString _cachedInstruction;
        private DeviceTrackingHelper _deviceTracker;
        private string _lastProcessedText;

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

        private void OnEnable()
        {
            // Subscribe to device tracker for real-time device switching
            if (_deviceTracker == null)
                _deviceTracker = new DeviceTrackingHelper(this, OnDeviceChanged);

            _deviceTracker.Subscribe();
        }

        private void OnDisable()
        {
            // Unsubscribe from device tracker
            _deviceTracker?.Unsubscribe();
        }

        private void OnDestroy()
        {
            CleanupTweens();
        }

        #endregion

        #region Device Tracking

        /// <summary>
        /// Called when the active input device changes.
        /// Refreshes sprite replacements for the new device.
        /// </summary>
        private void OnDeviceChanged(InputDevice previousDevice, InputDevice newDevice)
        {
            RefreshForDeviceChange();
        }

        #endregion

        #region Public Methods - Set Instruction

        /// <summary>
        /// Sets the instruction from a LocalizedString.
        /// </summary>
        /// <param name="instruction">The localized instruction to display.</param>
        public void SetInstruction(LocalizedString instruction)
        {
            _cachedInstruction = instruction;

            if (instruction != null && !instruction.IsEmpty && localizedText != null)
            {
                localizedText.StringReference = instruction;
                localizedText.OnUpdateString.SafeSubscribe(OnLocalizedStringUpdated);
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
            if (fallbackText != null)
            {
                var processedText = ProcessInputSprites(text);
                fallbackText.text = processedText ?? "";
                fallbackText.gameObject.SetActive(true);
            }

            if (localizedText != null && localizedText.gameObject != fallbackText?.gameObject)
                localizedText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the instruction from a LocalizedString with counter variables.
        /// </summary>
        /// <param name="instruction">The localized instruction to display.</param>
        /// <param name="current">Current progress count.</param>
        /// <param name="total">Total count.</param>
        public void SetInstruction(LocalizedString instruction, int current, int total)
        {
            _cachedInstruction = instruction;

            if (instruction != null && !instruction.IsEmpty && localizedText != null)
            {
                SetupLocalizedVariables(instruction, current, total);
                localizedText.StringReference = instruction;
                localizedText.OnUpdateString.SafeUnsubscribe(OnLocalizedStringUpdated);
                localizedText.RefreshString();
                localizedText.gameObject.SetActive(true);

                if (fallbackText != null && fallbackText.gameObject != localizedText.gameObject)
                    fallbackText.gameObject.SetActive(false);
                OnLocalizedStringUpdated(localizedText.StringReference.GetLocalizedString());
            }
            else if (fallbackText != null)
            {
                fallbackText.text = $"({current}/{total})";
                fallbackText.gameObject.SetActive(true);

                if (localizedText != null && localizedText.gameObject != fallbackText.gameObject)
                    localizedText.gameObject.SetActive(false);
            }
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

        #region Public Methods - Configuration

        /// <summary>
        /// Sets the icon provider at runtime.
        /// </summary>
        /// <param name="provider">The icon provider to use.</param>
        public void SetIconProvider(InputIconProvider_SO provider)
        {
            iconProvider = provider;
            RefreshForDeviceChange();
        }

        #endregion

        #region Public Methods - Clear

        /// <summary>
        /// Clears all content from this text item.
        /// </summary>
        public void Clear()
        {
            _cachedInstruction = null;
            _lastProcessedText = null;

            if (localizedText != null)
            {
                localizedText.OnUpdateString.SafeUnsubscribe(OnLocalizedStringUpdated);
            }

            if (fallbackText != null)
            {
                fallbackText.text = "";
            }

            if (checkmarkFill != null)
                checkmarkFill.enabled = false;
        }

        #endregion

        #region Private Methods - Sprite Processing

        /// <summary>
        /// Called when the localized string is updated.
        /// Processes sprite replacements based on current device.
        /// </summary>
        /// <param name="localizedValue">The localized string value.</param>
        private void OnLocalizedStringUpdated(string localizedValue)
        {
            var textMesh = TextMesh;
            if (textMesh != null)
            {
                var processedText = ProcessInputSprites(localizedValue);
                textMesh.text = processedText;
                _lastProcessedText = processedText;
            }
        }

        /// <summary>
        /// Processes input sprite tags using InputSpriteUtility.
        /// </summary>
        /// <param name="text">The text containing sprite tags.</param>
        /// <returns>Text with device-specific sprite names.</returns>
        private string ProcessInputSprites(string text)
        {
            if (string.IsNullOrEmpty(text) || iconProvider == null)
                return text;

            // Use utility to process sprites based on current device
            return InputSpriteUtility.ProcessInputSprites(text, iconProvider);
        }

        /// <summary>
        /// Refreshes the display when device layout changes.
        /// </summary>
        private void RefreshForDeviceChange()
        {
            if (_cachedInstruction != null && localizedText != null)
            {
                localizedText.RefreshString();
                OnLocalizedStringUpdated(localizedText.StringReference.GetLocalizedString());
            }
            else if (!string.IsNullOrEmpty(_lastProcessedText) && fallbackText != null)
            {
                // Reprocess last text with new device
                var reprocessed = ProcessInputSprites(_lastProcessedText);
                fallbackText.text = reprocessed;
            }
        }

        #endregion

        #region Private Methods - Localization

        /// <summary>
        /// Sets up localization variables for counter display (current/total).
        /// </summary>
        /// <param name="localizedString">The LocalizedString to configure.</param>
        /// <param name="current">Current progress count.</param>
        /// <param name="total">Total count.</param>
        private void SetupLocalizedVariables(LocalizedString localizedString, int current, int total)
        {
            if (localizedString == null) return;

            // Ensure "current" variable exists
            if (!localizedString.TryGetValue("current", out IVariable currentVariable))
            {
                localizedString.Add("current", new IntVariable { Value = current });
            }
            else if (currentVariable is IntVariable existingCurrent)
            {
                existingCurrent.Value = current;
            }

            // Ensure "required" variable exists
            if (!localizedString.TryGetValue("required", out IVariable requiredVariable))
            {
                localizedString.Add("required", new IntVariable { Value = total });
            }
            else if (requiredVariable is IntVariable existingRequired)
            {
                existingRequired.Value = total;
            }
        }

        private void ApplyStateVisuals()
        {
            Colour_SO targetColour = _currentState switch
            {
                TutorialStepTextState.Active => activeColour,
                TutorialStepTextState.Completed => completedColour,
                TutorialStepTextState.Pending => pendingColour,
                _ => activeColour
            };

            // Apply color via TextStyleUpdater
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

        private void CleanupTweens()
        {
            if (_canvasGroup != null)
                Tween.StopAll(_canvasGroup);
        }

        #endregion
    }
}