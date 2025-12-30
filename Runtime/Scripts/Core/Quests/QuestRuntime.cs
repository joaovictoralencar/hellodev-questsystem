using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Quests
{
    /// <summary>
    /// Represents a runtime quest instance. This class provides the core structure and
    /// state management for all quests. Supports both legacy (flat task groups) and
    /// stage-based quests (Skyrim-style multi-phase).
    /// </summary>
    public class QuestRuntime
    {
        #region Events - Quest Lifecycle

        /// <summary>Fired when the quest starts.</summary>
        public UnityEvent<QuestRuntime> OnQuestStarted = new();

        /// <summary>Fired when the quest completes successfully.</summary>
        public UnityEvent<QuestRuntime> OnQuestCompleted = new();

        /// <summary>Fired when the quest fails.</summary>
        public UnityEvent<QuestRuntime> OnQuestFailed = new();

        /// <summary>Fired when the quest is reset and restarted.</summary>
        public UnityEvent<QuestRuntime> OnQuestRestarted = new();

        /// <summary>Fired when quest progress changes (stage advances, group advances, task completes, etc.).</summary>
        public UnityEvent<QuestRuntime> OnQuestUpdated = new();

        #endregion

        #region Events - Tasks

        /// <summary>Fired when any task in this quest starts.</summary>
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskStarted = new();

        /// <summary>Fired when any task in this quest is updated.</summary>
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskUpdated = new();

        /// <summary>Fired when any task in this quest completes.</summary>
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskCompleted = new();

        /// <summary>Fired when any task in this quest fails.</summary>
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskFailed = new();

        #endregion

        #region Events - Stages

        /// <summary>Fired when a stage is entered.</summary>
        public UnityEvent<QuestRuntime, QuestStageRuntime> OnStageEntered = new();

        /// <summary>Fired when a stage is completed.</summary>
        public UnityEvent<QuestRuntime, QuestStageRuntime> OnStageCompleted = new();

        /// <summary>Fired when a stage transition occurs.</summary>
        public UnityEvent<QuestRuntime, StageTransitionInfo> OnStageTransition = new();

        #endregion

        #region Events - Player Choices

        /// <summary>
        /// Fired when a stage with player choices becomes active.
        /// Game systems (UI, dialogue, etc.) can subscribe to present choices.
        /// The list contains all PlayerChoice transitions for the current stage.
        /// </summary>
        public UnityEvent<QuestRuntime, List<StageTransition>> OnChoicesAvailable = new();

        /// <summary>
        /// Fired when a player choice is made (either explicitly via SelectChoice or implicitly via conditions).
        /// </summary>
        public UnityEvent<QuestRuntime, StageTransition> OnChoiceMade = new();

        /// <summary>
        /// Fired when a player choice's availability changes (conditions met/unmet).
        /// Useful for updating UI to enable/disable choice buttons.
        /// </summary>
        public UnityEvent<QuestRuntime, StageTransition, bool> OnChoiceAvailabilityChanged = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique identifier for this quest.
        /// </summary>
        public Guid QuestId { get; }

        /// <summary>
        /// Gets the current state of this quest.
        /// </summary>
        public QuestState CurrentState { get; private set; }

        /// <summary>
        /// Gets the ScriptableObject data for this quest.
        /// </summary>
        public Quest_SO QuestData { get; }

        /// <summary>
        /// Gets all stages in this quest.
        /// </summary>
        public List<QuestStageRuntime> Stages { get; }

        /// <summary>
        /// Gets the currently active stage, or null if quest is not in progress.
        /// </summary>
        public QuestStageRuntime CurrentStage { get; private set; }

        /// <summary>
        /// Gets the index of the current stage.
        /// </summary>
        public int CurrentStageIndex => CurrentStage?.StageIndex ?? -1;

        /// <summary>
        /// Gets all task groups across all stages (flattened for backward compatibility).
        /// </summary>
        public List<TaskGroupRuntime> TaskGroups => Stages.SelectMany(s => s.TaskGroups).ToList();

        /// <summary>
        /// Gets the currently active task group from the current stage.
        /// </summary>
        public TaskGroupRuntime CurrentGroup => CurrentStage?.CurrentGroup;

        /// <summary>
        /// Gets all tasks that are currently in progress (can be multiple for parallel groups).
        /// </summary>
        public IReadOnlyList<TaskRuntime> CurrentTasks =>
            CurrentStage?.CurrentTasks ?? Array.Empty<TaskRuntime>();

        /// <summary>
        /// Gets the first currently in-progress task, or null if none.
        /// Use CurrentTasks for parallel groups where multiple tasks may be active.
        /// </summary>
        public TaskRuntime CurrentTask => CurrentTasks.FirstOrDefault();

        /// <summary>
        /// Gets all tasks across all stages and groups (flattened list for backward compatibility).
        /// </summary>
        public List<TaskRuntime> Tasks => Stages.SelectMany(s => s.AllTasks).ToList();

        /// <summary>
        /// Gets the overall progress of this quest (0-1).
        /// Calculated as the weighted average of stage progress.
        /// </summary>
        public float CurrentProgress
        {
            get
            {
                if (Stages.Count == 0) return CurrentState == QuestState.Completed ? 1f : 0f;

                float totalProgress = 0f;
                int totalTaskCount = 0;

                foreach (var stage in Stages)
                {
                    int stageTaskCount = stage.AllTasks.Count;
                    totalProgress += stage.Progress * stageTaskCount;
                    totalTaskCount += stageTaskCount;
                }

                return totalTaskCount > 0 ? totalProgress / totalTaskCount : 1f;
            }
        }

        /// <summary>
        /// Dictionary tracking which branch decisions were made (for branching quests).
        /// Key is branch ID, value is choice ID.
        /// </summary>
        public Dictionary<string, string> BranchDecisions { get; } = new();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestRuntime"/> class.
        /// </summary>
        /// <param name="questData">The quest data.</param>
        /// <remarks>
        /// This constructor creates a runtime instance from a Quest_SO asset.
        /// Creates QuestStageRuntime instances from the quest's stages.
        /// Legacy quests (without stages) are auto-wrapped in a single stage.
        /// </remarks>
        public QuestRuntime(Quest_SO questData)
        {
            QuestData = questData;
            QuestId = questData.QuestId;
            CurrentState = QuestState.NotStarted;

            // Create runtime stages from the quest data
            // Quest_SO.Stages already handles legacy mode by returning a single auto-generated stage
            Stages = questData.Stages
                .Select(stageData => new QuestStageRuntime(stageData))
                .ToList();
        }

        private void UpdateQuestState(QuestState newState)
        {
            CurrentState = newState;
        }

        /// <summary>
        /// Attempts to start the quest, changing its state to InProgress if possible.
        /// Starts the first stage.
        /// </summary>
        public void StartQuest()
        {
            if (CurrentState != QuestState.NotStarted)
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{QuestData.DevName}' already in progress");
                return;
            }

            UnsubscribeFromStartConditions();
            SubscribeToAllEvents();

            UpdateQuestState(QuestState.InProgress);

            // Start the first stage (lowest index)
            var firstStage = GetStageByIndex(GetFirstStageIndex());
            if (firstStage != null)
            {
                TransitionToStage(firstStage);
                QuestLogger.LogStart(LogSubsystem.Quest, "Quest", QuestData.DevName);
            }
            else
            {
                QuestLogger.LogStart(LogSubsystem.Quest, "Quest", $"{QuestData.DevName} (no stages)");
            }

            OnQuestStarted.SafeInvoke(this);
        }

        /// <summary>
        /// Marks the quest as completed, changing its state to Completed.
        /// Distributes all rewards and fires completion events.
        /// </summary>
        public void CompleteQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                UnsubscribeFromAllEvents();

                // Distribute rewards
                if (QuestData.Rewards != null)
                {
                    foreach (var reward in QuestData.Rewards)
                    {
                        if (reward.RewardType != null)
                        {
                            reward.RewardType.GiveReward(reward.Amount);
                            QuestLogger.LogVerbose(LogSubsystem.Quest, $"Reward: {reward.RewardType.name} x{reward.Amount}");
                        }
                    }
                }

                OnQuestUpdated.SafeInvoke(this);
                UpdateQuestState(QuestState.Completed);
                OnQuestCompleted.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Marks the quest as failed, changing its state to Failed.
        /// </summary>
        public void FailQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                UpdateQuestState(QuestState.Failed);
                UnsubscribeFromAllEvents();
                OnQuestFailed.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Resets the quest to its initial state and restarts it.
        /// </summary>
        public void ResetQuest()
        {
            UnsubscribeFromAllEvents();

            // Reset all stages
            foreach (var stage in Stages)
            {
                stage.Reset();
            }

            CurrentStage = null;
            BranchDecisions.Clear();
            UpdateQuestState(QuestState.NotStarted);
            StartQuest();
            OnQuestRestarted.SafeInvoke(this);
        }

        #region Stage Management

        /// <summary>
        /// Attempts to set the quest to a specific stage by index.
        /// Used for manual stage transitions (e.g., from dialogue).
        /// </summary>
        /// <param name="stageIndex">The index of the stage to transition to.</param>
        /// <returns>True if transition was successful.</returns>
        public bool TrySetStage(int stageIndex)
        {
            if (CurrentState != QuestState.InProgress)
            {
                QuestLogger.LogWarning(LogSubsystem.Stage, $"Cannot set stage: '{QuestData.DevName}' not in progress");
                return false;
            }

            var targetStage = GetStageByIndex(stageIndex);
            if (targetStage == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Stage, $"Stage {stageIndex} not found in '{QuestData.DevName}'");
                return false;
            }

            int previousIndex = CurrentStageIndex;
            TransitionToStage(targetStage);
            OnStageTransition.SafeInvoke(this, new StageTransitionInfo(previousIndex, stageIndex));

            return true;
        }

        /// <summary>
        /// Gets a stage by its index.
        /// </summary>
        /// <param name="stageIndex">The stage index.</param>
        /// <returns>The stage, or null if not found.</returns>
        public QuestStageRuntime GetStageByIndex(int stageIndex)
        {
            return Stages.FirstOrDefault(s => s.StageIndex == stageIndex);
        }

        /// <summary>
        /// Gets the first stage index (lowest number).
        /// </summary>
        private int GetFirstStageIndex()
        {
            return Stages.Count > 0 ? Stages.Min(s => s.StageIndex) : -1;
        }

        /// <summary>
        /// Transitions to a new stage.
        /// </summary>
        private void TransitionToStage(QuestStageRuntime targetStage)
        {
            // Unsubscribe from previous stage's choice conditions
            UnsubscribeFromPlayerChoiceConditions();

            // Complete current stage if it's still in progress
            if (CurrentStage?.CurrentState == StageState.InProgress)
            {
                CurrentStage.Complete();
                OnStageCompleted.SafeInvoke(this, CurrentStage);
            }

            CurrentStage = targetStage;
            targetStage.Enter();
            OnStageEntered.SafeInvoke(this, targetStage);
            NotifyQuestUpdated();

            // If the new stage has player choices, set up choice handling
            if (targetStage.Data.HasPlayerChoices)
            {
                SubscribeToPlayerChoiceConditions();
                NotifyChoicesAvailable();
            }
        }

        #endregion

        #region Player Choice Methods

        /// <summary>
        /// Gets all player choices available in the current stage.
        /// Returns only choices whose conditions are met.
        /// </summary>
        /// <returns>List of available player choice transitions.</returns>
        public List<StageTransition> GetAvailableChoices()
        {
            if (CurrentStage == null || CurrentState != QuestState.InProgress)
                return new List<StageTransition>();

            return CurrentStage.Data.GetAvailablePlayerChoices();
        }

        /// <summary>
        /// Gets all player choices in the current stage, regardless of condition state.
        /// Useful for displaying all options with some potentially greyed out.
        /// </summary>
        /// <returns>List of all player choice transitions.</returns>
        public List<StageTransition> GetAllChoices()
        {
            if (CurrentStage == null || CurrentState != QuestState.InProgress)
                return new List<StageTransition>();

            return CurrentStage.Data.GetAllPlayerChoices();
        }

        /// <summary>
        /// Checks if the current stage requires the player to make a choice before progressing.
        /// </summary>
        public bool CurrentStageRequiresChoice =>
            CurrentStage?.Data.RequiresPlayerChoice ?? false;

        /// <summary>
        /// Selects a player choice, triggering the associated transition.
        /// </summary>
        /// <param name="choice">The choice transition to select.</param>
        /// <returns>True if the choice was valid and executed.</returns>
        public bool SelectChoice(StageTransition choice)
        {
            if (choice == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, "Cannot select null choice");
                return false;
            }

            if (CurrentState != QuestState.InProgress)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Quest '{QuestData.DevName}' not in progress");
                return false;
            }

            if (CurrentStage == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"No current stage in '{QuestData.DevName}'");
                return false;
            }

            if (!choice.IsPlayerChoice)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Transition to stage {choice.TargetStageIndex} is not a player choice");
                return false;
            }

            if (!choice.EvaluateConditions())
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Choice '{choice.ChoiceId}' conditions not met");
                return false;
            }

            // Record the decision
            string stageKey = $"stage_{CurrentStageIndex}";
            BranchDecisions[stageKey] = choice.ChoiceId;
            QuestLogger.LogChoice(QuestData.DevName, choice.ChoiceId);

            // Apply world flag modifications (consequences of the choice)
            choice.ApplyWorldFlagModifications();

            // Fire event before transition
            OnChoiceMade.SafeInvoke(this, choice);

            // Execute the transition
            int previousIndex = CurrentStageIndex;
            var targetStage = GetStageByIndex(choice.TargetStageIndex);

            if (targetStage != null)
            {
                TransitionToStage(targetStage);
                OnStageTransition.SafeInvoke(this, new StageTransitionInfo(previousIndex, choice.TargetStageIndex));
                return true;
            }
            else
            {
                QuestLogger.LogVerbose(LogSubsystem.Stage, $"Target stage {choice.TargetStageIndex} not found, completing quest");
                CompleteQuest();
                return true;
            }
        }

        /// <summary>
        /// Selects a player choice by its ID.
        /// </summary>
        /// <param name="choiceId">The choice ID to select.</param>
        /// <returns>True if the choice was found and executed.</returns>
        public bool SelectChoiceById(string choiceId)
        {
            if (string.IsNullOrEmpty(choiceId))
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, "Cannot select choice with null/empty ID");
                return false;
            }

            if (CurrentStage == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"No current stage in '{QuestData.DevName}'");
                return false;
            }

            var choice = CurrentStage.Data.GetPlayerChoiceById(choiceId);
            if (choice == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Choice '{choiceId}' not found");
                return false;
            }

            return SelectChoice(choice);
        }

        /// <summary>
        /// Checks if a specific choice is currently available.
        /// </summary>
        /// <param name="choiceId">The choice ID to check.</param>
        /// <returns>True if the choice exists and its conditions are met.</returns>
        public bool IsChoiceAvailable(string choiceId)
        {
            if (CurrentStage == null) return false;
            return CurrentStage.Data.IsChoiceAvailable(choiceId);
        }

        /// <summary>
        /// Fires the OnChoicesAvailable event for the current stage if it has player choices.
        /// Called when a stage with choices is entered.
        /// </summary>
        private void NotifyChoicesAvailable()
        {
            if (CurrentStage == null) return;

            var choices = CurrentStage.Data.GetAllPlayerChoices();
            if (choices.Count > 0)
            {
                QuestLogger.Log(LogSubsystem.Choice, $"<b>{choices.Count}</b> choices available in <b>'{CurrentStage.StageName}'</b>");
                OnChoicesAvailable.SafeInvoke(this, choices);
            }
        }

        /// <summary>
        /// Subscribes to player choice conditions for implicit choice detection.
        /// When a choice's conditions become met through game events, the choice is auto-selected.
        /// </summary>
        private void SubscribeToPlayerChoiceConditions()
        {
            if (CurrentStage == null) return;

            var choices = CurrentStage.Data.GetAllPlayerChoices();
            foreach (var choice in choices)
            {
                if (choice.Conditions == null) continue;

                foreach (var condition in choice.Conditions)
                {
                    if (condition is IConditionEventDriven eventDriven)
                    {
                        eventDriven.SubscribeToEvent(() => HandleImplicitChoiceConditionMet(choice));
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes from player choice conditions.
        /// </summary>
        private void UnsubscribeFromPlayerChoiceConditions()
        {
            if (CurrentStage == null) return;

            var choices = CurrentStage.Data.GetAllPlayerChoices();
            foreach (var choice in choices)
            {
                if (choice.Conditions == null) continue;

                foreach (var condition in choice.Conditions)
                {
                    if (condition is IConditionEventDriven eventDriven)
                    {
                        eventDriven.UnsubscribeFromEvent();
                    }
                }
            }
        }

        /// <summary>
        /// Called when a player choice's condition is met via event.
        /// If the choice is now available, it's implicitly selected.
        /// </summary>
        private void HandleImplicitChoiceConditionMet(StageTransition choice)
        {
            if (CurrentState != QuestState.InProgress) return;
            if (CurrentStage == null) return;

            // Check if this choice is now fully available
            if (choice.EvaluateConditions())
            {
                // Fire availability changed event
                OnChoiceAvailabilityChanged.SafeInvoke(this, choice, true);

                // Auto-select if this is an implicit choice (conditions met = choice made)
                // Only auto-select if there's no UI presentation expected (i.e., the choice was made through actions)
                var implicitChoice = CurrentStage.Data.GetImplicitlySelectedChoice();
                if (implicitChoice == choice)
                {
                    QuestLogger.LogVerbose(LogSubsystem.Choice, $"Implicit choice '{choice.ChoiceId}' triggered");
                    SelectChoice(choice);
                }
            }
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToAllEvents()
        {
            // Subscribe to stage events
            foreach (var stage in Stages)
            {
                stage.OnStageEntered.SafeSubscribe(HandleStageEntered);
                stage.OnStageCompleted.SafeSubscribe(HandleStageCompleted);
                stage.OnStageFailed.SafeSubscribe(HandleStageFailed);
                stage.OnTransitionReady.SafeSubscribe(HandleTransitionReady);
                stage.OnTaskInStageUpdated.SafeSubscribe(HandleTaskInStageUpdated);
                stage.OnGroupInStageStarted.SafeSubscribe(HandleGroupInStageStarted);
                stage.OnGroupInStageCompleted.SafeSubscribe(HandleGroupInStageCompleted);
                stage.OnGroupInStageFailed.SafeSubscribe(HandleGroupInStageFailed);
            }

            // Subscribe to global task failure conditions
            if (QuestData.GlobalTaskFailureConditions != null)
            {
                foreach (Condition_SO condition in QuestData.GlobalTaskFailureConditions)
                {
                    if (condition is IConditionEventDriven conditionEventDriven)
                    {
                        conditionEventDriven.SubscribeToEvent(HandleGlobalTaskFailure);
                    }
                }
            }
        }

        private void UnsubscribeFromAllEvents()
        {
            // Unsubscribe from stage events
            foreach (var stage in Stages)
            {
                stage.OnStageEntered.SafeUnsubscribe(HandleStageEntered);
                stage.OnStageCompleted.SafeUnsubscribe(HandleStageCompleted);
                stage.OnStageFailed.SafeUnsubscribe(HandleStageFailed);
                stage.OnTransitionReady.SafeUnsubscribe(HandleTransitionReady);
                stage.OnTaskInStageUpdated.SafeUnsubscribe(HandleTaskInStageUpdated);
                stage.OnGroupInStageStarted.SafeUnsubscribe(HandleGroupInStageStarted);
                stage.OnGroupInStageCompleted.SafeUnsubscribe(HandleGroupInStageCompleted);
                stage.OnGroupInStageFailed.SafeUnsubscribe(HandleGroupInStageFailed);
            }

            // Unsubscribe from global task failure conditions
            if (QuestData.GlobalTaskFailureConditions != null)
            {
                foreach (Condition_SO condition in QuestData.GlobalTaskFailureConditions)
                {
                    if (condition is IConditionEventDriven conditionEventDriven)
                    {
                        conditionEventDriven.UnsubscribeFromEvent();
                    }
                }
            }

            // Unsubscribe from player choice conditions
            UnsubscribeFromPlayerChoiceConditions();
        }

        private void UnsubscribeFromStartConditions()
        {
            if (QuestData.StartConditions == null) return;

            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.UnsubscribeFromEvent();
            }
        }

        public void SubscribeToStartQuestEvents()
        {
            if (QuestData.StartConditions == null) return;

            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.SubscribeToEvent(TryStartQuestIfConditionsMet);
            }
        }

        #endregion

        #region Stage Event Handlers

        private void HandleStageEntered(QuestStageRuntime stage)
        {
            // Logged by stage itself
        }

        private void HandleStageCompleted(QuestStageRuntime stage)
        {
            // Check if all stages are completed (for terminal stages)
            if (stage.Data.IsTerminal)
            {
                CompleteQuest();
            }
        }

        private void HandleStageFailed(QuestStageRuntime stage)
        {
            FailQuest();
        }

        private void HandleTransitionReady(QuestStageRuntime stage, int targetStageIndex)
        {
            var targetStage = GetStageByIndex(targetStageIndex);
            if (targetStage != null)
            {
                int previousIndex = CurrentStageIndex;
                TransitionToStage(targetStage);
                OnStageTransition.SafeInvoke(this, new StageTransitionInfo(previousIndex, targetStageIndex));
            }
            else
            {
                QuestLogger.LogVerbose(LogSubsystem.Stage, $"Target stage {targetStageIndex} not found, completing quest");
                CompleteQuest();
            }
        }

        private void HandleTaskInStageUpdated(QuestStageRuntime stage, TaskRuntime task)
        {
            OnAnyTaskUpdated.SafeInvoke(this, task);
            NotifyQuestUpdated();
        }

        private void HandleGroupInStageStarted(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            // Subscribe to task events for this group
            foreach (var task in group.Tasks)
            {
                task.OnTaskStarted.SafeSubscribe(t => OnAnyTaskStarted.SafeInvoke(this, t));
                task.OnTaskCompleted.SafeSubscribe(t => OnAnyTaskCompleted.SafeInvoke(this, t));
                task.OnTaskFailed.SafeSubscribe(t => OnAnyTaskFailed.SafeInvoke(this, t));
            }
        }

        private void HandleGroupInStageCompleted(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            NotifyQuestUpdated();
        }

        private void HandleGroupInStageFailed(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            // Logged by group itself
        }

        #endregion

        #region Other Event Handlers

        private void HandleGlobalTaskFailure()
        {
            var currentTasks = CurrentTasks;
            if (currentTasks.Count > 0)
            {
                foreach (var task in currentTasks)
                {
                    task.FailTask();
                }
            }
        }

        private void TryStartQuestIfConditionsMet()
        {
            if (CurrentState != QuestState.NotStarted) return;

            if (CheckStartConditions())
            {
                StartQuest();
            }
        }

        #endregion

        /// <summary>
        /// Single point for firing OnQuestUpdated to prevent double-fires.
        /// </summary>
        private void NotifyQuestUpdated()
        {
            OnQuestUpdated.SafeInvoke(this);
        }

        #region Condition Checking

        public bool CheckForConditionsAndStart()
        {
            if (CheckStartConditions())
            {
                StartQuest();
                return true;
            }
            return false;
        }

        public bool CheckStartConditions()
        {
            if (QuestData.StartConditions == null || QuestData.StartConditions.Count == 0)
                return true;

            return QuestData.StartConditions.All(c => c != null && c.Evaluate());
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Increments the current task's step. No-op if no task is in progress.
        /// </summary>
        public void IncrementCurrentTask() => CurrentTask?.IncrementStep();

        /// <summary>
        /// Decrements the current task's step. No-op if no task is in progress.
        /// </summary>
        public void DecrementCurrentTask() => CurrentTask?.DecrementStep();

        /// <summary>
        /// Force completes all remaining tasks and the quest.
        /// Useful for debugging or skip functionality.
        /// </summary>
        public void ForceComplete()
        {
            foreach (var task in Tasks)
            {
                if (task.CurrentState != TaskState.Completed)
                {
                    task.CompleteTask();
                }
            }
        }

        #endregion

        #region Event Subscription Helpers

        /// <summary>
        /// Subscribes a single handler to all quest lifecycle events (Started, Completed, Failed, Restarted, Updated).
        /// Reduces boilerplate when you need to respond to any quest state change.
        /// </summary>
        /// <param name="handler">Handler that receives the quest for any lifecycle event.</param>
        public void SubscribeToLifecycleEvents(UnityAction<QuestRuntime> handler)
        {
            OnQuestStarted.SafeSubscribe(handler);
            OnQuestCompleted.SafeSubscribe(handler);
            OnQuestFailed.SafeSubscribe(handler);
            OnQuestRestarted.SafeSubscribe(handler);
            OnQuestUpdated.SafeSubscribe(handler);
        }

        /// <summary>
        /// Unsubscribes a handler from all quest lifecycle events.
        /// </summary>
        /// <param name="handler">Handler to unsubscribe.</param>
        public void UnsubscribeFromLifecycleEvents(UnityAction<QuestRuntime> handler)
        {
            OnQuestStarted.SafeUnsubscribe(handler);
            OnQuestCompleted.SafeUnsubscribe(handler);
            OnQuestFailed.SafeUnsubscribe(handler);
            OnQuestRestarted.SafeUnsubscribe(handler);
            OnQuestUpdated.SafeUnsubscribe(handler);
        }

        /// <summary>
        /// Subscribes a single handler to all task events (Started, Updated, Completed, Failed).
        /// Reduces boilerplate when you need to respond to any task change.
        /// </summary>
        /// <param name="handler">Handler that receives the quest and task for any task event.</param>
        public void SubscribeToTaskEvents(UnityAction<QuestRuntime, TaskRuntime> handler)
        {
            OnAnyTaskStarted.SafeSubscribe(handler);
            OnAnyTaskUpdated.SafeSubscribe(handler);
            OnAnyTaskCompleted.SafeSubscribe(handler);
            OnAnyTaskFailed.SafeSubscribe(handler);
        }

        /// <summary>
        /// Unsubscribes a handler from all task events.
        /// </summary>
        /// <param name="handler">Handler to unsubscribe.</param>
        public void UnsubscribeFromTaskEvents(UnityAction<QuestRuntime, TaskRuntime> handler)
        {
            OnAnyTaskStarted.SafeUnsubscribe(handler);
            OnAnyTaskUpdated.SafeUnsubscribe(handler);
            OnAnyTaskCompleted.SafeUnsubscribe(handler);
            OnAnyTaskFailed.SafeUnsubscribe(handler);
        }

        /// <summary>
        /// Subscribes a single handler to all stage events (Entered, Completed, Transition).
        /// </summary>
        /// <param name="stageHandler">Handler for stage entered/completed events.</param>
        /// <param name="transitionHandler">Handler for stage transition events.</param>
        public void SubscribeToStageEvents(
            UnityAction<QuestRuntime, QuestStageRuntime> stageHandler,
            UnityAction<QuestRuntime, StageTransitionInfo> transitionHandler)
        {
            if (stageHandler != null)
            {
                OnStageEntered.SafeSubscribe(stageHandler);
                OnStageCompleted.SafeSubscribe(stageHandler);
            }
            if (transitionHandler != null)
            {
                OnStageTransition.SafeSubscribe(transitionHandler);
            }
        }

        /// <summary>
        /// Unsubscribes handlers from all stage events.
        /// </summary>
        public void UnsubscribeFromStageEvents(
            UnityAction<QuestRuntime, QuestStageRuntime> stageHandler,
            UnityAction<QuestRuntime, StageTransitionInfo> transitionHandler)
        {
            if (stageHandler != null)
            {
                OnStageEntered.SafeUnsubscribe(stageHandler);
                OnStageCompleted.SafeUnsubscribe(stageHandler);
            }
            if (transitionHandler != null)
            {
                OnStageTransition.SafeUnsubscribe(transitionHandler);
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is QuestRuntime other)
            {
                return QuestId == other.QuestId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return QuestId.GetHashCode();
        }

        #endregion
    }
}
