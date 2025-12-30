using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace HelloDev.QuestSystem.SaveLoad
{
    /// <summary>
    /// Default save data provider that uses JSON files in Unity's persistent data path.
    /// This is a simple implementation suitable for development and single-player games.
    ///
    /// For production games, consider implementing your own ISaveDataProvider for:
    /// - Cloud saves (Steam Cloud, PlayStation, Xbox, etc.)
    /// - Encrypted saves
    /// - Binary serialization for smaller files
    /// - Integration with third-party save systems (Easy Save 3, etc.)
    /// </summary>
    public class JsonFileSaveProvider : ISaveDataProvider
    {
        private readonly string _saveDirectory;
        private readonly string _fileExtension;
        private readonly bool _prettyPrint;

        /// <summary>
        /// Creates a new JSON file save provider.
        /// </summary>
        /// <param name="subdirectory">Subdirectory within Application.persistentDataPath (default: "QuestSaves").</param>
        /// <param name="fileExtension">File extension for save files (default: ".questsave").</param>
        /// <param name="prettyPrint">If true, JSON output is formatted for readability.</param>
        public JsonFileSaveProvider(
            string subdirectory = "QuestSaves",
            string fileExtension = ".questsave",
            bool prettyPrint = false)
        {
            _saveDirectory = Path.Combine(Application.persistentDataPath, subdirectory);
            _fileExtension = fileExtension;
            _prettyPrint = prettyPrint;

            // Ensure directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }

        /// <inheritdoc/>
        public Task<bool> SaveAsync(string slotKey, QuestSystemSnapshot snapshot)
        {
            try
            {
                string filePath = GetFilePath(slotKey);
                string json = JsonUtility.ToJson(snapshot, _prettyPrint);

                File.WriteAllText(filePath, json);

                // Also save metadata separately for quick access
                var metadata = CreateMetadata(slotKey, snapshot);
                string metaPath = GetMetadataPath(slotKey);
                string metaJson = JsonUtility.ToJson(metadata, _prettyPrint);
                File.WriteAllText(metaPath, metaJson);

                Debug.Log($"[QuestSystem] Saved to: {filePath}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestSystem] Save failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<QuestSystemSnapshot> LoadAsync(string slotKey)
        {
            try
            {
                string filePath = GetFilePath(slotKey);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[QuestSystem] Save file not found: {filePath}");
                    return Task.FromResult<QuestSystemSnapshot>(null);
                }

                string json = File.ReadAllText(filePath);
                var snapshot = JsonUtility.FromJson<QuestSystemSnapshot>(json);

                Debug.Log($"[QuestSystem] Loaded from: {filePath}");
                return Task.FromResult(snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestSystem] Load failed: {ex.Message}");
                return Task.FromResult<QuestSystemSnapshot>(null);
            }
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(string slotKey)
        {
            string filePath = GetFilePath(slotKey);
            return Task.FromResult(File.Exists(filePath));
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(string slotKey)
        {
            try
            {
                string filePath = GetFilePath(slotKey);
                string metaPath = GetMetadataPath(slotKey);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                if (File.Exists(metaPath))
                    File.Delete(metaPath);

                Debug.Log($"[QuestSystem] Deleted save: {slotKey}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestSystem] Delete failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<SaveSlotMetadata> GetMetadataAsync(string slotKey)
        {
            try
            {
                string metaPath = GetMetadataPath(slotKey);

                if (!File.Exists(metaPath))
                {
                    return Task.FromResult<SaveSlotMetadata>(null);
                }

                string json = File.ReadAllText(metaPath);
                var metadata = JsonUtility.FromJson<SaveSlotMetadata>(json);

                return Task.FromResult(metadata);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestSystem] GetMetadata failed: {ex.Message}");
                return Task.FromResult<SaveSlotMetadata>(null);
            }
        }

        /// <inheritdoc/>
        public Task<string[]> GetAllSlotsAsync()
        {
            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    return Task.FromResult(Array.Empty<string>());
                }

                var files = Directory.GetFiles(_saveDirectory, $"*{_fileExtension}");
                var slots = new string[files.Length];

                for (int i = 0; i < files.Length; i++)
                {
                    slots[i] = Path.GetFileNameWithoutExtension(files[i]);
                }

                return Task.FromResult(slots);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestSystem] GetAllSlots failed: {ex.Message}");
                return Task.FromResult(Array.Empty<string>());
            }
        }

        private string GetFilePath(string slotKey)
        {
            return Path.Combine(_saveDirectory, $"{slotKey}{_fileExtension}");
        }

        private string GetMetadataPath(string slotKey)
        {
            return Path.Combine(_saveDirectory, $"{slotKey}.meta.json");
        }

        private SaveSlotMetadata CreateMetadata(string slotKey, QuestSystemSnapshot snapshot)
        {
            return new SaveSlotMetadata
            {
                SlotKey = slotKey,
                Timestamp = snapshot.Timestamp,
                ActiveQuestCount = snapshot.ActiveQuests?.Count ?? 0,
                CompletedQuestCount = snapshot.CompletedQuests?.Count ?? 0
            };
        }
    }
}
