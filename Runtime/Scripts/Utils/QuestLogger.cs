using System.Globalization;
using UnityEngine;

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
        Manager,    // Core QuestManager operations
        Quest,      // Quest lifecycle (start, complete, fail)
        Task,       // Task progress and state changes
        Stage,      // Stage transitions and progress
        Group,      // Task group execution
        QuestLine,  // QuestLine progress
        Save,       // Save/Load operations
        Choice,     // Player choice events
        UI          // UI components
    }

    /// <summary>
    /// A centralized logger for all quest system messages with AAA-quality formatting.
    /// Features subsystem-based colors, log levels, and structured output.
    /// </summary>
    public static class QuestLogger
    {
        /// <summary>Master toggle for all logging.</summary>
        public static bool IsLoggingEnabled = true;

        /// <summary>Enable verbose logs (detailed step-by-step info). Set to false for production.</summary>
        public static bool IsVerboseEnabled = true;

        // Subsystem colors - carefully chosen for visual distinction in Unity console
        private static readonly string ColorManager   = "#4ECDC4"; // Teal
        private static readonly string ColorQuest     = "#FFE66D"; // Golden Yellow
        private static readonly string ColorTask      = "#95E1D3"; // Mint Green
        private static readonly string ColorStage     = "#F38181"; // Coral
        private static readonly string ColorGroup     = "#AA96DA"; // Lavender
        private static readonly string ColorQuestLine = "#FCBAD3"; // Pink
        private static readonly string ColorSave      = "#A8D8EA"; // Sky Blue
        private static readonly string ColorChoice    = "#DDA0DD"; // Plum
        private static readonly string ColorUI        = "#87CEEB"; // Light Sky Blue

        // Unicode icons for state changes (AAA visual polish)
        private const string IconStart    = "\u25B6"; // Play
        private const string IconComplete = "\u2713"; // Check
        private const string IconFail     = "\u2717"; // Cross
        private const string IconUpdate   = "\u2022"; // Bullet
        private const string IconTransition = "\u2192"; // Arrow
        private const string IconSave     = "\u21E9"; // Download
        private const string IconLoad     = "\u21E7"; // Upload
        private const string IconChoice   = "\u2605"; // Star

        /// <summary>
        /// Gets the color for a specific subsystem.
        /// </summary>
        private static string GetSubsystemColor(LogSubsystem subsystem)
        {
            return subsystem switch
            {
                LogSubsystem.Manager => ColorManager,
                LogSubsystem.Quest => ColorQuest,
                LogSubsystem.Task => ColorTask,
                LogSubsystem.Stage => ColorStage,
                LogSubsystem.Group => ColorGroup,
                LogSubsystem.QuestLine => ColorQuestLine,
                LogSubsystem.Save => ColorSave,
                LogSubsystem.Choice => ColorChoice,
                LogSubsystem.UI => ColorUI,
                _ => ColorManager
            };
        }

        /// <summary>
        /// Gets the tag name for a specific subsystem.
        /// </summary>
        private static string GetSubsystemTag(LogSubsystem subsystem)
        {
            return subsystem switch
            {
                LogSubsystem.Manager => "Manager",
                LogSubsystem.Quest => "Quest",
                LogSubsystem.Task => "Task",
                LogSubsystem.Stage => "Stage",
                LogSubsystem.Group => "Group",
                LogSubsystem.QuestLine => "QuestLine",
                LogSubsystem.Save => "Save",
                LogSubsystem.Choice => "Choice",
                LogSubsystem.UI => "UI",
                _ => "Quest"
            };
        }

        /// <summary>
        /// Formats a log message with subsystem tag and color.
        /// </summary>
        private static string FormatMessage(LogSubsystem subsystem, string icon, string message)
        {
            string color = GetSubsystemColor(subsystem);
            string tag = GetSubsystemTag(subsystem);
            return $"<color={color}>[{tag}]</color> {icon} {message}";
        }

        #region Standard Logging

        /// <summary>
        /// Logs an info message for a specific subsystem.
        /// </summary>
        public static void Log(LogSubsystem subsystem, string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(subsystem, IconUpdate, message));
        }

        /// <summary>
        /// Logs a warning message for a specific subsystem.
        /// </summary>
        public static void LogWarning(LogSubsystem subsystem, string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.LogWarning(FormatMessage(subsystem, "!", message));
        }

        /// <summary>
        /// Logs an error message for a specific subsystem.
        /// </summary>
        public static void LogError(LogSubsystem subsystem, string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.LogError(FormatMessage(subsystem, "X", message));
        }

        #endregion

        #region Semantic Logging (State Changes)

        /// <summary>Logs a start event (quest/task/stage started).</summary>
        public static void LogStart(LogSubsystem subsystem, string entityType, string entityName)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(subsystem, IconStart, $"{entityType} <b>'{entityName}'</b> started"));
        }

        /// <summary>Logs a completion event.</summary>
        public static void LogComplete(LogSubsystem subsystem, string entityType, string entityName)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(subsystem, IconComplete, $"{entityType} <b>'{entityName}'</b> completed"));
        }

        /// <summary>Logs a failure event.</summary>
        public static void LogFail(LogSubsystem subsystem, string entityType, string entityName)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(subsystem, IconFail, $"{entityType} <b>'{entityName}'</b> failed"));
        }

        /// <summary>Logs a transition event (stage to stage, etc.).</summary>
        public static void LogTransition(LogSubsystem subsystem, string from, string to)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(subsystem, IconTransition, $"<b>'{from}'</b> {IconTransition} <b>'{to}'</b>"));
        }

        /// <summary>Logs a save operation.</summary>
        public static void LogSave(string slot, bool success)
        {
            if (!IsLoggingEnabled) return;
            string result = success ? "succeeded" : "failed";
            string icon = success ? IconComplete : IconFail;
            Debug.Log(FormatMessage(LogSubsystem.Save, IconSave, $"Save to <b>'{slot}'</b> {result}"));
        }

        /// <summary>Logs a load operation.</summary>
        public static void LogLoad(string slot, bool success)
        {
            if (!IsLoggingEnabled) return;
            string result = success ? "succeeded" : "failed";
            Debug.Log(FormatMessage(LogSubsystem.Save, IconLoad, $"Load from <b>'{slot}'</b> {result}"));
        }

        /// <summary>Logs a player choice event.</summary>
        public static void LogChoice(string questName, string choiceId)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(LogSubsystem.Choice, IconChoice, $"Choice <b>'{choiceId}'</b> selected in quest <b>'{questName}'</b>"));
        }

        #endregion

        #region Verbose Logging (Debug-only details)

        /// <summary>
        /// Logs verbose debug info. Only shows when IsVerboseEnabled is true.
        /// Use for detailed step-by-step logging that's not needed in production.
        /// </summary>
        public static void LogVerbose(LogSubsystem subsystem, string message)
        {
            if (!IsLoggingEnabled || !IsVerboseEnabled) return;
            string color = GetSubsystemColor(subsystem);
            string tag = GetSubsystemTag(subsystem);
            Debug.Log($"<color=#888888>[{tag}]</color> <color=#AAAAAA>{message}</color>");
        }

        #endregion

        #region Legacy API (Backwards Compatibility)

        /// <summary>
        /// Logs a message using the default subsystem (Manager). For backwards compatibility.
        /// </summary>
        public static void Log(string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log(FormatMessage(LogSubsystem.Manager, IconUpdate, message));
        }

        /// <summary>
        /// Logs a warning using the default subsystem. For backwards compatibility.
        /// </summary>
        public static void LogWarning(string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.LogWarning(FormatMessage(LogSubsystem.Manager, "!", message));
        }

        /// <summary>
        /// Logs an error using the default subsystem. For backwards compatibility.
        /// </summary>
        public static void LogError(string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.LogError(FormatMessage(LogSubsystem.Manager, "X", message));
        }

        #endregion
    }
}