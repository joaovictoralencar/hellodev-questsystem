using System.Threading.Tasks;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Interface for save data providers. Implement this to integrate with your
    /// preferred save system (PlayerPrefs, JSON files, ES3, cloud saves, etc.).
    ///
    /// The Quest System uses this interface to save/load quest progress without
    /// being coupled to a specific storage implementation.
    /// </summary>
    /// <example>
    /// // Example: JSON file implementation
    /// public class JsonFileSaveProvider : ISaveDataProvider
    /// {
    ///     public Task&lt;bool&gt; SaveAsync(string key, QuestSystemSnapshot snapshot)
    ///     {
    ///         var json = JsonUtility.ToJson(snapshot, true);
    ///         File.WriteAllText($"{key}.json", json);
    ///         return Task.FromResult(true);
    ///     }
    /// }
    ///
    /// // Example: Easy Save 3 implementation
    /// public class ES3SaveProvider : ISaveDataProvider
    /// {
    ///     public Task&lt;bool&gt; SaveAsync(string key, QuestSystemSnapshot snapshot)
    ///     {
    ///         ES3.Save(key, snapshot);
    ///         return Task.FromResult(true);
    ///     }
    /// }
    /// </example>
    public interface ISaveDataProvider
    {
        /// <summary>
        /// Saves a quest system snapshot asynchronously.
        /// </summary>
        /// <param name="slotKey">The save slot identifier (e.g., "save_1", "autosave").</param>
        /// <param name="snapshot">The snapshot to save.</param>
        /// <returns>True if save was successful.</returns>
        Task<bool> SaveAsync(string slotKey, QuestSystemSnapshot snapshot);

        /// <summary>
        /// Loads a quest system snapshot asynchronously.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>The loaded snapshot, or null if not found or failed.</returns>
        Task<QuestSystemSnapshot> LoadAsync(string slotKey);

        /// <summary>
        /// Checks if a save slot exists.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if the slot exists.</returns>
        Task<bool> ExistsAsync(string slotKey);

        /// <summary>
        /// Deletes a save slot.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>True if deletion was successful or slot didn't exist.</returns>
        Task<bool> DeleteAsync(string slotKey);

        /// <summary>
        /// Gets metadata for a save slot without loading the full snapshot.
        /// Useful for displaying save slot information in UI.
        /// </summary>
        /// <param name="slotKey">The save slot identifier.</param>
        /// <returns>Metadata for the save slot, or null if not found.</returns>
        Task<SaveSlotMetadata> GetMetadataAsync(string slotKey);

        /// <summary>
        /// Lists all available save slot keys.
        /// </summary>
        /// <returns>Array of save slot identifiers.</returns>
        Task<string[]> GetAllSlotsAsync();
    }

    /// <summary>
    /// Metadata for a save slot, used for displaying save information in UI.
    /// </summary>
    [System.Serializable]
    public class SaveSlotMetadata
    {
        /// <summary>
        /// The save slot key.
        /// </summary>
        public string SlotKey;

        /// <summary>
        /// When the save was created (UTC).
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
