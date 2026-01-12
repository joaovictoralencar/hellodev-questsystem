# Understanding Node Options and Ports: Field Visibility Architecture

*Version 2.0 | For: Programmers | Prerequisites: Unity Graph Toolkit basics, C#*

This tutorial explains the architecture behind node fields in the Quest Graph Editor. You'll learn why some fields appear directly on the node while others only appear in the Inspector panel, and the standards for categorizing fields between options and ports.

---

## Table of Contents

1. [The Core Distinction](#the-core-distinction)
2. [Categorization Standards](#categorization-standards)
3. [How Options Are Defined](#how-options-are-defined)
4. [The ShowInInspectorOnly Attribute](#the-showinspectoronly-attribute)
5. [Dynamic Port Creation](#dynamic-port-creation)
6. [Decision Guidelines](#decision-guidelines)
7. [Complete Example: StageNode](#complete-example-stagenode)
8. [Creating Your Own Node with Options](#creating-your-own-node-with-options)

---

## The Core Distinction

When you select a node in the Quest Graph Editor, you'll notice some fields appear in two places:

- **Node Header**: Fields visible directly on the node in the graph canvas
- **Inspector Panel**: All fields, including those hidden from the node header

This distinction is controlled by a single attribute: `.ShowInInspectorOnly()`

### Visual Example

**StageNode Fields:**

| Field | Location | Why |
|-------|----------|-----|
| Stage Index | Node + Inspector | Controls stage ordering, affects node title |
| Has Player Choices | Node + Inspector | Creates/removes the Choices output port |
| Task Group Count | Node + Inspector | Creates/removes task group input ports |
| Is Terminal | Node + Inspector | Removes output flow ports |
| Is Optional | Node + Inspector | Critical stage property, frequently adjusted |
| Stage Icon | Inspector only | Visual data, doesn't affect graph |
| Journal Entry | Inspector only | Display text, doesn't affect graph |
| Is Hidden | Inspector only | Rare runtime flag |

---

## Categorization Standards

When designing nodes, fields fall into three categories. Use these standards to ensure consistent UX across all nodes.

### Category 1: OPTIONS (Inspector, Can Trigger Port Regeneration)

Use OPTIONS when the field:
- **Controls port structure** (creates/removes dynamic ports based on count)
- **Is a mode toggle** (UseTaskAsset, UseQuestAsset, UseStageSubgraph)

**Implementation:** `context.AddOption<T>()` in `OnDefineOptions()`

**Why:** Changing an option value triggers `OnDefinePorts()` to regenerate, enabling dynamic port creation.

**Examples from Quest System:**
| Node | Field | Reason |
|------|-------|--------|
| TaskTypedNode | UseTaskAsset | Mode toggle, changes ports |
| TaskTypedNode | TriggerConditionCount | Controls dynamic port count |
| TaskTypedNode | FailureConditionCount | Controls dynamic port count |
| QuestNode | UseQuestAsset | Mode toggle, changes ports |
| QuestNode | StageCount | Controls dynamic port count |
| StageNode | HasPlayerChoices | Controls Choices port visibility |
| StageNode | TaskGroupCount | Controls dynamic port count |
| TaskDiscoveryNode | RequiredDiscoveries | Controls Discovery ID port count |

### Category 2: INSPECTOR-ONLY OPTIONS

Use when the field:
- **Only affects runtime behavior** (rare flags, not frequently adjusted)
- **Would clutter the node** (secondary configuration)

**Implementation:** `context.AddOption<T>().ShowInInspectorOnly()`

**Examples from Quest System:**
| Node | Field | Reason |
|------|-------|--------|
| StageNode | IsHidden | Rare runtime flag |
| StageNode | StageIcon | Visual data only |
| StageNode | JournalEntry | Content, not structure |

### Category 3: PORTS (Visible on Node + Node Properties)

Use PORTS when the field:
- **Is critical identity** (DevName - users need to see/edit frequently)
- **Is type-specific essential value** (RequiredCount, TimeLimit, TargetValue)
- **Is LocalizedString** (needs table picker UI)
- **Is asset reference** (Condition_SO, Sprite, QuestType_SO)
- **Should be editable without opening inspector**

**Implementation:** `context.AddInputPort<T>()` in `OnDefinePorts()`

**Why:** Ports appear on the node AND in the "Node Properties" inspector section, giving users direct access without navigating to the full inspector.

**Examples from Quest System:**
| Node | Field | Reason |
|------|-------|--------|
| TaskTypedNode | DevName | Critical identity, shown in title |
| TaskIntNode | RequiredCount | Type-specific essential value |
| TaskTimedNode | TimeLimit | Type-specific essential value |
| TaskTimedNode | FailQuestOnExpire | Type-specific essential value |
| TaskStringNode | TargetValue | Type-specific essential value |
| QuestNode | DevName | Critical identity |
| QuestNode | IsOptional | Frequently adjusted |
| QuestNode | RecommendedLevel | Frequently viewed during design |
| QuestNode | DisplayName | LocalizedString needs picker |
| QuestNode | QuestType | Asset reference |

### Quick Decision Flowchart

```
Does field control dynamic port creation (count)?
  └─ YES → OPTION (for port regeneration)

Is field a mode toggle (UseTaskAsset, etc.)?
  └─ YES → OPTION (for port regeneration)

Is field LocalizedString, asset reference, or primitive value users edit frequently?
  └─ YES → PORT (visible on node + Node Properties)

Is field rarely changed or would clutter the node?
  └─ YES → INSPECTOR-ONLY OPTION
  └─ NO  → PORT
```

---

## How Options Are Defined

Options are defined in the `OnDefineOptions` method of your node class. The Graph Toolkit provides a fluent API for configuring each option.

### Basic Option Definition

```csharp
protected override void OnDefineOptions(IOptionDefinitionContext context)
{
    // This field appears on the node AND in inspector
    context.AddOption<int>(OPT_STAGE_INDEX)
        .WithDisplayName("Stage Index")
        .WithDefaultValue(0)
        .Delayed();  // Waits for user to finish typing

    // This field appears ONLY in inspector
    context.AddOption<Sprite>(OPT_STAGE_ICON)
        .WithDisplayName("Stage Icon")
        .ShowInInspectorOnly();  // <-- The key attribute
}
```

### Reading Option Values

Use the `GetOptionValue<T>()` helper method (defined in `QuestBaseNode`):

```csharp
// Property that reads the option value
public int StageIndex => GetOptionValue<int>(OPT_STAGE_INDEX);
public bool HasPlayerChoices => GetOptionValue<bool>(OPT_HAS_PLAYER_CHOICES);
public Sprite StageIcon => GetOptionValue<Sprite>(OPT_STAGE_ICON);
```

---

## The ShowInInspectorOnly Attribute

The `.ShowInInspectorOnly()` attribute controls visibility:

### Without ShowInInspectorOnly (Default)

```csharp
context.AddOption<bool>(OPT_HAS_PLAYER_CHOICES)
    .WithDisplayName("Has Player Choices")
    .WithDefaultValue(false);
```

**Result:** Field appears in the node header AND the Inspector panel.

### With ShowInInspectorOnly

```csharp
context.AddOption<LocalizedString>(OPT_JOURNAL_ENTRY)
    .WithDisplayName("Journal Entry")
    .ShowInInspectorOnly();
```

**Result:** Field appears ONLY in the Inspector panel.

---

## Dynamic Port Creation

The real power of node variables (fields on the node) is **reactive port generation**. When you change a field value, the Graph Toolkit automatically regenerates ports.

### How It Works

1. User changes a field value (e.g., toggles "Has Player Choices")
2. Graph Toolkit updates the option internally
3. Graph Toolkit calls `OnDefinePorts()` again
4. Your code reads the new value and creates/omits ports accordingly
5. UI updates immediately

### Example: HasPlayerChoices

```csharp
// StageNode.cs - OnDefinePorts method
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    // Always create the input port
    context.AddInputPort<StageFlow>("In")
        .WithDisplayName("From")
        .Build();

    // Success flow (unless terminal)
    if (!IsTerminal)
    {
        context.AddOutputPort<StageFlow>("Then")
            .WithDisplayName("Then")
            .Build();
    }

    // DYNAMIC: Only create Choices port if HasPlayerChoices is true
    if (HasPlayerChoices)
    {
        context.AddOutputPort<ChoiceFlow>("Choices")
            .WithDisplayName("Player Choices")
            .Build();
    }
}
```

**When user toggles "Has Player Choices":**
- `false` → No Choices port visible
- `true` → Choices port appears, ready for connections

### Example: TaskGroupCount

```csharp
// Multiple ports based on an integer count
protected override void OnDefinePorts(IPortDefinitionContext context)
{
    context.AddInputPort<StageFlow>("In").Build();

    // Create N task group ports based on count option
    for (int i = 0; i < TaskGroupCount; i++)
    {
        context.AddInputPort<TaskFlow>($"TaskGroup{i}")
            .WithDisplayName($"Task Group {i + 1}")
            .Build();
    }

    context.AddOutputPort<StageFlow>("Then").Build();
}
```

**When user changes count from 1 to 3:**
- 1 port → 3 ports appear dynamically

---

## Decision Guidelines

Use this decision tree when designing your node options:

### Keep on Node Header (No ShowInInspectorOnly)

Use when the field:
- **Controls port structure** (adds/removes ports)
- **Affects graph connectivity** (what can connect to what)
- **Changes node appearance** significantly
- **Is frequently adjusted** while designing the graph

**Examples:**
- `HasPlayerChoices` - Creates Choices output port
- `TaskGroupCount` - Creates multiple input ports
- `IsTerminal` - Removes output flow ports
- `OutputCount` - Creates multiple output ports
- `StageIndex` - Shows in node title, ordering matters

### Move to Inspector Only (Use ShowInInspectorOnly)

Use when the field:
- **Only affects runtime behavior** (not graph structure)
- **Is data/content** rather than structural
- **Doesn't affect connections**
- **Would clutter the node** if always visible
- **Is rarely changed** during graphing

**Examples:**
- `JournalEntry` - Just localized text for UI
- `StageIcon` - Just a sprite reference
- `IsHidden` - Runtime visibility flag (rarely changed)
- `QuestOrderOverride` - Rarely modified override value

### Summary Table

| Criteria | Node Header | Inspector Only | Port-Based |
|----------|-------------|----------------|------------|
| Controls port structure | Yes | No | N/A |
| Critical identity (DevName) | Yes | No | No |
| Type-specific essential value | Yes | No | No |
| Frequently adjusted | Yes | No | No |
| LocalizedString | No | No | Yes |
| Asset reference | No | No | Yes |
| Collection of items | No | No | Yes |
| Rare runtime flags | No | Yes | No |
| Would clutter node | No | Yes | No |

---

## Complete Example: StageNode

Here's the complete option definition from StageNode showing both patterns:

```csharp
public class StageNode : QuestBaseNode
{
    // Option name constants
    private const string OPT_USE_STAGE_SUBGRAPH = "UseStageSubgraph";
    private const string OPT_STAGE_INDEX = "StageIndex";
    private const string OPT_STAGE_NAME = "StageName";
    private const string OPT_JOURNAL_ENTRY = "JournalEntry";
    private const string OPT_STAGE_ICON = "StageIcon";
    private const string OPT_IS_TERMINAL = "IsTerminal";
    private const string OPT_IS_OPTIONAL = "IsOptional";
    private const string OPT_IS_HIDDEN = "IsHidden";
    private const string OPT_HAS_PLAYER_CHOICES = "HasPlayerChoices";
    private const string OPT_TASK_GROUP_COUNT = "TaskGroupCount";

    // Properties to read option values
    public bool UseStageSubgraph => GetOptionValue<bool>(OPT_USE_STAGE_SUBGRAPH);
    public int StageIndex => GetOptionValue<int>(OPT_STAGE_INDEX);
    public string StageName => GetOptionValue<string>(OPT_STAGE_NAME);
    public LocalizedString JournalEntry => GetOptionValue<LocalizedString>(OPT_JOURNAL_ENTRY);
    public Sprite StageIcon => GetOptionValue<Sprite>(OPT_STAGE_ICON);
    public bool IsTerminal => GetOptionValue<bool>(OPT_IS_TERMINAL);
    public bool IsOptional => GetOptionValue<bool>(OPT_IS_OPTIONAL);
    public bool IsHidden => GetOptionValue<bool>(OPT_IS_HIDDEN);
    public bool HasPlayerChoices => GetOptionValue<bool>(OPT_HAS_PLAYER_CHOICES);
    public int TaskGroupCount => GetOptionValue<int>(OPT_TASK_GROUP_COUNT);

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // === NODE HEADER OPTIONS (affect graph structure) ===

        // Mode toggle - determines Asset vs Define mode
        context.AddOption<bool>(OPT_USE_STAGE_SUBGRAPH)
            .WithDisplayName("Use Stage Subgraph")
            .WithDefaultValue(false);

        // Ordering - shown in node title
        context.AddOption<int>(OPT_STAGE_INDEX)
            .WithDisplayName("Stage Index")
            .WithDefaultValue(0)
            .Delayed();

        // Only show Define mode options when not using subgraph
        if (!UseStageSubgraph)
        {
            // Affects port generation
            context.AddOption<bool>(OPT_HAS_PLAYER_CHOICES)
                .WithDisplayName("Has Player Choices")
                .WithDefaultValue(false);

            context.AddOption<int>(OPT_TASK_GROUP_COUNT)
                .WithDisplayName("Task Group Count")
                .WithDefaultValue(1);

            // Affects output port visibility
            context.AddOption<bool>(OPT_IS_TERMINAL)
                .WithDisplayName("Is Terminal")
                .WithDefaultValue(false);

            // Frequently adjusted - keep on node
            context.AddOption<bool>(OPT_IS_OPTIONAL)
                .WithDisplayName("Is Optional")
                .WithDefaultValue(false);

            context.AddOption<string>(OPT_STAGE_NAME)
                .WithDisplayName("Stage Name")
                .WithDefaultValue("New Stage")
                .Delayed();

            // === INSPECTOR ONLY OPTIONS (data, not structure) ===

            context.AddOption<LocalizedString>(OPT_JOURNAL_ENTRY)
                .WithDisplayName("Journal Entry")
                .ShowInInspectorOnly();

            context.AddOption<Sprite>(OPT_STAGE_ICON)
                .WithDisplayName("Stage Icon")
                .ShowInInspectorOnly();

            context.AddOption<bool>(OPT_IS_HIDDEN)
                .WithDisplayName("Is Hidden")
                .ShowInInspectorOnly();
        }
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Input always exists
        context.AddInputPort<StageFlow>("In")
            .WithDisplayName("From")
            .Build();

        if (UseStageSubgraph)
        {
            // Asset mode: reference external subgraph
            context.AddInputPort<StageGraph>("StageSubgraph")
                .WithDisplayName("Stage Subgraph")
                .Build();
        }
        else
        {
            // Define mode: show task group ports
            for (int i = 0; i < TaskGroupCount; i++)
            {
                context.AddInputPort<TaskFlow>($"TaskGroup{i}")
                    .WithDisplayName($"Task Group {i + 1}")
                    .Build();
            }
        }

        // Output ports (unless terminal)
        if (!IsTerminal)
        {
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .Build();

            // Dynamic: only if HasPlayerChoices
            if (HasPlayerChoices)
            {
                context.AddOutputPort<ChoiceFlow>("Choices")
                    .WithDisplayName("Player Choices")
                    .Build();
            }
        }
    }
}
```

---

## Creating Your Own Node with Options

Follow this pattern when creating custom nodes:

### Step 1: Define Option Constants

```csharp
public class MyCustomNode : QuestBaseNode
{
    // Use constants for option names (prevents typos)
    private const string OPT_MY_COUNT = "MyCount";
    private const string OPT_MY_DATA = "MyData";
    private const string OPT_ENABLE_FEATURE = "EnableFeature";
```

### Step 2: Create Properties

```csharp
    // Properties provide clean access to option values
    public int MyCount => GetOptionValue<int>(OPT_MY_COUNT);
    public string MyData => GetOptionValue<string>(OPT_MY_DATA);
    public bool EnableFeature => GetOptionValue<bool>(OPT_ENABLE_FEATURE);
```

### Step 3: Define Options

```csharp
    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // Structural option - affects ports, keep on node
        context.AddOption<int>(OPT_MY_COUNT)
            .WithDisplayName("My Count")
            .WithDefaultValue(1);

        // Structural option - creates/removes port
        context.AddOption<bool>(OPT_ENABLE_FEATURE)
            .WithDisplayName("Enable Feature")
            .WithDefaultValue(false);

        // Data option - just content, hide from node
        context.AddOption<string>(OPT_MY_DATA)
            .WithDisplayName("My Data")
            .ShowInInspectorOnly();
    }
```

### Step 4: Use Options in Port Definition

```csharp
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<MyFlow>("In").Build();

        // Dynamic ports based on count
        for (int i = 0; i < MyCount; i++)
        {
            context.AddInputPort<DataFlow>($"Data{i}")
                .WithDisplayName($"Data {i + 1}")
                .Build();
        }

        context.AddOutputPort<MyFlow>("Then").Build();

        // Conditional port based on boolean
        if (EnableFeature)
        {
            context.AddOutputPort<FeatureFlow>("Feature")
                .WithDisplayName("Feature Output")
                .Build();
        }
    }
}
```

---

## Key Takeaways

1. **`.ShowInInspectorOnly()`** is the only attribute that controls field visibility
2. **Node header fields** should control graph structure (ports, connections)
3. **Inspector-only fields** should hold data that doesn't affect the graph
4. **Port generation is reactive** - changing an option value automatically regenerates ports
5. **Use properties** to access option values cleanly via `GetOptionValue<T>()`

---

## Related Documentation

- [Quest Graph Editor Guide](../quest-graph-editor-guide.md) - Full implementation details
- [Graph Creation Reference](../quest-graph-creation-reference.md) - YAML structure reference
- [Graph Editor Tutorial](tutorial-graph-editor.md) - Designer workflow guide
