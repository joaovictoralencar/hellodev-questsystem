using HelloDev.Input;
using HelloDev.Logging;
using HelloDev.QuestSystem.Tutorials;
using HelloDev.QuestSystem.Utils;
using HelloDev.UI.Default;
using HelloDev.Utils;
using PrimeTween;
using UnityEngine;
using Logger = HelloDev.Logging.Logger;

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
        [SerializeField]
        private UIContainer panelContainer;

#if ODIN_INSPECTOR
        [TitleGroup("Tutorial Panel")]
        [PropertyOrder(1)]
        [Tooltip("RectTransform to apply scale animation to. If null, scale animation is skipped.")]
#endif
        [SerializeField]
        private RectTransform panelContentRoot;

#if ODIN_INSPECTOR
        [TitleGroup("Step Display")]
        [PropertyOrder(10)]
        [Required("Step display component is required.")]
#else
        [Header("Step Display")]
#endif
        [SerializeField]
        private UI_TutorialStep stepDisplay;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(30)]
#else
        [Header("Buttons")]
#endif
        [SerializeField]
        private UIButton completeButton;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(31)]
#endif
        [SerializeField]
        private UIButton skipButton;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(32)]
#endif
        [SerializeField]
        private float buttonHoverScale = 1.05f;

#if ODIN_INSPECTOR
        [TitleGroup("Buttons")]
        [PropertyOrder(33)]
#endif
        [SerializeField]
        private float buttonPressScale = 0.95f;

#if ODIN_INSPECTOR
        [TitleGroup("Settings")]
        [PropertyOrder(40)]
#else
        [Header("Settings")]
#endif
        [SerializeField]
        private bool hideOnComplete = true;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(50)]
#else
        [Header("Animation")]
#endif
        [SerializeField]
        private bool useUnscaledTime = true;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(51)]
#endif
        [SerializeField]
        private bool useScaleAnimation = true;

#if ODIN_INSPECTOR
        [TitleGroup("Animation")]
        [PropertyOrder(52)]
        [ShowIf("useScaleAnimation")]
#endif
        [SerializeField]
        private float scaleAnimationDuration = 0.25f;

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
                Logger.LogError(LogSystems.Tutorial,$"UIContainer reference is required on '{gameObject.name}'. Panel animations will not work.", this);
            }

            if (stepDisplay == null)
            {
                Logger.LogError(LogSystems.Tutorial,$"UI_TutorialStep reference is required on '{gameObject.name}'. Step display will not work.", this);
            }
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            if (completeButton != null)
            {
                completeButton.OnClick.SafeSubscribe(HandleSkipClicked);
                SetupButtonAnimations(completeButton);
            }

            if (skipButton != null)
            {
                skipButton.OnClick.SafeSubscribe(HandleContinueClicked);
                SetupButtonAnimations(skipButton);
            }

            _isInitialized = true;
        }

        private void SetupButtonAnimations(UIButton button)
        {
            if (button == null) return;

            var buttonTransform = button.transform;
            var originalScale = buttonTransform.localScale;

            button.HighlightedStateEvent.SafeSubscribe(() => { AnimateButtonScale(buttonTransform, originalScale * buttonHoverScale, 0.15f, Ease.OutQuad); });

            button.SelectedStateEvent.SafeSubscribe(() => { AnimateButtonScale(buttonTransform, originalScale * buttonHoverScale, 0.15f, Ease.OutQuad); });

            button.PressedStateEvent.SafeSubscribe(() => { AnimateButtonScale(buttonTransform, originalScale * buttonPressScale, 0.08f, Ease.InQuad); });

            button.NormalStateEvent.SafeSubscribe(() => { AnimateButtonScale(buttonTransform, originalScale, 0.12f, Ease.OutBack); });
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

            // Handle case where tutorial was restored from save before UI subscribed
            var currentTutorial = TutorialManager.Instance.CurrentTutorial;
            if (currentTutorial != null && currentTutorial.CurrentState == HelloDev.Objectives.ObjectiveState.InProgress)
            {
                Logging.Logger.Log(LogSystems.Tutorial, "Found active tutorial on subscribe, showing UI");
                HandleTutorialStarted(currentTutorial);
                if (currentTutorial.CurrentStep != null)
                {
                    HandleStepStarted(currentTutorial, currentTutorial.CurrentStep);
                }
            }
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
            if (panelContentRoot != null)
                Tween.StopAll(panelContentRoot);
            if (completeButton != null)
                Tween.StopAll(completeButton.transform);
            if (skipButton != null)
                Tween.StopAll(skipButton.transform);
        }

        #endregion

        #region Event Handlers

        private void HandleTutorialStarted(TutorialRuntime tutorial)
        {
            _currentTutorial = tutorial;

            Logger.Log(LogSystems.Tutorial, $"Tutorial started: {tutorial.DevName}", this);

            SetButtonVisible(completeButton, tutorial.Data.CanSkip);
            UpdateProgress();
            ShowPanel();
        }

        private void HandleTutorialCompleted(TutorialRuntime tutorial)
        {
            Logger.Log(LogSystems.Tutorial, $"Tutorial completed: {tutorial.DevName}", this);

            if (hideOnComplete)
                HidePanel();

            _currentTutorial = null;
        }

        private void HandleStepStarted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            Logger.Log(LogSystems.Tutorial, $"Step started: {step.DevName}", this);

            // Subscribe to substep/count events for this step
            step.OnSubstepCompleted.SafeSubscribe(HandleSubstepCompleted);
            step.OnCountProgressChanged.SafeSubscribe(HandleCountProgressChanged);

            // Update step display
            if (stepDisplay != null)
            {
                stepDisplay.DisplayStep(step);
                stepDisplay.SetStepCounter(step.StepIndex + 1, tutorial.Steps.Count);
            }

            UpdateProgress();
            UpdateButtonStates(step);
        }

        private void HandleStepCompleted(TutorialRuntime tutorial, TutorialStepRuntime step)
        {
            Logger.Log(LogSystems.Tutorial, $"Step completed: {step.DevName}", this);

            // Unsubscribe from step events
            step.OnSubstepCompleted.SafeUnsubscribe(HandleSubstepCompleted);
            step.OnCountProgressChanged.SafeUnsubscribe(HandleCountProgressChanged);

            UpdateProgress();
        }

        private void HandleSubstepCompleted(TutorialStepRuntime step, TutorialSubstep_SO substep)
        {
            Logger.Log(LogSystems.Tutorial, $"Substep completed: {substep.DevName} ({step.CompletedSubstepCount}/{step.TotalSubstepCount})", this);

            // Update display to show next substep
            if (stepDisplay != null)
            {
                stepDisplay.UpdateSubstepProgress(step);
            }

            UpdateProgress();
        }

        private void HandleCountProgressChanged(TutorialStepRuntime step, int current, int required)
        {
            Logger.Log(LogSystems.Tutorial, $"Count progress: {current}/{required}", this);

            // Update display to show count progress
            if (stepDisplay != null)
            {
                stepDisplay.UpdateCountProgress(current, required);
            }

            UpdateProgress();
        }

        #endregion

        #region Private Methods - UI Updates

        private void UpdateProgress()
        {
            if (_currentTutorial == null || stepDisplay == null) return;

            float progress = _currentTutorial.Progress;
            Logger.Log(LogSystems.Tutorial, $"Progress: {progress}", this);
            stepDisplay.SetProgress(progress);
        }

        private void UpdateButtonStates(TutorialStepRuntime step)
        {
            if (step == null) return;

            // Show Continue button for:
            // 1. Simple manual steps (no timer, no condition, no substeps, not count-based), OR
            // 2. Steps that allow skipping (CanSkip = true) - allows manual progression even with conditions
            bool isSimpleManualStep = !step.Data.IsTimedStep
                                      && step.Data.CompletionCondition == null
                                      && !step.HasSubsteps
                                      && !step.IsCountBased;
            bool showContinue = isSimpleManualStep || step.Data.CanSkip;
            SetButtonVisible(skipButton, showContinue);

            // Show Skip button based on step's CanSkip setting
            SetButtonVisible(completeButton, step.Data.CanSkip);
        }

        private void SetButtonVisible(UIButton button, bool visible)
        {
            if (button == null) return;

            var buttonTransform = button.transform;

            // Stop any running animations to prevent conflicts
            Tween.StopAll(buttonTransform);

            if (visible)
            {
                if (!button.gameObject.activeSelf)
                {
                    button.gameObject.SetActive(true);
                    buttonTransform.localScale = Vector3.zero;
                }

                // Animate to full scale (handles both newly activated and interrupted hide animations)
                if (buttonTransform.localScale != Vector3.one)
                {
                    Tween.Scale(buttonTransform, Vector3.one, 0.2f, Ease.OutBack, useUnscaledTime: useUnscaledTime);
                }
            }
            else if (button.gameObject.activeSelf)
            {
                Tween.Scale(buttonTransform, Vector3.zero, 0.15f, Ease.InBack, useUnscaledTime: useUnscaledTime)
                    .OnComplete(() => button.gameObject.SetActive(false));
            }

            OnUpdateButtonVisibility(button, visible);
        }

        private void OnUpdateButtonVisibility(UIButton button, bool visible)
        {
            if (button == completeButton)
            {
                var onActionPerformed = button.GetComponentInChildren<InputButtonWithPrompt>().OnActionPerformed;
                if (visible)
                {
                    onActionPerformed.SafeSubscribe(OnSkipInput);
                }
                else
                {
                    onActionPerformed.SafeUnsubscribe(OnSkipInput);
                }
            }
            else if (button == skipButton)
            {
                var onActionPerformed = button.GetComponentInChildren<InputButtonWithPrompt>().OnActionPerformed;
                if (visible)
                {
                    Logger.Log(LogSystems.Tutorial,$"Adding OnContinueInput callback to button {button.name}", button);
                    onActionPerformed.SafeSubscribe(OnContinueInput);
                }
                else
                {
                    Logger.Log(LogSystems.Tutorial,$"Removing OnContinueInput callback to button {button.name}", button);
                    onActionPerformed.SafeUnsubscribe(OnContinueInput);
                }
            }
        }

        private void OnContinueInput()
        {
            HandleContinueClicked();
        }

        private void OnSkipInput()
        {
            HandleSkipClicked();
        }

        #endregion

        #region Private Methods - Button Handlers

        private void HandleSkipClicked()
        {
            Logger.Log(LogSystems.Tutorial, "Skip button clicked", this);
            TutorialManager.Instance?.SkipCurrentTutorial();
        }

        private void HandleContinueClicked()
        {
            Logger.Log(LogSystems.Tutorial, "Continue button clicked", this);

            var currentStep = _currentTutorial?.CurrentStep;
            if (currentStep == null)
            {
                TutorialManager.Instance?.CompleteCurrentStep();
                return;
            }

            // For substep-based steps, skip the current substep
            if (currentStep.HasSubsteps)
            {
                currentStep.SkipCurrentSubstep();
                return;
            }

            // For count-based steps, increment the count
            if (currentStep.IsCountBased)
            {
                currentStep.IncrementCount();
                return;
            }

            // For simple steps, complete the step
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
                Logger.LogError(LogSystems.Tutorial,"Cannot show panel - UIContainer is not assigned.", this);
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
                Logger.LogError(LogSystems.Tutorial,"Cannot hide panel - UIContainer is not assigned.", this);
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