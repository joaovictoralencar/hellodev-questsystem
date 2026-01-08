#if UNITY_EDITOR
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.UI.Default;
using HelloDev.Utils;
using UnityEngine;
using static HelloDev.QuestSystem.Utils.QuestLogger;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    /// <summary>
    /// Debug functionality for UI_QuestDetails. Editor-only.
    /// Contains debug buttons, inspector displays, and debug action methods.
    /// </summary>
    public partial class UI_QuestDetails
    {
        #region Runtime State Display

#if ODIN_INSPECTOR
        [TitleGroup("Runtime State")]
        [PropertyOrder(40)]
        [ShowInInspector, ReadOnly]
        [InfoBox("Runtime state is only visible during Play mode.", InfoMessageType.Info, "@!UnityEngine.Application.isPlaying")]
        private string CurrentQuestName => _currentQuest?.QuestData?.DevName ?? "None";

        [TitleGroup("Runtime State")]
        [PropertyOrder(41)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentQuest != null")]
        private string CurrentQuestState => _currentQuest?.CurrentState.ToString() ?? "N/A";

        [TitleGroup("Runtime State")]
        [PropertyOrder(42)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentQuest != null")]
        [ProgressBar(0, 1, ColorGetter = nameof(GetProgressColor))]
        private float CurrentQuestProgress => _currentQuest?.CurrentProgress ?? 0f;

        [TitleGroup("Runtime State")]
        [PropertyOrder(43)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentTask != null")]
        private string CurrentTaskName => _currentTask?.DevName ?? "None";

        [TitleGroup("Runtime State")]
        [PropertyOrder(44)]
        [ShowInInspector, ReadOnly]
        [ShowIf("@UnityEngine.Application.isPlaying && _currentTask != null")]
        private string CurrentTaskState => _currentTask?.CurrentState.ToString() ?? "N/A";

        private Color GetProgressColor()
        {
            if (_currentQuest == null) return Color.gray;
            return _currentQuest.CurrentState switch
            {
                QuestState.Completed => Color.green,
                QuestState.Failed => Color.red,
                QuestState.InProgress => new Color(0.2f, 0.6f, 1f),
                _ => Color.gray
            };
        }
#endif

        #endregion

        #region Debug Serialized Fields

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug", expanded: false)]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(50)]
#else
        [Header("Debug - Tasks")]
#endif
        [SerializeField] private UIButton CompleteCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(51)]
#endif
        [SerializeField] private UIButton FailCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(52)]
#endif
        [SerializeField] private UIButton InvokeEventTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(53)]
#endif
        [SerializeField] private UIButton IncrementCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(54)]
#endif
        [SerializeField] private UIButton DecrementCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Task Actions")]
        [PropertyOrder(55)]
#endif
        [SerializeField] private UIButton ResetCurrentTaskButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quest Actions")]
        [PropertyOrder(60)]
#else
        [Header("Debug - Quests")]
#endif
        [SerializeField] private UIButton CompleteCurrentQuestButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quest Actions")]
        [PropertyOrder(61)]
#endif
        [SerializeField] private UIButton FailCurrentQuestButton;

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quest Actions")]
        [PropertyOrder(62)]
#endif
        [SerializeField] private UIButton ResetCurrentQuestButton;

        #endregion

        #region Odin Quick Action Buttons

#if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(70)]
        [Button("Complete Current Task", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask != null && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickCompleteTask() => _currentTask?.CompleteTask();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(71)]
        [Button("Fail Current Task", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask != null && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickFailTask() => _currentTask?.FailTask();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(72)]
        [Button("Increment Task", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask != null && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickIncrementTask() => _currentTask?.IncrementStep();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(73)]
        [Button("Complete Current Quest", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickCompleteQuest() => DebugCompleteQuest();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Quick Actions")]
        [PropertyOrder(74)]
        [Button("Fail Current Quest", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickFailQuest() => _currentQuest?.FailQuest();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Location Task")]
        [PropertyOrder(80)]
        [Button("Trigger Location Reached", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is LocationTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickTriggerLocation() => _currentTask?.IncrementStep();

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Timed Task")]
        [PropertyOrder(81)]
        [Button("Add 30 Seconds", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is TimedTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickAddTime()
        {
            if (_currentTask is TimedTaskRuntime timedTask)
                timedTask.AddTime(30f);
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Timed Task")]
        [PropertyOrder(82)]
        [Button("Expire Timer", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is TimedTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickExpireTimer()
        {
            if (_currentTask is TimedTaskRuntime timedTask)
                timedTask.UpdateTimer(timedTask.RemainingTime + 1f);
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Timed Task")]
        [PropertyOrder(83)]
        [Button("Complete Timed Objective", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is TimedTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickCompleteTimedObjective()
        {
            if (_currentTask is TimedTaskRuntime timedTask)
                timedTask.MarkObjectiveComplete();
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Discovery Task")]
        [PropertyOrder(84)]
        [Button("Discover Next Item", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentTask is DiscoveryTaskRuntime && _currentTask.CurrentState == HelloDev.QuestSystem.Tasks.TaskState.InProgress")]
        private void QuickDiscoverItem()
        {
            if (_currentTask is DiscoveryTaskRuntime discoveryTask)
                discoveryTask.IncrementStep();
        }

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Player Choices")]
        [PropertyOrder(90)]
        [Button("Select Choice 1", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickSelectChoice1() => DebugSelectChoice(0);

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Player Choices")]
        [PropertyOrder(91)]
        [Button("Select Choice 2", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickSelectChoice2() => DebugSelectChoice(1);

        [FoldoutGroup("Debug")]
        [TitleGroup("Debug/Player Choices")]
        [PropertyOrder(92)]
        [Button("Select Choice 3", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying && _currentQuest != null && _currentQuest.CurrentState == HelloDev.QuestSystem.Quests.QuestState.InProgress")]
        private void QuickSelectChoice3() => DebugSelectChoice(2);
#endif

        #endregion

        #region Partial Method Implementations

        partial void OnSetupDebug()
        {
            SetupDebugButtons();
        }

        partial void OnTaskSelectedDebug()
        {
            SetupTaskDebugButtons();
            UpdateDebugButtons();
        }

        partial void OnTaskUpdatedDebug()
        {
            UpdateDebugButtons();
        }

        #endregion

        #region Debug Methods

        private void SetupDebugButtons()
        {
            CompleteCurrentQuestButton?.OnClick.SafeSubscribe(DebugCompleteQuest);
            FailCurrentQuestButton?.OnClick.SafeSubscribe(DebugFailQuest);
            ResetCurrentQuestButton?.OnClick.SafeSubscribe(DebugResetQuest);
            UpdateDebugButtons();
        }

        private void SetupTaskDebugButtons()
        {
            CompleteCurrentTaskButton?.OnClick.SafeSubscribe(DebugCompleteTask);
            FailCurrentTaskButton?.OnClick.SafeSubscribe(DebugFailTask);
            ResetCurrentTaskButton?.OnClick.SafeSubscribe(DebugResetTask);
            IncrementCurrentTaskButton?.OnClick.SafeSubscribe(DebugIncrementTask);
            DecrementCurrentTaskButton?.OnClick.SafeSubscribe(DebugDecrementTask);
            InvokeEventTaskButton?.OnClick.SafeSubscribe(DebugEventTask);
        }

        private void UpdateDebugButtons()
        {
            if (_currentTask != null)
            {
                bool isInProgress = _currentTask.CurrentState == TaskState.InProgress;
                bool isCompleted = _currentTask.CurrentState == TaskState.Completed;

                CompleteCurrentTaskButton?.SetInteractable(isInProgress);
                IncrementCurrentTaskButton?.SetInteractable(isInProgress);
                DecrementCurrentTaskButton?.SetInteractable(isInProgress || isCompleted);
                FailCurrentTaskButton?.SetInteractable(isInProgress || isCompleted);
                ResetCurrentTaskButton?.SetInteractable(_currentTask.CurrentState != TaskState.NotStarted);
            }

            if (_currentQuest != null)
            {
                bool isInProgress = _currentQuest.CurrentState == QuestState.InProgress;

                CompleteCurrentQuestButton?.SetInteractable(isInProgress);
                FailCurrentQuestButton?.SetInteractable(isInProgress);
                ResetCurrentQuestButton?.SetInteractable(_currentQuest.CurrentState != QuestState.NotStarted);
            }
        }

        private void DebugCompleteQuest()
        {
            if (_currentQuest == null) return;
            foreach (TaskRuntime task in _currentQuest.Tasks)
                task.CompleteTask();
        }

        private void DebugFailQuest() => _currentQuest?.FailQuest();

        private void DebugResetQuest()
        {
            if (_currentQuest == null) return;
            QuestManager.Instance.RestartQuest(_currentQuest.QuestData);
        }

        private void DebugCompleteTask() => _currentTask?.CompleteTask();

        private void DebugFailTask() => _currentTask?.FailTask();

        private void DebugResetTask()
        {
            if (_currentQuest == null || _currentTask == null) return;

            var tasks = _currentQuest.Tasks;
            int index = tasks.Select((t, i) => (Task: t, Index: i))
                .FirstOrDefault(x => x.Task == _currentTask).Index;
            for (int i = index; i < tasks.Count; i++)
                tasks[i].ResetTask();

            _currentTask.StartTask();
        }

        private void DebugIncrementTask() => _currentTask?.IncrementStep();

        private void DebugDecrementTask() => _currentTask?.DecrementStep();

        private void DebugEventTask()
        {
            if (_currentTask == null || _currentTask.CurrentState != TaskState.InProgress)
            {
                Log(LogSubsystem.UI, $"[DebugEventTask] Skipped - task is null or not in progress.");
                return;
            }

            Log(LogSubsystem.UI, $"[DebugEventTask] Called for task: {_currentTask.DevName}");

            // Try to find an event-driven condition to fulfill
            if (_currentTask.Data?.Conditions != null)
            {
                var discoveryTask = _currentTask as DiscoveryTaskRuntime;

                // First pass: find unfulfilled conditions
                foreach (Condition_SO condition in _currentTask.Data.Conditions)
                {
                    if (condition is not IConditionEventDriven conditionEventDriven)
                        continue;

                    bool isAlreadyFulfilled = discoveryTask != null
                        ? discoveryTask.FulfilledConditions.Contains(condition)
                        : condition.Evaluate();

                    if (isAlreadyFulfilled) continue;

                    Log(LogSubsystem.UI, $"[DebugEventTask] Calling ForceFulfillCondition() on '{condition.name}'");
                    conditionEventDriven.ForceFulfillCondition();
                    return;
                }

                // Second pass: re-trigger for repeatable tasks (not discovery tasks)
                if (discoveryTask == null)
                {
                    foreach (Condition_SO condition in _currentTask.Data.Conditions)
                    {
                        if (condition is not IConditionEventDriven conditionEventDriven) continue;

                        Log(LogSubsystem.UI, $"[DebugEventTask] Re-triggering condition '{condition.name}'");
                        conditionEventDriven.ForceFulfillCondition();
                        return;
                    }
                }
            }

            // Fallback to increment step
            Log(LogSubsystem.UI, "[DebugEventTask] Falling back to IncrementStep()");
            _currentTask.IncrementStep();
        }

        private void DebugSelectChoice(int choiceIndex)
        {
            if (_currentQuest == null || _currentQuest.CurrentState != QuestState.InProgress)
            {
                Log(LogSubsystem.UI, "[DebugSelectChoice] No active quest");
                return;
            }

            var currentStage = _currentQuest.CurrentStage;
            if (currentStage == null)
            {
                Log(LogSubsystem.UI, "[DebugSelectChoice] No current stage");
                return;
            }

            var choices = currentStage.Data.GetAllPlayerChoices();
            if (choices == null || choices.Count == 0)
            {
                Log(LogSubsystem.UI, "[DebugSelectChoice] No choices available");
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                Log(LogSubsystem.UI, $"[DebugSelectChoice] Choice index {choiceIndex} out of range");
                return;
            }

            var choice = choices[choiceIndex];
            Log(LogSubsystem.UI, $"[DebugSelectChoice] Selecting choice '{choice.ChoiceId}'");

            bool success = _currentQuest.SelectChoice(choice, bypassConditions: true);
            Log(LogSubsystem.UI, $"[DebugSelectChoice] SelectChoice result: {success}");
        }

        #endregion
    }
}
#endif
