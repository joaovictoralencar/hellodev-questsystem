using HelloDev.QuestSystem.Tutorials;
using HelloDev.QuestSystem.Utils;
using HelloDev.UI.Default;
using HelloDev.Utils;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicTutorialExample.UI
{
    /// <summary>
    /// UI controller for displaying tutorial instructions and handling user interaction.
    /// Listens to TutorialManager events and updates the UI accordingly.
    /// Requires a UIContainer component for panel show/hide animations.
    /// </summary>
    public class UI_TutorialController : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Tutorial Panel")]
        [PropertyOrder(0)]
        [Required("UIContainer is required for panel animations.")]
#else
        [Header("Tutorial Panel")]
#endif
        [SerializeField] private UIContainer panelContainer;

#if ODIN_INSPECTOR
        [TitleGroup("Tutorial Panel")]
        [PropertyOrder(1)]
        [Tooltip("RectTransform to apply scale animation to. If null, scale animation is skipped.")]
#endif
        [SerializeField] private RectTransform panelContentRoot;

#if ODIN_INSPECTOR
        [TitleGroup("Content")]
        [PropertyOrder(10)]
#else
        [Header("Content")]
#endif
        [SerializeField] private LocalizeStringEvent instructionText;

#if ODIN_INSPECTOR
        [TitleGroup("Content")]
        [PropertyOrder(11)]
#endif
        [SerializeField] private TextMeshProUGUI instructionTextFallback;

#if ODIN_INSPECTOR
        [TitleGroup("Content")]
        [PropertyOrder(12)]
#endif
        [SerializeField] private Image stepIcon;

#if ODIN_INSPECTOR
        [TitleGroup("Content")]
        [PropertyOrder(13)]
#endif
        [SerializeField] private TextMeshProUGUI stepCounterText;

#if ODIN_INSPECTOR
        [TitleGroup("Progress")]
        [PropertyOrder(20)]
#else
        [Header("Progress")]
#endif
        [SerializeField] private Slider progressBar;

#if ODIN_INSPECTOR
        [TitleGroup("Progress")]
        [PropertyOrder(21)]
#endif
        [SerializeField] private TextMeshProUGUI progressText;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(30)]
#else
        [Header("Buttons")]
#endif
        [SerializeField] private UIButton skipButton;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(31)]
#endif
        [SerializeField] private UIButton continueButton;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(32)]
#endif
        [SerializeField] private float buttonHoverScale = 1.05f;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(33)]
#endif
        [SerializeField] private float buttonPressScale = 0.95f;

#if ODIN_INSPECTOR
        [TitleGroup("Settings")]
        [PropertyOrder(40)]
#else
        [Header("Settings")]
#endif
        [SerializeField] private bool hideOnComplete = true;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(50)]
#else
        [Header("Animation")]
#endif
        [SerializeField] private float textFadeDuration = 0.25f;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(51)]
#endif
        [SerializeField] private bool useUnscaledTime = true;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(52)]
#endif
        [SerializeField] private bool useScaleAnimation = true;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(53)]
        [ShowIf("useScaleAnimation")]
#endif
        [SerializeField] private float scaleAnimationDuration = 0.25f;

        #endregion

        #region Private Fields

        private TutorialRuntime _currentTutorial;
        private bool _isInitialized;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the currently active tutorial.
        /// </summary>
        public TutorialRuntime CurrentTutorial => _currentTutorial;

        /// <summary>
        /// Gets whether the tutorial panel is visible.
        /// </summary>
        public bool IsPanelVisible => panelContainer != null && panelContainer.IsVisible();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            Initialize();
        }

        private void Start()
        {
            HidePanelInstant();
            SubscribeToManagerEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManagerEvents();
            CleanupTweens();
        }

        #endregion

        #region Initialization

        private void ValidateReferences()
        {
            if (panelContainer == null)
            {
                Debug.LogError($"[UI_TutorialController] UIContainer reference is required on '{gameObject.name}'. Panel animations will not work.", this);
            }
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            if (skipButton != null)
            {
                skipButton.OnClick.AddListener(HandleSkipClicked);
                SetupButtonAnimations(skipButton);
            }

            if (continueButton != null)
            {
                continueButton.OnClick.AddListener(HandleContinueClicked);
                SetupButtonAnimations(continueButton);
            }

            _isInitialized = true;
        }

        private void SetupButtonAnimations(UIButton button)
        {
            if (button == null) return;

            var buttonTransform = button.transform;
            var originalScale = buttonTransform.localScale;

            button.HighlightedStateEvent.AddListener(() =>
            {
                AnimateButtonScale(buttonTransform, originalScale * buttonHoverScale, 0.15f, Ease.OutQuad);
            });

            button.SelectedStateEvent.AddListener(() =>
            {
                AnimateButtonScale(buttonTransform, originalScale * buttonHoverScale, 0.15f, Ease.OutQuad);
            });

            button.PressedStateEvent.AddListener(() =>
            {
                AnimateButtonScale(buttonTransform, originalScale * buttonPressScale, 0.08f, Ease.InQuad);
            });

            button.NormalStateEvent.AddListener(() =>
            {
                AnimateButtonScale(buttonTransform, originalScale, 0.12f, Ease.OutBack);
            });
        }

        private void AnimateButtonScale(Transform buttonTransform, Vector3 targetScale, float duration, Ease ease)
        {
            // Skip if already at target scale (prevents PrimeTween warning)
            if (Vector3.Distance(buttonTransform.localScale, targetScale) < 0.001f)
                return;

            Tween.StopAll(buttonTransform);
            Tween.Scale(buttonTransform, targetScale, duration, ease, useUnscaledTime: useUnscaledTime);
        }

        private void SubscribeToManagerEvents()
        {
            if (TutorialManager.Instance == null) return;

            TutorialManager.Instance.OnTutorialStarted.SafeSubscribe(HandleTutorialStarted);
            TutorialManager.Instance.OnTutorialCompleted.SafeSubscribe(HandleTutorialCompleted);
            TutorialManager.Instance.OnStepStarted.SafeSubscribe(HandleStepStarted);
            TutorialManager.Instance.OnStepCompleted.SafeSubscribe(HandleStepCompleted);
        }

        private void UnsubscribeFromManagerEvents()
        {
            if (TutorialManager.Instance == null) return;

            TutorialManager.Instance.OnTutorialStarted.SafeUnsubscribe(HandleTutorialStarted);
            TutorialManager.Instance.OnTutorialCompleted.SafeUnsubscribe(HandleTutorialCompleted);
            TutorialManager.Instance.OnStepStarted.SafeUnsubscribe(HandleStepStarted);
            TutorialManager.Instance.OnStepCompleted.SafeUnsubscribe(HandleStepCompleted);
        }

        private void CleanupTweens()
        {
            if (instructionTextFallback != null)
                Tween.StopAll(instructionTextFallback);
            if (stepCounterText != null)
                Tween.StopAll(stepCounterText);
            if (panelContentRoot != null)
                Tween.StopAll(panelContentRoot);
            if (skipButton != null)
                Tween.StopAll(skipButton.transform);
            if (continueButton != null)
                Tween.StopAll(continueButton.transform);
        }

        #endregion

        #region Event Handlers

        private void HandleTutorialStarted(TutorialRuntime tutorial)
        {
            _currentTutorial = tutorial;

            QuestLogger.Log(LogSubsystem.Tutorial, $"[UI_TutorialController] Tutorial started: {tutorial.DevName}");

            SetButtonVisible(skipButton, tutorial.Data.CanSkip);
            UpdateProgress();
            ShowPanel();
        }

        private void HandleTutorialCompleted(TutorialRuntime tutorial)
        {
            QuestLogger.Log(LogSubsystem.Tutorial, $"[UI_TutorialController] Tutorial completed: {tutorial.DevName}");

            if (hideOnComplete)
                HidePanel();

            _currentTutorial = null;
        }

        private void HandleStepStarted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            QuestLogger.Log(LogSubsystem.Tutorial, $"[UI_TutorialController] Step started: {step.DevName}");

            UpdateStepDisplay(step);
            UpdateProgress();
            UpdateButtonStates(step);
        }

        private void HandleStepCompleted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            QuestLogger.Log(LogSubsystem.Tutorial, $"[UI_TutorialController] Step completed: {step.DevName}");

            UpdateProgress();
        }

        #endregion

        #region Private Methods - UI Updates

        private void UpdateStepDisplay(TutorialStepRuntime step)
        {
            if (step == null) return;

            if (instructionText != null && step.Data.Instruction != null && !step.Data.Instruction.IsEmpty)
            {
                instructionText.StringReference = step.Data.Instruction;
                instructionText.RefreshString();

                if (instructionTextFallback != null)
                    instructionTextFallback.gameObject.SetActive(false);

                var localizedTextMesh = instructionText.GetComponent<TextMeshProUGUI>();
                if (localizedTextMesh != null)
                    AnimateTextFadeIn(localizedTextMesh);
            }
            else if (instructionTextFallback != null)
            {
                instructionTextFallback.text = step.DevName;
                instructionTextFallback.gameObject.SetActive(true);
                AnimateTextFadeIn(instructionTextFallback);
            }

            if (stepIcon != null)
            {
                bool hasIcon = step.Data.StepIcon != null;
                stepIcon.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    stepIcon.sprite = step.Data.StepIcon;
                    Tween.Alpha(stepIcon, 0f, 1f, textFadeDuration, Ease.OutQuad, useUnscaledTime: useUnscaledTime);
                }
            }

            if (stepCounterText != null && _currentTutorial != null)
            {
                int current = step.StepIndex + 1;
                int total = _currentTutorial.Steps.Count;
                stepCounterText.text = $"{current}/{total}";
                AnimateTextFadeIn(stepCounterText);
            }
        }

        private void AnimateTextFadeIn(TextMeshProUGUI textMesh)
        {
            if (textMesh == null) return;

            Tween.StopAll(textMesh);
            Tween.Alpha(textMesh, 0f, 1f, textFadeDuration, Ease.OutQuad, useUnscaledTime: useUnscaledTime);
        }

        private void UpdateProgress()
        {
            if (_currentTutorial == null) return;

            float progress = _currentTutorial.Progress;

            if (progressBar != null)
                progressBar.value = progress;

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        private void UpdateButtonStates(TutorialStepRuntime step)
        {
            if (step == null) return;

            bool showContinue = !step.Data.IsTimedStep && step.Data.CompletionCondition == null;
            SetButtonVisible(continueButton, showContinue);

            if (skipButton != null)
                skipButton.SetInteractable(step.Data.CanSkip);
        }

        private void SetButtonVisible(UIButton button, bool visible)
        {
            if (button == null) return;

            if (visible && !button.gameObject.activeSelf)
            {
                button.gameObject.SetActive(true);
                var buttonTransform = button.transform;
                buttonTransform.localScale = Vector3.zero;
                Tween.Scale(buttonTransform, Vector3.one, 0.2f, Ease.OutBack, useUnscaledTime: useUnscaledTime);
            }
            else if (!visible && button.gameObject.activeSelf)
            {
                var buttonTransform = button.transform;
                Tween.Scale(buttonTransform, Vector3.zero, 0.15f, Ease.InBack, useUnscaledTime: useUnscaledTime)
                    .OnComplete(() => button.gameObject.SetActive(false));
            }
        }

        #endregion

        #region Private Methods - Button Handlers

        private void HandleSkipClicked()
        {
            QuestLogger.Log(LogSubsystem.Tutorial, "[UI_TutorialController] Skip button clicked");
            TutorialManager.Instance?.SkipCurrentTutorial();
        }

        private void HandleContinueClicked()
        {
            QuestLogger.Log(LogSubsystem.Tutorial, "[UI_TutorialController] Continue button clicked");
            TutorialManager.Instance?.CompleteCurrentStep();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the tutorial panel with scale animation.
        /// Uses InstaShow to avoid TweenService dependency, then applies PrimeTween scale animation.
        /// </summary>
        public void ShowPanel()
        {
            if (panelContainer == null)
            {
                Debug.LogError("[UI_TutorialController] Cannot show panel - UIContainer is not assigned.", this);
                return;
            }

            panelContainer.InstaShow();
            AnimatePanelScaleIn();
        }

        /// <summary>
        /// Hides the tutorial panel with scale animation.
        /// Uses InstaHide after scale animation completes to avoid TweenService dependency.
        /// </summary>
        public void HidePanel()
        {
            if (panelContainer == null)
            {
                Debug.LogError("[UI_TutorialController] Cannot hide panel - UIContainer is not assigned.", this);
                return;
            }

            AnimatePanelScaleOut(() => panelContainer.InstaHide());
        }

        /// <summary>
        /// Instantly hides the panel without animation.
        /// </summary>
        public void HidePanelInstant()
        {
            if (panelContentRoot != null)
            {
                Tween.StopAll(panelContentRoot);
                panelContentRoot.localScale = Vector3.one;
            }

            if (panelContainer != null)
                panelContainer.InstaHide();
        }

        #endregion

        #region Private Methods - Panel Animation

        private void AnimatePanelScaleIn()
        {
            if (!useScaleAnimation || panelContentRoot == null) return;

            Tween.StopAll(panelContentRoot);
            panelContentRoot.localScale = Vector3.one * 0.9f;
            Tween.Scale(panelContentRoot, 1f, scaleAnimationDuration, Ease.OutBack, useUnscaledTime: useUnscaledTime);
        }

        private void AnimatePanelScaleOut(System.Action onComplete = null)
        {
            if (!useScaleAnimation || panelContentRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            Tween.StopAll(panelContentRoot);
            Tween.Scale(panelContentRoot, 0.9f, scaleAnimationDuration * 0.5f, Ease.InBack, useUnscaledTime: useUnscaledTime)
                .OnComplete(() => onComplete?.Invoke());
        }

        #endregion
    }
}
