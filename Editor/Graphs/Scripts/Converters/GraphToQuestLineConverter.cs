using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
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
        /// Handles QuestChoiceNode for branching by following all output paths.
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

                    // Follow the "Then" port to the next quest or branch node
                    var thenNode = GraphTraversalUtility.GetNextNode(current, "Then");
                    if (thenNode != null && !visited.Contains(thenNode))
                    {
                        queue.Enqueue(thenNode);
                    }
                }
                else if (current is QuestChoiceNode choiceNode)
                {
                    // Handle quest branching - follow all output paths
                    var outputCount = choiceNode.OutputCount;

                    if (outputCount == 1)
                    {
                        // Single output mode
                        var targetNode = GraphTraversalUtility.GetNextNode(current, "Target");
                        if (targetNode != null && !visited.Contains(targetNode))
                        {
                            queue.Enqueue(targetNode);
                        }
                    }
                    else
                    {
                        // Multiple output mode - follow all paths
                        for (int i = 0; i < outputCount; i++)
                        {
                            var targetNode = GraphTraversalUtility.GetNextNode(current, $"Target{i}");
                            if (targetNode != null && !visited.Contains(targetNode))
                            {
                                queue.Enqueue(targetNode);
                            }
                        }

                        // Also follow the Default path
                        var defaultNode = GraphTraversalUtility.GetNextNode(current, "Default");
                        if (defaultNode != null && !visited.Contains(defaultNode))
                        {
                            queue.Enqueue(defaultNode);
                        }
                    }
                }
            }

            return quests;
        }

        /// <summary>
        /// Resolves a QuestNode to a Quest_SO.
        /// Handles both Asset Mode (returns referenced asset) and Define Mode (builds from inline data).
        /// </summary>
        private Quest_SO ResolveQuestReference(QuestNode questNode)
        {
            // Asset Mode: Return the referenced Quest_SO
            if (questNode.UseQuestAsset)
            {
                if (questNode.QuestAsset != null)
                {
                    return questNode.QuestAsset;
                }

                _context.AddWarning($"Quest node '{questNode.DisplayName}' has no Quest Asset assigned");
                return null;
            }

            // Define Mode: Build Quest_SO from inline data
            return BuildQuestFromDefineMode(questNode);
        }

        /// <summary>
        /// Builds a Quest_SO from a QuestNode in Define Mode.
        /// </summary>
        private Quest_SO BuildQuestFromDefineMode(QuestNode questNode)
        {
            var quest = ScriptableObject.CreateInstance<Quest_SO>();
            var so = new SerializedObject(quest);

            // Copy identity fields from QuestNode
            so.FindProperty("devName").stringValue = questNode.DevName;
            so.FindProperty("questId").stringValue = questNode.QuestId;
            so.FindProperty("questType").objectReferenceValue = questNode.QuestType;
            so.FindProperty("recommendedLevel").intValue = questNode.RecommendedLevel;

            // Copy display fields
            CopyLocalizedStringToProperty(so.FindProperty("displayName"), questNode.DisplayNameLocalized);
            CopyLocalizedStringToProperty(so.FindProperty("questDescription"), questNode.Description);
            CopyLocalizedStringToProperty(so.FindProperty("questLocation"), questNode.Location);
            so.FindProperty("questSprite").objectReferenceValue = questNode.QuestSprite;

            // Copy conditions
            CopyConditionList(so, "startConditions", questNode);
            CopyConditionList(so, "failureConditions", questNode);
            CopyConditionList(so, "globalTaskFailureConditions", questNode);

            // Copy rewards
            CopyRewardListFromQuestNode(so, "rewards", questNode);

            // Build stages from "Stages" output port connections
            BuildStagesFromFlow(so, questNode);

            so.ApplyModifiedPropertiesWithoutUndo();

            // Set name for debugging
            quest.name = $"InlineQuest_{questNode.DevName}";

            return quest;
        }

        /// <summary>
        /// Builds stages by traversing from the QuestNode's "Stages" output port.
        /// </summary>
        private void BuildStagesFromFlow(SerializedObject questSO, QuestNode questNode)
        {
            var stagesProperty = questSO.FindProperty("stages");
            stagesProperty.ClearArray();

            // Collect stages by following the "Stages" output port
            var stageNodes = CollectStagesFromFlow(questNode);

            foreach (var stageNode in stageNodes)
            {
                stagesProperty.InsertArrayElementAtIndex(stagesProperty.arraySize);
                var stageProperty = stagesProperty.GetArrayElementAtIndex(stagesProperty.arraySize - 1);
                BuildStageFromNode(stageProperty, stageNode);
            }
        }

        /// <summary>
        /// Collects StageNodes by traversing from the QuestNode's "Stages" output.
        /// </summary>
        private List<StageNode> CollectStagesFromFlow(QuestNode questNode)
        {
            var stages = new List<StageNode>();
            var visited = new HashSet<INode>();

            // Start from the "Stages" output port
            var firstStage = GraphTraversalUtility.GetNextNode(questNode, "Stages");
            if (firstStage == null)
                return stages;

            var queue = new Queue<INode>();
            queue.Enqueue(firstStage);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == null || visited.Contains(current))
                    continue;

                visited.Add(current);

                if (current is StageNode stageNode)
                {
                    stages.Add(stageNode);

                    // Follow the "Then" port to the next stage
                    var nextStage = GraphTraversalUtility.GetNextNode(current, "Then");
                    if (nextStage != null && !visited.Contains(nextStage))
                    {
                        queue.Enqueue(nextStage);
                    }

                    // Also follow "Choices" port for branching paths
                    if (stageNode.HasPlayerChoices)
                    {
                        var choiceNodes = GraphTraversalUtility.GetAllConnectedNodes(current, "Choices");
                        foreach (var choiceNode in choiceNodes)
                        {
                            // Follow choice targets
                            if (choiceNode is ChoiceNode choice)
                            {
                                var choiceTarget = GraphTraversalUtility.GetNextNode(choiceNode, "Target");
                                if (choiceTarget != null && !visited.Contains(choiceTarget))
                                {
                                    queue.Enqueue(choiceTarget);
                                }
                            }
                        }
                    }
                }
            }

            // Sort by stage index
            return stages.OrderBy(s => s.StageIndex).ToList();
        }

        /// <summary>
        /// Builds a single stage from a StageNode.
        /// </summary>
        private void BuildStageFromNode(SerializedProperty stageProperty, StageNode stageNode)
        {
            // Identity
            stageProperty.FindPropertyRelative("stageIndex").intValue = stageNode.StageIndex;
            stageProperty.FindPropertyRelative("stageName").stringValue = stageNode.StageName;

            // Display
            CopyLocalizedStringToProperty(stageProperty.FindPropertyRelative("journalEntry"), stageNode.JournalEntry);
            stageProperty.FindPropertyRelative("stageIcon").objectReferenceValue = stageNode.StageIcon;

            // Flags
            stageProperty.FindPropertyRelative("isOptional").boolValue = stageNode.IsOptional;
            stageProperty.FindPropertyRelative("isHidden").boolValue = stageNode.IsHidden;
            stageProperty.FindPropertyRelative("isTerminal").boolValue = stageNode.IsTerminal;

            // Task groups - find connected TaskGroupContextNodes
            BuildTaskGroupsFromStageNode(stageProperty.FindPropertyRelative("taskGroups"), stageNode);

            // Transitions
            BuildTransitionsFromStageNode(stageProperty.FindPropertyRelative("transitions"), stageNode);
        }

        /// <summary>
        /// Builds task groups from a StageNode's connected TaskGroupContextNodes.
        /// </summary>
        private void BuildTaskGroupsFromStageNode(SerializedProperty taskGroupsProperty, StageNode stageNode)
        {
            taskGroupsProperty.ClearArray();

            // Find TaskGroupContextNodes whose "Then" output connects to this stage's TaskGroupsInput
            foreach (var node in _allNodes.OfType<TaskGroupContextNode>())
            {
                try
                {
                    var thenPort = node.GetOutputPortByName("Then");
                    if (thenPort != null && thenPort.isConnected)
                    {
                        var connectedNode = thenPort.firstConnectedPort?.GetNode();
                        if (connectedNode == stageNode)
                        {
                            taskGroupsProperty.InsertArrayElementAtIndex(taskGroupsProperty.arraySize);
                            var tgProperty = taskGroupsProperty.GetArrayElementAtIndex(taskGroupsProperty.arraySize - 1);
                            BuildTaskGroupFromContext(tgProperty, node);
                        }
                    }
                }
                catch
                {
                    // Port access failed
                }
            }
        }

        /// <summary>
        /// Builds a task group from a TaskGroupContextNode.
        /// </summary>
        private void BuildTaskGroupFromContext(SerializedProperty tgProperty, TaskGroupContextNode tgContext)
        {
            tgProperty.FindPropertyRelative("groupName").stringValue = tgContext.GroupName;
            tgProperty.FindPropertyRelative("executionMode").enumValueIndex = (int)tgContext.ExecutionMode;
            tgProperty.FindPropertyRelative("requiredCount").intValue = tgContext.RequiredCount;

            // Extract tasks from blocks inside the context node
            var tasksProperty = tgProperty.FindPropertyRelative("tasks");
            tasksProperty.ClearArray();

            var taskBlocks = tgContext.blockNodes.OfType<TaskBlockBase>().ToList();
            foreach (var taskBlock in taskBlocks)
            {
                Task_SO taskAsset = taskBlock.IsDefineMode ? taskBlock.CreateTaskAsset() : taskBlock.TaskAsset;

                if (taskAsset != null)
                {
                    tasksProperty.InsertArrayElementAtIndex(tasksProperty.arraySize);
                    tasksProperty.GetArrayElementAtIndex(tasksProperty.arraySize - 1).objectReferenceValue = taskAsset;
                }
            }
        }

        /// <summary>
        /// Builds transitions from a StageNode's flow connections.
        /// </summary>
        private void BuildTransitionsFromStageNode(SerializedProperty transitionsProperty, StageNode stageNode)
        {
            transitionsProperty.ClearArray();

            // Terminal stages have no transitions
            if (stageNode.IsTerminal)
                return;

            // Then transition (success path)
            var thenTarget = GraphTraversalUtility.GetConnectedStageIndex(stageNode, "Then");
            if (thenTarget >= 0)
            {
                AddTransition(transitionsProperty, thenTarget, TransitionTrigger.OnGroupsComplete);
            }

            // Player choices
            if (stageNode.HasPlayerChoices)
            {
                var choiceNodes = GraphTraversalUtility.GetAllConnectedNodes(stageNode, "Choices")
                    .OfType<ChoiceNode>()
                    .OrderByDescending(c => c.Priority)
                    .ToList();

                foreach (var choice in choiceNodes)
                {
                    AddChoiceTransition(transitionsProperty, choice);
                }
            }
        }

        /// <summary>
        /// Adds a stage transition.
        /// </summary>
        private void AddTransition(SerializedProperty transitionsProperty, int targetIndex, TransitionTrigger trigger)
        {
            transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            var transProperty = transitionsProperty.GetArrayElementAtIndex(transitionsProperty.arraySize - 1);

            transProperty.FindPropertyRelative("targetStageIndex").intValue = targetIndex;
            transProperty.FindPropertyRelative("trigger").enumValueIndex = (int)trigger;
        }

        /// <summary>
        /// Adds a player choice transition.
        /// </summary>
        private void AddChoiceTransition(SerializedProperty transitionsProperty, ChoiceNode choice)
        {
            var targetIndex = GraphTraversalUtility.GetConnectedStageIndex(choice, "Target");
            if (targetIndex < 0)
            {
                _context.AddWarning($"Choice '{choice.ChoiceId}' has no target stage connected");
                return;
            }

            transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            var transProperty = transitionsProperty.GetArrayElementAtIndex(transitionsProperty.arraySize - 1);

            transProperty.FindPropertyRelative("targetStageIndex").intValue = targetIndex;
            transProperty.FindPropertyRelative("trigger").enumValueIndex = (int)TransitionTrigger.PlayerChoice;
            transProperty.FindPropertyRelative("priority").intValue = choice.Priority;
            transProperty.FindPropertyRelative("isPlayerChoice").boolValue = true;
            transProperty.FindPropertyRelative("choiceId").stringValue = choice.ChoiceId;

            CopyLocalizedStringToProperty(transProperty.FindPropertyRelative("choiceText"), choice.ChoiceText);
            CopyLocalizedStringToProperty(transProperty.FindPropertyRelative("choiceTooltip"), choice.ChoiceTooltip);
            transProperty.FindPropertyRelative("choiceIcon").objectReferenceValue = choice.ChoiceIcon;
        }

        /// <summary>
        /// Copies conditions from a ConditionContextNode to the Quest_SO.
        /// Uses the same pattern as BuildTaskGroupsFromStageNode for reliable node access.
        /// </summary>
        private void CopyConditionList(SerializedObject so, string propertyName, QuestNode questNode)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
                return;

            prop.ClearArray();

            // Map property name to input port name on QuestNode
            string inputPortName = propertyName switch
            {
                "startConditions" => "TriggerConditionsInput",
                "failureConditions" => "FailConditionsInput",
                "globalTaskFailureConditions" => "GlobalTaskFailureInput",
                _ => null
            };

            if (inputPortName == null)
                return;

            // Find ConditionContextNode whose "Then" output connects to questNode's input port
            // This pattern is more reliable than traversing from input to source
            foreach (var conditionContext in _allNodes.OfType<ConditionContextNode>())
            {
                try
                {
                    var thenPort = conditionContext.GetOutputPortByName("Then"); 
                    if (thenPort == null || !thenPort.isConnected)
                        continue;

                    var connectedPort = thenPort.firstConnectedPort;
                    var connectedNode = connectedPort?.GetNode();

                    // Check if the connected port's name matches the target input port
                    if (connectedNode == questNode && connectedPort?.name == inputPortName)
                    {
                        // Found the matching ConditionContextNode - extract conditions
                        var conditionBlocks = conditionContext.blockNodes.OfType<ConditionBlock>().ToList();
                        foreach (var block in conditionBlocks)
                        {
                            if (block.ConditionAsset != null)
                            {
                                prop.InsertArrayElementAtIndex(prop.arraySize);
                                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = block.ConditionAsset;
                            }
                        }
                        return; // Found the context, no need to continue
                    }
                }
                catch
                {
                    // Port access failed
                }
            }
        }

        /// <summary>
        /// Copies rewards from a RewardContextNode to the Quest_SO.
        /// Uses the same pattern as BuildTaskGroupsFromStageNode for reliable node access.
        /// </summary>
        private void CopyRewardListFromQuestNode(SerializedObject so, string propertyName, QuestNode questNode)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
                return;

            prop.ClearArray();

            // Find RewardContextNode whose "Then" output connects to questNode's RewardsInput
            foreach (var rewardContext in _allNodes.OfType<RewardContextNode>())
            {
                try
                {
                    var thenPort = rewardContext.GetOutputPortByName("Then");
                    if (thenPort == null || !thenPort.isConnected)
                        continue;

                    var connectedPort = thenPort.firstConnectedPort;
                    var connectedNode = connectedPort?.GetNode();

                    // Check if connected to this questNode's RewardsInput port
                    if (connectedNode == questNode && connectedPort?.name == "RewardsInput")
                    {
                        // Found the matching RewardContextNode - extract rewards
                        foreach (var block in rewardContext.blockNodes.OfType<RewardBlock>())
                        {
                            if (block.IsValid)
                            {
                                prop.InsertArrayElementAtIndex(prop.arraySize);
                                var rewardProp = prop.GetArrayElementAtIndex(prop.arraySize - 1);

                                var typeProp = rewardProp.FindPropertyRelative("RewardType");
                                var amountProp = rewardProp.FindPropertyRelative("Amount");

                                if (typeProp != null)
                                    typeProp.objectReferenceValue = block.RewardType;
                                if (amountProp != null)
                                    amountProp.intValue = block.Amount;
                            }
                        }
                        return; // Found the context, no need to continue
                    }
                }
                catch
                {
                    // Port access failed
                }
            }
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
                // Unity.Localization serializes as m_TableCollectionName with format "GUID:xxxxx"
                var tableCollectionName = tableRef.FindPropertyRelative("m_TableCollectionName");
                if (tableCollectionName != null)
                {
                    // Format: "GUID:" + GUID without dashes (e.g., "GUID:05b8775364730764ab5bf1891aa1cb86")
                    tableCollectionName.stringValue = "GUID:" + source.TableReference.TableCollectionNameGuid.ToString("N");
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
