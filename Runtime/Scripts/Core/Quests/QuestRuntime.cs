using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Interfaces;
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
    /// Implements <see cref="IQuest"/> for testability and dependency injection.
    /// Implements <see cref="IMission"/> for unified objective system compatibility.
    /// </summary>
    public class QuestRuntime : IQuest, IMission
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
        public IReadOnlyList<QuestStageRuntime> Stages { get; }

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
        public IReadOnlyList<TaskGroupRuntime> TaskGroups => Stages.SelectMany(s => s.TaskGroups).ToList();

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
        public IReadOnlyList<TaskRuntime> Tasks => Stages.SelectMany(s => s.AllTasks).ToList();

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

        /// <summary>
        /// When true, TryStartQuestIfConditionsMet will not start the quest.
        /// Used during restore to prevent events from triggering auto-start.
        /// </summary>
        private bool _blockAutoStart;

        /// <summary>
        /// Tracks tasks that have been subscribed to, preventing double-subscription
        /// when groups are started multiple times (e.g., after reset).
        /// </summary>
        private readonly HashSet<TaskRuntime> _subscribedTasks = new();

        /// <summary>
        /// True while transitioning between stages. Used to prevent saving during
        /// stage transitions which could capture inconsistent state.
        /// </summary>
        private bool _isTransitioningStage;

        /// <summary>
        /// Returns true if the quest is currently transitioning between stages.
        /// Save operations should check this and defer if true.
        /// </summary>
        public bool IsTransitioningStage => _isTransitioningStage;

        /// <summary>
        /// Stores player choice condition subscriptions for proper cleanup.
        /// </summary>
        private readonly List<(IConditionEventDriven Condition, System.Action Callback)> _playerChoiceSubscriptions = new();

        /// <summary>
        /// Tracks the availability state of each choice to detect changes.
        /// Key is choice ID, value is whether it was available.
        /// </summary>
        private readonly Dictionary<string, bool> _choiceAvailabilityCache = new();

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
                // Log quest start BEFORE starting the stage (correct chronological order)
                QuestLogger.LogStart(LogSubsystem.Quest, "Quest", QuestData.DevName);
                OnQuestStarted.SafeInvoke(this);
                TransitionToStage(firstStage);
            }
            else
            {
                // Quest has no stages - auto-complete immediately
                QuestLogger.LogWarning(LogSubsystem.Quest, $"Quest '{QuestData.DevName}' has no stages, auto-completing");
                OnQuestStarted.SafeInvoke(this);
                CompleteQuest();
            }
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
                        if (reward.RewardType != null && reward.Amount > 0)
                        {
                            reward.RewardType.GiveReward(reward.Amount);
                            QuestLogger.LogVerbose(LogSubsystem.Quest, $"Reward: {reward.RewardType.name} x{reward.Amount}");
                        }
                    }
                }

                OnQuestUpdated.SafeInvoke(this);
                UpdateQuestState(QuestState.Completed);
                QuestLogger.LogComplete(LogSubsystem.Quest, "Quest", QuestData.DevName);
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
                QuestLogger.LogFail(LogSubsystem.Quest, "Quest", QuestData.DevName);
                OnQuestFailed.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Resets the quest to its initial state and restarts it.
        /// </summary>
        public void ResetQuest()
        {
            UnsubscribeFromAllEvents();

            // Clear subscribed tasks tracking so fresh subscriptions happen on restart
            _subscribedTasks.Clear();

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

        #region Save/Load Restoration

        /// <summary>
        /// Directly sets the quest state and current stage without triggering events or side effects.
        /// Used during save/load restoration.
        /// </summary>
        /// <param name="state">The state to set.</param>
        /// <param name="stageIndex">The current stage index to set (-1 for no stage).</param>
        public void RestoreQuestState(QuestState state, int stageIndex)
        {
            CurrentState = state;
            CurrentStage = stageIndex >= 0 ? GetStageByIndex(stageIndex) : null;
        }

        /// <summary>
        /// Resumes a quest that was restored to InProgress state.
        /// Subscribes to events so the quest can respond to game events.
        /// Call this AFTER all task and stage states have been restored.
        /// </summary>
        public void ResumeQuest()
        {
            if (CurrentState == QuestState.InProgress)
            {
                // Unsubscribe from start conditions (quest already started)
                UnsubscribeFromStartConditions();

                // Subscribe to stage events
                SubscribeToAllEvents();

                // Resume the current stage
                CurrentStage?.ResumeStage();

                // Notify QuestManager of stage enter on resume
                if (CurrentStage != null)
                {
                    QuestManager.Instance?.NotifyStageEntered(this, CurrentStage);
                }

                // Resume all InProgress groups and tasks
                foreach (var stage in Stages)
                {
                    foreach (var group in stage.TaskGroups)
                    {
                        if (group.CurrentState == TaskGroupState.InProgress)
                        {
                            // Set up task event subscriptions for this group
                            // (normally done when group starts, but after load we need to reconnect)
                            HandleGroupInStageStarted(stage, group);
                            group.ResumeGroup();
                        }

                        foreach (var task in group.Tasks)
                        {
                            task.ResumeTask();
                        }
                    }
                }

                // If the current stage has player choices, set up choice handling
                if (CurrentStage?.Data.HasPlayerChoices == true)
                {
                    SubscribeToPlayerChoiceConditions();
                }

                QuestLogger.LogVerbose(LogSubsystem.Quest, $"Quest '{QuestData.DevName}' resumed from save");
            }
        }

        #endregion

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
            // Mark transition in progress to prevent save during this critical section
            _isTransitioningStage = true;
            try
            {
                // Unsubscribe from previous stage's choice conditions
                UnsubscribeFromPlayerChoiceConditions();

                // Complete current stage if it's still in progress
                if (CurrentStage?.CurrentState == StageState.InProgress)
                {
                    // Notify QuestManager of stage exit before completing
                    QuestManager.Instance?.NotifyStageExited(this, CurrentStage);

                    CurrentStage.Complete();
                    OnStageCompleted.SafeInvoke(this, CurrentStage);
                }

                CurrentStage = targetStage;
                targetStage.Enter();

                // Notify QuestManager of stage enter after entering
                QuestManager.Instance?.NotifyStageEntered(this, targetStage);

                OnStageEntered.SafeInvoke(this, targetStage);
                NotifyQuestUpdated();

                // If the new stage has player choices, set up choice handling
                if (targetStage.Data.HasPlayerChoices)
                {
                    SubscribeToPlayerChoiceConditions();
                    NotifyChoicesAvailable();
                }
            }
            finally
            {
                _isTransitioningStage = false;
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
        /// <param name="bypassConditions">If true, skips condition evaluation. Used for UI-based choices (Option B)
        /// where conditions are for gameplay triggers (Option C), not for gating UI availability.</param>
        /// <returns>True if the choice was valid and executed.</returns>
        public bool SelectChoice(StageTransition choice, bool bypassConditions = false)
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

            if (!bypassConditions && !choice.EvaluateConditions())
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Choice '{choice.ChoiceId}' conditions not met");
                return false;
            }

            // Record the decision
            string stageKey = $"stage_{CurrentStageIndex}";
            BranchDecisions[stageKey] = choice.ChoiceId;
            
            QuestLogger.Log(LogSubsystem.Choice, $"Choice <b>'{choice.ChoiceId}'</b> selected in quest <b>'{QuestData.DevName}'</b>");

            // Complete any in-progress tasks in the current stage (decision tasks)
            foreach (var task in CurrentTasks)
            {
                if (task.CurrentState == TaskState.InProgress)
                {
                    QuestLogger.Log(LogSubsystem.Task, $"Completing decision task '{task.DevName}' due to player choice");
                    task.CompleteTask();
                }
            }

            // Apply transition effects (consequences of the choice)
            choice.ApplyEffects();

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

            // Capture initial availability state for all choices
            _choiceAvailabilityCache.Clear();
            foreach (var choice in choices)
            {
                if (!string.IsNullOrEmpty(choice.ChoiceId))
                {
                    _choiceAvailabilityCache[choice.ChoiceId] = choice.EvaluateConditions();
                }
            }

            // Subscribe to conditions
            foreach (var choice in choices)
            {
                if (choice.Conditions == null) continue;

                foreach (var condition in choice.Conditions)
                {
                    if (condition is IConditionEventDriven eventDriven)
                    {
                        // Store callback for proper unsubscription
                        // When any condition fires, re-evaluate ALL choices
                        System.Action callback = () => HandleChoiceConditionChanged();
                        eventDriven.SubscribeToEvent(callback);
                        _playerChoiceSubscriptions.Add((eventDriven, callback));
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes from player choice conditions.
        /// </summary>
        private void UnsubscribeFromPlayerChoiceConditions()
        {
            foreach (var (condition, callback) in _playerChoiceSubscriptions)
            {
                condition.UnsubscribeFromEvent(callback);
            }
            _playerChoiceSubscriptions.Clear();
            _choiceAvailabilityCache.Clear();
        }

        /// <summary>
        /// Called when any player choice condition fires.
        /// Re-evaluates ALL choices and fires OnChoiceAvailabilityChanged for any that changed.
        /// </summary>
        private void HandleChoiceConditionChanged()
        {
            if (CurrentState != QuestState.InProgress) return;
            if (CurrentStage == null) return;

            var choices = CurrentStage.Data.GetAllPlayerChoices();
            StageTransition newlyAvailableImplicitChoice = null;

            foreach (var choice in choices)
            {
                if (string.IsNullOrEmpty(choice.ChoiceId)) continue;

                bool currentlyAvailable = choice.EvaluateConditions();
                bool wasAvailable = _choiceAvailabilityCache.TryGetValue(choice.ChoiceId, out var cached) && cached;

                // Only fire event if availability actually changed
                if (currentlyAvailable != wasAvailable)
                {
                    _choiceAvailabilityCache[choice.ChoiceId] = currentlyAvailable;
                    OnChoiceAvailabilityChanged.SafeInvoke(this, choice, currentlyAvailable);

                    QuestLogger.LogVerbose(LogSubsystem.Choice,
                        $"Choice '{choice.ChoiceId}' availability changed: {wasAvailable} → {currentlyAvailable}");

                    // Track if this newly available choice is an implicit choice
                    if (currentlyAvailable)
                    {
                        var implicitChoice = CurrentStage.Data.GetImplicitlySelectedChoice();
                        if (implicitChoice == choice)
                        {
                            newlyAvailableImplicitChoice = choice;
                        }
                    }
                }
            }

            // Handle implicit choice selection (after all events fired)
            if (newlyAvailableImplicitChoice != null)
            {
                QuestLogger.LogVerbose(LogSubsystem.Choice, $"Implicit choice '{newlyAvailableImplicitChoice.ChoiceId}' triggered");
                SelectChoice(newlyAvailableImplicitChoice);
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
                        conditionEventDriven.UnsubscribeFromEvent(HandleGlobalTaskFailure);
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
                    conditionEventDriven.UnsubscribeFromEvent(TryStartQuestIfConditionsMet);
            }
        }

        /// <summary>
        /// Subscribes to start condition events so the quest can auto-start when conditions are met.
        /// </summary>
        /// <param name="blockAutoStart">If true, prevents auto-start even if conditions are met during subscription.
        /// Used during restore to prevent events from triggering auto-start before restore completes.</param>
        public void SubscribeToStartQuestEvents(bool blockAutoStart = false)
        {
            _blockAutoStart = blockAutoStart;

            if (QuestData.StartConditions == null)
                return;

            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven)
                {
                    conditionEventDriven.SubscribeToEvent(TryStartQuestIfConditionsMet);
                }
            }
        }

        /// <summary>
        /// Clears the auto-start block, allowing future events to trigger quest start.
        /// Call this after restore is complete.
        /// </summary>
        public void UnblockAutoStart()
        {
            _blockAutoStart = false;
        }

        #endregion

        #region Stage Event Handlers

        private void HandleStageEntered(QuestStageRuntime stage)
        {
            // Logged by stage itself
        }

        private void HandleStageCompleted(QuestStageRuntime stage)
        {
            // Notify QuestManager of stage exit (for terminal stages that complete without transition)
            // Note: Non-terminal stages notify exit during TransitionToStage
            if (stage.Data.IsTerminal)
            {
                QuestManager.Instance?.NotifyStageExited(this, stage);
                CompleteQuest();
            }
        }

        private void HandleStageFailed(QuestStageRuntime stage)
        {
            // Notify QuestManager of stage exit
            QuestManager.Instance?.NotifyStageExited(this, stage);
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
            // Track subscribed tasks to prevent double-subscription on group restart
            foreach (var task in group.Tasks)
            {
                if (_subscribedTasks.Contains(task))
                {
                    continue; // Already subscribed
                }

                task.OnTaskStarted.SafeSubscribe(t => OnAnyTaskStarted.SafeInvoke(this, t));
                task.OnTaskCompleted.SafeSubscribe(t => OnAnyTaskCompleted.SafeInvoke(this, t));
                task.OnTaskFailed.SafeSubscribe(t => OnAnyTaskFailed.SafeInvoke(this, t));
                _subscribedTasks.Add(task);
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
            if (_blockAutoStart)
                return;

            if (CurrentState != QuestState.NotStarted)
                return;

            if (CheckStartConditions())
            {
                QuestLogger.Log(LogSubsystem.Quest, $"Chain trigger starting quest <b>'{QuestData.DevName}'</b>");
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

            foreach (var condition in QuestData.StartConditions)
            {
                if (condition == null || !condition.Evaluate())
                    return false;
            }
            return true;
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
        /// Ensures all world flags are applied via CompleteQuest().
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

            // Notify stage exit before completing quest
            if (CurrentState == QuestState.InProgress && CurrentStage?.CurrentState == StageState.InProgress)
            {
                QuestManager.Instance?.NotifyStageExited(this, CurrentStage);
            }

            // Ensure quest is properly completed
            if (CurrentState == QuestState.InProgress)
            {
                CompleteQuest();
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

        #region IMission Explicit Implementation

        /// <summary>
        /// Maps QuestState to ObjectiveState.
        /// </summary>
        private static ObjectiveState MapQuestStateToObjectiveState(QuestState questState)
        {
            return questState switch
            {
                QuestState.NotStarted => ObjectiveState.NotStarted,
                QuestState.InProgress => ObjectiveState.InProgress,
                QuestState.Completed => ObjectiveState.Completed,
                QuestState.Failed => ObjectiveState.Failed,
                _ => ObjectiveState.NotStarted
            };
        }

        // IMission.MissionId => QuestId
        Guid IMission.MissionId => QuestId;

        // IMission.DisplayName => localized display name from data
        string IMission.DisplayName => QuestData.DisplayName?.GetLocalizedString() ?? QuestData.DevName;

        // IMission.State => mapped QuestState
        ObjectiveState IMission.State => MapQuestStateToObjectiveState(CurrentState);

        // IMission.Progress => CurrentProgress
        float IMission.Progress => CurrentProgress;

        // IMission.Stages => cast QuestStageRuntime to IStage
        // Note: Requires QuestStageRuntime to implement IStage for this to return valid results
        IReadOnlyList<IStage> IMission.Stages => Stages.OfType<IStage>().ToList();

        // IMission.CurrentStage => CurrentStage as IStage
        // Note: Requires QuestStageRuntime to implement IStage for this to return non-null
        IStage IMission.CurrentStage => CurrentStage as IStage;

        // IMission.CurrentStageIndex => CurrentStageIndex (already matching)
        int IMission.CurrentStageIndex => CurrentStageIndex;

        // IMission lifecycle methods - delegate to existing methods
        void IMission.Start() => StartQuest();
        void IMission.Complete() => CompleteQuest();
        void IMission.Fail() => FailQuest();
        void IMission.Reset() => ResetQuest();

        // IMission events - backing fields for Action events
        private event Action<IMission> _onMissionStarted;
        private event Action<IMission> _onMissionProgressChanged;
        private event Action<IMission> _onMissionCompleted;
        private event Action<IMission> _onMissionFailed;
        private event Action<IMission, IStage> _onMissionStageEntered;
        private event Action<IMission, IStage> _onMissionStageCompleted;

        event Action<IMission> IMission.OnStarted
        {
            add
            {
                _onMissionStarted += value;
                // Also subscribe to the UnityEvent to forward it
                if (value != null)
                {
                    OnQuestStarted.SafeSubscribe(_ => value(this));
                }
            }
            remove => _onMissionStarted -= value;
        }

        event Action<IMission> IMission.OnProgressChanged
        {
            add
            {
                _onMissionProgressChanged += value;
                if (value != null)
                {
                    OnQuestUpdated.SafeSubscribe(_ => value(this));
                }
            }
            remove => _onMissionProgressChanged -= value;
        }

        event Action<IMission> IMission.OnCompleted
        {
            add
            {
                _onMissionCompleted += value;
                if (value != null)
                {
                    OnQuestCompleted.SafeSubscribe(_ => value(this));
                }
            }
            remove => _onMissionCompleted -= value;
        }

        event Action<IMission> IMission.OnFailed
        {
            add
            {
                _onMissionFailed += value;
                if (value != null)
                {
                    OnQuestFailed.SafeSubscribe(_ => value(this));
                }
            }
            remove => _onMissionFailed -= value;
        }

        event Action<IMission, IStage> IMission.OnStageEntered
        {
            add
            {
                _onMissionStageEntered += value;
                if (value != null)
                {
                    OnStageEntered.SafeSubscribe((_, stage) => value(this, stage as IStage));
                }
            }
            remove => _onMissionStageEntered -= value;
        }

        event Action<IMission, IStage> IMission.OnStageCompleted
        {
            add
            {
                _onMissionStageCompleted += value;
                if (value != null)
                {
                    OnStageCompleted.SafeSubscribe((_, stage) => value(this, stage as IStage));
                }
            }
            remove => _onMissionStageCompleted -= value;
        }

        #endregion
    }
}
