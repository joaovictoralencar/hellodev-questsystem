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
    /// Represents a runtime quest instance. Provides core structure and
    /// state management for all quests. Supports both legacy (flat task groups) and
    /// stage-based quests (Skyrim‑style multi‑phase).
    /// Implements <see cref="IQuest"/> for testability and dependency injection.
    /// Implements <see cref="IMission"/> for unified objective system compatibility.
    /// </summary>
    public class QuestRuntime : IQuest
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

        /// <summary>Fired when quest progress changes.</summary>
        public UnityEvent<QuestRuntime> OnQuestUpdated = new();

        #endregion

        #region Events - Tasks

        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskStarted = new();
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskUpdated = new();
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskCompleted = new();
        public UnityEvent<QuestRuntime, TaskRuntime> OnAnyTaskFailed = new();

        #endregion

        #region Events - Stages

        public UnityEvent<QuestRuntime, QuestStageRuntime> OnQuestStageEntered = new();
        public UnityEvent<QuestRuntime, QuestStageRuntime> OnQuestStageCompleted = new();
        public UnityEvent<QuestRuntime, StageTransitionInfo> OnStageTransition = new();

        #endregion

        #region Events - Player Choices

        public UnityEvent<QuestRuntime, List<StageTransition>> OnChoicesAvailable = new();
        public UnityEvent<QuestRuntime, StageTransition> OnChoiceMade = new();
        public UnityEvent<QuestRuntime, StageTransition, bool> OnChoiceAvailabilityChanged = new();

        #endregion

        #region Events - Mission (IMission Action events)

        private event Action<IMission> _onStarted;
        private event Action<IMission> _onProgressChanged;
        private event Action<IMission> _onCompleted;
        private event Action<IMission> _onFailed;
        private event Action<IMission, IStage> _onStageEntered;
        private event Action<IMission, IStage> _onStageCompleted;

        public event Action<IMission> OnStarted
        {
            add => _onStarted += value;
            remove => _onStarted -= value;
        }

        public event Action<IMission> OnProgressChanged
        {
            add => _onProgressChanged += value;
            remove => _onProgressChanged -= value;
        }

        public event Action<IMission> OnCompleted
        {
            add => _onCompleted += value;
            remove => _onCompleted -= value;
        }

        public event Action<IMission> OnFailed
        {
            add => _onFailed += value;
            remove => _onFailed -= value;
        }

        public event Action<IMission, IStage> OnStageEntered
        {
            add => _onStageEntered += value;
            remove => _onStageEntered -= value;
        }

        public event Action<IMission, IStage> OnStageCompleted
        {
            add => _onStageCompleted += value;
            remove => _onStageCompleted -= value;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public Guid MissionId { get; }

        /// <inheritdoc />
        public string DisplayName => QuestData.DisplayName?.GetLocalizedString() ?? QuestData.DevName;

        /// <inheritdoc />
        public State State { get; private set; }

        /// <summary>
        /// Gets the ScriptableObject data for this quest.
        /// </summary>
        public Quest_SO QuestData { get; }

        /// <inheritdoc />
        public IReadOnlyList<IStage> Stages { get; }

        /// <summary>
        /// Gets all concrete quest stages in this quest.
        /// </summary>
        public IReadOnlyList<QuestStageRuntime> QuestStages { get; }

        /// <inheritdoc />
        public IStage CurrentStage => CurrentQuestStage;

        /// <summary>
        /// Gets the currently active quest stage, or null if quest is not in progress.
        /// </summary>
        public QuestStageRuntime CurrentQuestStage { get; private set; }

        /// <inheritdoc />
        public int CurrentStageIndex => CurrentQuestStage?.StageIndex ?? -1;

        /// <inheritdoc />
        public IReadOnlyList<TaskGroupRuntime> TaskGroups =>
            QuestStages.SelectMany(s => s.TaskGroups).ToList();

        /// <inheritdoc />
        public TaskGroupRuntime CurrentGroup => CurrentQuestStage?.CurrentGroup;

        /// <inheritdoc />
        public IReadOnlyList<TaskRuntime> CurrentTasks =>
            CurrentQuestStage?.CurrentTasks ?? Array.Empty<TaskRuntime>();

        /// <inheritdoc />
        public TaskRuntime CurrentTask => CurrentTasks.FirstOrDefault();

        /// <inheritdoc />
        public IReadOnlyList<TaskRuntime> Tasks =>
            QuestStages.SelectMany(s => s.AllTasks).ToList();

        /// <inheritdoc />
        public float Progress
        {
            get
            {
                if (QuestStages.Count == 0) return State == State.Completed ? 1f : 0f;
                float totalProgress = 0f;
                int totalTaskCount = 0;
                foreach (var stage in QuestStages)
                {
                    int stageTaskCount = stage.AllTasks.Count;
                    totalProgress += stage.Progress * stageTaskCount;
                    totalTaskCount += stageTaskCount;
                }

                return totalTaskCount > 0 ? totalProgress / totalTaskCount : 1f;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, string> BranchDecisions { get; } = new();

        /// <inheritdoc />
        public bool CurrentStageRequiresChoice =>
            CurrentQuestStage?.Data.RequiresPlayerChoice ?? false;

        private bool _blockAutoStart;
        private readonly HashSet<TaskRuntime> _subscribedTasks = new();
        private bool _isTransitioningStage;
        public bool IsTransitioningStage => _isTransitioningStage;

        private readonly List<(IConditionEventDriven Condition, System.Action Callback)> _playerChoiceSubscriptions = new();
        private readonly Dictionary<string, bool> _choiceAvailabilityCache = new();

        #endregion

        public QuestRuntime(Quest_SO questData)
        {
            QuestData = questData;
            MissionId = questData.QuestId;
            State = State.NotStarted;

            QuestStages = questData.Stages
                .Select(stageData => new QuestStageRuntime(stageData))
                .ToList();
            Stages = QuestStages.Cast<IStage>().ToList();
        }

        // ---------------------------------------------------------------
        //  IMission / IQuest Lifecycle (the only lifecycle entry points)
        // ---------------------------------------------------------------

        public void Start()
        {
            if (State != State.NotStarted)
            {
                QuestLogger.LogVerbose(LogSubsystem.Quest, $"'{QuestData.DevName}' already in progress");
                return;
            }

            UnsubscribeFromStartConditions();
            SubscribeToAllEvents();
            State = State.InProgress;

            var firstStage = GetStageByIndex(GetFirstStageIndex());
            if (firstStage != null)
            {
                QuestLogger.LogStart(LogSubsystem.Quest, "Quest", QuestData.DevName);
                OnQuestStarted.SafeInvoke(this);
                _onStarted?.Invoke(this);
                TransitionToStage(firstStage);
            }
            else
            {
                QuestLogger.LogWarning(LogSubsystem.Quest,
                    $"Quest '{QuestData.DevName}' has no stages, auto-completing");
                OnQuestStarted.SafeInvoke(this);
                _onStarted?.Invoke(this);
                Complete();
            }
        }

        public void Complete()
        {
            if (State == State.InProgress)
            {
                UnsubscribeFromAllEvents();

                if (QuestData.Rewards != null)
                {
                    foreach (var reward in QuestData.Rewards)
                    {
                        if (reward.RewardType != null && reward.Amount > 0)
                        {
                            reward.RewardType.GiveReward(reward.Amount);
                            QuestLogger.LogVerbose(LogSubsystem.Quest,
                                $"Reward: {reward.RewardType.name} x{reward.Amount}");
                        }
                    }
                }

                NotifyQuestUpdated();
                State = State.Completed;
                QuestLogger.LogComplete(LogSubsystem.Quest, "Quest", QuestData.DevName);
                OnQuestCompleted.SafeInvoke(this);
                _onCompleted?.Invoke(this);
            }
        }

        public void Fail()
        {
            if (State == State.InProgress)
            {
                State = State.Failed;
                UnsubscribeFromAllEvents();
                QuestLogger.LogFail(LogSubsystem.Quest, "Quest", QuestData.DevName);
                OnQuestFailed.SafeInvoke(this);
                _onFailed?.Invoke(this);
            }
        }

        public void Reset()
        {
            UnsubscribeFromAllEvents();
            _subscribedTasks.Clear();

            foreach (var stage in QuestStages)
                stage.Reset();

            CurrentQuestStage = null;
            BranchDecisions.Clear();
            State = State.NotStarted;
            Start();
            OnQuestRestarted.SafeInvoke(this);
        }

        #region Save / Load

        public void RestoreQuestState(State state, int stageIndex)
        {
            State = state;
            CurrentQuestStage = stageIndex >= 0 ? GetStageByIndex(stageIndex) : null;
        }

        public void ResumeQuest()
        {
            if (State != State.InProgress) return;

            UnsubscribeFromStartConditions();
            SubscribeToAllEvents();

            CurrentQuestStage?.ResumeStage();
            if (CurrentQuestStage != null)
                QuestManager.Instance?.NotifyStageEntered(this, CurrentQuestStage);

            foreach (var stage in QuestStages)
            {
                foreach (var group in stage.TaskGroups)
                {
                    if (group.CurrentState == TaskGroupState.InProgress)
                    {
                        HandleGroupInStageStarted(stage, group);
                        group.ResumeGroup();
                    }

                    foreach (var task in group.Tasks)
                        task.ResumeTask();
                }
            }

            if (CurrentQuestStage?.Data.HasPlayerChoices == true)
                SubscribeToPlayerChoiceConditions();

            QuestLogger.LogVerbose(LogSubsystem.Quest, $"Quest '{QuestData.DevName}' resumed from save");
        }

        #endregion

        #region Stage Management

        public bool TrySetStage(int stageIndex)
        {
            if (State != State.InProgress)
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

        public QuestStageRuntime GetStageByIndex(int stageIndex)
        {
            return QuestStages.FirstOrDefault(s => s.StageIndex == stageIndex);
        }

        private int GetFirstStageIndex()
        {
            return QuestStages.Count > 0 ? QuestStages.Min(s => s.StageIndex) : -1;
        }

        private void TransitionToStage(QuestStageRuntime targetStage)
        {
            _isTransitioningStage = true;
            try
            {
                UnsubscribeFromPlayerChoiceConditions();

                if (CurrentQuestStage?.CurrentState == StageState.InProgress)
                {
                    QuestManager.Instance?.NotifyStageExited(this, CurrentQuestStage);
                    CurrentQuestStage.Complete();
                    OnQuestStageCompleted.SafeInvoke(this, CurrentQuestStage);
                    _onStageCompleted?.Invoke(this, CurrentQuestStage);
                }

                CurrentQuestStage = targetStage;
                targetStage.Enter();

                QuestManager.Instance?.NotifyStageEntered(this, targetStage);
                OnQuestStageEntered.SafeInvoke(this, targetStage);
                _onStageEntered?.Invoke(this, targetStage);
                NotifyQuestUpdated();

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

        public List<StageTransition> GetAvailableChoices()
        {
            if (CurrentQuestStage == null || State != State.InProgress)
                return new List<StageTransition>();
            return CurrentQuestStage.Data.GetAvailablePlayerChoices();
        }

        public List<StageTransition> GetAllChoices()
        {
            if (CurrentQuestStage == null || State != State.InProgress)
                return new List<StageTransition>();
            return CurrentQuestStage.Data.GetAllPlayerChoices();
        }

        public bool SelectChoice(StageTransition choice, bool bypassConditions = false)
        {
            if (choice == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, "Cannot select null choice");
                return false;
            }

            if (State != State.InProgress)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Quest '{QuestData.DevName}' not in progress");
                return false;
            }

            if (CurrentQuestStage == null)
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

            string stageKey = $"stage_{CurrentStageIndex}";
            BranchDecisions[stageKey] = choice.ChoiceId;

            QuestLogger.Log(LogSubsystem.Choice,
                $"Choice <b>'{choice.ChoiceId}'</b> selected in quest <b>'{QuestData.DevName}'</b>");

            foreach (var task in CurrentTasks)
            {
                if (task.State == State.InProgress)
                {
                    QuestLogger.Log(LogSubsystem.Task, $"Completing decision task '{task.DevName}' due to player choice");
                    task.Complete();
                }
            }

            choice.ApplyEffects();
            OnChoiceMade.SafeInvoke(this, choice);

            int previousIndex = CurrentStageIndex;
            var targetStage = GetStageByIndex(choice.TargetStageIndex);

            if (targetStage != null)
            {
                TransitionToStage(targetStage);
                OnStageTransition.SafeInvoke(this, new StageTransitionInfo(previousIndex, choice.TargetStageIndex));
            }
            else
            {
                QuestLogger.LogVerbose(LogSubsystem.Stage, $"Target stage {choice.TargetStageIndex} not found, completing quest");
                Complete();
            }

            return true;
        }

        public bool SelectChoiceById(string choiceId)
        {
            if (string.IsNullOrEmpty(choiceId))
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, "Cannot select choice with null/empty ID");
                return false;
            }

            if (CurrentQuestStage == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"No current stage in '{QuestData.DevName}'");
                return false;
            }

            var choice = CurrentQuestStage.Data.GetPlayerChoiceById(choiceId);
            if (choice == null)
            {
                QuestLogger.LogWarning(LogSubsystem.Choice, $"Choice '{choiceId}' not found");
                return false;
            }

            return SelectChoice(choice);
        }

        public bool IsChoiceAvailable(string choiceId)
        {
            if (CurrentQuestStage == null) return false;
            return CurrentQuestStage.Data.IsChoiceAvailable(choiceId);
        }

        private void NotifyChoicesAvailable()
        {
            if (CurrentQuestStage == null) return;
            var choices = CurrentQuestStage.Data.GetAllPlayerChoices();
            if (choices.Count > 0)
            {
                QuestLogger.Log(LogSubsystem.Choice,
                    $"<b>{choices.Count}</b> choices available in <b>'{CurrentQuestStage.StageName}'</b>");
                OnChoicesAvailable.SafeInvoke(this, choices);
            }
        }

        private void SubscribeToPlayerChoiceConditions()
        {
            if (CurrentQuestStage == null) return;
            var choices = CurrentQuestStage.Data.GetAllPlayerChoices();

            _choiceAvailabilityCache.Clear();
            foreach (var choice in choices)
            {
                if (!string.IsNullOrEmpty(choice.ChoiceId))
                    _choiceAvailabilityCache[choice.ChoiceId] = choice.EvaluateConditions();
            }

            foreach (var choice in choices)
            {
                if (choice.Conditions == null) continue;
                foreach (var condition in choice.Conditions)
                {
                    if (condition is IConditionEventDriven eventDriven)
                    {
                        System.Action callback = () => HandleChoiceConditionChanged();
                        eventDriven.SubscribeToEvent(callback);
                        _playerChoiceSubscriptions.Add((eventDriven, callback));
                    }
                }
            }
        }

        private void UnsubscribeFromPlayerChoiceConditions()
        {
            foreach (var (condition, callback) in _playerChoiceSubscriptions)
                condition.UnsubscribeFromEvent(callback);
            _playerChoiceSubscriptions.Clear();
            _choiceAvailabilityCache.Clear();
        }

        private void HandleChoiceConditionChanged()
        {
            if (State != State.InProgress || CurrentQuestStage == null) return;

            var choices = CurrentQuestStage.Data.GetAllPlayerChoices();
            StageTransition newlyAvailableImplicitChoice = null;

            foreach (var choice in choices)
            {
                if (string.IsNullOrEmpty(choice.ChoiceId)) continue;

                bool currentlyAvailable = choice.EvaluateConditions();
                bool wasAvailable = _choiceAvailabilityCache.TryGetValue(choice.ChoiceId, out var cached) && cached;

                if (currentlyAvailable != wasAvailable)
                {
                    _choiceAvailabilityCache[choice.ChoiceId] = currentlyAvailable;
                    OnChoiceAvailabilityChanged.SafeInvoke(this, choice, currentlyAvailable);

                    QuestLogger.LogVerbose(LogSubsystem.Choice,
                        $"Choice '{choice.ChoiceId}' availability changed: {wasAvailable} → {currentlyAvailable}");

                    if (currentlyAvailable)
                    {
                        var implicitChoice = CurrentQuestStage.Data.GetImplicitlySelectedChoice();
                        if (implicitChoice == choice)
                            newlyAvailableImplicitChoice = choice;
                    }
                }
            }

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
            foreach (var stage in QuestStages)
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

            if (QuestData.GlobalTaskFailureConditions != null)
            {
                foreach (Condition_SO condition in QuestData.GlobalTaskFailureConditions)
                {
                    if (condition is IConditionEventDriven conditionEventDriven)
                        conditionEventDriven.SubscribeToEvent(HandleGlobalTaskFailure);
                }
            }
        }

        private void UnsubscribeFromAllEvents()
        {
            foreach (var stage in QuestStages)
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

            if (QuestData.GlobalTaskFailureConditions != null)
            {
                foreach (Condition_SO condition in QuestData.GlobalTaskFailureConditions)
                {
                    if (condition is IConditionEventDriven conditionEventDriven)
                        conditionEventDriven.UnsubscribeFromEvent(HandleGlobalTaskFailure);
                }
            }

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

        public void SubscribeToStartQuestEvents(bool blockAutoStart = false)
        {
            _blockAutoStart = blockAutoStart;
            if (QuestData.StartConditions == null) return;
            foreach (Condition_SO condition in QuestData.StartConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.SubscribeToEvent(TryStartQuestIfConditionsMet);
            }
        }

        public void UnblockAutoStart()
        {
            _blockAutoStart = false;
        }

        #endregion

        #region Stage Event Handlers

        private void HandleStageEntered(QuestStageRuntime stage)
        {
        }

        private void HandleStageCompleted(QuestStageRuntime stage)
        {
            if (stage.Data.IsTerminal)
            {
                QuestManager.Instance?.NotifyStageExited(this, stage);
                Complete();
            }
        }

        private void HandleStageFailed(QuestStageRuntime stage)
        {
            QuestManager.Instance?.NotifyStageExited(this, stage);
            Fail();
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
                Complete();
            }
        }

        private void HandleTaskInStageUpdated(QuestStageRuntime stage, TaskRuntime task)
        {
            OnAnyTaskUpdated.SafeInvoke(this, task);
            NotifyQuestUpdated();
        }

        private void HandleGroupInStageStarted(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            foreach (var task in group.Tasks)
            {
                if (_subscribedTasks.Contains(task))
                    continue;

                task.Started.SafeSubscribe(t => OnAnyTaskStarted.SafeInvoke(this, t as TaskRuntime));
                task.Completed.SafeSubscribe(t => OnAnyTaskCompleted.SafeInvoke(this, t as TaskRuntime));
                task.Failed.SafeSubscribe(t => OnAnyTaskFailed.SafeInvoke(this, t as TaskRuntime));
                _subscribedTasks.Add(task);
            }
        }

        private void HandleGroupInStageCompleted(QuestStageRuntime stage, TaskGroupRuntime group)
        {
            NotifyQuestUpdated();
        }

        private void HandleGroupInStageFailed(QuestStageRuntime stage, TaskGroupRuntime group)
        {
        }

        #endregion

        #region Other Handlers

        private void HandleGlobalTaskFailure()
        {
            foreach (var task in CurrentTasks)
                task.Fail();
        }

        private void TryStartQuestIfConditionsMet()
        {
            if (_blockAutoStart) return;
            if (State != State.NotStarted) return;
            if (CheckStartConditions())
            {
                QuestLogger.Log(LogSubsystem.Quest, $"Chain trigger starting quest <b>'{QuestData.DevName}'</b>");
                Start();
            }
        }

        #endregion

        private void NotifyQuestUpdated()
        {
            OnQuestUpdated.SafeInvoke(this);
            _onProgressChanged?.Invoke(this);
        }

        #region Condition Checking

        public bool CheckForConditionsAndStart()
        {
            if (CheckStartConditions())
            {
                Start();
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

        public void IncrementCurrentTask() => CurrentTask?.IncrementStep();
        public void DecrementCurrentTask() => CurrentTask?.DecrementStep();

        public void ForceComplete()
        {
            foreach (var task in Tasks)
            {
                if (task.State != State.Completed)
                    task.Complete();
            }

            if (State == State.InProgress && CurrentQuestStage?.CurrentState == StageState.InProgress)
                QuestManager.Instance?.NotifyStageExited(this, CurrentQuestStage);

            if (State == State.InProgress)
                Complete();
        }

        #endregion

        #region Bulk Subscription Helpers

        public void SubscribeToLifecycleEvents(UnityAction<QuestRuntime> handler)
        {
            OnQuestStarted.SafeSubscribe(handler);
            OnQuestCompleted.SafeSubscribe(handler);
            OnQuestFailed.SafeSubscribe(handler);
            OnQuestRestarted.SafeSubscribe(handler);
            OnQuestUpdated.SafeSubscribe(handler);
        }

        public void UnsubscribeFromLifecycleEvents(UnityAction<QuestRuntime> handler)
        {
            OnQuestStarted.SafeUnsubscribe(handler);
            OnQuestCompleted.SafeUnsubscribe(handler);
            OnQuestFailed.SafeUnsubscribe(handler);
            OnQuestRestarted.SafeUnsubscribe(handler);
            OnQuestUpdated.SafeUnsubscribe(handler);
        }

        public void SubscribeToTaskEvents(UnityAction<QuestRuntime, TaskRuntime> handler)
        {
            OnAnyTaskStarted.SafeSubscribe(handler);
            OnAnyTaskUpdated.SafeSubscribe(handler);
            OnAnyTaskCompleted.SafeSubscribe(handler);
            OnAnyTaskFailed.SafeSubscribe(handler);
        }

        public void UnsubscribeFromTaskEvents(UnityAction<QuestRuntime, TaskRuntime> handler)
        {
            OnAnyTaskStarted.SafeUnsubscribe(handler);
            OnAnyTaskUpdated.SafeUnsubscribe(handler);
            OnAnyTaskCompleted.SafeUnsubscribe(handler);
            OnAnyTaskFailed.SafeUnsubscribe(handler);
        }

        public void SubscribeToStageEvents(
            UnityAction<QuestRuntime, QuestStageRuntime> stageHandler,
            UnityAction<QuestRuntime, StageTransitionInfo> transitionHandler)
        {
            if (stageHandler != null)
            {
                OnQuestStageEntered.SafeSubscribe(stageHandler);
                OnQuestStageCompleted.SafeSubscribe(stageHandler);
            }

            if (transitionHandler != null)
                OnStageTransition.SafeSubscribe(transitionHandler);
        }

        public void UnsubscribeFromStageEvents(
            UnityAction<QuestRuntime, QuestStageRuntime> stageHandler,
            UnityAction<QuestRuntime, StageTransitionInfo> transitionHandler)
        {
            if (stageHandler != null)
            {
                OnQuestStageEntered.SafeUnsubscribe(stageHandler);
                OnQuestStageCompleted.SafeUnsubscribe(stageHandler);
            }

            if (transitionHandler != null)
                OnStageTransition.SafeUnsubscribe(transitionHandler);
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is QuestRuntime other)
                return MissionId == other.MissionId;
            return false;
        }

        public override int GetHashCode()
        {
            return MissionId.GetHashCode();
        }

        #endregion
    }
}