using System.Globalization;
using HelloDev.Logging;

namespace HelloDev.QuestSystem.Utils
{
    /// <summary>
    /// Utility methods for quest system calculations and formatting.
    /// </summary>
    public static class QuestUtils
    {
        /// <summary>
        /// Converts a float value (0-1) to a percentage string (0-100).
        /// </summary>
        public static string GetPercentage(float value)
        {
            return ((int)(value * 100)).ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Log subsystems for the quest system. Each has its own color for easy visual filtering.
    /// </summary>
    public enum LogSubsystem
    {
        Manager,     // Core QuestManager operations
        Quest,       // Quest lifecycle (start, complete, fail)
        Task,        // Task progress and state changes
        TaskGroup,   // Task group execution
        Stage,       // Stage transitions and progress
        Group,       // Task group execution
        QuestLine,   // QuestLine progress
        Save,        // Save/Load operations
        SaveManager, // QuestSnapshotProvider operations
        Choice,      // Player choice events
        UI,          // UI components
        Achievement  // Achievement system operations
    }

    /// <summary>
    /// Quest system logger that delegates to the centralized HelloDev.Logging.Logger.
    /// Systems are registered via LoggerSettings_SO and LoggerInitializer.
    /// </summary>
    public static class QuestLogger
    {
        #region Toggle Properties (Delegates to Logger)

        /// <summary>Master toggle for all logging.</summary>
        public static bool IsLoggingEnabled
        {
            get => Logger.IsEnabled;
            set => Logger.IsEnabled = value;
        }

        /// <summary>Enable verbose logs (detailed step-by-step info). Set to false for production.</summary>
        public static bool IsVerboseEnabled
        {
            get => Logger.IsVerboseEnabled;
            set => Logger.IsVerboseEnabled = value;
        }

        #endregion

        #region System ID Mapping

        // Unicode icons for specialized logging
        private const string IconSave = "\u21E9";   // Download
        private const string IconLoad = "\u21E7";   // Upload
        private const string IconChoice = "\u2605"; // Star

        private static string GetSystemId(LogSubsystem subsystem)
        {
            return subsystem switch
            {
                LogSubsystem.Manager => "Quest.Manager",
                LogSubsystem.Quest => "Quest.Quest",
                LogSubsystem.Task => "Quest.Task",
                LogSubsystem.TaskGroup => "Quest.TaskGroup",
                LogSubsystem.Stage => "Quest.Stage",
                LogSubsystem.Group => "Quest.Group",
                LogSubsystem.QuestLine => "Quest.QuestLine",
                LogSubsystem.Save => "Quest.Save",
                LogSubsystem.SaveManager => "Quest.SaveManager",
                LogSubsystem.Choice => "Quest.Choice",
                LogSubsystem.UI => "Quest.UI",
                _ => "Quest.Manager"
            };
        }

        #endregion

        #region Standard Logging

        /// <summary>
        /// Logs an info message for a specific subsystem.
        /// </summary>
        public static void Log(LogSubsystem subsystem, string message)
        {
            Logger.Log(GetSystemId(subsystem), message);
        }

        /// <summary>
        /// Logs a warning message for a specific subsystem.
        /// </summary>
        public static void LogWarning(LogSubsystem subsystem, string message)
        {
            Logger.LogWarning(GetSystemId(subsystem), message);
        }

        /// <summary>
        /// Logs an error message for a specific subsystem.
        /// </summary>
        public static void LogError(LogSubsystem subsystem, string message)
        {
            Logger.LogError(GetSystemId(subsystem), message);
        }

        /// <summary>
        /// Logs verbose debug info. Only shows when IsVerboseEnabled is true.
        /// </summary>
        public static void LogVerbose(LogSubsystem subsystem, string message)
        {
            Logger.LogVerbose(GetSystemId(subsystem), message);
        }

        #endregion

        #region Semantic Logging (State Changes)

        /// <summary>Logs a start event (quest/task/stage started).</summary>
        public static void LogStart(LogSubsystem subsystem, string entityType, string entityName)
        {
            Logger.Log(GetSystemId(subsystem), $"Started: Entity type: {entityType}, Entity name: {entityName}");
        }

        /// <summary>Logs a completion event.</summary>
        public static void LogComplete(LogSubsystem subsystem, string entityType, string entityName)
        {
            Logger.Log(GetSystemId(subsystem), $"Completed: Entity type: {entityType}, Entity name: {entityName}");
        }

        /// <summary>Logs a failure event.</summary>
        public static void LogFail(LogSubsystem subsystem, string entityType, string entityName)
        {
            Logger.Log(GetSystemId(subsystem), $"Failed: Entity type: {entityType}, Entity name: {entityName}");
        }

        /// <summary>Logs a transition event (stage to stage, etc.).</summary>
        public static void LogTransition(LogSubsystem subsystem, string from, string to)
        {
            Logger.Log(GetSystemId(subsystem), $"from {from} to {to}");
        }

        #endregion
    }
}
