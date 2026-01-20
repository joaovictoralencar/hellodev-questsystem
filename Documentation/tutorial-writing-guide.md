# Tutorial Writing Guide

*A style guide for creating user-friendly documentation*

---

## Purpose

This guide defines the structure and best practices for writing tutorials in the HelloDev framework. Following these guidelines ensures consistency and a positive experience for users of all skill levels.

---

## Document Structure

Every tutorial MUST include these sections in order:

### 1. What You'll Build (Required)

**Purpose:** Set expectations and motivate the reader.

**Must Include:**
- Brief list of what the end result will do (3-5 bullet points)
- Visual preview (ASCII art, screenshot reference, or diagram)

**Example:**
```markdown
## What You'll Build

By the end of this guide, you'll have:

- A working inventory system with drag-and-drop
- Item stacking and splitting functionality
- Save/load integration

**Preview:**
┌─────────────────────────────┐
│ [Sword]  [Shield]  [Potion] │
│ [Empty]  [Empty]   [Gold]   │
└─────────────────────────────┘
```

**Why:** Users need to know if this tutorial solves their problem before investing time.

---

### 2. Prerequisites (Required)

**Purpose:** Ensure users can actually complete the tutorial.

**Must Include:**
- Required packages (with verification steps)
- Optional packages (with what features they enable)
- Unity version requirements
- Any project setup needed

**Template:**
```markdown
## Prerequisites

### Required Packages

| Package | Purpose | How to Verify |
|---------|---------|---------------|
| **Package Name** | What it's used for | Steps to check it's installed |

### Optional Packages

| Package | Purpose | Needed For |
|---------|---------|------------|
| **Package Name** | What it enables | Which features require it |

### Unity Version

- Unity X.X or newer

### Project Setup

- Any existing requirements (e.g., "A scene with a player character")
```

**Why:** Nothing frustrates users more than getting stuck because they're missing something.

---

### 3. Glossary (Required for Complex Systems)

**Purpose:** Explain naming conventions and terminology.

**Must Include:**
- Suffix/prefix meanings (e.g., `_SO`, `Runtime`, `Manager`)
- How components connect (simple diagram)

**Template:**
```markdown
## Glossary

| Term | Meaning |
|------|---------|
| `_SO` suffix | **ScriptableObject** - Data asset in Project folder |
| `Runtime` suffix | **Runtime Instance** - Live object during Play mode |

**How they connect:**

Data_SO (asset)
    ↓ creates
DataRuntime (live instance)
    ↓ managed by
DataManager (singleton)
```

**Why:** Users unfamiliar with your codebase need a decoder ring for your naming conventions.

---

### 4. Quick Start (Recommended)

**Purpose:** Let users verify the system works before full setup.

**Must Include:**
- Minimal viable setup (under 10 minutes)
- Test script or method to trigger functionality
- Expected output to verify success

**Guidelines:**
- Skip all optional features
- Use hardcoded values instead of proper configuration
- Focus only on "does it work?"

**Template:**
```markdown
## Quick Start (Minimal Setup)

### Step 1: Create Manager (X minutes)
[Minimal steps]

### Step 2: Create Test Data (X minutes)
[Minimal steps]

### Step 3: Create Test Script (X minutes)
[Code block with simple trigger]

### Step 4: Test It
1. Enter Play Mode
2. [Trigger action]
3. Check Console for:
   ```
   [Expected output]
   ```

**It works!** Continue to Full Setup for proper implementation.
```

**Why:** Users gain confidence when they see something work early. It also helps diagnose issues - if Quick Start fails, the problem is foundational.

---

### 5. Full Setup Guide (Required)

**Purpose:** Complete walkthrough with proper practices.

**Must Include:**
- Numbered parts for major sections
- Numbered steps within each part
- Checkpoints after each part
- Tables for configuration with "Why" column

**Structure:**
```markdown
## Full Setup Guide

### Part 1: [Section Name]

#### Step 1.1: [Action]

1. [Specific instruction]
2. [Specific instruction]

| Field | Value | Why |
|-------|-------|-----|
| **Field Name** | `value` | Explanation of why this value |

#### Checkpoint: Verify [Part Name]

1. [Verification step]
2. Expected result: [what they should see]

---

### Part 2: [Next Section]
...
```

**Guidelines for Steps:**
- One action per numbered item
- Use exact UI paths: **Hierarchy** → Right-click → **Create Empty**
- Use exact field names in **bold**
- Include "Why" explanations for non-obvious settings
- Show expected hierarchy/folder structure after complex sections

---

### 6. Checkpoints (Required - Embedded in Full Setup)

**Purpose:** Confirm progress and catch errors early.

**When to Add Checkpoints:**
- After creating the manager/core component
- After creating data assets
- After building UI
- After connecting components
- Before any "point of no return"

**Template:**
```markdown
#### Checkpoint: Verify [What Was Done]

1. [Action to verify]
2. Expected result:
   ```
   [Console output or visual description]
   ```

If you see [error], check [common cause].
```

**Why:** Users shouldn't complete 100% of the tutorial before discovering step 3 was wrong.

---

### 7. Optional Enhancements (Recommended)

**Purpose:** Separate "nice to have" from "must have."

**Must Clarify:**
- What happens if skipped (system still works? degraded experience?)
- Dependencies between enhancements

**Template:**
```markdown
## Optional Enhancements

These additions improve the experience but aren't required for basic functionality.

### Enhancement 1: [Feature Name]

[What it adds]

1. [Steps]
2. [Steps]

**Without this:** [What the user experience is like without it]
```

**Why:** Users should know the minimum viable path vs. the polished path.

---

### 8. Advanced Topics (Optional)

**Purpose:** Cover complex features without overwhelming beginners.

**Candidates for Advanced Section:**
- Localization/internationalization
- Complex conditions or logic
- Custom extensions
- Performance optimization
- Integration with other systems

**Template:**
```markdown
## [Feature Name] (Advanced)

This section covers [feature]. Skip this if you don't need [use case].

### When to Use This

- [Scenario 1]
- [Scenario 2]

### Setup Steps

[Detailed walkthrough]
```

**Why:** Beginners shouldn't feel overwhelmed, but advanced users shouldn't feel limited.

---

### 9. API Reference (Required for Programmers)

**Purpose:** Quick reference for common operations.

**Must Include:**
- Most common methods with brief comments
- Event subscription patterns
- Save/load integration (if applicable)

**Template:**
```markdown
## API Reference

### [Manager] Methods

```csharp
// Brief description
ReturnType result = Manager.Instance.Method(params);

// Another common operation
Manager.Instance.AnotherMethod();
```

### Events

```csharp
Manager.Instance.OnSomething.AddListener(Handler);

void Handler(ParamType param)
{
    // What to do here
}
```
```

**Why:** Programmers want copy-paste snippets, not prose.

---

### 10. Troubleshooting (Required)

**Purpose:** Self-service problem resolution.

**Must Include:**
- Table format: Symptom | Cause | Solution
- Sections for different problem categories
- Debugging tips

**Template:**
```markdown
## Troubleshooting

### [Category] Problems

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| [What user sees] | [Why it happens] | [How to fix] |

### Debugging Tips

1. **Enable logging**: [How to enable]
2. **Check Console**: [What prefix to look for]
3. **Verify references**: [What to check in Inspector]
```

**Why:** Users will have problems. Good troubleshooting reduces support burden and user frustration.

---

### 11. Architecture Reference (Recommended for Complex Systems)

**Purpose:** Help users understand the big picture.

**Must Include:**
- ASCII diagram showing relationships
- Brief explanation of each layer

**Template:**
```markdown
## Architecture Reference

```
┌─────────────────────────────────┐
│        SCRIPTABLEOBJECTS        │
│      (Assets in Project)        │
├─────────────────────────────────┤
│   Config_SO                     │
│   - field1                      │
│   - field2                      │
└─────────────────────────────────┘
              │
              ↓ creates
┌─────────────────────────────────┐
│       RUNTIME INSTANCES         │
├─────────────────────────────────┤
│   ConfigRuntime                 │
│   - mutableState                │
└─────────────────────────────────┘
              │
              ↓ managed by
┌─────────────────────────────────┐
│       SCENE COMPONENTS          │
├─────────────────────────────────┤
│   ConfigManager (Singleton)     │
│   UI_ConfigController           │
└─────────────────────────────────┘
```
```

**Why:** Understanding architecture helps users extend and debug the system.

---

### 12. Related Documentation (Required)

**Purpose:** Guide users to next steps.

**Template:**
```markdown
## Related Documentation

- [Related Topic 1](link.md) - Brief description
- [Related Topic 2](link.md) - Brief description
```

---

## Writing Style Guidelines

### Use Tables for Configuration

**Bad:**
> Set the Duration field to 3. This controls how long the step shows.

**Good:**

| Field | Value | Why |
|-------|-------|-----|
| **Duration** | `3` | Time in seconds before auto-advance |

### Use Exact UI Paths

**Bad:**
> Create a new empty game object and add the manager component.

**Good:**
> 1. In **Hierarchy**, right-click → **Create Empty**
> 2. Name it: `TutorialManager`
> 3. **Add Component** → search `TutorialManager` → add it

### Explain the "Why"

**Bad:**
> Set PlayOnce to true.

**Good:**
> | **Play Once** | ✓ checked | Tutorial won't replay after completion - players hate repeating tutorials |

### Show Expected Output

**Bad:**
> The tutorial should start.

**Good:**
> Check Console for:
> ```
> [Tutorial] TutorialManager initialized with 1 tutorials.
> [Tutorial] Step started: Welcome Step
> ```

### Use Visual Hierarchy References

After creating complex structures, show what the user should have:

```markdown
Your Hierarchy should look like:
```
Canvas
├── TutorialPanel
│   ├── InstructionText
│   └── ContinueButton
└── EventSystem
```
```

### Mark Optional vs Required Clearly

**In section headers:**
- `(Required)` - Must complete
- `(Recommended)` - Should complete for good experience
- `(Optional)` - Can skip entirely
- `(Advanced)` - For experienced users only

**In reference tables:**
- Bold field names that are required
- Add "(optional)" suffix to optional fields

---

## Anti-Patterns to Avoid

### 1. Assuming Knowledge

**Bad:** "Configure the condition to listen for your jump event."

**Good:** Full walkthrough of creating the event, the condition, and connecting them.

### 2. Inconsistent Detail Levels

**Bad:** Detailed Rect Transform values for one element, then "position the button appropriately" for another.

**Good:** Same level of detail throughout, or explicitly state "use similar settings as above."

### 3. Testing at the End Only

**Bad:** 47 steps, then "Enter Play Mode to test."

**Good:** Checkpoints every 5-10 steps with expected results.

### 4. Mixing Required and Optional

**Bad:** Steps 1-10 where steps 4 and 7 are optional but not marked.

**Good:** Clear separation: "Full Setup" → "Optional Enhancements" → "Advanced"

### 5. Missing Error Recovery

**Bad:** Just steps, no troubleshooting.

**Good:** Troubleshooting section with common errors and solutions.

### 6. Wall of Text

**Bad:** Paragraphs explaining what to click.

**Good:** Numbered steps, tables, code blocks, visual hierarchy.

### 7. Outdated Screenshots

**Bad:** Screenshots from Unity 2019 in a 2024 tutorial.

**Good:** ASCII diagrams (never go out of date) or text descriptions with exact paths.

---

## Checklist Before Publishing

Use this checklist before finalizing any tutorial:

### Structure
- [ ] Has "What You'll Build" with preview
- [ ] Has Prerequisites with verification steps
- [ ] Has Glossary (if system has custom terminology)
- [ ] Has Quick Start (if setup > 15 minutes)
- [ ] Has Full Setup with numbered parts/steps
- [ ] Has Checkpoints after each major part
- [ ] Has Troubleshooting section
- [ ] Has Related Documentation links

### Content Quality
- [ ] Every configuration has a "Why" explanation
- [ ] All UI paths use exact menu names in bold
- [ ] Expected output shown for all checkpoints
- [ ] Optional features clearly marked
- [ ] No assumed knowledge without explanation

### User Experience
- [ ] Can be completed by someone new to Unity
- [ ] Can be completed by someone new to this framework
- [ ] First success moment within 10 minutes (Quick Start)
- [ ] No step requires more than 5 sub-actions
- [ ] Errors are recoverable with Troubleshooting section

### Technical Accuracy
- [ ] All code snippets compile
- [ ] All menu paths are correct
- [ ] All field names match actual Inspector names
- [ ] All expected outputs match actual system behavior

---

## Template Document

A blank template following this guide is available at:
[tutorial-template.md](tutorial-template.md)

Copy this template when starting a new tutorial.
