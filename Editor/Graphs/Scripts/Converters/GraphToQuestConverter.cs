using System;
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<StageNode, List<TaskGroupNode>> _stageTaskGroups;
        private Dictionary<ISubgraphNode, List<INode>> _stageSubgraphTaskGroups;
        private Dictionary<StageNode, List<ChoiceNode>> _stageChoices;
        private List<RewardNode> _rewardNodes;

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
                // Create or use existing asset
                var quest = existing != null ? existing : ScriptableObject.CreateInstance<Quest_SO>();

                // Pass 1: Collect all nodes
                CollectNodes(graph);

                // Pass 2: Build Quest_SO structure
                BuildQuestData(graph, quest);

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
            _stageTaskGroups = new Dictionary<StageNode, List<TaskGroupNode>>();
            foreach (var stageNode in _stageNodeLookup.Values)
            {
                _stageTaskGroups[stageNode] = FindConnectedTaskGroups(stageNode);
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
        /// Finds TaskGroupNodes connected to a StageNode.
        /// TaskGroups are connected via TaskGroupNode's "In" port from StageNode's ports.
        /// </summary>
        private List<TaskGroupNode> FindConnectedTaskGroups(StageNode stageNode)
        {
            var taskGroups = new List<TaskGroupNode>();

            // TaskGroupNodes should be connected to the stage
            // Check all TaskGroupNodes and see which ones have their "In" port connected to this stage
            foreach (var node in _allNodes.OfType<TaskGroupNode>())
            {
                try
                {
                    var inPort = node.GetInputPortByName("In");
                    if (inPort != null && inPort.isConnected)
                    {
                        var connectedNode = inPort.firstConnectedPort?.GetNode();
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
        /// Finds TaskGroupNodes connected to a stage ISubgraphNode,
        /// or extracts them from within the stage subgraph itself.
        /// </summary>
        private List<INode> FindConnectedTaskGroupsForSubgraph(ISubgraphNode subgraphNode)
        {
            var taskGroups = new List<INode>();

            // First check for TaskGroupNodes inside the stage subgraph
            var stageGraph = subgraphNode.GetSubgraph() as StageGraph;
            if (stageGraph != null)
            {
                foreach (var node in stageGraph.GetNodes())
                {
                    if (node is TaskGroupNode tgNode)
                    {
                        taskGroups.Add(tgNode);
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

            // Also check for TaskGroupNodes connected in the main quest graph
            foreach (var node in _allNodes.OfType<TaskGroupNode>())
            {
                try
                {
                    var inPort = node.GetInputPortByName("In");
                    if (inPort != null && inPort.isConnected)
                    {
                        var connectedNode = inPort.firstConnectedPort?.GetNode();
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

            // Copy identity fields
            so.FindProperty("devName").stringValue = graph.DevName;
            so.FindProperty("questId").stringValue = graph.QuestId;
            so.FindProperty("questType").objectReferenceValue = graph.QuestType;
            so.FindProperty("recommendedLevel").intValue = graph.RecommendedLevel;

            // Copy display fields
            CopyLocalizedString(so, "displayName", graph.DisplayName);
            CopyLocalizedString(so, "questDescription", graph.QuestDescription);
            CopyLocalizedString(so, "questLocation", graph.QuestLocation);
            so.FindProperty("questSprite").objectReferenceValue = graph.QuestSprite;

            // Copy conditions
            CopyConditionList(so, "startConditions", graph.StartConditions);
            CopyConditionList(so, "failureConditions", graph.FailureConditions);

            // Copy rewards - combine graph rewards and RewardNode rewards
            var allRewards = new List<RewardInstance>();
            if (graph.Rewards != null)
            {
                allRewards.AddRange(graph.Rewards);
            }

            // Add rewards from RewardNodes in the graph
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

            foreach (var stageIndex in allStageIndices)
            {
                stagesProperty.InsertArrayElementAtIndex(stagesProperty.arraySize);
                var stageProperty = stagesProperty.GetArrayElementAtIndex(stagesProperty.arraySize - 1);

                // Check if it's an inline StageNode or a stage subgraph node
                if (_stageNodeLookup.TryGetValue(stageIndex, out var stageNode))
                {
                    BuildStage(stageProperty, stageNode);
                }
                else if (_stageSubgraphLookup.TryGetValue(stageIndex, out var subgraphNode))
                {
                    BuildStageFromSubgraph(stageProperty, subgraphNode);
                }
            }
        }

        private void BuildStage(SerializedProperty stageProperty, StageNode stageNode)
        {
            // Identity
            stageProperty.FindPropertyRelative("stageIndex").intValue = stageNode.StageIndex;
            stageProperty.FindPropertyRelative("stageName").stringValue = stageNode.StageName;

            // Display
            CopyLocalizedStringToProperty(stageProperty, "journalEntry", stageNode.JournalEntry);
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

        private void BuildStageFromSubgraph(SerializedProperty stageProperty, ISubgraphNode subgraphNode)
        {
            var stageGraph = subgraphNode.GetSubgraph() as StageGraph;

            if (stageGraph != null)
            {
                // Identity - from the stage graph
                stageProperty.FindPropertyRelative("stageIndex").intValue = stageGraph.StageIndex;
                stageProperty.FindPropertyRelative("stageName").stringValue = stageGraph.StageName;

                // Display
                CopyLocalizedStringToProperty(stageProperty, "journalEntry", stageGraph.JournalEntry);
                stageProperty.FindPropertyRelative("stageIcon").objectReferenceValue = stageGraph.StageIcon;

                // Flags
                stageProperty.FindPropertyRelative("isOptional").boolValue = stageGraph.IsOptional;
                stageProperty.FindPropertyRelative("isHidden").boolValue = stageGraph.IsHidden;
                stageProperty.FindPropertyRelative("isTerminal").boolValue = stageGraph.IsTerminal;
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
            foreach (var tgNode in taskGroupNodes)
            {
                taskGroupsProperty.InsertArrayElementAtIndex(taskGroupsProperty.arraySize);
                var tgProperty = taskGroupsProperty.GetArrayElementAtIndex(taskGroupsProperty.arraySize - 1);

                BuildTaskGroup(tgProperty, tgNode);
            }
        }

        private void BuildTaskGroup(SerializedProperty tgProperty, TaskGroupNode tgNode)
        {
            tgProperty.FindPropertyRelative("groupName").stringValue = tgNode.GroupName;
            tgProperty.FindPropertyRelative("executionMode").enumValueIndex = (int)tgNode.ExecutionMode;
            tgProperty.FindPropertyRelative("requiredCount").intValue = tgNode.RequiredCount;

            // Find connected tasks
            var tasksProperty = tgProperty.FindPropertyRelative("tasks");
            tasksProperty.ClearArray();

            var taskNodes = FindConnectedTasks(tgNode);
            foreach (var taskNode in taskNodes)
            {
                if (taskNode.TaskAsset != null)
                {
                    tasksProperty.InsertArrayElementAtIndex(tasksProperty.arraySize);
                    tasksProperty.GetArrayElementAtIndex(tasksProperty.arraySize - 1).objectReferenceValue = taskNode.TaskAsset;
                }
            }

            // If the TaskGroupNode references a subgraph, also get tasks from there
            if (tgNode.Subgraph != null)
            {
                var subgraphTasks = ExtractTasksFromSubgraph(tgNode.Subgraph);
                foreach (var task in subgraphTasks)
                {
                    tasksProperty.InsertArrayElementAtIndex(tasksProperty.arraySize);
                    tasksProperty.GetArrayElementAtIndex(tasksProperty.arraySize - 1).objectReferenceValue = task;
                }
            }
        }

        private List<TaskNode> FindConnectedTasks(TaskGroupNode tgNode)
        {
            var tasks = new List<TaskNode>();

            if (_allNodes == null) return tasks;

            // Find TaskNodes whose "In" port connects to this TaskGroupNode
            foreach (var node in _allNodes.OfType<TaskNode>())
            {
                try
                {
                    var inPort = node.GetInputPortByName("In");
                    if (inPort != null && inPort.isConnected)
                    {
                        var connectedNode = inPort.firstConnectedPort?.GetNode();
                        if (connectedNode == tgNode)
                        {
                            tasks.Add(node);
                        }

                        // Also check if connected via another TaskNode (sequential chain)
                        if (connectedNode is TaskNode prevTask)
                        {
                            // Walk back the chain to find the TaskGroupNode
                            if (IsTaskInGroup(prevTask, tgNode, new HashSet<INode>()))
                            {
                                tasks.Add(node);
                            }
                        }
                    }
                }
                catch
                {
                    // Port access failed
                }
            }

            return tasks.Distinct().ToList();
        }

        /// <summary>
        /// Recursively checks if a task is part of a task group's chain.
        /// </summary>
        private bool IsTaskInGroup(TaskNode task, TaskGroupNode group, HashSet<INode> visited)
        {
            if (visited.Contains(task))
                return false;

            visited.Add(task);

            try
            {
                var inPort = task.GetInputPortByName("In");
                if (inPort == null || !inPort.isConnected)
                    return false;

                var connectedNode = inPort.firstConnectedPort?.GetNode();

                if (connectedNode == group)
                    return true;

                if (connectedNode is TaskNode prevTask)
                    return IsTaskInGroup(prevTask, group, visited);
            }
            catch
            {
                // Port access failed
            }

            return false;
        }

        private void BuildTransitions(SerializedProperty transitionsProperty, StageNode stageNode)
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

            // Else transition (failure path)
            var elseTarget = GraphTraversalUtility.GetConnectedStageIndex(stageNode, "Else");
            if (elseTarget >= 0)
            {
                AddTransition(transitionsProperty, elseTarget, TransitionTrigger.OnConditionsMet, "Stage Failed");
            }

            // Player choices
            var choices = _stageChoices[stageNode];
            foreach (var choice in choices)
            {
                AddChoiceTransition(transitionsProperty, choice);
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
                    case TaskGroupNode tgNode:
                        BuildTaskGroup(tgProperty, tgNode);
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
                // Use values from the TaskGroupGraph
                tgProperty.FindPropertyRelative("groupName").stringValue = taskGroupGraph.GroupName;
                tgProperty.FindPropertyRelative("executionMode").enumValueIndex = (int)taskGroupGraph.ExecutionMode;
                tgProperty.FindPropertyRelative("requiredCount").intValue = taskGroupGraph.RequiredCount;

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

            // Then transition (success path) - check for "Then" port on the subgraph node
            var thenTarget = GraphTraversalUtility.GetConnectedStageIndex(subgraphNode, "Then");
            if (thenTarget >= 0)
            {
                AddTransition(transitionsProperty, thenTarget, TransitionTrigger.OnGroupsComplete);
            }

            // Else transition (failure path) - check for "Else" port on the subgraph node
            var elseTarget = GraphTraversalUtility.GetConnectedStageIndex(subgraphNode, "Else");
            if (elseTarget >= 0)
            {
                AddTransition(transitionsProperty, elseTarget, TransitionTrigger.OnConditionsMet, "Stage Failed");
            }
        }

        /// <summary>
        /// Extracts Task_SO assets from a TaskGroupGraph subgraph.
        /// </summary>
        private List<Task_SO> ExtractTasksFromSubgraph(TaskGroupGraph subgraph)
        {
            var tasks = new List<Task_SO>();

            if (subgraph == null)
                return tasks;

            try
            {
                foreach (var taskNode in subgraph.GetNodes().OfType<TaskNode>())
                {
                    if (taskNode.TaskAsset != null)
                    {
                        tasks.Add(taskNode.TaskAsset);
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
            TransitionTrigger trigger, string label = null)
        {
            transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            var transProperty = transitionsProperty.GetArrayElementAtIndex(transitionsProperty.arraySize - 1);

            transProperty.FindPropertyRelative("targetStageIndex").intValue = targetIndex;
            transProperty.FindPropertyRelative("trigger").enumValueIndex = (int)trigger;

            if (!string.IsNullOrEmpty(label))
            {
                transProperty.FindPropertyRelative("transitionLabel").stringValue = label;
            }
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

        private void CopyLocalizedString(SerializedObject so, string propertyName, UnityEngine.Localization.LocalizedString source)
        {
            var prop = so.FindProperty(propertyName);
            CopyLocalizedStringToProperty(prop, source);
        }

        private void CopyLocalizedStringToProperty(SerializedProperty parent, string propertyName, UnityEngine.Localization.LocalizedString source)
        {
            var prop = parent.FindPropertyRelative(propertyName);
            CopyLocalizedStringToProperty(prop, source);
        }

        private void CopyLocalizedStringToProperty(SerializedProperty prop, UnityEngine.Localization.LocalizedString source)
        {
            if (prop == null || source == null)
                return;

            // LocalizedString has m_TableReference and m_TableEntryReference
            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef != null && source.TableReference.TableCollectionNameGuid != Guid.Empty)
            {
                var tableGuid = tableRef.FindPropertyRelative("m_TableCollectionNameGuid");
                if (tableGuid != null)
                {
                    // TableReference uses a serialized GUID string
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

        #region Context Access

        /// <summary>
        /// Gets the conversion context with errors and warnings.
        /// </summary>
        public ConversionContext Context => _context;

        #endregion
    }
}
