using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using HelloDev.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.TaskGroups
{
    /// <summary>
    /// Runtime representation of a task group, managing task state and group completion logic.
    /// Created from a TaskGroup (serialized data) at quest start.
    /// </summary>
    public class TaskGroupRuntime
    {
        #region Events

        /// <summary>
        /// Fired when this group starts.
        /// </summary>
        public UnityEvent<TaskGroupRuntime> OnGroupStarted = new();

        /// <summary>
        /// Fired when this group completes successfully.
        /// </summary>
        public UnityEvent<TaskGroupRuntime> OnGroupCompleted = new();

        /// <summary>
        /// Fired when this group fails.
        /// </summary>
        public UnityEvent<TaskGroupRuntime> OnGroupFailed = new();

        /// <summary>
        /// Fired when any task in this group is updated.
        /// </summary>
        public UnityEvent<TaskGroupRuntime, Task> OnTaskInGroupUpdated = new();

        /// <summary>
        /// Fired when any task in this group completes.
        /// </summary>
        public UnityEvent<TaskGroupRuntime, Task> OnTaskInGroupCompleted = new();

        /// <summary>
        /// Fired when any task in this group fails.
        /// </summary>
        public UnityEvent<TaskGroupRuntime, Task> OnTaskInGroupFailed = new();

        /// <summary>
        /// Fired when any task in this group starts.
        /// </summary>
        public UnityEvent<TaskGroupRuntime, Task> OnTaskInGroupStarted = new();

        #endregion

        #region Properties

        /// <summary>
        /// The name of this task group.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// How tasks in this group are executed.
        /// </summary>
        public TaskExecutionMode ExecutionMode { get; }

        /// <summary>
        /// For OptionalXofY mode: minimum number of tasks required to complete.
        /// </summary>
        public int RequiredCount { get; }

        /// <summary>
        /// All runtime tasks in this group.
        /// </summary>
        public List<Task> Tasks { get; }

        /// <summary>
        /// Current state of this group.
        /// </summary>
        public TaskGroupState CurrentState { get; private set; }

        /// <summary>
        /// Returns all tasks that are currently in progress.
        /// For Sequential mode, this is at most 1 task.
        /// For Parallel/AnyOrder/OptionalXofY, this can be multiple tasks.
        /// </summary>
        public IReadOnlyList<Task> CurrentTasks =>
            Tasks.Where(t => t.CurrentState == TaskState.InProgress).ToList();

        /// <summary>
        /// Returns all tasks that are available to work on.
        /// For Sequential: only the current InProgress task.
        /// For other modes: all InProgress tasks.
        /// </summary>
        public IReadOnlyList<Task> AvailableTasks => CurrentTasks;

        /// <summary>
        /// Number of completed tasks in this group.
        /// </summary>
        public int CompletedTaskCount => Tasks.Count(t => t.CurrentState == TaskState.Completed);

        /// <summary>
        /// Number of failed tasks in this group.
        /// </summary>
        public int FailedTaskCount => Tasks.Count(t => t.CurrentState == TaskState.Failed);

        /// <summary>
        /// Number of tasks still in progress or not started.
        /// </summary>
        public int RemainingTaskCount => Tasks.Count(t =>
            t.CurrentState == TaskState.NotStarted || t.CurrentState == TaskState.InProgress);

        /// <summary>
        /// Progress of this group (0-1).
        /// For OptionalXofY: based on RequiredCount.
        /// For others: average of all task progress.
        /// </summary>
        public float Progress
        {
            get
            {
                if (Tasks.Count == 0) return 1f;

                return ExecutionMode switch
                {
                    TaskExecutionMode.OptionalXofY =>
                        Math.Min(1f, (float)CompletedTaskCount / RequiredCount),
                    _ => Tasks.Sum(t => t.Progress) / Tasks.Count
                };
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a runtime task group from serialized data.
        /// </summary>
        /// <param name="groupData">The serialized task group data.</param>
        public TaskGroupRuntime(TaskGroup groupData)
        {
            GroupName = groupData.GroupName;
            ExecutionMode = groupData.ExecutionMode;
            RequiredCount = groupData.RequiredCount;
            CurrentState = TaskGroupState.NotStarted;

            // Create runtime tasks from Task_SO data
            Tasks = groupData.Tasks
                .Where(t => t != null)
                .Select(so => so.GetRuntimeTask())
                .ToList();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts this task group based on its execution mode.
        /// </summary>
        public void StartGroup()
        {
            if (CurrentState != TaskGroupState.NotStarted)
            {
                QuestLogger.Log($"TaskGroup '{GroupName}' is already started or completed.");
                return;
            }

            CurrentState = TaskGroupState.InProgress;
            SubscribeToTaskEvents();

            switch (ExecutionMode)
            {
                case TaskExecutionMode.Sequential:
                    // Start only the first task
                    var firstTask = Tasks.FirstOrDefault();
                    firstTask.StartTask();
                    QuestLogger.Log($"TaskGroup '{GroupName}' started (Sequential). First task: {firstTask.DevName}");
                    break;

                case TaskExecutionMode.Parallel:
                case TaskExecutionMode.AnyOrder:
                case TaskExecutionMode.OptionalXofY:
                    // Start all tasks immediately
                    foreach (var task in Tasks)
                    {
                        task.StartTask();
                    }
                    QuestLogger.Log($"TaskGroup '{GroupName}' started ({ExecutionMode}). {Tasks.Count} tasks active.");
                    break;
            }

            OnGroupStarted.SafeInvoke(this);
        }

        /// <summary>
        /// Checks if the group completion criteria is met.
        /// </summary>
        /// <returns>True if the group should be marked as completed.</returns>
        public bool CheckCompletion()
        {
            if (CurrentState != TaskGroupState.InProgress) return false;

            return ExecutionMode switch
            {
                TaskExecutionMode.OptionalXofY => CompletedTaskCount >= RequiredCount,
                _ => Tasks.All(t => t.CurrentState == TaskState.Completed)
            };
        }

        /// <summary>
        /// Checks if completion has become impossible due to failures.
        /// </summary>
        /// <returns>True if the group can no longer be completed.</returns>
        public bool IsCompletionImpossible()
        {
            if (CurrentState != TaskGroupState.InProgress) return false;

            int remainingPossible = Tasks.Count - FailedTaskCount;

            return ExecutionMode switch
            {
                TaskExecutionMode.OptionalXofY => remainingPossible < RequiredCount,
                _ => FailedTaskCount > 0 // Any failure makes completion impossible for other modes
            };
        }

        /// <summary>
        /// Completes the group successfully.
        /// </summary>
        public void CompleteGroup()
        {
            if (CurrentState != TaskGroupState.InProgress) return;

            CurrentState = TaskGroupState.Completed;
            UnsubscribeFromTaskEvents();

            QuestLogger.Log($"TaskGroup '{GroupName}' completed. {CompletedTaskCount}/{Tasks.Count} tasks done.");
            OnGroupCompleted.SafeInvoke(this);
        }

        /// <summary>
        /// Fails the group.
        /// </summary>
        public void FailGroup()
        {
            if (CurrentState != TaskGroupState.InProgress) return;

            CurrentState = TaskGroupState.Failed;
            UnsubscribeFromTaskEvents();

            QuestLogger.Log($"TaskGroup '{GroupName}' failed. {FailedTaskCount} tasks failed.");
            OnGroupFailed.SafeInvoke(this);
        }

        /// <summary>
        /// Resets all tasks in the group to NotStarted state.
        /// </summary>
        public void ResetGroup()
        {
            UnsubscribeFromTaskEvents();

            foreach (var task in Tasks)
            {
                task.ResetTask();
            }

            CurrentState = TaskGroupState.NotStarted;
            QuestLogger.Log($"TaskGroup '{GroupName}' reset.");
        }

        /// <summary>
        /// Gets a task by its ID.
        /// </summary>
        /// <param name="taskId">The task's GUID.</param>
        /// <returns>The task, or null if not found.</returns>
        public Task GetTask(Guid taskId)
        {
            return Tasks.FirstOrDefault(t => t.TaskId == taskId);
        }

        #endregion

        #region Private Methods

        private void SubscribeToTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnTaskStarted.SafeSubscribe(HandleTaskStarted);
                task.OnTaskCompleted.SafeSubscribe(HandleTaskCompleted);
                task.OnTaskUpdated.SafeSubscribe(HandleTaskUpdated);
                task.OnTaskFailed.SafeSubscribe(HandleTaskFailed);
            }
        }

        private void UnsubscribeFromTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnTaskStarted.SafeUnsubscribe(HandleTaskStarted);
                task.OnTaskCompleted.SafeUnsubscribe(HandleTaskCompleted);
                task.OnTaskUpdated.SafeUnsubscribe(HandleTaskUpdated);
                task.OnTaskFailed.SafeUnsubscribe(HandleTaskFailed);
            }
        }

        private void HandleTaskStarted(Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' started in group '{GroupName}'.");
            OnTaskInGroupStarted.SafeInvoke(this, task);
        }

        private void HandleTaskCompleted(Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' completed in group '{GroupName}'.");
            OnTaskInGroupCompleted.SafeInvoke(this, task);

            if (CheckCompletion())
            {
                CompleteGroup();
            }
            else if (ExecutionMode == TaskExecutionMode.Sequential)
            {
                // Start next task in sequence
                var nextTask = Tasks.FirstOrDefault(t => t.CurrentState == TaskState.NotStarted);
                if (nextTask != null)
                {
                    nextTask.StartTask();
                    QuestLogger.Log($"Starting next task '{nextTask.DevName}' in group '{GroupName}'.");
                }
            }
        }

        private void HandleTaskUpdated(Task task)
        {
            OnTaskInGroupUpdated.SafeInvoke(this, task);
        }

        private void HandleTaskFailed(Task task)
        {
            QuestLogger.Log($"Task '{task.DevName}' failed in group '{GroupName}'.");
            OnTaskInGroupFailed.SafeInvoke(this, task);

            // Check if completion has become impossible
            if (IsCompletionImpossible())
            {
                FailGroup();
            }
            // Otherwise, other tasks continue (for OptionalXofY mode)
        }

        #endregion
    }
}
