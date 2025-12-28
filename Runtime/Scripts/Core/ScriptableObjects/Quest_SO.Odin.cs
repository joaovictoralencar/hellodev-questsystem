#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using UnityEngine;
using HelloDev.QuestSystem.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.Stages;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.Conditions;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    public partial class Quest_SO
    {
        #region Overview Tab

        [TabGroup("Tabs", "Overview", SdfIconType.Eye, Order = 0)]
        [OnInspectorGUI("DrawHeader")]
        [PropertyOrder(-100)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _headerPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawDashboard")]
        [PropertyOrder(-90)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _dashboardPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawPrerequisites")]
        [PropertyOrder(-80)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _prerequisitesPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawRewardsSection")]
        [PropertyOrder(-70)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _rewardsPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawTaskGroupsSection")]
        [PropertyOrder(-60)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _taskGroupsPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawConditionsSection")]
        [PropertyOrder(-50)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _conditionsPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawQuestLinesSection")]
        [PropertyOrder(-40)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _questLinesPlaceholder => "";

        #endregion

        #region Quick Actions Tab

        [TabGroup("Tabs", "Quick Actions", SdfIconType.Lightning, Order = 2)]
        [OnInspectorGUI("DrawQuickActionsSection")]
        [PropertyOrder(0)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _quickActionsPlaceholder => "";

        #endregion

        #region Validation Tab

        [TabGroup("Tabs", "Validation", SdfIconType.CheckCircle, Order = 3)]
        [OnInspectorGUI("DrawValidationSection")]
        [PropertyOrder(0)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _validationPlaceholder => "";

        #endregion

#if UNITY_EDITOR
        #region Editor Styles

        private static class Styles
        {
            public static readonly GUIStyle Tag;
            public static readonly GUIStyle Title;
            public static readonly GUIStyle Subtitle;
            public static readonly GUIStyle StatValue;
            public static readonly GUIStyle StatLabel;
            public static readonly GUIStyle SectionHeader;
            public static readonly GUIStyle ItemName;
            public static readonly GUIStyle ItemDetail;

            static Styles()
            {
                Tag = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(10, 10, 4, 4),
                    normal = { textColor = Color.white }
                };

                Title = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(0, 0, 4, 2)
                };

                Subtitle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 9,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };

                StatValue = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                StatLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };

                SectionHeader = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    margin = new RectOffset(0, 0, 4, 4),
                    padding = new RectOffset(0, 0, 0, 0),
                    fixedHeight = 20
                };

                ItemName = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };

                ItemDetail = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 9,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader()
        {
            var accentColor = questType != null ? questType.Color : new Color(0.3f, 0.3f, 0.3f);

            // Color accent line at top
            var lineRect = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, accentColor);

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            // Sprite
            if (questSprite != null)
            {
                var spriteRect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80));
                GUI.DrawTexture(spriteRect, questSprite.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Space(80);
            }

            GUILayout.Space(12);

            // Info section
            EditorGUILayout.BeginVertical();

            // Row 1: Tags
            EditorGUILayout.BeginHorizontal();
            if (questType != null)
            {
                DrawTag(questType.DevName, accentColor);
                GUILayout.Space(6);
            }
            if (recommendedLevel > 0)
            {
                DrawTag($"Lv.{recommendedLevel}", new Color(0.35f, 0.35f, 0.35f));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Row 2: Quest Name
            var displayNameText = string.IsNullOrEmpty(devName) ? "Unnamed Quest" : devName;
            EditorGUILayout.LabelField(displayNameText, Styles.Title);

            // Row 3: GUID
            EditorGUILayout.LabelField(questId ?? "No GUID", Styles.Subtitle);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
        }

        private void DrawDashboard()
        {
            var bgColor = new Color(0.18f, 0.18f, 0.18f);
            var cardBg = new Color(0.22f, 0.22f, 0.22f);

            // Background
            var bgRect = GUILayoutUtility.GetRect(0, 70, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bgRect, bgColor);

            // Calculate card positions
            float cardWidth = (bgRect.width - 40) / 4f;
            float cardHeight = 54;
            float startX = bgRect.x + 10;
            float startY = bgRect.y + 8;

            // Stats data
            int taskCount = AllTasks?.Count ?? 0;
            int startCondCount = startConditions?.Count ?? 0;
            int failCondCount = failureConditions?.Count ?? 0;
            int globalFailCount = globalTaskFailureConditions?.Count ?? 0;
            int totalConditions = startCondCount + failCondCount + globalFailCount;
            int rewardCount = rewards?.Count ?? 0;
            int stageCount = stages?.Count ?? 0;

            // Draw stat cards
            DrawStatCard(new Rect(startX, startY, cardWidth - 5, cardHeight), cardBg,
                taskCount.ToString(), "Tasks", new Color(0.4f, 0.7f, 1f));

            DrawStatCard(new Rect(startX + cardWidth, startY, cardWidth - 5, cardHeight), cardBg,
                stageCount.ToString(), "Stages", new Color(0.6f, 0.5f, 0.9f));

            DrawStatCard(new Rect(startX + cardWidth * 2, startY, cardWidth - 5, cardHeight), cardBg,
                totalConditions.ToString(), "Conditions", new Color(0.5f, 0.8f, 0.5f));

            DrawStatCard(new Rect(startX + cardWidth * 3, startY, cardWidth - 5, cardHeight), cardBg,
                rewardCount.ToString(), "Rewards", new Color(1f, 0.8f, 0.3f));

            GUILayout.Space(4);
        }

        private void DrawPrerequisites()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Prerequisites", new Color(0.6f, 0.85f, 1f));

            // Find quest state conditions in start conditions
            var questConditions = new System.Collections.Generic.List<ConditionQuestState_SO>();
            if (startConditions != null)
            {
                foreach (var condition in startConditions)
                {
                    if (condition is ConditionQuestState_SO questCond)
                    {
                        questConditions.Add(questCond);
                    }
                    else if (condition is CompositeCondition_SO composite)
                    {
                        FindQuestConditionsRecursive(composite, questConditions);
                    }
                }
            }

            if (questConditions.Count == 0)
            {
                DrawEmptyState("No prerequisites - quest available immediately");
                return;
            }

            var cardBg = new Color(0.2f, 0.2f, 0.2f);

            foreach (var questCond in questConditions)
            {
                if (questCond.QuestToCheck == null) continue;

                var prereqQuest = questCond.QuestToCheck;

                // Get available width for responsive calculations
                float availableWidth = EditorGUIUtility.currentViewWidth - 40f; // Account for margins

                // Responsive breakpoints
                bool isNarrow = availableWidth < 350f;
                bool isWide = availableWidth > 500f;

                // Calculate responsive dimensions
                float iconSize = isNarrow ? 32f : (isWide ? 48f : 40f);
                float rowHeight = isNarrow ? 60f : 50f;
                float padding = 8f;
                float spacing = isNarrow ? 6f : 10f;

                // Card background
                var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
                DrawRoundedRect(rowRect, cardBg, 4f);

                // Calculate content area (inside padding)
                float contentX = rowRect.x + padding;
                float contentWidth = rowRect.width - (padding * 2);

                // Icon (left side)
                var iconRect = new Rect(contentX, rowRect.y + (rowHeight - iconSize) / 2f, iconSize, iconSize);
                if (prereqQuest.QuestSprite != null)
                {
                    GUI.DrawTexture(iconRect, prereqQuest.QuestSprite.texture, ScaleMode.ScaleToFit);
                }

                // Calculate remaining width after icon
                float afterIconX = iconRect.xMax + spacing;
                float remainingWidth = contentX + contentWidth - afterIconX;

                if (isNarrow)
                {
                    // Narrow layout: Stack vertically
                    // Row 1: Name (full width)
                    var nameRect = new Rect(afterIconX, rowRect.y + 6f, remainingWidth, 16f);
                    GUI.Label(nameRect, prereqQuest.DevName, Styles.ItemName);

                    // Row 2: Tag + Object field side by side
                    var conditionText = GetConditionDisplayText(questCond);
                    var conditionColor = GetConditionColor(questCond.TargetState);

                    float tagWidth = Mathf.Min(80f, remainingWidth * 0.35f);
                    var tagRect = new Rect(afterIconX, rowRect.y + 24f, tagWidth, 14f);
                    DrawMiniTag(tagRect, conditionText, conditionColor);

                    float fieldWidth = remainingWidth - tagWidth - spacing;
                    var fieldRect = new Rect(tagRect.xMax + spacing, rowRect.y + 22f, fieldWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.ObjectField(fieldRect, prereqQuest, typeof(Quest_SO), false);
                }
                else
                {
                    // Wide/Medium layout: Horizontal flow with flexible object field
                    // Left section: Name + Tag (takes minimum needed space)
                    float leftSectionWidth = Mathf.Min(remainingWidth * 0.5f, 200f);

                    var nameRect = new Rect(afterIconX, rowRect.y + 8f, leftSectionWidth, 16f);
                    GUI.Label(nameRect, prereqQuest.DevName, Styles.ItemName);

                    var conditionText = GetConditionDisplayText(questCond);
                    var conditionColor = GetConditionColor(questCond.TargetState);

                    float tagWidth = Mathf.Min(100f, leftSectionWidth);
                    var tagRect = new Rect(afterIconX, rowRect.y + 26f, tagWidth, 14f);
                    DrawMiniTag(tagRect, conditionText, conditionColor);

                    // Right section: Object field (takes remaining space with max width)
                    float fieldMaxWidth = isWide ? 300f : 200f;
                    float fieldWidth = Mathf.Min(fieldMaxWidth, remainingWidth - leftSectionWidth - spacing);
                    fieldWidth = Mathf.Max(fieldWidth, 100f); // Minimum usable width

                    float fieldX = rowRect.xMax - padding - fieldWidth;
                    var fieldRect = new Rect(fieldX, rowRect.y + (rowHeight - EditorGUIUtility.singleLineHeight) / 2f,
                                            fieldWidth, EditorGUIUtility.singleLineHeight);
                    EditorGUI.ObjectField(fieldRect, prereqQuest, typeof(Quest_SO), false);
                }

                GUILayout.Space(4);
            }
        }

        private void FindQuestConditionsRecursive(CompositeCondition_SO composite, System.Collections.Generic.List<ConditionQuestState_SO> results)
        {
            if (composite.Conditions == null) return;

            foreach (var condition in composite.Conditions)
            {
                if (condition is ConditionQuestState_SO questCond)
                {
                    results.Add(questCond);
                }
                else if (condition is CompositeCondition_SO nestedComposite)
                {
                    FindQuestConditionsRecursive(nestedComposite, results);
                }
            }
        }

        private string GetConditionDisplayText(ConditionQuestState_SO cond)
        {
            var prefix = cond.ComparisonType == QuestStateComparison.NotEquals ? "Not " : "";
            return $"{prefix}{cond.TargetState}";
        }

        private Color GetConditionColor(QuestState state)
        {
            return state switch
            {
                QuestState.Completed => new Color(0.4f, 0.75f, 0.4f),
                QuestState.InProgress => new Color(0.4f, 0.6f, 0.9f),
                QuestState.Failed => new Color(0.9f, 0.4f, 0.4f),
                QuestState.NotStarted => new Color(0.5f, 0.5f, 0.5f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        private void DrawMiniTag(Rect rect, string text, Color bgColor)
        {
            DrawRoundedRect(rect, bgColor, 3f);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(rect, text, style);
        }

        private void DrawEmptyState(string message)
        {
            var rect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            var bgColor = new Color(0.18f, 0.18f, 0.18f);
            DrawRoundedRect(rect, bgColor, 4f);

            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic
            };
            GUI.Label(rect, message, style);
        }

        private void DrawSectionHeader(string title, Color color)
        {
            var style = new GUIStyle(Styles.SectionHeader)
            {
                normal = { textColor = color }
            };
            EditorGUILayout.LabelField(title, style);
        }

        private void DrawRewardsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Rewards", new Color(1f, 0.8f, 0.4f));

            if (rewards == null || rewards.Count == 0)
            {
                DrawEmptyState("No rewards configured");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            var goldColor = new Color(1f, 0.8f, 0.3f);

            foreach (var reward in rewards)
            {
                if (reward.RewardType == null) continue;

                var cardRect = GUILayoutUtility.GetRect(80, 70);
                DrawRoundedRect(cardRect, cardBg, 4f);

                // Accent line
                var accentRect = new Rect(cardRect.x, cardRect.y, cardRect.width, 3);
                DrawRoundedRect(accentRect, goldColor, 2f);

                // Icon
                if (reward.RewardType.RewardIcon != null)
                {
                    var iconRect = new Rect(cardRect.x + (cardRect.width - 32) / 2, cardRect.y + 10, 32, 32);
                    GUI.DrawTexture(iconRect, reward.RewardType.RewardIcon.texture, ScaleMode.ScaleToFit);
                }

                // Amount
                var amountStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = goldColor }
                };
                var amountRect = new Rect(cardRect.x, cardRect.y + 44, cardRect.width, 20);
                GUI.Label(amountRect, reward.Amount.ToString(), amountStyle);

                GUILayout.Space(4);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTaskGroupsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Task Groups", new Color(0.7f, 0.5f, 1f));

            var allTaskGroups = TaskGroups;
            if (allTaskGroups == null || allTaskGroups.Count == 0)
            {
                DrawEmptyState("No task groups configured");
                return;
            }

            // Responsive breakpoints
            float availableWidth = EditorGUIUtility.currentViewWidth - 40f;
            bool isNarrow = availableWidth < 350f;
            bool isWide = availableWidth > 500f;

            // Execution mode icons
            var modeIcons = new Dictionary<TaskExecutionMode, string>
            {
                { TaskExecutionMode.Sequential, "1→2→3" },
                { TaskExecutionMode.Parallel, "1|2|3" },
                { TaskExecutionMode.AnyOrder, "?→?→?" },
                { TaskExecutionMode.OptionalXofY, "X/Y" }
            };

            // Execution mode colors
            var modeColors = new Dictionary<TaskExecutionMode, Color>
            {
                { TaskExecutionMode.Sequential, new Color(0.4f, 0.7f, 1f) },
                { TaskExecutionMode.Parallel, new Color(0.6f, 0.5f, 0.9f) },
                { TaskExecutionMode.AnyOrder, new Color(0.4f, 0.8f, 0.6f) },
                { TaskExecutionMode.OptionalXofY, new Color(0.9f, 0.6f, 0.4f) }
            };

            var cardBg = new Color(0.18f, 0.18f, 0.18f);
            var taskBg = new Color(0.22f, 0.22f, 0.22f);

            for (int g = 0; g < allTaskGroups.Count; g++)
            {
                var group = allTaskGroups[g];
                var groupColor = modeColors.TryGetValue(group.ExecutionMode, out var c) ? c : new Color(0.5f, 0.5f, 0.5f);
                var modeIcon = modeIcons.TryGetValue(group.ExecutionMode, out var icon) ? icon : "???";

                if (isNarrow)
                {
                    // Narrow: Compact stacked layout
                    float headerHeight = 44f;
                    var headerRect = GUILayoutUtility.GetRect(0, headerHeight, GUILayout.ExpandWidth(true));
                    DrawRoundedRect(headerRect, cardBg, 4f);

                    // Accent line
                    var accentRect = new Rect(headerRect.x, headerRect.y, 4, headerRect.height);
                    EditorGUI.DrawRect(accentRect, groupColor);

                    // Group name (row 1)
                    var nameRect = new Rect(headerRect.x + 12, headerRect.y + 6, headerRect.width - 60, 16);
                    GUI.Label(nameRect, group.GroupName, Styles.ItemName);

                    // Task count badge
                    var countBadgeRect = new Rect(headerRect.xMax - 40, headerRect.y + 6, 32, 14);
                    DrawMiniTag(countBadgeRect, $"{group.TaskCount}", new Color(0.4f, 0.4f, 0.4f));

                    // Mode icon + name (row 2)
                    var modeIconStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 9,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = groupColor }
                    };
                    var modeIconRect = new Rect(headerRect.x + 12, headerRect.y + 24, 40, 14);
                    GUI.Label(modeIconRect, modeIcon, modeIconStyle);

                    var modeText = GetExecutionModeText(group.ExecutionMode, group.RequiredCount, group.TaskCount);
                    var modeRect = new Rect(headerRect.x + 52, headerRect.y + 24, 100, 14);
                    DrawMiniTag(modeRect, modeText, groupColor);

                    // Tasks - compact list
                    if (group.Tasks != null && group.Tasks.Count > 0)
                    {
                        for (int t = 0; t < group.Tasks.Count; t++)
                        {
                            var task = group.Tasks[t];
                            var taskRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
                            var taskCardRect = new Rect(taskRect.x + 12, taskRect.y, taskRect.width - 12, taskRect.height - 2);
                            DrawRoundedRect(taskCardRect, taskBg, 3f);

                            // Task number
                            var numStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                fontSize = 9,
                                alignment = TextAnchor.MiddleCenter,
                                normal = { textColor = groupColor }
                            };
                            var numRect = new Rect(taskCardRect.x + 4, taskCardRect.y + 2, 18, 16);
                            GUI.Label(numRect, $"{t + 1}", numStyle);

                            if (task != null)
                            {
                                // Task type mini tag
                                var typeInfo = GetTaskTypeInfo(task);
                                var typeRect = new Rect(taskCardRect.x + 24, taskCardRect.y + 3, 50, 14);
                                DrawMiniTag(typeRect, typeInfo.name, typeInfo.color);

                                // Task name (truncated)
                                var taskNameStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    fontSize = 9,
                                    clipping = TextClipping.Clip
                                };
                                var taskNameRect = new Rect(taskCardRect.x + 78, taskCardRect.y + 3, taskCardRect.width - 86, 14);
                                GUI.Label(taskNameRect, task.DevName, taskNameStyle);
                            }
                            else
                            {
                                var errorStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    fontSize = 9,
                                    normal = { textColor = new Color(0.9f, 0.4f, 0.4f) }
                                };
                                GUI.Label(new Rect(taskCardRect.x + 24, taskCardRect.y + 3, 80, 14), "Missing!", errorStyle);
                            }
                        }
                    }
                }
                else if (isWide)
                {
                    // Wide: Full layout with icon badge and object fields
                    float headerHeight = 42f;
                    var headerRect = GUILayoutUtility.GetRect(0, headerHeight, GUILayout.ExpandWidth(true));
                    DrawRoundedRect(headerRect, cardBg, 4f);

                    // Accent line at top
                    var accentRect = new Rect(headerRect.x, headerRect.y, headerRect.width, 3);
                    DrawRoundedRect(accentRect, groupColor, 2f);

                    // Mode icon badge
                    var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    var iconBgRect = new Rect(headerRect.x + 10, headerRect.y + 12, 48, 18);
                    DrawRoundedRect(iconBgRect, groupColor, 3f);
                    GUI.Label(iconBgRect, modeIcon, iconStyle);

                    // Group name
                    var nameRect = new Rect(headerRect.x + 66, headerRect.y + 12, headerRect.width - 220, 18);
                    GUI.Label(nameRect, group.GroupName, Styles.ItemName);

                    // Execution mode text
                    var modeText = GetExecutionModeText(group.ExecutionMode, group.RequiredCount, group.TaskCount);
                    var modeRect = new Rect(headerRect.xMax - 150, headerRect.y + 12, 80, 16);
                    DrawMiniTag(modeRect, modeText, groupColor);

                    // Task count
                    var countStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    var countRect = new Rect(headerRect.xMax - 65, headerRect.y + 12, 55, 18);
                    GUI.Label(countRect, $"{group.TaskCount} tasks", countStyle);

                    // Tasks list with object fields
                    if (group.Tasks != null && group.Tasks.Count > 0)
                    {
                        for (int t = 0; t < group.Tasks.Count; t++)
                        {
                            var task = group.Tasks[t];
                            var taskRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                            var taskCardRect = new Rect(taskRect.x + 16, taskRect.y, taskRect.width - 16, taskRect.height - 2);
                            DrawRoundedRect(taskCardRect, taskBg, 3f);

                            // Task number
                            var numStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                fontSize = 11,
                                fontStyle = FontStyle.Bold,
                                alignment = TextAnchor.MiddleCenter,
                                normal = { textColor = groupColor }
                            };
                            var numRect = new Rect(taskCardRect.x + 8, taskCardRect.y + 5, 20, 16);
                            GUI.Label(numRect, $"{t + 1}", numStyle);

                            if (task != null)
                            {
                                // Task type tag
                                var typeInfo = GetTaskTypeInfo(task);
                                var typeRect = new Rect(taskCardRect.x + 32, taskCardRect.y + 5, 65, 16);
                                DrawMiniTag(typeRect, typeInfo.name, typeInfo.color);

                                // Object field
                                var fieldRect = new Rect(taskCardRect.x + 105, taskCardRect.y + 4, taskCardRect.width - 115, EditorGUIUtility.singleLineHeight);
                                EditorGUI.ObjectField(fieldRect, task, typeof(Task_SO), false);
                            }
                            else
                            {
                                var errorStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    normal = { textColor = new Color(0.9f, 0.4f, 0.4f) }
                                };
                                GUI.Label(new Rect(taskCardRect.x + 32, taskCardRect.y + 5, 100, 16), "Missing Task!", errorStyle);
                            }
                        }
                    }
                }
                else
                {
                    // Medium: Balanced layout
                    float headerHeight = 38f;
                    var headerRect = GUILayoutUtility.GetRect(0, headerHeight, GUILayout.ExpandWidth(true));
                    DrawRoundedRect(headerRect, cardBg, 4f);

                    // Accent line
                    var accentRect = new Rect(headerRect.x, headerRect.y, 4, headerRect.height);
                    EditorGUI.DrawRect(accentRect, groupColor);

                    // Mode icon
                    var iconStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = groupColor }
                    };
                    var iconRect = new Rect(headerRect.x + 10, headerRect.y + 11, 44, 16);
                    GUI.Label(iconRect, modeIcon, iconStyle);

                    // Group name
                    var nameRect = new Rect(headerRect.x + 56, headerRect.y + 6, headerRect.width - 160, 16);
                    GUI.Label(nameRect, group.GroupName, Styles.ItemName);

                    // Execution mode tag
                    var modeText = GetExecutionModeText(group.ExecutionMode, group.RequiredCount, group.TaskCount);
                    var modeRect = new Rect(headerRect.x + 56, headerRect.y + 22, 80, 12);
                    DrawMiniTag(modeRect, modeText, groupColor);

                    // Task count
                    var countStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    var countRect = new Rect(headerRect.xMax - 70, headerRect.y + 11, 60, 16);
                    GUI.Label(countRect, $"{group.TaskCount} tasks", countStyle);

                    // Tasks list
                    if (group.Tasks != null && group.Tasks.Count > 0)
                    {
                        for (int t = 0; t < group.Tasks.Count; t++)
                        {
                            var task = group.Tasks[t];
                            var taskRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
                            var taskCardRect = new Rect(taskRect.x + 14, taskRect.y, taskRect.width - 14, taskRect.height - 2);
                            DrawRoundedRect(taskCardRect, taskBg, 3f);

                            // Task number
                            var numStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                fontSize = 10,
                                alignment = TextAnchor.MiddleCenter,
                                normal = { textColor = groupColor }
                            };
                            var numRect = new Rect(taskCardRect.x + 6, taskCardRect.y + 3, 20, 16);
                            GUI.Label(numRect, $"{t + 1}", numStyle);

                            if (task != null)
                            {
                                // Task type tag
                                var typeInfo = GetTaskTypeInfo(task);
                                var typeRect = new Rect(taskCardRect.x + 28, taskCardRect.y + 3, 60, 15);
                                DrawMiniTag(typeRect, typeInfo.name, typeInfo.color);

                                // Object field
                                var fieldRect = new Rect(taskCardRect.x + 94, taskCardRect.y + 2, taskCardRect.width - 102, EditorGUIUtility.singleLineHeight);
                                EditorGUI.ObjectField(fieldRect, task, typeof(Task_SO), false);
                            }
                            else
                            {
                                var errorStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    normal = { textColor = new Color(0.9f, 0.4f, 0.4f) }
                                };
                                GUI.Label(new Rect(taskCardRect.x + 28, taskCardRect.y + 3, 80, 16), "Missing!", errorStyle);
                            }
                        }
                    }
                }

                GUILayout.Space(6);
            }
        }

        private void DrawConditionsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Conditions", new Color(0.5f, 0.9f, 0.6f));

            // Start Conditions
            DrawConditionList("Start Conditions", startConditions, new Color(0.5f, 0.8f, 0.5f));

            // Failure Conditions (always show)
            DrawConditionList("Failure Conditions", failureConditions, new Color(0.9f, 0.4f, 0.4f));

            // Global Task Failure Conditions (always show)
            DrawConditionList("Global Task Failure", globalTaskFailureConditions, new Color(0.9f, 0.6f, 0.3f));
        }

        private void DrawQuestLinesSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Part of QuestLines", new Color(0.6f, 0.4f, 0.8f));

            var containingQuestLines = FindContainingQuestLines();

            if (containingQuestLines.Count == 0)
            {
                DrawEmptyState("Not part of any questline");
                return;
            }

            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            var accentColor = new Color(0.6f, 0.4f, 0.8f);

            foreach (var questLine in containingQuestLines)
            {
                float rowHeight = 44f;
                var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
                DrawRoundedRect(rowRect, cardBg, 4f);

                float padding = 8f;

                // QuestLine icon
                float iconSize = 32f;
                var iconRect = new Rect(rowRect.x + padding, rowRect.y + (rowHeight - iconSize) / 2f, iconSize, iconSize);
                if (questLine.Icon != null)
                {
                    GUI.DrawTexture(iconRect, questLine.Icon.texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(iconRect, new Color(0.25f, 0.25f, 0.25f));
                    var placeholderStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                    };
                    GUI.Label(iconRect, "QL", placeholderStyle);
                }

                // QuestLine name
                var nameRect = new Rect(iconRect.xMax + 10, rowRect.y + 6, rowRect.width - 220, 18);
                GUI.Label(nameRect, questLine.DevName, Styles.ItemName);

                // Quest count tag
                var countText = $"{questLine.QuestCount} quests";
                var tagRect = new Rect(iconRect.xMax + 10, rowRect.y + 26, 70, 14);
                DrawMiniTag(tagRect, countText, new Color(0.4f, 0.4f, 0.4f));

                // Position in line
                int position = GetQuestPositionInLine(questLine);
                if (position > 0)
                {
                    var posTagRect = new Rect(tagRect.xMax + 6, rowRect.y + 26, 50, 14);
                    DrawMiniTag(posTagRect, $"#{position}", accentColor);
                }

                // Object field
                var fieldRect = new Rect(rowRect.xMax - padding - 150, rowRect.y + (rowHeight - EditorGUIUtility.singleLineHeight) / 2f,
                                        150, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(fieldRect, questLine, typeof(QuestLine_SO), false);

                GUILayout.Space(2);
            }
        }

        private List<QuestLine_SO> FindContainingQuestLines()
        {
            var result = new List<QuestLine_SO>();

            // Find all QuestLine_SO assets in the project
            var guids = AssetDatabase.FindAssets("t:QuestLine_SO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var questLine = AssetDatabase.LoadAssetAtPath<QuestLine_SO>(path);

                if (questLine != null && questLine.Quests != null)
                {
                    foreach (var quest in questLine.Quests)
                    {
                        if (quest == this)
                        {
                            result.Add(questLine);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private int GetQuestPositionInLine(QuestLine_SO questLine)
        {
            if (questLine.Quests == null) return 0;

            for (int i = 0; i < questLine.Quests.Count; i++)
            {
                if (questLine.Quests[i] == this)
                    return i + 1;
            }
            return 0;
        }

        private void DrawValidationSection()
        {
            var issues = GetValidationIssues();
            var warnings = GetValidationWarnings();
            var localizationIssues = GetLocalizationIssues();
            var circularDependencyIssues = GetCircularDependencyIssues();

            // Add circular dependencies to critical issues
            issues.AddRange(circularDependencyIssues);

            // Summary header
            GUILayout.Space(8);
            var totalErrors = issues.Count;
            var totalWarnings = warnings.Count + localizationIssues.Count;

            if (totalErrors == 0 && totalWarnings == 0)
            {
                DrawValidationStatus("All checks passed", new Color(0.3f, 0.7f, 0.4f), true);
            }
            else
            {
                if (totalErrors > 0)
                    DrawValidationStatus($"{totalErrors} Error(s)", new Color(0.9f, 0.35f, 0.35f), false);
                if (totalWarnings > 0)
                    DrawValidationStatus($"{totalWarnings} Warning(s)", new Color(0.9f, 0.7f, 0.3f), false);
            }

            GUILayout.Space(12);

            // Errors (Critical)
            if (issues.Count > 0)
            {
                DrawSectionHeader("Errors", new Color(0.9f, 0.4f, 0.4f));
                foreach (var issue in issues)
                {
                    DrawValidationItem(issue, new Color(0.9f, 0.35f, 0.35f));
                }
                GUILayout.Space(8);
            }

            // Warnings
            if (warnings.Count > 0)
            {
                DrawSectionHeader("Warnings", new Color(0.9f, 0.7f, 0.3f));
                foreach (var warning in warnings)
                {
                    DrawValidationItem(warning, new Color(0.9f, 0.7f, 0.3f));
                }
                GUILayout.Space(8);
            }

            // Localization Issues
            if (localizationIssues.Count > 0)
            {
                DrawSectionHeader("Localization", new Color(0.6f, 0.7f, 0.9f));
                foreach (var issue in localizationIssues)
                {
                    DrawValidationItem(issue, new Color(0.6f, 0.7f, 0.9f));
                }
            }
        }

        private System.Collections.Generic.List<string> GetValidationIssues()
        {
            var issues = new System.Collections.Generic.List<string>();

            // Critical issues
            if (string.IsNullOrEmpty(devName))
                issues.Add("Dev Name is empty");

            if (string.IsNullOrEmpty(questId))
                issues.Add("Quest ID is missing");

            if (stages == null || stages.Count == 0)
                issues.Add("No stages configured");
            else
            {
                foreach (var stage in stages)
                {
                    if (!stage.HasTaskGroups)
                        issues.Add($"Stage '{stage.StageName}' has no task groups");
                    else
                    {
                        foreach (var group in stage.TaskGroups)
                        {
                            if (group.Tasks == null || group.Tasks.Count == 0)
                                issues.Add($"Task Group '{group.GroupName}' in stage '{stage.StageName}' has no tasks");
                            else
                            {
                                for (int t = 0; t < group.Tasks.Count; t++)
                                {
                                    if (group.Tasks[t] == null)
                                        issues.Add($"Task Group '{group.GroupName}' has null task at index {t}");
                                }
                            }
                        }
                    }
                }
            }

            // Check for null conditions
            if (startConditions != null)
            {
                for (int i = 0; i < startConditions.Count; i++)
                {
                    if (startConditions[i] == null)
                        issues.Add($"Start Condition at index {i} is null");
                }
            }

            if (failureConditions != null)
            {
                for (int i = 0; i < failureConditions.Count; i++)
                {
                    if (failureConditions[i] == null)
                        issues.Add($"Failure Condition at index {i} is null");
                }
            }

            // Check rewards
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].RewardType == null)
                        issues.Add($"Reward at index {i} has no type");
                    else if (rewards[i].Amount <= 0)
                        issues.Add($"Reward '{rewards[i].RewardType.name}' has invalid amount");
                }
            }

            // Validate event-driven conditions have target values (uses Task_SO's static helper)
            issues.AddRange(Task_SO.ValidateEventDrivenConditions(startConditions, "Start Conditions"));
            issues.AddRange(Task_SO.ValidateEventDrivenConditions(failureConditions, "Failure Conditions"));
            issues.AddRange(Task_SO.ValidateEventDrivenConditions(globalTaskFailureConditions, "Global Task Failure"));

            // Validate task conditions (calls Task_SO's instance method)
            if (AllTasks != null)
            {
                foreach (var task in AllTasks)
                {
                    if (task == null) continue;
                    issues.AddRange(task.GetEventDrivenConditionIssues());
                }
            }

            return issues;
        }

        private System.Collections.Generic.List<string> GetValidationWarnings()
        {
            var warnings = new System.Collections.Generic.List<string>();

            if (questType == null)
                warnings.Add("Quest Type not assigned");

            if (questSprite == null)
                warnings.Add("Quest icon not assigned");

            if (recommendedLevel <= 0)
                warnings.Add("Recommended level not set");

            if (rewards == null || rewards.Count == 0)
                warnings.Add("No rewards configured");

            // Check for non-event-driven start conditions
            if (startConditions != null)
            {
                foreach (var cond in startConditions)
                {
                    if (cond != null && !(cond is IConditionEventDriven))
                        warnings.Add($"Start condition '{cond.name}' is not event-driven");
                }
            }

            return warnings;
        }

        private void DrawValidationStatus(string message, Color color, bool success)
        {
            var rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            var bgColor = success ? new Color(0.15f, 0.25f, 0.18f) : new Color(0.25f, 0.15f, 0.15f);
            EditorGUI.DrawRect(rect, bgColor);

            // Icon
            var iconRect = new Rect(rect.x + 12, rect.y + 8, 24, 24);
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
            GUI.Label(iconRect, success ? "\u2713" : "\u2716", iconStyle);

            // Message
            var msgStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = color }
            };
            var msgRect = new Rect(rect.x + 44, rect.y, rect.width - 56, rect.height);
            GUI.Label(msgRect, message, msgStyle);
        }

        private void DrawValidationItem(string message, Color accentColor)
        {
            var rect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            var cardRect = new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height - 2);
            EditorGUI.DrawRect(cardRect, new Color(0.2f, 0.2f, 0.2f));

            // Accent bar
            var accentRect = new Rect(cardRect.x, cardRect.y, 3, cardRect.height);
            EditorGUI.DrawRect(accentRect, accentColor);

            // Message
            var msgStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            var msgRect = new Rect(cardRect.x + 12, cardRect.y, cardRect.width - 16, cardRect.height);
            GUI.Label(msgRect, message, msgStyle);
        }

        private void DrawConditionList(string label, System.Collections.Generic.List<Condition_SO> conditions, Color accentColor)
        {
            var cardBg = new Color(0.2f, 0.2f, 0.2f);

            // Sub-header
            EditorGUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = accentColor }
            };
            GUILayout.Space(8);
            EditorGUILayout.LabelField(label, labelStyle, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            if (conditions == null || conditions.Count == 0)
            {
                var emptyRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
                var emptyCardRect = new Rect(emptyRect.x + 8, emptyRect.y, emptyRect.width - 8, emptyRect.height - 2);
                DrawRoundedRect(emptyCardRect, new Color(0.16f, 0.16f, 0.16f), 3f);

                var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 9,
                    fontStyle = FontStyle.Italic
                };
                GUI.Label(emptyCardRect, "None", emptyStyle);
                GUILayout.Space(4);
                return;
            }

            foreach (var condition in conditions)
            {
                if (condition == null) continue;

                var rowRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
                var cardRect = new Rect(rowRect.x + 8, rowRect.y, rowRect.width - 8, rowRect.height - 2);
                DrawRoundedRect(cardRect, cardBg, 3f);

                // Accent dot
                var dotRect = new Rect(cardRect.x + 8, cardRect.y + 8, 8, 8);
                DrawRoundedRect(dotRect, accentColor, 4f);

                // Object field
                var fieldRect = new Rect(cardRect.x + 24, cardRect.y + 2, cardRect.width - 32, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(fieldRect, condition, typeof(Condition_SO), false);
            }

            GUILayout.Space(4);
        }

        private string GetExecutionModeText(TaskExecutionMode mode, int required, int total)
        {
            return mode switch
            {
                TaskExecutionMode.Sequential => "Sequential",
                TaskExecutionMode.Parallel => "Parallel (All)",
                TaskExecutionMode.AnyOrder => "Any Order",
                TaskExecutionMode.OptionalXofY => $"{required} of {total}",
                _ => mode.ToString()
            };
        }

        private (string name, Color color) GetTaskTypeInfo(Task_SO task)
        {
            var typeName = task.GetType().Name.Replace("Task", "").Replace("_SO", "");

            // Darker, more muted colors for better white text readability
            return typeName switch
            {
                "Int" => ("Int", new Color(0.2f, 0.45f, 0.7f)),
                "Bool" => ("Bool", new Color(0.25f, 0.55f, 0.3f)),
                "String" => ("Text", new Color(0.6f, 0.45f, 0.2f)),
                "Location" => ("Location", new Color(0.55f, 0.3f, 0.55f)),
                "Timed" => ("Timed", new Color(0.7f, 0.3f, 0.3f)),
                "Discovery" => ("Discovery", new Color(0.55f, 0.5f, 0.2f)),
                _ => (typeName, new Color(0.4f, 0.4f, 0.4f))
            };
        }

        private void DrawStatCard(Rect rect, Color bgColor, string value, string label, Color accentColor)
        {
            // Card background
            DrawRoundedRect(rect, bgColor, 4f);

            // Accent line at top
            var accentRect = new Rect(rect.x, rect.y, rect.width, 3);
            DrawRoundedRect(accentRect, accentColor, 2f);

            // Value
            var valueRect = new Rect(rect.x, rect.y + 8, rect.width, 24);
            var valueStyle = new GUIStyle(Styles.StatValue) { normal = { textColor = accentColor } };
            GUI.Label(valueRect, value, valueStyle);

            // Label
            var labelRect = new Rect(rect.x, rect.y + 32, rect.width, 16);
            GUI.Label(labelRect, label, Styles.StatLabel);
        }

        private void DrawTag(string text, Color bgColor)
        {
            var content = new GUIContent(text);
            var size = Styles.Tag.CalcSize(content);
            var rect = GUILayoutUtility.GetRect(size.x, size.y);

            DrawRoundedRect(rect, bgColor, 4f);
            GUI.Label(rect, content, Styles.Tag);
        }

        private void DrawRoundedRect(Rect rect, Color color, float radius = 0)
        {
            // Simple rectangle (radius parameter kept for API compatibility but ignored)
            EditorGUI.DrawRect(rect, color);
        }

        #endregion

        #region Quick Actions

        private void DrawQuickActionsSection()
        {
            // Ensure GUI is enabled for interactive elements
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            GUILayout.Space(8);

            // Prerequisites Section
            DrawSectionHeader("Add Prerequisite Quest", new Color(0.6f, 0.85f, 1f));
            DrawQuickActionInfo("Create a condition that requires another quest to be completed before this one can start.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Add Prerequisite Quest...", GUILayout.Height(32)))
            {
                ShowQuestPickerForPrerequisite();
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Task Creation Section
            DrawSectionHeader("Create Tasks", new Color(0.7f, 0.5f, 1f));
            DrawQuickActionInfo("Create new task assets and automatically add them to the first task group.");

            // Task type buttons - 2 rows of 3
            var taskTypes = new[]
            {
                ("Int", "TaskInt_SO", new Color(0.2f, 0.45f, 0.7f)),
                ("Bool", "TaskBool_SO", new Color(0.25f, 0.55f, 0.3f)),
                ("Location", "TaskLocation_SO", new Color(0.55f, 0.3f, 0.55f)),
                ("Discovery", "TaskDiscovery_SO", new Color(0.55f, 0.5f, 0.2f)),
                ("Timed", "TaskTimed_SO", new Color(0.7f, 0.3f, 0.3f)),
                ("Text", "TaskString_SO", new Color(0.6f, 0.45f, 0.2f)),
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            for (int i = 0; i < 3; i++)
            {
                var (label, typeName, color) = taskTypes[i];
                var originalBg = GUI.backgroundColor;
                GUI.backgroundColor = color;

                if (GUILayout.Button(label, GUILayout.Height(28)))
                {
                    CreateTaskOfType(typeName);
                }

                GUI.backgroundColor = originalBg;
                GUILayout.Space(4);
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            for (int i = 3; i < 6; i++)
            {
                var (label, typeName, color) = taskTypes[i];
                var originalBg = GUI.backgroundColor;
                GUI.backgroundColor = color;

                if (GUILayout.Button(label, GUILayout.Height(28)))
                {
                    CreateTaskOfType(typeName);
                }

                GUI.backgroundColor = originalBg;
                GUILayout.Space(4);
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Auto-populate Section
            DrawSectionHeader("Auto-Populate Tasks", new Color(0.5f, 0.8f, 0.5f));
            DrawQuickActionInfo("Scan the Tasks/ subfolder and add all found Task_SO assets to the first task group.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Scan & Add Tasks from Folder", GUILayout.Height(32)))
            {
                AutoPopulateTasksFromFolder();
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Stage Info Section
            DrawSectionHeader("Quest Structure", new Color(0.4f, 0.8f, 0.9f));
            DrawQuickActionInfo("Stages define the quest structure. Add or edit stages in the Configuration tab.");

            // Show current stage count
            var stageCount = stages?.Count ?? 0;
            var totalTasks = stages?.Sum(s => s.TotalTaskCount) ?? 0;
            var stageInfo = $"{stageCount} stage(s) with {totalTasks} total task(s)";
            var infoRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            var infoCardRect = new Rect(infoRect.x + 8, infoRect.y, infoRect.width - 16, infoRect.height - 2);
            DrawRoundedRect(infoCardRect, new Color(0.2f, 0.25f, 0.28f), 4f);

            var infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.8f, 0.9f) }
            };
            GUI.Label(infoCardRect, stageInfo, infoStyle);

            GUILayout.Space(16);

            // Task Group Section - Responsive AAA Design
            DrawSectionHeader("Task Groups", new Color(0.9f, 0.6f, 0.4f));
            DrawQuickActionInfo("Add a new task group to organize tasks with different execution modes.");

            DrawTaskGroupButtons();

            // Restore GUI state
            GUI.enabled = wasEnabled;
        }

        private void DrawQuickActionInfo(string message)
        {
            var rect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            var infoRect = new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height);

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            GUI.Label(infoRect, message, style);
        }

        private void DrawTaskGroupButtons()
        {
            // Responsive breakpoints
            float availableWidth = EditorGUIUtility.currentViewWidth - 40f;
            bool isNarrow = availableWidth < 400f;
            bool isWide = availableWidth > 550f;

            // Task group types with their properties
            var groupTypes = new[]
            {
                (
                    mode: TaskExecutionMode.Sequential,
                    name: "Sequential",
                    icon: "1→2→3",
                    desc: "Tasks complete one after another in order",
                    color: new Color(0.4f, 0.7f, 1f)
                ),
                (
                    mode: TaskExecutionMode.Parallel,
                    name: "Parallel",
                    icon: "1|2|3",
                    desc: "All tasks active at once, all must complete",
                    color: new Color(0.6f, 0.5f, 0.9f)
                ),
                (
                    mode: TaskExecutionMode.AnyOrder,
                    name: "Any Order",
                    icon: "?→?→?",
                    desc: "All tasks available, complete in any order",
                    color: new Color(0.4f, 0.8f, 0.6f)
                ),
                (
                    mode: TaskExecutionMode.OptionalXofY,
                    name: "Optional",
                    icon: "X/Y",
                    desc: "Complete X of Y tasks to finish group",
                    color: new Color(0.9f, 0.6f, 0.4f)
                )
            };

            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            var hoverBg = new Color(0.25f, 0.25f, 0.25f);
            float padding = 8f;

            if (isNarrow)
            {
                // Narrow: Stack vertically, compact cards
                float cardHeight = 50f;

                foreach (var group in groupTypes)
                {
                    var cardRect = GUILayoutUtility.GetRect(0, cardHeight, GUILayout.ExpandWidth(true));
                    cardRect.x += padding;
                    cardRect.width -= padding * 2;

                    // Hover detection
                    bool isHover = cardRect.Contains(Event.current.mousePosition);
                    DrawRoundedRect(cardRect, isHover ? hoverBg : cardBg, 4f);

                    // Accent line
                    var accentRect = new Rect(cardRect.x, cardRect.y, 4, cardRect.height);
                    EditorGUI.DrawRect(accentRect, group.color);

                    // Icon - wider for longer icons like "1→2→3"
                    var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = group.color }
                    };
                    var iconRect = new Rect(cardRect.x + 10, cardRect.y + 8, 50, 16);
                    GUI.Label(iconRect, group.icon, iconStyle);

                    // Name
                    var nameRect = new Rect(cardRect.x + 65, cardRect.y + 8, cardRect.width - 140, 16);
                    GUI.Label(nameRect, group.name, Styles.ItemName);

                    // Description
                    var descRect = new Rect(cardRect.x + 65, cardRect.y + 26, cardRect.width - 140, 16);
                    GUI.Label(descRect, group.desc, Styles.ItemDetail);

                    // Add button
                    var buttonRect = new Rect(cardRect.xMax - 60, cardRect.y + (cardHeight - 24) / 2, 52, 24);
                    if (GUI.Button(buttonRect, "Add"))
                    {
                        AddTaskGroup(group.mode);
                    }

                    GUILayout.Space(4);
                }
            }
            else if (isWide)
            {
                // Wide: 2x2 grid with larger cards
                float cardWidth = (availableWidth - padding * 3) / 2f;
                float cardHeight = 70f;

                for (int row = 0; row < 2; row++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(padding);

                    for (int col = 0; col < 2; col++)
                    {
                        int index = row * 2 + col;
                        var group = groupTypes[index];

                        var cardRect = GUILayoutUtility.GetRect(cardWidth, cardHeight);

                        // Hover detection
                        bool isHover = cardRect.Contains(Event.current.mousePosition);
                        DrawRoundedRect(cardRect, isHover ? hoverBg : cardBg, 4f);

                        // Accent line at top
                        var accentRect = new Rect(cardRect.x, cardRect.y, cardRect.width, 3);
                        DrawRoundedRect(accentRect, group.color, 2f);

                        // Icon badge - wider to fit longer icons like "1→2→3"
                        var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 11,
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white }
                        };
                        var iconBgRect = new Rect(cardRect.x + 10, cardRect.y + 12, 52, 18);
                        DrawRoundedRect(iconBgRect, group.color, 3f);
                        GUI.Label(iconBgRect, group.icon, iconStyle);

                        // Name
                        var nameRect = new Rect(cardRect.x + 70, cardRect.y + 12, cardRect.width - 80, 18);
                        GUI.Label(nameRect, group.name, Styles.ItemName);

                        // Description
                        var descStyle = new GUIStyle(Styles.ItemDetail) { wordWrap = true };
                        var descRect = new Rect(cardRect.x + 10, cardRect.y + 34, cardRect.width - 20, 28);
                        GUI.Label(descRect, group.desc, descStyle);

                        // Click to add
                        if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
                        {
                            AddTaskGroup(group.mode);
                        }

                        // Show "Click to add" hint on hover
                        if (isHover)
                        {
                            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                fontSize = 9,
                                alignment = TextAnchor.LowerRight,
                                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                            };
                            var hintRect = new Rect(cardRect.x, cardRect.y + cardHeight - 16, cardRect.width - 6, 14);
                            GUI.Label(hintRect, "Click to add", hintStyle);
                        }

                        if (col == 0) GUILayout.Space(padding);
                    }

                    GUILayout.Space(padding);
                    EditorGUILayout.EndHorizontal();

                    if (row == 0) GUILayout.Space(4);
                }
            }
            else
            {
                // Medium: Horizontal row with compact buttons
                float cardHeight = 60f;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(padding);

                foreach (var group in groupTypes)
                {
                    var cardRect = GUILayoutUtility.GetRect(0, cardHeight, GUILayout.ExpandWidth(true));

                    // Hover detection
                    bool isHover = cardRect.Contains(Event.current.mousePosition);
                    DrawRoundedRect(cardRect, isHover ? hoverBg : cardBg, 4f);

                    // Accent line at top
                    var accentRect = new Rect(cardRect.x, cardRect.y, cardRect.width, 3);
                    DrawRoundedRect(accentRect, group.color, 2f);

                    // Icon
                    var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = group.color }
                    };
                    var iconRect = new Rect(cardRect.x, cardRect.y + 10, cardRect.width, 16);
                    GUI.Label(iconRect, group.icon, iconStyle);

                    // Name
                    var nameStyle = new GUIStyle(Styles.ItemName)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter
                    };
                    var nameRect = new Rect(cardRect.x, cardRect.y + 28, cardRect.width, 16);
                    GUI.Label(nameRect, group.name, nameStyle);

                    // Click to add
                    if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
                    {
                        AddTaskGroup(group.mode);
                    }

                    GUILayout.Space(4);
                }

                GUILayout.Space(padding - 4);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
        }

        private void ShowQuestPickerForPrerequisite()
        {
            var questPath = AssetDatabase.GetAssetPath(this);
            var questFolder = System.IO.Path.GetDirectoryName(questPath);

            // Show object picker for Quest_SO
            EditorGUIUtility.ShowObjectPicker<Quest_SO>(null, false, "", GUIUtility.GetControlID(FocusType.Passive));

            // Register callback
            EditorApplication.update += OnQuestPickerUpdate;
        }

        private void OnQuestPickerUpdate()
        {
            if (Event.current != null && Event.current.commandName == "ObjectSelectorClosed")
            {
                EditorApplication.update -= OnQuestPickerUpdate;

                var selectedQuest = EditorGUIUtility.GetObjectPickerObject() as Quest_SO;
                if (selectedQuest != null && selectedQuest != this)
                {
                    CreatePrerequisiteCondition(selectedQuest);
                }
            }

            // Check if picker is still open
            if (EditorGUIUtility.GetObjectPickerControlID() == 0)
            {
                EditorApplication.update -= OnQuestPickerUpdate;
            }
        }

        private void CreatePrerequisiteCondition(Quest_SO prerequisiteQuest)
        {
            var questPath = AssetDatabase.GetAssetPath(this);
            var questFolder = System.IO.Path.GetDirectoryName(questPath);
            var conditionsFolder = System.IO.Path.Combine(questFolder, "Conditions");

            // Create Conditions folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(conditionsFolder))
            {
                AssetDatabase.CreateFolder(questFolder, "Conditions");
            }

            // Create the condition asset
            var condition = ScriptableObject.CreateInstance<ConditionQuestState_SO>();

            // Use reflection to set private fields (or make them settable)
            var questToCheckField = typeof(ConditionQuestState_SO).GetField("questToCheck",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetStateField = typeof(ConditionQuestState_SO).GetField("targetState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (questToCheckField != null) questToCheckField.SetValue(condition, prerequisiteQuest);
            if (targetStateField != null) targetStateField.SetValue(condition, QuestState.Completed);

            // Save the asset
            var conditionName = $"SO_Condition_Requires_{prerequisiteQuest.DevName}Completed.asset";
            var conditionPath = System.IO.Path.Combine(conditionsFolder, conditionName);
            conditionPath = AssetDatabase.GenerateUniqueAssetPath(conditionPath);

            AssetDatabase.CreateAsset(condition, conditionPath);
            AssetDatabase.SaveAssets();

            // Add to start conditions
            if (startConditions == null)
            {
                startConditions = new List<Condition_SO>();
            }
            startConditions.Add(condition);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Quest_SO] Created prerequisite condition: {conditionPath}");
            EditorGUIUtility.PingObject(condition);
        }

        private void CreateTaskOfType(string typeName)
        {
            var questPath = AssetDatabase.GetAssetPath(this);
            var questFolder = System.IO.Path.GetDirectoryName(questPath);
            var tasksFolder = System.IO.Path.Combine(questFolder, "Tasks");

            // Create Tasks folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(tasksFolder))
            {
                AssetDatabase.CreateFolder(questFolder, "Tasks");
            }

            // Find the correct type
            var taskType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName && typeof(Task_SO).IsAssignableFrom(t));

            if (taskType == null)
            {
                Debug.LogError($"[Quest_SO] Could not find task type: {typeName}");
                return;
            }

            // Create the task asset
            var task = ScriptableObject.CreateInstance(taskType) as Task_SO;
            if (task == null)
            {
                Debug.LogError($"[Quest_SO] Could not create instance of: {typeName}");
                return;
            }

            // Set dev name
            int taskNumber = (AllTasks?.Count ?? 0) + 1;
            var devNameField = typeof(Task_SO).GetField("devName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (devNameField != null)
            {
                devNameField.SetValue(task, $"{devName}_Task{taskNumber:D2}");
            }

            // Generate GUID
            var taskIdField = typeof(Task_SO).GetField("taskId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (taskIdField != null)
            {
                taskIdField.SetValue(task, System.Guid.NewGuid().ToString());
            }

            // Save the asset
            var taskName = $"SO_Task_{devName}_{taskNumber:D2}.asset";
            var taskPath = System.IO.Path.Combine(tasksFolder, taskName);
            taskPath = AssetDatabase.GenerateUniqueAssetPath(taskPath);

            AssetDatabase.CreateAsset(task, taskPath);
            AssetDatabase.SaveAssets();

            // Add to first stage's first task group (create if none exists)
            EnsureDefaultStageExists();

            if (stages[0].TaskGroups.Count == 0)
            {
                stages[0].TaskGroups.Add(new TaskGroup { GroupName = "Main Tasks" });
            }

            stages[0].TaskGroups[0].Tasks.Add(task);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Quest_SO] Created task: {taskPath}");
            Selection.activeObject = task;
            EditorGUIUtility.PingObject(task);
        }

        private void AutoPopulateTasksFromFolder()
        {
            var questPath = AssetDatabase.GetAssetPath(this);
            var questFolder = System.IO.Path.GetDirectoryName(questPath);
            var tasksFolder = System.IO.Path.Combine(questFolder, "Tasks");

            if (!AssetDatabase.IsValidFolder(tasksFolder))
            {
                Debug.LogWarning($"[Quest_SO] Tasks folder not found at: {tasksFolder}");
                return;
            }

            // Find all Task_SO assets in the folder
            var guids = AssetDatabase.FindAssets("t:Task_SO", new[] { tasksFolder });
            var foundTasks = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<Task_SO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null)
                .ToList();

            if (foundTasks.Count == 0)
            {
                Debug.LogWarning($"[Quest_SO] No Task_SO assets found in: {tasksFolder}");
                return;
            }

            // Create stage and task group if needed
            EnsureDefaultStageExists();

            if (stages[0].TaskGroups.Count == 0)
            {
                stages[0].TaskGroups.Add(new TaskGroup { GroupName = "Main Tasks" });
            }

            // Add tasks that aren't already in any group
            var existingTasks = new HashSet<Task_SO>(AllTasks);
            int addedCount = 0;

            foreach (var task in foundTasks)
            {
                if (!existingTasks.Contains(task))
                {
                    stages[0].TaskGroups[0].Tasks.Add(task);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Quest_SO] Added {addedCount} tasks from folder. Total tasks: {AllTasks.Count}");
            }
            else
            {
                Debug.Log($"[Quest_SO] All {foundTasks.Count} tasks from folder are already added.");
            }
        }

        private void AddTaskGroup(TaskExecutionMode mode)
        {
            EnsureDefaultStageExists();

            var currentGroupCount = stages[0].TaskGroups.Count;
            var groupName = mode switch
            {
                TaskExecutionMode.Sequential => $"Sequential Group {currentGroupCount + 1}",
                TaskExecutionMode.Parallel => $"Parallel Group {currentGroupCount + 1}",
                TaskExecutionMode.AnyOrder => $"Any Order Group {currentGroupCount + 1}",
                TaskExecutionMode.OptionalXofY => $"Optional Group {currentGroupCount + 1}",
                _ => $"Task Group {currentGroupCount + 1}"
            };

            stages[0].TaskGroups.Add(new TaskGroup
            {
                GroupName = groupName,
                ExecutionMode = mode
            });

            EditorUtility.SetDirty(this);
            Debug.Log($"[Quest_SO] Added new task group: {groupName}");
        }

        private void EnsureDefaultStageExists()
        {
            if (stages == null)
            {
                stages = new List<QuestStage>();
            }

            if (stages.Count == 0)
            {
                stages.Add(QuestStage.CreateEmpty(0, "Main"));
            }
        }

        #endregion

        #region Enhanced Validation

        private List<string> GetLocalizationIssues()
        {
            var issues = new List<string>();

            // Check quest localization
            if (displayName != null && displayName.IsEmpty)
                issues.Add("Display Name localization not configured");

            if (questDescription != null && questDescription.IsEmpty)
                issues.Add("Quest Description localization not configured");

            if (questLocation != null && questLocation.IsEmpty)
                issues.Add("Quest Location localization not configured");

            // Check task localizations
            if (AllTasks != null)
            {
                foreach (var task in AllTasks)
                {
                    if (task == null) continue;

                    if (task.DisplayName != null && task.DisplayName.IsEmpty)
                        issues.Add($"Task '{task.DevName}' display name not localized");

                    if (task.TaskDescription != null && task.TaskDescription.IsEmpty)
                        issues.Add($"Task '{task.DevName}' description not localized");
                }
            }

            return issues;
        }

        private List<string> GetCircularDependencyIssues()
        {
            var issues = new List<string>();
            var visited = new HashSet<Quest_SO>();
            var chain = new List<string>();

            if (HasCircularDependency(this, visited, chain))
            {
                chain.Add(devName);
                chain.Reverse();
                issues.Add($"Circular dependency detected: {string.Join(" → ", chain)}");
            }

            return issues;
        }

        private bool HasCircularDependency(Quest_SO quest, HashSet<Quest_SO> visited, List<string> chain)
        {
            if (quest == null) return false;
            if (visited.Contains(quest)) return true;

            visited.Add(quest);

            // Check start conditions for quest state conditions
            if (quest.StartConditions != null)
            {
                foreach (var condition in quest.StartConditions)
                {
                    var prereqQuests = GetPrerequisiteQuests(condition);
                    foreach (var prereq in prereqQuests)
                    {
                        if (prereq == this)
                        {
                            chain.Add(quest.DevName);
                            return true;
                        }

                        if (HasCircularDependency(prereq, visited, chain))
                        {
                            chain.Add(quest.DevName);
                            return true;
                        }
                    }
                }
            }

            visited.Remove(quest);
            return false;
        }

        private List<Quest_SO> GetPrerequisiteQuests(Condition_SO condition)
        {
            var result = new List<Quest_SO>();

            if (condition is ConditionQuestState_SO questCond && questCond.QuestToCheck != null)
            {
                result.Add(questCond.QuestToCheck);
            }
            else if (condition is CompositeCondition_SO composite && composite.Conditions != null)
            {
                foreach (var subCond in composite.Conditions)
                {
                    result.AddRange(GetPrerequisiteQuests(subCond));
                }
            }

            return result;
        }

        #endregion
#endif
    }
}
#endif
