using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Subgraph for designing individual Quest Stages.
    /// Contains task groups and transition logic.
    /// Can be reused across multiple quests.
    /// </summary>
    [Subgraph(typeof(QuestGraph))]
    [Graph(AssetExtension, GraphOptions.SupportsSubgraphs)]
    [Serializable]
    public class StageGraph : Graph
    {
        public const string AssetExtension = "stage";

        #region Serialized Data - Identity

        [SerializeField] private int stageIndex;
        [SerializeField] private string stageName = "New Stage";

        #endregion

        #region Serialized Data - Display

        [SerializeField] private LocalizedString journalEntry;
        [SerializeField] private Sprite stageIcon;

        #endregion

        #region Serialized Data - Flags

        [SerializeField] private bool isTerminal;
        [SerializeField] private bool isOptional;
        [SerializeField] private bool isHidden;

        #endregion

        #region Properties

        public int StageIndex
        {
            get => stageIndex;
            set => stageIndex = value;
        }

        public string StageName
        {
            get => stageName;
            set => stageName = value;
        }

        public LocalizedString JournalEntry => journalEntry;
        public Sprite StageIcon => stageIcon;

        public bool IsTerminal
        {
            get => isTerminal;
            set => isTerminal = value;
        }

        public bool IsOptional
        {
            get => isOptional;
            set => isOptional = value;
        }

        public bool IsHidden
        {
            get => isHidden;
            set => isHidden = value;
        }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/Stage Subgraph", false, 102)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<StageGraph>();
        }

        #endregion

        #region Lifecycle

        public override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(stageName))
            {
                stageName = name;
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
            var results = validationService.ValidateStageGraph(this);

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
                        UnityEngine.Debug.Log($"[StageGraph] {result.Message}");
                        break;
                }
            }
        }

        #endregion
    }
}
