using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject that represents a QuestLine - a narrative grouping of related quests.
    /// Unlike quest chains (execution dependencies via ConditionQuestState_SO), a QuestLine is
    /// a thematic container that groups quests belonging to the same storyline.
    /// </summary>
    /// <remarks>
    /// AAA Examples:
    /// - Skyrim: "Companions Questline", "Thieves Guild Questline"
    /// - Witcher 3: Story "threads" within narrative phases
    /// - Cyberpunk 2077: Character arcs (Panam's arc, Judy's arc)
    /// </remarks>
    [CreateAssetMenu(fileName = "NewQuestLine", menuName = "HelloDev/Quest System/Scriptable Objects/Quest Line")]
    public partial class QuestLine_SO : RuntimeScriptableObject
    {
        #region Serialized Fields - Identity

#if ODIN_INSPECTOR
        [TitleGroup("Identity")]
        [LabelText("Dev Name"), PropertyOrder(0)]
#else
        [Header("Identity")]
#endif
        [SerializeField] private string devName;

#if ODIN_INSPECTOR
        [TitleGroup("Identity")]
        [LabelText("Quest Line ID"), DisplayAsString, PropertyOrder(1)]
#endif
        [SerializeField] private string questLineId;

#if ODIN_INSPECTOR
        [TitleGroup("Identity")]
        [Button("Generate New ID", ButtonSizes.Small), PropertyOrder(2)]
        private void GenerateNewIdButton()
        {
            questLineId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
#endif

        #endregion

        #region Serialized Fields - Display

#if ODIN_INSPECTOR
        [TitleGroup("Display")]
        [PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [SerializeField] private LocalizedString displayName;

#if ODIN_INSPECTOR
        [TitleGroup("Display")]
        [TextArea(3, 5)]
        [PropertyOrder(11)]
#endif
        [SerializeField] private LocalizedString description;

#if ODIN_INSPECTOR
        [TitleGroup("Display")]
        [PreviewField(60, Alignment = ObjectFieldAlignment.Left)]
        [PropertyOrder(12)]
#endif
        [SerializeField] private Sprite icon;

        #endregion

        #region Serialized Fields - Quests

#if ODIN_INSPECTOR
        [TitleGroup("Quests")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
        [InfoBox("$" + nameof(GetQuestListInfo), InfoMessageType.Info)]
        [PropertyOrder(20)]
#else
        [Header("Quests")]
#endif
        [Tooltip("Ordered list of quests in this questline")]
        [SerializeField] private List<Quest_SO> quests = new();

#if ODIN_INSPECTOR
        [TitleGroup("Quests")]
        [ToggleLeft]
        [PropertyOrder(21)]
#endif
        [Tooltip("If true, quests must be completed in order (each quest's startConditions should enforce this)")]
        [SerializeField] private bool requireSequentialCompletion = true;

        #endregion

        #region Serialized Fields - Prerequisites

#if ODIN_INSPECTOR
        [TitleGroup("Prerequisites")]
        [PropertyOrder(30)]
#else
        [Header("Prerequisites")]
#endif
        [Tooltip("Optional: Another questline that must be completed before this one becomes available")]
        [SerializeField] private QuestLine_SO prerequisiteLine;

        #endregion

        #region Serialized Fields - Failure Behavior

#if ODIN_INSPECTOR
        [TitleGroup("Failure Behavior")]
        [ToggleLeft]
        [PropertyOrder(35)]
#else
        [Header("Failure Behavior")]
#endif
        [Tooltip("If true, failing any quest in the line fails the entire questline")]
        [SerializeField] private bool failOnAnyQuestFailed = false;

        #endregion

        #region Serialized Fields - Rewards

#if ODIN_INSPECTOR
        [TitleGroup("Completion Rewards")]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Bonus rewards given when ALL quests in the line are completed")]
        [PropertyOrder(40)]
#else
        [Header("Completion Rewards")]
#endif
        [SerializeField] private List<RewardInstance> completionRewards = new();

        #endregion

        #region Properties

        /// <summary>Gets the developer-friendly name of the questline.</summary>
        public string DevName => devName;

        /// <summary>Gets the unique, permanent identifier for this questline.</summary>
        public Guid QuestLineId => Guid.Parse(questLineId);

        /// <summary>Gets the localized display name of the questline.</summary>
        public LocalizedString DisplayName => displayName;

        /// <summary>Gets the localized description of the questline.</summary>
        public LocalizedString Description => description;

        /// <summary>Gets the icon associated with the questline.</summary>
        public Sprite Icon => icon;

        /// <summary>Gets the ordered list of quests in this questline.</summary>
        public IReadOnlyList<Quest_SO> Quests => quests;

        /// <summary>Gets whether quests must be completed in order.</summary>
        public bool RequireSequentialCompletion => requireSequentialCompletion;

        /// <summary>Gets the prerequisite questline (if any).</summary>
        public QuestLine_SO PrerequisiteLine => prerequisiteLine;

        /// <summary>Gets whether failing any quest fails the entire line.</summary>
        public bool FailOnAnyQuestFailed => failOnAnyQuestFailed;

        /// <summary>Gets the completion rewards for this questline.</summary>
        public List<RewardInstance> CompletionRewards => completionRewards;

        /// <summary>Gets the number of quests in this questline.</summary>
        public int QuestCount => quests?.Count ?? 0;

        #endregion

        #region Factory

        /// <summary>
        /// Creates and returns a new runtime instance of this questline.
        /// </summary>
        public QuestLineRuntime GetRuntimeQuestLine()
        {
            return new QuestLineRuntime(this);
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questLineId))
            {
                GenerateNewGuid();
            }

            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }

            ValidateConfiguration();
        }

        private void GenerateNewGuid()
        {
            questLineId = Guid.NewGuid().ToString();
        }

        private void ValidateConfiguration()
        {
            // Validate quests
            if (quests == null || quests.Count == 0)
            {
                Debug.LogWarning($"[QuestLine_SO] '{devName}': No quests configured.", this);
                return;
            }

            // Check for null quests
            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i] == null)
                {
                    Debug.LogWarning($"[QuestLine_SO] '{devName}': Quest at index {i} is null.", this);
                }
            }

            // Check for duplicate quests
            var seen = new HashSet<Guid>();
            foreach (var quest in quests)
            {
                if (quest != null && !seen.Add(quest.QuestId))
                {
                    Debug.LogWarning($"[QuestLine_SO] '{devName}': Duplicate quest '{quest.DevName}'.", this);
                }
            }

            // Check for circular prerequisite
            if (prerequisiteLine != null)
            {
                var visited = new HashSet<Guid> { QuestLineId };
                var current = prerequisiteLine;
                while (current != null)
                {
                    if (visited.Contains(current.QuestLineId))
                    {
                        Debug.LogError($"[QuestLine_SO] '{devName}': Circular prerequisite detected!", this);
                        break;
                    }
                    visited.Add(current.QuestLineId);
                    current = current.PrerequisiteLine;
                }
            }

            // Validate rewards
            if (completionRewards != null)
            {
                for (int i = 0; i < completionRewards.Count; i++)
                {
                    if (completionRewards[i].RewardType == null)
                    {
                        Debug.LogWarning($"[QuestLine_SO] '{devName}': Reward at index {i} has null RewardType.", this);
                    }
                    else if (completionRewards[i].Amount <= 0)
                    {
                        Debug.LogWarning($"[QuestLine_SO] '{devName}': Reward '{completionRewards[i].RewardType.name}' has invalid amount.", this);
                    }
                }
            }
        }

        #endregion

        #region Odin Helper Methods

#if ODIN_INSPECTOR
        private string GetQuestListInfo()
        {
            if (quests == null || quests.Count == 0)
                return "No quests added yet";

            int validCount = 0;
            foreach (var q in quests)
            {
                if (q != null) validCount++;
            }

            return $"{validCount} quest(s) in this questline";
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
            if (obj is QuestLine_SO other)
            {
                return QuestLineId == other.QuestLineId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return QuestLineId.GetHashCode();
        }

        #endregion
    }
}
