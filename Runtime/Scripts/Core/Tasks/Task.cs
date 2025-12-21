using System;
using HelloDev.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Localization;
using HelloDev.Utils;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Tasks
{
    /// <summary>
    /// Represents a single objective within a quest. This abstract class provides the
    /// core functionality for all task types. Specific tasks must inherit from it.
    /// </summary>
    public abstract class Task
    {
        #region Events
        /// <summary>
        /// Fired when the task's state changes. Provides the task and the new state.
        /// </summary>
        public UnityEvent<Task, TaskState> OnTaskStateChanged = new();

        /// <summary>
        /// Fired when the task's progress has changed. Provides the task and an update info object.
        /// </summary>
        public UnityEvent<Task> OnTaskUpdated = new();

        /// <summary>
        /// Fired specifically when the task is started.
        /// </summary>
        public UnityEvent<Task> OnTaskStarted = new();

        /// <summary>
        /// Fired specifically when the task is completed.
        /// </summary>
        public UnityEvent<Task> OnTaskCompleted = new();

        /// <summary>
        /// Fired specifically when the task fails.
        /// </summary>
        public UnityEvent<Task> OnTaskFailed = new();

        #endregion

        #region Properties

        /// <summary>
        /// A unique, permanent identifier for the task instance.
        /// </summary>
        public Guid TaskId { get; }

        /// <summary>
        /// A developer-friendly name for the task, used for internal identification.
        /// </summary>
        public string DevName { get; private set; }

        /// <summary>
        /// The localized name of the task for display in the UI.
        /// </summary>
        public LocalizedString DisplayName { get; private set; }

        /// <summary>
        /// The localized description of the task.
        /// </summary>
        public LocalizedString Description { get; private set; }

        /// <summary>
        /// The current state of the task (NotStarted, InProgress, Completed, Failed).
        /// </summary>
        public TaskState CurrentState { get; private set; }
        
        /// <summary>
        /// The ScriptableObject data that this task was created from.
        /// </summary>
        public Task_SO Data { get; private set; }
        
        public abstract float Progress { get; }

        #endregion

        /// <summary>
        /// Initializes a new task instance from a ScriptableObject.
        /// </summary>
        /// <param name="data">The ScriptableObject containing the task's data.</param>
        protected Task(Task_SO data)
        {
            Data = data;
            TaskId = data.TaskId;
            DevName = data.DevName;
            DisplayName = data.DisplayName;
            Description = data.TaskDescription;
            CurrentState = TaskState.NotStarted;
        }

        /// <summary>
        /// Attempts to start the task, changing its state to InProgress if possible.
        /// </summary>
        public virtual void StartTask()
        {
            if (CurrentState == TaskState.NotStarted)
            {
                SetTaskState(TaskState.InProgress);

                SubscribeToEvents();

                OnTaskStarted?.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Marks the task as completed.
        /// </summary>
        public virtual void CompleteTask()
        {
            if (CurrentState == TaskState.InProgress)
            {
                SetTaskState(TaskState.Completed);

                UnsubscribeFromEvents();
                ForceCompleteState();

                OnTaskUpdated?.SafeInvoke(this);
                OnTaskCompleted?.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Forces the task parameters to a completed state.
        /// </summary>
        public abstract void ForceCompleteState();
        
        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public abstract bool OnIncrementStep();

        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public abstract bool OnDecrementStep();
        
        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public void IncrementStep()
        {
            if (OnIncrementStep() && CurrentState == TaskState.InProgress)
            {
                QuestLogger.Log($"Incremented step for Task '{DevName}'");
                OnTaskUpdated.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public void DecrementStep()
        {
            if (OnDecrementStep() && CurrentState == TaskState.InProgress)
            {
                QuestLogger.Log($"Decremented step for Task '{DevName}'");
                OnTaskUpdated.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Marks the task as failed.
        /// </summary>
        public virtual void FailTask()
        {
            if (CurrentState == TaskState.InProgress)
            {
                SetTaskState(TaskState.Failed);

                UnsubscribeFromEvents();

                OnTaskFailed?.SafeInvoke(this);
            }
        }

        /// <summary>
        /// Resets the task to its initial state.
        /// </summary>
        public virtual void ResetTask()
        {
            SetTaskState(TaskState.NotStarted);
            QuestLogger.Log($"Task '{DevName}' reset.");
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// Subscribes to events that can trigger failure checks.
        /// This method should be implemented by concrete task types.
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            foreach (Condition_SO condition in Data.Conditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven) 
                    conditionEventDriven.SubscribeToEvent(CompleteTask);
            }
            
            foreach (Condition_SO condition in Data.FailureConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven) 
                    conditionEventDriven.SubscribeToEvent(FailTask);
            }
            
            OnTaskUpdated.SafeSubscribe(CheckCompletion);
        }

        protected abstract void CheckCompletion(Task task);

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks.
        /// </summary>
        protected virtual void UnsubscribeFromEvents()
        {
            foreach (Condition_SO condition in Data.Conditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven) 
                    conditionEventDriven.UnsubscribeFromEvent();
            }
            
            foreach (Condition_SO condition in Data.FailureConditions)
            {
                if (condition is IConditionEventDriven conditionEventDriven) 
                    conditionEventDriven.UnsubscribeFromEvent();
            }
            
            OnTaskUpdated.Unsubscribe(CheckCompletion);
        }

        private void SetTaskState(TaskState state)
        {
            CurrentState = state;
            QuestLogger.Log($"Task '{DevName}' state changed to {state}.");
            OnTaskStateChanged?.SafeInvoke(this, CurrentState);
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Task other)
            {
                return TaskId == other.TaskId;
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }
    }
}