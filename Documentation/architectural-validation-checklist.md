# Architectural Validation Checklist

*Based on: 5 Architectural Tips for Unity Developers*
*Created: 2026-01-04*

This document lists every concept from the architectural tips and what needs to be validated in the HelloDev Quest System.

---

## TIP 1: Start with Interfaces

### Core Principles to Validate

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 1.1 | Classes depend on behavior, not concrete types | Do Quest System classes reference interfaces or concrete classes? | ⬜ |
| 1.2 | Swapping/extending doesn't require rewiring | Can we swap QuestManager implementation without changing consumers? | ⬜ |
| 1.3 | Logic easier to test outside play mode | Can TaskRuntime be tested without Unity? | ⬜ |
| 1.4 | Systems become reusable | Can the quest system work in different games? | ⬜ |
| 1.5 | Interface = shared contract | Are interfaces defining clear contracts? | ⬜ |
| 1.6 | Target typed as interface, not specific class | Do methods accept interfaces or concrete types? | ⬜ |
| 1.7 | Break tight coupling | Are there circular dependencies? | ⬜ |
| 1.8 | Opt-in via interface implementation | Do classes opt into systems via interfaces? | ⬜ |

### Files to Check

```
□ com.hellodev.conditions/Runtime/Scripts/ICondition.cs
  - Does ICondition define a clear contract?
  - Is IConditionEventDriven properly extending it?

□ com.hellodev.questsystem/Runtime/Scripts/Core/Internal/IQuestRegistry.cs
  - What methods does it expose?
  - Is it used consistently?

□ com.hellodev.questsystem/Runtime/Scripts/Core/Internal/IQuestLineRegistry.cs
  - Same validation as above

□ All Runtime classes - do they depend on interfaces or concrete types?
  - QuestManager.cs - what does it depend on?
  - QuestRuntime.cs - what does it depend on?
  - TaskRuntime.cs - what does it depend on?
```

### Missing Interfaces to Consider

```
□ IQuestManager - Does this exist? Should it?
□ IQuestRuntime - Does this exist? Should it?
□ ITaskRuntime - Does this exist? Should it?
□ ISaveManager - Does this exist? Should it?
□ IQuestSaveProvider - Does this exist?
```

### Testing Infrastructure

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 1.9 | Interfaces enable test doubles/mocks | Can we create fake conditions for testing? | ⬜ |
| 1.10 | Tests can use fake implementations | Do test files exist? Do they use mocks? | ⬜ |
| 1.11 | Assembly definitions for testing | Does Tests folder have proper asmdef? | ⬜ |
| 1.12 | Test references assembly definitions | Does test asmdef reference runtime asmdef? | ⬜ |
| 1.13 | Edit mode vs Play mode tests | Are tests configured for edit mode (faster)? | ⬜ |
| 1.14 | Arrange-Act-Assert pattern | Do tests follow AAA pattern? | ⬜ |

### Files to Check

```
□ com.hellodev.questsystem/Tests/Runtime/QuestSystemTests.cs
  - Does it have actual test code or just stubs?
  - Does it use test doubles?
  - Does it follow Arrange-Act-Assert?

□ com.hellodev.questsystem/Tests/Editor/QuestEditorTests.cs
  - Same validation

□ com.hellodev.questsystem/Tests/*.asmdef
  - Is it configured for edit mode or play mode?
  - Does it reference the runtime asmdef?
```

---

## TIP 2: Separate Logic from MonoBehaviors

### Core Principles to Validate

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 2.1 | Core behavior can run without Unity | Can QuestRuntime work without MonoBehaviour? | ⬜ |
| 2.2 | Logic becomes trivial to test | Can we test TaskRuntime without GameObjects? | ⬜ |
| 2.3 | MonoBehaviors focus on lifecycle only | Does QuestManager only do lifecycle, not logic? | ⬜ |
| 2.4 | Plain C# class for logic | Are runtime classes plain C#? | ⬜ |
| 2.5 | Pass values through constructor | Do runtime classes use constructor injection? | ⬜ |
| 2.6 | Logic knows nothing about GameObjects | Do runtime classes reference Transform, GameObject? | ⬜ |
| 2.7 | MonoBehavior holds instance of logic | Does QuestManager hold QuestRuntime instances? | ⬜ |
| 2.8 | MonoBehavior NOT the logic, just OWNS it | Is logic inside MonoBehavior or separate class? | ⬜ |
| 2.9 | Unity responsible for setup/timing | Does MonoBehavior handle Awake/Start/Update? | ⬜ |
| 2.10 | MonoBehavior forwards calls to logic | Does QuestManager delegate to registries? | ⬜ |
| 2.11 | Public API stays the same | Is API clean regardless of internal structure? | ⬜ |

### Files to Check - Runtime Classes (Should be Plain C#)

```
□ Runtime/Scripts/Core/Tasks/TaskRuntime.cs
  - Is it a plain C# class or MonoBehaviour?
  - Does constructor take dependencies?
  - Does it reference GameObject, Transform, Component?

□ Runtime/Scripts/Core/Tasks/IntTaskRuntime.cs
□ Runtime/Scripts/Core/Tasks/BoolTaskRuntime.cs
□ Runtime/Scripts/Core/Tasks/StringTaskRuntime.cs
□ Runtime/Scripts/Core/Tasks/LocationTaskRuntime.cs
□ Runtime/Scripts/Core/Tasks/TimedTaskRuntime.cs
□ Runtime/Scripts/Core/Tasks/DiscoveryTaskRuntime.cs
  - Same validation for each

□ Runtime/Scripts/Core/Quests/QuestRuntime.cs
  - Plain C# class?
  - Constructor injection?
  - Unity dependencies?

□ Runtime/Scripts/Core/QuestLines/QuestLineRuntime.cs
  - Same validation

□ Runtime/Scripts/Core/TaskGroups/TaskGroupRuntime.cs
  - Same validation

□ Runtime/Scripts/Core/Stages/QuestStageRuntime.cs
  - Same validation (if exists)
```

### Files to Check - MonoBehaviors (Should only do lifecycle)

```
□ Runtime/Scripts/Core/QuestManager.cs
  - Is it a MonoBehaviour?
  - Does it contain business logic or just lifecycle?
  - Does it delegate to internal registries/services?

□ Runtime/Scripts/Core/SaveLoad/QuestSaveManager.cs
  - Same validation

□ BasicQuestExample/Scripts/UI/*.cs
  - Are UI scripts MonoBehaviours?
  - Do they contain logic or just wire events?
```

---

## TIP 3: Separate Data from Logic

### Core Principles to Validate

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 3.1 | Balance changes don't require code | Can designer change quest values without code? | ⬜ |
| 3.2 | Logic stays focused on behavior | Does TaskRuntime only have behavior, not data? | ⬜ |
| 3.3 | Designers can tweak values safely | Are values in ScriptableObjects, not code? | ⬜ |
| 3.4 | Tests become clear and explicit | Can tests create configs with specific values? | ⬜ |
| 3.5 | Code = what happens, Data = values | Is this separation clear? | ⬜ |
| 3.6 | ScriptableObject for shared editable data | Are all configs ScriptableObjects? | ⬜ |
| 3.7 | CreateAssetMenu attribute | Do all *_SO have create menu? | ⬜ |
| 3.8 | Reference config from MonoBehavior | Does QuestManager reference Quest_SO assets? | ⬜ |
| 3.9 | Assign values from inspector | Can all values be set in inspector? | ⬜ |
| 3.10 | Pass config into logic (not raw values) | Does QuestRuntime receive Quest_SO? | ⬜ |
| 3.11 | MonoBehavior wires, not decides | Is wiring in Awake, behavior in runtime classes? | ⬜ |
| 3.12 | Logic stores reference to config | Does TaskRuntime store Task_SO reference? | ⬜ |
| 3.13 | Inject config through constructor | Is Task_SO passed to TaskRuntime constructor? | ⬜ |
| 3.14 | Read values from config when needed | Does logic read from _SO at runtime? | ⬜ |

### Data Classes (ScriptableObjects) to Check

```
□ Runtime/Scripts/Core/ScriptableObjects/Quest_SO.cs
  - Has CreateAssetMenu?
  - Contains configuration data only?
  - No behavior logic inside?

□ Runtime/Scripts/Core/ScriptableObjects/Task_SO.cs
  - Same validation
  - Has factory method GetRuntimeTask()?

□ Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskInt_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskBool_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskString_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskLocation_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskTimed_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/Task Types/TaskDiscovery_SO.cs
  - Same validation for each

□ Runtime/Scripts/Core/ScriptableObjects/QuestLine_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/QuestType_SO.cs
□ Runtime/Scripts/Core/ScriptableObjects/QuestRewardType_SO.cs
  - Same validation
```

### Factory Pattern to Check

```
□ Does Quest_SO.GetRuntimeQuest() exist?
□ Does Task_SO.GetRuntimeTask() exist?
□ Does QuestLine_SO.GetRuntimeQuestLine() exist?
□ Does TaskGroup.GetRuntimeTaskGroup() exist?
```

### Interface Serialization Workaround

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 3.15 | Unity can't serialize interfaces | Are we working around this? | ⬜ |
| 3.16 | Serialize MonoBehavior, store as interface | Is this pattern used? | ⬜ |
| 3.17 | Cast in Awake to interfaces | Do we cast in Awake? | ⬜ |
| 3.18 | Fail loudly if misconfigured | Do we validate and throw early? | ⬜ |
| 3.19 | Early validation vs null later | Are null checks happening early? | ⬜ |

### Files to Check

```
□ Any file that needs interface references in inspector
  - How is this handled?
  - Is there validation?
```

---

## TIP 4: Event Driven Architecture

### Core Principles to Validate

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 4.1 | Isolate control flow with events | Is control flow event-based or polling? | ⬜ |
| 4.2 | Separate detecting from deciding | Are triggers separate from handlers? | ⬜ |
| 4.3 | React when meaningful (not constantly ask) | No Update() polling for state? | ⬜ |
| 4.4 | Clearer intent | Are events named for intent? | ⬜ |
| 4.5 | Fewer conditionals in gameplay | Is logic spread or consolidated? | ⬜ |
| 4.6 | Reuse from other sources (AI, UI) | Can systems be driven by different sources? | ⬜ |
| 4.7 | Class for turning input into intent | Is there separation of concerns? | ⬜ |
| 4.8 | Expose events not method calls | Do classes expose events? | ⬜ |
| 4.9 | Raise event instead of acting directly | Do conditions raise events? | ⬜ |
| 4.10 | One place cares about triggers | Is trigger logic centralized? | ⬜ |
| 4.11 | Everything else just listens | Do consumers subscribe, not poll? | ⬜ |
| 4.12 | Subscribe in Awake/Start | Where do subscriptions happen? | ⬜ |
| 4.13 | Logic in own method | Are event handlers clean methods? | ⬜ |
| 4.14 | No Update when event-driven | Are there unnecessary Update() methods? | ⬜ |
| 4.15 | Unsubscribe when destroyed | Are there matching unsubscribes? | ⬜ |
| 4.16 | Event-driven removes control flow | Is control flow minimal? | ⬜ |

### Event Architecture to Check

```
□ Runtime/Scripts/Core/Tasks/TaskRuntime.cs
  - What events does it expose?
  - OnTaskUpdated, OnTaskStarted, OnTaskCompleted, OnTaskFailed?
  - Are they UnityEvents?

□ Runtime/Scripts/Core/Quests/QuestRuntime.cs
  - What events does it expose?
  - OnQuestStarted, OnQuestCompleted, OnQuestFailed?
  - OnChoicesAvailable, OnChoiceMade?
  - OnStageChanged?

□ Runtime/Scripts/Core/TaskGroups/TaskGroupRuntime.cs
  - OnGroupStarted, OnGroupCompleted, OnGroupFailed?
  - OnTaskInGroup* events?

□ Runtime/Scripts/Core/QuestManager.cs
  - QuestAdded, QuestStarted, QuestCompleted, QuestFailed?
  - QuestUpdated, QuestRemoved?
```

### Event Bubbling Pattern to Check

```
□ Do Task events bubble to TaskGroup?
□ Do TaskGroup events bubble to Quest?
□ Do Quest events bubble to QuestManager?
□ Is context added/removed appropriately at each level?
```

### Subscription/Unsubscription Pairs to Check

```
□ QuestRuntime.cs
  - Every AddListener has matching RemoveListener?
  - Subscriptions in constructor/Start?
  - Unsubscriptions in cleanup/destroy?

□ TaskGroupRuntime.cs
  - Same validation

□ TaskRuntime.cs
  - Same validation

□ QuestManager.cs
  - Same validation

□ UI classes (UI_QuestDetails, UI_TaskItem, etc.)
  - Same validation
```

### Condition Event System to Check

```
□ com.hellodev.conditions/Runtime/Scripts/ICondition.cs
  - IConditionEventDriven.SubscribeToEvent()
  - IConditionEventDriven.UnsubscribeFromEvent()
  - How do conditions notify listeners?

□ How do Tasks subscribe to Conditions?
  - Is it in TaskRuntime.StartTask()?
  - Is cleanup in TaskRuntime.CompleteTask()/FailTask()?
```

---

## TIP 5: Registry Pattern

### Core Principles to Validate

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 5.1 | Keep track of collection of things | Are there registries for quests/tasks? | ⬜ |
| 5.2 | Generic registry | Is there a reusable Registry<T>? | ⬜ |
| 5.3 | Static, type-based | Is registry static per type? | ⬜ |
| 5.4 | Stores collection of items | What data structure? HashSet? | ⬜ |
| 5.5 | Each type gets own registry | Separate registries for Quest, Task? | ⬜ |
| 5.6 | Runtime creates when first needed | Lazy initialization? | ⬜ |
| 5.7 | TryAdd guards against null | Null checks in registration? | ⬜ |
| 5.8 | HashSet for storage | Is HashSet used? | ⬜ |
| 5.9 | Safe removal | Remove doesn't throw if missing? | ⬜ |
| 5.10 | Query for items | GetById, GetAll methods? | ⬜ |
| 5.11 | Get first available (simple) | GetFirst or similar? | ⬜ |
| 5.12 | Selection strategy delegate | Can pass selection function? | ⬜ |
| 5.13 | Pass strategy, don't hardcode | Is strategy configurable? | ⬜ |
| 5.14 | Registry owns data, caller owns decision | Clean separation? | ⬜ |
| 5.15 | Expose all registered items | GetAll() or similar? | ⬜ |

### Registry Files to Check

```
□ Runtime/Scripts/Core/Internal/IQuestRegistry.cs
  - What interface does it define?
  - Register, Unregister, GetById, GetAll?

□ Runtime/Scripts/Core/Internal/IQuestLineRegistry.cs
  - Same validation

□ Is there a concrete implementation?
  - QuestRegistry.cs?
  - Where is it instantiated?

□ Is there a generic Registry<T> class?
  - Could be reused across systems?
```

### Self-Registration Pattern to Check

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 5.16 | Objects register in Awake | Do quests register when created? | ⬜ |
| 5.17 | Objects unregister in OnDestroy | Do quests unregister when removed? | ⬜ |
| 5.18 | Registry reflects current state | Is registry always accurate? | ⬜ |
| 5.19 | No serialized references needed | Can find quests dynamically? | ⬜ |
| 5.20 | Query registry at runtime | Does QuestManager query registries? | ⬜ |

### Selection Strategy Pattern to Check

| # | Concept | What to Check | Status |
|---|---------|---------------|--------|
| 5.21 | Strategies from factory/builder | Is there a strategy pattern? | ⬜ |
| 5.22 | Strategy implements delegate | Is delegate used? | ⬜ |
| 5.23 | Track best with extreme initial | Is this pattern used? | ⬜ |
| 5.24 | Loop through candidates | Iteration pattern? | ⬜ |
| 5.25 | Guard against null | Null checks in loops? | ⬜ |
| 5.26 | Check if Unity component | Type checks for Transform access? | ⬜ |
| 5.27 | Skip non-Unity safely | Graceful handling? | ⬜ |
| 5.28 | Check for null result | Final null check before use? | ⬜ |

---

## Validation Execution Plan

### Phase 1: File Inventory
1. List all interfaces in the codebase
2. List all MonoBehaviors vs plain C# classes
3. List all ScriptableObjects
4. List all events exposed
5. List all registry-like structures

### Phase 2: Interface Analysis
1. Check each runtime class for interface dependencies
2. Identify tight coupling
3. Identify missing interfaces

### Phase 3: Logic Separation Analysis
1. Verify runtime classes are plain C#
2. Verify MonoBehaviors only do lifecycle
3. Check for Unity dependencies in logic

### Phase 4: Data Separation Analysis
1. Verify all config in ScriptableObjects
2. Verify factory methods exist
3. Check constructor injection

### Phase 5: Event Architecture Analysis
1. Map all events
2. Verify subscription/unsubscription pairs
3. Check for polling vs event-driven

### Phase 6: Registry Analysis
1. Identify all registry patterns
2. Check for generic implementation
3. Verify self-registration pattern

---

## Summary Checklist

| Tip | Total Items | Validated | Issues |
|-----|-------------|-----------|--------|
| 1. Interfaces | 14 | 0 | - |
| 2. Logic Separation | 11 | 0 | - |
| 3. Data Separation | 19 | 0 | - |
| 4. Event Driven | 16 | 0 | - |
| 5. Registry | 28 | 0 | - |
| **TOTAL** | **88** | **0** | - |
