using System;
using HelloDev.Input;
using HelloDev.Logging;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Tutorials;
using HelloDev.UI.Default;
using HelloDev.Utils;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;
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
        [TitleGroup("References")]
        [PropertyOrder(0)]
#else
        [Header("References")]
#endif
        [SerializeField, Tooltip("Primary localized text display.")]
        private LocalizeStringEvent localizedText;

#if ODIN_INSPECTOR
        [TitleGroup("References")]
        [PropertyOrder(1)]
#else
#endif
        [SerializeField, Tooltip("Layout group for managing text item spacing.")]
        private LayoutGroup _layoutGroup;
#if ODIN_INSPECTOR
        [TitleGroup("References")]
        [PropertyOrder(2)]
#else
#endif
        [SerializeField, Tooltip("CanvasGroup for alpha and visibility control.")]
        private CanvasGroup _canvasGroup;


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
        private float animationDuration = .35f;

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
        private LocalizedString _cachedInstruction;
        private DeviceTrackingHelper _deviceTracker;
        private UnityAction Completed;
        private TutorialStepRuntime _step;

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
                    TextMeshProUGUI textMesh = localizedText.GetComponent<TextMeshProUGUI>();
                    if (textMesh != null) return textMesh;
                }

                return null;
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
            }
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

                OnLocalizedStringUpdated(localizedText.StringReference.GetLocalizedString());
            }
        }

        public void SetStep(TutorialStepRuntime step)
        {
            _step = step;
        }

        #endregion

        #region Public Methods - State

        /// <summary>
        /// Updates the visual state of this text item.
        /// </summary>
        /// <param name="state">The new state to apply.</param>
        public void SetState(TutorialStepTextState state)
        {
            if (_currentState == state) return;
            _currentState = state;
            ApplyStateVisuals();
        }

        #endregion

        #region Public Methods - Animation

        /// <summary>
        /// Animates the text item into view.
        /// </summary>
        public Sequence AnimateIn()
        {
            _canvasGroup.alpha = 0f;
            int startValue = -175;
            _layoutGroup.padding.left = startValue;
            Sequence animateIn = Sequence.Create();
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            animateIn.Group(Tween.Custom(startValue, 0, duration: animationDuration, ease: Ease.OutBack, onValueChange: (tempValue) =>
            {
                _layoutGroup.padding.left = (int)tempValue;
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }));
            animateIn.Group(Tween.Alpha(_canvasGroup, startValue: 0f, endValue: 1f, duration: animationDuration * 1.1f, ease: Ease.InCubic));
            animateIn.OnComplete(() => { LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform); }, gameObject);
            return animateIn;
        }

        #endregion

        #region Public Methods - Clear

        /// <summary>
        /// Clears all content from this text item.
        /// </summary>
        public void Clear()
        {
            _cachedInstruction = null;
            if (localizedText != null)
            {
                localizedText.OnUpdateString.SafeUnsubscribe(OnLocalizedStringUpdated);
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
            TextMeshProUGUI textMesh = TextMesh;
            if (textMesh != null)
            {
                string processedText = ProcessInputSprites(localizedValue);
                textMesh.text = processedText;
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
            bool isCompleted = _currentState == TutorialStepTextState.Completed;
            if (checkmarkFill != null)
            {
                checkmarkFill.enabled = isCompleted;
            }
            // Tint checkmark fill with the completed color
            if (isCompleted)
            {
                if (completedColour != null)
                {
                    checkmarkFill.color = completedColour.Colour;
                }

                float duration = 0.1f;

                Logger.LogVerbose(LogSystems.UI, $"Starting animation for {name}", this);
                // Always run a small pop animation and invoke the Completed callback afterwards
                Sequence sequence = Sequence.Create();
                sequence.Chain(Tween.Scale(TextMesh.transform, Vector3.one * 1.05f, duration, Ease.OutQuad));
                sequence.Chain(Tween.Scale(TextMesh.transform, Vector3.one, duration, Ease.InQuad));
                sequence.OnComplete(() =>
                {
                    if (_step != null && _step.HasSubsteps)
                    {
                        if (_step.CurrentState != ObjectiveState.Completed) return;
                    }

                    Completed?.Invoke();
                });
            }
        }

        public void SetOnCompleteStepAction(UnityAction onCompleted)
        {
            Completed = onCompleted;
        }

        public void RunCompleteStepAnimation(TutorialStepRuntime step)
        {
            _currentState = TutorialStepTextState.Completed;
            ApplyStateVisuals();
        }
    }

    #endregion
}