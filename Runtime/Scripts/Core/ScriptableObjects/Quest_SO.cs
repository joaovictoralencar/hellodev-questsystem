using System;
using System.Collections.Generic;
using HelloDev.Conditions;
using UnityEngine;
using UnityEngine.Localization;
using HelloDev.QuestSystem.Quests;
using HelloDev.Utils;
using Sirenix.OdinInspector;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject that represents the static data for a quest.
    /// This is the primary asset for configuring quests in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "HelloDev/Quest System/Scriptable Objects/Quest")]
    public class Quest_SO : RuntimeScriptableObject
    {
        [Header("Core Info")]
        [Tooltip("Internal name for developers, used for identification in code.")]
        [SerializeField]
        private string devName;
        
        [Tooltip("A unique, permanent identifier for this quest. Auto-generated.")]
        [SerializeField, ReadOnly]
        private string questId;

        [Header("Content")]
        [Tooltip("The localized display name of the quest.")]
        [SerializeField]
        private LocalizedString displayName;
        
        [Tooltip("The localized description of the quest.")]
        [SerializeField]
        private LocalizedString questDescription;
        
        [Tooltip("The localized quest location. Optional.")]
        [SerializeField] private LocalizedString questLocation; 
        
        [Tooltip("An icon to display in the UI, representing the quest.")]
        [SerializeField]
        private Sprite questSprite;

        [Header("Structure")]
        [Tooltip("The list of tasks that make up this quest.")]
        [SerializeField]
        private List<Task_SO> tasks;
        
        [Tooltip("The list of conditions that must be met to start this quest.")]
        [SerializeField]
        private List<Condition_SO> startConditions;
        
        [Tooltip("The list of conditions that, when all met, will cause this quest to fail.")]
        [SerializeField]
        private List<Condition_SO> failureConditions;
        
        [Tooltip("The list of conditions that, if any is met, will cause any task to fail.")]
        [SerializeField]
        private List<Condition_SO> globalTaskFailureConditions;
        
        [Tooltip("The type of the quest. Use this to group quests together.")]
        [SerializeField] private QuestType_SO questType; 
        
        [Tooltip("The list of rewards for completing this quest.")]
        [SerializeField] private List<RewardInstance> rewards;
        
        [Tooltip("The recommended level for the player to start this quest.")]
        [SerializeField]
        private int recommendedLevel = -1;

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

        /// <summary>
        /// Creates and returns a new runtime instance of this quest.
        /// </summary>
        public Quest GetRuntimeQuest()
        {
            return new Quest(this);
        }
        
        /// <summary>
        /// Called when the script is loaded or a value is changed in the Inspector.
        /// Ensures the quest has a unique ID and a default dev name.
        /// </summary>
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
        }

        [Button]
        private void GenerateNewGuid()
        {
            questId = Guid.NewGuid().ToString();
        }

        protected override void OnScriptableObjectReset()
        {
        }
        
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
    }
}