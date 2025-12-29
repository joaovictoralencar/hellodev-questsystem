# Changelog

All notable changes to this project will be documented in this file.

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