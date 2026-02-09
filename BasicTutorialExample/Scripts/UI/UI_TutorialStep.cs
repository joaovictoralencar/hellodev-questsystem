using System;
using System.Collections.Generic;
using HelloDev.Logging;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Tutorials;
using HelloDev.Utils;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UI;
using Logger = HelloDev.Logging.Logger;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicTutorialExample.UI
{
    /// <summary>
    /// Display mode for substeps within a tutorial step.
    /// </summary>
    public enum SubstepDisplayMode
    {
        /// <summary>
        /// Shows only the current substep with a counter (e.g., "Move Forward (1/4)").
        /// </summary>
        SingleLine,

        /// <summary>
        /// Shows all substeps as a checklist with completion states.
        /// </summary>
        Checklist
    }

    /// <summary>
    /// Component that manages tutorial step content display.
    /// Spawns and manages text items, handles progress bar and step counter.
    /// Buttons are managed by UI_TutorialController.
    /// </summary>
    public class UI_TutorialStep : MonoBehaviour
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [TitleGroup("Container")]
        [PropertyOrder(0)]
        [Required("Content root is required for spawning text items.")]
#else
        [Header("Container")]
#endif
        [SerializeField, Tooltip("Parent transform for spawned text items.")]
        private RectTransform contentRoot;

#if ODIN_INSPECTOR
        [TitleGroup("Container")]
        [PropertyOrder(1)]
        [Required("Text item prefab is required.")]
#endif
        [SerializeField, Tooltip("Prefab to spawn for each instruction line.")]
        private UI_TutorialStepText textItemPrefab;

#if ODIN_INSPECTOR
        [TitleGroup("Progress")]
        [PropertyOrder(10)]
#else
        [Header("Progress")]
#endif
        [SerializeField, Tooltip("Progress bar slider.")]
        private Slider progressBar;

#if ODIN_INSPECTOR
        [TitleGroup("Progress")]
        [PropertyOrder(11)]
#endif
        [SerializeField, Tooltip("Progress percentage text (e.g., '50%').")]
        private TextMeshProUGUI progressText;

#if ODIN_INSPECTOR
        [TitleGroup("Progress")]
        [PropertyOrder(12)]
#endif
        [SerializeField, Tooltip("Step counter text (e.g., '3/4').")]
        private TextMeshProUGUI stepCounterText;

#if ODIN_INSPECTOR
        [TitleGroup("Display Mode")]
        [PropertyOrder(20)]
#else
        [Header("Display Mode")]
#endif
        [SerializeField, Tooltip("How substeps should be displayed.")]
        private SubstepDisplayMode substepDisplayMode = SubstepDisplayMode.SingleLine;
#if ODIN_INSPECTOR
        [TitleGroup("UI Events")]
        [PropertyOrder(30)]
#else
        [Header("UI Events")]
#endif
        [SerializeField]
        UnityEvent OnTutorialCompleted = new();
#if ODIN_INSPECTOR
        [TitleGroup("UI Events")]
        [PropertyOrder(31)]
#endif
        [SerializeField]
        UnityEvent OnDisplayStep = new();
#if ODIN_INSPECTOR
        [TitleGroup("UI Events")]
        [PropertyOrder(32)]
#endif
        [SerializeField]
        UnityEvent OnUpdateCounter = new();
#if ODIN_INSPECTOR
        [TitleGroup("UI Events")]
        [PropertyOrder(33)]
#endif
        [SerializeField]
        UnityEvent OnDisplayStepFirstTime = new();

        #endregion

        #region Private Fields

        private readonly List<UI_TutorialStepText> _activeTextItems = new();
        private TutorialStepRuntime _currentStep;
        private Action StepCompleted;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            CleanupAllTextItems();
        }

        #endregion

        #region Public Methods - Display Step

        public void SaveCurrentStep(TutorialStepRuntime step)
        {
            if (Equals(_currentStep, step))
            {
                return;
            }

            _currentStep = step;
        }

        private void OnCompleted()
        {
            SaveCurrentStep(TutorialManager.Instance.GetCurrentStep());
            // For non-checklist modes or simple steps, rebuild as before

            if (TutorialManager.Instance.CurrentTutorial == null)
            {
                OnTutorialCompleted.SafeInvoke();
            }
            else
            {
                ShowStep(_currentStep);
            }
        }

        bool firstShow = true;

        public void ShowStep(TutorialStepRuntime step)
        {
            if (step == null) return;

            if (step.CurrentState != ObjectiveState.Completed)
            {
                if (firstShow)
                {
                    OnDisplayStepFirstTime.SafeInvoke();
                    firstShow = false;
                }
                else OnDisplayStep.SafeInvoke();
            }

            Logger.LogVerbose(LogSystems.Tutorial, $"[UI_TutorialStep] ShowStep called for '{step.DevName}' HasSubsteps={step.HasSubsteps} CompletedSubsteps={step.CompletedSubstepCount}/{step.TotalSubstepCount}");

            // Clean up previous content
            if (contentRoot != null)
            {
                contentRoot.DestroyAllChildren();
            }

            _activeTextItems.Clear();

            if (step.HasSubsteps)
            {
                DisplaySubstepBasedStep(step);
            }
            else if (step.IsCountBased)
            {
                DisplayCountBasedStep(step);
            }
            else
            {
                DisplaySimpleStep(step);
            }

            SetStepCounter(step.StepIndex + 1, TutorialManager.Instance.CurrentTutorial.Steps.Count);
        }

        /// <summary>
        /// Updates the display for count-based progression.
        /// </summary>
        /// <param name="current">Current count.</param>
        /// <param name="required">Required count.</param>
        public void UpdateCountProgress(int current, int required)
        {
            if (_currentStep == null || !_currentStep.IsCountBased) return;

            // Update the existing text item with new count
            if (_activeTextItems.Count > 0)
            {
                UI_TutorialStepText textItem = _activeTextItems[0];
                LocalizedString instruction = _currentStep.Data.Instruction;

                if (instruction != null && !instruction.IsEmpty)
                {
                    textItem.SetInstruction(instruction, current, required);
                }
                else
                {
                    textItem.SetInstruction(_currentStep.Data.Instruction, current, required);
                }

                OnUpdateCounter.SafeInvoke();
            }
        }

        #endregion

        #region Public Methods - Progress

        /// <summary>
        /// Sets the overall tutorial progress (0-1).
        /// </summary>
        /// <param name="progress">Progress value between 0 and 1.</param>
        public void SetProgress(float progress)
        {
            float duration = 1;
            Ease ease = Ease.OutExpo;
            if (progressBar != null)
            {
                progress = Mathf.Clamp01(progress);
                if (progressBar.value != progress)
                {
                    Tween.UISliderValue(progressBar, progress, duration, ease, useUnscaledTime: true);
                }
            }

            if (progressText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100);
                int prevProgress = int.Parse(progressText.text.Substring(0, progressText.text.Length - 1));
                prevProgress = Mathf.Clamp(prevProgress, 0, 100);
                Tween.Custom(prevProgress, percentage, duration: duration, onValueChange: tempValue => progressText.text = $"{Mathf.RoundToInt(tempValue)}%");
            }
        }

        /// <summary>
        /// Sets the step counter display.
        /// </summary>
        /// <param name="current">Current step number (1-based).</param>
        /// <param name="total">Total number of steps.</param>
        public void SetStepCounter(int current, int total)
        {
            if (stepCounterText != null)
                stepCounterText.text = $"{current}/{total}";
        }

        #endregion

        #region Private Methods - Display

        private void DisplaySubstepBasedStep(TutorialStepRuntime step)
        {
            UI_TutorialStepText item = null;
            if (substepDisplayMode == SubstepDisplayMode.Checklist)
            {
                var seq = Sequence.Create();
                // Show all substeps as checklist with states
                int spawnIndex = 0;
                foreach (TutorialSubstep_SO substep in step.Data.Substeps)
                {
                    TutorialStepTextState state = GetSubstepState(step, substep);
                    Logger.Log(LogSystems.Tutorial, $"[UI_TutorialStep] Spawning substep[{spawnIndex}] state={state} name={substep.DevName}");
                    item = SpawnTextItem(substep.Instruction, state, false);
                    seq.ChainCallback(() => OnDisplayStep.SafeInvoke());
                    seq.Chain(item.AnimateIn());
                    spawnIndex++;
                    if (item != null) item.SetStep(step);
                }
            }
            else // SingleLine
            {
                // Show only current substep with counter
                TutorialSubstep_SO currentSubstep = step.CurrentSubstep;
                if (currentSubstep != null)
                {
                    SpawnTextItemWithCounter(currentSubstep.Instruction, step.CompletedSubstepCount + 1, step.TotalSubstepCount);
                }
                else
                {
                    // All substeps complete, show step instruction
                    item = SpawnTextItem(step.Data.Instruction, TutorialStepTextState.Completed);
                    step.OnStepCompleted.SafeSubscribe(item.RunCompleteStepAnimation);
                    if (item != null) item.SetStep(step);
                }
            }
        }

        private void DisplayCountBasedStep(TutorialStepRuntime step)
        {
            // Show step instruction with count: "Jump! (0/3)"
            UI_TutorialStepText item = SpawnTextItemWithCounter(step.Data.Instruction, step.CurrentCount, step.RequiredCount);
            step.OnStepCompleted.SafeSubscribe(item.RunCompleteStepAnimation);

            if (item != null) item.SetStep(step);
        }

        private void DisplaySimpleStep(TutorialStepRuntime step)
        {
            UI_TutorialStepText item = SpawnTextItem(step.Data.Instruction, TutorialStepTextState.Active);
            step.OnStepCompleted.SafeSubscribe(item.RunCompleteStepAnimation);
            if (item != null) item.SetStep(step);
        }

        #endregion

        #region Private Methods - Spawning

        private UI_TutorialStepText SpawnTextItem(LocalizedString instruction, TutorialStepTextState state, bool animateIn = true)
        {
            UI_TutorialStepText item = CreateTextItem();
            item.SetOnCompleteStepAction(OnCompleted);

            if (item == null) return null;

            if (instruction is { IsEmpty: false })
            {
                item.SetInstruction(instruction);
            }

            item.SetState(state);

            if (animateIn)
            {
                // Start the animation
                item.AnimateIn();
            }

            _activeTextItems.Add(item);
            return item;
        }

        private UI_TutorialStepText SpawnTextItemWithCounter(LocalizedString instruction, int current, int total)
        {
            UI_TutorialStepText item = CreateTextItem();

            if (item == null) return null;

            if (instruction != null && !instruction.IsEmpty)
            {
                item.SetInstruction(instruction, current, total);
            }
            else
            {
                item.SetInstruction(instruction, current, total);
            }

            item.SetState(TutorialStepTextState.Active);
            item.AnimateIn();

            _activeTextItems.Add(item);
            return item;
        }

        private UI_TutorialStepText CreateTextItem()
        {
            if (textItemPrefab == null || contentRoot == null)
            {
                Debug.LogError("[UI_TutorialStep] Cannot spawn text item - prefab or content root is null.", this);
                return null;
            }

            UI_TutorialStepText uiTutorialStepText = Instantiate(textItemPrefab, contentRoot);
            uiTutorialStepText.SetOnCompleteStepAction(OnCompleted);

            return uiTutorialStepText;
        }

        #endregion

        #region Private Methods - Helpers

        private TutorialStepTextState GetSubstepState(TutorialStepRuntime step, TutorialSubstep_SO substep)
        {
            if (step.IsSubstepCompleted(substep))
                return TutorialStepTextState.Completed;

            if (substep == step.CurrentSubstep)
                return TutorialStepTextState.Active;

            return TutorialStepTextState.Pending;
        }

        private void CleanupAllTextItems()
        {
            // Stop any running animations on text items before destroying them
            foreach (UI_TutorialStepText item in _activeTextItems)
            {
                if (item != null && item.gameObject != null)
                {
                    Tween.StopAll(item.gameObject);
                }
            }

            // Also stop any tweens on the content root itself
            if (contentRoot != null)
            {
                Tween.StopAll(contentRoot.gameObject);
                contentRoot.DestroyAllChildren();
            }

            _activeTextItems.Clear();
        }

        #endregion

        #region Public Methods - Sync

        /// <summary>
        /// Immediately refreshes checklist UI states to match runtime step state without running completion animations.
        /// Useful as a fallback when the UI might be out-of-sync (e.g., manual skip).
        /// </summary>
        public void ForceRefreshSubstepUI(TutorialStepRuntime step)
        {
            if (step == null || !step.HasSubsteps) return;

            // Guard: Don't refresh if this isn't the current step being displayed
            // This prevents late callbacks from old steps interfering with new step UI
            if (!Equals(_currentStep, step))
            {
                return;
            }

            if (_activeTextItems.Count == 0)
            {
                ShowStep(step);
                return;
            }

            IReadOnlyList<TutorialSubstep_SO> substeps = step.Data.Substeps;
            int count = Math.Min(_activeTextItems.Count, substeps.Count);
            for (int i = 0; i < count; i++)
            {
                UI_TutorialStepText item = _activeTextItems[i];
                TutorialStepTextState desired = GetSubstepState(step, substeps[i]);

                // Stop any running animations before changing state to prevent tween errors
                if (item != null && item.gameObject != null)
                {
                    Tween.StopAll(item.gameObject);
                }

                // Directly set state without triggering completion callback animation
                item.SetState(desired);
            }

            // Ensure the currently active item is visible/active
            int activeIndex = step.CurrentSubstepIndex;
            if (activeIndex >= 0 && activeIndex < _activeTextItems.Count)
            {
                if (_activeTextItems[activeIndex] != null && _activeTextItems[activeIndex].gameObject != null)
                {
                    Tween.StopAll(_activeTextItems[activeIndex].gameObject);
                }

                _activeTextItems[activeIndex].SetState(TutorialStepTextState.Active);
            }

            if (transform is RectTransform rt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        #endregion
    }
}