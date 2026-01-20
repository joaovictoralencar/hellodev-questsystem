# Unified Save System Refactoring Plan

*Created: 2026-01-18*

---

## Overview

Refactor the save system from independent per-system saves to a unified save file architecture with ScriptableObject configuration.

### Goals
1. **ScriptableObject Config** - Replace SerializedField config in SaveSystemSetup with a globally accessible SO
2. **Unified Save File** - All saveable data batched into ONE file per slot
3. **Extensible Design** - Easy to add new saveable systems without modifying core

### Current Problems
- Provider settings scattered in MonoBehaviour (SaveSystemSetup)
- Multiple files per slot (quest save + tutorial save = 2 files)
- No unified version control for migrations
- Tight coupling between SaveSystemSetup and individual managers

---

## Architecture

### Unified Save File Structure
```json
{
  "Version": 1,
  "Timestamp": "2026-01-18T14:30:00Z",
  "Systems": [
    { "Key": "quests", "TypeName": "...", "JsonData": "{...}" },
    { "Key": "tutorials", "TypeName": "...", "JsonData": "{...}" }
  ],
  "Metadata": {
    "SlotKey": "save-0",
    "PlayTimeSeconds": 3600
  }
}
```

### Component Diagram
```
SaveSystemSettings_SO (global config)
         │
         ▼
UnifiedSaveManager (coordinator)
         │
    ┌────┴────┐
    ▼         ▼
QuestSaveable  TutorialSaveable
   System         System
    │              │
    ▼              ▼
QuestSave      TutorialSave
  Manager        Manager
```

---

## Implementation Tasks

### Phase 1: Core Infrastructure (com.hellodev.utils)

#### 1.1 Create `SaveSystemSettings_SO.cs`
**Path:** `Assets/HelloDev/com.hellodev.utils/Runtime/Scripts/Saving/SaveSystemSettings_SO.cs`

ScriptableObject containing:
- `saveSubdirectory` (string) - default "Saves"
- `fileExtension` (string) - default ".save"
- `prettyPrint` (bool) - default true
- `currentVersion` (int) - for migration support
- `slotConfig` (SaveSlotConfig_SO) - optional reference
- `autoMigrateLegacySaves` (bool)
- `deleteLegacyAfterMigration` (bool)
- Method: `CreateProvider()` returns configured ISaveProvider

#### 1.2 Create `ISaveableSystem.cs`
**Path:** `Assets/HelloDev/com.hellodev.utils/Runtime/Scripts/Saving/ISaveableSystem.cs`

Interface for systems that can be saved:
```csharp
public interface ISaveableSystem
{
    string SystemKey { get; }           // e.g., "quests", "tutorials"
    int SavePriority { get; }           // Order for capture/restore
    Type SnapshotType { get; }          // For deserialization
    object CaptureSnapshot();
    bool RestoreSnapshot(object snapshot);
    void OnBeforeSave();
    void OnAfterSave(bool success);
    void OnBeforeLoad();
    void OnAfterLoad(bool success);
}
```

#### 1.3 Create `UnifiedSnapshot.cs`
**Path:** `Assets/HelloDev/com.hellodev.utils/Runtime/Scripts/Saving/UnifiedSnapshot.cs`

Container classes:
- `UnifiedSnapshot` - Version, Timestamp, Systems list, Metadata
- `SystemSnapshotEntry` - Key, TypeName, JsonData (serialized snapshot)
- `UnifiedSnapshotMetadata` - SlotKey, Timestamp, PlayTimeSeconds, CustomData

#### 1.4 Create `UnifiedSaveManager.cs`
**Path:** `Assets/HelloDev/com.hellodev.utils/Runtime/Scripts/Saving/UnifiedSaveManager.cs`

Central coordinator:
- Implements `IBootstrapInitializable` (priority 50 - before other systems)
- References `SaveSystemSettings_SO` for configuration
- Maintains list of registered `ISaveableSystem`
- `RegisterSystem(ISaveableSystem)` / `UnregisterSystem(ISaveableSystem)`
- `SaveAsync(slotKey)` - captures all systems, saves unified file
- `LoadAsync(slotKey)` - loads unified file, restores all systems
- Migration logic for legacy saves

#### 1.5 Create `UnifiedSaveLocator_SO.cs`
**Path:** `Assets/HelloDev/com.hellodev.utils/Runtime/Scripts/Saving/UnifiedSaveLocator_SO.cs`

Locator for decoupled access (extends LocatorBase_SO)

---

### Phase 2: Adapter Classes (com.hellodev.questsystem)

#### 2.1 Create `QuestSaveableSystem.cs`
**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/SaveLoad/QuestSaveableSystem.cs`

Adapter that implements `ISaveableSystem`:
- SystemKey: "quests"
- SavePriority: 100
- Delegates to existing `QuestSaveManager.CaptureSnapshot()` / `RestoreSnapshot()`
- Self-registers with `UnifiedSaveLocator_SO` in OnEnable

#### 2.2 Create `TutorialSaveableSystem.cs`
**Path:** `Assets/com.hellodev.questsystem/Runtime/Scripts/Core/Tutorials/SaveLoad/TutorialSaveableSystem.cs`

Adapter that implements `ISaveableSystem`:
- SystemKey: "tutorials"
- SavePriority: 110
- Delegates to existing `TutorialSaveManager`

---

### Phase 3: Modifications

#### 3.1 Modify `SaveSystemSetup.cs`
**Path:** `Assets/com.hellodev.questsystem/BasicQuestExample/Scripts/SaveSystemSetup.cs`

Changes:
- Remove: `saveSubdirectory`, `fileExtension`, `prettyPrint` fields
- Add: Reference to `UnifiedSaveLocator_SO`
- Keep: Auto-save/load policy settings
- Update: `InitializeAsync()` to use unified system

#### 3.2 Keep Existing Managers Intact
- `QuestSaveManager.cs` - No changes (used by adapter)
- `TutorialSaveManager.cs` - No changes (used by adapter)
- `QuestSaveLocator_SO.cs` - Keep for backward compatibility
- `TutorialSaveLocator_SO.cs` - Keep for backward compatibility

---

### Phase 4: Migration Support

#### 4.1 Legacy Detection
Check for old per-system files when loading:
- `quest_{slotKey}` files
- `tutorial_{slotKey}` files

#### 4.2 Auto-Migration
If legacy files exist and no unified save:
1. Load legacy snapshots
2. Create unified snapshot containing all data
3. Save unified snapshot
4. Optionally delete legacy files

---

## File Summary

### New Files (7 total)

| File | Location | Purpose |
|------|----------|---------|
| `SaveSystemSettings_SO.cs` | `utils/Saving/` | Global config SO |
| `ISaveableSystem.cs` | `utils/Saving/` | Interface for saveable systems |
| `UnifiedSnapshot.cs` | `utils/Saving/` | Container for all data |
| `UnifiedSaveManager.cs` | `utils/Saving/` | Central coordinator |
| `UnifiedSaveLocator_SO.cs` | `utils/Saving/` | Decoupled access |
| `QuestSaveableSystem.cs` | `questsystem/SaveLoad/` | Quest adapter |
| `TutorialSaveableSystem.cs` | `questsystem/Tutorials/SaveLoad/` | Tutorial adapter |

### Modified Files (1 total)

| File | Changes |
|------|---------|
| `SaveSystemSetup.cs` | Remove provider config, use unified system |

### Assets to Create in Unity

1. `SaveSystemSettings.asset` - Global save configuration
2. `UnifiedSaveLocator.asset` - Locator instance

---

## Bootstrap Priority Order

| Priority | Component | Purpose |
|----------|-----------|---------|
| 50 | UnifiedSaveManager | Configure provider, ready for registrations |
| 100 | QuestManager | Core quest functionality |
| 105 | TutorialManager | Core tutorial functionality |
| 150 | QuestSaveableSystem | Register with unified system |
| 155 | TutorialSaveableSystem | Register with unified system |
| 200 | QuestSaveManager | Legacy standalone support |
| 205 | TutorialSaveManager | Legacy standalone support |
| 250 | SaveSystemSetup | Auto-load if configured |

---

## Verification

### Testing Steps
1. Create `SaveSystemSettings.asset` with test configuration
2. Create `UnifiedSaveLocator.asset`
3. Add `UnifiedSaveManager` to scene, assign references
4. Add `QuestSaveableSystem` and `TutorialSaveableSystem` to scene
5. Enter Play mode, start a quest and tutorial
6. Save using unified system
7. Verify single `.save` file created with both systems' data
8. Restart Play mode, load save
9. Verify quest and tutorial state restored correctly

### Migration Testing
1. Create legacy saves using old system
2. Switch to unified system
3. Load - verify auto-migration creates unified file
4. Verify legacy data preserved in unified format

---

## Notes

- Existing `QuestSaveManager` and `TutorialSaveManager` remain functional for standalone use
- Adapter pattern allows gradual migration without breaking existing code
- `ISaveableSystem` interface enables future systems (inventory, achievements) to integrate easily
- Version field in `UnifiedSnapshot` supports future schema migrations
