# [System Name]

*Last Updated: YYYY-MM-DD*

---

## What You'll Build

By the end of this guide, you'll have:

- [Outcome 1]
- [Outcome 2]
- [Outcome 3]

**Preview:**

```
[ASCII art or description of end result]
```

---

## Prerequisites

### Required Packages

| Package | Purpose | How to Verify |
|---------|---------|---------------|
| **[Package]** | [Purpose] | [Verification steps] |

### Optional Packages

| Package | Purpose | Needed For |
|---------|---------|------------|
| **[Package]** | [Purpose] | [Which features] |

### Unity Version

- Unity [X.X] or newer

### Project Setup

- [Any existing requirements]

---

## Glossary

| Term | Meaning |
|------|---------|
| `[Term]` | **[Full Name]** - [Description] |

**How they connect:**

```
[Component1] (description)
    ↓ [relationship]
[Component2] (description)
    ↓ [relationship]
[Component3] (description)
```

---

## Quick Start (Minimal Setup)

### Step 1: [Action] (X minutes)

1. [Instruction]
2. [Instruction]

### Step 2: [Action] (X minutes)

1. [Instruction]
2. [Instruction]

### Step 3: Create Test Script (X minutes)

```csharp
using [Namespace];
using UnityEngine;

public class [SystemName]TestStarter : MonoBehaviour
{
    [SerializeField] private [Type] [field];

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.[Key]))
        {
            Debug.Log("[Starting system]...");
            [Manager].Instance.[Method]([field]);
        }
    }
}
```

### Step 4: Test It

1. [Setup instruction]
2. Enter **Play Mode**
3. Press **[Key]**
4. Check Console for:
   ```
   [Expected output line 1]
   [Expected output line 2]
   ```

**It works!** Continue to Full Setup for proper implementation.

---

## Full Setup Guide

### Part 1: [Section Name]

#### Step 1.1: [Action]

1. In **[Location]**, right-click → **[Menu Path]**
2. Name it: `[Name]`
3. **Add Component** → search `[Component]` → add it

#### Step 1.2: Configure Settings

| Field | Value | Why |
|-------|-------|-----|
| **[Field Name]** | `[value]` | [Explanation] |
| **[Field Name]** | ✓ checked | [Explanation] |

#### Checkpoint: Verify [Part 1]

1. Enter **Play Mode**
2. Check Console for: `[Expected message]`
3. Exit Play Mode

---

### Part 2: [Section Name]

#### Step 2.1: [Action]

1. [Instruction]
2. [Instruction]

| Field | Value | Why |
|-------|-------|-----|
| **[Field]** | `[value]` | [Why] |

#### Step 2.2: [Action]

1. [Instruction]
2. [Instruction]

#### Checkpoint: Verify [Part 2]

Your [location] should look like:
```
[Parent]
├── [Child 1]
├── [Child 2]
└── [Child 3]
```

---

### Part 3: [Section Name]

#### Step 3.1: [Action]

1. [Instruction]
2. [Instruction]

#### Step 3.2: Assign References

**Required References:**

| Field | What to Drag | From Where |
|-------|--------------|------------|
| **[Field]** | `[Object]` | [Location] |

**Optional References:**

| Field | What to Drag | What Happens If Empty |
|-------|--------------|----------------------|
| **[Field]** | `[Object]` | [Consequence] |

#### Checkpoint: Test Complete Flow

1. Enter **Play Mode**
2. [Trigger action]
3. Observe:
   - [Expected behavior 1]
   - [Expected behavior 2]
4. Check Console shows completion messages

---

## Optional Enhancements

### Enhancement 1: [Feature Name]

[Description of what this adds]

1. [Instruction]
2. [Instruction]

**Without this:** [What experience is like without it]

### Enhancement 2: [Feature Name]

[Description]

1. [Instruction]
2. [Instruction]

---

## [Advanced Feature] (Advanced)

For [use case description].

### When to Use This

- [Scenario 1]
- [Scenario 2]

### Step 1: [Action]

1. [Instruction]
2. [Instruction]

### Step 2: [Action]

```csharp
// Code example if applicable
```

### Step 3: Configure

1. [Instruction]
2. [Instruction]

---

## API Reference

### [Manager] Methods

```csharp
// [Description]
[ReturnType] result = [Manager].Instance.[Method]([params]);

// [Description]
[Manager].Instance.[Method]();

// [Description]
bool [result] = [Manager].Instance.[Property];
```

### Events

```csharp
// Subscribe to events
[Manager].Instance.[Event].AddListener([Handler]);

void [Handler]([ParamType] [param])
{
    // [What to do]
}
```

### Save/Load Integration

```csharp
// Saving
[Type] data = [Manager].Instance.[GetSaveMethod]();
// Store in save file

// Loading
[Manager].Instance.[RestoreMethod](loadedData);
```

---

## Troubleshooting

### [Category 1] Problems

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| [What user sees] | [Why] | [Fix] |
| [What user sees] | [Why] | [Fix] |

### [Category 2] Problems

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| [What user sees] | [Why] | [Fix] |
| [What user sees] | [Why] | [Fix] |

### Debugging Tips

1. **Enable logging**: [How to enable debug mode]
2. **Check Console**: Look for `[Prefix]` messages
3. **Verify references**: Select [Component] and confirm all fields assigned
4. **Test isolation**: [How to test component independently]

---

## Architecture Reference

```
┌─────────────────────────────────────────────────────────────────┐
│                        SCRIPTABLEOBJECTS                        │
│                    (Assets in Project folder)                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Config]_SO ─────────────┐                                    │
│   - [field1]               │                                    │
│   - [field2]               │ [relationship]                     │
│                            ▼                                    │
│                      [SubConfig]_SO                             │
│                      - [field1]                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ creates (at runtime)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      RUNTIME INSTANCES                          │
│                   (Created during Play mode)                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Config]Runtime                                               │
│   - [mutableState]                                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ managed by
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SCENE COMPONENTS                            │
│                  (MonoBehaviours in scene)                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [System]Manager (Singleton)                                   │
│   - [responsibility 1]                                          │
│   - [responsibility 2]                                          │
│            │                                                    │
│            │ events                                             │
│            ▼                                                    │
│   UI_[System]Controller                                         │
│   - [responsibility]                                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Example Assets

The `[ExampleFolder]` folder contains ready-to-use examples:

```
[ExampleFolder]/
├── Scripts/
│   └── [Script files]
└── ScriptableObjects/
    └── [Asset files]
```

---

## Best Practices

1. **[Practice 1]** - [Explanation]
2. **[Practice 2]** - [Explanation]
3. **[Practice 3]** - [Explanation]

---

## Related Documentation

- [Related Topic 1](link.md) - Brief description
- [Related Topic 2](link.md) - Brief description
