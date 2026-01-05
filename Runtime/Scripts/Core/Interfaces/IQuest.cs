using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Contract for quest operations. Enables mocking in tests and alternative quest implementations.
    /// Consumers can depend on this interface instead of the concrete QuestRuntime class.
    /// </summary>
    /// <remarks>
    /// This interface extracts the public API of QuestRuntime to enable:
    /// - Unit testing with mock implementations
    /// - Dependency injection patterns
    /// - Future alternative quest implementations
    /// </remarks>
    public interface IQuest
    {
        #region Identity

        /// <summary>
        /// Gets the unique identifier for this quest.
        /// </summary>
        Guid QuestId { get; }

        #endregion

        #region State

        /// <summary>
        /// Gets the current state of the quest (NotStarted, InProgress, Completed, Failed).
        /// </summary>
        QuestState CurrentState { get; }

        /// <summary>
        /// Gets the overall progress of the quest (0-1).
        /// </summary>
        float CurrentProgress { get; }

        /// <summary>
        /// Gets the index of the current stage.
        /// </summary>
        int CurrentStageIndex { get; }

        /// <summary>
        /// Returns true if the quest is currently transitioning between stages.
        /// </summary>
        bool IsTransitioningStage { get; }

        #endregion

        #region Stages

        /// <summary>
        /// Gets all stages in this quest.
        /// </summary>
        List<QuestStageRuntime> Stages { get; }

        /// <summary>
        /// Gets the currently active stage, or null if quest is not in progress.
        /// </summary>
        QuestStageRuntime CurrentStage { get; }

        #endregion

        #region Task Groups

        /// <summary>
        /// Gets all task groups across all stages.
        /// </summary>
        List<TaskGroupRuntime> TaskGroups { get; }

        /// <summary>
        /// Gets the currently active task group.
        /// </summary>
        TaskGroupRuntime CurrentGroup { get; }

        #endregion

        #region Tasks

        /// <summary>
        /// Gets all tasks that are currently in progress.
        /// </summary>
        IReadOnlyList<TaskRuntime> CurrentTasks { get; }

        /// <summary>
        /// Gets the first currently in-progress task, or null if none.
        /// </summary>
        TaskRuntime CurrentTask { get; }

        /// <summary>
        /// Gets all tasks across all stages (flattened list).
        /// </summary>
        List<TaskRuntime> Tasks { get; }

        #endregion

        #region Branching

        /// <summary>
        /// Dictionary tracking which branch decisions were made.
        /// Key is branch ID, value is choice ID.
        /// </summary>
        Dictionary<string, string> BranchDecisions { get; }

        /// <summary>
        /// Returns true if the current stage requires a player choice before progressing.
        /// </summary>
        bool CurrentStageRequiresChoice { get; }

        /// <summary>
        /// Gets all player choices available in the current stage (conditions met).
        /// </summary>
        List<StageTransition> GetAvailableChoices();

        /// <summary>
        /// Gets all player choices in the current stage (regardless of conditions).
        /// </summary>
        List<StageTransition> GetAllChoices();

        /// <summary>
        /// Checks if a specific choice is currently available.
        /// </summary>
        bool IsChoiceAvailable(string choiceId);

        /// <summary>
        /// Selects a player choice, triggering the associated transition.
        /// </summary>
        bool SelectChoice(StageTransition choice, bool bypassConditions = false);

        /// <summary>
        /// Selects a player choice by its ID.
        /// </summary>
        bool SelectChoiceById(string choiceId);

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Starts the quest, changing its state to InProgress.
        /// </summary>
        void StartQuest();

        /// <summary>
        /// Marks the quest as completed.
        /// </summary>
        void CompleteQuest();

        /// <summary>
        /// Marks the quest as failed.
        /// </summary>
        void FailQuest();

        /// <summary>
        /// Resets the quest to its initial state and restarts it.
        /// </summary>
        void ResetQuest();

        /// <summary>
        /// Force completes all remaining tasks and the quest.
        /// </summary>
        void ForceComplete();

        #endregion

        #region Stage Navigation

        /// <summary>
        /// Attempts to set the quest to a specific stage by index.
        /// </summary>
        bool TrySetStage(int stageIndex);

        /// <summary>
        /// Gets a stage by its index.
        /// </summary>
        QuestStageRuntime GetStageByIndex(int stageIndex);

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Increments the current task's step.
        /// </summary>
        void IncrementCurrentTask();

        /// <summary>
        /// Decrements the current task's step.
        /// </summary>
        void DecrementCurrentTask();

        /// <summary>
        /// Checks start conditions and starts the quest if met.
        /// </summary>
        bool CheckForConditionsAndStart();

        /// <summary>
        /// Checks if the quest's start conditions are met.
        /// </summary>
        bool CheckStartConditions();

        #endregion
    }
}
