namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Metadata for a save slot, used for displaying save information in UI.
    /// Stored separately from the main snapshot for quick access.
    /// </summary>
    [System.Serializable]
    public class SaveSlotMetadata
    {
        /// <summary>
        /// The save slot key.
        /// </summary>
        public string SlotKey;

        /// <summary>
        /// When the save was created (UTC ISO 8601 format).
        /// </summary>
        public string Timestamp;

        /// <summary>
        /// Total play time in seconds (optional, set by game).
        /// </summary>
        public float PlayTimeSeconds;

        /// <summary>
        /// Number of active quests at time of save.
        /// </summary>
        public int ActiveQuestCount;

        /// <summary>
        /// Number of completed quests at time of save.
        /// </summary>
        public int CompletedQuestCount;

        /// <summary>
        /// Custom data for game-specific metadata (player level, location, etc.).
        /// Serialized as JSON string.
        /// </summary>
        public string CustomData;
    }
}
