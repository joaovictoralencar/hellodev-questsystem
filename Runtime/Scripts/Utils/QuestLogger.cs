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
        SaveManager, // QuestSaveManager operations
        Choice,      // Player choice events
        UI           // UI components
    }

    /// <summary>
    /// Quest system logger that delegates to the centralized HelloDev.Logging.Logger.
    /// Self-registers all subsystems on first use.
    /// </summary>
    public static class QuestLogger
    {
        #region Static Constructor - Register Systems

        private static bool _registered = false;

        private static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;

            // Register all quest subsystems with the central Logger
            Logger.RegisterSystem("Quest.Manager", "#4ECDC4", "Manager");
            Logger.RegisterSystem("Quest.Quest", "#FFE66D", "Quest");
            Logger.RegisterSystem("Quest.Task", "#95E1D3", "Task");
            Logger.RegisterSystem("Quest.TaskGroup", "#95E1D3", "TaskGroup");
            Logger.RegisterSystem("Quest.Stage", "#F38181", "Stage");
            Logger.RegisterSystem("Quest.Group", "#AA96DA", "Group");
            Logger.RegisterSystem("Quest.QuestLine", "#FCBAD3", "QuestLine");
            Logger.RegisterSystem("Quest.Save", "#A8D8EA", "Save");
            Logger.RegisterSystem("Quest.SaveManager", "#7EC8E3", "SaveManager");
            Logger.RegisterSystem("Quest.Choice", "#DDA0DD", "Choice");
            Logger.RegisterSystem("Quest.UI", "#87CEEB", "UI");
        }

        #endregion

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
            EnsureRegistered();
            Logger.Log(GetSystemId(subsystem), message);
        }

        /// <summary>
        /// Logs a warning message for a specific subsystem.
        /// </summary>
        public static void LogWarning(LogSubsystem subsystem, string message)
        {
            EnsureRegistered();
            Logger.LogWarning(GetSystemId(subsystem), message);
        }

        /// <summary>
        /// Logs an error message for a specific subsystem.
        /// </summary>
        public static void LogError(LogSubsystem subsystem, string message)
        {
            EnsureRegistered();
            Logger.LogError(GetSystemId(subsystem), message);
        }

        /// <summary>
        /// Logs verbose debug info. Only shows when IsVerboseEnabled is true.
        /// </summary>
        public static void LogVerbose(LogSubsystem subsystem, string message)
        {
            EnsureRegistered();
            Logger.LogVerbose(GetSystemId(subsystem), message);
        }

        #endregion

        #region Semantic Logging (State Changes)

        /// <summary>Logs a start event (quest/task/stage started).</summary>
        public static void LogStart(LogSubsystem subsystem, string entityType, string entityName)
        {
            EnsureRegistered();
            Logger.LogStart(GetSystemId(subsystem), entityType, entityName);
        }

        /// <summary>Logs a completion event.</summary>
        public static void LogComplete(LogSubsystem subsystem, string entityType, string entityName)
        {
            EnsureRegistered();
            Logger.LogComplete(GetSystemId(subsystem), entityType, entityName);
        }

        /// <summary>Logs a failure event.</summary>
        public static void LogFail(LogSubsystem subsystem, string entityType, string entityName)
        {
            EnsureRegistered();
            Logger.LogFail(GetSystemId(subsystem), entityType, entityName);
        }

        /// <summary>Logs a transition event (stage to stage, etc.).</summary>
        public static void LogTransition(LogSubsystem subsystem, string from, string to)
        {
            EnsureRegistered();
            Logger.LogTransition(GetSystemId(subsystem), from, to);
        }

        /// <summary>Logs a save operation.</summary>
        public static void LogSave(string slot, bool success)
        {
            EnsureRegistered();
            string result = success ? "succeeded" : "failed";
            Logger.Log("Quest.Save", $"{IconSave} Save to <b>'{slot}'</b> {result}");
        }

        /// <summary>Logs a load operation.</summary>
        public static void LogLoad(string slot, bool success)
        {
            EnsureRegistered();
            string result = success ? "succeeded" : "failed";
            Logger.Log("Quest.Save", $"{IconLoad} Load from <b>'{slot}'</b> {result}");
        }

        /// <summary>Logs a player choice event.</summary>
        public static void LogChoice(string questName, string choiceId)
        {
            EnsureRegistered();
            Logger.Log("Quest.Choice", $"{IconChoice} Choice <b>'{choiceId}'</b> selected in quest <b>'{questName}'</b>");
        }

        #endregion

        #region Legacy API (Backwards Compatibility)

        /// <summary>
        /// Logs a message using the default subsystem (Manager). For backwards compatibility.
        /// </summary>
        public static void Log(string message)
        {
            EnsureRegistered();
            Logger.Log("Quest.Manager", message);
        }

        /// <summary>
        /// Logs a warning using the default subsystem. For backwards compatibility.
        /// </summary>
        public static void LogWarning(string message)
        {
            EnsureRegistered();
            Logger.LogWarning("Quest.Manager", message);
        }

        /// <summary>
        /// Logs an error using the default subsystem. For backwards compatibility.
        /// </summary>
        public static void LogError(string message)
        {
            EnsureRegistered();
            Logger.LogError("Quest.Manager", message);
        }

        #endregion
    }
}
