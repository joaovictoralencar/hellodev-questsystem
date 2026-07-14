using System;
using HelloDev.Conditions;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Interfaces;
using HelloDev.QuestSystem.SaveLoad;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// Represents a single objective within a quest. This abstract class provides the
    /// core functionality for all task types. Specific tasks must inherit from it.
    /// Implements <see cref="ITask"/> (which extends <see cref="IObjective"/>).
    /// </summary>
    public abstract class TaskRuntime : ITask
    {
        #region IObjective Events (the only events)

        /// <inheritdoc />
        public UnityEvent<IObjective> Started { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> ProgressChanged { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> Completed { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> Failed { get; set; } = new();

        /// <inheritdoc />
        public UnityEvent<IObjective> Updated { get; set; } = new();

        #endregion

        #region Properties

        /// <inheritdoc />
        public Guid Id { get; }

        /// <inheritdoc />
        public string DevName { get; private set; }

        /// <inheritdoc />
        public LocalizedString DisplayName { get; private set; }

        /// <inheritdoc />
        public LocalizedString Description { get; private set; }

        /// <inheritdoc />
        public State State { get; private set; }

        /// <summary>
        /// The ScriptableObject data that this task was created from.
        /// </summary>
        public Task_SO Data { get; private set; }

        /// <inheritdoc />
        public abstract float Progress { get; }

        /// <inheritdoc />
        public bool IsComplete => State == State.Completed;

        /// <inheritdoc />
        public bool IsFailed => State == State.Failed;

        #endregion

        /// <summary>
        /// Initializes a new task instance from a ScriptableObject.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        protected TaskRuntime(Task_SO data)
        {
            Data = data;
            Id = data.TaskId;
            DevName = data.DevName;
            DisplayName = data.DisplayName;
            Description = data.TaskDescription;
            State = State.NotStarted;
        }

        // ---------------------------------------------------------------
        //  IObjective Lifecycle (the only lifecycle methods)
        // ---------------------------------------------------------------

        /// <inheritdoc />
        public virtual void Start()
        {
            if (State == State.NotStarted)
            {
                SetState(State.InProgress);
                SubscribeToEvents();
                Started?.Invoke(this);
            }
        }

        /// <inheritdoc />
        public virtual void Complete()
        {
            if (State == State.InProgress)
            {
                SetState(State.Completed);
                UnsubscribeFromEvents();
                ForceCompleteState();
                ProgressChanged?.Invoke(this);
                Completed?.Invoke(this);
            }
        }

        /// <inheritdoc />
        public virtual void Fail()
        {
            if (State == State.InProgress)
            {
                SetState(State.Failed);
                UnsubscribeFromEvents();
                Failed?.Invoke(this);
            }
        }

        /// <inheritdoc />
        public virtual void Reset()
        {
            SetState(State.NotStarted);
            UnsubscribeFromEvents();
        }

        // ---------------------------------------------------------------
        //  ITask – Task‑specific methods
        // ---------------------------------------------------------------

        /// <inheritdoc />
        public abstract void ForceCompleteState();

        /// <inheritdoc />
        public void IncrementStep()
        {
            if (OnIncrementStep() && State == State.InProgress)
            {
                ProgressChanged?.Invoke(this);
                CheckCompletion(this);
            }
        }

        /// <inheritdoc />
        public void DecrementStep()
        {
            if (OnDecrementStep() && State == State.InProgress)
            {
                ProgressChanged?.Invoke(this);
                CheckCompletion(this);
            }
        }

        /// <summary>
        /// Template method for increment logic. Returns true if the step was actually modified.
        /// </summary>
        public abstract bool OnIncrementStep();

        /// <summary>
        /// Template method for decrement logic. Returns true if the step was actually modified.
        /// </summary>
        public abstract bool OnDecrementStep();

        #region Save / Load

        /// <summary>
        /// Captures task-specific progress data for saving.
        /// </summary>
        public abstract void CaptureProgress(TaskProgressData progressData);

        /// <summary>
        /// Restores task-specific progress data after loading (events not fired).
        /// </summary>
        public abstract void RestoreProgress(TaskProgressData progressData);

        /// <summary>
        /// Directly sets the task state without triggering events or side effects.
        /// Used during save/load restoration.
        /// </summary>
        public void RestoreState(State state)
        {
            State = state;
        }

        /// <summary>
        /// Resumes a task that was restored to InProgress state.
        /// Subscribes to events so the task can respond to game events.
        /// Call this AFTER <see cref="RestoreProgress"/> and <see cref="RestoreState"/>.
        /// </summary>
        public void ResumeTask()
        {
            if (State == State.InProgress)
            {
                SubscribeToEvents();
                QuestLogger.LogVerbose(LogSubsystem.Task, $"Task '{DevName}' resumed from save");
            }
        }

        #endregion

        #region Internal Helpers

        /// <summary>
        /// Subscribes to conditions that trigger auto‑completion or failure.
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            foreach (Condition_SO condition in Data.Conditions)
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.SubscribeToEvent(Complete);

            foreach (Condition_SO condition in Data.FailureConditions)
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.SubscribeToEvent(Fail);
        }

        /// <summary>
        /// Unsubscribes from all condition events to prevent memory leaks.
        /// </summary>
        protected virtual void UnsubscribeFromEvents()
        {
            foreach (Condition_SO condition in Data.Conditions)
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.UnsubscribeFromEvent(Complete);

            foreach (Condition_SO condition in Data.FailureConditions)
                if (condition is IConditionEventDriven conditionEventDriven)
                    conditionEventDriven.UnsubscribeFromEvent(Fail);
        }

        /// <summary>
        /// Concrete task types implement this to check auto‑completion criteria.
        /// Called after any step increment/decrement.
        /// </summary>
        protected abstract void CheckCompletion(IObjective task);

        private void SetState(State state)
        {
            State = state;
            switch (state)
            {
                case State.InProgress:
                    QuestLogger.LogStart(LogSubsystem.Task, "Task", DevName);
                    break;
                case State.Completed:
                    QuestLogger.LogComplete(LogSubsystem.Task, "Task", DevName);
                    break;
                case State.Failed:
                    QuestLogger.LogFail(LogSubsystem.Task, "Task", DevName);
                    break;
            }
        }

        #endregion

        #region Equality

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is TaskRuntime other)
                return Id == other.Id;
            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion
    }
}