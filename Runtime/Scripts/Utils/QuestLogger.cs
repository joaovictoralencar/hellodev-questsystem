using UnityEngine;

namespace HelloDev.QuestSystem.Utils
{
    /// <summary>
    /// A centralized logger for all quest system messages.
    /// Use this to easily toggle logging on/off and standardize output.
    /// </summary>
    public static class QuestLogger
    {
        public static bool IsLoggingEnabled = true;
        public static Color LogColor = Color.orange;

        /// <summary>
        /// Converts a Color to a hex string in the format #RRGGBB.
        /// </summary>
        public static string ColorToHex(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255), 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// Logs a regular message to the console with a standardized prefix.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            if (IsLoggingEnabled)
            {
                Debug.Log($"<color={ColorToHex(LogColor)}>[QuestSystem]</color> {message}");
            }
        }

        /// <summary>
        /// Logs a warning message to the console with a standardized prefix.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void LogWarning(string message)
        {
            if (IsLoggingEnabled)
            {
                Debug.LogWarning($"<color={ColorToHex(LogColor)}>[QuestSystem]</color> {message}");
            }
        }

        /// <summary>
        /// Logs an error message to the console with a standardized prefix.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(string message)
        {
            if (IsLoggingEnabled)
            {
                Debug.LogError($"<color={ColorToHex(LogColor)}>[QuestSystem]</color> {message}");
            }
        }
    }
}