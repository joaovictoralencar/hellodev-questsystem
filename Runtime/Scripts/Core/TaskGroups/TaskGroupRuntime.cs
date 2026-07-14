using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Interfaces;
using HelloDev.QuestSystem.Tasks;
using HelloDev.QuestSystem.Utils;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.TaskGroups
{
    /// <summary>
    /// Runtime representation of a task group, managing task state and group completion logic.
    /// Implements <see cref="ITaskGroup"/> (and through it <see cref="IObjectiveGroup"/> / <see cref="IObjective"/>).
    /// </summary>
    public class TaskGroupRuntime : ITaskGroup
    {
        // ---------------------------------------------------------------
        //  IObjective UnityEvents (the only lifecycle events)
        // ---------------------------------------------------------------

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

        // ---------------------------------------------------------------
        //  IObjectiveGroup event
        // ---------------------------------------------------------------

        /// <inheritdoc />
        public UnityEvent<IObjectiveGroup, IObjective> OnObjectiveCompleted { get; set; } = new();

        // ---------------------------------------------------------------
        //  Properties (interface implementations)
        // ---------------------------------------------------------------

        /// <inheritdoc />
        public Guid Id { get; }

        /// <summary>
        /// The human‑readable name of this task group.
        /// </summary>
        public string GroupName { get; }

        /// <inheritdoc />
        public IReadOnlyList<TaskRuntime> Tasks { get; }

        // IObjectiveGroup.Objectives – returns the same tasks, upcasted.
        /// <inheritdoc />
        public IReadOnlyList<IObjective> Objectives => Tasks.Cast<IObjective>().ToList().AsReadOnly();

        /// <summary>
        /// Underlying execution mode stored as the task‑system enum.
        /// </summary>
        private readonly TaskExecutionMode _taskExecutionMode;

        /// <inheritdoc />
        public ObjectiveExecutionMode ExecutionMode => MapExecutionMode(_taskExecutionMode);

        /// <inheritdoc />
        public int RequiredCount { get; }

        /// <summary>
        /// Current state stored as the unified <see cref="State"/> enum.
        /// </summary>
        private State _state = State.NotStarted;

        /// <inheritdoc />
        public State State => _state;

        /// <summary>
        /// Provides backward‑compatible access to the group state using <see cref="TaskGroupState"/>.
        /// </summary>
        public TaskGroupState CurrentState => MapToTaskGroupState(_state);

        /// <inheritdoc />
        public float Progress
        {
            get
            {
                if (Tasks.Count == 0) return 1f;
                return _taskExecutionMode switch
                {
                    TaskExecutionMode.OptionalXofY => Math.Min(1f, (float)CompletedCount / RequiredCount),
                    _ => Tasks.Sum(t => t.Progress) / Tasks.Count
                };
            }
        }

        /// <inheritdoc />
        public bool IsComplete => _state == State.Completed;

        /// <inheritdoc />
        public bool IsFailed => _state == State.Failed;

        /// <inheritdoc />
        public int CompletedCount => Tasks.Count(t => t.State == State.Completed);

        UnityEvent<IObjectiveGroup, IObjective> IObjectiveGroup.OnObjectiveCompleted { get; set; }

        // Convenience properties retained for backward compatibility
        private int FailedTaskCount => Tasks.Count(t => t.State == State.Failed);
        private int RemainingTaskCount => Tasks.Count(t => t.State == State.NotStarted || t.State == State.InProgress);

        // ---------------------------------------------------------------
        //  Constructor
        // ---------------------------------------------------------------

        public TaskGroupRuntime(TaskGroup groupData)
        {
            // Ideally Id would come from serialized data; here we generate a runtime Id.
            Id = Guid.NewGuid();
            GroupName = groupData.GroupName;
            _taskExecutionMode = groupData.ExecutionMode;
            RequiredCount = groupData.RequiredCount;

            Tasks = groupData.Tasks
                .Where(t => t != null)
                .Select(so => so.GetRuntimeTask())
                .ToList();
        }

        // ---------------------------------------------------------------
        //  IObjective Lifecycle (the only lifecycle entry points)
        // ---------------------------------------------------------------

        public void Start()
        {
            if (_state != State.NotStarted) return;

            _state = State.InProgress;
            SubscribeToTaskEvents();

            QuestLogger.Log(LogSubsystem.TaskGroup, $"TaskGroup '{GroupName}' started ({_taskExecutionMode}).");
            Started.Invoke(this);

            switch (_taskExecutionMode)
            {
                case TaskExecutionMode.Sequential:
                    Tasks.FirstOrDefault()?.Start();
                    break;
                default:
                    foreach (var task in Tasks)
                        task.Start();
                    break;
            }
        }

        public void Complete()
        {
            if (_state != State.InProgress) return;

            _state = State.Completed;
            UnsubscribeFromTaskEvents();

            QuestLogger.Log(LogSubsystem.TaskGroup, $"TaskGroup '{GroupName}' completed.");
            Completed.Invoke(this);
        }

        public void Fail()
        {
            if (_state != State.InProgress) return;

            _state = State.Failed;
            UnsubscribeFromTaskEvents();

            QuestLogger.Log(LogSubsystem.TaskGroup, $"TaskGroup '{GroupName}' failed.");
            Failed.Invoke(this);
        }

        public void Reset()
        {
            UnsubscribeFromTaskEvents();

            foreach (var task in Tasks)
                task.Reset();

            _state = State.NotStarted;
            QuestLogger.Log(LogSubsystem.TaskGroup, $"TaskGroup '{GroupName}' reset.");
        }

        // ---------------------------------------------------------------
        //  ITaskGroup – specific logic
        // ---------------------------------------------------------------

        public TaskRuntime GetTask(Guid taskId) => Tasks.FirstOrDefault(t => t.Id == taskId);

        public bool CheckCompletion()
        {
            if (_state != State.InProgress) return false;
            return _taskExecutionMode switch
            {
                TaskExecutionMode.OptionalXofY => CompletedCount >= RequiredCount,
                _ => Tasks.All(t => t.State == State.Completed)
            };
        }

        public bool IsCompletionImpossible()
        {
            if (_state != State.InProgress) return false;
            int remainingPossible = Tasks.Count - FailedTaskCount;
            return _taskExecutionMode switch
            {
                TaskExecutionMode.OptionalXofY => remainingPossible < RequiredCount,
                _ => FailedTaskCount > 0
            };
        }

        // ---------------------------------------------------------------
        //  Task event subscriptions (internal)
        // ---------------------------------------------------------------

        private void SubscribeToTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.Started.AddListener(OnTaskStarted);
                task.ProgressChanged.AddListener(OnTaskProgressChanged);
                task.Completed.AddListener(OnTaskCompleted);
                task.Failed.AddListener(OnTaskFailed);
            }
        }

        private void UnsubscribeFromTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.Started.RemoveListener(OnTaskStarted);
                task.ProgressChanged.RemoveListener(OnTaskProgressChanged);
                task.Completed.RemoveListener(OnTaskCompleted);
                task.Failed.RemoveListener(OnTaskFailed);
            }
        }

        private void OnTaskStarted(IObjective task)
        {
            // No group‑level event for individual task starts –
            // consumers can subscribe to task.Started directly.
        }

        private void OnTaskProgressChanged(IObjective task)
        {
            ProgressChanged.Invoke(this);
        }

        private void OnTaskCompleted(IObjective task)
        {
            OnObjectiveCompleted?.Invoke(this, task);

            if (CheckCompletion())
            {
                Complete();
            }
            else if (_taskExecutionMode == TaskExecutionMode.Sequential)
            {
                var nextTask = Tasks.FirstOrDefault(t => t.State == State.NotStarted);
                if (nextTask != null)
                {
                    nextTask.Start();
                    QuestLogger.Log(LogSubsystem.TaskGroup, $"Starting next task '{nextTask.DevName}' in group '{GroupName}'.");
                }
            }
        }

        private void OnTaskFailed(IObjective task)
        {
            QuestLogger.Log(LogSubsystem.TaskGroup, $"Task '{((TaskRuntime)task).DevName}' failed in group '{GroupName}'.");
            if (IsCompletionImpossible())
            {
                Fail();
            }
        }

        // ---------------------------------------------------------------
        //  Save / Load
        // ---------------------------------------------------------------

        public void RestoreGroupState(TaskGroupState state)
        {
            _state = MapToState(state);
        }

        public void ResumeGroup()
        {
            if (_state == State.InProgress)
            {
                SubscribeToTaskEvents();
                QuestLogger.LogVerbose(LogSubsystem.TaskGroup, $"TaskGroup '{GroupName}' resumed from save");
            }
        }

        // ---------------------------------------------------------------
        //  Mapping helpers
        // ---------------------------------------------------------------

        private static State MapToState(TaskGroupState tgs) => tgs switch
        {
            TaskGroupState.NotStarted => State.NotStarted,
            TaskGroupState.InProgress => State.InProgress,
            TaskGroupState.Completed => State.Completed,
            TaskGroupState.Failed => State.Failed,
            _ => State.NotStarted
        };

        private static TaskGroupState MapToTaskGroupState(State s) => s switch
        {
            State.NotStarted => TaskGroupState.NotStarted,
            State.InProgress => TaskGroupState.InProgress,
            State.Completed => TaskGroupState.Completed,
            State.Failed => TaskGroupState.Failed,
            _ => TaskGroupState.NotStarted
        };

        private static ObjectiveExecutionMode MapExecutionMode(TaskExecutionMode mode) => mode switch
        {
            TaskExecutionMode.Sequential => ObjectiveExecutionMode.Sequential,
            TaskExecutionMode.Parallel => ObjectiveExecutionMode.Parallel,
            TaskExecutionMode.AnyOrder => ObjectiveExecutionMode.AnyOrder,
            TaskExecutionMode.OptionalXofY => ObjectiveExecutionMode.OptionalXOfY,
            _ => ObjectiveExecutionMode.Sequential
        };
    }
}