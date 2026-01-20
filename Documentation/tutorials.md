# Tutorial System

*Last Updated: 2026-01-18*

---

## What You'll Build

By the end of this guide, you'll have a fully functional in-game tutorial system that:

- Displays a welcome message that auto-dismisses after 3 seconds
- Shows movement instructions with a "Continue" button
- Tracks completion so tutorials don't repeat
- Can be triggered when the player enters an area

**Final Result Preview:**

```
┌─────────────────────────────────────────────────────────────┐
│ [■■■■■■■■■■░░░░░░░░░░]                              1/2     │
│                                                             │
│           Use WASD keys to move around                      │
│                                                             │
│  [Skip]                                      [Continue]     │
└─────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

Before starting, ensure you have:

### Required Packages

| Package | Purpose | How to Verify |
|---------|---------|---------------|
| **Quest System** | The tutorial system is part of this package | Check `Assets/com.hellodev.questsystem` exists |
| **TextMeshPro** | UI text rendering | Window > TextMeshPro > Import TMP Essential Resources |

### Optional Packages

| Package | Purpose | Needed For |
|---------|---------|------------|
| **Localization** | Multi-language support | Localized tutorial text (skip for now) |
| **Odin Inspector** | Enhanced Unity inspector | Better editor experience (not required) |

### Unity Version

- Unity 2021.3 or newer recommended

### Project Setup

- A scene with a player character (for trigger-based tutorials)
- Basic UI Canvas knowledge helpful but not required

---

## Glossary

Understanding the naming conventions will help you navigate the system:

| Term | Meaning |
|------|---------|
| `_SO` suffix | **ScriptableObject** - A data asset that lives in your Project folder. Contains configuration, not runtime state. |
| `Runtime` suffix | **Runtime Instance** - A live object created during gameplay from a ScriptableObject. Contains mutable state. |
| `Manager` | **Singleton Controller** - One instance in the scene that orchestrates everything. |
| `UI_` prefix | **UI Component** - A MonoBehaviour that controls user interface elements. |

**How they connect:**

```
Tutorial_SO (asset in Project)
    ↓ creates
TutorialRuntime (live instance during Play mode)
    ↓ managed by
TutorialManager (singleton in scene)
    ↓ sends events to
UI_TutorialController (updates the UI)
```

---

## Quick Start (Minimal Setup)

Want to test the system quickly? Here's the minimum viable setup:

### Step 1: Create TutorialManager (2 minutes)

1. **Hierarchy** → Right-click → **Create Empty**
2. Name it: `TutorialManager`
3. **Add Component** → search `TutorialManager` → add it

### Step 2: Create One Tutorial Step (2 minutes)

1. **Project** → Right-click → **Create > HelloDev > Quest System > Tutorials > Tutorial Step**
2. Name it: `SO_Step_Test`
3. In Inspector, set:
   - **Dev Name**: `Test Step`
   - **Is Timed Step**: ✓ checked
   - **Duration**: `3`

### Step 3: Create One Tutorial (2 minutes)

1. **Project** → Right-click → **Create > HelloDev > Quest System > Tutorials > Tutorial**
2. Name it: `SO_Tutorial_Test`
3. In Inspector:
   - **Dev Name**: `Test Tutorial`
   - **Steps**: Click **+**, drag `SO_Step_Test` into the slot

### Step 4: Add Test Script (1 minute)

**Option A: Use the provided script**
1. In **Project**, navigate to `com.hellodev.questsystem/BasicTutorialExample/Scripts/`
2. Find `TutorialTestStarter.cs`
3. Create empty GameObject in Hierarchy, drag the script onto it
4. Assign `SO_Tutorial_Test` to the **Tutorial** field

**Option B: Create your own**
1. **Project** → Right-click → **Create > C# Script**
2. Name it: `TutorialTestStarter`
3. Replace contents with:

```csharp
using HelloDev.QuestSystem.Tutorials;
using UnityEngine;

public class TutorialTestStarter : MonoBehaviour
{
    [SerializeField] private Tutorial_SO tutorial;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Starting tutorial...");
            TutorialManager.Instance.StartTutorial(tutorial);
        }
    }
}
```

4. Create empty GameObject, add the script, assign `SO_Tutorial_Test`

### Step 5: Test It

1. Select `TutorialManager` GameObject
2. Add `SO_Tutorial_Test` to the **Tutorial Database** list
3. **Enter Play Mode**
4. Press **T**
5. Check Console for:
   ```
   Starting tutorial...
   [Tutorial] TutorialManager initialized with 1 tutorials.
   [Tutorial] Step started: Test Step
   [Tutorial] Step completed: Test Step
   [Tutorial] Tutorial completed: Test Tutorial
   ```

**It works!** Now continue to the full guide to add UI and more features.

---

## Full Setup Guide

### Part 1: Scene Setup

#### Step 1.1: Create the TutorialManager GameObject

1. In **Hierarchy**, right-click → **Create Empty**
2. Name it: `TutorialManager`
3. Click **Add Component** in Inspector
4. Search for `TutorialManager` and add it

#### Step 1.2: Configure TutorialManager Settings

| Field | Recommended Value | Why |
|-------|-------------------|-----|
| **Tutorial Database** | (leave empty for now) | We'll add tutorials after creating them |
| **Enable Debug Logging** | ✓ checked | Shows helpful messages in Console during development |
| **Allow Tutorial Queue** | ✓ checked | If player triggers multiple tutorials, they play in sequence instead of being ignored |

#### Checkpoint: Verify Setup

1. Enter **Play Mode**
2. Check Console for: `[Tutorial] TutorialManager initialized with 0 tutorials.`
3. Exit Play Mode

If you see this message, the TutorialManager is working correctly.

---

### Part 2: Create Tutorial Step Assets

Tutorial steps are the individual instructions players see. Each step is a ScriptableObject asset.

#### Step 2.1: Create a Folder for Organization

1. In **Project** window, navigate to where you want to store assets (e.g., `Assets/ScriptableObjects/`)
2. Right-click → **Create > Folder**
3. Name it: `Tutorials`

**Why organize?** You'll create multiple assets. Keeping them in one folder makes them easy to find and manage.

#### Step 2.2: Create the Welcome Step (Timed Auto-Advance)

This step shows a welcome message and automatically advances after 3 seconds.

1. Right-click in `Tutorials` folder
2. **Create > HelloDev > Quest System > Tutorials > Tutorial Step**
3. Name it: `SO_TutorialStep_Welcome`
4. Configure in Inspector:

| Field | Value | Why |
|-------|-------|-----|
| **Dev Name** | `Welcome Step` | This name appears in logs and editor - make it descriptive |
| **Is Timed Step** | ✓ checked | Step will auto-complete after duration |
| **Duration** | `3` | 3 seconds is enough to read a short message |
| **Can Skip** | ✓ checked | Lets impatient players skip |

**Leave these empty for now:**
- **Instruction** - We'll use the fallback text system (simpler than Localization)
- **Completion Condition** - Not needed for timed steps
- **Step Icon** - Optional visual flair

#### Step 2.3: Create the Movement Step (Manual Continue)

This step waits for the player to click "Continue".

1. Right-click in `Tutorials` folder
2. **Create > HelloDev > Quest System > Tutorials > Tutorial Step**
3. Name it: `SO_TutorialStep_Movement`
4. Configure:

| Field | Value | Why |
|-------|-------|-----|
| **Dev Name** | `Movement Step` | Descriptive name for editor |
| **Is Timed Step** | ☐ unchecked | Player controls when to advance |
| **Can Skip** | ✓ checked | Respect player's time |

**How does it advance without a timer?** When a step has no timer AND no condition, the UI shows a "Continue" button automatically.

#### Checkpoint: Verify Assets Created

In your `Tutorials` folder, you should now have:
- `SO_TutorialStep_Welcome`
- `SO_TutorialStep_Movement`

Both should show a GUID in the **Step Id** field (auto-generated, don't modify).

---

### Part 3: Create the Tutorial Asset

The Tutorial asset is a container that holds steps in order.

#### Step 3.1: Create the Tutorial ScriptableObject

1. Right-click in `Tutorials` folder
2. **Create > HelloDev > Quest System > Tutorials > Tutorial**
3. Name it: `SO_Tutorial_BasicMovement`

#### Step 3.2: Configure the Tutorial

| Field | Value | Why |
|-------|-------|-----|
| **Dev Name** | `Basic Movement Tutorial` | Shown in logs and editor |
| **Play Once** | ✓ checked | Tutorial won't replay after completion - players hate repeating tutorials |
| **Can Skip** | ✓ checked | Allows skipping the entire tutorial |
| **Priority** | `0` | Higher numbers play first when multiple tutorials are queued. 0 is fine for now. |

**Leave these empty for now:**
- **Display Name** - Requires Localization setup
- **Tutorial Description** - Requires Localization setup

#### Step 3.3: Add Steps to the Tutorial

1. Find the **Steps** list in Inspector
2. Click **+** to add an element
3. Drag `SO_TutorialStep_Welcome` from Project into **Element 0**
4. Click **+** again
5. Drag `SO_TutorialStep_Movement` into **Element 1**

**Important:** The order in this list IS the playback order. Element 0 plays first.

Your Steps list should look like:
```
Steps
├── Element 0: SO_TutorialStep_Welcome
└── Element 1: SO_TutorialStep_Movement
```

#### Step 3.4: Register Tutorial in TutorialManager

1. Select `TutorialManager` in Hierarchy
2. Find **Tutorial Database** list in Inspector
3. Click **+** to add an element
4. Drag `SO_Tutorial_BasicMovement` into the slot

**Why register?** The TutorialManager needs to know about your tutorials to:
- Start them by ID (for save/load)
- Check if they're already completed
- Manage the queue

#### Checkpoint: Test Without UI

1. Make sure `TutorialTestStarter` script is in scene (from Quick Start)
2. Assign `SO_Tutorial_BasicMovement` to its Tutorial field
3. Enter **Play Mode**
4. Press **T**
5. Watch Console:

```
[Tutorial] TutorialManager initialized with 1 tutorials.
[Tutorial] Step started: Welcome Step
(after 3 seconds)
[Tutorial] Step completed: Welcome Step
[Tutorial] Step started: Movement Step
(stays here - waiting for manual completion)
```

The tutorial is working! It just has no UI yet. Press **T** won't advance because that only starts tutorials - we need UI to complete manual steps.

---

### Part 4: Create the Tutorial UI

Now we'll build the visual interface players see.

#### Step 4.1: Create the Canvas

1. In **Hierarchy**, right-click → **UI > Canvas**
2. Unity creates Canvas and EventSystem (keep both)
3. Select **Canvas**, configure in Inspector:

| Component | Field | Value | Why |
|-----------|-------|-------|-----|
| Canvas | Render Mode | `Screen Space - Overlay` | UI renders on top of everything |
| Canvas Scaler | UI Scale Mode | `Scale With Screen Size` | UI scales with resolution |
| Canvas Scaler | Reference Resolution | `1920 x 1080` | Design for HD, scales to other sizes |

#### Step 4.2: Create the Tutorial Panel

The panel is the background container for all tutorial UI elements.

1. Right-click on **Canvas** → **UI > Panel**
2. Rename to: `TutorialPanel`
3. Configure **Rect Transform**:

| Field | Value | Why |
|-------|-------|-----|
| **Anchor** | Bottom-center (click anchor icon, hold Alt, click bottom-center) | Panel appears at bottom of screen |
| **Pos X** | `0` | Centered horizontally |
| **Pos Y** | `100` | 100 pixels from bottom edge - adjust for your game |
| **Width** | `600` | Wide enough for instruction text |
| **Height** | `150` | Tall enough for text + buttons |

4. Configure **Image** component:

| Field | Value | Why |
|-------|-------|-----|
| **Color** | `(0, 0, 0, 200)` or RGBA `#000000C8` | Semi-transparent black background |

**Visual reference - your panel position:**
```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                      (game view)                            │
│                                                             │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                   [TutorialPanel]                           │  ← 100px from bottom
└─────────────────────────────────────────────────────────────┘
```

#### Step 4.3: Create Instruction Text

This displays the tutorial instructions to the player.

1. Right-click on **TutorialPanel** → **UI > Text - TextMeshPro**
2. If prompted, click **Import TMP Essentials** (one-time setup)
3. Rename to: `InstructionText`
4. Configure **Rect Transform**:

| Field | Value | Why |
|-------|-------|-----|
| **Anchor** | Stretch-stretch (hold Alt, click the stretch option) | Text fills available space |
| **Left** | `20` | Padding from left edge |
| **Right** | `20` | Padding from right edge |
| **Top** | `20` | Padding from top |
| **Bottom** | `60` | Leave room for buttons at bottom |

5. Configure **TextMeshPro - Text (UI)**:

| Field | Value |
|-------|-------|
| **Text** | `Tutorial instruction...` (placeholder) |
| **Font Size** | `24` |
| **Alignment** | Center + Middle (click both center icons) |
| **Color** | White |

#### Step 4.4: Create the Continue Button

Players click this to advance manual steps.

1. Right-click on **TutorialPanel** → **UI > Button - TextMeshPro**
2. Rename to: `ContinueButton`
3. Configure **Rect Transform**:

| Field | Value | Why |
|-------|-------|-----|
| **Anchor** | Bottom-right (hold Alt, click bottom-right) | Button in bottom-right corner |
| **Pos X** | `-80` | 80 pixels from right edge |
| **Pos Y** | `30` | 30 pixels from bottom |
| **Width** | `120` | Standard button width |
| **Height** | `40` | Standard button height |

4. Expand `ContinueButton` in Hierarchy, select child **Text (TMP)**
5. Set **Text** to: `Continue`

#### Step 4.5: Create the Skip Button

Players click this to skip the current step or entire tutorial.

1. Right-click on **TutorialPanel** → **UI > Button - TextMeshPro**
2. Rename to: `SkipButton`
3. Configure **Rect Transform**:

| Field | Value |
|-------|-------|
| **Anchor** | Bottom-left (hold Alt, click) |
| **Pos X** | `80` |
| **Pos Y** | `30` |
| **Width** | `120` |
| **Height** | `40` |

4. Expand, select **Text (TMP)**, set text to: `Skip`

#### Checkpoint: Verify UI Layout

Your Hierarchy should look like:
```
Canvas
├── TutorialPanel
│   ├── InstructionText
│   ├── ContinueButton
│   │   └── Text (TMP)
│   └── SkipButton
│       └── Text (TMP)
└── EventSystem
```

Enter **Play Mode** briefly - you should see the panel at the bottom of the screen. Exit Play Mode.

---

### Part 5: Connect UI to Tutorial System

Now we connect the UI elements to the TutorialManager using the UI_TutorialController component.

#### Step 5.1: Create UI Controller GameObject

1. Right-click on **Canvas** → **Create Empty**
2. Rename to: `TutorialUIController`
3. **Add Component** → search `UI_TutorialController` → add it

#### Step 5.2: Assign Required References

These references are **required** for basic functionality:

| Field | What to Drag | From Where |
|-------|--------------|------------|
| **Tutorial Panel** | `TutorialPanel` | Hierarchy |
| **Instruction Text Fallback** | `InstructionText` | Hierarchy (under TutorialPanel) |
| **Skip Button** | `SkipButton` | Hierarchy |
| **Continue Button** | `ContinueButton` | Hierarchy |

**What is "Instruction Text Fallback"?** When you don't use the Localization system, the controller displays the step's Dev Name in this text field instead. It's a simple way to show text without setting up Localization.

#### Step 5.3: Assign Optional References

These add extra features but the tutorial works without them:

| Field | What to Drag | What Happens If Empty |
|-------|--------------|----------------------|
| **Panel Canvas Group** | (none for now) | No fade animations |
| **Instruction Text** | (none for now) | Uses fallback text instead |
| **Step Icon** | (none for now) | No icons shown |
| **Step Counter Text** | (none for now) | No "1/3" counter shown |
| **Progress Bar** | (none for now) | No progress bar shown |

#### Step 5.4: Configure Settings

| Field | Value | Why |
|-------|-------|-----|
| **Hide On Complete** | ✓ checked | Panel hides automatically when tutorial ends |

#### Checkpoint: Test Complete Flow

1. Enter **Play Mode**
2. Press **T** to start tutorial
3. Observe:
   - Panel appears
   - "Welcome Step" shows (from Dev Name)
   - After 3 seconds, changes to "Movement Step"
   - Continue button appears
   - Click Continue
   - Panel hides
4. Check Console shows completion messages

**Congratulations!** You have a working tutorial system.

---

### Part 6: Add a Tutorial Trigger (Optional Enhancement)

Instead of pressing T, start tutorials when players walk into an area.

#### Step 6.1: Create the Trigger GameObject

1. In **Hierarchy**, right-click → **Create Empty**
2. Rename to: `TutorialTrigger_Movement`
3. Position it where you want the tutorial to start (use Transform position)

#### Step 6.2: Add Collider Component

1. Select `TutorialTrigger_Movement`
2. **Add Component** → **Box Collider**
3. Configure:

| Field | Value | Why |
|-------|-------|-----|
| **Is Trigger** | ✓ checked | Detects overlap without physics collision |
| **Size** | `(5, 3, 5)` | Adjust based on your game scale |

**Tip:** The trigger shows as a green wireframe in Scene view when selected.

#### Step 6.3: Add TutorialTrigger Component

1. **Add Component** → search `TutorialTrigger` → add it
2. Configure:

| Field | Value | Why |
|-------|-------|-----|
| **Tutorial** | Drag `SO_Tutorial_BasicMovement` | Which tutorial to start |
| **Player Tag** | `Player` | Only triggers for objects with this tag |
| **Trigger Once** | ✓ checked | Won't re-trigger if player walks in again |
| **Disable After Trigger** | ✓ checked | Deactivates GameObject after triggering (performance) |
| **Log Trigger Events** | ✓ checked | Shows trigger events in Console (disable for release) |

#### Step 6.4: Tag Your Player

1. Select your **Player** GameObject in Hierarchy
2. In Inspector, click the **Tag** dropdown (shows "Untagged" by default)
3. Select `Player`
4. If `Player` tag doesn't exist: click **Add Tag...** → click **+** → type `Player` → Save → go back and select it

#### Checkpoint: Test Trigger

1. Position `TutorialTrigger_Movement` near your player's spawn point
2. Enter **Play Mode**
3. Move player into the trigger zone
4. Tutorial should start automatically
5. Console shows: `[TutorialTrigger] Started tutorial 'Basic Movement Tutorial'.`

---

### Part 7: Add Optional UI Enhancements

These additions improve the player experience but aren't required.

#### Enhancement 7.1: Step Counter ("1/3")

Shows players their progress through the tutorial.

1. Right-click on **TutorialPanel** → **UI > Text - TextMeshPro**
2. Rename to: `StepCounterText`
3. Configure **Rect Transform**:

| Field | Value |
|-------|-------|
| **Anchor** | Top-right |
| **Pos X** | `-40` |
| **Pos Y** | `-20` |
| **Width** | `60` |
| **Height** | `30` |

4. Configure text: Font Size `18`, Alignment: Right, Text: `1/3`
5. Select `TutorialUIController`, drag `StepCounterText` to **Step Counter Text** field

#### Enhancement 7.2: Progress Bar

Visual progress indicator.

1. Right-click on **TutorialPanel** → **UI > Slider**
2. Rename to: `ProgressBar`
3. Configure **Rect Transform**:

| Field | Value |
|-------|-------|
| **Anchor** | Top-stretch |
| **Pos Y** | `-10` |
| **Left** | `20` |
| **Right** | `20` |
| **Height** | `10` |

4. Configure **Slider** component:
   - **Interactable**: ☐ unchecked (display only)
   - **Value**: `0`

5. Delete **Handle Slide Area** child (not needed)
6. Select `TutorialUIController`, drag `ProgressBar` to **Progress Bar** field

---

## Adding Localization (Advanced)

If you want multi-language support, follow these additional steps.

### Setup Localization Package

1. **Window > Package Manager**
2. Search for `Localization`
3. Click **Install**

### Create Localization Tables

1. **Window > Asset Management > Localization Tables**
2. Create a new **String Table Collection**
3. Name it: `TutorialStrings`
4. Add your languages (e.g., English, Spanish)

### Configure Tutorial Steps with Localization

1. Select a TutorialStep_SO (e.g., `SO_TutorialStep_Welcome`)
2. Find the **Instruction** field
3. Click the **●** button next to it
4. Select **Create Table Entry**
5. Choose your `TutorialStrings` table
6. Enter the key name (e.g., `tutorial_welcome_instruction`)
7. In the Localization Tables window, enter translations for each language

### Connect Localized Text to UI

1. Select `InstructionText` GameObject
2. **Add Component** → **Localize String Event**
3. Select `TutorialUIController`
4. Drag the `InstructionText` GameObject to the **Instruction Text** field (not Fallback)

Now the UI will display localized text when available, falling back to Dev Name otherwise.

---

## Condition-Based Steps (Advanced)

For steps that complete when the player performs an action (like jumping).

### Step 1: Create a Game Event

1. **Create > HelloDev > Events > Game Event**
2. Name it: `SO_Event_PlayerJumped`

### Step 2: Raise Event in Your Code

In your player controller script:

```csharp
using HelloDev.Events;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameEvent onPlayerJumped;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Your jump logic here...

            // Raise the event so the tutorial knows
            onPlayerJumped.Raise();
        }
    }
}
```

### Step 3: Create a Condition

1. **Create > HelloDev > Conditions > Condition Event Driven**
2. Name it: `SO_Condition_PlayerJumped`
3. Assign `SO_Event_PlayerJumped` to the event field

### Step 4: Configure Tutorial Step

1. Select your jump tutorial step
2. Set **Is Timed Step**: ☐ unchecked
3. Drag `SO_Condition_PlayerJumped` to **Completion Condition**

Now the step auto-completes when the player jumps!

---

## Save System Setup (Advanced)

If you want tutorial progress to persist between play sessions, set up the TutorialSaveManager.

### Prerequisites

Before setting up tutorial saving, you need:
- A configured `SaveService.Provider` (see `com.hellodev.saving` package documentation)
- The TutorialManager already set up in your scene

### Step 1: Create the TutorialSaveLocator Asset

The locator is a ScriptableObject that provides decoupled access to the save manager.

1. In **Project** window, navigate to your ScriptableObjects folder
2. Right-click → **Create > HelloDev > Locators > Tutorial Save Locator**
3. Name it: `TutorialSaveLocator`

**Why a locator?** Any script can reference this asset to save/load tutorials without needing a direct reference to the manager GameObject.

### Step 2: Create the TutorialSaveManager GameObject

1. In **Hierarchy**, right-click → **Create Empty**
2. Name it: `TutorialSaveManager`
3. **Add Component** → search `TutorialSaveManager` → add it

### Step 3: Configure TutorialSaveManager

| Field | What to Assign | Why |
|-------|----------------|-----|
| **Locator** | Drag `TutorialSaveLocator` asset | Registers manager with locator for decoupled access |
| **Tutorial Manager** | Drag `TutorialManager` GameObject | The manager whose state will be saved/loaded |
| **Persistent** | ✓ checked (default) | Survives scene loads via DontDestroyOnLoad |
| **Self Initialize** | ✓ checked (default) | Set to ☐ unchecked if using GameBootstrap |

### Step 4: Test Save/Load

1. Enter **Play Mode**
2. Start a tutorial and complete a few steps
3. In Inspector, find `TutorialSaveManager`
4. Click **Quick Save (tutorial_debug_save)** button
5. Stop and restart Play Mode
6. Click **Quick Load (tutorial_debug_save)** button
7. Verify tutorial state is restored

### Step 5: Integrate with Your Game's Save System

Reference the locator in your game's save/load code:

```csharp
using HelloDev.QuestSystem.Tutorials;
using UnityEngine;

public class GameSaveController : MonoBehaviour
{
    [SerializeField] private TutorialSaveLocator_SO tutorialSaveLocator;

    public async void SaveGame(string slotName)
    {
        // Save other game data...

        // Save tutorial progress
        string tutorialSlot = $"{slotName}_tutorials";
        await tutorialSaveLocator.SaveAsync(tutorialSlot);
    }

    public async void LoadGame(string slotName)
    {
        // Load other game data...

        // Load tutorial progress
        string tutorialSlot = $"{slotName}_tutorials";
        await tutorialSaveLocator.LoadAsync(tutorialSlot);
    }
}
```

### Using with GameBootstrap (Optional)

If you're using the GameBootstrap system for coordinated initialization:

1. Select `TutorialManager` GameObject
2. Set **Self Initialize**: ☐ unchecked
3. Select `TutorialSaveManager` GameObject
4. Set **Self Initialize**: ☐ unchecked
5. GameBootstrap will initialize them in priority order:
   - TutorialManager: Priority 105
   - TutorialSaveManager: Priority 205

---

## API Reference

### TutorialManager Methods

```csharp
// Start a tutorial
TutorialRuntime runtime = TutorialManager.Instance.StartTutorial(myTutorial_SO);
TutorialRuntime runtime = TutorialManager.Instance.StartTutorial(tutorialGuid);

// Control playback
TutorialManager.Instance.CompleteCurrentStep();  // Advance to next step
TutorialManager.Instance.SkipCurrentStep();      // Skip if allowed
TutorialManager.Instance.SkipCurrentTutorial();  // Skip entire tutorial if allowed

// Query state
bool active = TutorialManager.Instance.IsTutorialActive;
TutorialRuntime current = TutorialManager.Instance.CurrentTutorial;
bool done = TutorialManager.Instance.IsTutorialCompleted(tutorialGuid);

// Reset (useful for testing)
TutorialManager.Instance.ResetAllProgress();
```

### TutorialManager Events

```csharp
// Subscribe to events
TutorialManager.Instance.OnTutorialStarted.AddListener(OnTutorialStarted);
TutorialManager.Instance.OnTutorialCompleted.AddListener(OnTutorialCompleted);
TutorialManager.Instance.OnStepStarted.AddListener(OnStepStarted);
TutorialManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);

void OnStepStarted(TutorialRuntime tutorial, TutorialStepRuntime step)
{
    Debug.Log($"Now showing: {step.DevName}");
}
```

### Save/Load Integration

The tutorial system has its own save/load support, independent from the quest save system. For setup instructions, see [Save System Setup (Advanced)](#save-system-setup-advanced).

#### TutorialSaveLocator Methods

```csharp
[SerializeField] private TutorialSaveLocator_SO tutorialSaveLocator;

// Save/Load
await tutorialSaveLocator.SaveAsync("slot_name");
await tutorialSaveLocator.LoadAsync("slot_name");

// Query and delete
bool exists = await tutorialSaveLocator.SaveExistsAsync("slot_name");
await tutorialSaveLocator.DeleteSaveAsync("slot_name");

// Snapshot operations (without storage)
TutorialSystemSnapshot snapshot = tutorialSaveLocator.CaptureSnapshot();
tutorialSaveLocator.RestoreSnapshot(snapshot);
```

#### TutorialSaveLocator Events

```csharp
tutorialSaveLocator.OnManagerRegistered.AddListener(() => Debug.Log("Manager ready"));
tutorialSaveLocator.OnBeforeSave.AddListener(slot => Debug.Log($"Saving to {slot}..."));
tutorialSaveLocator.OnAfterSave.AddListener((slot, success) => Debug.Log($"Save: {success}"));
tutorialSaveLocator.OnBeforeLoad.AddListener(slot => Debug.Log($"Loading from {slot}..."));
tutorialSaveLocator.OnAfterLoad.AddListener((slot, success) => Debug.Log($"Load: {success}"));
```

#### Manual Snapshot (Alternative)

Use `CaptureSnapshot()` and `RestoreSnapshot()` directly on TutorialManager when you need full control over serialization:

```csharp
// Capture current state
TutorialSystemSnapshot snapshot = TutorialManager.Instance.CaptureSnapshot();
// Serialize snapshot to your save file (JSON, binary, etc.)

// Restore from snapshot
TutorialManager.Instance.RestoreSnapshot(snapshot);

// Simple API (completed tutorials only)
List<string> completedIds = TutorialManager.Instance.GetCompletedTutorialIdsForSave();
TutorialManager.Instance.RestoreCompletedTutorialIds(completedIds);
```

#### What Gets Saved

| Data | Description |
|------|-------------|
| Completed Tutorial IDs | GUIDs of all tutorials the player has finished (supports PlayOnce) |
| Active Tutorial State | Current state, step index, and step progress |
| Step Elapsed Time | For timed steps, how much time has passed |
| Queued Tutorials | Tutorials waiting in the queue |

---

## Troubleshooting

### Tutorial Doesn't Start

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Nothing happens when triggering | Player tag mismatch | Verify player has `Player` tag |
| Console: "TutorialManager.Instance is null" | No manager in scene | Add TutorialManager GameObject |
| Console: "Tutorial already completed" | PlayOnce is enabled | Call `ResetAllProgress()` or uncheck PlayOnce for testing |
| Console: "Tutorial not found in database" | Not registered | Add tutorial to TutorialManager's Tutorial Database list |

### UI Doesn't Show

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Panel never appears | Tutorial Panel reference missing | Check UI_TutorialController has TutorialPanel assigned |
| Panel shows but no text | Both text references empty | Assign InstructionTextFallback at minimum |
| Buttons do nothing | Button references missing | Assign SkipButton and ContinueButton |

### Steps Don't Complete

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Timed step never advances | Duration is 0 | Set Duration > 0 |
| Condition step never advances | Condition never satisfied | Test condition separately, verify event is raised |
| Manual step has no Continue button | IsTimedStep is checked | Uncheck IsTimedStep |

### Debugging Tips

1. **Enable logging**: Check `Enable Debug Logging` on TutorialManager
2. **Check Console**: All tutorial events are logged with `[Tutorial]` prefix
3. **Verify references**: Select UI_TutorialController and confirm all fields are assigned
4. **Test ScriptableObjects**: Use the Quick Start test script to verify tutorials work without UI

---

## Architecture Reference

### Class Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                        SCRIPTABLEOBJECTS                        │
│                    (Assets in Project folder)                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Tutorial_SO ─────────────┐                                    │
│   - devName                │                                    │
│   - tutorialId             │ contains                           │
│   - playOnce               │                                    │
│   - canSkip                ▼                                    │
│   - steps[]  ────►  TutorialStep_SO                             │
│                     - devName                                   │
│                     - stepId                                    │
│                     - isTimedStep                               │
│                     - duration                                  │
│                     - completionCondition                       │
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
│   TutorialRuntime ─────────┐                                    │
│   - CurrentState           │                                    │
│   - Progress               │ contains                           │
│   - CurrentStep            │                                    │
│                            ▼                                    │
│   Steps[]  ────►  TutorialStepRuntime                           │
│                   - CurrentState                                │
│                   - ElapsedTime                                 │
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
│   TutorialManager (Singleton)                                   │
│   - Starts/stops tutorials                                      │
│   - Tracks completion                                           │
│   - Fires events                                                │
│   - CaptureSnapshot()/RestoreSnapshot()                         │
│            │                                                    │
│            │ events                                             │
│            ▼                                                    │
│   UI_TutorialController                                         │
│   - Shows/hides panel                                           │
│   - Updates text                                                │
│   - Handles buttons                                             │
│                                                                 │
│   TutorialSaveManager ◄──── TutorialSaveLocator_SO              │
│   - SaveAsync()/LoadAsync()     (decoupled access)              │
│   - Uses SaveService.Provider                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Interface Implementation

The Tutorial System implements the Objective interfaces for consistency with the Quest System:

| Class | Implements | Role |
|-------|------------|------|
| `TutorialRuntime` | `IMission` | Top-level container |
| `TutorialStepRuntime` | `IStage` | Individual step |
| `TutorialManager` | `IBootstrapInitializable` | Priority 105 (Core phase) |
| `TutorialSaveManager` | `IBootstrapInitializable` | Priority 205 (Persistence phase) |

---

## Example Assets

The `BasicTutorialExample` folder contains ready-to-use examples:

```
BasicTutorialExample/
├── Scripts/
│   ├── TutorialTestStarter.cs      (Key-press tutorial starter for testing)
│   └── UI/
│       ├── UI_TutorialController.cs    (UI panel controller)
│       └── TutorialTrigger.cs          (Collision-based trigger)
└── ScriptableObjects/Tutorials/
    ├── SO_Tutorial_BasicMovement.asset
    ├── SO_TutorialStep_Welcome.asset
    ├── SO_TutorialStep_Movement.asset
    └── SO_TutorialStep_Jump.asset
```

---

## Best Practices

1. **Test early, test often** - Use the Quick Start approach to verify each piece works
2. **Use timed steps sparingly** - Players want to play, not wait
3. **Keep instructions concise** - One concept per step
4. **Always allow skip** - Respect player time, especially for returning players
5. **Enable PlayOnce** - Nobody wants to repeat tutorials
6. **Use descriptive Dev Names** - They appear in logs and (as fallback) in UI
7. **Disable debug logging for release** - Uncheck `Enable Debug Logging` before shipping

---

## Related Documentation

- [Extensible Architecture Design](extensible-architecture-design.md) - Interface design
- [Achievement System](achievements.md) - Related tracking system
- [Overview](overview.md) - Quest System overview
