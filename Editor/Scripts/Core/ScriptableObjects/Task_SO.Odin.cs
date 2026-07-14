#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using UnityEngine;
using HelloDev.Conditions;
using HelloDev.Events;
using HelloDev.QuestSystem.Stages;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions.Types;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    public abstract partial class Task_SO
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
        [OnInspectorGUI("DrawConditionsSection")]
        [PropertyOrder(-80)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _conditionsPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawUsedByQuestsSection")]
        [PropertyOrder(-70)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _usedByQuestsPlaceholder => "";

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
            var taskTypeInfo = GetTaskTypeInfo();
            var accentColor = taskTypeInfo.color;

            // Color accent line at top
            var lineRect = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, accentColor);

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            // Task type icon placeholder
            var iconRect = GUILayoutUtility.GetRect(60, 60, GUILayout.Width(60));
            EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f));
            var iconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = accentColor }
            };
            GUI.Label(iconRect, taskTypeInfo.icon, iconStyle);

            GUILayout.Space(12);

            // Info section
            EditorGUILayout.BeginVertical();

            // Row 1: Task type tag
            EditorGUILayout.BeginHorizontal();
            DrawTag(taskTypeInfo.name, accentColor);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Row 2: Task Name
            var displayNameText = string.IsNullOrEmpty(devName) ? "Unnamed Task" : devName;
            EditorGUILayout.LabelField(displayNameText, Styles.Title);

            // Row 3: GUID
            EditorGUILayout.LabelField(taskId ?? "No GUID", Styles.Subtitle);

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
            float cardWidth = (bgRect.width - 30) / 3f;
            float cardHeight = 54;
            float startX = bgRect.x + 10;
            float startY = bgRect.y + 8;

            // Stats data
            int failureCondCount = failureConditions?.Count ?? 0;
            int questsUsingThis = FindContainingQuests().Count;
            var eventInfo = GetTaskEventInfo();

            // Draw stat cards
            DrawStatCard(new Rect(startX, startY, cardWidth - 5, cardHeight), cardBg,
                eventInfo.hasEvent ? "✓" : "✗", "Event", eventInfo.hasEvent ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.4f));

            DrawStatCard(new Rect(startX + cardWidth, startY, cardWidth - 5, cardHeight), cardBg,
                failureCondCount.ToString(), "Fail Cond.", new Color(0.9f, 0.4f, 0.4f));

            DrawStatCard(new Rect(startX + cardWidth * 2, startY, cardWidth - 5, cardHeight), cardBg,
                questsUsingThis.ToString(), "Used By", new Color(0.4f, 0.6f, 0.9f));

            GUILayout.Space(4);

            // Draw event info section
            DrawEventInfoSection(eventInfo);
        }

        /// <summary>
        /// Gets task event information from conditions.
        /// Since tasks now use event-driven conditions, this checks the conditions array.
        /// </summary>
        protected virtual (bool hasEvent, string eventName, string targetName) GetTaskEventInfo()
        {
            if (conditions == null || conditions.Count == 0)
            {
                return (false, "No conditions", "None");
            }

            // Possible event field names used by different condition types
            string[] eventFieldNames = { "GameEventID", "GameEventString", "gameEvent" };

            // Find the first event-driven condition with an event
            foreach (var condition in conditions)
            {
                if (condition == null) continue;
                if (condition is not IConditionEventDriven) continue;

                var condType = condition.GetType();

                // Try each possible event field name
                System.Reflection.FieldInfo eventField = null;
                foreach (var fieldName in eventFieldNames)
                {
                    eventField = condType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (eventField != null) break;
                }

                if (eventField != null)
                {
                    var eventValue = eventField.GetValue(condition);
                    if (eventValue is UnityEngine.Object eventObj && eventObj != null)
                    {
                        // Look for targetValue field in condition
                        var targetField = condType.GetField("targetValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        string targetName = "None";
                        if (targetField != null)
                        {
                            var targetValue = targetField.GetValue(condition);
                            if (targetValue is UnityEngine.Object targetObj && targetObj != null)
                            {
                                targetName = targetObj.name;
                            }
                            else if (targetValue != null)
                            {
                                targetName = targetValue.ToString();
                            }
                        }

                        return (true, eventObj.name, targetName);
                    }
                }
            }

            // If we have conditions but none are event-driven with events
            int eventDrivenCount = 0;
            foreach (var condition in conditions)
            {
                if (condition is IConditionEventDriven) eventDrivenCount++;
            }

            if (eventDrivenCount > 0)
            {
                return (false, $"{eventDrivenCount} condition(s) - no event set", "None");
            }

            return (false, $"{conditions.Count} non-event condition(s)", "None");
        }

        private void DrawEventInfoSection((bool hasEvent, string eventName, string targetName) eventInfo)
        {
            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            float padding = 8f;

            var sectionRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            DrawRoundedRect(sectionRect, cardBg, 4f);

            // Event row
            var eventLabelRect = new Rect(sectionRect.x + padding, sectionRect.y + 6, 60, 16);
            var eventLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.8f, 0.5f) }
            };
            GUI.Label(eventLabelRect, "Event:", eventLabelStyle);

            var eventValueRect = new Rect(sectionRect.x + padding + 60, sectionRect.y + 6, sectionRect.width - padding * 2 - 60, 16);
            var eventValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = eventInfo.hasEvent ? Color.white : new Color(0.6f, 0.6f, 0.6f) }
            };
            GUI.Label(eventValueRect, eventInfo.eventName, eventValueStyle);

            // Target row
            var targetLabelRect = new Rect(sectionRect.x + padding, sectionRect.y + 26, 60, 16);
            var targetLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.7f, 0.4f) }
            };
            GUI.Label(targetLabelRect, "Target:", targetLabelStyle);

            var targetValueRect = new Rect(sectionRect.x + padding + 60, sectionRect.y + 26, sectionRect.width - padding * 2 - 60, 16);
            var targetValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = eventInfo.targetName != "None" ? Color.white : new Color(0.6f, 0.6f, 0.6f) }
            };
            GUI.Label(targetValueRect, eventInfo.targetName, targetValueStyle);

            GUILayout.Space(4);
        }

        private void DrawConditionsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Conditions", new Color(0.5f, 0.9f, 0.6f));

            DrawConditionList("Completion Conditions", conditions, new Color(0.4f, 0.8f, 0.4f));
            DrawConditionList("Failure Conditions", failureConditions, new Color(0.9f, 0.4f, 0.4f));
        }

        private void DrawUsedByQuestsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Used by Quests", new Color(0.4f, 0.6f, 0.9f));

            var containingQuests = FindContainingQuests();

            if (containingQuests.Count == 0)
            {
                DrawEmptyState("Not used by any quest");
                return;
            }

            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            var stageColor = new Color(0.6f, 0.5f, 0.9f);
            var groupColor = new Color(0.5f, 0.4f, 0.7f);
            var choiceColor = new Color(0.9f, 0.6f, 0.3f);
            var branchColor = new Color(0.5f, 0.7f, 0.9f);

            foreach (var questInfo in containingQuests)
            {
                var quest = questInfo.quest;
                bool hasBranchInfo = questInfo.hasChoices || questInfo.isBranchPath;
                float rowHeight = hasBranchInfo ? 64f : 54f; // Taller if branching info

                var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
                DrawRoundedRect(rowRect, cardBg, 4f);

                // Left accent for branch paths
                if (questInfo.isBranchPath)
                {
                    var accentRect = new Rect(rowRect.x, rowRect.y, 3, rowRect.height);
                    EditorGUI.DrawRect(accentRect, branchColor);
                }

                float padding = 8f;

                // Quest icon
                float iconSize = 36f;
                var iconRect = new Rect(rowRect.x + padding, rowRect.y + 8, iconSize, iconSize);
                if (quest.QuestSprite != null)
                {
                    GUI.DrawTexture(iconRect, quest.QuestSprite.texture, ScaleMode.ScaleToFit);
                }
                else
                {
                    EditorGUI.DrawRect(iconRect, new Color(0.25f, 0.25f, 0.25f));
                }

                // Quest name
                var nameRect = new Rect(iconRect.xMax + 10, rowRect.y + 6, rowRect.width - 220, 18);
                GUI.Label(nameRect, quest.DevName, Styles.ItemName);

                // Quest type tag
                float tagX = iconRect.xMax + 10;
                if (quest.QuestType != null)
                {
                    var typeRect = new Rect(tagX, rowRect.y + 26, 70, 14);
                    DrawMiniTag(typeRect, quest.QuestType.DevName, quest.QuestType.Color);
                    tagX += 76;
                }

                // Stage info tag
                var stageText = $"S{questInfo.stageIndex}";
                var stageRect = new Rect(tagX, rowRect.y + 26, 30, 14);
                DrawMiniTag(stageRect, stageText, stageColor);
                tagX += 36;

                // Group info tag
                var groupRect = new Rect(tagX, rowRect.y + 26, 30, 14);
                DrawMiniTag(groupRect, $"G{questInfo.groupIndex + 1}", groupColor);
                tagX += 36;

                // Branching indicators
                if (questInfo.hasChoices)
                {
                    var choiceRect = new Rect(tagX, rowRect.y + 26, 55, 14);
                    DrawMiniTag(choiceRect, "Choices", choiceColor);
                    tagX += 60;
                }
                if (questInfo.isBranchPath)
                {
                    var branchRect = new Rect(tagX, rowRect.y + 26, 50, 14);
                    DrawMiniTag(branchRect, "Branch", branchColor);
                }

                // Stage name (below tags)
                var stageNameStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 9,
                    normal = { textColor = new Color(0.55f, 0.5f, 0.7f) }
                };
                var stageNameRect = new Rect(iconRect.xMax + 10, rowRect.y + 42, 200, 12);
                GUI.Label(stageNameRect, $"Stage: {questInfo.stageName}", stageNameStyle);

                // Branch path indicator text
                if (questInfo.isBranchPath)
                {
                    var branchStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 8,
                        fontStyle = FontStyle.Italic,
                        normal = { textColor = branchColor }
                    };
                    var branchTextRect = new Rect(iconRect.xMax + 120, rowRect.y + 42, 150, 12);
                    GUI.Label(branchTextRect, "(reached via player choice)", branchStyle);
                }

                // Object field
                var fieldRect = new Rect(rowRect.xMax - padding - 140, rowRect.y + (rowHeight - EditorGUIUtility.singleLineHeight) / 2f,
                                        140, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(fieldRect, quest, typeof(Quest_SO), false);

                GUILayout.Space(2);
            }
        }

        private void DrawQuickActionsSection()
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            GUILayout.Space(8);

            // Add Completion Condition Section
            DrawSectionHeader("Add Completion Condition", new Color(0.4f, 0.8f, 0.4f));
            DrawQuickActionInfo("Add a condition that completes this task when met.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Add Condition...", GUILayout.Height(28)))
            {
                ShowConditionPicker(false);
            }

            if (GUILayout.Button("Create Event Condition", GUILayout.Height(28)))
            {
                CreateEventCondition(false);
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Add Failure Condition Section
            DrawSectionHeader("Add Failure Condition", new Color(0.9f, 0.4f, 0.4f));
            DrawQuickActionInfo("Add a condition that fails this task when met.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Add Condition...", GUILayout.Height(28)))
            {
                ShowConditionPicker(true);
            }

            if (GUILayout.Button("Create Event Condition", GUILayout.Height(28)))
            {
                CreateEventCondition(true);
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Create & Wire Event Section
            DrawSectionHeader("Create & Wire Event", new Color(0.9f, 0.7f, 0.3f));
            DrawQuickActionInfo("Create a new GameEvent and a condition that listens to it, then wire it to this task.");

            DrawEventCreationButtons();

            GUI.enabled = wasEnabled;
        }

        private void DrawEventCreationButtons()
        {
            var eventTypes = new[]
            {
                ("Bool Event", typeof(GameEventBool_SO), new Color(0.25f, 0.55f, 0.3f)),
                ("Int Event", typeof(GameEventInt_SO), new Color(0.2f, 0.45f, 0.7f)),
                ("Float Event", typeof(GameEventFloat_SO), new Color(0.5f, 0.4f, 0.7f)),
                ("String Event", typeof(GameEventString_SO), new Color(0.6f, 0.45f, 0.2f)),
            };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            foreach (var (label, type, color) in eventTypes)
            {
                var originalBg = GUI.backgroundColor;
                GUI.backgroundColor = color;

                if (GUILayout.Button(label, GUILayout.Height(28)))
                {
                    CreateAndWireEvent(type);
                }

                GUI.backgroundColor = originalBg;
                GUILayout.Space(4);
            }

            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationSection()
        {
            var errors = GetValidationErrors();
            var warnings = GetValidationWarnings();
            var localizationIssues = GetLocalizationIssues();

            GUILayout.Space(8);
            int totalErrors = errors.Count;
            int totalWarnings = warnings.Count + localizationIssues.Count;

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

            if (errors.Count > 0)
            {
                DrawSectionHeader("Errors", new Color(0.9f, 0.4f, 0.4f));
                foreach (var error in errors)
                {
                    DrawValidationItem(error, new Color(0.9f, 0.35f, 0.35f));
                }
                GUILayout.Space(8);
            }

            if (warnings.Count > 0)
            {
                DrawSectionHeader("Warnings", new Color(0.9f, 0.7f, 0.3f));
                foreach (var warning in warnings)
                {
                    DrawValidationItem(warning, new Color(0.9f, 0.7f, 0.3f));
                }
                GUILayout.Space(8);
            }

            if (localizationIssues.Count > 0)
            {
                DrawSectionHeader("Localization", new Color(0.6f, 0.7f, 0.9f));
                foreach (var issue in localizationIssues)
                {
                    DrawValidationItem(issue, new Color(0.6f, 0.7f, 0.9f));
                }
            }
        }

        #endregion

        #region Helper Methods

        private (string name, string icon, Color color) GetTaskTypeInfo()
        {
            var typeName = GetType().Name.Replace("Task", "").Replace("_SO", "");

            return typeName switch
            {
                "Int" => ("Int Task", "#", new Color(0.2f, 0.45f, 0.7f)),
                "Bool" => ("Bool Task", "?", new Color(0.25f, 0.55f, 0.3f)),
                "String" => ("String Task", "T", new Color(0.6f, 0.45f, 0.2f)),
                "Location" => ("Location Task", "@", new Color(0.55f, 0.3f, 0.55f)),
                "Timed" => ("Timed Task", "T", new Color(0.7f, 0.3f, 0.3f)),
                "Discovery" => ("Discovery Task", "!", new Color(0.55f, 0.5f, 0.2f)),
                _ => (typeName + " Task", "*", new Color(0.4f, 0.4f, 0.4f))
            };
        }

        private List<(Quest_SO quest, int stageIndex, string stageName, int groupIndex, bool hasChoices, bool isBranchPath)> FindContainingQuests()
        {
            var result = new List<(Quest_SO, int, string, int, bool, bool)>();

            var guids = AssetDatabase.FindAssets("t:Quest_SO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var quest = AssetDatabase.LoadAssetAtPath<Quest_SO>(path);

                if (quest != null && quest.Stages != null)
                {
                    bool found = false;
                    for (int s = 0; s < quest.Stages.Count && !found; s++)
                    {
                        var stage = quest.Stages[s];
                        if (stage.TaskGroups != null)
                        {
                            for (int g = 0; g < stage.TaskGroups.Count && !found; g++)
                            {
                                var group = stage.TaskGroups[g];
                                if (group.Tasks != null && group.Tasks.Contains(this))
                                {
                                    // Check if this stage has player choices (branching point)
                                    bool hasChoices = stage.Transitions != null &&
                                        stage.Transitions.Any(t => t.IsPlayerChoice);

                                    // Check if this stage is a branch path (reached via player choice)
                                    bool isBranchPath = quest.Stages.Any(st =>
                                        st.Transitions != null &&
                                        st.Transitions.Any(t => t.IsPlayerChoice && t.TargetStageIndex == stage.StageIndex));

                                    result.Add((quest, stage.StageIndex, stage.StageName, g, hasChoices, isBranchPath));
                                    found = true;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void DrawSectionHeader(string title, Color color)
        {
            var style = new GUIStyle(Styles.SectionHeader)
            {
                normal = { textColor = color }
            };
            EditorGUILayout.LabelField(title, style);
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

        private void DrawConditionList(string label, List<Condition_SO> conditionsList, Color accentColor)
        {
            var cardBg = new Color(0.2f, 0.2f, 0.2f);

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

            if (conditionsList == null || conditionsList.Count == 0)
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

            foreach (var condition in conditionsList)
            {
                if (condition == null) continue;

                var rowRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
                var cardRect = new Rect(rowRect.x + 8, rowRect.y, rowRect.width - 8, rowRect.height - 2);
                DrawRoundedRect(cardRect, cardBg, 3f);

                var dotRect = new Rect(cardRect.x + 8, cardRect.y + 8, 8, 8);
                DrawRoundedRect(dotRect, accentColor, 4f);

                var fieldRect = new Rect(cardRect.x + 24, cardRect.y + 2, cardRect.width - 32, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(fieldRect, condition, typeof(Condition_SO), false);
            }

            GUILayout.Space(4);
        }

        private void DrawStatCard(Rect rect, Color bgColor, string value, string label, Color accentColor)
        {
            DrawRoundedRect(rect, bgColor, 4f);

            var accentRect = new Rect(rect.x, rect.y, rect.width, 3);
            DrawRoundedRect(accentRect, accentColor, 2f);

            var valueRect = new Rect(rect.x, rect.y + 8, rect.width, 24);
            var valueStyle = new GUIStyle(Styles.StatValue) { normal = { textColor = accentColor } };
            GUI.Label(valueRect, value, valueStyle);

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

        private void DrawRoundedRect(Rect rect, Color color, float radius = 0)
        {
            EditorGUI.DrawRect(rect, color);
        }

        private void DrawValidationStatus(string message, Color color, bool success)
        {
            var rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            var bgColor = success ? new Color(0.15f, 0.25f, 0.18f) : new Color(0.25f, 0.15f, 0.15f);
            EditorGUI.DrawRect(rect, bgColor);

            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
            var iconRect = new Rect(rect.x + 12, rect.y + 8, 24, 24);
            GUI.Label(iconRect, success ? "\u2713" : "\u2716", iconStyle);

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

            var accentRect = new Rect(cardRect.x, cardRect.y, 3, cardRect.height);
            EditorGUI.DrawRect(accentRect, accentColor);

            var msgStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            var msgRect = new Rect(cardRect.x + 12, cardRect.y, cardRect.width - 16, cardRect.height);
            GUI.Label(msgRect, message, msgStyle);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates that an event-driven condition has its target value set.
        /// Returns an error message if invalid, null if valid.
        /// Can be called by Quest_SO to validate conditions.
        /// </summary>
        public static string ValidateEventDrivenCondition(Condition_SO condition, string contextName)
        {
            if (condition == null) return null;

            // Check if it's an event-driven condition
            if (condition is not IConditionEventDriven) return null;

            // Use reflection to check for targetValue field
            var targetValueField = condition.GetType().GetField("targetValue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (targetValueField == null) return null;

            var targetValue = targetValueField.GetValue(condition);

            // Check if target value is null or unset
            if (targetValue == null)
            {
                return $"{contextName}: Event-driven condition '{condition.name}' has no target value set";
            }

            // For Unity Objects, also check if it's a valid reference
            if (targetValue is UnityEngine.Object unityObj && unityObj == null)
            {
                return $"{contextName}: Event-driven condition '{condition.name}' has null target value";
            }

            return null;
        }

        /// <summary>
        /// Validates all event-driven conditions in a list.
        /// </summary>
        public static List<string> ValidateEventDrivenConditions(List<Condition_SO> conditionsList, string contextName)
        {
            var issues = new List<string>();
            if (conditionsList == null) return issues;

            foreach (var condition in conditionsList)
            {
                var issue = ValidateEventDrivenCondition(condition, contextName);
                if (issue != null) issues.Add(issue);

                // Also check composite conditions recursively
                if (condition is CompositeCondition_SO composite && composite.Conditions != null)
                {
                    issues.AddRange(ValidateEventDrivenConditions(composite.Conditions.ToList(), contextName));
                }
            }

            return issues;
        }

        /// <summary>
        /// Returns validation issues for this task's event-driven conditions.
        /// Called by Quest_SO.Odin for aggregated validation.
        /// </summary>
        public List<string> GetEventDrivenConditionIssues()
        {
            var issues = new List<string>();
            issues.AddRange(ValidateEventDrivenConditions(conditions, $"Task '{devName}'"));
            issues.AddRange(ValidateEventDrivenConditions(failureConditions, $"Task '{devName}' Failure"));
            return issues;
        }

        private List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(devName))
                errors.Add("Dev Name is empty");

            if (string.IsNullOrEmpty(taskId))
                errors.Add("Task ID is missing");

            if (conditions != null)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (conditions[i] == null)
                        errors.Add($"Completion condition at index {i} is null");
                }
            }

            if (failureConditions != null)
            {
                for (int i = 0; i < failureConditions.Count; i++)
                {
                    if (failureConditions[i] == null)
                        errors.Add($"Failure condition at index {i} is null");
                }
            }

            // Add event-driven condition validation
            errors.AddRange(GetEventDrivenConditionIssues());

            return errors;
        }

        private List<string> GetValidationWarnings()
        {
            var warnings = new List<string>();

            if (conditions == null || conditions.Count == 0)
                warnings.Add("No completion conditions - task won't auto-complete");

            if (conditions != null)
            {
                foreach (var cond in conditions)
                {
                    if (cond != null && !(cond is IConditionEventDriven))
                        warnings.Add($"Condition '{cond.name}' is not event-driven");
                }
            }

            var containingQuests = FindContainingQuests();
            if (containingQuests.Count == 0)
                warnings.Add("Task is not used by any quest");

            return warnings;
        }

        private List<string> GetLocalizationIssues()
        {
            var issues = new List<string>();

            if (displayName != null && displayName.IsEmpty)
                issues.Add("Display Name localization not configured");

            if (taskDescription != null && taskDescription.IsEmpty)
                issues.Add("Task Description localization not configured");

            return issues;
        }

        #endregion

        #region Quick Actions Implementation

        private bool _isPickingForFailure;

        private void ShowConditionPicker(bool forFailure)
        {
            _isPickingForFailure = forFailure;
            EditorGUIUtility.ShowObjectPicker<Condition_SO>(null, false, "", GUIUtility.GetControlID(FocusType.Passive));
            EditorApplication.update += OnConditionPickerUpdate;
        }

        private void OnConditionPickerUpdate()
        {
            if (Event.current != null && Event.current.commandName == "ObjectSelectorClosed")
            {
                EditorApplication.update -= OnConditionPickerUpdate;

                var selected = EditorGUIUtility.GetObjectPickerObject() as Condition_SO;
                if (selected != null)
                {
                    AddCondition(selected, _isPickingForFailure);
                }
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == 0)
            {
                EditorApplication.update -= OnConditionPickerUpdate;
            }
        }

        private void AddCondition(Condition_SO condition, bool toFailure)
        {
            if (toFailure)
            {
                if (failureConditions == null)
                    failureConditions = new List<Condition_SO>();
                failureConditions.Add(condition);
            }
            else
            {
                if (conditions == null)
                    conditions = new List<Condition_SO>();
                conditions.Add(condition);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Task_SO] Added condition: {condition.name}");
        }

        private void CreateEventCondition(bool forFailure)
        {
            var taskPath = AssetDatabase.GetAssetPath(this);
            var taskFolder = System.IO.Path.GetDirectoryName(taskPath);
            var conditionsFolder = System.IO.Path.Combine(taskFolder, "Conditions");

            if (!AssetDatabase.IsValidFolder(conditionsFolder))
            {
                var parentFolder = System.IO.Path.GetDirectoryName(conditionsFolder);
                AssetDatabase.CreateFolder(parentFolder, "Conditions");
            }

            // Create a bool condition by default
            var condition = ScriptableObject.CreateInstance<ConditionBool_SO>();

            var suffix = forFailure ? "Fail" : "Complete";
            var conditionName = $"SO_Condition_{devName}_{suffix}.asset";
            var conditionPath = System.IO.Path.Combine(conditionsFolder, conditionName);
            conditionPath = AssetDatabase.GenerateUniqueAssetPath(conditionPath);

            AssetDatabase.CreateAsset(condition, conditionPath);
            AssetDatabase.SaveAssets();

            AddCondition(condition, forFailure);
            Selection.activeObject = condition;
            EditorGUIUtility.PingObject(condition);
        }

        private void CreateAndWireEvent(System.Type eventType)
        {
            var taskPath = AssetDatabase.GetAssetPath(this);
            var taskFolder = System.IO.Path.GetDirectoryName(taskPath);
            var eventsFolder = System.IO.Path.Combine(taskFolder, "Events");
            var conditionsFolder = System.IO.Path.Combine(taskFolder, "Conditions");

            // Create folders if needed
            if (!AssetDatabase.IsValidFolder(eventsFolder))
            {
                var parentFolder = System.IO.Path.GetDirectoryName(eventsFolder);
                AssetDatabase.CreateFolder(parentFolder, "Events");
            }
            if (!AssetDatabase.IsValidFolder(conditionsFolder))
            {
                var parentFolder = System.IO.Path.GetDirectoryName(conditionsFolder);
                AssetDatabase.CreateFolder(parentFolder, "Conditions");
            }

            // Create the event
            var gameEvent = ScriptableObject.CreateInstance(eventType);
            var eventName = $"SO_Event_{devName}.asset";
            var eventPath = System.IO.Path.Combine(eventsFolder, eventName);
            eventPath = AssetDatabase.GenerateUniqueAssetPath(eventPath);
            AssetDatabase.CreateAsset(gameEvent, eventPath);

            // Create matching condition
            Condition_SO condition = null;
            if (eventType == typeof(GameEventBool_SO))
            {
                var boolCond = ScriptableObject.CreateInstance<ConditionBool_SO>();
                // Wire the event using reflection
                var eventField = typeof(ConditionBool_SO).GetField("gameEvent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                eventField?.SetValue(boolCond, gameEvent);
                condition = boolCond;
            }
            else if (eventType == typeof(GameEventInt_SO))
            {
                var intCond = ScriptableObject.CreateInstance<ConditionInt_SO>();
                var eventField = typeof(ConditionInt_SO).GetField("gameEvent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                eventField?.SetValue(intCond, gameEvent);
                condition = intCond;
            }
            else if (eventType == typeof(GameEventFloat_SO))
            {
                var floatCond = ScriptableObject.CreateInstance<ConditionFloat_SO>();
                var eventField = typeof(ConditionFloat_SO).GetField("gameEvent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                eventField?.SetValue(floatCond, gameEvent);
                condition = floatCond;
            }
            else if (eventType == typeof(GameEventString_SO))
            {
                var stringCond = ScriptableObject.CreateInstance<ConditionString_SO>();
                var eventField = typeof(ConditionString_SO).GetField("gameEvent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                eventField?.SetValue(stringCond, gameEvent);
                condition = stringCond;
            }

            if (condition != null)
            {
                var conditionName = $"SO_Condition_{devName}.asset";
                var conditionPath = System.IO.Path.Combine(conditionsFolder, conditionName);
                conditionPath = AssetDatabase.GenerateUniqueAssetPath(conditionPath);
                AssetDatabase.CreateAsset(condition, conditionPath);

                AddCondition(condition, false);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Task_SO] Created event and condition: {eventPath}");
            EditorGUIUtility.PingObject(gameEvent);
        }

        #endregion
#endif
    }
}
#endif
