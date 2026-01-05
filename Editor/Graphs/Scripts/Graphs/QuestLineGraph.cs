using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Graph for designing QuestLines - collections of related quests.
    /// This is the highest-level graph type.
    /// </summary>
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class QuestLineGraph : Graph
    {
        // Note: Extension WITHOUT dot - Unity adds it automatically for the importer
        public const string AssetExtension = "questline";

        #region Serialized Data - Identity

        [SerializeField] private string devName;
        [SerializeField] private string questLineId;

        #endregion

        #region Serialized Data - Display

        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;

        #endregion

        #region Serialized Data - Settings

        [SerializeField] private bool requireSequentialCompletion = true;
        [SerializeField] private bool failOnAnyQuestFailed = false;

        #endregion

        #region Serialized Data - Chaining

        [SerializeField] private QuestLine_SO prerequisiteLine;

        #endregion

        #region Serialized Data - Rewards

        [SerializeField] private List<RewardInstance> completionRewards = new();

        #endregion

        #region Serialized Data - Export

        // Reference to the generated QuestLine_SO (for export)
        [SerializeField] private QuestLine_SO targetAsset;

        #endregion

        #region Properties

        public string DevName
        {
            get => devName;
            set => devName = value;
        }

        public string QuestLineId => questLineId;

        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;

        public bool RequireSequentialCompletion
        {
            get => requireSequentialCompletion;
            set => requireSequentialCompletion = value;
        }

        public bool FailOnAnyQuestFailed
        {
            get => failOnAnyQuestFailed;
            set => failOnAnyQuestFailed = value;
        }

        public QuestLine_SO PrerequisiteLine => prerequisiteLine;
        public List<RewardInstance> CompletionRewards => completionRewards;

        public QuestLine_SO TargetAsset
        {
            get => targetAsset;
            set => targetAsset = value;
        }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/QuestLine Graph", false, 100)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<QuestLineGraph>();
        }

        #endregion

        #region Lifecycle

        public override void OnEnable()
        {
            base.OnEnable();
            // Initialize default values
            if (string.IsNullOrEmpty(devName))
            {
                devName = name;
            }

            if (string.IsNullOrEmpty(questLineId))
            {
                questLineId = Guid.NewGuid().ToString();
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
            var results = validationService.ValidateQuestLineGraph(this);

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
                        UnityEngine.Debug.Log($"[QuestLineGraph] {result.Message}");
                        break;
                }
            }
        }

        #endregion
    }
}
