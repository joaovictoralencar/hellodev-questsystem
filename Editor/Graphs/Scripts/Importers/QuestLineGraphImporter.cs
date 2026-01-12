using System.Linq;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Importers
{
    /// <summary>
    /// ScriptedImporter for QuestLineGraph assets (.questline files).
    /// Converts the graph to a QuestLine_SO runtime asset on import.
    /// </summary>
    /// <remarks>
    /// Following Graph Toolkit's Visual Novel sample pattern:
    /// - Graph is loaded via GraphDatabase (Graph Toolkit handles graph storage internally)
    /// - Graph is converted to runtime ScriptableObject (QuestLine_SO)
    /// - Only the runtime asset is added to the import context
    /// </remarks>
    [ScriptedImporter(1, QuestLineGraph.AssetExtension)]
    public class QuestLineGraphImporter : ScriptedImporter
    {
        /// <summary>
        /// Whether to recursively convert embedded QuestGraph subgraphs.
        /// </summary>
        [SerializeField]
        [Tooltip("If enabled, embedded QuestGraph references will also be converted to Quest_SO.")]
        private bool convertSubgraphs = true;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load the graph using Graph Toolkit's database
            var graph = GraphDatabase.LoadGraphForImporter<QuestLineGraph>(ctx.assetPath);

            if (graph == null)
            {
                ctx.LogImportError($"Failed to load QuestLineGraph asset: {ctx.assetPath}");
                return;
            }

            // Validate: Must have a start node
            var startNode = graph.GetNodes().OfType<QuestLineStartNode>().FirstOrDefault();
            if (startNode == null)
            {
                ctx.LogImportWarning("QuestLineGraph has no QuestLineStartNode - cannot generate QuestLine_SO");
                return;
            }

            // Convert the graph to a runtime QuestLine_SO
            var converter = new GraphToQuestLineConverter();

            // Validate first
            if (!converter.ValidateForExport(graph, out var errors))
            {
                foreach (var error in errors)
                {
                    ctx.LogImportWarning($"Validation: {error}");
                }
                // Continue anyway - create what we can
            }

            // Set up conversion context
            var context = new ConversionContext
            {
                ConvertSubgraphs = convertSubgraphs,
                OutputFolder = System.IO.Path.GetDirectoryName(ctx.assetPath)
            };

            // Convert
            var questLine = converter.Export(graph, null, context);

            if (questLine == null)
            {
                foreach (var error in context.Errors)
                {
                    ctx.LogImportError(error);
                }
                return;
            }

            // Set the name to match the graph
            questLine.name = $"{graph.DevName}_QuestLine";

            // Log any warnings from conversion
            foreach (var warning in context.Warnings)
            {
                ctx.LogImportWarning(warning);
            }

            // Add the runtime asset and set it as the main object
            // This allows the .questline file to be used wherever a QuestLine_SO is expected
            ctx.AddObjectToAsset("QuestLine", questLine);
            ctx.SetMainObject(questLine);
        }
    }
}
