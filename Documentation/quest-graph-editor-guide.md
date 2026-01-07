# Quest Graph Editor Implementation Guide

*Version 1.6 | Last Updated: 2026-01-05*

> **Important**: This guide uses Unity Graph Toolkit 0.4.0-exp.2+ syntax. See the [official documentation](https://docs.unity3d.com/Packages/com.unity.graphtoolkit@0.1/manual/index.html) for updates.

## Key Changes in v1.4

**Phase 5 - Data Conversion (COMPLETE)**
- Implemented `IGraphConverter<TGraph, TAsset>` interface for type-safe conversion
- Created `GraphTraversalUtility` for traversing graph connections
- Built `GraphToQuestConverter` with two-pass algorithm (nodes first, connections second)
- Built `GraphToQuestLineConverter` with recursive subgraph handling
- Created `QuestGraphImporter` and `QuestLineGraphImporter` (ScriptedImporter pattern)
- Export produces Quest_SO/QuestLine_SO as main asset (Graph handled internally by Toolkit)

**Phase 6 - Validation System (COMPLETE)**
- Created `ValidationResult` with Error/Warning/Info severity levels
- Built `GraphValidationService` with comprehensive rules per graph type
- Implemented `GraphReachabilityAnalyzer` using BFS traversal
- Created `PortConnectionValidator` for connection compatibility checks
- Validation runs on `OnGraphChanged` and blocks export on errors

**Phase 7 - Polish & UX (COMPLETE)**
- Created `QuestGraphStyles.uss` with color-coded node styles
- Built `QuestGraphContextMenu` with validation and statistics actions
- Added `ExportCommands` for validation and reimport menu items
- Node styles: Start (green), Stage (blue-green), Terminal (red), Choice (blue)

**New Designer Documentation**
- Created `quest-graph-designer-workflow.md` for non-programmers

## Key Changes in v1.3

- Created `StageSubgraphNode` for embedding StageGraph in QuestGraph
- Created `TaskGroupSubgraphNode` for embedding TaskGroupGraph in StageGraph
- Enhanced `QuestRefNode` with additional ports (Then, Else) and override support
- Added `Subgraphs/` folder to organize subgraph reference nodes
- Updated file structure documentation to include new subgraph nodes
- Added subgraph mapping table (Parent Graph → Subgraph Node → Child Graph)

## Key Changes in v1.6

**Native Subgraph Migration (COMPLETE)**
- Migrated from custom `StageSubgraphNode`/`TaskGroupSubgraphNode` to Unity Graph Toolkit's native `SubgraphNodeModel` system
- Stage and TaskGroup subgraphs now use **Graph Variables** with `ModifierFlags` for automatic port generation:
  - `ModifierFlags.Read` (1) → INPUT port on subgraph node
  - `ModifierFlags.Write` (2) → OUTPUT port on subgraph node
- **Removed**: `StageSubgraphNode.cs`, `TaskGroupSubgraphNode.cs` (deprecated)
- Updated `GraphToQuestConverter`, `GraphValidationService`, `GraphTraversalUtility`, `GraphReachabilityAnalyzer` to use `ISubgraphNode` interface
- Subgraph detection pattern: `node is ISubgraphNode s when s.GetSubgraph() is StageGraph`
- Stage files now define their own `StageIndex`, `StageName`, `IsTerminal` properties directly (no overrides)

> **Note:** The code examples in sections 3.2-3.3 reference the deprecated custom subgraph nodes. Use native `SubgraphNodeModel` with Graph Variables instead.

## Key Changes in v1.5

**Node Inspector Integration (COMPLETE)**
- All nodes now use `OnDefineOptions` for Inspector integration
- Options appear in node header and Graph Inspector when node is selected
- Added `GetOptionValue<T>()` helper method to `QuestBaseNode`
- Removed `[SerializeField]` fields - replaced with Graph Toolkit options
- Options with `.ShowInInspectorOnly()` appear only in Inspector, not on node

## Key Changes in v1.2

- Added `[Subgraph]` attribute on child graphs (QuestGraph, StageGraph, TaskGroupGraph)
- Created `QuestBaseNode` base class for all nodes
- Added `QuestLineStartNode` for QuestLine entry point
- ~~Removed `OnDefineOptions` from all nodes~~ (Reverted in v1.5)
- Added missing fields (questId, questLineId, LocalizedString display fields)
- Fixed StageNode design (removed confusing StageGraph reference)
- Updated file structure to match actual implementation

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Prerequisites](#2-prerequisites)
3. [Architecture Overview](#3-architecture-overview)
4. [Phase 1: Foundation Setup](#4-phase-1-foundation-setup)
5. [Phase 2: Core Node Types](#5-phase-2-core-node-types)
6. [Phase 3: Subgraph System (Modularity)](#6-phase-3-subgraph-system-modularity)
7. [Phase 4: Port Types & Connections](#7-phase-4-port-types--connections)
8. [Phase 5: Data Conversion](#8-phase-5-data-conversion)
9. [Phase 6: Validation System](#9-phase-6-validation-system)
10. [Phase 7: Polish & UX](#10-phase-7-polish--ux)
11. [Feature Matrix](#11-feature-matrix)
12. [File Structure](#12-file-structure)
13. [Code Examples](#13-code-examples)
14. [Troubleshooting](#14-troubleshooting)
15. [Future Improvements](#15-future-improvements)

---

## 1. Executive Summary

### What We're Building

A visual node-based editor using Unity's Graph Toolkit that allows designers to create:

- **Quests** with stages, branching paths, and player choices
- **Tasks** of various types (Int, Bool, Location, Discovery, Timed, String)
- **QuestLines** grouping related quests
- **Conditions** for triggering events
- **Events** for game system communication
- **ID_SO** assets for unique identifiers

### Key Design Principle: Modularity Through Subgraphs

> **SUPER IMPORTANT**: Every reusable component becomes a **subgraph**. This keeps main graphs clean and enables asset reuse across multiple quests.

```
                    ┌─────────────────────────────────────────────┐
                    │           QuestLineGraph.questline          │
                    │  ┌─────────┐  ┌─────────┐  ┌─────────┐     │
                    │  │ Quest   │─▶│ Quest   │─▶│ Quest   │     │
                    │  │Subgraph │  │Subgraph │  │Subgraph │     │
                    │  └─────────┘  └─────────┘  └─────────┘     │
                    └─────────────────────────────────────────────┘
                                        │
                                        ▼
                    ┌─────────────────────────────────────────────┐
                    │            QuestGraph.quest                  │
                    │  ┌────────┐  ┌────────────┐  ┌────────┐    │
                    │  │ Stage  │─▶│   Stage    │─▶│ Stage  │    │
                    │  │Subgraph│  │  Subgraph  │  │Subgraph│    │
                    │  └────────┘  │(with choice)│  └────────┘   │
                    │              └────────────┘                 │
                    └─────────────────────────────────────────────┘
                                        │
                                        ▼
                    ┌─────────────────────────────────────────────┐
                    │            StageGraph.stage                  │
                    │  ┌──────────┐  ┌──────────┐                 │
                    │  │TaskGroup │  │TaskGroup │                 │
                    │  │ Subgraph │  │ Subgraph │                 │
                    │  └──────────┘  └──────────┘                 │
                    └─────────────────────────────────────────────┘
                                        │
                                        ▼
                    ┌─────────────────────────────────────────────┐
                    │         TaskGroupGraph.taskgroup             │
                    │  ┌──────┐  ┌──────┐  ┌──────┐              │
                    │  │ Task │  │ Task │  │ Task │              │
                    │  │ Node │  │ Node │  │ Node │              │
                    │  └──────┘  └──────┘  └──────┘              │
                    └─────────────────────────────────────────────┘
```

### Benefits

| Benefit | Description |
|---------|-------------|
| **Reusability** | Create a "Kill 10 Goblins" task group once, use it in multiple quests |
| **Clarity** | Main quest graph shows high-level flow, not individual tasks |
| **Maintainability** | Update a subgraph, all references update automatically |
| **Team Workflow** | Designers work on subgraphs independently |
| **Version Control** | Smaller, focused files = fewer merge conflicts |

---

## 2. Prerequisites

### Unity Version

- **Minimum**: Unity 6.2+ (Graph Toolkit experimental package requirement)
- **Recommended**: Unity 6.3+ (better Graph Toolkit stability)

### Required Packages

```json
{
    "dependencies": {
        "com.unity.graphtoolkit": "0.4.0-exp.2"
    }
}
```

### HelloDev Dependencies

Your quest system already has these - they remain unchanged:

- `com.hellodev.utils` (1.3.0+)
- `com.hellodev.events` (1.1.0+)
- `com.hellodev.conditions` (1.3.0+)
- `com.hellodev.ids` (1.1.0+)
- `com.unity.localization`

### Skills Required

| Skill | Level | Where Used |
|-------|-------|------------|
| C# | Intermediate | All graph classes |
| Unity Editor Scripting | Basic | Menu items, asset creation |
| ScriptableObjects | Intermediate | Data conversion |
| Graph Theory | Basic | Understanding node/edge relationships |

---

## 3. Architecture Overview

### Graph Toolkit Core Concepts

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Graph Toolkit Architecture                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐    Your Code Extends These                            │
│  │    Graph     │◄───────────────────────────────────────┐              │
│  │  (abstract)  │                                        │              │
│  └──────────────┘                                        │              │
│         │                                                │              │
│         │ contains                                       │              │
│         ▼                                                │              │
│  ┌──────────────┐         ┌──────────────┐              │              │
│  │    Node      │◄────────│  BlockNode   │              │              │
│  │  (abstract)  │         │  ContextNode │              │              │
│  └──────────────┘         └──────────────┘              │              │
│         │                                                │              │
│         │ defines                                        │              │
│         ▼                                                │              │
│  ┌──────────────┐                                        │              │
│  │    IPort     │    Input/Output connection points      │              │
│  │  (interface) │                                        │              │
│  └──────────────┘                                        │              │
│         │                                                │              │
│         │ connects via                                   │              │
│         ▼                                                │              │
│  ┌──────────────┐         ┌──────────────┐              │              │
│  │    Wire      │         │   Portal     │  (wireless)   │              │
│  │  (visible)   │         │   (pairs)    │               │              │
│  └──────────────┘         └──────────────┘              │              │
│                                                                          │
│  Built-in UI Components:                                                 │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │
│  │ Blackboard  │ │   Minimap   │ │  Inspector  │ │Search Window│       │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘       │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                       │
│  │Sticky Notes │ │  Placemats  │ │  Subgraphs  │                       │
│  └─────────────┘ └─────────────┘ └─────────────┘                       │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Graph Toolkit Features Summary

| Feature | Description | Quest System Usage |
|---------|-------------|-------------------|
| **Nodes** | Fundamental building blocks representing operations | Stages, Tasks, Choices |
| **Ports** | Connection points on nodes (input/output) | Stage flow, task dependencies |
| **Wires** | Visible connections between ports | Stage transitions |
| **Portals** | Wireless connection pairs (reduce visual clutter) | Complex branching paths |
| **Blackboard** | Panel for managing graph-level variables | Quest parameters, flags |
| **Variables** | Data containers accessible throughout graph | Progress tracking, conditions |
| **Subgraphs** | Nested graphs for organization and reusability | Reusable task groups, stages |
| **Minimap** | Overview navigation for large graphs | Navigate complex quests |
| **Sticky Notes** | Annotations for design documentation | Quest design notes |
| **Placemats** | Visual grouping of related nodes | Group stages by act |
| **Graph Inspector** | Detailed element properties panel | Edit node properties |

### SubgraphAttribute - How It Works

> **CRITICAL**: The `[Subgraph]` attribute goes on the **SUBGRAPH class**, NOT the main graph!

```csharp
// CORRECT: SubgraphAttribute on the subgraph, pointing to its parent
[Subgraph(typeof(QuestLineGraph))]  // "I can be used as subgraph IN QuestLineGraph"
[Graph("quest")]  // Extension WITHOUT dot
public class QuestGraph : Graph { }

// The main graph declares it supports subgraphs via second parameter
[Graph("questline", GraphOptions.SupportsSubgraphs)]  // Second positional param
public class QuestLineGraph : Graph { }

// Combine multiple options with bitwise OR
[Graph("quest", GraphOptions.SupportsSubgraphs | GraphOptions.Default)]
```

**GraphAttribute Syntax**:
```csharp
// Constructor signature:
public GraphAttribute(string extension, GraphOptions options = GraphOptions.Default)

// IMPORTANT: Extension WITHOUT dot - Unity adds it automatically
// Examples:
[Graph("mygraph")]  // Creates .mygraph files
[Graph("mygraph", GraphOptions.SupportsSubgraphs)]  // With subgraph support
[Graph("mygraph", GraphOptions.SupportsSubgraphs | GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly)]
```

> **Note**: Based on the VisualNovelDirector sample (Graph Toolkit 0.4.0-exp.2), extensions are specified **without the dot prefix**. Unity adds the dot when creating the file importer.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SubgraphAttribute Relationship                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Main Graph (Parent)                    Subgraph (Child)                 │
│  ──────────────────                     ────────────────                 │
│                                                                          │
│  [Graph("questline",                    [Subgraph(typeof(QuestLineGraph))]
│   GraphOptions.SupportsSubgraphs)]      [Graph("quest")]                 │
│  class QuestLineGraph : Graph           class QuestGraph : Graph         │
│         │                                       │                        │
│         │  "I accept subgraphs"                 │  "I belong in          │
│         │                                       │   QuestLineGraph"      │
│         └───────────────────────────────────────┘                        │
│                                                                          │
│  Multiple subgraph types can point to the same parent:                   │
│                                                                          │
│  [Subgraph(typeof(QuestGraph))]         [Subgraph(typeof(QuestGraph))]   │
│  class StageGraph : Graph               class ConditionGraph : Graph     │
│         │                                       │                        │
│         └──────────────┬────────────────────────┘                        │
│                        │                                                 │
│                        ▼                                                 │
│  [Graph("quest", GraphOptions.SupportsSubgraphs)]                        │
│  class QuestGraph : Graph                                                │
│                                                                          │
│  DEFAULT BEHAVIOR: If main graph has SupportsSubgraphs but no            │
│  SubgraphAttribute exists, the main graph itself becomes the default     │
│  subgraph type.                                                          │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Patterns from VisualNovelDirector Sample

> The following patterns are validated against Unity's official Graph Toolkit 0.4.0-exp.2 sample.

#### Base Node Class Pattern

Create a base class for all your nodes to share common behavior:

```csharp
using System;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Base class for all Quest Graph nodes.
    /// </summary>
    [Serializable]
    internal abstract class QuestNode : Node
    {
        public const string EXECUTION_PORT_NAME = "Flow";

        /// <summary>
        /// Adds standard flow ports (In/Out) for sequential execution.
        /// </summary>
        protected void AddFlowPorts(IPortDefinitionContext context)
        {
            context.AddInputPort(EXECUTION_PORT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort(EXECUTION_PORT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

#### Port Definition Patterns

```csharp
// Execution/Flow ports (untyped - just for sequencing)
context.AddInputPort("ExecutionPort")
    .WithDisplayName(string.Empty)          // No label
    .WithConnectorUI(PortConnectorUI.Arrowhead)
    .Build();

// Typed data ports
context.AddInputPort<string>("ActorName")
    .WithDisplayName("Actor Name")
    .Build();

context.AddInputPort<int>("StageIndex")
    .WithDisplayName("Stage Index")
    .Build();

context.AddInputPort<Sprite>("Icon")
    .WithDisplayName("Stage Icon")
    .Build();

// Enum ports
context.AddInputPort<TaskExecutionMode>("ExecutionMode")
    .WithDisplayName("Execution Mode")
    .Build();

// Output ports (same pattern)
context.AddOutputPort<StageFlow>("Then")
    .WithDisplayName("Then")
    .Build();
```

#### Graph Lifecycle Methods

```csharp
[Graph("questline", GraphOptions.SupportsSubgraphs)]
[Serializable]
public class QuestLineGraph : Graph
{
    // Called when graph is modified
    public override void OnGraphChanged(GraphLogger logger)
    {
        base.OnGraphChanged(logger);
        ValidateGraph(logger);
    }

    void ValidateGraph(GraphLogger logger)
    {
        var startNodes = GetNodes().OfType<QuestStartNode>().ToList();

        if (startNodes.Count == 0)
        {
            logger.LogError("Add a StartNode to your quest graph.", this);
        }
        else if (startNodes.Count > 1)
        {
            foreach (var node in startNodes.Skip(1))
            {
                logger.LogWarning("Only one StartNode is allowed.", node);
            }
        }
    }
}
```

#### ScriptedImporter Pattern (Graph → Runtime Asset)

```csharp
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Imports QuestGraph assets and generates Quest_SO at import time.
    /// </summary>
    [ScriptedImporter(1, QuestGraph.AssetExtension)]
    internal class QuestGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load the graph
            var graph = GraphDatabase.LoadGraphForImporter<QuestGraph>(ctx.assetPath);
            if (graph == null)
            {
                Debug.LogError($"Failed to load graph: {ctx.assetPath}");
                return;
            }

            // Find start node
            var startNode = graph.GetNodes().OfType<QuestStartNode>().FirstOrDefault();
            if (startNode == null) return;

            // Create runtime asset
            var quest = ScriptableObject.CreateInstance<Quest_SO>();

            // Convert graph nodes to quest data
            ConvertGraphToQuest(graph, quest);

            // Add to asset and set as main object
            ctx.AddObjectToAsset("Quest", quest);
            ctx.SetMainObject(quest);
        }

        void ConvertGraphToQuest(QuestGraph graph, Quest_SO quest)
        {
            // Walk the graph and convert nodes to quest stages
            // ...
        }
    }
}
```

#### Accessing Port Values

```csharp
// Get port by name
IPort port = node.GetInputPortByName("StageIndex");

// Check if connected
if (port.isConnected)
{
    // Get connected port
    IPort connectedPort = port.firstConnectedPort;
    INode connectedNode = connectedPort.GetNode();

    // Handle different node types
    if (connectedNode is IVariableNode variableNode)
    {
        variableNode.variable.TryGetDefaultValue<int>(out var value);
    }
    else if (connectedNode is IConstantNode constantNode)
    {
        constantNode.TryGetValue<int>(out var value);
    }
}
else
{
    // Get embedded/default value
    port.TryGetValue<int>(out var value);
}
```

### Quest System Data Model (What We're Visualizing)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     HelloDev Quest System Hierarchy                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  QuestLine_SO                                                            │
│  ├── devName, questLineId                                               │
│  ├── displayName (LocalizedString)                                      │
│  ├── quests: List<Quest_SO>                                             │
│  ├── prerequisiteLine: QuestLine_SO                                     │
│  ├── requireSequentialCompletion: bool                                  │
│  ├── failOnAnyQuestFailed: bool                                         │
│  └── completionRewards: List<RewardInstance>                            │
│                                                                          │
│  Quest_SO                                                                │
│  ├── devName, questId                                                   │
│  ├── displayName, questDescription (LocalizedString)                    │
│  ├── questType: QuestType_SO                                            │
│  ├── stages: List<QuestStage>                                           │
│  │   ├── stageIndex, stageName                                          │
│  │   ├── isTerminal, isOptional, isHidden                               │
│  │   ├── taskGroups: List<TaskGroup>                                    │
│  │   │   ├── groupName, executionMode                                   │
│  │   │   ├── requiredCount (for OptionalXofY)                           │
│  │   │   └── tasks: List<Task_SO>                                       │
│  │   └── transitions: List<StageTransition>                             │
│  │       ├── targetStageIndex, trigger, priority                        │
│  │       ├── conditions: List<Condition_SO>                             │
│  │       ├── isPlayerChoice, choiceId, choiceText                       │
│  │       └── worldFlagsOnSelect: List<WorldFlagModification>            │
│  ├── startConditions: List<Condition_SO>                                │
│  ├── failureConditions: List<Condition_SO>                              │
│  └── rewards: List<RewardInstance>                                      │
│                                                                          │
│  Task_SO (Abstract - 6 concrete types)                                   │
│  ├── TaskInt_SO     → requiredCount (kill X, collect Y)                 │
│  ├── TaskBool_SO    → single condition check                            │
│  ├── TaskString_SO  → string matching                                   │
│  ├── TaskLocation_SO → reach specific location                          │
│  ├── TaskDiscovery_SO → find X items/clues                              │
│  └── TaskTimed_SO   → complete within time limit                        │
│                                                                          │
│  Condition_SO (Various types)                                            │
│  ├── ConditionInt_SO, ConditionBool_SO, etc.                            │
│  ├── ConditionWorldFlagBool_SO, ConditionWorldFlagInt_SO                │
│  ├── ConditionQuestState_SO (quest chains)                              │
│  └── ConditionQuestLineState_SO (questline chains)                      │
│                                                                          │
│  GameEvent_SO<T>                                                         │
│  ├── GameEventVoid_SO    → no parameter                                 │
│  ├── GameEventBool_SO    → bool parameter                               │
│  ├── GameEventInt_SO     → int parameter                                │
│  ├── GameEventFloat_SO   → float parameter                              │
│  └── GameEventString_SO  → string parameter                             │
│                                                                          │
│  ID_SO                                                                   │
│  ├── devName, id (GUID)                                                 │
│  └── displayName (LocalizedString)                                      │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Phase 1: Foundation Setup

### Complexity: Low | Time: ~2 hours | Priority: MANDATORY

### Step 1.1: Create Assembly Definition

Create the editor assembly that references Graph Toolkit.

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/HelloDev.QuestSystem.QuestGraph.Editor.asmdef`

```json
{
    "name": "HelloDev.QuestSystem.QuestGraph.Editor",
    "rootNamespace": "HelloDev.QuestSystem.QuestGraph.Editor",
    "references": [
        "Unity.GraphToolkit.Editor",
        "Unity.GraphToolkit.Common.Editor",
        "HelloDev.QuestSystem",
        "HelloDev.Conditions",
        "HelloDev.Events",
        "HelloDev.IDs",
        "HelloDev.Utils",
        "Unity.Localization"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [
        {
            "name": "com.sirenix.odin.inspector",
            "expression": "",
            "define": "ODIN_INSPECTOR"
        }
    ],
    "noEngineReferences": false
}
```

### Step 1.2: Create Base Graph Classes

Each graph type in your system needs its own class. Note the **SubgraphAttribute** on child graphs pointing to their parent.

> **Key Pattern**: Use `[Subgraph(typeof(ParentGraph))]` to declare that a graph can be embedded in another graph type.

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Graphs/QuestLineGraph.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Graph for designing QuestLines - collections of related quests.
    /// This is the highest-level graph type (no parent).
    /// </summary>
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestLineGraph : Graph
    {
        public const string AssetExtension = "questline";

        #region Serialized Data - Identity

        [SerializeField] private string devName;
        [SerializeField] private string questLineId;

        #endregion

        #region Serialized Data - Display

        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;

        #endregion

        #region Serialized Data - Settings

        [SerializeField] private bool requireSequentialCompletion = true;
        [SerializeField] private bool failOnAnyQuestFailed = false;

        #endregion

        #region Serialized Data - Chaining

        [SerializeField] private QuestLine_SO prerequisiteLine;

        #endregion

        #region Serialized Data - Rewards

        [SerializeField] private List<RewardInstance> completionRewards = new();

        #endregion

        #region Serialized Data - Export

        [SerializeField] private QuestLine_SO targetAsset;

        #endregion

        #region Properties

        public string DevName { get => devName; set => devName = value; }
        public string QuestLineId => questLineId;
        public LocalizedString DisplayName => displayName;
        public bool RequireSequentialCompletion { get => requireSequentialCompletion; set => requireSequentialCompletion = value; }
        public QuestLine_SO PrerequisiteLine => prerequisiteLine;
        public List<RewardInstance> CompletionRewards => completionRewards;
        public QuestLine_SO TargetAsset { get => targetAsset; set => targetAsset = value; }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/QuestLine Graph", false, 100)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<QuestLineGraph>();
        }

        #endregion

        #region Lifecycle

        public override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(devName)) devName = name;
            if (string.IsNullOrEmpty(questLineId)) questLineId = Guid.NewGuid().ToString();
        }

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            ValidateGraph(logger);
        }

        #endregion

        #region Validation

        private void ValidateGraph(GraphLogger logger)
        {
            var startNodes = GetNodes().OfType<QuestLineStartNode>().ToList();
            if (startNodes.Count == 0)
                logger.LogError("QuestLine graph requires a QuestLineStartNode.", this);
            else if (startNodes.Count > 1)
                foreach (var node in startNodes.Skip(1))
                    logger.LogWarning("Only one QuestLineStartNode is allowed.", node);
        }

        #endregion
    }
}
```

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Graphs/QuestGraph.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Graph for designing individual Quests with stages and branching.
    /// Can be used as a subgraph in QuestLineGraph.
    /// </summary>
    [Subgraph(typeof(QuestLineGraph))]  // <-- Declares this as subgraph of QuestLineGraph
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestGraph : Graph
    {
        public const string AssetExtension = "quest";

        #region Serialized Data - Identity

        [SerializeField] private string devName;
        [SerializeField] private string questId;
        [SerializeField] private QuestType_SO questType;
        [SerializeField] private int recommendedLevel = -1;

        #endregion

        #region Serialized Data - Display

        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString questDescription;
        [SerializeField] private LocalizedString questLocation;
        [SerializeField] private Sprite questSprite;

        #endregion

        #region Serialized Data - Conditions

        [SerializeField] private List<Condition_SO> startConditions = new();
        [SerializeField] private List<Condition_SO> failureConditions = new();

        #endregion

        #region Serialized Data - Rewards

        [SerializeField] private List<RewardInstance> rewards = new();

        #endregion

        #region Serialized Data - Export

        [SerializeField] private Quest_SO targetAsset;

        #endregion

        #region Properties

        public string DevName { get => devName; set => devName = value; }
        public string QuestId => questId;
        public QuestType_SO QuestType { get => questType; set => questType = value; }
        public int RecommendedLevel => recommendedLevel;
        public LocalizedString DisplayName => displayName;
        public LocalizedString QuestDescription => questDescription;
        public List<Condition_SO> StartConditions => startConditions;
        public List<RewardInstance> Rewards => rewards;
        public Quest_SO TargetAsset { get => targetAsset; set => targetAsset = value; }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/Quest Graph", false, 101)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<QuestGraph>();
        }

        #endregion

        #region Lifecycle

        public override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }
        }

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            ValidateGraph(logger);
        }

        #endregion

        #region Validation

        private void ValidateGraph(GraphLogger logger)
        {
            // Implemented in Phase 6
        }

        #endregion
    }
}
```

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Graphs/StageGraph.cs`

```csharp
using System;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Subgraph for designing individual Quest Stages.
    /// Contains task groups and transition logic.
    /// </summary>
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class StageGraph : Graph
    {
        public const string AssetExtension = "stage";

        #region Serialized Data

        [SerializeField] private int stageIndex;
        [SerializeField] private string stageName = "New Stage";
        [SerializeField] private bool isTerminal;
        [SerializeField] private bool isOptional;
        [SerializeField] private bool isHidden;

        #endregion

        #region Properties

        public int StageIndex
        {
            get => stageIndex;
            set => stageIndex = value;
        }

        public string StageName
        {
            get => stageName;
            set => stageName = value;
        }

        public bool IsTerminal
        {
            get => isTerminal;
            set => isTerminal = value;
        }

        public bool IsOptional
        {
            get => isOptional;
            set => isOptional = value;
        }

        public bool IsHidden
        {
            get => isHidden;
            set => isHidden = value;
        }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/Stage Subgraph", false, 102)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<StageGraph>();
        }

        #endregion
    }
}
```

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Graphs/TaskGroupGraph.cs`

```csharp
using System;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Subgraph for designing Task Groups - collections of tasks with execution modes.
    /// This is a highly reusable component.
    /// </summary>
    [Graph(AssetExtension)]
    [Serializable]
    public class TaskGroupGraph : Graph
    {
        public const string AssetExtension = "taskgroup";

        #region Serialized Data

        [SerializeField] private string groupName = "Task Group";
        [SerializeField] private TaskExecutionMode executionMode = TaskExecutionMode.Sequential;
        [SerializeField] private int requiredCount = 1;

        #endregion

        #region Properties

        public string GroupName
        {
            get => groupName;
            set => groupName = value;
        }

        public TaskExecutionMode ExecutionMode
        {
            get => executionMode;
            set => executionMode = value;
        }

        public int RequiredCount
        {
            get => requiredCount;
            set => requiredCount = value;
        }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/TaskGroup Subgraph", false, 103)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TaskGroupGraph>();
        }

        #endregion
    }
}
```

### Step 1.3: Verify Setup

1. Open Unity and wait for compilation
2. Navigate to `Assets > Create > HelloDev > Quest System > Graphs`
3. You should see:
   - QuestLine Graph
   - Quest Graph
   - Stage Subgraph
   - TaskGroup Subgraph
4. Create each one and double-click to open the graph window
5. Verify the canvas appears with toolbar and empty workspace

---

## 5. Phase 2: Core Node Types

### Complexity: Medium | Time: ~4 hours | Priority: MANDATORY

### Node Type Overview

| Node | Purpose | Ports | Used In |
|------|---------|-------|---------|
| `QuestBaseNode` | **Base class** for all nodes | - | - |
| `QuestLineStartNode` | Entry point for questline | Out: First Quest | QuestLineGraph |
| `QuestStartNode` | Entry point for quest | Out: First Stage | QuestGraph |
| `StageStartNode` | Entry point for stage subgraph | Out: First TaskGroup | StageGraph |
| `TaskGroupStartNode` | Entry point for task group subgraph | Out: First Task | TaskGroupGraph |
| `QuestRefNode` | Reference to quest | In/Out: QuestFlow | QuestLineGraph |
| `StageNode` | Quest phase | In: From, Out: Then/Else/Choices | QuestGraph |
| `ChoiceNode` | Player branching | In: ChoiceFlow, Out: StageFlow | QuestGraph |
| `ConditionGateNode` | Automatic condition branching | In: StageFlow, Out: Then/Else | QuestGraph |
| `EventTriggerNode` | Fire GameEvents in flow | In: StageFlow, Out: Then | QuestGraph |
| `WorldFlagSetNode` | Set world flags in flow | In: StageFlow, Out: Then | QuestGraph |
| `RewardNode` | Grant rewards in flow | In: StageFlow, Out: Then | QuestGraph |
| `TaskGroupNode` | Group of tasks | In: From Stage, Out: Then/Else | StageGraph |
| `TaskNode` | Individual task | In: Group, Out: Then | TaskGroupGraph |

> **Key Pattern**: All nodes extend `QuestBaseNode` and use `OnDefineOptions` to define editable fields. Options appear in the node header and Graph Inspector when selected. Use `.ShowInInspectorOnly()` for options that should only appear in the Inspector. Access option values via `GetOptionValue<T>(optionName)`.

### Step 2.1: Create Port Types

Port types define what can connect to what.

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Ports/QuestPorts.cs`

```csharp
using System;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Ports
{
    /// <summary>
    /// Represents flow between quests in a QuestLine.
    /// </summary>
    [Serializable]
    public class QuestFlow { }

    /// <summary>
    /// Represents flow between stages in a Quest.
    /// </summary>
    [Serializable]
    public class StageFlow { }

    /// <summary>
    /// Represents flow within a TaskGroup.
    /// </summary>
    [Serializable]
    public class TaskFlow { }

    /// <summary>
    /// Represents a condition evaluation output.
    /// </summary>
    [Serializable]
    public class ConditionResult { }

    /// <summary>
    /// Represents a player choice branch.
    /// </summary>
    [Serializable]
    public class ChoiceFlow { }
}
```

### Step 2.2: Create Base Node Class

All nodes extend this base class for consistent port patterns and option access.

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/QuestBaseNode.cs`

```csharp
using System;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Base class for all Quest Graph nodes.
    /// Provides common port definitions and option utilities.
    /// </summary>
    [Serializable]
    public abstract class QuestBaseNode : Node
    {
        public const string FLOW_PORT_NAME = "Flow";

        #region Option Helpers

        /// <summary>
        /// Gets the value of a node option by name.
        /// Returns default(T) if the option doesn't exist or has no value.
        /// </summary>
        protected T GetOptionValue<T>(string optionName)
        {
            var option = GetNodeOptionByName(optionName);
            if (option != null && option.TryGetValue<T>(out var value))
                return value;
            return default;
        }

        #endregion

        /// <summary>
        /// Adds standard input and output flow ports for sequential execution.
        /// </summary>
        protected void AddFlowPorts(IPortDefinitionContext context)
        {
            context.AddInputPort(FLOW_PORT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort(FLOW_PORT_NAME)
                .WithDisplayName(string.Empty)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected void AddOutputFlowPort(IPortDefinitionContext context, string displayName = "")
        {
            context.AddOutputPort(FLOW_PORT_NAME)
                .WithDisplayName(displayName)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        protected void AddInputFlowPort(IPortDefinitionContext context, string displayName = "")
        {
            context.AddInputPort(FLOW_PORT_NAME)
                .WithDisplayName(displayName)
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

### Step 2.3: Create Start Nodes

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/QuestLineStartNode.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point for a QuestLine. Every QuestLineGraph should have exactly one.
    /// </summary>
    [Serializable]
    public class QuestLineStartNode : QuestBaseNode
    {
        [SerializeField] private List<Condition_SO> startConditions = new();

        public List<Condition_SO> StartConditions => startConditions;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddOutputPort<QuestFlow>("FirstQuest")
                .WithDisplayName("First Quest")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/QuestStartNode.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point for a Quest. Every QuestGraph should have exactly one.
    /// </summary>
    [Serializable]
    public class QuestStartNode : QuestBaseNode
    {
        [SerializeField] private List<Condition_SO> startConditions = new();

        public List<Condition_SO> StartConditions => startConditions;

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddOutputPort<StageFlow>("FirstStage")
                .WithDisplayName("First Stage")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

### Step 2.4: Create Stage Node

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/StageNode.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a Quest Stage - a discrete phase of quest progression.
    /// Stages can be terminal (quest ends), optional, or hidden.
    /// Connect ChoiceNodes to the Choice output for player branching.
    /// </summary>
    [Serializable]
    public class StageNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_STAGE_INDEX = "StageIndex";
        private const string OPT_STAGE_NAME = "StageName";
        private const string OPT_JOURNAL_ENTRY = "JournalEntry";
        private const string OPT_STAGE_ICON = "StageIcon";
        private const string OPT_IS_TERMINAL = "IsTerminal";
        private const string OPT_IS_OPTIONAL = "IsOptional";
        private const string OPT_IS_HIDDEN = "IsHidden";
        private const string OPT_HAS_PLAYER_CHOICES = "HasPlayerChoices";

        #endregion

        #region Properties

        public int StageIndex => GetOptionValue<int>(OPT_STAGE_INDEX);
        public string StageName => GetOptionValue<string>(OPT_STAGE_NAME);
        public LocalizedString JournalEntry => GetOptionValue<LocalizedString>(OPT_JOURNAL_ENTRY);
        public Sprite StageIcon => GetOptionValue<Sprite>(OPT_STAGE_ICON);
        public bool IsTerminal => GetOptionValue<bool>(OPT_IS_TERMINAL);
        public bool IsOptional => GetOptionValue<bool>(OPT_IS_OPTIONAL);
        public bool IsHidden => GetOptionValue<bool>(OPT_IS_HIDDEN);
        public bool HasPlayerChoices => GetOptionValue<bool>(OPT_HAS_PLAYER_CHOICES);

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Identity options (shown on node header)
            context.AddOption<int>(OPT_STAGE_INDEX)
                .WithDisplayName("Stage Index")
                .WithDefaultValue(0)
                .WithTooltip("Unique index for this stage (use gaps of 10)")
                .Delayed();

            context.AddOption<string>(OPT_STAGE_NAME)
                .WithDisplayName("Stage Name")
                .WithDefaultValue("New Stage")
                .Delayed();

            // Display options (Inspector only)
            context.AddOption<LocalizedString>(OPT_JOURNAL_ENTRY)
                .WithDisplayName("Journal Entry")
                .ShowInInspectorOnly();

            context.AddOption<Sprite>(OPT_STAGE_ICON)
                .WithDisplayName("Stage Icon")
                .ShowInInspectorOnly();

            // Flag options
            context.AddOption<bool>(OPT_IS_TERMINAL)
                .WithDisplayName("Is Terminal")
                .WithDefaultValue(false);

            context.AddOption<bool>(OPT_IS_OPTIONAL)
                .WithDisplayName("Is Optional")
                .WithDefaultValue(false)
                .ShowInInspectorOnly();

            context.AddOption<bool>(OPT_IS_HIDDEN)
                .WithDisplayName("Is Hidden")
                .WithDefaultValue(false)
                .ShowInInspectorOnly();

            context.AddOption<bool>(OPT_HAS_PLAYER_CHOICES)
                .WithDisplayName("Has Player Choices")
                .WithDefaultValue(false);
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous stage or start node
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Terminal stages have no output ports
            if (IsTerminal)
                return;

            // Default completion output
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Failure output
            context.AddOutputPort<StageFlow>("Else")
                .WithDisplayName("Else")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // If stage has player choices, add a choice output port
            if (HasPlayerChoices)
            {
                context.AddOutputPort<ChoiceFlow>("Choices")
                    .WithDisplayName("Player Choices")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion
    }
}
```

### Step 2.4: Create Task Node

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Nodes/TaskNode.cs`

```csharp
using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents an individual Task in a TaskGroup.
    /// References an existing Task_SO asset directly - no enum needed.
    /// The task type is determined by the concrete Task_SO subclass assigned.
    /// </summary>
    /// <remarks>
    /// Design: Task_SO assets are created separately using Unity's CreateAssetMenu.
    /// This node just references them, following the Open/Closed principle.
    /// Adding new task types requires no changes to this class.
    /// </remarks>
    [Serializable]
    public class TaskNode : Node
    {
        #region Serialized Data

        // Direct reference to Task_SO asset (TaskInt_SO, TaskLocation_SO, etc.)
        // The concrete type determines behavior - no enum needed
        [SerializeField] private Task_SO taskAsset;

        #endregion

        #region Properties

        public Task_SO TaskAsset => taskAsset;

        // Convenience properties that read from the referenced asset
        public string DevName => taskAsset != null ? taskAsset.DevName : "No Task Assigned";
        public string TaskTypeName => taskAsset != null ? taskAsset.GetType().Name : "None";

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From TaskGroup
            context.AddInputPort<TaskFlow>("In")
                .WithDisplayName("From Group")
                .Build();

            // Output: Task completion (for sequential mode)
            context.AddOutputPort<TaskFlow>("Complete")
                .WithDisplayName("Then")
                .Build();
        }

        #endregion
    }
}
```

### Step 2.5: Create TaskGroup Node

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Nodes/TaskGroupNode.cs`

```csharp
using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a TaskGroup within a Stage.
    /// Can reference a TaskGroupGraph subgraph for reusability.
    /// </summary>
    [Serializable]
    public class TaskGroupNode : Node
    {
        #region Constants

        private const string OPT_GROUP_NAME = "GroupName";
        private const string OPT_EXECUTION_MODE = "ExecutionMode";
        private const string OPT_REQUIRED_COUNT = "RequiredCount";

        #endregion

        #region Serialized Data

        [SerializeField] private string groupName = "Task Group";
        [SerializeField] private TaskExecutionMode executionMode = TaskExecutionMode.Sequential;
        [SerializeField] private int requiredCount = 1;

        // Reference to subgraph (for reusable task groups)
        [SerializeField] private TaskGroupGraph subgraph;

        #endregion

        #region Properties

        public string GroupName => groupName;
        public TaskExecutionMode ExecutionMode => executionMode;
        public int RequiredCount => requiredCount;
        public TaskGroupGraph Subgraph => subgraph;

        #endregion

        #region Options Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_GROUP_NAME)
                .WithDisplayName("Group Name")
                .WithDefaultValue("Task Group")
                .Build();

            context.AddOption<TaskExecutionMode>(OPT_EXECUTION_MODE)
                .WithDisplayName("Execution Mode")
                .WithDefaultValue(TaskExecutionMode.Sequential)
                .Build();

            context.AddOption<int>(OPT_REQUIRED_COUNT)
                .WithDisplayName("Required Count")
                .WithDefaultValue(1)
                .Build();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From Stage")
                .Build();

            // Output: All tasks complete
            context.AddOutputPort<StageFlow>("Complete")
                .WithDisplayName("Group Complete")
                .Build();

            // Output: Group failed
            context.AddOutputPort<StageFlow>("Fail")
                .WithDisplayName("Group Failed")
                .Build();
        }

        #endregion
    }
}
```

### Step 2.6: Create Choice Node

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Nodes/ChoiceNode.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Represents a player choice that branches the quest.
    /// Connects to a Stage's choice output and leads to a target stage.
    /// </summary>
    [Serializable]
    public class ChoiceNode : Node
    {
        #region Constants

        private const string OPT_CHOICE_ID = "ChoiceId";
        private const string OPT_PRIORITY = "Priority";

        #endregion

        #region Serialized Data

        [SerializeField] private string choiceId;
        [SerializeField] private LocalizedString choiceText;
        [SerializeField] private LocalizedString choiceTooltip;
        [SerializeField] private Sprite choiceIcon;
        [SerializeField] private int priority = 0;

        [SerializeField] private List<Condition_SO> conditions = new();
        [SerializeField] private List<WorldFlagModification> worldFlagsOnSelect = new();

        #endregion

        #region Properties

        public string ChoiceId => choiceId;
        public LocalizedString ChoiceText => choiceText;
        public int Priority => priority;
        public List<Condition_SO> Conditions => conditions;
        public List<WorldFlagModification> WorldFlagsOnSelect => worldFlagsOnSelect;

        #endregion

        #region Options Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_CHOICE_ID)
                .WithDisplayName("Choice ID")
                .WithDefaultValue("")
                .Build();

            context.AddOption<int>(OPT_PRIORITY)
                .WithDisplayName("Priority")
                .WithDefaultValue(0)
                .Build();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage's choice port
            context.AddInputPort<ChoiceFlow>("In")
                .WithDisplayName("From Choice")
                .Build();

            // Output: To target stage
            context.AddOutputPort<StageFlow>("Target")
                .WithDisplayName("To Stage")
                .Build();
        }

        #endregion

        #region Lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            // Auto-generate choice ID if empty
            if (string.IsNullOrEmpty(choiceId))
            {
                choiceId = Guid.NewGuid().ToString().Substring(0, 8);
            }
        }

        #endregion
    }
}
```

### Step 2.7: Create Condition Gate Node (Automatic Branching)

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/ConditionGateNode.cs`

```csharp
using System;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that branches flow based on condition evaluation.
    /// Unlike ChoiceNode (player decision), this is automatic branching based on game state.
    /// </summary>
    [Serializable]
    public class ConditionGateNode : QuestBaseNode
    {
        private const string OPT_CONDITION = "Condition";
        private const string OPT_GATE_NAME = "GateName";
        private const string OPT_INVERT_RESULT = "InvertResult";

        public Condition_SO Condition => GetOptionValue<Condition_SO>(OPT_CONDITION);
        public string GateName => GetOptionValue<string>(OPT_GATE_NAME);
        public bool InvertResult => GetOptionValue<bool>(OPT_INVERT_RESULT);

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<Condition_SO>(OPT_CONDITION)
                .WithDisplayName("Condition")
                .WithTooltip("The condition to evaluate. True → Then, False → Else");

            context.AddOption<string>(OPT_GATE_NAME)
                .WithDisplayName("Gate Name")
                .WithDefaultValue("")
                .WithTooltip("Optional developer-friendly name for this gate")
                .Delayed();

            context.AddOption<bool>(OPT_INVERT_RESULT)
                .WithDisplayName("Invert Result")
                .WithDefaultValue(false)
                .WithTooltip("If true, swaps Then and Else behavior");
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then (True)")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<StageFlow>("Else")
                .WithDisplayName("Else (False)")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

### Step 2.8: Create Event Trigger Node (Fire GameEvents)

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/EventTriggerNode.cs`

```csharp
using System;
using Unity.GraphToolkit.Editor;
using HelloDev.Events;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that fires a GameEvent when reached in the quest flow.
    /// </summary>
    [Serializable]
    public class EventTriggerNode : QuestBaseNode
    {
        private const string OPT_EVENT = "Event";
        private const string OPT_TRIGGER_NAME = "TriggerName";
        private const string OPT_DELAY_FRAMES = "DelayFrames";

        public GameEventVoid_SO Event => GetOptionValue<GameEventVoid_SO>(OPT_EVENT);
        public string TriggerName => GetOptionValue<string>(OPT_TRIGGER_NAME);
        public int DelayFrames => GetOptionValue<int>(OPT_DELAY_FRAMES);

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<GameEventVoid_SO>(OPT_EVENT)
                .WithDisplayName("Event")
                .WithTooltip("The GameEvent to fire when this node is reached");

            context.AddOption<string>(OPT_TRIGGER_NAME)
                .WithDisplayName("Trigger Name")
                .WithDefaultValue("")
                .WithTooltip("Optional developer-friendly name for this trigger")
                .Delayed();

            context.AddOption<int>(OPT_DELAY_FRAMES)
                .WithDisplayName("Delay Frames")
                .WithDefaultValue(0)
                .WithTooltip("Number of frames to wait before firing (0 = immediate)")
                .ShowInInspectorOnly();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

### Step 2.9: Create World Flag Set Node

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/WorldFlagSetNode.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions.WorldFlags;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that sets world flags when reached in the quest flow.
    /// </summary>
    [Serializable]
    public class WorldFlagSetNode : QuestBaseNode
    {
        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_FLAG_LOCATOR = "FlagLocator";

        [SerializeField]
        private List<WorldFlagModification> modifications = new();

        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);
        public WorldFlagLocator_SO FlagLocator => GetOptionValue<WorldFlagLocator_SO>(OPT_FLAG_LOCATOR);
        public List<WorldFlagModification> Modifications => modifications;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_NODE_NAME)
                .WithDisplayName("Node Name")
                .WithDefaultValue("")
                .WithTooltip("Optional developer-friendly name for this node")
                .Delayed();

            context.AddOption<WorldFlagLocator_SO>(OPT_FLAG_LOCATOR)
                .WithDisplayName("Flag Locator")
                .WithTooltip("The WorldFlagLocator that provides access to flag runtime values");
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

### Step 2.10: Create Reward Node

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/RewardNode.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Events;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    [Serializable]
    public class RewardEntry
    {
        public ScriptableObject reward;
        public int amount = 1;
    }

    /// <summary>
    /// A node that grants rewards when reached in the quest flow.
    /// </summary>
    [Serializable]
    public class RewardNode : QuestBaseNode
    {
        private const string OPT_NODE_NAME = "NodeName";
        private const string OPT_XP_AMOUNT = "XpAmount";
        private const string OPT_CURRENCY_AMOUNT = "CurrencyAmount";
        private const string OPT_ON_REWARDS_GRANTED = "OnRewardsGranted";

        [SerializeField]
        private List<RewardEntry> rewards = new();

        public string NodeName => GetOptionValue<string>(OPT_NODE_NAME);
        public int XpAmount => GetOptionValue<int>(OPT_XP_AMOUNT);
        public int CurrencyAmount => GetOptionValue<int>(OPT_CURRENCY_AMOUNT);
        public GameEventVoid_SO OnRewardsGranted => GetOptionValue<GameEventVoid_SO>(OPT_ON_REWARDS_GRANTED);
        public List<RewardEntry> Rewards => rewards;

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<string>(OPT_NODE_NAME)
                .WithDisplayName("Node Name")
                .WithDefaultValue("")
                .Delayed();

            context.AddOption<int>(OPT_XP_AMOUNT)
                .WithDisplayName("XP Amount")
                .WithDefaultValue(0);

            context.AddOption<int>(OPT_CURRENCY_AMOUNT)
                .WithDisplayName("Currency Amount")
                .WithDefaultValue(0);

            context.AddOption<GameEventVoid_SO>(OPT_ON_REWARDS_GRANTED)
                .WithDisplayName("On Rewards Granted")
                .ShowInInspectorOnly();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }
    }
}
```

### Step 2.11: Create Quest Reference Node (for QuestLines)

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Nodes/QuestRefNode.cs`

```csharp
using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// References a Quest in a QuestLine graph.
    /// Can reference either a QuestGraph or an existing Quest_SO.
    /// </summary>
    [Serializable]
    public class QuestRefNode : Node
    {
        public enum ReferenceType
        {
            ExistingAsset,
            GraphAsset
        }

        #region Constants

        private const string OPT_REF_TYPE = "ReferenceType";

        #endregion

        #region Serialized Data

        [SerializeField] private ReferenceType referenceType = ReferenceType.GraphAsset;
        [SerializeField] private Quest_SO questAsset;
        [SerializeField] private QuestGraph questGraph;

        #endregion

        #region Properties

        public ReferenceType RefType => referenceType;
        public Quest_SO QuestAsset => questAsset;
        public QuestGraph QuestGraphAsset => questGraph;

        public string DisplayName
        {
            get
            {
                if (referenceType == ReferenceType.ExistingAsset && questAsset != null)
                    return questAsset.DevName;
                if (referenceType == ReferenceType.GraphAsset && questGraph != null)
                    return questGraph.DevName;
                return "Empty Quest Reference";
            }
        }

        #endregion

        #region Options Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<ReferenceType>(OPT_REF_TYPE)
                .WithDisplayName("Reference Type")
                .WithDefaultValue(ReferenceType.GraphAsset)
                .Build();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous quest
            context.AddInputPort<QuestFlow>("In")
                .WithDisplayName("After")
                .Build();

            // Output: To next quest
            context.AddOutputPort<QuestFlow>("Out")
                .WithDisplayName("Before")
                .Build();
        }

        #endregion
    }
}
```

---

## 6. Phase 3: Subgraph System (Modularity)

### Complexity: Medium-High | Time: ~4 hours | Priority: SUPER IMPORTANT

> This section covers the **modular subgraph architecture** that keeps your graphs clean and enables reuse.

### Subgraph Hierarchy

```
┌───────────────────────────────────────────────────────────────────────┐
│                        SUBGRAPH HIERARCHY                              │
├───────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  Level 1: QuestLineGraph (.questline)                                  │
│  ├── Contains: QuestRefNode → references QuestGraph subgraphs          │
│  └── Purpose: High-level narrative structure                           │
│                                                                        │
│  Level 2: QuestGraph (.quest)                                          │
│  ├── Contains: QuestStartNode, StageNode, ChoiceNode                   │
│  ├── Embeds: StageGraph subgraphs OR inline stages                     │
│  └── Purpose: Single quest with branching                              │
│                                                                        │
│  Level 3: StageGraph (.stage)                                          │
│  ├── Contains: TaskGroupNode (references TaskGroupGraph)               │
│  └── Purpose: Single stage with its task groups                        │
│                                                                        │
│  Level 4: TaskGroupGraph (.taskgroup)                                  │
│  ├── Contains: TaskNode (leaf nodes)                                   │
│  └── Purpose: Reusable task collections                                │
│                                                                        │
│  REUSABILITY EXAMPLES:                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │ "KillGoblins.taskgroup" - Kill 10 Goblins task group            │  │
│  │  └─► Used in: Quest A Stage 2, Quest B Stage 1, Quest C Stage 4 │  │
│  │                                                                  │  │
│  │ "TalkToMerchant.stage" - Investigation stage                    │  │
│  │  └─► Used in: Merchant Quest, Trade Guild Quest                  │  │
│  │                                                                  │  │
│  │ "GoblinThreat.quest" - Complete quest graph                     │  │
│  │  └─► Used in: Main Story QuestLine, Side Stories QuestLine      │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                        │
└───────────────────────────────────────────────────────────────────────┘
```

### Step 3.1: Configure Subgraph Support

> **IMPORTANT**: The `[Subgraph]` attribute goes on the **CHILD** (subgraph), pointing to the **PARENT** (main graph).
> The parent graph only needs `GraphOptions.SupportsSubgraphs`.

**Hierarchy Overview**:
```
QuestLineGraph (parent) ◄── QuestGraph (subgraph)
QuestGraph (parent)     ◄── StageGraph (subgraph)
StageGraph (parent)     ◄── TaskGroupGraph (subgraph)
```

**File**: `QuestLineGraph.cs` - The top-level parent

```csharp
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    // QuestLineGraph is a PARENT graph - it accepts subgraphs
    // No [Subgraph] attribute needed here (it's the root)
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestLineGraph : Graph
    {
        // Extension WITHOUT dot - Unity adds it automatically
        public const string AssetExtension = "questline";
        // ... existing code ...
    }
}
```

**File**: `QuestGraph.cs` - Both a subgraph AND a parent

```csharp
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    // QuestGraph is a SUBGRAPH of QuestLineGraph
    // AND a PARENT that accepts StageGraph subgraphs
    [Subgraph(typeof(QuestLineGraph))]  // "I can be embedded in QuestLineGraph"
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestGraph : Graph
    {
        public const string AssetExtension = "quest";
        // ... existing code ...
    }
}
```

**File**: `StageGraph.cs` - Both a subgraph AND a parent

```csharp
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    // StageGraph is a SUBGRAPH of QuestGraph
    // AND a PARENT that accepts TaskGroupGraph subgraphs
    [Subgraph(typeof(QuestGraph))]  // "I can be embedded in QuestGraph"
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class StageGraph : Graph
    {
        public const string AssetExtension = "stage";
        // ... existing code ...
    }
}
```

**File**: `TaskGroupGraph.cs` - Leaf subgraph (no children)

```csharp
using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    // TaskGroupGraph is a SUBGRAPH of StageGraph
    // It does NOT support subgraphs (leaf level)
    [Subgraph(typeof(StageGraph))]  // "I can be embedded in StageGraph"
    [Graph(AssetExtension)]  // No SupportsSubgraphs - this is the leaf level
    [Serializable]
    public class TaskGroupGraph : Graph
    {
        public const string AssetExtension = "taskgroup";
        // ... existing code ...
    }
}
```

**Complete Attribute Chain**:
```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Complete Subgraph Attribute Chain                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Level 1: QuestLineGraph                                                 │
│  ───────────────────────                                                │
│  [Graph("questline", GraphOptions.SupportsSubgraphs)]                   │
│  class QuestLineGraph : Graph { }                                        │
│         ▲                                                                │
│         │ [Subgraph(typeof(QuestLineGraph))]                            │
│         │                                                                │
│  Level 2: QuestGraph                                                     │
│  ──────────────────                                                     │
│  [Subgraph(typeof(QuestLineGraph))]                                      │
│  [Graph("quest", GraphOptions.SupportsSubgraphs)]                       │
│  class QuestGraph : Graph { }                                            │
│         ▲                                                                │
│         │ [Subgraph(typeof(QuestGraph))]                                │
│         │                                                                │
│  Level 3: StageGraph                                                     │
│  ──────────────────                                                     │
│  [Subgraph(typeof(QuestGraph))]                                          │
│  [Graph("stage", GraphOptions.SupportsSubgraphs)]                       │
│  class StageGraph : Graph { }                                            │
│         ▲                                                                │
│         │ [Subgraph(typeof(StageGraph))]                                │
│         │                                                                │
│  Level 4: TaskGroupGraph (Leaf)                                          │
│  ─────────────────────────────                                          │
│  [Subgraph(typeof(StageGraph))]                                          │
│  [Graph("taskgroup")]  // No SupportsSubgraphs                          │
│  class TaskGroupGraph : Graph { }                                        │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Step 3.2: Create Subgraph Reference Nodes

The subgraph reference nodes enable embedding reusable graphs within parent graphs. Each level of the hierarchy has a corresponding subgraph node:

| Parent Graph | Subgraph Node | Child Graph |
|-------------|---------------|-------------|
| QuestLineGraph | QuestRefNode | QuestGraph |
| QuestGraph | StageSubgraphNode | StageGraph |
| StageGraph | TaskGroupSubgraphNode | TaskGroupGraph |

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/Subgraphs/StageSubgraphNode.cs`

```csharp
using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that references a StageGraph subgraph.
    /// Used in QuestGraph to embed reusable stage definitions.
    /// </summary>
    /// <remarks>
    /// Subgraph nodes provide flow-through connections while encapsulating
    /// the stage's internal logic (task groups, transitions) in a separate asset.
    ///
    /// Use this when:
    /// - A stage pattern is reused across multiple quests
    /// - You want to keep the main quest graph clean
    /// - Multiple designers work on different stages
    /// </remarks>
    [Serializable]
    public class StageSubgraphNode : QuestBaseNode
    {
        #region Serialized Data

        [SerializeField] private StageGraph stageSubgraph;

        // Optional overrides - when set, these take precedence over subgraph values
        [SerializeField] private int overrideStageIndex = -1;
        [SerializeField] private string overrideStageName;

        #endregion

        #region Properties

        public StageGraph StageSubgraph
        {
            get => stageSubgraph;
            set => stageSubgraph = value;
        }

        public int EffectiveStageIndex
        {
            get
            {
                if (overrideStageIndex >= 0)
                    return overrideStageIndex;
                return stageSubgraph?.StageIndex ?? 0;
            }
        }

        public string EffectiveStageName
        {
            get
            {
                if (!string.IsNullOrEmpty(overrideStageName))
                    return overrideStageName;
                return stageSubgraph?.StageName ?? "Empty Stage";
            }
        }

        public string DisplayName => stageSubgraph != null
            ? $"[Stage] {EffectiveStageName}"
            : "[Stage] Empty Reference";

        public bool IsTerminal => stageSubgraph?.IsTerminal ?? false;
        public bool IsOptional => stageSubgraph?.IsOptional ?? false;
        public bool IsHidden => stageSubgraph?.IsHidden ?? false;

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous stage or quest start
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Only add output ports if not terminal
            if (!IsTerminal)
            {
                // Output: Stage completed successfully
                context.AddOutputPort<StageFlow>("Then")
                    .WithDisplayName("Then")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                // Output: Stage failed
                context.AddOutputPort<StageFlow>("Else")
                    .WithDisplayName("Else")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion
    }
}
```

### Step 3.3: Create TaskGroup Subgraph Node

**File**: `Assets/com.hellodev.questsystem/Editor/Graphs/Scripts/Nodes/Subgraphs/TaskGroupSubgraphNode.cs`

```csharp
using System;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that references a TaskGroupGraph subgraph.
    /// Used in StageGraph to embed reusable task group definitions.
    /// </summary>
    /// <remarks>
    /// This is the most commonly reused subgraph type. Examples:
    /// - "Kill 10 Goblins" task group used in multiple quests
    /// - "Collect Evidence" pattern reused across investigation stages
    /// - "Talk to NPC" interaction shared between quests
    ///
    /// Optional overrides allow customization without modifying the subgraph:
    /// - Override group name for context-specific display
    /// - Override execution mode for different completion logic
    /// - Override required count for X of Y variations
    /// </remarks>
    [Serializable]
    public class TaskGroupSubgraphNode : QuestBaseNode
    {
        #region Serialized Data

        [SerializeField] private TaskGroupGraph taskGroupSubgraph;

        // Optional overrides - when set, these take precedence over subgraph values
        [SerializeField] private string overrideGroupName;
        [SerializeField] private TaskExecutionMode overrideExecutionMode = TaskExecutionMode.Sequential;
        [SerializeField] private bool useOverrideExecutionMode = false;
        [SerializeField] private int overrideRequiredCount = -1;

        #endregion

        #region Properties

        public TaskGroupGraph TaskGroupSubgraph
        {
            get => taskGroupSubgraph;
            set => taskGroupSubgraph = value;
        }

        public string EffectiveGroupName
        {
            get
            {
                if (!string.IsNullOrEmpty(overrideGroupName))
                    return overrideGroupName;
                return taskGroupSubgraph?.GroupName ?? "Task Group";
            }
        }

        public TaskExecutionMode EffectiveExecutionMode
        {
            get
            {
                if (useOverrideExecutionMode)
                    return overrideExecutionMode;
                return taskGroupSubgraph?.ExecutionMode ?? TaskExecutionMode.Sequential;
            }
        }

        public int EffectiveRequiredCount
        {
            get
            {
                if (overrideRequiredCount >= 0)
                    return overrideRequiredCount;
                return taskGroupSubgraph?.RequiredCount ?? 1;
            }
        }

        public string DisplayName => taskGroupSubgraph != null
            ? $"[TaskGroup] {EffectiveGroupName}"
            : "[TaskGroup] Empty Reference";

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From Stage (or previous TaskGroup in sequence)
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("From Stage")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Output: All tasks in group completed
            context.AddOutputPort<StageFlow>("Complete")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Output: Group failed (optional tasks not met, etc.)
            context.AddOutputPort<StageFlow>("Fail")
                .WithDisplayName("Else")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
```

### Step 3.4: Subgraph Best Practices

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      SUBGRAPH BEST PRACTICES                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. NAMING CONVENTION                                                    │
│     ─────────────────                                                   │
│     [Category]_[Description].[extension]                                │
│                                                                          │
│     Examples:                                                            │
│     • Combat_KillGoblins.taskgroup                                      │
│     • Investigation_TalkToWitnesses.stage                               │
│     • MainStory_TheGoblinThreat.quest                                   │
│     • Companions_Questline.questline                                    │
│                                                                          │
│  2. FOLDER STRUCTURE                                                     │
│     ─────────────────                                                   │
│     Assets/Content/QuestGraphs/                                         │
│     ├── QuestLines/                                                     │
│     │   ├── MainStory.questline                                        │
│     │   └── SideStories.questline                                      │
│     ├── Quests/                                                         │
│     │   ├── MainStory/                                                 │
│     │   │   ├── GoblinsBane.quest                                      │
│     │   │   └── TheBanditEmployer.quest                                │
│     │   └── SideQuests/                                                │
│     │       └── MerchantTroubles.quest                                 │
│     ├── Stages/                                                         │
│     │   ├── Common/                                                    │
│     │   │   └── TalkToQuestGiver.stage                                 │
│     │   └── Combat/                                                    │
│     │       └── DefendVillage.stage                                    │
│     └── TaskGroups/                                                     │
│         ├── Combat/                                                    │
│         │   ├── KillGoblins.taskgroup                                  │
│         │   └── KillBandits.taskgroup                                  │
│         ├── Collect/                                                   │
│         │   └── GatherEvidence.taskgroup                               │
│         └── Interact/                                                  │
│             └── TalkToNPCs.taskgroup                                   │
│                                                                          │
│  3. WHEN TO CREATE A SUBGRAPH                                           │
│     ────────────────────────                                            │
│     ✓ Task group used in 2+ stages                                      │
│     ✓ Stage pattern repeats (e.g., "talk to NPC" stages)               │
│     ✓ Quest template for similar quests                                 │
│     ✓ Complex branching you want to encapsulate                         │
│                                                                          │
│     ✗ One-off task groups (embed inline)                                │
│     ✗ Unique stages with no reuse potential                             │
│     ✗ Simple linear quests                                              │
│                                                                          │
│  4. OVERRIDE vs INHERIT                                                  │
│     ───────────────────                                                 │
│     Subgraph nodes can OVERRIDE subgraph properties:                    │
│                                                                          │
│     TaskGroupSubgraphNode:                                              │
│     ├── subgraph.GroupName = "Kill Enemies"                             │
│     └── overrideGroupName = "Kill Goblins" (context-specific)          │
│                                                                          │
│     Use overrides when:                                                  │
│     • Same structure, different parameters                               │
│     • Localizing a generic subgraph                                     │
│     • Testing variations                                                 │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Phase 4: Port Types & Connections

### Complexity: Low | Time: ~2 hours | Priority: MANDATORY

### Port Connection Rules

| Output Port Type | Can Connect To | Description |
|-----------------|----------------|-------------|
| `QuestFlow` | `QuestFlow` input | Quest sequence in QuestLine |
| `StageFlow` | `StageFlow` input | Stage sequence in Quest |
| `ChoiceFlow` | `ChoiceFlow` input | Player choice to ChoiceNode |
| `TaskFlow` | `TaskFlow` input | Task sequence in TaskGroup |
| `ConditionResult` | Any condition input | Boolean condition evaluation |

### Complete Port Connection Matrix

Each graph type has specific nodes with defined input/output ports. This matrix shows all valid connections.

#### QuestLineGraph Connections

```
QuestLineStartNode
└── FirstQuest (QuestFlow) ──────────────────► QuestRefNode.In

QuestRefNode
├── In (QuestFlow)      ◄── QuestLineStartNode.FirstQuest or QuestRefNode.Out
├── Out (QuestFlow)     ──► Next QuestRefNode.In (linear chain)
├── Then (QuestFlow)    ──► QuestRefNode.In (on quest complete)
└── Else (QuestFlow)    ──► QuestRefNode.In (on quest fail)
```

#### QuestGraph Connections

```
QuestStartNode
└── FirstStage (StageFlow) ──────────────────► StageNode.In

StageNode / StageSubgraphNode
├── In (StageFlow)          ◄── QuestStartNode.FirstStage or StageNode.Then/Else
├── TaskGroups (StageFlow)  ──► TaskGroupNode.In (inline task groups)
├── Then (StageFlow)        ──► Next StageNode.In (on stage complete)
├── Else (StageFlow)        ──► StageNode.In (on stage fail)
└── Choices (ChoiceFlow)    ──► ChoiceNode.In (player branching)

ChoiceNode
├── In (ChoiceFlow)     ◄── StageNode.Choices
└── Target (StageFlow)  ──► StageNode.In (choice destination)

ConditionGateNode (automatic branching based on conditions)
├── In (StageFlow)      ◄── StageNode.Then/Else or EventTriggerNode.Then
├── Then (StageFlow)    ──► StageNode.In (condition evaluates true)
└── Else (StageFlow)    ──► StageNode.In (condition evaluates false)

EventTriggerNode (fire GameEvents in flow)
├── In (StageFlow)      ◄── StageNode.Then/Else or other utility nodes
└── Then (StageFlow)    ──► StageNode.In or other utility nodes

WorldFlagSetNode (set world flags in flow)
├── In (StageFlow)      ◄── StageNode.Then/Else or other utility nodes
└── Then (StageFlow)    ──► StageNode.In or other utility nodes

RewardNode (grant rewards in flow)
├── In (StageFlow)      ◄── StageNode.Then/Else or other utility nodes
└── Then (StageFlow)    ──► StageNode.In or other utility nodes

TaskGroupNode / TaskGroupSubgraphNode (inline in QuestGraph)
├── In (StageFlow)      ◄── StageNode.TaskGroups
├── Tasks (TaskFlow)    ──► TaskNode.In (inline tasks)
├── Then (StageFlow)    ──► TaskGroupNode.In or back to stage flow
└── Else (StageFlow)    ──► Failure handling
```

#### StageGraph Connections

```
StageStartNode
└── FirstTaskGroup (StageFlow) ──────────────► TaskGroupNode.In

TaskGroupNode / TaskGroupSubgraphNode
├── In (StageFlow)      ◄── StageStartNode.FirstTaskGroup or TaskGroupNode.Then
├── Tasks (TaskFlow)    ──► TaskNode.In (inline tasks)
├── Then (StageFlow)    ──► Next TaskGroupNode.In
└── Else (StageFlow)    ──► Failure handling
```

#### TaskGroupGraph Connections

```
TaskGroupStartNode
└── FirstTask (TaskFlow) ────────────────────► TaskNode.In

TaskNode
├── In (TaskFlow)   ◄── TaskGroupStartNode.FirstTask or TaskNode.Then
└── Then (TaskFlow) ──► Next TaskNode.In
```

#### Start Node Summary

| Graph Type | Start Node | Output Port | Connects To |
|------------|------------|-------------|-------------|
| QuestLineGraph | `QuestLineStartNode` | FirstQuest (QuestFlow) | QuestRefNode.In |
| QuestGraph | `QuestStartNode` | FirstStage (StageFlow) | StageNode.In |
| StageGraph | `StageStartNode` | FirstTaskGroup (StageFlow) | TaskGroupNode.In |
| TaskGroupGraph | `TaskGroupStartNode` | FirstTask (TaskFlow) | TaskNode.In |

### Step 4.1: Port Connection Validation

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Validation/PortValidator.cs`

```csharp
using System;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    /// <summary>
    /// Validates port connections in quest graphs.
    /// </summary>
    public static class PortValidator
    {
        /// <summary>
        /// Checks if two ports can be connected based on type compatibility.
        /// </summary>
        public static bool CanConnect(IPort outputPort, IPort inputPort)
        {
            if (outputPort == null || inputPort == null)
                return false;

            // Ports must have opposite directions
            if (outputPort.direction == inputPort.direction)
                return false;

            // Type must match or be derived
            Type outputType = outputPort.dataType;
            Type inputType = inputPort.dataType;

            return inputType.IsAssignableFrom(outputType);
        }

        /// <summary>
        /// Gets a user-friendly message explaining why connection failed.
        /// </summary>
        public static string GetConnectionError(IPort outputPort, IPort inputPort)
        {
            if (outputPort == null)
                return "Output port is null";
            if (inputPort == null)
                return "Input port is null";
            if (outputPort.direction == inputPort.direction)
                return "Cannot connect ports with same direction";
            if (!inputPort.dataType.IsAssignableFrom(outputPort.dataType))
                return $"Type mismatch: {outputPort.dataType.Name} → {inputPort.dataType.Name}";

            return "Unknown error";
        }
    }
}
```

---

## 8. Phase 5: Data Conversion

### Complexity: High | Time: ~6 hours | Priority: MANDATORY

### Conversion Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     DATA CONVERSION FLOW                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  EXPORT: Graph → ScriptableObject                                        │
│  ─────────────────────────────                                          │
│                                                                          │
│  QuestLineGraph ──┬──► GraphToQuestLineConverter ──► QuestLine_SO       │
│                   │                                                      │
│                   └──► (each QuestRefNode) ──► Quest_SO (if needed)     │
│                                                                          │
│  QuestGraph ──────┬──► GraphToQuestConverter ──► Quest_SO               │
│                   │                                                      │
│                   ├──► (each StageNode) ──► QuestStage                  │
│                   └──► (each ChoiceNode) ──► StageTransition            │
│                                                                          │
│  StageGraph ──────┬──► Embedded in Quest_SO.Stages                      │
│                   └──► (each TaskGroupNode) ──► TaskGroup               │
│                                                                          │
│  TaskGroupGraph ──┬──► Embedded in QuestStage.TaskGroups                │
│                   └──► (each TaskNode) ──► Task_SO (created or ref)     │
│                                                                          │
│  IMPORT: ScriptableObject → Graph (Future)                               │
│  ─────────────────────────────────                                      │
│                                                                          │
│  Quest_SO ──► QuestToGraphConverter ──► QuestGraph                      │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Step 5.1: Base Converter Interface

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Converters/IGraphConverter.cs`

```csharp
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Converters
{
    /// <summary>
    /// Interface for converting between graph assets and ScriptableObjects.
    /// </summary>
    /// <typeparam name="TGraph">The graph type.</typeparam>
    /// <typeparam name="TAsset">The ScriptableObject type.</typeparam>
    public interface IGraphConverter<TGraph, TAsset>
        where TGraph : Unity.GraphToolkit.Editor.Graph
        where TAsset : ScriptableObject
    {
        /// <summary>
        /// Exports a graph to a ScriptableObject.
        /// Creates a new asset if targetAsset is null.
        /// </summary>
        TAsset Export(TGraph graph, TAsset targetAsset = null);

        /// <summary>
        /// Imports a ScriptableObject into a graph.
        /// Creates a new graph if targetGraph is null.
        /// </summary>
        TGraph Import(TAsset asset, TGraph targetGraph = null);

        /// <summary>
        /// Validates the graph before export.
        /// Returns true if valid, false with error messages if not.
        /// </summary>
        bool ValidateForExport(TGraph graph, out string[] errors);
    }
}
```

### Step 5.2: Quest Graph Converter

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Converters/GraphToQuestConverter.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Converters
{
    /// <summary>
    /// Converts QuestGraph assets to Quest_SO ScriptableObjects.
    /// </summary>
    public class GraphToQuestConverter : IGraphConverter<QuestGraph, Quest_SO>
    {
        #region Export

        public Quest_SO Export(QuestGraph graph, Quest_SO targetAsset = null)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            // Validate first
            if (!ValidateForExport(graph, out string[] errors))
            {
                string errorMsg = string.Join("\n", errors);
                throw new InvalidOperationException($"Graph validation failed:\n{errorMsg}");
            }

            // Create or update asset
            Quest_SO quest = targetAsset;
            bool creatingNew = quest == null;

            if (creatingNew)
            {
                quest = ScriptableObject.CreateInstance<Quest_SO>();
            }

            // Convert nodes to quest data
            ConvertGraphToQuest(graph, quest);

            // Save asset
            if (creatingNew)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Quest Asset",
                    graph.DevName,
                    "asset",
                    "Choose location for the Quest asset"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(quest, path);
                }
            }

            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();

            return quest;
        }

        private void ConvertGraphToQuest(QuestGraph graph, Quest_SO quest)
        {
            // Use reflection or serialized property to set private fields
            var so = new SerializedObject(quest);

            // Set identity
            so.FindProperty("devName").stringValue = graph.DevName;
            so.FindProperty("questType").objectReferenceValue = graph.QuestType;
            so.FindProperty("recommendedLevel").intValue = graph.RecommendedLevel;

            // Get all nodes
            var nodes = graph.GetNodes().ToList();

            // Find start node
            var startNode = nodes.OfType<QuestStartNode>().FirstOrDefault();
            if (startNode != null)
            {
                SetConditionList(so, "startConditions", startNode.StartConditions);
            }

            // Convert stage nodes to QuestStage list
            var stageNodes = nodes.OfType<StageNode>().OrderBy(s => s.StageIndex).ToList();
            var stages = new List<QuestStage>();

            foreach (var stageNode in stageNodes)
            {
                var stage = ConvertStageNodeToStage(stageNode, graph);
                stages.Add(stage);
            }

            // Set stages (requires custom serialization handling)
            SetStageList(so, "stages", stages);

            so.ApplyModifiedProperties();
        }

        private QuestStage ConvertStageNodeToStage(StageNode node, QuestGraph graph)
        {
            var stage = new QuestStage();

            // Use reflection to set private fields
            SetField(stage, "stageIndex", node.StageIndex);
            SetField(stage, "stageName", node.StageName);
            SetField(stage, "isTerminal", node.IsTerminal);
            SetField(stage, "isOptional", node.IsOptional);
            SetField(stage, "isHidden", node.IsHidden);

            // Find connected TaskGroupNodes
            var taskGroups = new List<TaskGroup>();
            // TODO: Traverse connections to find TaskGroupNodes
            SetField(stage, "taskGroups", taskGroups);

            // Find connected transitions (via ChoiceNodes)
            var transitions = new List<StageTransition>();
            // TODO: Convert ChoiceNodes to StageTransitions
            SetField(stage, "transitions", transitions);

            return stage;
        }

        private void SetConditionList(SerializedObject so, string propertyName,
            List<HelloDev.Conditions.Condition_SO> conditions)
        {
            var prop = so.FindProperty(propertyName);
            prop.ClearArray();

            if (conditions == null) return;

            for (int i = 0; i < conditions.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = conditions[i];
            }
        }

        private void SetStageList(SerializedObject so, string propertyName,
            List<QuestStage> stages)
        {
            // This requires custom serialization since QuestStage is a nested class
            // Implementation depends on your serialization approach
        }

        private void SetField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        #endregion

        #region Import

        public QuestGraph Import(Quest_SO asset, QuestGraph targetGraph = null)
        {
            // Future implementation
            throw new NotImplementedException("Import not yet implemented");
        }

        #endregion

        #region Validation

        public bool ValidateForExport(QuestGraph graph, out string[] errors)
        {
            var errorList = new List<string>();
            var nodes = graph.GetNodes().ToList();

            // Check for exactly one start node
            var startNodes = nodes.OfType<QuestStartNode>().ToList();
            if (startNodes.Count == 0)
            {
                errorList.Add("Graph must have a QuestStartNode");
            }
            else if (startNodes.Count > 1)
            {
                errorList.Add("Graph must have exactly one QuestStartNode");
            }

            // Check for at least one stage
            var stageNodes = nodes.OfType<StageNode>().ToList();
            if (stageNodes.Count == 0)
            {
                errorList.Add("Graph must have at least one StageNode");
            }

            // Check for at least one terminal stage
            var terminalStages = stageNodes.Where(s => s.IsTerminal).ToList();
            if (terminalStages.Count == 0)
            {
                errorList.Add("Graph must have at least one terminal stage");
            }

            // Check for duplicate stage indices
            var duplicateIndices = stageNodes
                .GroupBy(s => s.StageIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var index in duplicateIndices)
            {
                errorList.Add($"Duplicate stage index: {index}");
            }

            // Check all stages are reachable from start
            // (Graph traversal - implement if needed)

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        #endregion
    }
}
```

### Step 5.3: Export Menu Command

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Commands/ExportGraphCommand.cs`

```csharp
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Commands
{
    /// <summary>
    /// Editor menu commands for exporting graphs.
    /// </summary>
    public static class ExportGraphCommand
    {
        [MenuItem("Assets/HelloDev/Quest System/Export Quest Graph", false, 200)]
        private static void ExportSelectedQuestGraph()
        {
            var graph = Selection.activeObject as QuestGraph;
            if (graph == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Please select a Quest Graph asset.", "OK");
                return;
            }

            var converter = new Converters.GraphToQuestConverter();

            if (!converter.ValidateForExport(graph, out string[] errors))
            {
                string errorMsg = string.Join("\n", errors);
                EditorUtility.DisplayDialog("Validation Failed",
                    $"Cannot export graph:\n\n{errorMsg}", "OK");
                return;
            }

            try
            {
                var quest = converter.Export(graph, graph.TargetAsset);
                graph.TargetAsset = quest;
                EditorUtility.SetDirty(graph);

                EditorUtility.DisplayDialog("Success",
                    $"Exported to: {AssetDatabase.GetAssetPath(quest)}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed",
                    ex.Message, "OK");
            }
        }

        [MenuItem("Assets/HelloDev/Quest System/Export Quest Graph", true)]
        private static bool ExportSelectedQuestGraphValidation()
        {
            return Selection.activeObject is QuestGraph;
        }
    }
}
```

---

## 9. Phase 6: Validation System

### Complexity: Medium | Time: ~3 hours | Priority: NICE TO HAVE

### Validation Rules

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       VALIDATION RULES                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  QUEST GRAPH VALIDATION                                                  │
│  ─────────────────────                                                  │
│  [ERROR] Must have exactly 1 QuestStartNode                             │
│  [ERROR] Must have at least 1 StageNode                                  │
│  [ERROR] Must have at least 1 terminal stage                             │
│  [ERROR] No duplicate stage indices                                      │
│  [ERROR] All stages must be reachable from start                         │
│  [ERROR] No orphan nodes (disconnected)                                  │
│  [WARN]  Stage indices should use gaps (0, 10, 20...)                   │
│  [WARN]  Non-terminal stages should have transitions                     │
│                                                                          │
│  STAGE VALIDATION                                                        │
│  ───────────────                                                        │
│  [ERROR] Non-terminal stage must have at least 1 output connection      │
│  [ERROR] Terminal stage must not have output connections                 │
│  [WARN]  Empty stage (no task groups)                                   │
│  [WARN]  PlayerChoice stages should have 2+ choices                     │
│                                                                          │
│  TASK GROUP VALIDATION                                                   │
│  ────────────────────                                                   │
│  [ERROR] Must have at least 1 task                                      │
│  [ERROR] OptionalXofY: requiredCount must be <= task count              │
│  [WARN]  Empty task references                                          │
│                                                                          │
│  CHOICE VALIDATION                                                       │
│  ────────────────                                                       │
│  [ERROR] Choice must have target stage connection                        │
│  [WARN]  Empty choice text                                              │
│  [WARN]  Duplicate choice IDs                                           │
│                                                                          │
│  QUESTLINE VALIDATION                                                    │
│  ───────────────────                                                    │
│  [ERROR] Must have at least 1 quest reference                           │
│  [ERROR] No circular quest dependencies                                  │
│  [WARN]  Empty quest references                                         │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Step 6.1: Validation Service

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Validation/GraphValidationService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Validation
{
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Info
    }

    public class ValidationResult
    {
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
        public INode RelatedNode { get; set; }

        public ValidationResult(ValidationSeverity severity, string message, INode node = null)
        {
            Severity = severity;
            Message = message;
            RelatedNode = node;
        }
    }

    /// <summary>
    /// Validates quest graphs and reports issues.
    /// </summary>
    public class GraphValidationService
    {
        public List<ValidationResult> ValidateQuestGraph(QuestGraph graph)
        {
            var results = new List<ValidationResult>();
            var nodes = graph.GetNodes().ToList();

            // Rule: Exactly one start node
            var startNodes = nodes.OfType<QuestStartNode>().ToList();
            if (startNodes.Count == 0)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    "Quest must have a QuestStartNode"
                ));
            }
            else if (startNodes.Count > 1)
            {
                foreach (var node in startNodes.Skip(1))
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Error,
                        "Only one QuestStartNode allowed",
                        node
                    ));
                }
            }

            // Rule: At least one stage
            var stageNodes = nodes.OfType<StageNode>().ToList();
            if (stageNodes.Count == 0)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    "Quest must have at least one stage"
                ));
            }

            // Rule: At least one terminal stage
            var terminalStages = stageNodes.Where(s => s.IsTerminal).ToList();
            if (terminalStages.Count == 0 && stageNodes.Count > 0)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    "Quest must have at least one terminal stage"
                ));
            }

            // Rule: No duplicate stage indices
            var duplicates = stageNodes
                .GroupBy(s => s.StageIndex)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicates)
            {
                foreach (var node in group)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Error,
                        $"Duplicate stage index: {group.Key}",
                        node
                    ));
                }
            }

            // Rule: Stage indices should have gaps
            var sortedIndices = stageNodes
                .Select(s => s.StageIndex)
                .OrderBy(i => i)
                .ToList();

            for (int i = 1; i < sortedIndices.Count; i++)
            {
                if (sortedIndices[i] - sortedIndices[i - 1] == 1)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        $"Consider using gaps between stage indices (e.g., 0, 10, 20) for easier insertion. " +
                        $"Found consecutive indices: {sortedIndices[i - 1]}, {sortedIndices[i]}"
                    ));
                    break; // Only warn once
                }
            }

            // Validate each stage
            foreach (var stageNode in stageNodes)
            {
                ValidateStageNode(stageNode, results);
            }

            return results;
        }

        private void ValidateStageNode(StageNode node, List<ValidationResult> results)
        {
            // Terminal stages should not have connections
            if (node.IsTerminal && node.outputPortCount > 0)
            {
                // Check if any output ports are connected
                var outputPorts = node.GetOutputPorts();
                bool hasConnections = outputPorts.Any(p => p.isConnected);

                if (hasConnections)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        "Terminal stage has output connections that will be ignored",
                        node
                    ));
                }
            }

            // Non-terminal stages should have at least one transition
            if (!node.IsTerminal)
            {
                var outputPorts = node.GetOutputPorts();
                bool hasConnections = outputPorts.Any(p => p.isConnected);

                if (!hasConnections)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Error,
                        "Non-terminal stage must have at least one transition",
                        node
                    ));
                }
            }
        }

        public List<ValidationResult> ValidateQuestLineGraph(QuestLineGraph graph)
        {
            var results = new List<ValidationResult>();
            var nodes = graph.GetNodes().ToList();

            // Rule: At least one quest reference
            var questRefs = nodes.OfType<QuestRefNode>().ToList();
            if (questRefs.Count == 0)
            {
                results.Add(new ValidationResult(
                    ValidationSeverity.Error,
                    "QuestLine must have at least one quest"
                ));
            }

            // Rule: No empty references
            foreach (var questRef in questRefs)
            {
                if (questRef.QuestAsset == null && questRef.QuestGraphAsset == null)
                {
                    results.Add(new ValidationResult(
                        ValidationSeverity.Warning,
                        "Quest reference is empty",
                        questRef
                    ));
                }
            }

            return results;
        }
    }
}
```

---

## 10. Phase 7: Polish & UX

### Complexity: Low-Medium | Time: ~4 hours | Priority: NICE TO HAVE

### Step 7.1: Custom Node Colors

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/Styling/QuestGraphStyles.uss`

```css
/* Stage Node Styles */
.stage-node {
    background-color: #2d5a27;
}

.stage-node.terminal {
    background-color: #5a2727;
    border-color: #ff6b6b;
    border-width: 2px;
}

.stage-node.optional {
    background-color: #4a4a27;
}

/* Choice Node Styles */
.choice-node {
    background-color: #27455a;
    border-color: #6bb3ff;
    border-width: 1px;
}

/* Task Node Styles */
.task-node {
    background-color: #3d3d3d;
}

.task-node.int-task {
    border-left: 3px solid #4CAF50;
}

.task-node.bool-task {
    border-left: 3px solid #2196F3;
}

.task-node.location-task {
    border-left: 3px solid #FF9800;
}

.task-node.discovery-task {
    border-left: 3px solid #9C27B0;
}

.task-node.timed-task {
    border-left: 3px solid #F44336;
}

/* Start Node */
.quest-start-node {
    background-color: #1a472a;
    border-color: #4CAF50;
    border-width: 2px;
}

/* Subgraph Reference Nodes */
.subgraph-node {
    background-color: #3d3d4d;
    border-style: dashed;
    border-width: 2px;
}
```

### Step 7.2: Context Menu Actions

**File**: `Assets/com.hellodev.questsystem/Editor/QuestGraph/UI/QuestGraphContextMenu.cs`

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.UI
{
    /// <summary>
    /// Custom context menu items for quest graphs.
    /// </summary>
    public static class QuestGraphContextMenu
    {
        /// <summary>
        /// Adds quick-create options to the graph context menu.
        /// </summary>
        public static void PopulateContextMenu(DropdownMenu menu, QuestGraph graph)
        {
            menu.AppendAction("Add Stage (Index 0)",
                action => CreateStageNode(graph, 0));

            menu.AppendAction("Add Stage (Auto Index)",
                action => CreateStageNode(graph, GetNextStageIndex(graph)));

            menu.AppendSeparator();

            menu.AppendAction("Add Terminal Stage",
                action => CreateTerminalStage(graph));

            menu.AppendSeparator();

            menu.AppendAction("Validate Graph",
                action => ValidateGraph(graph));

            menu.AppendAction("Export to Quest_SO",
                action => ExportGraph(graph));
        }

        private static void CreateStageNode(QuestGraph graph, int stageIndex)
        {
            // Implementation: Create node via Graph Toolkit API
        }

        private static void CreateTerminalStage(QuestGraph graph)
        {
            // Implementation: Create terminal stage node
        }

        private static int GetNextStageIndex(QuestGraph graph)
        {
            // Find highest stage index and add 10
            int maxIndex = 0;
            foreach (var node in graph.GetNodes())
            {
                if (node is Nodes.StageNode stageNode)
                {
                    if (stageNode.StageIndex > maxIndex)
                        maxIndex = stageNode.StageIndex;
                }
            }
            return maxIndex + 10;
        }

        private static void ValidateGraph(QuestGraph graph)
        {
            var validator = new Validation.GraphValidationService();
            var results = validator.ValidateQuestGraph(graph);

            if (results.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation",
                    "Graph is valid!", "OK");
            }
            else
            {
                var errors = results.Count(r => r.Severity == Validation.ValidationSeverity.Error);
                var warnings = results.Count(r => r.Severity == Validation.ValidationSeverity.Warning);

                string message = $"Found {errors} error(s) and {warnings} warning(s):\n\n";
                foreach (var result in results.Take(10))
                {
                    message += $"[{result.Severity}] {result.Message}\n";
                }

                if (results.Count > 10)
                {
                    message += $"\n... and {results.Count - 10} more issues.";
                }

                EditorUtility.DisplayDialog("Validation Results", message, "OK");
            }
        }

        private static void ExportGraph(QuestGraph graph)
        {
            Commands.ExportGraphCommand.ExportQuestGraph(graph);
        }
    }
}
```

---

## 11. Feature Matrix

### Implementation Priority

| Feature | Priority | Complexity | Phase | Status |
|---------|----------|------------|-------|--------|
| Assembly Definition | MANDATORY | Low | 1 | ✅ |
| QuestLineGraph | MANDATORY | Low | 1 | ✅ |
| QuestGraph | MANDATORY | Low | 1 | ✅ |
| StageGraph | MANDATORY | Low | 1 | ✅ |
| TaskGroupGraph | MANDATORY | Low | 1 | ✅ |
| QuestStartNode | MANDATORY | Low | 2 | ✅ |
| StageNode | MANDATORY | Medium | 2 | ✅ |
| TaskNode | MANDATORY | Low | 2 | ✅ |
| TaskGroupNode | MANDATORY | Low | 2 | ✅ |
| ChoiceNode | MANDATORY | Medium | 2 | ✅ |
| QuestRefNode | MANDATORY | Low | 2 | ✅ |
| **Subgraph Support** | **SUPER IMPORTANT** | Medium | 3 | ✅ |
| StageSubgraphNode | **SUPER IMPORTANT** | Medium | 3 | ✅ |
| TaskGroupSubgraphNode | **SUPER IMPORTANT** | Medium | 3 | ✅ |
| Port Types | MANDATORY | Low | 4 | ✅ |
| Port Validation | MANDATORY | Low | 4 | ✅ |
| GraphToQuestConverter | MANDATORY | High | 5 | ✅ |
| GraphToQuestLineConverter | MANDATORY | High | 5 | ✅ |
| ScriptedImporters | MANDATORY | Medium | 5 | ✅ |
| Export Menu Command | MANDATORY | Low | 5 | ✅ |
| Validation Service | NICE TO HAVE | Medium | 6 | ✅ |
| Validation Rules | NICE TO HAVE | Medium | 6 | ✅ |
| Reachability Analyzer | NICE TO HAVE | Medium | 6 | ✅ |
| Custom Node Styles (USS) | NICE TO HAVE | Low | 7 | ✅ |
| Context Menu Actions | NICE TO HAVE | Low | 7 | ✅ |
| QuestToGraphConverter (Import) | FUTURE | High | - | ⬜ |
| Live Preview | FUTURE | High | - | ⬜ |
| Undo/Redo Enhancements | FUTURE | Medium | - | ⬜ |

### Legend
- ⬜ Not Started
- 🔄 In Progress
- ✅ Complete

---

## 12. File Structure

```
Assets/com.hellodev.questsystem/
├── Editor/
│   └── Graphs/
│       └── Scripts/
│           ├── HelloDev.QuestSystem.QuestGraph.Editor.asmdef
│           │
│           ├── Graphs/
│           │   ├── QuestLineGraph.cs      # Top-level graph
│           │   ├── QuestGraph.cs          # [Subgraph(typeof(QuestLineGraph))]
│           │   ├── StageGraph.cs          # [Subgraph(typeof(QuestGraph))]
│           │   └── TaskGroupGraph.cs      # [Subgraph(typeof(StageGraph))]
│           │
│           ├── Nodes/
│           │   ├── QuestBaseNode.cs       # Base class for all nodes
│           │   ├── QuestLineStartNode.cs  # Entry point for QuestLine
│           │   ├── QuestStartNode.cs      # Entry point for Quest
│           │   ├── StageStartNode.cs      # Entry point for StageGraph
│           │   ├── TaskGroupStartNode.cs  # Entry point for TaskGroupGraph
│           │   ├── QuestRefNode.cs        # Reference quest in QuestLine (quest subgraph)
│           │   ├── StageNode.cs           # Quest stage (inline)
│           │   ├── ChoiceNode.cs          # Player choice branch
│           │   ├── ConditionGateNode.cs   # Automatic condition branching
│           │   ├── EventTriggerNode.cs    # Fire GameEvents in flow
│           │   ├── WorldFlagSetNode.cs    # Set world flags in flow
│           │   ├── RewardNode.cs          # Grant rewards in flow
│           │   ├── TaskGroupNode.cs       # Group of tasks (inline)
│           │   └── TaskNode.cs            # Individual task reference
│           │   │
│           │   └── Subgraphs/             # Subgraph reference nodes
│           │       ├── StageSubgraphNode.cs      # Reference StageGraph
│           │       └── TaskGroupSubgraphNode.cs  # Reference TaskGroupGraph
│           │
│           ├── Ports/
│           │   └── QuestPorts.cs          # QuestFlow, StageFlow, TaskFlow, ChoiceFlow
│           │
│           ├── Converters/                # Phase 5 - Data Conversion
│           │   ├── IGraphConverter.cs           # Interface for graph → SO conversion
│           │   ├── ConversionContext.cs         # Shared context (caching, errors)
│           │   ├── GraphTraversalUtility.cs     # Port connection traversal
│           │   ├── GraphToQuestConverter.cs     # QuestGraph → Quest_SO
│           │   └── GraphToQuestLineConverter.cs # QuestLineGraph → QuestLine_SO
│           │
│           ├── Importers/                 # Phase 5 - ScriptedImporters
│           │   ├── QuestGraphImporter.cs        # .quest → Quest_SO
│           │   └── QuestLineGraphImporter.cs    # .questline → QuestLine_SO
│           │
│           ├── Commands/                  # Phase 5 - Menu Commands
│           │   └── ExportCommands.cs            # Validation & reimport menus
│           │
│           ├── Validation/                # Phase 6 - Validation System
│           │   ├── ValidationResult.cs          # Error/Warning/Info severity
│           │   ├── GraphValidationService.cs    # Validates all graph types
│           │   ├── GraphReachabilityAnalyzer.cs # BFS unreachable node detection
│           │   └── PortConnectionValidator.cs   # Port compatibility checks
│           │
│           ├── Styling/                   # Phase 7 - Visual Polish
│           │   └── QuestGraphStyles.uss         # USS styles for node colors
│           │
│           └── UI/                        # Phase 7 - Context Menus
│               └── QuestGraphContextMenu.cs     # Right-click actions
│
│       └── QuestCreationWizard.cs (existing)
│
└── Runtime/
    └── Scripts/
        └── Core/
            └── (existing quest system code)
```

---

## 13. Code Examples

### Example 1: Creating a Simple Quest in the Graph Editor

```
STEP 1: Create Quest Graph
──────────────────────────
Assets > Create > HelloDev > Quest System > Graphs > Quest Graph
Name: "GoblinHunt.quest"

STEP 2: Add Start Node
──────────────────────
Right-click canvas > Create Node > QuestStartNode

STEP 3: Add Stages
─────────────────
Right-click canvas > Create Node > StageNode
Configure: StageIndex=0, StageName="Talk to Guard", IsTerminal=false

Right-click canvas > Create Node > StageNode
Configure: StageIndex=10, StageName="Kill Goblins", IsTerminal=false

Right-click canvas > Create Node > StageNode
Configure: StageIndex=100, StageName="Return to Guard", IsTerminal=true

STEP 4: Connect Nodes
────────────────────
Drag from QuestStartNode.FirstStage → Stage0.In
Drag from Stage0.Then → Stage10.In
Drag from Stage10.Then → Stage100.In

STEP 5: Add Task Groups (as subgraphs)
─────────────────────────────────────
Create TaskGroup subgraph: "Combat_KillGoblins.taskgroup"
Add TaskGroupSubgraphNode to Stage10
Reference the subgraph

STEP 6: Export
─────────────
Assets > HelloDev > Quest System > Export Quest Graph
Choose save location → Quest_SO created!
```

### Example 2: Reusing a TaskGroup Subgraph

```csharp
// Scenario: "Kill 10 Goblins" task group used in multiple quests

// 1. Create TaskGroupGraph asset: "Combat_KillGoblins.taskgroup"
//    Contains: TaskNode (IntTask, requiredCount=10)

// 2. In Quest A - Stage 2:
//    Add TaskGroupSubgraphNode → reference "Combat_KillGoblins.taskgroup"

// 3. In Quest B - Stage 1:
//    Add TaskGroupSubgraphNode → reference "Combat_KillGoblins.taskgroup"
//    Set overrideGroupName = "Goblin Patrol" (optional customization)

// 4. In Quest C - Stage 4:
//    Add TaskGroupSubgraphNode → reference "Combat_KillGoblins.taskgroup"

// Now if you update the task group (e.g., change to 15 goblins),
// all three quests automatically update!
```

### Example 3: Branching Quest with Player Choices

```
QUEST: "The Merchant's Dilemma"

      ┌──────────────┐
      │ QuestStart   │
      └──────┬───────┘
             │
             ▼
      ┌──────────────┐
      │  Stage 0     │
      │ "Meet the    │
      │  Merchant"   │
      └──────┬───────┘
             │ Then
             ▼
      ┌──────────────┐
      │  Stage 10    │
      │ "The Choice" │
      │ ChoiceCount=3│
      └──┬───┬───┬───┘
         │   │   │
    ┌────┘   │   └────┐
    │        │        │
    ▼        ▼        ▼
┌────────┐┌────────┐┌────────┐
│ Choice ││ Choice ││ Choice │
│"Combat"││"Diplo" ││"Lawful"│
│        ││        ││ (cond) │
└───┬────┘└───┬────┘└───┬────┘
    │         │         │
    ▼         ▼         ▼
┌────────┐┌────────┐┌────────┐
│Stage 20││Stage 30││Stage 40│
│"Fight" ││"Negoti"││"Report"│
│Terminal││Terminal││Terminal│
└────────┘└────────┘└────────┘
```

---

## 14. Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Graph window doesn't open | Missing Graph Toolkit package | Install `com.unity.graphtoolkit@0.4.0-exp.2` |
| Nodes don't appear in search | Wrong assembly reference | Check asmdef includes Graph Toolkit refs |
| Ports won't connect | Type mismatch | Verify port types match (StageFlow → StageFlow) |
| Export fails | Validation errors | Run validation first, fix all errors |
| Subgraph not found | Wrong file extension | Ensure subgraph uses correct extension |
| Changes not saved | Missing `[Serializable]` | Add attribute to all node classes |

### Debugging Tips

```csharp
// Add to graph class for initialization debugging
public override void OnEnable()
{
    base.OnEnable();
    Debug.Log($"[{GetType().Name}] OnEnable - Name: {name}");
}

// Add to graph class for modification tracking
public override void OnGraphChanged(GraphLogger logger)
{
    base.OnGraphChanged(logger);
    Debug.Log($"[{GetType().Name}] Graph changed - Node count: {nodeCount}");
}

// Add to node class for port debugging (note: protected for nodes)
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    Debug.Log($"[{GetType().Name}] Defining ports");
    // Port definitions...
}
```

---

## 15. Future Improvements

### Planned Features

| Feature | Description | Benefit |
|---------|-------------|---------|
| **Import Quest_SO** | Convert existing Quest_SO to graph | Edit legacy quests visually |
| **Live Preview** | See quest flow in real-time | Better testing experience |
| **Minimap Labels** | Show stage names in minimap | Easier navigation |
| **Template Library** | Pre-built quest templates | Faster quest creation |
| **Auto-Layout** | Automatic node arrangement | Cleaner graphs |
| **Diff Tool** | Compare graph versions | Version control friendly |
| **Localization Panel** | Edit all text in one place | Faster localization |
| **Runtime Debugging** | Highlight active stage during play | Debug quest flow |

### Extensibility Points

```csharp
// Custom Task Node Types
public class CustomTaskNode : TaskNode
{
    // Add game-specific task properties
}

// Custom Validation Rules
public class ProjectSpecificValidator : GraphValidationService
{
    protected override void ValidateProjectRules(Graph graph, List<ValidationResult> results)
    {
        // Add project-specific validation
    }
}

// Custom Export Formats
public class GraphToJsonExporter : IGraphConverter<QuestGraph, TextAsset>
{
    // Export to JSON for web integration
}
```

---

## Summary

This guide provides a complete roadmap for implementing a modular Quest Graph Editor using Unity's Graph Toolkit. The key architectural decision is the **subgraph hierarchy** that enables:

1. **Clean main graphs** - High-level overview without clutter
2. **Maximum reusability** - Create once, use everywhere
3. **Team collaboration** - Designers work on independent pieces
4. **Easy maintenance** - Update a subgraph, all references update

Follow the phases in order, validate at each step, and refer to the code examples when implementing. The feature matrix helps prioritize work, and the troubleshooting section addresses common issues.

**Start with Phase 1 and 2 to get a working prototype, then add Phase 3 (Subgraphs) for the modularity benefits, and finally add polish in later phases.**

---

*Document Version: 1.0*
*Compatible with: Unity 6.2+, Graph Toolkit 0.4.0-exp.2*
*Quest System Version: 3.5.1+*
