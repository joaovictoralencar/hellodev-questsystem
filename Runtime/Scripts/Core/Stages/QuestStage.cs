using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.QuestSystem.TaskGroups;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.Stages
{
    /// <summary>
    /// Represents a discrete phase of a quest.
    /// Stages are numbered (like Skyrim: 0, 10, 20...) for easy insertion of new stages.
    /// Each stage contains task groups and transitions to other stages.
    /// </summary>
    [Serializable]
    public class QuestStage
    {
        #region Serialized Fields

#if ODIN_INSPECTOR
        [BoxGroup("Stage Identity"), PropertyOrder(0)]
#endif
        [SerializeField, Tooltip("Unique index for this stage. Use gaps (0, 10, 20) to allow inserting stages later.")]
        private int stageIndex;

#if ODIN_INSPECTOR
        [BoxGroup("Stage Identity"), PropertyOrder(1)]
#endif
        [SerializeField, Tooltip("Developer-friendly name for this stage.")]
        private string stageName = "New Stage";

#if ODIN_INSPECTOR
        [BoxGroup("Stage Identity"), PropertyOrder(2)]
#endif
        [SerializeField, Tooltip("Localized journal entry shown to the player when this stage is active.")]
        private LocalizedString journalEntry;

#if ODIN_INSPECTOR
        [BoxGroup("Stage Identity"), PropertyOrder(3), PreviewField(40, Alignment = ObjectFieldAlignment.Left)]
#endif
        [SerializeField, Tooltip("Icon displayed in the journal for this stage.")]
        private Sprite stageIcon;

#if ODIN_INSPECTOR
        [BoxGroup("Task Groups"), PropertyOrder(10), ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
#endif
        [SerializeField, Tooltip("Task groups that must be completed in this stage.")]
        private List<TaskGroup> taskGroups = new();

#if ODIN_INSPECTOR
        [BoxGroup("Transitions"), PropertyOrder(20), ListDrawerSettings(ShowFoldout = true)]
#endif
        [SerializeField, Tooltip("Defines how to move from this stage to other stages.")]
        private List<StageTransition> transitions = new();

#if ODIN_INSPECTOR
        [BoxGroup("Settings"), PropertyOrder(30)]
#endif
        [SerializeField, Tooltip("If true, this stage can be skipped without failing the quest.")]
        private bool isOptional;

#if ODIN_INSPECTOR
        [BoxGroup("Settings"), PropertyOrder(31)]
#endif
        [SerializeField, Tooltip("If true, this stage won't appear in the journal until reached. Useful for secret paths.")]
        private bool isHidden;

#if ODIN_INSPECTOR
        [BoxGroup("Settings"), PropertyOrder(32)]
#endif
        [SerializeField, Tooltip("If true, this stage is a terminal stage (quest ends when completed, no transitions needed).")]
        private bool isTerminal;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique index for this stage.
        /// </summary>
        public int StageIndex
        {
            get => stageIndex;
            set => stageIndex = value;
        }

        /// <summary>
        /// Gets or sets the developer-friendly name for this stage.
        /// </summary>
        public string StageName
        {
            get => stageName;
            set => stageName = value;
        }

        /// <summary>
        /// Gets the localized journal entry for this stage.
        /// </summary>
        public LocalizedString JournalEntry => journalEntry;

        /// <summary>
        /// Gets the icon for this stage.
        /// </summary>
        public Sprite StageIcon => stageIcon;

        /// <summary>
        /// Gets the task groups in this stage.
        /// </summary>
        public List<TaskGroup> TaskGroups => taskGroups;

        /// <summary>
        /// Gets the transitions from this stage.
        /// </summary>
        public List<StageTransition> Transitions => transitions;

        /// <summary>
        /// Gets whether this stage is optional.
        /// </summary>
        public bool IsOptional => isOptional;

        /// <summary>
        /// Gets whether this stage is hidden from the journal until reached.
        /// </summary>
        public bool IsHidden => isHidden;

        /// <summary>
        /// Gets whether this stage is terminal (quest ends when completed).
        /// </summary>
        public bool IsTerminal => isTerminal;

        /// <summary>
        /// Gets the total number of tasks across all groups in this stage.
        /// </summary>
        public int TotalTaskCount => taskGroups?.Sum(g => g.TaskCount) ?? 0;

        /// <summary>
        /// Gets whether this stage has any task groups.
        /// </summary>
        public bool HasTaskGroups => taskGroups != null && taskGroups.Count > 0;

        /// <summary>
        /// Gets whether this stage has any transitions.
        /// </summary>
        public bool HasTransitions => transitions != null && transitions.Count > 0;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a default stage from a list of task groups.
        /// Used for migrating legacy quests that don't use stages.
        /// </summary>
        /// <param name="groups">The task groups to include.</param>
        /// <param name="name">Optional stage name.</param>
        /// <returns>A new QuestStage configured for linear progression.</returns>
        public static QuestStage CreateFromTaskGroups(List<TaskGroup> groups, string name = "Main")
        {
            return new QuestStage
            {
                stageIndex = 0,
                stageName = name,
                taskGroups = groups ?? new List<TaskGroup>(),
                isTerminal = true
            };
        }

        /// <summary>
        /// Creates an empty stage with the given index.
        /// </summary>
        /// <param name="index">The stage index.</param>
        /// <param name="name">The stage name.</param>
        /// <returns>A new empty QuestStage.</returns>
        public static QuestStage CreateEmpty(int index, string name)
        {
            return new QuestStage
            {
                stageIndex = index,
                stageName = name,
                taskGroups = new List<TaskGroup>(),
                transitions = new List<StageTransition>()
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the transition with the highest priority that matches the specified trigger
        /// and has all conditions met.
        /// </summary>
        /// <param name="trigger">The trigger type to match.</param>
        /// <returns>The highest priority valid transition, or null if none found.</returns>
        public StageTransition GetValidTransition(TransitionTrigger trigger)
        {
            if (transitions == null || transitions.Count == 0)
                return null;

            return transitions
                .Where(t => t.Trigger == trigger && t.EvaluateConditions())
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets all valid transitions (conditions met) ordered by priority.
        /// </summary>
        /// <returns>List of valid transitions.</returns>
        public List<StageTransition> GetValidTransitions()
        {
            if (transitions == null || transitions.Count == 0)
                return new List<StageTransition>();

            return transitions
                .Where(t => t.EvaluateConditions())
                .OrderByDescending(t => t.Priority)
                .ToList();
        }

        /// <summary>
        /// Validates the stage configuration.
        /// </summary>
        /// <param name="maxStageIndex">The maximum valid stage index in the quest.</param>
        /// <returns>List of validation warnings.</returns>
        public List<string> Validate(int maxStageIndex)
        {
            var warnings = new List<string>();

            if (stageIndex < 0)
            {
                warnings.Add($"Stage '{stageName}' has invalid index: {stageIndex}");
            }

            if (string.IsNullOrEmpty(stageName))
            {
                warnings.Add($"Stage at index {stageIndex} has no name");
            }

            // Validate task groups
            if (taskGroups != null)
            {
                for (int i = 0; i < taskGroups.Count; i++)
                {
                    if (taskGroups[i] == null)
                    {
                        warnings.Add($"Stage '{stageName}' has null task group at index {i}");
                    }
                    else
                    {
                        warnings.AddRange(taskGroups[i].Validate());
                    }
                }
            }

            // Validate transitions
            if (transitions != null)
            {
                foreach (var transition in transitions)
                {
                    if (transition == null)
                    {
                        warnings.Add($"Stage '{stageName}' has null transition");
                    }
                    else
                    {
                        warnings.AddRange(transition.Validate(maxStageIndex));
                    }
                }
            }

            // Check for terminal stage without transitions
            if (!isTerminal && (transitions == null || transitions.Count == 0))
            {
                warnings.Add($"Stage '{stageName}' is not terminal but has no transitions defined");
            }

            return warnings;
        }

        /// <summary>
        /// Adds a transition to the next sequential stage.
        /// Convenience method for linear quest progression.
        /// </summary>
        /// <param name="nextStageIndex">The index of the next stage.</param>
        public void AddSequentialTransition(int nextStageIndex)
        {
            transitions ??= new List<StageTransition>();
            // Note: StageTransition fields are private, so we'd need to add a constructor or factory
            // For now, this is a placeholder - proper implementation would need StageTransition modification
        }

        #endregion

        #region Player Choice Methods

        /// <summary>
        /// Gets whether this stage has any player choice transitions.
        /// </summary>
        public bool HasPlayerChoices =>
            transitions != null && transitions.Any(t => t.IsPlayerChoice);

        /// <summary>
        /// Gets whether this stage requires player to make a choice before progressing.
        /// True if all non-Manual transitions are PlayerChoice type.
        /// </summary>
        public bool RequiresPlayerChoice
        {
            get
            {
                if (transitions == null || transitions.Count == 0)
                    return false;

                // Stage requires choice if it has PlayerChoice transitions
                // and no automatic transitions that could fire instead
                var hasPlayerChoices = transitions.Any(t => t.Trigger == TransitionTrigger.PlayerChoice);
                var hasAutoTransitions = transitions.Any(t =>
                    t.Trigger == TransitionTrigger.OnGroupsComplete ||
                    t.Trigger == TransitionTrigger.OnConditionsMet);

                return hasPlayerChoices && !hasAutoTransitions;
            }
        }

        /// <summary>
        /// Gets all PlayerChoice transitions, regardless of condition state.
        /// Use this to show all possible choices (with some potentially greyed out).
        /// </summary>
        public List<StageTransition> GetAllPlayerChoices()
        {
            if (transitions == null || transitions.Count == 0)
                return new List<StageTransition>();

            return transitions
                .Where(t => t.IsPlayerChoice)
                .OrderByDescending(t => t.Priority)
                .ToList();
        }

        /// <summary>
        /// Gets PlayerChoice transitions that have all conditions met.
        /// Use this to show only currently available choices.
        /// </summary>
        public List<StageTransition> GetAvailablePlayerChoices()
        {
            if (transitions == null || transitions.Count == 0)
                return new List<StageTransition>();

            return transitions
                .Where(t => t.IsPlayerChoice && t.EvaluateConditions())
                .OrderByDescending(t => t.Priority)
                .ToList();
        }

        /// <summary>
        /// Gets a player choice by its ID.
        /// </summary>
        /// <param name="choiceId">The choice ID to find.</param>
        /// <returns>The matching transition, or null if not found.</returns>
        public StageTransition GetPlayerChoiceById(string choiceId)
        {
            if (string.IsNullOrEmpty(choiceId) || transitions == null)
                return null;

            return transitions.FirstOrDefault(t =>
                t.IsPlayerChoice && t.ChoiceId == choiceId);
        }

        /// <summary>
        /// Checks if a specific choice is currently available (conditions met).
        /// </summary>
        /// <param name="choiceId">The choice ID to check.</param>
        /// <returns>True if the choice exists and its conditions are met.</returns>
        public bool IsChoiceAvailable(string choiceId)
        {
            var choice = GetPlayerChoiceById(choiceId);
            return choice != null && choice.EvaluateConditions();
        }

        /// <summary>
        /// Gets the first PlayerChoice transition whose conditions were just met.
        /// This is used for implicit choices triggered by event-driven conditions.
        /// Returns null if no implicit choice is ready.
        /// </summary>
        /// <remarks>
        /// Implicit choices are those where the player's action (buying an item,
        /// completing a task, entering an area, etc.) fulfills the condition,
        /// automatically selecting that branch without explicit UI interaction.
        /// </remarks>
        public StageTransition GetImplicitlySelectedChoice()
        {
            if (transitions == null || transitions.Count == 0)
                return null;

            // Find player choices with met conditions
            // Priority determines which one wins if multiple conditions met simultaneously
            return transitions
                .Where(t => t.IsPlayerChoice && t.EvaluateConditions())
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();
        }

        #endregion
    }
}
