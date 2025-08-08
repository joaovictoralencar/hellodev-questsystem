using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Conditions;
using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine.Localization;
using HelloDev.QuestSystem.Utils;

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
        public Action<Task, TaskState> OnTaskStateChanged;

        /// <summary>
        /// Fired when the task's progress has changed. Provides the task and an update info object.
        /// </summary>
        public Action<Task> OnTaskUpdated;

        /// <summary>
        /// Fired specifically when the task is started.
        /// </summary>
        public Action<Task> OnTaskStarted;

        /// <summary>
        /// Fired specifically when the task is completed.
        /// </summary>
        public Action<Task> OnTaskCompleted;

        /// <summary>
        /// Fired specifically when the task fails.
        /// </summary>
        public Action<Task> OnTaskFailed;

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
        /// A list of conditions that must be met to fail this task.
        /// </summary>
        protected List<ICondition> FailureConditions { get; private set; }

        /// <summary>
        /// The ScriptableObject data that this task was created from.
        /// </summary>
        public Task_SO TaskData { get; private set; }

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

            // FailureConditions = taskData.FailureConditions.Select(so => so.GetRuntimeCondition()).ToList();
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

                SubscribeToFailureEvents();

                OnTaskStarted?.Invoke(this);
                OnTaskStateChanged?.Invoke(this, CurrentState);
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

                UnsubscribeFromFailureEvents();

                OnTaskCompleted?.Invoke(this);
                OnTaskUpdated?.Invoke(this);
                OnTaskStateChanged?.Invoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public void IncrementStep()
        {
            if (OnIncrementStep())
            {
                QuestLogger.Log($"Incrementing step for Task '{DevName}'");
                //CheckForCompletion();
            }
        }

        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public abstract bool OnIncrementStep();

        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        public void DecrementStep()
        {
            // if (CurrentState != TaskState.InProgress)
            // {
            //     return false;
            // }
            //
            // QuestLogger.Log($"Incrementing step for Task '{DevName}'");
            //
            // return true;
        }

        /// <summary>
        /// Increments the step for the task.
        /// </summary>
        protected virtual bool OnDecrementStep()
        {
            return true;
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

                UnsubscribeFromFailureEvents();

                OnTaskFailed?.Invoke(this);
                OnTaskStateChanged?.Invoke(this, CurrentState);
            }
        }

        /// <summary>
        /// Resets the task to its initial state.
        /// </summary>
        public virtual void ResetTask()
        {
            CurrentState = TaskState.NotStarted;
            QuestLogger.Log($"Task '{DevName}' reset.");

            UnsubscribeFromFailureEvents();

            OnTaskStateChanged?.Invoke(this, CurrentState);
        }

        /// <summary>
        /// Evaluates all failure conditions for the task.
        /// </summary>
        protected virtual bool CheckFailure()
        {
            if (CurrentState != TaskState.InProgress)
            {
                return false;
            }

            foreach (var condition in FailureConditions)
            {
                if (condition.Evaluate())
                {
                    QuestLogger.Log($"Task '{DevName}' failure condition met. Failing task.");
                    FailTask();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Subscribes to events that can trigger failure checks.
        /// This method should be implemented by concrete task types.
        /// </summary>
        protected virtual void SubscribeToFailureEvents()
        {
            // Concrete tasks will implement this method to listen for relevant events.
        }

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks.
        /// </summary>
        protected virtual void UnsubscribeFromFailureEvents()
        {
            // Concrete tasks will implement this method.
        }
    }
}