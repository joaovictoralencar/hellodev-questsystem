using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using IVariable = UnityEngine.Localization.SmartFormat.PersistentVariables.IVariable;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Converters
{
    /// <summary>
    /// Converts a QuestGraph to a Quest_SO ScriptableObject.
    /// Uses a two-pass algorithm:
    /// 1. Collect all nodes and build lookup tables
    /// 2. Build relationships (stages, transitions, task groups)
    /// </summary>
    public class GraphToQuestConverter : IGraphConverter<QuestGraph, Quest_SO>
    {
        #region Conversion Context

        private ConversionContext _context;
        private QuestGraph _currentGraph;
        private List<INode> _allNodes;
        private Dictionary<int, StageNode> _stageNodeLookup;
        private Dictionary<int, ISubgraphNode> _stageSubgraphLookup;
        private Dictionary<StageNode, List<TaskGroupContextNode>> _stageTaskGroups;
        private Dictionary<ISubgraphNode, List<INode>> _stageSubgraphTaskGroups;
        private Dictionary<StageNode, List<ChoiceNode>> _stageChoices;
        private List<RewardNode> _rewardNodes;

        // Track inline tasks created during conversion for sub-asset registration
        private List<Task_SO> _createdInlineTasks = new();

        // Store LocalizedString mappings for reflection-based fallback
        // Key: stage index (in order they appear in stages list), Value: journalEntry LocalizedString
        private Dictionary<int, LocalizedString> _stageJournalEntries = new();

        #endregion

        #region Inline Task Access

        /// <summary>
        /// Gets all inline task assets created during conversion.
        /// Call after Export() to add these to the import context as sub-assets.
        /// </summary>
        public IReadOnlyList<Task_SO> CreatedInlineTasks => _createdInlineTasks;

        #endregion

        #region IGraphConverter Implementation

        /// <summary>
        /// Exports a QuestGraph to a Quest_SO ScriptableObject.
        /// </summary>
        /// <param name="graph">The graph to export.</param>
        /// <param name="existing">Optional existing asset to update.</param>
        /// <returns>The exported Quest_SO.</returns>
        public Quest_SO Export(QuestGraph graph, Quest_SO existing = null)
        {
            return Export(graph, existing, new ConversionContext());
        }

        /// <summary>
        /// Exports a QuestGraph with custom context.
        /// </summary>
        public Quest_SO Export(QuestGraph graph, Quest_SO existing, ConversionContext context)
        {
            _context = context ?? new ConversionContext();

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
                // Clear data from previous conversions
                _createdInlineTasks.Clear();
                _stageJournalEntries.Clear();

                // Create or use existing asset
                var quest = existing != null ? existing : ScriptableObject.CreateInstance<Quest_SO>();

                // Pass 1: Collect all nodes
                CollectNodes(graph);

                // Pass 2: Build Quest_SO structure
                BuildQuestData(graph, quest);

                // Pass 3: Apply LocalizedStrings via reflection (fallback for SerializedProperty issues)
                ApplyLocalizedStringsViaReflection(quest);

                return quest;
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
        public bool ValidateForExport(QuestGraph graph, out List<string> errors)
        {
            errors = new List<string>();

            var validationService = new GraphValidationService();
            var results = validationService.ValidateQuestGraph(graph);

            // Also check reachability
            var reachabilityAnalyzer = new GraphReachabilityAnalyzer();
            results.AddRange(reachabilityAnalyzer.ValidateReachability(graph));

            // Only errors block export, warnings are allowed
            var errorResults = results.Where(r => r.Severity == ValidationSeverity.Error).ToList();
            errors.AddRange(errorResults.Select(r => r.Message));

            return errorResults.Count == 0;
        }

        #endregion

        #region Pass 1: Node Collection

        private void CollectNodes(QuestGraph graph)
        {
            _currentGraph = graph;
            _allNodes = graph.GetNodes().ToList();

            // Build stage node lookup by index
            _stageNodeLookup = new Dictionary<int, StageNode>();
            foreach (var node in _allNodes.OfType<StageNode>())
            {
                if (!_stageNodeLookup.ContainsKey(node.StageIndex))
                {
                    _stageNodeLookup[node.StageIndex] = node;
                }
                else
                {
                    _context.AddWarning($"Duplicate stage index {node.StageIndex}, using first occurrence");
                }
            }

            // Build stage subgraph node lookup by index (native ISubgraphNode referencing StageGraph)
            _stageSubgraphLookup = new Dictionary<int, ISubgraphNode>();
            foreach (var node in _allNodes.OfType<ISubgraphNode>())
            {
                var stageGraph = node.GetSubgraph() as StageGraph;
                if (stageGraph == null)
                    continue;

                var index = stageGraph.StageIndex;
                if (!_stageNodeLookup.ContainsKey(index) && !_stageSubgraphLookup.ContainsKey(index))
                {
                    _stageSubgraphLookup[index] = node;
                }
                else
                {
                    _context.AddWarning($"Duplicate stage index {index} (subgraph), using first occurrence");
                }
            }

            // Build stage → task groups mapping
            _stageTaskGroups = new Dictionary<StageNode, List<TaskGroupContextNode>>();
            foreach (var stageNode in _stageNodeLookup.Values)
            {
                _stageTaskGroups[stageNode] = FindConnectedTaskGroupContexts(stageNode);
            }

            // Build stage subgraph → task groups mapping
            _stageSubgraphTaskGroups = new Dictionary<ISubgraphNode, List<INode>>();
            foreach (var subgraphNode in _stageSubgraphLookup.Values)
            {
                _stageSubgraphTaskGroups[subgraphNode] = FindConnectedTaskGroupsForSubgraph(subgraphNode);
            }

            // Build stage → choices mapping
            _stageChoices = new Dictionary<StageNode, List<ChoiceNode>>();
            foreach (var stageNode in _stageNodeLookup.Values)
            {
                if (stageNode.HasPlayerChoices)
                {
                    _stageChoices[stageNode] = FindConnectedChoices(stageNode);
                }
                else
                {
                    _stageChoices[stageNode] = new List<ChoiceNode>();
                }
            }

            // Collect RewardNodes
            _rewardNodes = _allNodes.OfType<RewardNode>().ToList();
        }

        /// <summary>
        /// Finds TaskGroupContextNodes connected to a StageNode.
        /// TaskGroups connect via TaskGroupContextNode.Then to StageNode's "TaskGroupsInput" port.
        /// </summary>
        private List<TaskGroupContextNode> FindConnectedTaskGroupContexts(StageNode stageNode)
        {
            var taskGroups = new List<TaskGroupContextNode>();

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
                            taskGroups.Add(node);
                        }
                    }
                }
                catch
                {
                    // Port access failed
                }
            }

            return taskGroups;
        }

        /// <summary>
        /// Finds TaskGroupContextNodes connected to a stage ISubgraphNode,
        /// or extracts them from within the stage subgraph itself.
        /// </summary>
        private List<INode> FindConnectedTaskGroupsForSubgraph(ISubgraphNode subgraphNode)
        {
            var taskGroups = new List<INode>();

            // First check for TaskGroupContextNodes inside the stage subgraph
            var stageGraph = subgraphNode.GetSubgraph() as StageGraph;
            if (stageGraph != null)
            {
                foreach (var node in stageGraph.GetNodes())
                {
                    if (node is TaskGroupContextNode tgContextNode)
                    {
                        taskGroups.Add(tgContextNode);
                    }
                    else if (node is ISubgraphNode innerSubgraph)
                    {
                        // Check if this inner subgraph references a TaskGroupGraph
                        if (innerSubgraph.GetSubgraph() is TaskGroupGraph)
                        {
                            taskGroups.Add(innerSubgraph);
                        }
                    }
                }
            }

            // Also check for TaskGroupContextNodes whose "Then" connects to this subgraph node
            foreach (var node in _allNodes.OfType<TaskGroupContextNode>())
            {
                try
                {
                    var thenPort = node.GetOutputPortByName("Then");
                    if (thenPort != null && thenPort.isConnected)
                    {
                        var connectedNode = thenPort.firstConnectedPort?.GetNode();
                        if (connectedNode == subgraphNode)
                        {
                            taskGroups.Add(node);
                        }
                    }
                }
                catch
                {
                    // Port access failed
                }
            }

            return taskGroups;
        }

        /// <summary>
        /// Finds ChoiceNodes connected to a StageNode's Choices port.
        /// </summary>
        private List<ChoiceNode> FindConnectedChoices(StageNode stageNode)
        {
            var choices = new List<ChoiceNode>();

            var connectedNodes = GraphTraversalUtility.GetAllConnectedNodes(stageNode, "Choices");
            foreach (var node in connectedNodes.OfType<ChoiceNode>())
            {
                choices.Add(node);
            }

            // Sort by priority (higher first)
            return choices.OrderByDescending(c => c.Priority).ToList();
        }

        #endregion

        #region Pass 2: Building Quest Data

        private void BuildQuestData(QuestGraph graph, Quest_SO quest)
        {
            var so = new SerializedObject(quest);

            // Check for QuestNode in Define Mode - use its values over graph fields
            var questNode = _allNodes.OfType<QuestNode>().FirstOrDefault();
            bool useQuestNode = questNode != null && !questNode.UseQuestAsset;

            // Copy identity fields - prefer QuestNode (Define mode) over graph fields
            var devName = useQuestNode ? questNode.DevName : graph.DevName;
            var questType = useQuestNode ? questNode.QuestType : graph.QuestType;
            var recommendedLevel = useQuestNode ? questNode.RecommendedLevel : graph.RecommendedLevel;

            so.FindProperty("devName").stringValue = devName;
            so.FindProperty("questId").stringValue = graph.QuestId;
            so.FindProperty("questType").objectReferenceValue = questType;
            so.FindProperty("recommendedLevel").intValue = recommendedLevel;

            // Copy display fields - prefer QuestNode (Define mode) over graph fields
            var displayName = useQuestNode ? questNode.DisplayNameLocalized : graph.DisplayName;
            var description = useQuestNode ? questNode.Description : graph.QuestDescription;
            var location = useQuestNode ? questNode.Location : graph.QuestLocation;
            var sprite = useQuestNode ? questNode.QuestSprite : graph.QuestSprite;

            CopyLocalizedString(so, "displayName", displayName);
            CopyLocalizedString(so, "questDescription", description);
            CopyLocalizedString(so, "questLocation", location);
            so.FindProperty("questSprite").objectReferenceValue = sprite;

            // Copy conditions - prefer QuestNode's context inputs over graph fields
            var startConditions = useQuestNode
                ? CollectConditionsFromQuestNode(questNode, "TriggerConditionsInput")
                : graph.StartConditions;
            var failureConditions = useQuestNode
                ? CollectConditionsFromQuestNode(questNode, "FailConditionsInput")
                : graph.FailureConditions;
            var globalTaskFailure = useQuestNode
                ? CollectConditionsFromQuestNode(questNode, "GlobalTaskFailureInput")
                : new List<HelloDev.Conditions.Condition_SO>();

            CopyConditionList(so, "startConditions", startConditions);
            CopyConditionList(so, "failureConditions", failureConditions);
            CopyConditionList(so, "globalTaskFailureConditions", globalTaskFailure);

            // Copy rewards - prefer QuestNode context, fall back to graph fields and legacy nodes
            var allRewards = new List<RewardInstance>();

            if (useQuestNode)
            {
                // QuestNode Define mode: get rewards from connected RewardContextNode
                allRewards.AddRange(CollectRewardsFromQuestNode(questNode));
            }
            else
            {
                // Fallback: Graph's serialized rewards list
                if (graph.Rewards != null)
                {
                    allRewards.AddRange(graph.Rewards);
                }

                // Legacy: RewardNode instances in the graph
                foreach (var rewardNode in _rewardNodes)
                {
                    if (rewardNode.Rewards != null)
                    {
                        foreach (var reward in rewardNode.Rewards)
                        {
                            if (reward.IsValid)
                            {
                                allRewards.Add(reward);
                            }
                        }
                    }
                }

                // Legacy: All RewardContextNodes in graph (when not using QuestNode)
                foreach (var rewardContext in _allNodes.OfType<RewardContextNode>())
                {
                    foreach (var block in rewardContext.blockNodes.OfType<RewardBlock>())
                    {
                        if (block.IsValid)
                        {
                            allRewards.Add(block.ToRewardInstance());
                        }
                    }
                }
            }

            CopyRewardList(so, "rewards", allRewards);

            // Build stages
            BuildStages(so);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void BuildStages(SerializedObject questSO)
        {
            var stagesProperty = questSO.FindProperty("stages");
            stagesProperty.ClearArray();

            // Combine all stage indices (inline + subgraph)
            var allStageIndices = _stageNodeLookup.Keys
                .Concat(_stageSubgraphLookup.Keys)
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            int arrayIndex = 0;
            foreach (var stageIndex in allStageIndices)
            {
                stagesProperty.InsertArrayElementAtIndex(stagesProperty.arraySize);
                var stageProperty = stagesProperty.GetArrayElementAtIndex(stagesProperty.arraySize - 1);

                // Check if it's an inline StageNode or a stage subgraph node
                if (_stageNodeLookup.TryGetValue(stageIndex, out var stageNode))
                {
                    BuildStage(stageProperty, stageNode, arrayIndex);
                }
                else if (_stageSubgraphLookup.TryGetValue(stageIndex, out var subgraphNode))
                {
                    BuildStageFromSubgraph(stageProperty, subgraphNode, arrayIndex);
                }
                arrayIndex++;
            }
        }

        private void BuildStage(SerializedProperty stageProperty, StageNode stageNode, int arrayIndex)
        {
            // Identity
            stageProperty.FindPropertyRelative("stageIndex").intValue = stageNode.StageIndex;
            stageProperty.FindPropertyRelative("stageName").stringValue = stageNode.StageName;

            // Display - store for reflection fallback
            var journalEntry = stageNode.JournalEntry;
            _stageJournalEntries[arrayIndex] = journalEntry;
            CopyLocalizedStringToProperty(stageProperty, "journalEntry", journalEntry);
            stageProperty.FindPropertyRelative("stageIcon").objectReferenceValue = stageNode.StageIcon;

            // Flags
            stageProperty.FindPropertyRelative("isOptional").boolValue = stageNode.IsOptional;
            stageProperty.FindPropertyRelative("isHidden").boolValue = stageNode.IsHidden;
            stageProperty.FindPropertyRelative("isTerminal").boolValue = stageNode.IsTerminal;

            // Task groups
            BuildTaskGroups(stageProperty.FindPropertyRelative("taskGroups"), stageNode);

            // Transitions
            BuildTransitions(stageProperty.FindPropertyRelative("transitions"), stageNode);
        }

        private void BuildStageFromSubgraph(SerializedProperty stageProperty, ISubgraphNode subgraphNode, int arrayIndex)
        {
            var stageGraph = subgraphNode.GetSubgraph() as StageGraph;

            if (stageGraph != null)
            {
                // Identity - check I/O variable ports first, then fall back to serialized fields
                // This allows parent graphs to override subgraph values via connected inputs
                stageProperty.FindPropertyRelative("stageIndex").intValue =
                    GraphTraversalUtility.ResolveSubgraphInput(subgraphNode, "StageIndex", stageGraph.StageIndex);
                stageProperty.FindPropertyRelative("stageName").stringValue =
                    GraphTraversalUtility.ResolveSubgraphInput(subgraphNode, "StageName", stageGraph.StageName);

                // Display - check I/O variable ports with serialized field fallback
                var journalEntry = GraphTraversalUtility.ResolveSubgraphInput(
                    subgraphNode, "JournalEntry", stageGraph.JournalEntry);
                _stageJournalEntries[arrayIndex] = journalEntry;
                CopyLocalizedStringToProperty(stageProperty, "journalEntry", journalEntry);

                stageProperty.FindPropertyRelative("stageIcon").objectReferenceValue =
                    GraphTraversalUtility.ResolveSubgraphInput(subgraphNode, "StageIcon", stageGraph.StageIcon);

                // Flags - check I/O variable ports with serialized field fallback
                stageProperty.FindPropertyRelative("isOptional").boolValue =
                    GraphTraversalUtility.ResolveSubgraphInput(subgraphNode, "IsOptional", stageGraph.IsOptional);
                stageProperty.FindPropertyRelative("isHidden").boolValue =
                    GraphTraversalUtility.ResolveSubgraphInput(subgraphNode, "IsHidden", stageGraph.IsHidden);
                stageProperty.FindPropertyRelative("isTerminal").boolValue =
                    GraphTraversalUtility.ResolveSubgraphInput(subgraphNode, "IsTerminal", stageGraph.IsTerminal);
            }
            else
            {
                // No valid subgraph reference, use defaults
                _context.AddWarning($"Stage subgraph node has invalid or missing StageGraph reference");
                stageProperty.FindPropertyRelative("stageIndex").intValue = 0;
                stageProperty.FindPropertyRelative("stageName").stringValue = "Unknown Stage";
                stageProperty.FindPropertyRelative("isOptional").boolValue = false;
                stageProperty.FindPropertyRelative("isHidden").boolValue = false;
                stageProperty.FindPropertyRelative("isTerminal").boolValue = false;
            }

            // Task groups - from inside the stage subgraph or connected in main graph
            BuildTaskGroupsFromSubgraphStage(stageProperty.FindPropertyRelative("taskGroups"), subgraphNode);

            // Transitions
            BuildTransitionsFromSubgraph(stageProperty.FindPropertyRelative("transitions"), subgraphNode, stageGraph);
        }

        private void BuildTaskGroups(SerializedProperty taskGroupsProperty, StageNode stageNode)
        {
            taskGroupsProperty.ClearArray();

            var taskGroupNodes = _stageTaskGroups[stageNode];
            foreach (var tgContext in taskGroupNodes)
            {
                taskGroupsProperty.InsertArrayElementAtIndex(taskGroupsProperty.arraySize);
                var tgProperty = taskGroupsProperty.GetArrayElementAtIndex(taskGroupsProperty.arraySize - 1);

                BuildTaskGroupFromContext(tgProperty, tgContext);
            }
        }

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
                // Handle both Asset and Define modes
                Task_SO taskAsset = GetTaskAssetFromBlock(taskBlock);

                if (taskAsset != null)
                {
                    tasksProperty.InsertArrayElementAtIndex(tasksProperty.arraySize);
                    tasksProperty.GetArrayElementAtIndex(tasksProperty.arraySize - 1).objectReferenceValue = taskAsset;
                }
            }
        }

        /// <summary>
        /// Gets the Task_SO asset from a TaskBlockBase, handling both Asset and Define modes.
        /// </summary>
        private Task_SO GetTaskAssetFromBlock(TaskBlockBase taskBlock)
        {
            if (taskBlock == null)
                return null;

            Debug.Log($"[GraphToQuestConverter] GetTaskAssetFromBlock: {taskBlock.DevName}, IsDefineMode={taskBlock.IsDefineMode}, IsAssetMode={taskBlock.IsAssetMode}");

            if (taskBlock.IsDefineMode)
            {
                // Create Task_SO from inline data
                return CreateInlineTaskAssetFromBlock(taskBlock);
            }
            else
            {
                // Use referenced Task_SO asset
                return taskBlock.TaskAsset;
            }
        }

        /// <summary>
        /// Creates a Task_SO instance from a TaskBlockBase's inline data.
        /// The created asset is tracked for later registration as a sub-asset.
        /// </summary>
        private Task_SO CreateInlineTaskAssetFromBlock(TaskBlockBase taskBlock)
        {
            Debug.Log($"[GraphToQuestConverter] CreateInlineTaskAssetFromBlock called for: {taskBlock?.DevName ?? "null"}");

            if (taskBlock == null)
            {
                _context.AddWarning($"Task block is null");
                return null;
            }

            try
            {
                // Validate the task configuration
                if (!taskBlock.HasValidTask)
                {
                    _context.AddWarning($"Task '{taskBlock.DevName}' has invalid configuration for type {taskBlock.TaskTypeName}");
                }

                Debug.Log($"[GraphToQuestConverter] Calling taskBlock.CreateTaskAsset() for: {taskBlock.DevName}");

                // Create the Task_SO asset using the block's factory method
                var task = taskBlock.CreateTaskAsset();

                Debug.Log($"[GraphToQuestConverter] Created task asset: {task?.name ?? "null"}");

                // Name the asset for debugging and sub-asset identification
                task.name = $"{taskBlock.DevName}_{_createdInlineTasks.Count}";

                // Track for sub-asset registration
                _createdInlineTasks.Add(task);

                return task;
            }
            catch (Exception ex)
            {
                _context.AddError($"Failed to create inline task: {ex.Message}");
                Debug.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the Task_SO asset from a TaskBaseNode, handling both Asset and Define modes.
        /// Used for TaskBaseNodes inside TaskGroupGraph subgraphs.
        /// </summary>
        private Task_SO GetTaskAssetFromNode(TaskBaseNode taskNode)
        {
            if (taskNode == null)
                return null;

            if (taskNode.IsDefineMode)
            {
                // Create Task_SO from inline data
                return CreateInlineTaskAssetFromNode(taskNode);
            }
            else
            {
                // Use referenced Task_SO asset
                return taskNode.TaskAsset;
            }
        }

        /// <summary>
        /// Creates a Task_SO instance from a TaskBaseNode's inline data.
        /// Used for TaskBaseNodes inside TaskGroupGraph subgraphs.
        /// </summary>
        private Task_SO CreateInlineTaskAssetFromNode(TaskBaseNode taskNode)
        {
            Debug.Log($"[GraphToQuestConverter] CreateInlineTaskAssetFromNode called for: {taskNode?.DevName ?? "null"}");

            if (taskNode == null)
            {
                _context.AddWarning($"Task node is null");
                return null;
            }

            try
            {
                // Validate the task configuration
                if (!taskNode.HasValidTask)
                {
                    _context.AddWarning($"Task '{taskNode.DevName}' has invalid configuration for type {taskNode.TaskTypeName}");
                }

                Debug.Log($"[GraphToQuestConverter] Calling taskNode.CreateTaskAsset() for: {taskNode.DevName}");

                // Create the Task_SO asset using the node's factory method
                var task = taskNode.CreateTaskAsset();

                Debug.Log($"[GraphToQuestConverter] Created task asset from node: {task?.name ?? "null"}");

                // Name the asset for debugging and sub-asset identification
                task.name = $"{taskNode.DevName}_{_createdInlineTasks.Count}";

                // Track for sub-asset registration
                _createdInlineTasks.Add(task);

                return task;
            }
            catch (Exception ex)
            {
                _context.AddError($"Failed to create inline task from node: {ex.Message}");
                Debug.LogException(ex);
                return null;
            }
        }

        private void BuildTransitions(SerializedProperty transitionsProperty, StageNode stageNode)
        {
            transitionsProperty.ClearArray();

            // Terminal stages have no transitions
            if (stageNode.IsTerminal)
                return;

            // Get all nodes connected to the "Then" output port
            var connectedNodes = GraphTraversalUtility.GetAllConnectedNodes(stageNode, "Then");

            foreach (var connectedNode in connectedNodes)
            {
                switch (connectedNode)
                {
                    case TransitionNode transitionNode:
                        // Use TransitionNode's full configuration
                        AddTransitionFromNode(transitionsProperty, transitionNode);
                        break;

                    case StageNode targetStage:
                        // Direct Stage→Stage connection: implicit OnGroupsComplete
                        AddTransition(transitionsProperty, targetStage.StageIndex, TransitionTrigger.OnGroupsComplete);
                        break;

                    case ISubgraphNode subgraphNode when subgraphNode.GetSubgraph() is StageGraph stageGraph:
                        // Direct Stage→StageSubgraph connection: implicit OnGroupsComplete
                        AddTransition(transitionsProperty, stageGraph.StageIndex, TransitionTrigger.OnGroupsComplete);
                        break;
                }
            }

            // If no transitions were added from "Then" port but we have connected nodes, warn
            if (connectedNodes.Count == 0)
            {
                // Check for legacy direct connection (GetConnectedStageIndex handles single connection)
                var thenTarget = GraphTraversalUtility.GetConnectedStageIndex(stageNode, "Then");
                if (thenTarget >= 0)
                {
                    AddTransition(transitionsProperty, thenTarget, TransitionTrigger.OnGroupsComplete);
                }
            }

            // Player choices (from Choices output port)
            if (_stageChoices.TryGetValue(stageNode, out var choices))
            {
                foreach (var choice in choices)
                {
                    AddChoiceTransition(transitionsProperty, choice);
                }
            }
        }

        private void BuildTaskGroupsFromSubgraphStage(SerializedProperty taskGroupsProperty, ISubgraphNode subgraphNode)
        {
            taskGroupsProperty.ClearArray();

            // Get connected task group nodes (both inline and subgraph)
            if (!_stageSubgraphTaskGroups.TryGetValue(subgraphNode, out var taskGroupNodes))
                return;

            foreach (var tgNodeObj in taskGroupNodes)
            {
                taskGroupsProperty.InsertArrayElementAtIndex(taskGroupsProperty.arraySize);
                var tgProperty = taskGroupsProperty.GetArrayElementAtIndex(taskGroupsProperty.arraySize - 1);

                switch (tgNodeObj)
                {
                    case TaskGroupContextNode tgContext:
                        // Extract tasks from blocks inside the context node
                        BuildTaskGroupFromContext(tgProperty, tgContext);
                        break;
                    case ISubgraphNode tgSubgraph when tgSubgraph.GetSubgraph() is TaskGroupGraph:
                        BuildTaskGroupFromSubgraph(tgProperty, tgSubgraph);
                        break;
                }
            }
        }

        private void BuildTaskGroupFromSubgraph(SerializedProperty tgProperty, ISubgraphNode tgSubgraphNode)
        {
            var taskGroupGraph = tgSubgraphNode.GetSubgraph() as TaskGroupGraph;

            if (taskGroupGraph != null)
            {
                // Check I/O variable ports first, then fall back to serialized fields
                // This allows parent graphs to override subgraph values via connected inputs
                tgProperty.FindPropertyRelative("groupName").stringValue =
                    GraphTraversalUtility.ResolveSubgraphInput(tgSubgraphNode, "GroupName", taskGroupGraph.GroupName);

                // For ExecutionMode, try to resolve from port (as int) then fall back
                var executionMode = GraphTraversalUtility.ResolveSubgraphInput(
                    tgSubgraphNode, "ExecutionMode", (int)taskGroupGraph.ExecutionMode);
                tgProperty.FindPropertyRelative("executionMode").enumValueIndex = executionMode;

                tgProperty.FindPropertyRelative("requiredCount").intValue =
                    GraphTraversalUtility.ResolveSubgraphInput(tgSubgraphNode, "RequiredCount", taskGroupGraph.RequiredCount);

                var tasksProperty = tgProperty.FindPropertyRelative("tasks");
                tasksProperty.ClearArray();

                // Extract tasks from the TaskGroupGraph
                var subgraphTasks = ExtractTasksFromSubgraph(taskGroupGraph);
                foreach (var task in subgraphTasks)
                {
                    tasksProperty.InsertArrayElementAtIndex(tasksProperty.arraySize);
                    tasksProperty.GetArrayElementAtIndex(tasksProperty.arraySize - 1).objectReferenceValue = task;
                }
            }
            else
            {
                _context.AddWarning($"TaskGroup subgraph node has invalid or missing TaskGroupGraph reference");
                tgProperty.FindPropertyRelative("groupName").stringValue = "Unknown Group";
                tgProperty.FindPropertyRelative("executionMode").enumValueIndex = 0;
                tgProperty.FindPropertyRelative("requiredCount").intValue = 1;
            }
        }

        private void BuildTransitionsFromSubgraph(SerializedProperty transitionsProperty, ISubgraphNode subgraphNode, StageGraph stageGraph)
        {
            transitionsProperty.ClearArray();

            // Terminal stages have no transitions
            if (stageGraph != null && stageGraph.IsTerminal)
                return;

            // Cast to INode to access port methods
            var node = subgraphNode as INode;
            if (node == null)
                return;

            // Get all nodes connected to the "Then" output port
            var connectedNodes = GraphTraversalUtility.GetAllConnectedNodes(node, "Then");

            foreach (var connectedNode in connectedNodes)
            {
                switch (connectedNode)
                {
                    case TransitionNode transitionNode:
                        // Use TransitionNode's full configuration
                        AddTransitionFromNode(transitionsProperty, transitionNode);
                        break;

                    case StageNode targetStage:
                        // Direct connection: implicit OnGroupsComplete
                        AddTransition(transitionsProperty, targetStage.StageIndex, TransitionTrigger.OnGroupsComplete);
                        break;

                    case ISubgraphNode targetSubgraph when targetSubgraph.GetSubgraph() is StageGraph targetStageGraph:
                        // Direct connection: implicit OnGroupsComplete
                        AddTransition(transitionsProperty, targetStageGraph.StageIndex, TransitionTrigger.OnGroupsComplete);
                        break;
                }
            }

            // Fallback for legacy single connection
            if (connectedNodes.Count == 0)
            {
                var thenTarget = GraphTraversalUtility.GetConnectedStageIndex(node, "Then");
                if (thenTarget >= 0)
                {
                    AddTransition(transitionsProperty, thenTarget, TransitionTrigger.OnGroupsComplete);
                }
            }
        }

        /// <summary>
        /// Extracts Task_SO assets from a TaskGroupGraph subgraph.
        /// Handles both Asset and Define mode TaskBaseNodes.
        /// </summary>
        private List<Task_SO> ExtractTasksFromSubgraph(TaskGroupGraph subgraph)
        {
            var tasks = new List<Task_SO>();

            if (subgraph == null)
                return tasks;

            try
            {
                foreach (var taskNode in subgraph.GetNodes().OfType<TaskBaseNode>())
                {
                    // Handle both Asset and Define modes
                    Task_SO taskAsset = GetTaskAssetFromNode(taskNode);

                    if (taskAsset != null)
                    {
                        tasks.Add(taskAsset);
                    }
                }
            }
            catch
            {
                _context.AddWarning($"Failed to extract tasks from subgraph '{subgraph.name}'");
            }

            return tasks;
        }

        private void AddTransition(SerializedProperty transitionsProperty, int targetIndex,
            TransitionTrigger trigger, string label = null, int priority = 0)
        {
            transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            var transProperty = transitionsProperty.GetArrayElementAtIndex(transitionsProperty.arraySize - 1);

            transProperty.FindPropertyRelative("targetStageIndex").intValue = targetIndex;
            transProperty.FindPropertyRelative("trigger").enumValueIndex = (int)trigger;
            transProperty.FindPropertyRelative("priority").intValue = priority;

            if (!string.IsNullOrEmpty(label))
            {
                transProperty.FindPropertyRelative("transitionLabel").stringValue = label;
            }
        }

        /// <summary>
        /// Adds a transition from a TransitionNode with full configuration.
        /// </summary>
        private void AddTransitionFromNode(SerializedProperty transitionsProperty, TransitionNode transitionNode)
        {
            // Get target stage from TransitionNode's "To" output port
            var targetIndex = transitionNode.TargetStageIndex;
            if (targetIndex < 0)
            {
                _context.AddWarning($"TransitionNode '{transitionNode.Label}' has no target stage connected");
                return;
            }

            transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            var transProperty = transitionsProperty.GetArrayElementAtIndex(transitionsProperty.arraySize - 1);

            // Basic fields from TransitionNode
            transProperty.FindPropertyRelative("targetStageIndex").intValue = targetIndex;
            transProperty.FindPropertyRelative("trigger").enumValueIndex = (int)transitionNode.Trigger;
            transProperty.FindPropertyRelative("priority").intValue = transitionNode.Priority;

            if (!string.IsNullOrEmpty(transitionNode.Label))
            {
                transProperty.FindPropertyRelative("transitionLabel").stringValue = transitionNode.Label;
            }

            // Collect conditions from connected ConditionContextNode
            var conditions = CollectConditionsFromTransitionNode(transitionNode);
            var conditionsProperty = transProperty.FindPropertyRelative("conditions");
            conditionsProperty.ClearArray();

            foreach (var condition in conditions)
            {
                if (condition != null)
                {
                    conditionsProperty.InsertArrayElementAtIndex(conditionsProperty.arraySize);
                    conditionsProperty.GetArrayElementAtIndex(conditionsProperty.arraySize - 1).objectReferenceValue = condition;
                }
            }
        }

        /// <summary>
        /// Collects conditions from a TransitionNode's ConditionsInput port.
        /// </summary>
        private List<HelloDev.Conditions.Condition_SO> CollectConditionsFromTransitionNode(TransitionNode transitionNode)
        {
            var conditions = new List<HelloDev.Conditions.Condition_SO>();

            if (transitionNode == null)
                return conditions;

            var connectedNode = GraphTraversalUtility.GetConnectedInputNode(transitionNode, "ConditionsInput");

            if (connectedNode is ConditionContextNode conditionContext)
            {
                foreach (var block in conditionContext.blockNodes.OfType<ConditionBlock>())
                {
                    // Resolve condition from port (embedded or connected), fall back to option
                    var condition = GraphTraversalUtility.ResolveDataPort<HelloDev.Conditions.Condition_SO>(
                        block, "ConditionAssetInput", null) ?? block.ConditionAsset;

                    if (condition != null)
                        conditions.Add(condition);
                }
            }

            return conditions;
        }

        private void AddChoiceTransition(SerializedProperty transitionsProperty, ChoiceNode choice)
        {
            // Get target stage from choice's output
            var targetIndex = GraphTraversalUtility.GetConnectedStageIndex(choice, "Target");
            if (targetIndex < 0)
            {
                _context.AddWarning($"Choice '{choice.ChoiceId}' has no target stage connected");
                return;
            }

            transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            var transProperty = transitionsProperty.GetArrayElementAtIndex(transitionsProperty.arraySize - 1);

            // Basic fields
            transProperty.FindPropertyRelative("targetStageIndex").intValue = targetIndex;
            transProperty.FindPropertyRelative("trigger").enumValueIndex = (int)TransitionTrigger.PlayerChoice;
            transProperty.FindPropertyRelative("priority").intValue = choice.Priority;
            transProperty.FindPropertyRelative("isPlayerChoice").boolValue = true;
            transProperty.FindPropertyRelative("choiceId").stringValue = choice.ChoiceId;

            // Display fields
            CopyLocalizedStringToProperty(transProperty, "choiceText", choice.ChoiceText);
            CopyLocalizedStringToProperty(transProperty, "choiceTooltip", choice.ChoiceTooltip);
            transProperty.FindPropertyRelative("choiceIcon").objectReferenceValue = choice.ChoiceIcon;

            // Conditions
            var conditionsProperty = transProperty.FindPropertyRelative("conditions");
            conditionsProperty.ClearArray();
            if (choice.Conditions != null)
            {
                foreach (var condition in choice.Conditions)
                {
                    if (condition != null)
                    {
                        conditionsProperty.InsertArrayElementAtIndex(conditionsProperty.arraySize);
                        conditionsProperty.GetArrayElementAtIndex(conditionsProperty.arraySize - 1).objectReferenceValue = condition;
                    }
                }
            }

            // World flag modifications
            var flagsProperty = transProperty.FindPropertyRelative("worldFlagsOnSelect");
            flagsProperty.ClearArray();
            if (choice.WorldFlagsOnSelect != null)
            {
                for (int i = 0; i < choice.WorldFlagsOnSelect.Count; i++)
                {
                    flagsProperty.InsertArrayElementAtIndex(flagsProperty.arraySize);
                    // WorldFlagModification is a class, copy its fields
                    var flagModProp = flagsProperty.GetArrayElementAtIndex(flagsProperty.arraySize - 1);
                    var sourceMod = choice.WorldFlagsOnSelect[i];

                    // Copy WorldFlagModification fields using actual field names
                    try
                    {
                        // Locator reference
                        var locatorProp = flagModProp.FindPropertyRelative("flagLocator");
                        if (locatorProp != null && sourceMod.HasLocator)
                        {
                            // Note: We can't easily access the private flagLocator field
                            // The source modification should have its locator set already
                        }

                        // Flag type
                        var isBoolProp = flagModProp.FindPropertyRelative("isBoolFlag");
                        if (isBoolProp != null)
                            isBoolProp.boolValue = sourceMod.IsBoolFlag;

                        // Boolean flag fields
                        var boolFlagProp = flagModProp.FindPropertyRelative("boolFlag");
                        if (boolFlagProp != null)
                            boolFlagProp.objectReferenceValue = sourceMod.BoolFlag;

                        var boolValueProp = flagModProp.FindPropertyRelative("boolValue");
                        if (boolValueProp != null)
                            boolValueProp.boolValue = sourceMod.BoolValue;

                        // Integer flag fields
                        var intFlagProp = flagModProp.FindPropertyRelative("intFlag");
                        if (intFlagProp != null)
                            intFlagProp.objectReferenceValue = sourceMod.IntFlag;

                        var intOpProp = flagModProp.FindPropertyRelative("intOperation");
                        if (intOpProp != null)
                            intOpProp.enumValueIndex = (int)sourceMod.IntOperation;

                        var intValueProp = flagModProp.FindPropertyRelative("intValue");
                        if (intValueProp != null)
                            intValueProp.intValue = sourceMod.IntValue;
                    }
                    catch
                    {
                        _context.AddWarning($"Could not fully copy WorldFlagModification for choice '{choice.ChoiceId}'");
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Collects conditions from a QuestNode's context input port.
        /// Finds connected ConditionContextNode and extracts Condition_SO from its blocks.
        /// </summary>
        private List<HelloDev.Conditions.Condition_SO> CollectConditionsFromQuestNode(QuestNode questNode, string portName)
        {
            var conditions = new List<HelloDev.Conditions.Condition_SO>();

            if (questNode == null)
                return conditions;

            var connectedNode = GraphTraversalUtility.GetConnectedInputNode(questNode, portName);

            if (connectedNode is ConditionContextNode conditionContext)
            {
                foreach (var block in conditionContext.blockNodes.OfType<ConditionBlock>())
                {
                    // Resolve condition from port (embedded or connected), fall back to option
                    var condition = GraphTraversalUtility.ResolveDataPort<HelloDev.Conditions.Condition_SO>(
                        block, "ConditionAssetInput", null) ?? block.ConditionAsset;

                    if (condition != null)
                        conditions.Add(condition);
                }
            }

            return conditions;
        }

        /// <summary>
        /// Collects rewards from a QuestNode's RewardsInput context port.
        /// Finds connected RewardContextNode and extracts RewardInstances from its blocks.
        /// </summary>
        private List<RewardInstance> CollectRewardsFromQuestNode(QuestNode questNode)
        {
            var rewards = new List<RewardInstance>();

            if (questNode == null)
                return rewards;

            // Find the RewardContextNode connected to the RewardsInput port
            var connectedNode = GraphTraversalUtility.GetConnectedInputNode(questNode, "RewardsInput");
            if (connectedNode is RewardContextNode rewardContext)
            {
                // Extract rewards from blocks
                foreach (var block in rewardContext.blockNodes.OfType<RewardBlock>())
                {
                    if (block.IsValid)
                    {
                        rewards.Add(block.ToRewardInstance());
                    }
                }
            }

            return rewards;
        }
        
        static readonly FieldInfo VariablesSourceField =
            typeof(LocalizedString).GetField("m_Variables", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly FieldInfo VariablesDictField =
            typeof(PersistentVariablesSource).GetField("m_Variables", BindingFlags.Instance | BindingFlags.NonPublic);


        private void CopyLocalizedString(SerializedObject so, string propertyName, LocalizedString source)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null || source == null)
                return;

            so.ApplyModifiedProperties();
            var target = GetTargetObjectOfProperty(prop) as LocalizedString;
            if (target == null)
                return;

            CopyLocalizedStringObject(target, source);

            EditorUtility.SetDirty(so.targetObject);
        }

        private void CopyLocalizedStringObject(LocalizedString target, LocalizedString source)
        {
            target.TableReference = source.TableReference;
            target.TableEntryReference = source.TableEntryReference;

            CopySmartVariables(target, source);
        }

        private void CopySmartVariables(LocalizedString target, LocalizedString source)
        {
            if (target == null || source == null)
                return;

            if (VariablesSourceField == null || VariablesDictField == null)
                return;

            var srcSource = VariablesSourceField.GetValue(source) as PersistentVariablesSource;
            if (srcSource == null)
                return;

            var srcDict = VariablesDictField.GetValue(srcSource) as Dictionary<string, IVariable>;
            if (srcDict == null || srcDict.Count == 0)
                return;

            var formatter = LocalizationSettings.StringDatabase?.SmartFormatter;
            if (formatter == null)
                return;

            var dstSource = new PersistentVariablesSource(formatter);
            var dstDict = new Dictionary<string, IVariable>();

            foreach (var pair in srcDict)
            {
                if (pair.Value == null)
                    continue;

                var clone = CloneVariable(pair.Value);
                if (clone != null)
                    dstDict[pair.Key] = clone;
            }

            VariablesDictField.SetValue(dstSource, dstDict);
            VariablesSourceField.SetValue(target, dstSource);
        }
        
        private void CopyLocalizedStringToProperty(SerializedProperty parent, string propertyName, UnityEngine.Localization.LocalizedString source)
        {
            var prop = parent.FindPropertyRelative(propertyName);
            CopyLocalizedStringToProperty(prop, source);
        }

        private IVariable CloneVariable(IVariable source)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var clone = System.Activator.CreateInstance(type);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsInitOnly)
                    continue;

                try
                {
                    field.SetValue(clone, field.GetValue(source));
                }
                catch
                {
                    // ignora campos não copiáveis internamente
                }
            }

            return clone as IVariable;
        }

        
        private void CopyLocalizedStringToProperty(SerializedProperty prop, LocalizedString source)
        {
            if (prop == null || source == null)
                return;

            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef != null && source.TableReference.TableCollectionNameGuid != Guid.Empty)
            {
                var tableCollectionName = tableRef.FindPropertyRelative("m_TableCollectionName");
                if (tableCollectionName != null)
                {
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

        private void CopyConditionList(SerializedObject so, string propertyName, List<HelloDev.Conditions.Condition_SO> conditions)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
                return;

            prop.ClearArray();

            if (conditions == null)
                return;

            foreach (var condition in conditions)
            {
                if (condition != null)
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = condition;
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

                // RewardInstance is a struct with RewardType and Amount fields
                var typeProp = rewardProp.FindPropertyRelative("RewardType");
                var amountProp = rewardProp.FindPropertyRelative("Amount");

                if (typeProp != null)
                    typeProp.objectReferenceValue = reward.RewardType;
                if (amountProp != null)
                    amountProp.intValue = reward.Amount;
            }
        }

        #endregion

        #region Reflection-Based LocalizedString Assignment

        /// <summary>
        /// Applies LocalizedStrings directly via reflection as a fallback for SerializedProperty issues.
        /// This bypasses Unity's serialization system entirely by directly setting field values.
        /// </summary>
        private void ApplyLocalizedStringsViaReflection(Quest_SO quest)
        {
            if (_stageJournalEntries.Count == 0)
                return;

            var stagesField = typeof(Quest_SO).GetField("stages", BindingFlags.NonPublic | BindingFlags.Instance);
            if (stagesField == null)
                return;

            var stagesList = stagesField.GetValue(quest) as System.Collections.IList;
            if (stagesList == null)
                return;

            var journalEntryField = typeof(QuestStage).GetField("journalEntry", BindingFlags.NonPublic | BindingFlags.Instance);
            if (journalEntryField == null)
                return;

            foreach (var kvp in _stageJournalEntries)
            {
                int arrayIndex = kvp.Key;
                var localizedString = kvp.Value;

                if (arrayIndex < stagesList.Count)
                {
                    var stage = stagesList[arrayIndex];
                    if (stage != null && localizedString != null)
                    {
                        bool hasTable = localizedString.TableReference.TableCollectionNameGuid != Guid.Empty;
                        bool hasKey = localizedString.TableEntryReference.KeyId != 0;

                        if (hasTable || hasKey)
                        {
                            journalEntryField.SetValue(stage, localizedString);
                        }
                    }
                }
            }

            EditorUtility.SetDirty(quest);
        }

        #endregion

        #region Context Access

        /// <summary>
        /// Gets the conversion context with errors and warnings.
        /// </summary>
        public ConversionContext Context => _context;

        #endregion
        
        #region Utilities
        
        private static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;

            foreach (var element in path.Split('.'))
            {
                if (element.Contains("["))
                {
                    var name = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, name, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }

            return obj;
        }

        private static object GetValue(object source, string name)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
                return f.GetValue(source);

            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(source, null);
        }

        private static object GetValue(object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as System.Collections.IEnumerable;
            if (enumerable == null)
                return null;

            var enm = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
                if (!enm.MoveNext()) return null;

            return enm.Current;
        }
        
        #endregion
    }
}
