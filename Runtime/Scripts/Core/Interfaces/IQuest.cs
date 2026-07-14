using System;
using System.Collections.Generic;
using HelloDev.Objectives;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.QuestSystem.Tasks;

namespace HelloDev.QuestSystem.Interfaces
{
    /// <summary>
    /// Represents a quest mission.
    /// <para>
    /// A quest is a specialized <see cref="IMission"/> that is composed of task groups,
    /// tasks, and optional branching choices. It provides gameplay-specific functionality
    /// such as task progression, branching decisions, and force completion while inheriting
    /// the common mission lifecycle, stage management, and events from <see cref="IMission"/>.
    /// </para>
    /// </summary>
    public interface IQuest : IMission
    {
        #region Task Groups

        /// <summary>
        /// Gets all task groups across all stages.
        /// </summary>
        IReadOnlyList<TaskGroupRuntime> TaskGroups { get; }

        /// <summary>
        /// Gets the currently active task group.
        /// Returns <c>null</c> if no task group is currently active.
        /// </summary>
        TaskGroupRuntime CurrentGroup { get; }

        #endregion

        #region Tasks

        /// <summary>
        /// Gets all tasks that belong to this quest.
        /// </summary>
        IReadOnlyList<TaskRuntime> Tasks { get; }

        /// <summary>
        /// Gets all tasks that are currently in progress.
        /// </summary>
        IReadOnlyList<TaskRuntime> CurrentTasks { get; }

        /// <summary>
        /// Gets the first currently active task.
        /// Returns <c>null</c> if no task is currently active.
        /// </summary>
        TaskRuntime CurrentTask { get; }

        #endregion

        #region Branching

        /// <summary>
        /// Gets the dictionary containing all branch decisions made during this quest.
        /// The key represents the branch identifier, and the value represents the selected choice identifier.
        /// </summary>
        Dictionary<string, string> BranchDecisions { get; }

        /// <summary>
        /// Gets whether the current stage requires the player to make a choice before progression can continue.
        /// </summary>
        bool CurrentStageRequiresChoice { get; }

        /// <summary>
        /// Gets all choices currently available in the active stage.
        /// Choices whose conditions are not met are excluded.
        /// </summary>
        /// <returns>A list of available stage transitions.</returns>
        List<StageTransition> GetAvailableChoices();

        /// <summary>
        /// Gets every choice defined in the current stage, regardless of whether its conditions are currently satisfied.
        /// </summary>
        /// <returns>A list containing every stage transition for the current stage.</returns>
        List<StageTransition> GetAllChoices();

        /// <summary>
        /// Determines whether a choice with the specified identifier is currently available.
        /// </summary>
        /// <param name="choiceId">The identifier of the choice.</param>
        /// <returns>
        /// <c>true</c> if the choice exists and its conditions are currently met; otherwise, <c>false</c>.
        /// </returns>
        bool IsChoiceAvailable(string choiceId);

        /// <summary>
        /// Selects the specified player choice and executes its associated stage transition.
        /// </summary>
        /// <param name="choice">The choice to execute.</param>
        /// <param name="bypassConditions">
        /// If <c>true</c>, the choice will be executed even if its conditions are not currently met.
        /// </param>
        /// <returns>
        /// <c>true</c> if the choice was successfully selected; otherwise, <c>false</c>.
        /// </returns>
        bool SelectChoice(StageTransition choice, bool bypassConditions = false);

        /// <summary>
        /// Selects a player choice by its identifier.
        /// </summary>
        /// <param name="choiceId">The identifier of the choice to select.</param>
        /// <returns>
        /// <c>true</c> if the choice was found and successfully selected; otherwise, <c>false</c>.
        /// </returns>
        bool SelectChoiceById(string choiceId);

        #endregion

        #region Stage Navigation

        /// <summary>
        /// Attempts to transition the quest to the specified stage.
        /// </summary>
        /// <param name="stageIndex">The zero-based index of the target stage.</param>
        /// <returns>
        /// <c>true</c> if the transition succeeded; otherwise, <c>false</c>.
        /// </returns>
        bool TrySetStage(int stageIndex);

        /// <summary>
        /// Gets the stage at the specified index.
        /// </summary>
        /// <param name="stageIndex">The zero-based stage index.</param>
        /// <returns>
        /// The stage at the specified index, or <c>null</c> if the index is invalid.
        /// </returns>
        QuestStageRuntime GetStageByIndex(int stageIndex);

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Advances the currently active task by one step.
        /// If no task is active, this method has no effect.
        /// </summary>
        void IncrementCurrentTask();

        /// <summary>
        /// Reverts the currently active task by one step.
        /// If no task is active, this method has no effect.
        /// </summary>
        void DecrementCurrentTask();

        /// <summary>
        /// Evaluates the quest's start conditions and starts the quest if they are satisfied.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the quest started successfully; otherwise, <c>false</c>.
        /// </returns>
        bool CheckForConditionsAndStart();

        /// <summary>
        /// Determines whether all conditions required to start this quest are currently satisfied.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the quest can start; otherwise, <c>false</c>.
        /// </returns>
        bool CheckStartConditions();

        /// <summary>
        /// Immediately completes all remaining objectives, tasks, and stages,
        /// causing the quest to finish successfully.
        /// This method bypasses normal gameplay progression.
        /// </summary>
        void ForceComplete();

        #endregion
    }
}