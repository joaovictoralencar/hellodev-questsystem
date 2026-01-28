using System.Collections.Generic;
using HelloDev.QuestSystem.Tutorials;
using HelloDev.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        #endregion

        #region Private Fields

        private readonly List<UI_TutorialStepText> _activeTextItems = new();
        private TutorialStepRuntime _currentStep;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently displayed step.
        /// </summary>
        public TutorialStepRuntime CurrentStep => _currentStep;

        /// <summary>
        /// Gets the substep display mode.
        /// </summary>
        public SubstepDisplayMode DisplayMode => substepDisplayMode;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            CleanupAllTextItems();
        }

        #endregion

        #region Public Methods - Display Step

        /// <summary>
        /// Displays a tutorial step, spawning appropriate text items.
        /// </summary>
        /// <param name="step">The step to display.</param>
        public void DisplayStep(TutorialStepRuntime step)
        {
            // Clean up previous content
            if (contentRoot != null)
                contentRoot.DestroyAllChildren();
            _activeTextItems.Clear();
            _currentStep = step;

            if (step == null) return;

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
        }

        /// <summary>
        /// Updates the display for substep progression.
        /// </summary>
        /// <param name="step">The step with updated substep progress.</param>
        public void UpdateSubstepProgress(TutorialStepRuntime step)
        {
            if (step == null || _currentStep != step) return;

            if (substepDisplayMode == SubstepDisplayMode.Checklist)
            {
                // Update states of existing items
                var substeps = step.Data.Substeps;
                for (int i = 0; i < _activeTextItems.Count && i < substeps.Count; i++)
                {
                    var substep = substeps[i];
                    var state = GetSubstepState(step, substep);
                    _activeTextItems[i].SetState(state);
                }
            }
            else // SingleLine
            {
                // Replace with new substep
                DisplayStep(step);
            }
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
                var textItem = _activeTextItems[0];
                var instruction = _currentStep.Data.Instruction;

                if (instruction != null && !instruction.IsEmpty)
                {
                    textItem.SetInstruction(instruction, current, required);
                }
                else
                {
                    textItem.SetInstruction(_currentStep.DevName, current, required);
                }
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
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(progress);

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
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

        #region Public Methods - Clear

        /// <summary>
        /// Clears all spawned text items.
        /// </summary>
        public void ClearTextItems()
        {
            if (contentRoot != null)
                contentRoot.DestroyAllChildren();

            _activeTextItems.Clear();
            _currentStep = null;
        }

        #endregion

        #region Private Methods - Display

        private void DisplaySubstepBasedStep(TutorialStepRuntime step)
        {
            if (substepDisplayMode == SubstepDisplayMode.Checklist)
            {
                // Show all substeps as checklist with states
                foreach (var substep in step.Data.Substeps)
                {
                    var state = GetSubstepState(step, substep);
                    SpawnTextItem(substep.Instruction, substep.DevName, state);
                }
            }
            else // SingleLine
            {
                // Show only current substep with counter
                var currentSubstep = step.CurrentSubstep;
                if (currentSubstep != null)
                {
                    SpawnTextItemWithCounter(
                        currentSubstep.Instruction,
                        currentSubstep.DevName,
                        step.CompletedSubstepCount + 1,
                        step.TotalSubstepCount);
                }
                else
                {
                    // All substeps complete, show step instruction
                    SpawnTextItem(step.Data.Instruction, step.DevName, TutorialStepTextState.Completed);
                }
            }
        }

        private void DisplayCountBasedStep(TutorialStepRuntime step)
        {
            // Show step instruction with count: "Jump! (0/3)"
            SpawnTextItemWithCounter(
                step.Data.Instruction,
                step.DevName,
                step.CurrentCount,
                step.RequiredCount);
        }

        private void DisplaySimpleStep(TutorialStepRuntime step)
        {
            // Simple step - show instruction
            SpawnTextItem(step.Data.Instruction, step.DevName, TutorialStepTextState.Active);
        }

        #endregion

        #region Private Methods - Spawning

        private UI_TutorialStepText SpawnTextItem(
            UnityEngine.Localization.LocalizedString instruction,
            string fallbackText,
            TutorialStepTextState state)
        {
            var item = CreateTextItem();
            if (item == null) return null;

            if (instruction != null && !instruction.IsEmpty)
            {
                item.SetInstruction(instruction);
            }
            else
            {
                item.SetInstruction(fallbackText ?? "");
            }

            item.SetState(state);
            item.AnimateIn();

            _activeTextItems.Add(item);
            return item;
        }

        private UI_TutorialStepText SpawnTextItemWithCounter(
            UnityEngine.Localization.LocalizedString instruction,
            string fallbackText,
            int current,
            int total)
        {
            var item = CreateTextItem();
            if (item == null) return null;

            if (instruction != null && !instruction.IsEmpty)
            {
                item.SetInstruction(instruction, current, total);
            }
            else
            {
                item.SetInstruction(fallbackText ?? "", current, total);
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

            return Instantiate(textItemPrefab, contentRoot);
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
            if (contentRoot != null)
                contentRoot.DestroyAllChildren();

            _activeTextItems.Clear();
        }

        #endregion
    }
}
