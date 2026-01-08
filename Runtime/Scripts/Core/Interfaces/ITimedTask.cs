namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Interface for tasks with a time limit.
    /// Implemented by TimedTaskRuntime.
    /// </summary>
    /// <remarks>
    /// Use this interface when:
    /// - Binding UI to show countdown timers
    /// - Checking if a task is time-limited before casting
    /// - Creating timer displays that update every frame
    /// </remarks>
    /// <example>
    /// <code>
    /// if (task is ITimedTask timed)
    /// {
    ///     timerText.text = $"{timed.RemainingTime:F1}s";
    ///     timerBar.fillAmount = timed.TimeProgress;
    /// }
    /// </code>
    /// </example>
    public interface ITimedTask : ITask
    {
        /// <summary>
        /// Gets the remaining time in seconds.
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// Gets the original time limit in seconds.
        /// </summary>
        float TimeLimit { get; }

        /// <summary>
        /// Gets the time progress as a value from 0 to 1 (1 = full time, 0 = expired).
        /// </summary>
        float TimeProgress { get; }

        /// <summary>
        /// Gets whether the timer has expired.
        /// </summary>
        bool IsExpired { get; }

        /// <summary>
        /// Gets whether the task objective has been completed (before time expired).
        /// </summary>
        bool IsCompleted { get; }
    }
}
