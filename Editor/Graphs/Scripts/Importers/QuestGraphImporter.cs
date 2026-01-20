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
    /// ScriptedImporter for QuestGraph assets (.quest files).
    /// Converts the graph to a Quest_SO runtime asset on import.
    /// </summary>
    /// <remarks>
    /// Following Graph Toolkit's Visual Novel sample pattern:
    /// - Graph is loaded via GraphDatabase (Graph Toolkit handles graph storage internally)
    /// - Graph is converted to runtime ScriptableObject (Quest_SO)
    /// - Only the runtime asset is added to the import context
    /// </remarks>
    // NOTE: Using string literal instead of QuestGraph.AssetExtension to avoid
    // circular type reference that crashes Unity when QuestGraph is both a
    // subgraph and supports subgraphs.
    [ScriptedImporter(1, "quest")]
    public class QuestGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load the graph using Graph Toolkit's database
            var graph = GraphDatabase.LoadGraphForImporter<QuestGraph>(ctx.assetPath);

            if (graph == null)
            {
                ctx.LogImportError($"Failed to load QuestGraph asset: {ctx.assetPath}");
                return;
            }

            // Validate: Must have a start node
            var startNode = graph.GetNodes().OfType<QuestStartNode>().FirstOrDefault();
            if (startNode == null)
            {
                ctx.LogImportWarning("QuestGraph has no QuestStartNode - cannot generate Quest_SO");
                return;
            }

            // Convert the graph to a runtime Quest_SO
            var converter = new GraphToQuestConverter();

            // Validate first
            if (!converter.ValidateForExport(graph, out var errors))
            {
                foreach (var error in errors)
                {
                    ctx.LogImportWarning($"Validation: {error}");
                }
                // Continue anyway - create what we can
            }

            // Convert
            var context = new ConversionContext();
            var quest = converter.Export(graph, null, context);

            if (quest == null)
            {
                foreach (var error in context.Errors)
                {
                    ctx.LogImportError(error);
                }
                return;
            }

            // Set the name to match the graph
            quest.name = $"{graph.DevName}_Quest";

            // Log any warnings from conversion
            foreach (var warning in context.Warnings)
            {
                ctx.LogImportWarning(warning);
            }

            // Add the runtime asset and set it as the main object
            // This allows the .quest file to be used wherever a Quest_SO is expected
            ctx.AddObjectToAsset("Quest", quest);
            ctx.SetMainObject(quest);

            // Add inline task assets as sub-assets
            // These are Task_SO instances created from inline task node definitions (Define mode)
            var inlineTasks = converter.CreatedInlineTasks;
            for (int i = 0; i < inlineTasks.Count; i++)
            {
                var inlineTask = inlineTasks[i];
                if (inlineTask != null)
                {
                    ctx.AddObjectToAsset($"{i}_{inlineTask.name}", inlineTask);
                }
            }

        }
    }
}
