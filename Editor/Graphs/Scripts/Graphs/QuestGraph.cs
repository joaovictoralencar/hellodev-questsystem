using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Graph for designing individual Quests with stages and branching.
    /// Can be used as a subgraph in QuestLineGraph.
    /// </summary>
    [Subgraph(typeof(QuestLineGraph))]
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestGraph : Graph
    {
        public const string AssetExtension = "quest";

        #region Serialized Data - Identity

        [SerializeField] private string devName;
        [SerializeField] private string questId;
        [SerializeField] private QuestType_SO questType;
        [SerializeField] private int recommendedLevel = -1;

        #endregion

        #region Serialized Data - Display

        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString questDescription;
        [SerializeField] private LocalizedString questLocation;
        [SerializeField] private Sprite questSprite;

        #endregion

        #region Serialized Data - Conditions

        [SerializeField] private List<Condition_SO> startConditions = new();
        [SerializeField] private List<Condition_SO> failureConditions = new();

        #endregion

        #region Serialized Data - Rewards

        [SerializeField] private List<RewardInstance> rewards = new();

        #endregion

        #region Serialized Data - Export

        // Reference to the generated Quest_SO (for export)
        [SerializeField] private Quest_SO targetAsset;

        #endregion

        #region Properties

        public string DevName
        {
            get => devName;
            set => devName = value;
        }

        public string QuestId => questId;

        public QuestType_SO QuestType
        {
            get => questType;
            set => questType = value;
        }

        public int RecommendedLevel => recommendedLevel;
        public LocalizedString DisplayName => displayName;
        public LocalizedString QuestDescription => questDescription;
        public LocalizedString QuestLocation => questLocation;
        public Sprite QuestSprite => questSprite;
        public List<Condition_SO> StartConditions => startConditions;
        public List<Condition_SO> FailureConditions => failureConditions;
        public List<RewardInstance> Rewards => rewards;

        public Quest_SO TargetAsset
        {
            get => targetAsset;
            set => targetAsset = value;
        }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/Quest Graph", false, 101)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<QuestGraph>();
        }

        #endregion

        #region Lifecycle

        public override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }

            if (string.IsNullOrEmpty(questId))
            {
                questId = Guid.NewGuid().ToString();
            }
        }

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            ValidateGraph(logger);
        }

        #endregion

        #region Validation

        private void ValidateGraph(GraphLogger logger)
        {
            var validationService = new GraphValidationService();
            var results = validationService.ValidateQuestGraph(this);

            // Also check reachability
            var reachabilityAnalyzer = new GraphReachabilityAnalyzer();
            results.AddRange(reachabilityAnalyzer.ValidateReachability(this));

            // Report results to GraphLogger
            foreach (var result in results)
            {
                switch (result.Severity)
                {
                    case ValidationSeverity.Error:
                        logger.LogError(result.Message, result.RelatedNode ?? (object)this);
                        break;
                    case ValidationSeverity.Warning:
                        logger.LogWarning(result.Message, result.RelatedNode ?? (object)this);
                        break;
                    case ValidationSeverity.Info:
                        // GraphLogger doesn't have LogInfo, log to Unity console instead
                        UnityEngine.Debug.Log($"[QuestGraph] {result.Message}");
                        break;
                }
            }
        }

        #endregion
    }
}
