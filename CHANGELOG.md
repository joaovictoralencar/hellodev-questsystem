# Changelog

All notable changes to this project will be documented in this file.

## [3.10.0] - 2026-01-14

### Added
- **TransitionNode**: New graph node for configurable stage transitions
  - Supports three trigger types: OnGroupsComplete, OnConditionsMet, Manual
  - Priority ordering when multiple transitions are valid (higher = evaluated first)
  - Optional label for debugging and identification
  - Conditions input port for conditional transitions
  - Enables complex patterns like conditional stage skipping
- **PortCapacityHelper**: Utility for setting port capacity via reflection
  - Workaround for Unity Graph Toolkit's internal PortCapacity API
  - Extension methods: `SetMultiCapacity()`, `SetSingleCapacity()`

### Changed
- **StageNode**: `In` port now accepts multiple connections (multi-capacity)
  - Allows multiple TransitionNodes or sources to target the same stage
  - Enables branching patterns where different paths converge

### Documentation
- Updated quest-graph-creation-reference.md with TransitionNode section
- Updated Goblin's Bane tutorial with conditional skip path using TransitionNode
- Added connection patterns and port capacity notes

## [3.9.0] - 2026-01-11

### Added
- **QuestChoiceNode**: New node for quest-level branching in QuestLineGraph
  - Allows branching questlines based on conditions or quest outcomes
  - Quest1 -> QuestChoiceNode -> Quest2 (Path A) / Quest3 (Path B)
  - Supports 1-4 output paths with conditional routing
  - Default output path when no conditions match

### Changed
- **StageNode**: Converted identity fields from options to ports for consistency
  - StageName, JournalEntry, StageIcon now ports (visible on node)
  - IsTerminal, IsOptional, IsHidden now ports (visible on node)
  - HasPlayerChoices, TaskGroupCount remain options (control port generation)

### Fixed
- GraphToQuestLineConverter now handles QuestChoiceNode when collecting quests
- Added validation rules for QuestChoiceNode (connected outputs, condition count)

## [3.8.0] - 2026-01-11

### Changed
- **Graph Node UX Improvements**: Converted key fields from options to ports for better visibility
  - Fields now appear directly on nodes AND in Node Properties inspector section
  - Users can edit values without opening the full inspector
- **QuestNode**: DevName, IsOptional, RecommendedLevel now ports; removed OrderOverride
- **TaskTypedNode**: DevName now a port (propagates to all task nodes)
- **TaskIntNode**: RequiredCount now a port
- **TaskTimedNode**: TimeLimit, FailQuestOnExpire now ports
- **TaskStringNode**: TargetValue now a port
- **QuestStartNode**: Added OutputMode toggle to support both StageFlow and QuestFlow outputs

### Fixed
- TaskBaseNode.InlineData is now virtual for proper override instead of shadowing
- Count fields (StageCount, TriggerConditionCount, etc.) remain as options to preserve dynamic port regeneration
- QuestNode stage ports now use StageGraph type (was incorrectly using StageFlow)
- Validation no longer reports false "unreachable" warnings for subgraph nodes and Blackboard variables

## [2.2.0] - 2025-12-28

### Added
- **Quest_SO.Odin.cs**: New "Quest Stages" section in Overview tab
  - Shows stage index, name, and settings (Terminal, Optional, Hidden)
  - Displays journal entry status
  - Shows task groups within each stage with execution mode icons
  - Visualizes stage transitions with target stage and trigger type
  - Connector lines between stages for visual flow
- **Task_SO.Odin.cs**: Enhanced "Used by Quests" section
  - Now shows stage information (stage index, stage name) alongside group info
  - Improved layout with stage tags and labels

### Changed
- Replaced "Task Groups" section with comprehensive "Quest Stages" section in Quest_SO Overview
- Task_SO now searches through stages to find containing quests (instead of flat task groups)

## [2.1.0] - 2025-12-27

### Added
- Quest Stages system with conditional transitions
- Modular localization tables (Quests, Tasks, Locations, Stages)
- Stage journal entries with LocalizedString support

## [2.0.0] - 2025-12-24

### Added
- Designer UX improvements with AAA-quality inspectors
- Quest Creation Wizard
- QuestLine system for narrative grouping
- Task Groups with execution modes (Sequential, Parallel, AnyOrder, OptionalXofY)

## [1.0.0] - 2024-01-01

### Added
- Initial release
- Basic quest system functionality
- Quest objectives and rewards
- Editor tools
- Sample scenes and documentation