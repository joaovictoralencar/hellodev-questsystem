using System.IO;
using HelloDev.QuestSystem.QuestGraph.Editor.Validation;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Commands
{
    /// <summary>
    /// Menu commands for validating Quest Graph assets.
    /// </summary>
    /// <remarks>
    /// Note: Export is handled automatically by the ScriptedImporters.
    /// When a .quest or .questline file is saved, the importer converts it
    /// to Quest_SO or QuestLine_SO automatically.
    ///
    /// These commands provide validation functionality for the generated assets.
    /// </remarks>
    public static class ExportCommands
    {
        private const string MenuPath = "Assets/HelloDev/Quest System/";

        #region Validation Commands

        [MenuItem(MenuPath + "Validate Selected Quest", false, 200)]
        private static void ValidateSelectedQuest()
        {
            var quest = Selection.activeObject as Quest_SO;
            if (quest == null)
            {
                EditorUtility.DisplayDialog("Validation Failed",
                    "Please select a Quest_SO asset.", "OK");
                return;
            }

            // Basic Quest_SO validation
            var issues = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(quest.DevName))
                issues.Add("Quest has no DevName");

            if (quest.Stages == null || quest.Stages.Count == 0)
                issues.Add("Quest has no stages");
            else
            {
                // Check for terminal stage
                bool hasTerminal = false;
                foreach (var stage in quest.Stages)
                {
                    if (stage.IsTerminal)
                    {
                        hasTerminal = true;
                        break;
                    }
                }
                if (!hasTerminal)
                    issues.Add("Quest has no terminal stage");

                // Check for duplicate stage indices
                var indices = new System.Collections.Generic.HashSet<int>();
                foreach (var stage in quest.Stages)
                {
                    if (!indices.Add(stage.StageIndex))
                        issues.Add($"Duplicate stage index: {stage.StageIndex}");
                }
            }

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Passed",
                    $"Quest '{quest.DevName}' is valid.", "OK");
            }
            else
            {
                var message = $"Found {issues.Count} issue(s):\n\n";
                foreach (var issue in issues)
                    message += $"• {issue}\n";

                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }

        [MenuItem(MenuPath + "Validate Selected Quest", true)]
        private static bool ValidateSelectedQuestValidate()
        {
            return Selection.activeObject is Quest_SO;
        }

        [MenuItem(MenuPath + "Validate Selected QuestLine", false, 201)]
        private static void ValidateSelectedQuestLine()
        {
            var questLine = Selection.activeObject as QuestLine_SO;
            if (questLine == null)
            {
                EditorUtility.DisplayDialog("Validation Failed",
                    "Please select a QuestLine_SO asset.", "OK");
                return;
            }

            // Basic QuestLine_SO validation
            var issues = new System.Collections.Generic.List<string>();

            if (string.IsNullOrEmpty(questLine.DevName))
                issues.Add("QuestLine has no DevName");

            if (questLine.Quests == null || questLine.Quests.Count == 0)
                issues.Add("QuestLine has no quests");
            else
            {
                // Check for null quest references
                for (int i = 0; i < questLine.Quests.Count; i++)
                {
                    if (questLine.Quests[i] == null)
                        issues.Add($"Quest at index {i} is null");
                }
            }

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Passed",
                    $"QuestLine '{questLine.DevName}' is valid.", "OK");
            }
            else
            {
                var message = $"Found {issues.Count} issue(s):\n\n";
                foreach (var issue in issues)
                    message += $"• {issue}\n";

                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }

        [MenuItem(MenuPath + "Validate Selected QuestLine", true)]
        private static bool ValidateSelectedQuestLineValidate()
        {
            return Selection.activeObject is QuestLine_SO;
        }

        #endregion

        #region Reimport Commands

        [MenuItem(MenuPath + "Reimport Quest Graph", false, 300)]
        private static void ReimportQuestGraph()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Reimport Failed",
                    "Please select an asset.", "OK");
                return;
            }

            if (path.EndsWith(".quest") || path.EndsWith(".questline"))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Reimported: {path}");
            }
            else
            {
                EditorUtility.DisplayDialog("Reimport Failed",
                    "Please select a .quest or .questline file.", "OK");
            }
        }

        [MenuItem(MenuPath + "Reimport Quest Graph", true)]
        private static bool ReimportQuestGraphValidate()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) &&
                   (path.EndsWith(".quest") || path.EndsWith(".questline"));
        }

        #endregion
    }
}
