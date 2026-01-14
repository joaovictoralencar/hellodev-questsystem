using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Converters
{
    /// <summary>
    /// Interface for converting between graph assets and ScriptableObjects.
    /// </summary>
    /// <typeparam name="TGraph">The graph type.</typeparam>
    /// <typeparam name="TAsset">The ScriptableObject type.</typeparam>
    public interface IGraphConverter<TGraph, TAsset> 
        where TGraph : Graph
        where TAsset : ScriptableObject
    {
        /// <summary>
        /// Exports a graph to a ScriptableObject.
        /// Creates a new asset if existing is null, otherwise updates the existing asset.
        /// </summary>
        /// <param name="graph">The graph to export.</param>
        /// <param name="existing">Optional existing asset to update.</param>
        /// <returns>The exported ScriptableObject.</returns>
        TAsset Export(TGraph graph, TAsset existing = null);

        /// <summary>
        /// Validates the graph before export.
        /// </summary>
        /// <param name="graph">The graph to validate.</param>
        /// <param name="errors">List of validation errors if any.</param>
        /// <returns>True if valid, false otherwise.</returns>
        bool ValidateForExport(TGraph graph, out List<string> errors);
    }

    /// <summary>
    /// Context for graph conversion operations.
    /// Tracks state during recursive conversion of subgraphs.
    /// </summary>
    public class ConversionContext
    {
        /// <summary>
        /// Cache of already converted assets to avoid duplicate conversion.
        /// Key: Graph asset path, Value: Converted ScriptableObject.
        /// </summary>
        public Dictionary<string, ScriptableObject> ConvertedAssets { get; } = new();

        /// <summary>
        /// Folder path where new assets should be created.
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// Whether to overwrite existing assets.
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;

        /// <summary>
        /// Whether to recursively convert subgraphs.
        /// </summary>
        public bool ConvertSubgraphs { get; set; } = true;

        /// <summary>
        /// Current depth of recursive conversion.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Maximum depth for recursive conversion (prevents infinite loops).
        /// </summary>
        public int MaxDepth { get; set; } = 10;

        /// <summary>
        /// Errors encountered during conversion.
        /// </summary>
        public List<string> Errors { get; } = new();

        /// <summary>
        /// Warnings encountered during conversion.
        /// </summary>
        public List<string> Warnings { get; } = new();

        /// <summary>
        /// Check if we can convert subgraphs at current depth.
        /// </summary>
        public bool CanConvertSubgraphs => ConvertSubgraphs && Depth < MaxDepth;

        /// <summary>
        /// Add an error message.
        /// </summary>
        public void AddError(string message)
        {
            Errors.Add($"[Depth {Depth}] {message}");
        }

        /// <summary>
        /// Add a warning message.
        /// </summary>
        public void AddWarning(string message)
        {
            Warnings.Add($"[Depth {Depth}] {message}");
        }

        /// <summary>
        /// Try to get a cached converted asset.
        /// </summary>
        public bool TryGetCachedAsset<T>(string graphPath, out T asset) where T : ScriptableObject
        {
            if (ConvertedAssets.TryGetValue(graphPath, out var cached) && cached is T typedAsset)
            {
                asset = typedAsset;
                return true;
            }

            asset = null;
            return false;
        }

        /// <summary>
        /// Cache a converted asset.
        /// </summary>
        public void CacheAsset(string graphPath, ScriptableObject asset)
        {
            ConvertedAssets[graphPath] = asset;
        }
    }
}
