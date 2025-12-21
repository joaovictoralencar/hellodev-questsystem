using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
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
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "HelloDev/Quest System/Scriptable Objects/Quest")]
    public class Quest_SO : RuntimeScriptableObject
    {
        #region Identity

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(0)]
        [Required("Dev Name is required for identification.")]
#else
        [Header("Identity")]
#endif
        [Tooltip("Internal name for developers, used for identification in code.")]
        [SerializeField]
        private string devName;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(1)]
        [ReadOnly]
        [DisplayAsString]
#endif
        [Tooltip("A unique, permanent identifier for this quest. Auto-generated.")]
        [SerializeField]
        private string questId;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(2)]
#endif
        [Tooltip("The type of the quest. Use this to group quests together.")]
        [SerializeField]
        private QuestType_SO questType;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(3)]
#endif
        [Tooltip("The recommended level for the player to start this quest.")]
        [SerializeField]
        private int recommendedLevel = -1;

        #endregion

        #region Display

#if ODIN_INSPECTOR
        [FoldoutGroup("Display", expanded: true)]
        [PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [Tooltip("The localized display name of the quest.")]
        [SerializeField]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(11)]
        [TextArea(3, 6)]
#endif
        [Tooltip("The localized description of the quest.")]
        [SerializeField]
        private LocalizedString questDescription;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(12)]
#endif
        [Tooltip("The localized quest location. Optional.")]
        [SerializeField]
        private LocalizedString questLocation;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(13)]
        [PreviewField(50, ObjectFieldAlignment.Left)]
#endif
        [Tooltip("An icon to display in the UI, representing the quest.")]
        [SerializeField]
        private Sprite questSprite;

        #endregion

        #region Tasks

#if ODIN_INSPECTOR
        [FoldoutGroup("Tasks", expanded: true)]
        [PropertyOrder(20)]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
        [ValidateInput(nameof(ValidateTasks), "One or more tasks are invalid. Check for null or duplicate entries.")]
        [InfoBox("$" + nameof(GetTasksInfoMessage), InfoMessageType.Warning, nameof(HasTaskWarnings))]
#else
        [Header("Tasks")]
#endif
        [Tooltip("The list of tasks that make up this quest. Tasks are executed sequentially.")]
        [SerializeField]
        private List<Task_SO> tasks;

        #endregion

        #region Conditions

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(30)]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Start conditions should be event-driven for automatic quest activation.", InfoMessageType.Info)]
#else
        [Header("Conditions")]
#endif
        [Tooltip("The list of conditions that must be met to start this quest.")]
        [SerializeField]
        private List<Condition_SO> startConditions;

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(31)]
        [ListDrawerSettings(ShowFoldout = true)]
#endif
        [Tooltip("The list of conditions that, when all met, will cause this quest to fail.")]
        [SerializeField]
        private List<Condition_SO> failureConditions;

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(32)]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("When any of these conditions is met, the current in-progress task will fail.", InfoMessageType.Info)]
#endif
        [Tooltip("The list of conditions that, if any is met, will cause the current task to fail.")]
        [SerializeField]
        private List<Condition_SO> globalTaskFailureConditions;

        #endregion

        #region Rewards

#if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        [PropertyOrder(40)]
        [ListDrawerSettings(ShowFoldout = true)]
        [ValidateInput(nameof(ValidateRewards), "One or more rewards are invalid. Check for null types or zero amounts.")]
        [InfoBox("Rewards are automatically distributed when the quest is completed.", InfoMessageType.Info)]
#else
        [Header("Rewards")]
#endif
        [Tooltip("The list of rewards for completing this quest.")]
        [SerializeField]
        private List<RewardInstance> rewards;

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
        /// Gets the list of tasks that compose this quest.
        /// </summary>
        public List<Task_SO> Tasks => tasks;

        /// <summary>
        /// Gets the list of conditions required to start the quest.
        /// </summary>
        public List<Condition_SO> StartConditions => startConditions;

        /// <summary>
        /// Gets the list of conditions that, if any is met, will cause any task to fail.
        /// </summary>
        public List<Condition_SO> FailureConditions => failureConditions;

        /// <summary>
        /// Gets the list of conditions that, when all met, will cause the quest to fail.
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
        public Quest GetRuntimeQuest()
        {
            return new Quest(this);
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
        }

        private void ValidateConfiguration()
        {
            // Validate tasks list
            if (tasks == null || tasks.Count == 0)
            {
                Debug.LogWarning($"[Quest_SO] '{devName}': Tasks list is empty. Quest will complete immediately.", this);
            }
            else
            {
                // Check for null entries
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i] == null)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': Task at index {i} is null.", this);
                    }
                }

                // Check for duplicates
                var seen = new HashSet<Task_SO>();
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i] != null && !seen.Add(tasks[i]))
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': Duplicate task '{tasks[i].DevName}' at index {i}.", this);
                    }
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

#if ODIN_INSPECTOR
        private bool ValidateTasks(List<Task_SO> taskList)
        {
            if (taskList == null || taskList.Count == 0) return true; // Empty is valid for warning, not error
            if (taskList.Any(t => t == null)) return false;
            if (taskList.Count != taskList.Distinct().Count()) return false;
            return true;
        }

        private bool HasTaskWarnings()
        {
            return tasks == null || tasks.Count == 0;
        }

        private string GetTasksInfoMessage()
        {
            if (tasks == null || tasks.Count == 0)
                return "No tasks configured. The quest will complete immediately when started.";
            return string.Empty;
        }

        private bool ValidateRewards(List<RewardInstance> rewardList)
        {
            if (rewardList == null) return true;
            foreach (var reward in rewardList)
            {
                if (reward.RewardType == null) return false;
                if (reward.Amount <= 0) return false;
            }
            return true;
        }
#endif

        #endregion

        #region Editor Buttons

#if ODIN_INSPECTOR
        [ButtonGroup("Actions")]
        [Button("Generate New ID", ButtonSizes.Medium)]
        [PropertyOrder(100)]
#endif
        private void GenerateNewGuid()
        {
            questId = Guid.NewGuid().ToString();
        }

#if ODIN_INSPECTOR
        [ButtonGroup("Actions")]
        [Button("Validate Configuration", ButtonSizes.Medium)]
        [PropertyOrder(100)]
        private void ValidateButton()
        {
            ValidateConfiguration();
            Debug.Log($"[Quest_SO] '{devName}': Validation complete. Check console for warnings.", this);
        }
#endif

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
