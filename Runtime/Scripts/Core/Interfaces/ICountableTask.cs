namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Interface for tasks that track progress as a count toward a required total.
    /// Implemented by IntTaskRuntime and DiscoveryTaskRuntime.
    /// </summary>
    /// <remarks>
    /// Use this interface when:
    /// - Binding UI to show "X/Y" progress
    /// - Checking if a task is count-based before casting
    /// - Creating generic progress displays that work with multiple task types
    /// </remarks>
    /// <example>
    /// <code>
    /// if (task is ICountableTask countable)
    /// {
    ///     progressText.text = $"{countable.CurrentCount}/{countable.RequiredCount}";
    /// }
    /// </code>
    /// </example>
    public interface ICountableTask : ITask
    {
        /// <summary>
        /// Gets the current count/progress toward the required total.
        /// </summary>
        int CurrentCount { get; }

        /// <summary>
        /// Gets the required count to complete this task.
        /// </summary>
        int RequiredCount { get; }
    }
}
