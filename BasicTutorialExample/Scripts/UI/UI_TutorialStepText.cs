using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HelloDev.Input;
using HelloDev.UI.Default;
using HelloDev.Utils;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
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

#if ODIN_INSPECTOR
        [TitleGroup("Input Icons")]
        [PropertyOrder(40)]
#else
        [Header("Input Icons")]
#endif
        [SerializeField, Tooltip("Icon provider for resolving device-specific input sprites.")]
        private InputIconProvider_SO iconProvider;

#if ODIN_INSPECTOR
        [TitleGroup("Input Icons")]
        [PropertyOrder(41)]
#endif
        [SerializeField, Tooltip("Default device layout to use (e.g., 'XInputController', 'DualShockGamepad'). Leave empty to auto-detect.")]
        private string defaultDeviceLayout = "XInputController";

        #endregion

        #region Static Fields

        // Static list optimization (Unity's pattern) - single subscription for all instances
        private static List<UI_TutorialStepText> s_Instances;

        #endregion

        #region Private Fields

        private TutorialStepTextState _currentState = TutorialStepTextState.Pending;
        private CanvasGroup _canvasGroup;
        private string _currentDeviceLayout;
        private LocalizedString _cachedInstruction;

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

            // Detect current device layout
            DetectDeviceLayout();
        }

        private void OnEnable()
        {
            // Static list optimization - single subscription shared by all instances
            if (s_Instances == null)
                s_Instances = new List<UI_TutorialStepText>();

            s_Instances.Add(this);

            // Only subscribe once when first instance is enabled
            if (s_Instances.Count == 1)
                InputSystem.onActionChange += OnActionChange;
        }

        private void OnDisable()
        {
            if (s_Instances != null)
            {
                s_Instances.Remove(this);
                // Unsubscribe when last instance is disabled
                if (s_Instances.Count == 0)
                {
                    s_Instances = null;
                    InputSystem.onActionChange -= OnActionChange;
                }
            }
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
            _cachedInstruction = instruction;

            if (instruction != null && !instruction.IsEmpty && localizedText != null)
            {
                localizedText.StringReference = instruction;
                localizedText.OnUpdateString.SafeUnsubscribe(OnLocalizedStringUpdated);
                localizedText.OnUpdateString.AddListener(OnLocalizedStringUpdated);
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
                fallbackText.text = text ?? "";
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
                localizedText.OnUpdateString.AddListener(OnLocalizedStringUpdated);
                localizedText.RefreshString();
                localizedText.gameObject.SetActive(true);

                if (fallbackText != null && fallbackText.gameObject != localizedText.gameObject)
                    fallbackText.gameObject.SetActive(false);
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

        /// <summary>
        /// Manually sets the device layout to use for sprite resolution.
        /// </summary>
        /// <param name="deviceLayout">The device layout name (e.g., "XInputController", "DualShockGamepad").</param>
        public void SetDeviceLayout(string deviceLayout)
        {
            if (_currentDeviceLayout != deviceLayout)
            {
                _currentDeviceLayout = deviceLayout;
                RefreshForDeviceChange();
            }
        }

        /// <summary>
        /// Gets the current device layout being used.
        /// </summary>
        public string GetCurrentDeviceLayout() => _currentDeviceLayout;

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

            if (fallbackText != null)
            {
                fallbackText.text = "";
            }

            if (checkmarkFill != null)
                checkmarkFill.enabled = false;
        }

        #endregion

        #region Private Methods - Device Detection & Sprite Processing

        /// <summary>
        /// Detects the current device layout from the Input System.
        /// </summary>
        private void DetectDeviceLayout()
        {
            // Try to get the last used device
            var lastDevice = InputSystem.devices.Count > 0 ? InputSystem.devices[InputSystem.devices.Count - 1] : null;
            
            if (lastDevice != null && lastDevice is Gamepad)
            {
                _currentDeviceLayout = lastDevice.layout;
            }
            else
            {
                _currentDeviceLayout = defaultDeviceLayout;
            }
        }

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
                textMesh.text = ProcessInputSprites(localizedValue);
            }
        }

        /// <summary>
        /// Processes input sprite tags and replaces them with device-specific sprite names.
        /// Pattern: &lt;sprite name=keyName&gt; -> &lt;sprite name=deviceSpecificKey&gt;
        /// </summary>
        /// <param name="text">The text containing sprite tags.</param>
        /// <returns>Text with device-specific sprite names.</returns>
        private string ProcessInputSprites(string text)
        {
            if (string.IsNullOrEmpty(text) || iconProvider == null)
                return text;

            // Regex pattern to match <sprite name=keyName>
            var pattern = @"<sprite\s+name=([^>]+)>";
            
            return Regex.Replace(text, pattern, match =>
            {
                var controlPath = match.Groups[1].Value.Trim();
                
                // Get the device-specific icon from the icon provider
                var iconMap = iconProvider.GetIconMapForLayout(_currentDeviceLayout);
                if (iconMap != null)
                {
                    var (icon, mappedText) = iconMap.GetBinding(controlPath);
                    
                    // If we have an icon sprite, use its name for the TMP sprite tag
                    if (icon != null)
                    {
                        return $"<sprite name=\"{icon.name}\">";
                    }
                }
                
                // Fallback: keep original sprite tag
                return match.Value;
            });
        }

        /// <summary>
        /// Refreshes the display when device layout changes.
        /// </summary>
        private void RefreshForDeviceChange()
        {
            if (_cachedInstruction != null && localizedText != null)
            {
                localizedText.RefreshString();
            }
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Called when Input System action changes occur.
        /// </summary>
        private void OnInputActionChange(object obj, InputActionChange change)
        {
            // Detect device changes and refresh display
            if (change == InputActionChange.BoundControlsChanged)
            {
                var oldLayout = _currentDeviceLayout;
                DetectDeviceLayout();
                
                if (oldLayout != _currentDeviceLayout)
                {
                    RefreshForDeviceChange();
                }
            }
        }
#endif

        #endregion

        #region Static Event Handler

        // Static handler - updates all instances when device changes (Unity's pattern)
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            if (s_Instances == null || s_Instances.Count == 0)
                return;

            // Update all instances when device bindings change
            for (var i = 0; i < s_Instances.Count; ++i)
            {
                var instance = s_Instances[i];
                if (instance == null) continue;

                var oldLayout = instance._currentDeviceLayout;
                instance.DetectDeviceLayout();

                if (oldLayout != instance._currentDeviceLayout)
                {
                    instance.RefreshForDeviceChange();
                }
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

        private void CleanupTweens()
        {
            if (_canvasGroup != null)
                Tween.StopAll(_canvasGroup);
        }

        #endregion
    }
}