# Quest Graph Creation Reference

This document captures the complete process for programmatically creating quest graphs using the Unity Graph Toolkit. It serves as a reference for AI assistants and developers to create quest graphs without manual editor interaction.

## Table of Contents

1. [File Locations](#file-locations)
2. [YAML Structure Overview](#yaml-structure-overview)
3. [Node Types](#node-types)
4. [Asset References (GUIDs)](#asset-references-guids)
5. [LocalizedString Format](#localizedstring-format)
6. [Wire Connections](#wire-connections)
7. [Step-by-Step Creation Process](#step-by-step-creation-process)
8. [Common Pitfalls](#common-pitfalls)
9. [Validation Checklist](#validation-checklist)

---

## File Locations

### Quest Graphs
```
Assets/com.hellodev.questsystem/BasicQuestExample/Graphs/Quests/Graph_Quest_<QuestName>.quest
```

### Questline Graphs
```
Assets/com.hellodev.questsystem/BasicQuestExample/Graphs/Questlines/Graph_Questline_<Name>.questline
```

### Reference Examples
- **Simple Quest**: `Graph_Quest_MyFirstQuest.quest` - Basic linear quest
- **Branching Quest**: `Graph_Quest_TheMerchantsDilemma.quest` - Quest with player choices
- **Questline with Choices**: `Graph_Questline_Test.questline` - Contains ChoiceNode examples

### Asset Locations
```
Assets/com.hellodev.questsystem/BasicQuestExample/
├── ScriptableObjects/
│   ├── Quests/<QuestName>/Tasks/          # Task_SO assets
│   ├── IDs/                                # ID assets (NPCs, Enemies, etc.)
│   ├── Conditions/                         # Condition_SO assets
│   └── Events/                             # GameEvent assets
├── Settings/Localization/
│   ├── Tables/                             # Localization tables (.asset)
│   └── CSV/                                # Localization source files
└── Graphs/                                 # Quest/Questline graphs
```

---

## YAML Structure Overview

Quest graphs use Unity's YAML serialization format. The basic structure is:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &1
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 790b4d75d92f4b0984310a268dbd952f, type: 3}
  m_Name: Graph_Quest_<Name>
  m_GraphModel:
    rid: <root_rid>
  references:
    version: 2
    RefIds:
    - rid: -2
      type: {class: , ns: , asm: }
    - rid: <root_rid>
      type: {class: GraphModelImp, ...}
      data:
        m_GraphNodeModels: [list of node RIDs]
        m_GraphWireModels: [list of wire RIDs]
        m_EntryPoint: rid: <QuestStartNode_rid>
        m_Graph: rid: <QuestGraphDefinition_rid>
    # ... node and wire definitions
```

### RID (Reference ID) System

Every element has a unique RID. Use a consistent numbering scheme:

```
Base RID: 2249559135886770000 (example)

Increment by type:
- GraphModel: +268
- SectionModel: +269
- QuestGraphDefinition: +270
- QuestStartNode: +300
- Stages: +310, +350, +460, +560, +660, +760 (with gaps for children)
- TaskGroups: +330, +370, +480, +580, +680, +780
- Tasks: +340, +380, +490, +590, +690, +790
- ChoiceNodes: +400, +420, +440
- Wires: +700, +701, +702...
```

---

## Node Types

### 1. QuestStartNode (Entry Point)

Every quest graph must have exactly one QuestStartNode as the entry point.

```yaml
# UserNodeModelImp wrapper
- rid: 2249559135886770300
  type: {class: UserNodeModelImp, ns: Unity.GraphToolkit.Editor.Implementation, asm: Unity.GraphToolkit.Editor}
  data:
    m_Guid:
      m_Value0: 1001000000000000001
      m_Value1: 2001000000000000001
    m_HashGuid:
      serializedVersion: 2
      Hash: a1000000000000000000000000000001
    m_Version: 2
    m_Position: {x: -200, y: 0}
    m_InputConstantsById:
      m_KeyList: []
      m_ValueList: []
    m_Node:
      rid: 2249559135886770301

# QuestStartNode definition
- rid: 2249559135886770301
  type: {class: QuestStartNode, ns: HelloDev.QuestSystem.QuestGraph.Editor.Nodes, asm: HelloDev.QuestSystem.QuestGraph.Editor}
  data:
```

**Ports:**
- Output: `FirstStage` (StageFlow) - connects to first stage

### 2. StageNode

Represents a quest stage with configuration options.

```yaml
# UserNodeModelImp wrapper
- rid: 2249559135886770310
  type: {class: UserNodeModelImp, ...}
  data:
    m_Position: {x: 0, y: 0}
    m_InputConstantsById:
      m_KeyList:
      - In
      - __option_StageIndex
      - __option_StageName
      - __option_JournalEntry
      - __option_StageIcon
      - __option_IsTerminal
      - __option_IsOptional
      - __option_IsHidden
      - __option_HasPlayerChoices
      m_ValueList:
      - rid: <StageFlow_constant>
      - rid: <int_constant>           # StageIndex (0, 10, 20, etc.)
      - rid: <string_constant>        # StageName (e.g., "Introduction")
      - rid: <LocalizedString_constant>
      - rid: <Sprite_constant>
      - rid: <bool_constant>          # IsTerminal
      - rid: <bool_constant>          # IsOptional
      - rid: <bool_constant>          # IsHidden
      - rid: <bool_constant>          # HasPlayerChoices
    m_Node:
      rid: <StageNode_rid>

# Constants for StageNode
- rid: <StageFlow_constant>
  type: {class: 'Constant`1[[HelloDev.QuestSystem.QuestGraph.Editor.Ports.StageFlow, ...]]', ...}
  data: {}

- rid: <int_constant>
  type: {class: 'Constant`1[[System.Int32, mscorlib]]', ...}
  data:
    m_Value: 0

- rid: <string_constant>
  type: {class: 'Constant`1[[System.String, mscorlib]]', ...}
  data:
    m_Value: Introduction
```

**Ports:**
- Input: `In` (StageFlow)
- Output: `TaskGroups` (StageFlow) - connect to TaskGroupNodes
- Output: `Then` (StageFlow) - success flow for linear progression
- Output: `Else` (StageFlow) - failure flow
- Output: `Choices` (ChoiceFlow) - only if HasPlayerChoices=true

**Key Options:**
- `StageIndex`: Integer index (use gaps: 0, 10, 20, 30... for future insertion)
- `StageName`: String identifier (e.g., "Introduction", "TheChoice")
- `JournalEntry`: LocalizedString for quest journal display
- `IsTerminal`: true for final stages (removes Then/Else/Choices ports)
- `HasPlayerChoices`: true if stage presents player choices (adds Choices port)

### 3. TaskGroupNode

Groups related tasks within a stage.

```yaml
- rid: <TaskGroupNode_wrapper>
  type: {class: UserNodeModelImp, ...}
  data:
    m_InputConstantsById:
      m_KeyList:
      - In
      - __option_GroupName
      - __option_ExecutionMode
      - __option_RequiredCount
      m_ValueList:
      - rid: <StageFlow_constant>
      - rid: <string_constant>        # GroupName (display)
      - rid: <int_constant>           # ExecutionMode (0=Sequential, 1=Parallel, 2=AnyOrder, 3=OptionalXofY)
      - rid: <int_constant>           # RequiredCount (for OptionalXofY mode)
```

**Ports:**
- Input: `In` (StageFlow) - from Stage's TaskGroups port
- Output: `Tasks` (TaskFlow) - connect to TaskNodes
- Output: `Then` (StageFlow) - success flow after all tasks complete
- Output: `Else` (StageFlow) - failure flow if group fails

### 4. TaskNode

References an existing Task_SO asset.

```yaml
- rid: <TaskNode_wrapper>
  type: {class: UserNodeModelImp, ...}
  data:
    m_InputConstantsById:
      m_KeyList:
      - In
      - __option_TaskAsset
      m_ValueList:
      - rid: <TaskFlow_constant>
      - rid: <Task_SO_constant>

# Task_SO reference constant
- rid: <Task_SO_constant>
  type: {class: 'Constant`1[[HelloDev.QuestSystem.ScriptableObjects.Task_SO, HelloDev.QuestSystem]]', ...}
  data:
    m_Value: {fileID: 11400000, guid: <task_guid>, type: 2}
```

**Ports:**
- Input: `In` (TaskFlow)

### 5. ChoiceNode

Represents a player choice that branches the quest.

```yaml
- rid: <ChoiceNode_wrapper>
  type: {class: UserNodeModelImp, ...}
  data:
    m_Position: {x: 450, y: -150}
    m_ElementColor:
      m_Color: {r: 0.8, g: 0.4, b: 0.1, a: 1}  # Orange for combat
      m_HasUserColor: 1
    m_InputConstantsById:
      m_KeyList:
      - In
      - __option_ChoiceId
      - __option_Priority
      - __option_ChoiceText
      - __option_ChoiceTooltip
      - __option_ChoiceIcon
      - __option_Conditions
      - __option_WorldFlagsOnSelect
      m_ValueList:
      - rid: <ChoiceFlow_constant>
      - rid: <string_constant>           # ChoiceId (e.g., "combat_path")
      - rid: <int_constant>              # Priority (display order)
      - rid: <LocalizedString_constant>  # Choice text shown to player
      - rid: <LocalizedString_constant>  # Tooltip on hover
      - rid: <Sprite_constant>           # Optional icon
      - rid: <Conditions_list_constant>  # Gate conditions
      - rid: <WorldFlags_list_constant>  # Flags to set on selection
```

**Ports:**
- Input: `In` (ChoiceFlow) - from Stage's Choices port
- Output: `Target` (StageFlow) - to target stage

**Key Options:**
- `ChoiceId`: Unique identifier string
- `Priority`: Integer for display order (higher = shown first)
- `ChoiceText`: LocalizedString for button/option text
- `Conditions`: List of Condition_SO for gating (e.g., reputation requirements)
- `WorldFlagsOnSelect`: WorldFlagModification list to set flags when chosen

---

## Asset References (GUIDs)

### Finding GUIDs

GUIDs are stored in `.meta` files alongside each asset:

```bash
# Read the .meta file
cat Assets/path/to/Asset.asset.meta
# Look for: guid: <32-character-hex-string>
```

### Reference Format

```yaml
# ScriptableObject reference
{fileID: 11400000, guid: <32-char-guid>, type: 2}

# Null reference
{fileID: 0}
```

### Common Asset Types to Reference

| Asset Type | Example GUID Location |
|------------|----------------------|
| Task_SO | `ScriptableObjects/Quests/<Quest>/Tasks/*.asset.meta` |
| Condition_SO | `ScriptableObjects/Conditions/*.asset.meta` |
| WorldFlag_SO | Located in `com.hellodev.conditions` package |
| QuestType_SO | `ScriptableObjects/QuestTypes/*.asset.meta` |

---

## LocalizedString Format

LocalizedStrings reference entries in Unity Localization tables.

### Structure

```yaml
- rid: <LocalizedString_constant>
  type: {class: 'Constant`1[[UnityEngine.Localization.LocalizedString, Unity.Localization]]', ...}
  data:
    m_Value:
      m_TableReference:
        m_TableCollectionName: GUID:<table_guid>
      m_TableEntryReference:
        m_KeyId: <entry_key_id>
        m_Key:
      m_FallbackState: 0
      m_WaitForCompletion: 0
      m_LocalVariables: []
```

### Finding Table GUID and Key IDs

1. **Table GUID**: Found in `<TableName> Shared Data.asset.meta`
   ```
   Assets/com.hellodev.questsystem/BasicQuestExample/Settings/Localization/Tables/Stages Shared Data.asset.meta
   → guid: ccbd9d2a3cd282b4d94cebc2587a2f74
   ```

2. **Key IDs**: Found in `<TableName> Shared Data.asset` under `m_Entries`
   ```yaml
   m_Entries:
   - m_Id: 2097639549091847    # ← This is the KeyId
     m_Key: Choice_ConfrontBandits
   ```

### Empty LocalizedString (No Text)

```yaml
m_Value:
  m_TableReference:
    m_TableCollectionName:
  m_TableEntryReference:
    m_KeyId: 0
    m_Key:
  m_FallbackState: 0
  m_WaitForCompletion: 0
  m_LocalVariables: []
```

---

## Wire Connections

Wires connect output ports to input ports between nodes.

### Wire Structure

```yaml
- rid: 2249559135886770700
  type: {class: WireModel, ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}
  data:
    m_Guid:
      m_Value0: 3001000000000000001
      m_Value1: 4001000000000000001
    m_HashGuid:
      serializedVersion: 2
      Hash: b1000000000000000000000000000001
    m_ToPortId: In
    m_ToNodeGuid:
      m_Value0: <target_node_guid_value0>
      m_Value1: <target_node_guid_value1>
    m_FromPortId: FirstStage
    m_FromNodeGuid:
      m_Value0: <source_node_guid_value0>
      m_Value1: <source_node_guid_value1>
```

### Port Names by Node Type

| Node Type | Input Ports | Output Ports |
|-----------|-------------|--------------|
| QuestStartNode | (none) | `FirstStage` |
| StageNode | `In` | `TaskGroups`, `Then`, `Else`, `Choices` |
| TaskGroupNode | `In` | `Tasks`, `Then`, `Else` |
| TaskNode | `In` | (none) |
| ChoiceNode | `In` | `Target` |

### Connection Patterns

```
QuestStartNode.FirstStage → StageNode.In
StageNode.TaskGroups → TaskGroupNode.In
TaskGroupNode.Tasks → TaskNode.In
StageNode.Choices → ChoiceNode.In
ChoiceNode.Target → StageNode.In
StageNode.Then → StageNode.In (linear progression)
TaskGroupNode.Then → TaskGroupNode.In (sequential task groups)
```

---

## Step-by-Step Creation Process

### Phase 1: Gather Information

1. **Read the quest design** - Understand stages, tasks, branching points
2. **Collect Task_SO GUIDs** - Find all task assets and their GUIDs
3. **Collect Condition GUIDs** - For any gated content
4. **Collect WorldFlag GUIDs** - For choice consequences
5. **Find Localization KeyIds** - From Shared Data assets

### Phase 2: Plan the Graph

1. **Map out node structure**:
   ```
   QuestStart → Stage0 → TaskGroup0 → Task0
                      ↓
                Stage1 (HasPlayerChoices=true) → TaskGroup1 → Task1
                      ↓ (Choices port)
                ChoiceA → Stage10 → ...
                ChoiceB → Stage20 → ...
                      ↓
                Stage100 (IsTerminal=true) → TaskGroup100 → Task100
   ```

2. **Assign RID numbers** with gaps for future expansion
3. **Plan node positions** for visual layout

### Phase 3: Create the Graph File

1. **Start with header and GraphModelImp**
2. **Add QuestGraphDefinition** with quest metadata
3. **Create nodes in order**: QuestStart → Stages → TaskGroups → Tasks → Choices
4. **Add all wires** connecting the nodes
5. **Register all node RIDs** in `m_GraphNodeModels`
6. **Register all wire RIDs** in `m_GraphWireModels`
7. **Set entry point** to QuestStartNode RID

### Phase 4: Validate

1. Open Unity and let it reimport
2. Check console for errors/warnings
3. Open the graph in Quest Graph Editor
4. Verify all nodes appear and are connected
5. Check inspector values for each node

---

## Common Pitfalls

### DO

- Use unique GUIDs for every node (`m_Value0`, `m_Value1`, `Hash`)
- Reference existing Task_SO assets (don't create inline)
- Use gaps in stage indices (0, 10, 20, 30) for future insertion
- Set `IsTerminal: true` on final stages
- Set `HasPlayerChoices: true` on stages with choice branches
- Add LocalizedString references for ChoiceNode text
- Include WorldFlagModifications for tracking player choices

### DON'T

- Create `.meta` files manually (Unity generates these)
- Use duplicate RIDs anywhere in the file
- Forget to add nodes to `m_GraphNodeModels` list
- Forget to add wires to `m_GraphWireModels` list
- Leave ChoiceText empty (causes validation warnings)
- Connect wrong port types (StageFlow ↔ StageFlow, etc.)
- Set `IsTerminal: true` on non-final stages

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "No choice text" warning | Empty LocalizedString | Add table GUID and KeyId |
| "Consecutive indices" warning | Stage indices like 0,1,2 | Use gaps: 0, 10, 20 |
| Nodes not appearing | RID not in m_GraphNodeModels | Add to the list |
| Wires not connecting | Wrong port names or GUIDs | Verify port names match node type |
| Task shows as null | Wrong GUID format | Use `{fileID: 11400000, guid: X, type: 2}` |

---

## Validation Checklist

Before considering a quest graph complete:

- [ ] All nodes appear in the graph editor
- [ ] All wires connect correctly
- [ ] QuestStartNode is set as entry point
- [ ] Terminal stage has `IsTerminal: true`
- [ ] Choice stages have `HasPlayerChoices: true`
- [ ] All Task_SO references resolve (not null)
- [ ] All Condition_SO references resolve (if used)
- [ ] All WorldFlag_SO references resolve (if used)
- [ ] ChoiceNodes have localized text (no warnings)
- [ ] Stage indices use gaps for insertion flexibility
- [ ] Node positions create readable layout

---

## Example: The Merchant's Dilemma

Reference implementation: `Graph_Quest_TheMerchantsDilemma.quest`

**Structure:**
```
QuestStart
    ↓
Stage 0 "Introduction" (index: 0)
    → TaskGroup "Meet the Merchant"
        → Task "TalkToMerchant"
    ↓
Stage 1 "TheChoice" (index: 1, HasPlayerChoices: true)
    → TaskGroup "Make Your Choice"
        → Task "DecideBanditApproach"
    ↓ (Choices)
    ├─ Choice "combat_path" (Priority: 0)
    │      WorldFlag: ChoseCombat = true
    │      → Stage 10 "CombatPath"
    │          → Task "DefeatBandits"
    │
    ├─ Choice "diplomacy_path" (Priority: 1)
    │      WorldFlag: ChoseDiplomacy = true
    │      → Stage 20 "DiplomacyPath"
    │          → Task "NegotiateWithBandits"
    │
    └─ Choice "lawful_path" (Priority: 2, Condition: GuardReputation≥20)
           WorldFlag: ChoseLawful = true
           → Stage 30 "LawfulPath"
               → Task "ReportToGuards"
    ↓
Stage 100 "Resolution" (index: 100, IsTerminal: true)
    → TaskGroup "Complete the Quest"
        → Task "ReturnToMerchant"
```

**Key GUIDs Used:**
- Stages Table: `ccbd9d2a3cd282b4d94cebc2587a2f74`
- QuestType_Secondary: `955a193800c30ce41b19879861efbd2f`
- Task_TalkToMerchant: `11d0b01da0f40ef45858a6847b5b72ee`
- Task_DecideBanditApproach: `442743243284c9e4c95f41dd3295fca9`
- Task_DefeatBandits: `0814db39d2e53a14c89588956ecc5c00`
- Task_NegotiateWithBandits: `db4aaf135547ca843abdf319659f87bd`
- Task_ReportToGuards: `fa4c7cb20854a3a4a9a0c4e011c917db`
- Task_ReturnToMerchant: `1b304ff6652e8e3478965ecfb0c21fd4`
- WorldFlag_ChoseCombat: `403b20f15c7514244babced236623411`
- WorldFlag_ChoseDiplomacy: `6943833d92ae6a647b3245de78020ae0`
- WorldFlag_ChoseLawful: `7d93028b9d609d246af7cd4d69e71b5d`
- Condition_GuardReputation20: `766a6c7dfd81c22449f3fed8e3260d5f`

---

## Quick Reference: Constant Types

```yaml
# String
type: {class: 'Constant`1[[System.String, mscorlib]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# Int32
type: {class: 'Constant`1[[System.Int32, mscorlib]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# Boolean
type: {class: 'Constant`1[[System.Boolean, mscorlib]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# LocalizedString
type: {class: 'Constant`1[[UnityEngine.Localization.LocalizedString, Unity.Localization]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# Sprite
type: {class: 'Constant`1[[UnityEngine.Sprite, UnityEngine.CoreModule]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# Task_SO
type: {class: 'Constant`1[[HelloDev.QuestSystem.ScriptableObjects.Task_SO, HelloDev.QuestSystem]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# List<Condition_SO>
type: {class: 'Constant`1[[System.Collections.Generic.List`1[[HelloDev.Conditions.Condition_SO, HelloDev.Conditions]], mscorlib]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# List<WorldFlagModification>
type: {class: 'Constant`1[[System.Collections.Generic.List`1[[HelloDev.Conditions.WorldFlags.WorldFlagModification, HelloDev.Conditions]], mscorlib]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# Flow Types
# StageFlow
type: {class: 'Constant`1[[HelloDev.QuestSystem.QuestGraph.Editor.Ports.StageFlow, HelloDev.QuestSystem.QuestGraph.Editor]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# TaskGroupFlow
type: {class: 'Constant`1[[HelloDev.QuestSystem.QuestGraph.Editor.Ports.TaskGroupFlow, HelloDev.QuestSystem.QuestGraph.Editor]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# TaskFlow
type: {class: 'Constant`1[[HelloDev.QuestSystem.QuestGraph.Editor.Ports.TaskFlow, HelloDev.QuestSystem.QuestGraph.Editor]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}

# ChoiceFlow
type: {class: 'Constant`1[[HelloDev.QuestSystem.QuestGraph.Editor.Ports.ChoiceFlow, HelloDev.QuestSystem.QuestGraph.Editor]]', ns: Unity.GraphToolkit.Editor, asm: Unity.GraphToolkit.Internal.Editor}
```
