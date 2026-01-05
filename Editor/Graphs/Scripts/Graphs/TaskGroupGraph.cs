using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using HelloDev.QuestSystem.TaskGroups;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// Subgraph for designing Task Groups - collections of tasks with execution modes.
    /// This is a highly reusable component that can be embedded in multiple stages.
    /// </summary>
    [Subgraph(typeof(StageGraph))]
    [Graph(AssetExtension)]
    [Serializable]
    public class TaskGroupGraph : Graph
    {
        public const string AssetExtension = "taskgroup";

        #region Serialized Data

        [SerializeField] private string groupName = "Task Group";
        [SerializeField] private TaskExecutionMode executionMode = TaskExecutionMode.Sequential;
        [SerializeField] private int requiredCount = 1;

        #endregion

        #region Properties

        public string GroupName
        {
            get => groupName;
            set => groupName = value;
        }

        public TaskExecutionMode ExecutionMode
        {
            get => executionMode;
            set => executionMode = value;
        }

        public int RequiredCount
        {
            get => requiredCount;
            set => requiredCount = value;
        }

        #endregion

        #region Menu Item

        [MenuItem("Assets/Create/HelloDev/Quest System/Graphs/TaskGroup Subgraph", false, 103)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<TaskGroupGraph>();
        }

        #endregion

        #region Lifecycle

        public override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = name;
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
            var results = validationService.ValidateTaskGroupGraph(this);

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
                        UnityEngine.Debug.Log($"[TaskGroupGraph] {result.Message}");
                        break;
                }
            }
        }

        #endregion
    }
}
