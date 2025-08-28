using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Conditions;
using HelloDev.QuestSystem.Conditions.ScriptableObjects;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine.Localization;
using HelloDev.QuestSystem.Utils;
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
        public Guid TaskId { get; private set; }

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
        public TaskState CurrentState { get; protected set; }
        
        /// <summary>
        /// The ScriptableObject data that this task was created from.
        /// </summary>
        public Task_SO TaskData { get; private set; }
        
        public abstract float Progress { get; }

        #endregion

        /// <summary>
        /// Initializes a new task instance from a ScriptableObject.
        /// </summary>
        /// <param name="taskData">The ScriptableObject containing the task's data.</param>
        protected Task(Task_SO taskData)
        {
            TaskData = taskData;
            TaskId = new Guid(taskData.TaskId);
            DevName = taskData.DevName;
            DisplayName = taskData.DisplayName;
            Description = taskData.TaskDescription;
            CurrentState = TaskState.NotStarted;
        }

        /// <summary>
        /// Attempts to start the task, changing its state to InProgress if possible.
        /// </summary>
        public virtual void StartTask()
        {
            if (CurrentState == TaskState.NotStarted)
            {
                CurrentState = TaskState.InProgress;
                QuestLogger.Log($"Task '{DevName}' started. TaskId: {TaskId}");

                SubscribeToEvents();

                OnTaskStarted?.SafeInvoke(this);
                OnTaskStateChanged?.SafeInvoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Marks the task as completed.
        /// </summary>
        public virtual void CompleteTask()
        {
            if (CurrentState == TaskState.InProgress)
            {
                CurrentState = TaskState.Completed;
                QuestLogger.Log($"Task '{DevName}' completed!");

                UnsubscribeFromEvents();

                OnTaskUpdated?.SafeInvoke(this);
                OnTaskCompleted?.SafeInvoke(this);
                OnTaskStateChanged?.SafeInvoke(this, CurrentState);
            }
        }
        
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
            if (OnIncrementStep())
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
            if (OnDecrementStep())
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
                CurrentState = TaskState.Failed;
                QuestLogger.Log($"Task '{DevName}' failed.");

                UnsubscribeFromEvents();

                OnTaskFailed?.SafeInvoke(this);
                OnTaskStateChanged?.SafeInvoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Resets the task to its initial state.
        /// </summary>
        public virtual void ResetTask()
        {
            CurrentState = TaskState.NotStarted;
            QuestLogger.Log($"Task '{DevName}' reset.");

            UnsubscribeFromEvents();

            OnTaskStateChanged?.SafeInvoke(this, CurrentState);
        }
        
        /// <summary>
        /// Subscribes to events that can trigger failure checks.
        /// This method should be implemented by concrete task types.
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            foreach (Condition_SO condition in TaskData.FailureConditions)
            {
                if (condition is IEventDrivenCondition eventCondition)
                {
                    eventCondition.SubscribeToEvent(FailTask);
                }
            }

            OnTaskUpdated.SafeSubscribe(CheckCompletion);
        }

        protected abstract void CheckCompletion(Task task);

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks.
        /// </summary>
        protected virtual void UnsubscribeFromEvents()
        {
        }
    }
}