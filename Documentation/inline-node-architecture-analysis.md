# Inline Node Architecture Analysis

## Executive Summary

This document analyzes the feasibility and design patterns for extending the Quest Graph Editor's inline creation pattern (currently used for Tasks) to Conditions and Events. It also explains why IDs and WorldFlags must remain as pure ScriptableObject assets.

**Key Decisions:**
- **Conditions**: Can and should support inline creation (high value)
- **Events**: Must remain ScriptableObjects only (shared communication channels)
- **IDs**: Must remain ScriptableObjects only (identity/reference integrity)
- **WorldFlags**: Must remain ScriptableObjects only (state management)

---

## Related Documentation

Before implementing any changes, review these documents:

| Document | Purpose | Location |
|----------|---------|----------|
| **Quest Graph Editor Guide** | Complete editor documentation | [quest-graph-editor-guide.md](quest-graph-editor-guide.md) |
| **Quest Graph Creation Reference** | YAML structure and node details | [quest-graph-creation-reference.md](quest-graph-creation-reference.md) |
| **Tutorial: Creating Tasks** | Task creation workflow | [tutorial-creating-tasks.md](tutorial-creating-tasks.md) |
| **Tutorial: Creating Quests** | Quest creation workflow | [tutorial-creating-quests.md](tutorial-creating-quests.md) |
| **Tasks Documentation** | Task types and runtime behavior | [tasks.md](tasks.md) |
| **Architecture Overview** | System architecture | [architecture.md](architecture.md) |

---

## Current State: Task Inline Pattern

The Quest Graph Editor currently supports inline task creation with this pattern:

```
TaskNode
├── Asset Mode: Reference existing Task_SO
└── Define Mode: Create task inline (auto-detected when Task Asset is empty)
```

### How It Works

1. **Mode Detection** (TaskBaseNode.cs)
   ```csharp
   public bool IsAssetMode => TaskAsset != null;
   public bool IsDefineMode => TaskAsset == null;
   ```

2. **Inline Data Container** (InlineTaskData.cs)
   - Holds all fields needed to create a Task_SO
   - Factory method: `CreateTaskAsset<TTask>()`

3. **Converter** (GraphToQuestConverter.cs)
   - Checks mode and creates inline assets during export
   - Tracks created assets in `_createdInlineTasks` list

4. **Importer** (QuestGraphImporter.cs)
   - Registers inline tasks as sub-assets within the .quest file
   - Pattern: `ctx.AddObjectToAsset($"InlineTask_{i}_{name}", task)`

### Node Type Hierarchy

```
TaskBaseNode (abstract)
├── TaskBoolNode      → TaskBool_SO
├── TaskIntNode       → TaskInt_SO (RequiredCount)
├── TaskStringNode    → TaskString_SO (TargetValue)
├── TaskLocationNode  → TaskLocation_SO
├── TaskDiscoveryNode → TaskDiscovery_SO (RequiredDiscoveries)
└── TaskTimedNode     → TaskTimed_SO (TimeLimit, FailQuestOnExpire)
```

---

## Analysis: Conditions as Nodes

### Current Condition System

**Class Hierarchy:**
```
ICondition (interface)
└── Condition_SO (abstract)
    ├── ConditionEventDriven_SO<T> (generic base)
    │   ├── ConditionInt_SO      → GameEventInt_SO + targetValue
    │   ├── ConditionBool_SO     → GameEventBool_SO + targetValue
    │   ├── ConditionFloat_SO    → GameEventFloat_SO + targetValue
    │   ├── ConditionString_SO   → GameEventString_SO + targetValue
    │   └── ConditionID_SO       → GameEventID_SO + targetValue (ID_SO)
    ├── ConditionWorldFlagBool_SO → WorldFlagBool_SO + expectedValue
    ├── ConditionWorldFlagInt_SO  → WorldFlagInt_SO + targetValue
    ├── ConditionQuestState_SO    → Quest_SO + targetState
    ├── ConditionQuestLineState_SO → QuestLine_SO + targetState
    └── CompositeCondition_SO     → List<Condition_SO> + AND/OR operator
```

### Proposed Condition Node Architecture

**Pattern: Subgraph → Asset Reference → Inline**

```
ConditionNode (new, for embedding in task nodes)
├── Asset Mode: Reference existing Condition_SO
└── Define Mode: Create condition inline

Condition-Specific Nodes (for standalone use):
├── ConditionIntNode      → Creates ConditionInt_SO
├── ConditionBoolNode     → Creates ConditionBool_SO
├── ConditionIDNode       → Creates ConditionID_SO
├── ConditionWorldFlagNode → Creates ConditionWorldFlag_SO
└── ConditionCompositeNode → Creates CompositeCondition_SO (subgraph pattern)
```

### Implementation Requirements

#### 1. Create ConditionBaseNode
```csharp
[Serializable]
public abstract class ConditionBaseNode : QuestBaseNode
{
    // Asset mode
    public abstract Condition_SO ConditionAsset { get; }

    // Mode detection (same pattern as TaskBaseNode)
    public bool IsAssetMode => ConditionAsset != null;
    public bool IsDefineMode => ConditionAsset == null;

    // Inline data
    public abstract InlineConditionData InlineData { get; }

    // Factory
    public abstract Condition_SO CreateConditionAsset();

    // Type-specific options
    protected abstract void OnDefineTypeSpecificOptions(IOptionDefinitionContext context);
    protected abstract void PopulateTypeSpecificData(InlineConditionData data);
}
```

#### 2. Create InlineConditionData
```csharp
[Serializable]
public class InlineConditionData
{
    // Common fields
    public string devName = "New Condition";
    public bool isInverted = false;
    public ComparisonType comparisonType = ComparisonType.Equals;

    // Event-driven fields
    public GameEventInt_SO gameEventInt;
    public GameEventBool_SO gameEventBool;
    public GameEventID_SO gameEventID;
    public int targetInt;
    public bool targetBool;
    public ID_SO targetID;

    // WorldFlag fields
    public WorldFlagLocator_SO flagLocator;
    public WorldFlagBool_SO worldFlagBool;
    public WorldFlagInt_SO worldFlagInt;
    public bool expectedBoolValue;
    public int expectedIntValue;

    // Factory method
    public TCondition CreateConditionAsset<TCondition>() where TCondition : Condition_SO;
}
```

#### 3. Type-Specific Nodes

| Node | Inline Fields | Creates |
|------|---------------|---------|
| ConditionIntNode | GameEventInt, targetValue, comparisonType | ConditionInt_SO |
| ConditionBoolNode | GameEventBool, targetValue | ConditionBool_SO |
| ConditionIDNode | GameEventID, targetID | ConditionID_SO |
| ConditionWorldFlagBoolNode | WorldFlagLocator, WorldFlagBool, expectedValue | ConditionWorldFlagBool_SO |
| ConditionWorldFlagIntNode | WorldFlagLocator, WorldFlagInt, targetValue, comparisonType | ConditionWorldFlagInt_SO |

#### 4. Composite Condition Node (Subgraph Pattern)

```
CompositeConditionGraph (.conditiongroup extension)
├── ConditionStartNode (defines composite operator: AND/OR)
├── Multiple ConditionNodes (child conditions)
└── Exports to: CompositeCondition_SO
```

### Integration Points

1. **TaskBaseNode**: Replace `List<Condition_SO>` option with embedded condition nodes
2. **StageNode**: Replace conditions list with condition node connections
3. **ChoiceNode**: Replace conditions list with condition node connections
4. **GraphToQuestConverter**: Add `_createdInlineConditions` tracking
5. **QuestGraphImporter**: Register inline conditions as sub-assets

### Benefits

- **Designers don't need to pre-create Condition_SO assets**
- **Visual condition authoring directly in the graph**
- **Composite conditions become visual subgraphs**
- **Consistent pattern with inline tasks**

### Complexity Assessment

| Aspect | Difficulty | Notes |
|--------|------------|-------|
| Base class pattern | Low | Copy TaskBaseNode pattern |
| Type-specific nodes | Medium | 5-7 node types to create |
| Composite subgraph | High | New graph type needed |
| Converter updates | Medium | Follow task pattern |
| Migration | Low | Existing graphs continue to work |

---

## Analysis: Events - Why No Inline Support

### Current Event System

**Class Hierarchy:**
```
GameEventBase_SO (abstract)
├── GameEventVoid_SO          → No parameter
└── GameEvent_SO<T> (generic)
    ├── GameEventInt_SO       → int parameter
    ├── GameEventBool_SO      → bool parameter
    ├── GameEventFloat_SO     → float parameter
    ├── GameEventString_SO    → string parameter
    └── GameEventID_SO        → ID_SO parameter
```

### The Problem with Inline Events

Events are **shared communication channels**. Publishers and subscribers must reference the **exact same event object**.

**If events were inline:**
```
Monster.cs:
  [SerializeField] private GameEventID_SO onMonsterKilled;  // Inline event
  void OnDeath() => onMonsterKilled.Raise(this.monsterId);

QuestCondition (in graph):
  → Inline GameEventID_SO "OnMonsterKilled"  // DIFFERENT object!

// Result: Quest NEVER receives the monster's event!
// The publisher and subscriber are using different event instances.
```

### Events Are Like IDs

| Aspect | ID_SO | GameEvent_SO |
|--------|-------|--------------|
| **Purpose** | Unique identifier | Communication channel |
| **Shared by** | Multiple systems referencing same entity | Publishers + subscribers |
| **Requires same instance?** | Yes - for equality | Yes - for communication |
| **Inline breaks** | Reference equality | Pub/sub connection |

### Why ScriptableObject is Required

| Requirement | Asset Approach | Inline Would Break |
|-------------|----------------|-------------------|
| **Pub/sub connection** | Same asset = connected | Different instances = disconnected |
| **Cross-system** | Monster → Event ← Quest | Each has different event |
| **Runtime listeners** | Single listener list | Multiple orphaned lists |
| **Event discovery** | Find asset in project | Can't find inline events |
| **Debugging** | Inspect event's listeners | Which event to inspect? |

### Event Usage Pattern (Correct)

```csharp
// 1. Create GameEventID_SO asset: "OnMonsterKilled"

// 2. Publisher references the asset
public class Monster : MonoBehaviour
{
    [SerializeField] private GameEventID_SO onMonsterKilled;  // Drag asset here
    void OnDeath() => onMonsterKilled.Raise(monsterId);
}

// 3. Condition references SAME asset
// ConditionID_SO.gameEvent → same "OnMonsterKilled" asset
// When monster dies, condition receives the event
```

### Recommendation

**Events must remain ScriptableObject-only.** No inline support.

The graph should provide:
- Event **picker** UI for selecting existing events
- Event **browser** for finding events by type/category
- Clear visual **connection** showing which events are referenced

---

## Analysis: IDs - Why No Inline Support

### The Problem with Inline IDs

IDs serve as **stable, persistent identifiers** across:
- Save/load systems
- Multiple quest references
- Cross-scene persistence
- Runtime lookup tables

**If IDs were inline:**
```
Quest A → Inline ID "Goblin"
Quest B → Inline ID "Goblin"  // DIFFERENT object!

// These would NOT be equal:
questA.TargetID == questB.TargetID  // FALSE - different instances!
```

### ID_SO Critical Properties

```csharp
public class ID_SO : RuntimeScriptableObject, IEquatable<ID_SO>
{
    [SerializeField] private string id;  // GUID - stable across sessions

    // Equality based on GUID, not object reference
    public bool Equals(ID_SO other) =>
        other != null && id == other.id;
}
```

### Why ScriptableObject is Required

| Requirement | Asset Approach | Inline Would Break |
|-------------|----------------|-------------------|
| **GUID stability** | Single source of truth | Each inline = new GUID |
| **Reference equality** | Same asset = same ID | Multiple copies = different IDs |
| **Save/load** | Serialize asset reference | Which inline to save? |
| **Cross-quest** | Share same asset | Each quest has copy |
| **Designer workflow** | Drag-drop reuse | Must recreate each time |

### Recommendation

**IDs must remain ScriptableObject-only.** No inline support.

The graph should provide:
- ID **picker** UI for selecting existing IDs
- ID **browser** for finding IDs by category
- Quick **create** button that makes a new ID_SO asset

---

## Analysis: WorldFlags - Why No Inline Support

### The Problem with Inline WorldFlags

WorldFlags represent **persistent world state** that:
- Survives scene changes
- Is saved/loaded with game state
- Is referenced by multiple systems
- Has runtime instances managed by WorldFlagManager

**If WorldFlags were inline:**
```
Stage A → Inline WorldFlag "PlayerChoseViolence"
Stage B → Check WorldFlag "PlayerChoseViolence"  // DIFFERENT flag!

// Stage B would never see Stage A's flag change
```

### WorldFlag Architecture (Two-Layer)

```
Configuration Layer (ScriptableObject - Immutable)
├── WorldFlagBool_SO  → flagGuid, flagName, defaultValue
└── WorldFlagInt_SO   → flagGuid, flagName, defaultValue

Runtime Layer (C# Class - Mutable)
├── WorldFlagBoolRuntime  → currentValue, OnValueChanged events
└── WorldFlagIntRuntime   → currentValue, OnValueChanged events

Management Layer
├── WorldFlagManager      → Creates runtime from config, manages state
└── WorldFlagLocator_SO   → Decoupled access to manager
```

### Why ScriptableObject is Required

| Requirement | Asset Approach | Inline Would Break |
|-------------|----------------|-------------------|
| **Runtime creation** | Manager creates from asset | What creates inline runtime? |
| **State persistence** | Manager saves all flag state | Inline flags are orphaned |
| **Cross-stage access** | Same asset = same runtime | Each stage has different flag |
| **Modification pattern** | WorldFlagModification refs asset | Can't reference inline |
| **Condition pattern** | ConditionWorldFlag refs asset | Can't reference inline |

### Recommendation

**WorldFlags must remain ScriptableObject-only.** No inline support.

The graph should provide:
- WorldFlag **picker** UI
- WorldFlagModification **editor** in stage nodes
- Visual **dependency graph** showing which stages modify/check flags

---

## Implementation Priority

### Phase 1: Condition Nodes (High Value)

1. Create `ConditionBaseNode` abstract class
2. Create `InlineConditionData` container
3. Implement type-specific nodes:
   - `ConditionIntNode`
   - `ConditionBoolNode`
   - `ConditionIDNode`
   - `ConditionWorldFlagBoolNode`
   - `ConditionWorldFlagIntNode`
4. Update `GraphToQuestConverter` with `_createdInlineConditions`
5. Update `QuestGraphImporter` to register inline conditions
6. Connect condition nodes to TaskNodes (replace `List<Condition_SO>` option)

### Phase 2: Composite Condition Subgraph (Medium Value)

1. Create `ConditionGroupGraph` (.conditiongroup extension)
2. Create `ConditionGroupStartNode` (AND/OR operator)
3. Create `ConditionGroupImporter`
4. Create `CompositeConditionNode` for embedding in main graphs

### Phase 3: Improved Asset Pickers (Quality of Life)

1. Better **event picker** UI with search/filter
2. Better **ID picker** UI with category grouping
3. Better **WorldFlag picker** UI with flag browser
4. Event/ID/Flag **browser panels** for discovery

### Not Planned (Architectural Constraints)

| System | Why No Inline |
|--------|---------------|
| **IDs** | Breaks referential integrity - multiple systems must reference same ID |
| **WorldFlags** | Breaks state management - runtime state tied to asset identity |
| **Events** | Breaks pub/sub - publishers and subscribers must share same instance |

---

## Migration Strategy

### Backward Compatibility

All existing graphs continue to work:
- Task nodes with asset references → unchanged
- Condition lists as options → continue working
- Event references → unchanged

### Forward Path

New graphs can choose:
- Asset mode: reference existing assets (same as before)
- Define mode: create inline (new capability)

### No Breaking Changes

The inline pattern is additive:
- Old graphs: use asset references
- New graphs: can use inline or asset
- Mixed: some inline, some asset references

---

## File Structure (Proposed)

```
Editor/Graphs/Scripts/
├── Nodes/
│   ├── Conditions/
│   │   ├── ConditionBaseNode.cs       # Abstract base
│   │   ├── ConditionIntNode.cs        # Inline ConditionInt_SO
│   │   ├── ConditionBoolNode.cs       # Inline ConditionBool_SO
│   │   ├── ConditionIDNode.cs         # Inline ConditionID_SO
│   │   ├── ConditionWorldFlagBoolNode.cs
│   │   ├── ConditionWorldFlagIntNode.cs
│   │   └── InlineConditionData.cs     # Data container
│   ├── Events/
│   │   ├── EventBaseNode.cs           # (Future)
│   │   └── EventReferenceNode.cs      # (Future)
│   └── (existing task nodes)
├── Converters/
│   └── GraphToQuestConverter.cs       # Add condition handling
└── Importers/
    └── QuestGraphImporter.cs          # Add condition sub-assets
```

---

## Summary Decision Matrix

| System | Inline Support | Subgraph Support | Reason |
|--------|---------------|------------------|--------|
| **Tasks** | Yes (existing) | Yes (TaskGroupGraph) | Quest-specific, high designer value |
| **Conditions** | Yes (proposed) | Yes (CompositeCondition) | Evaluation logic, high designer value |
| **Events** | No | No | Shared pub/sub channels - must be same instance |
| **IDs** | No | No | Shared identity - must be same instance |
| **WorldFlags** | No | No | Shared state - runtime tied to asset identity |

### The Shared vs. Quest-Specific Rule

**Can be inline (quest-specific):**
- Tasks: Each quest has its own task instances
- Conditions: Evaluation logic can be quest-specific

**Must be assets (shared across systems):**
- Events: Publisher and subscriber must reference same object
- IDs: Multiple systems must agree on identity
- WorldFlags: State must be accessible from anywhere

---

## Appendix: Graph Toolkit Limitation Note

**Important:** As of Graph Toolkit 0.4.0-exp.2, the `IOptionBuilder` API does not support conditional field visibility. This means:

- All fields (Asset mode + Define mode) are always visible
- "Inline:" prefix is used to clarify which fields apply to inline mode
- Users fill in only the relevant fields based on their chosen mode

This is a known limitation and may be addressed in future Graph Toolkit versions.

---

*Document created: 2026-01-06*
*Last updated: 2026-01-06*
