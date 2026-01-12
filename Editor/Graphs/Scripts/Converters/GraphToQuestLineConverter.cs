using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Converters
{
    /// <summary>
    /// Converts a QuestLineGraph to a QuestLine_SO ScriptableObject.
    /// Handles recursive conversion of embedded QuestGraph subgraphs.
    /// </summary>
    public class GraphToQuestLineConverter : IGraphConverter<QuestLineGraph, QuestLine_SO>
    {
        #region Conversion Context

        private ConversionContext _context;
        private GraphToQuestConverter _questConverter;
        private List<INode> _allNodes;

        #endregion

        #region IGraphConverter Implementation

        /// <summary>
        /// Exports a QuestLineGraph to a QuestLine_SO ScriptableObject.
        /// </summary>
        public QuestLine_SO Export(QuestLineGraph graph, QuestLine_SO existing = null)
        {
            return Export(graph, existing, new ConversionContext());
        }

        /// <summary>
        /// Exports a QuestLineGraph with custom context.
        /// </summary>
        public QuestLine_SO Export(QuestLineGraph graph, QuestLine_SO existing, ConversionContext context)
        {
            _context = context ?? new ConversionContext();
            _questConverter = new GraphToQuestConverter();

            if (graph == null)
            {
                _context.AddError("Cannot export null graph");
                return null;
            }

            // Validate before export
            if (!ValidateForExport(graph, out var errors))
            {
                foreach (var error in errors)
                {
                    _context.AddError(error);
                }
                return null;
            }

            try
            {
                // Create or use existing asset
                var questLine = existing != null ? existing : ScriptableObject.CreateInstance<QuestLine_SO>();

                // Collect nodes
                _allNodes = graph.GetNodes().ToList();

                // Build QuestLine_SO structure
                BuildQuestLineData(graph, questLine);

                return questLine;
            }
            catch (Exception ex)
            {
                _context.AddError($"Export failed: {ex.Message}");
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Validates the graph before export.
        /// </summary>
        public bool ValidateForExport(QuestLineGraph graph, out List<string> errors)
        {
            errors = new List<string>();

            var validationService = new GraphValidationService();
            var results = validationService.ValidateQuestLineGraph(graph);

            // Only errors block export
            var errorResults = results.Where(r => r.Severity == ValidationSeverity.Error).ToList();
            errors.AddRange(errorResults.Select(r => r.Message));

            return errorResults.Count == 0;
        }

        #endregion

        #region Building QuestLine Data

        private void BuildQuestLineData(QuestLineGraph graph, QuestLine_SO questLine)
        {
            var so = new SerializedObject(questLine);

            // Copy identity fields
            so.FindProperty("devName").stringValue = graph.DevName;
            so.FindProperty("questLineId").stringValue = graph.QuestLineId;

            // Copy display fields
            CopyLocalizedString(so, "displayName", graph.DisplayName);
            CopyLocalizedString(so, "description", graph.Description);
            // Note: QuestLineGraph doesn't have icon field, skip if not present

            // Copy settings
            so.FindProperty("requireSequentialCompletion").boolValue = graph.RequireSequentialCompletion;
            so.FindProperty("failOnAnyQuestFailed").boolValue = graph.FailOnAnyQuestFailed;

            // Copy prerequisite line reference
            so.FindProperty("prerequisiteLine").objectReferenceValue = graph.PrerequisiteLine;

            // Copy rewards
            CopyRewardList(so, "completionRewards", graph.CompletionRewards);

            // Build quests list from graph
            BuildQuestsList(so, graph);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void BuildQuestsList(SerializedObject questLineSO, QuestLineGraph graph)
        {
            var questsProperty = questLineSO.FindProperty("quests");
            questsProperty.ClearArray();

            // Find the start node
            var startNode = _allNodes.OfType<QuestLineStartNode>().FirstOrDefault();
            if (startNode == null)
            {
                _context.AddWarning("No QuestLineStartNode found - quests list will be empty");
                return;
            }

            // Traverse from start to collect quests in order
            var questRefs = CollectQuestsInOrder(startNode);

            foreach (var questRef in questRefs)
            {
                var questSO = ResolveQuestReference(questRef);
                if (questSO != null)
                {
                    questsProperty.InsertArrayElementAtIndex(questsProperty.arraySize);
                    questsProperty.GetArrayElementAtIndex(questsProperty.arraySize - 1).objectReferenceValue = questSO;
                }
            }
        }

        /// <summary>
        /// Collects QuestNodes in execution order by traversing the graph.
        /// </summary>
        private List<QuestNode> CollectQuestsInOrder(QuestLineStartNode startNode)
        {
            var quests = new List<QuestNode>();
            var visited = new HashSet<INode>();

            // BFS traversal following port connections
            var queue = new Queue<INode>();

            // Get first quest from start node
            var firstQuest = GraphTraversalUtility.GetNextNode(startNode, "FirstQuest");
            if (firstQuest != null)
            {
                queue.Enqueue(firstQuest);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == null || visited.Contains(current))
                    continue;

                visited.Add(current);

                if (current is QuestNode questNode)
                {
                    quests.Add(questNode);

                    // Follow the "Then" port to the next quest
                    var thenNode = GraphTraversalUtility.GetNextNode(current, "Then");
                    if (thenNode != null && !visited.Contains(thenNode))
                    {
                        queue.Enqueue(thenNode);
                    }
                }
            }

            return quests;
        }

        /// <summary>
        /// Resolves a QuestNode to a Quest_SO.
        /// </summary>
        private Quest_SO ResolveQuestReference(QuestNode questNode)
        {
            if (questNode.QuestAsset != null)
            {
                return questNode.QuestAsset;
            }

            _context.AddWarning($"Quest node '{questNode.DisplayName}' has no Quest Asset assigned");
            return null;
        }

        /// <summary>
        /// Converts a QuestGraph to Quest_SO, handling caching.
        /// </summary>
        private Quest_SO ConvertQuestGraph(QuestGraph questGraph, string displayName)
        {
            if (questGraph == null)
            {
                _context.AddWarning($"Null QuestGraph reference for '{displayName}'");
                return null;
            }

            // Check depth limit
            if (!_context.CanConvertSubgraphs)
            {
                _context.AddWarning($"Maximum subgraph depth reached, skipping '{displayName}'");
                return null;
            }

            // Use DevName as cache key since Graph is not a ScriptableObject
            var cacheKey = $"QuestGraph:{questGraph.DevName}";
            if (_context.TryGetCachedAsset<Quest_SO>(cacheKey, out var cached))
            {
                return cached;
            }

            // Check if there's already a target asset
            if (questGraph.TargetAsset != null)
            {
                return questGraph.TargetAsset;
            }

            // Convert the subgraph
            _context.Depth++;
            try
            {
                var quest = _questConverter.Export(questGraph, null, _context);
                if (quest != null)
                {
                    _context.CacheAsset(cacheKey, quest);

                    // If we need to save the asset
                    if (!string.IsNullOrEmpty(_context.OutputFolder))
                    {
                        var assetPath = $"{_context.OutputFolder}/{questGraph.DevName}.asset";
                        AssetDatabase.CreateAsset(quest, assetPath);
                    }
                }
                return quest;
            }
            finally
            {
                _context.Depth--;
            }
        }

        #endregion

        #region Helper Methods

        private void CopyLocalizedString(SerializedObject so, string propertyName, UnityEngine.Localization.LocalizedString source)
        {
            var prop = so.FindProperty(propertyName);
            CopyLocalizedStringToProperty(prop, source);
        }

        private void CopyLocalizedStringToProperty(SerializedProperty prop, UnityEngine.Localization.LocalizedString source)
        {
            if (prop == null || source == null)
                return;

            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef != null && source.TableReference.TableCollectionNameGuid != Guid.Empty)
            {
                var tableGuid = tableRef.FindPropertyRelative("m_TableCollectionNameGuid");
                if (tableGuid != null)
                {
                    tableGuid.stringValue = source.TableReference.TableCollectionNameGuid.ToString();
                }
            }

            if (entryRef != null && source.TableEntryReference.KeyId != 0)
            {
                var keyId = entryRef.FindPropertyRelative("m_KeyId");
                if (keyId != null)
                {
                    keyId.longValue = source.TableEntryReference.KeyId;
                }
            }
        }

        private void CopyRewardList(SerializedObject so, string propertyName, List<RewardInstance> rewards)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
                return;

            prop.ClearArray();

            if (rewards == null)
                return;

            foreach (var reward in rewards)
            {
                prop.InsertArrayElementAtIndex(prop.arraySize);
                var rewardProp = prop.GetArrayElementAtIndex(prop.arraySize - 1);

                var typeProp = rewardProp.FindPropertyRelative("rewardType");
                var amountProp = rewardProp.FindPropertyRelative("amount");

                if (typeProp != null)
                    typeProp.objectReferenceValue = reward.RewardType;
                if (amountProp != null)
                    amountProp.intValue = reward.Amount;
            }
        }

        #endregion

        #region Context Access

        /// <summary>
        /// Gets the conversion context with errors and warnings.
        /// </summary>
        public ConversionContext Context => _context;

        #endregion
    }
}
