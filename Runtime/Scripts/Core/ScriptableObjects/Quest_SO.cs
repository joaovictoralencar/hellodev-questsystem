using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject that represents the static data for a quest.
    /// This is the primary asset for configuring quests in the editor.
    /// Editor UI is handled by the Quest_SO.Odin.cs partial class when Odin is available.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "HelloDev/Quest System/Scriptable Objects/Quest")]
    public partial class Quest_SO : RuntimeScriptableObject
    {
        #region Odin Constants

#if ODIN_INSPECTOR
        private const string TAB_OVERVIEW = "Tabs/Overview";
        private const string TAB_CONFIG = "Tabs/Configuration";
        private const string TAB_VALIDATION = "Tabs/Validation";
#endif

        #endregion

        #region Serialized Fields - Identity

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration", SdfIconType.GearFill, Order = 1)]
        [BoxGroup(TAB_CONFIG + "/Identity")]
        [LabelText("Dev Name"), PropertyOrder(0)]
#endif
        [SerializeField] private string devName;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Identity")]
        [LabelText("Quest ID"), DisplayAsString, PropertyOrder(1)]
#endif
        [SerializeField] private string questId;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Identity")]
        [LabelText("Quest Type"), PropertyOrder(2)]
#endif
        [SerializeField] private QuestType_SO questType;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Identity")]
        [LabelText("Recommended Level"), PropertyOrder(3)]
#endif
        [SerializeField] private int recommendedLevel = -1;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Identity")]
        [Button("Generate New ID", ButtonSizes.Small), PropertyOrder(4)]
        private void GenerateNewIdButton()
        {
            questId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
#endif

        #endregion

        #region Serialized Fields - Display

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [BoxGroup(TAB_CONFIG + "/Display")]
        [PropertyOrder(10)]
#endif
        [SerializeField] private LocalizedString displayName;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Display")]
        [PropertyOrder(11)]
#endif
        [SerializeField] private LocalizedString questDescription;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Display")]
        [PropertyOrder(12)]
#endif
        [SerializeField] private LocalizedString questLocation;

#if ODIN_INSPECTOR
        [BoxGroup(TAB_CONFIG + "/Display")]
        [PreviewField(60, Alignment = ObjectFieldAlignment.Left)]
        [PropertyOrder(13)]
#endif
        [SerializeField] private Sprite questSprite;

        #endregion

        #region Serialized Fields - Task Groups (Legacy)

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
        [PropertyOrder(20)]
        [HideIf(nameof(usesStages))]
        [InfoBox("Task Groups are used for quests without stages. Enable 'Uses Stages' for multi-stage quests.", InfoMessageType.Info)]
#endif
        [SerializeField] private List<TaskGroup> taskGroups = new();

        #endregion

        #region Serialized Fields - Stages

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [PropertyOrder(21)]
        [Tooltip("Enable to use the stage-based quest structure instead of flat task groups.")]
#endif
        [SerializeField] private bool usesStages = false;

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
        [PropertyOrder(22)]
        [ShowIf(nameof(usesStages))]
        [InfoBox("Stages enable Skyrim-style quest structure with multiple phases and branching.", InfoMessageType.Info)]
#endif
        [SerializeField] private List<QuestStage> stages = new();

        #endregion

        #region Serialized Fields - Conditions

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [FoldoutGroup(TAB_CONFIG + "/Conditions", Expanded = true)]
        [LabelText("Start Conditions")]
        [ListDrawerSettings(ShowFoldout = true)]
        [PropertyOrder(30)]
#endif
        [SerializeField] private List<Condition_SO> startConditions;

#if ODIN_INSPECTOR
        [FoldoutGroup(TAB_CONFIG + "/Conditions")]
        [LabelText("Failure Conditions")]
        [ListDrawerSettings(ShowFoldout = true)]
        [PropertyOrder(31)]
#endif
        [SerializeField] private List<Condition_SO> failureConditions;

#if ODIN_INSPECTOR
        [FoldoutGroup(TAB_CONFIG + "/Conditions")]
        [LabelText("Global Task Failure")]
        [ListDrawerSettings(ShowFoldout = true)]
        [PropertyOrder(32)]
#endif
        [SerializeField] private List<Condition_SO> globalTaskFailureConditions;

        #endregion

        #region Serialized Fields - Rewards

#if ODIN_INSPECTOR
        [TabGroup("Tabs", "Configuration")]
        [ListDrawerSettings(ShowFoldout = true)]
        [PropertyOrder(40)]
#endif
        [SerializeField] private List<RewardInstance> rewards;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the developer-friendly name of the quest.
        /// </summary>
        public string DevName => devName;

        /// <summary>
        /// Gets the unique, permanent identifier for this quest.
        /// </summary>
        public Guid QuestId => Guid.Parse(questId);

        /// <summary>
        /// Gets the localized display name of the quest.
        /// </summary>
        public LocalizedString DisplayName => displayName;

        /// <summary>
        /// Gets the localized description of the quest.
        /// </summary>
        public LocalizedString QuestDescription => questDescription;

        /// <summary>
        /// Gets the icon associated with the quest.
        /// </summary>
        public Sprite QuestSprite => questSprite;

        /// <summary>
        /// Gets whether this quest uses stage-based structure.
        /// </summary>
        public bool UsesStages => usesStages;

        /// <summary>
        /// Gets all stages for this quest.
        /// For legacy quests (usesStages=false), returns a single auto-generated stage wrapping task groups.
        /// </summary>
        public List<QuestStage> Stages
        {
            get
            {
                if (usesStages)
                    return stages ?? new List<QuestStage>();

                // Legacy mode: wrap task groups in a single stage
                return new List<QuestStage> { CreateLegacyStage() };
            }
        }

        /// <summary>
        /// Gets the list of task groups for this quest.
        /// For stage-based quests, returns task groups from all stages.
        /// </summary>
        public List<TaskGroup> TaskGroups
        {
            get
            {
                if (usesStages)
                {
                    // Return all task groups from all stages
                    return stages?.SelectMany(s => s.TaskGroups).ToList() ?? new List<TaskGroup>();
                }
                return taskGroups ?? new List<TaskGroup>();
            }
        }

        /// <summary>
        /// Gets all tasks across all groups (flattened list).
        /// Use this when you need all tasks regardless of grouping.
        /// </summary>
        public List<Task_SO> AllTasks => TaskGroups?.SelectMany(g => g.Tasks).Where(t => t != null).ToList() ?? new List<Task_SO>();

        /// <summary>
        /// Gets the list of conditions required to start the quest.
        /// </summary>
        public List<Condition_SO> StartConditions => startConditions;

        /// <summary>
        /// Gets the list of conditions that, when all met, will cause the quest to fail.
        /// </summary>
        public List<Condition_SO> FailureConditions => failureConditions;

        /// <summary>
        /// Gets the list of conditions that, if any is met, will cause any task to fail.
        /// </summary>
        public List<Condition_SO> GlobalTaskFailureConditions => globalTaskFailureConditions;

        /// <summary>
        /// Gets the type of the quest.
        /// </summary>
        public QuestType_SO QuestType => questType;

        /// <summary>
        /// Gets the localized location of the quest.
        /// </summary>
        public LocalizedString QuestLocation => questLocation;

        /// <summary>
        /// Gets the recommended level for the player to start this quest.
        /// </summary>
        public int RecommendedLevel => recommendedLevel;

        /// <summary>
        /// Gets the list of rewards for this quest.
        /// </summary>
        public List<RewardInstance> Rewards => rewards;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and returns a new runtime instance of this quest.
        /// </summary>
        public QuestRuntime GetRuntimeQuest()
        {
            return new QuestRuntime(this);
        }

        /// <summary>
        /// Migrates this quest from flat task groups to stage-based structure.
        /// Creates a single stage containing all existing task groups.
        /// </summary>
        public void MigrateToStages()
        {
            if (usesStages)
            {
                Debug.LogWarning($"[Quest_SO] '{devName}': Already using stages.", this);
                return;
            }

            // Create a single stage from existing task groups
            var mainStage = QuestStage.CreateFromTaskGroups(taskGroups, "Main");
            stages = new List<QuestStage> { mainStage };
            usesStages = true;

            // Clear legacy task groups (data is now in stage)
            taskGroups = new List<TaskGroup>();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif

            Debug.Log($"[Quest_SO] '{devName}': Migrated to stages. Created 1 stage with {mainStage.TotalTaskCount} tasks.", this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a temporary legacy stage for backward compatibility.
        /// Used when usesStages=false to provide a unified stage interface.
        /// </summary>
        private QuestStage CreateLegacyStage()
        {
            return QuestStage.CreateFromTaskGroups(taskGroups, "Main");
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questId))
            {
                GenerateNewGuid();
            }

            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }

            ValidateConfiguration();

            if (usesStages)
            {
                ValidateStages();
            }
            else
            {
                ValidateTaskGroups();
            }
        }

        private void GenerateNewGuid()
        {
            questId = Guid.NewGuid().ToString();
        }

        private void ValidateTaskGroups()
        {
            if (taskGroups == null) return;

            foreach (var group in taskGroups)
            {
                var warnings = group.Validate();
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[Quest_SO] '{devName}': {warning}", this);
                }
            }
        }

        private void ValidateStages()
        {
            if (stages == null || stages.Count == 0)
            {
                Debug.LogWarning($"[Quest_SO] '{devName}': No stages configured. Quest will complete immediately.", this);
                return;
            }

            int maxStageIndex = stages.Max(s => s.StageIndex);

            foreach (var stage in stages)
            {
                var warnings = stage.Validate(maxStageIndex);
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[Quest_SO] '{devName}': {warning}", this);
                }
            }

            // Check for duplicate stage indices
            var duplicateIndices = stages.GroupBy(s => s.StageIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var index in duplicateIndices)
            {
                Debug.LogWarning($"[Quest_SO] '{devName}': Duplicate stage index: {index}", this);
            }
        }

        private void ValidateConfiguration()
        {
            // Validate task groups or stages have content
            if (usesStages)
            {
                if (stages == null || stages.Count == 0)
                {
                    Debug.LogWarning($"[Quest_SO] '{devName}': No stages configured. Quest will complete immediately.", this);
                }
            }
            else
            {
                if (taskGroups == null || taskGroups.Count == 0)
                {
                    Debug.LogWarning($"[Quest_SO] '{devName}': No task groups configured. Quest will complete immediately.", this);
                }
            }

            // Validate start conditions
            if (startConditions != null)
            {
                for (int i = 0; i < startConditions.Count; i++)
                {
                    if (startConditions[i] == null)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': StartCondition at index {i} is null.", this);
                    }
                    else if (startConditions[i] is not IConditionEventDriven)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': StartCondition '{startConditions[i].name}' is not event-driven. Quest may not auto-start.", this);
                    }
                }
            }

            // Validate rewards
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].RewardType == null)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': Reward at index {i} has null RewardType.", this);
                    }
                    else if (rewards[i].Amount <= 0)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': Reward '{rewards[i].RewardType.name}' has invalid amount ({rewards[i].Amount}).", this);
                    }
                }
            }
        }

        #endregion

        #region Unity Callbacks

        protected override void OnScriptableObjectReset()
        {
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is Quest_SO other)
            {
                return QuestId == other.QuestId;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return QuestId.GetHashCode();
        }

        #endregion
    }
}
