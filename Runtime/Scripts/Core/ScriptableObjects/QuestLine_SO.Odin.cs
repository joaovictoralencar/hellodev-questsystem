#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using UnityEngine;
using HelloDev.QuestSystem.Conditions;
using HelloDev.QuestSystem.QuestLines;
using HelloDev.Conditions;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    public partial class QuestLine_SO
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
        [OnInspectorGUI("DrawPrerequisiteSection")]
        [PropertyOrder(-80)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _prerequisitePlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawQuestsSection")]
        [PropertyOrder(-70)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _questsPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawRewardsSection")]
        [PropertyOrder(-60)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _rewardsPlaceholder => "";

        [TabGroup("Tabs", "Overview")]
        [OnInspectorGUI("DrawSettingsSection")]
        [PropertyOrder(-50)]
        [ShowInInspector, HideLabel, ReadOnly, DisplayAsString]
        private string _settingsPlaceholder => "";

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
            var accentColor = new Color(0.6f, 0.4f, 0.8f); // Purple for questlines

            // Color accent line at top
            var lineRect = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, accentColor);

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            // Icon
            if (icon != null)
            {
                var spriteRect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80));
                GUI.DrawTexture(spriteRect, icon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Placeholder icon
                var placeholderRect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80));
                EditorGUI.DrawRect(placeholderRect, new Color(0.2f, 0.2f, 0.2f));
                var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 32,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.4f, 0.4f, 0.4f) }
                };
                GUI.Label(placeholderRect, "QL", iconStyle);
            }

            GUILayout.Space(12);

            // Info section
            EditorGUILayout.BeginVertical();

            // Row 1: Tags
            EditorGUILayout.BeginHorizontal();
            DrawTag("QuestLine", accentColor);
            GUILayout.Space(6);
            if (quests != null && quests.Count > 0)
            {
                DrawTag($"{quests.Count} Quests", new Color(0.35f, 0.35f, 0.35f));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Row 2: QuestLine Name
            var displayNameText = string.IsNullOrEmpty(devName) ? "Unnamed QuestLine" : devName;
            EditorGUILayout.LabelField(displayNameText, Styles.Title);

            // Row 3: GUID
            EditorGUILayout.LabelField(questLineId ?? "No GUID", Styles.Subtitle);

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
            int questCount = quests?.Count ?? 0;
            int validQuests = quests?.Count(q => q != null) ?? 0;
            int rewardCount = completionRewards?.Count ?? 0;
            string progressText = requireSequentialCompletion ? "Linear" : "Free";

            // Draw stat cards
            DrawStatCard(new Rect(startX, startY, cardWidth - 5, cardHeight), cardBg,
                questCount.ToString(), "Quests", new Color(0.6f, 0.4f, 0.8f));

            DrawStatCard(new Rect(startX + cardWidth, startY, cardWidth - 5, cardHeight), cardBg,
                validQuests.ToString(), "Valid", new Color(0.4f, 0.7f, 0.4f));

            DrawStatCard(new Rect(startX + cardWidth * 2, startY, cardWidth - 5, cardHeight), cardBg,
                rewardCount.ToString(), "Rewards", new Color(1f, 0.8f, 0.3f));

            DrawStatCard(new Rect(startX + cardWidth * 3, startY, cardWidth - 5, cardHeight), cardBg,
                progressText, "Mode", new Color(0.4f, 0.7f, 1f));

            GUILayout.Space(4);
        }

        private void DrawPrerequisiteSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Prerequisite QuestLine", new Color(0.6f, 0.85f, 1f));

            if (prerequisiteLine == null)
            {
                DrawEmptyState("No prerequisite - available immediately");
                return;
            }

            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            float rowHeight = 50f;

            var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
            DrawRoundedRect(rowRect, cardBg, 4f);

            float padding = 8f;
            float iconSize = 40f;

            // Icon
            var iconRect = new Rect(rowRect.x + padding, rowRect.y + (rowHeight - iconSize) / 2f, iconSize, iconSize);
            if (prerequisiteLine.Icon != null)
            {
                GUI.DrawTexture(iconRect, prerequisiteLine.Icon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(iconRect, new Color(0.3f, 0.3f, 0.3f));
            }

            // Name
            var nameRect = new Rect(iconRect.xMax + 10, rowRect.y + 8, rowRect.width - iconSize - 180, 18);
            GUI.Label(nameRect, prerequisiteLine.DevName, Styles.ItemName);

            // Quest count tag
            var countText = $"{prerequisiteLine.QuestCount} quests";
            var tagRect = new Rect(iconRect.xMax + 10, rowRect.y + 28, 80, 14);
            DrawMiniTag(tagRect, countText, new Color(0.4f, 0.4f, 0.4f));

            // Object field
            var fieldRect = new Rect(rowRect.xMax - padding - 150, rowRect.y + (rowHeight - EditorGUIUtility.singleLineHeight) / 2f,
                                    150, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(fieldRect, prerequisiteLine, typeof(QuestLine_SO), false);

            GUILayout.Space(4);
        }

        private void DrawQuestsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Quests in Line", new Color(0.7f, 0.5f, 1f));

            if (quests == null || quests.Count == 0)
            {
                DrawEmptyState("No quests added - use Quick Actions to add quests");
                return;
            }

            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            var accentColor = new Color(0.6f, 0.4f, 0.8f);

            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                float rowHeight = 44f;

                var rowRect = GUILayoutUtility.GetRect(0, rowHeight, GUILayout.ExpandWidth(true));
                DrawRoundedRect(rowRect, cardBg, 4f);

                float padding = 8f;

                // Quest number
                var numStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = accentColor }
                };
                var numRect = new Rect(rowRect.x + padding, rowRect.y + (rowHeight - 24) / 2f, 24, 24);
                GUI.Label(numRect, $"{i + 1}", numStyle);

                if (quest != null)
                {
                    // Quest icon
                    float iconSize = 32f;
                    var iconRect = new Rect(numRect.xMax + 8, rowRect.y + (rowHeight - iconSize) / 2f, iconSize, iconSize);
                    if (quest.QuestSprite != null)
                    {
                        GUI.DrawTexture(iconRect, quest.QuestSprite.texture, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        EditorGUI.DrawRect(iconRect, new Color(0.25f, 0.25f, 0.25f));
                    }

                    // Quest name
                    var nameRect = new Rect(iconRect.xMax + 10, rowRect.y + 6, rowRect.width - 250, 18);
                    GUI.Label(nameRect, quest.DevName, Styles.ItemName);

                    // Quest type tag
                    if (quest.QuestType != null)
                    {
                        var typeRect = new Rect(iconRect.xMax + 10, rowRect.y + 26, 80, 14);
                        DrawMiniTag(typeRect, quest.QuestType.DevName, quest.QuestType.Color);
                    }

                    // Task count
                    var taskCountStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                    };
                    var taskCountRect = new Rect(rowRect.xMax - padding - 210, rowRect.y + 14, 60, 16);
                    GUI.Label(taskCountRect, $"{quest.AllTasks?.Count ?? 0} tasks", taskCountStyle);

                    // Object field
                    var fieldRect = new Rect(rowRect.xMax - padding - 150, rowRect.y + (rowHeight - EditorGUIUtility.singleLineHeight) / 2f,
                                            150, EditorGUIUtility.singleLineHeight);
                    EditorGUI.ObjectField(fieldRect, quest, typeof(Quest_SO), false);
                }
                else
                {
                    // Missing quest warning
                    var errorStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(0.9f, 0.4f, 0.4f) }
                    };
                    var errorRect = new Rect(numRect.xMax + 16, rowRect.y + (rowHeight - 16) / 2f, 150, 16);
                    GUI.Label(errorRect, "Missing Quest!", errorStyle);
                }

                GUILayout.Space(2);
            }
        }

        private void DrawRewardsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Completion Rewards", new Color(1f, 0.8f, 0.4f));

            if (completionRewards == null || completionRewards.Count == 0)
            {
                DrawEmptyState("No completion rewards configured");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            var cardBg = new Color(0.2f, 0.2f, 0.2f);
            var goldColor = new Color(1f, 0.8f, 0.3f);

            foreach (var reward in completionRewards)
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

        private void DrawSettingsSection()
        {
            GUILayout.Space(8);
            DrawSectionHeader("Settings", new Color(0.5f, 0.7f, 0.9f));

            var cardBg = new Color(0.2f, 0.2f, 0.2f);

            // Sequential completion
            var seqRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            DrawRoundedRect(seqRect, cardBg, 4f);

            var seqLabelRect = new Rect(seqRect.x + 12, seqRect.y + 5, 180, 18);
            GUI.Label(seqLabelRect, "Sequential Completion", Styles.ItemName);

            var seqValueColor = requireSequentialCompletion ? new Color(0.4f, 0.7f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            var seqValueRect = new Rect(seqRect.xMax - 80, seqRect.y + 6, 70, 16);
            DrawMiniTag(seqValueRect, requireSequentialCompletion ? "Required" : "Optional", seqValueColor);

            GUILayout.Space(4);

            // Fail on any quest failed
            var failRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            DrawRoundedRect(failRect, cardBg, 4f);

            var failLabelRect = new Rect(failRect.x + 12, failRect.y + 5, 180, 18);
            GUI.Label(failLabelRect, "Fail on Quest Failure", Styles.ItemName);

            var failValueColor = failOnAnyQuestFailed ? new Color(0.9f, 0.4f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            var failValueRect = new Rect(failRect.xMax - 80, failRect.y + 6, 70, 16);
            DrawMiniTag(failValueRect, failOnAnyQuestFailed ? "Enabled" : "Disabled", failValueColor);
        }

        #endregion

        #region Quick Actions

        private void DrawQuickActionsSection()
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            GUILayout.Space(8);

            // Add Quest Section
            DrawSectionHeader("Add Quest", new Color(0.7f, 0.5f, 1f));
            DrawQuickActionInfo("Add an existing quest to this questline.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Add Quest...", GUILayout.Height(32)))
            {
                ShowQuestPicker();
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Auto-populate Section
            DrawSectionHeader("Auto-Populate Quests", new Color(0.5f, 0.8f, 0.5f));
            DrawQuickActionInfo("Scan the parent folder for Quest_SO assets and add them to this questline.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Scan & Add Quests from Folder", GUILayout.Height(32)))
            {
                AutoPopulateQuestsFromFolder();
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Prerequisite Section
            DrawSectionHeader("Set Prerequisite QuestLine", new Color(0.6f, 0.85f, 1f));
            DrawQuickActionInfo("Require another questline to be completed before this one becomes available.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Set Prerequisite...", GUILayout.Height(32)))
            {
                ShowPrerequisitePicker();
            }

            if (prerequisiteLine != null)
            {
                GUILayout.Space(8);
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                if (GUILayout.Button("Clear", GUILayout.Height(32), GUILayout.Width(60)))
                {
                    prerequisiteLine = null;
                    EditorUtility.SetDirty(this);
                }
                GUI.backgroundColor = originalColor;
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(16);

            // Add Reward Section
            DrawSectionHeader("Add Completion Reward", new Color(1f, 0.8f, 0.4f));
            DrawQuickActionInfo("Add a reward that is granted when all quests in the line are completed.");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            if (GUILayout.Button("Add Reward...", GUILayout.Height(32)))
            {
                ShowRewardTypePicker();
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

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

        private void ShowQuestPicker()
        {
            EditorGUIUtility.ShowObjectPicker<Quest_SO>(null, false, "", GUIUtility.GetControlID(FocusType.Passive));
            EditorApplication.update += OnQuestPickerUpdate;
        }

        private void OnQuestPickerUpdate()
        {
            if (Event.current != null && Event.current.commandName == "ObjectSelectorClosed")
            {
                EditorApplication.update -= OnQuestPickerUpdate;

                var selectedQuest = EditorGUIUtility.GetObjectPickerObject() as Quest_SO;
                if (selectedQuest != null)
                {
                    AddQuestToLine(selectedQuest);
                }
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == 0)
            {
                EditorApplication.update -= OnQuestPickerUpdate;
            }
        }

        private void AddQuestToLine(Quest_SO quest)
        {
            if (quests == null)
            {
                quests = new List<Quest_SO>();
            }

            if (quests.Contains(quest))
            {
                Debug.LogWarning($"[QuestLine_SO] Quest '{quest.DevName}' is already in this questline.");
                return;
            }

            quests.Add(quest);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[QuestLine_SO] Added quest: {quest.DevName}");
        }

        private void AutoPopulateQuestsFromFolder()
        {
            var questLinePath = AssetDatabase.GetAssetPath(this);
            var questLineFolder = System.IO.Path.GetDirectoryName(questLinePath);
            var parentFolder = System.IO.Path.GetDirectoryName(questLineFolder);

            // Search for Quest_SO in parent folder and subfolders
            var guids = AssetDatabase.FindAssets("t:Quest_SO", new[] { parentFolder });
            var foundQuests = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<Quest_SO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(q => q != null)
                .ToList();

            if (foundQuests.Count == 0)
            {
                Debug.LogWarning($"[QuestLine_SO] No Quest_SO assets found in: {parentFolder}");
                return;
            }

            if (quests == null)
            {
                quests = new List<Quest_SO>();
            }

            var existingQuests = new HashSet<Quest_SO>(quests);
            int addedCount = 0;

            foreach (var quest in foundQuests)
            {
                if (!existingQuests.Contains(quest))
                {
                    quests.Add(quest);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                Debug.Log($"[QuestLine_SO] Added {addedCount} quests. Total: {quests.Count}");
            }
            else
            {
                Debug.Log($"[QuestLine_SO] All {foundQuests.Count} quests are already in this questline.");
            }
        }

        private void ShowPrerequisitePicker()
        {
            EditorGUIUtility.ShowObjectPicker<QuestLine_SO>(null, false, "", GUIUtility.GetControlID(FocusType.Passive));
            EditorApplication.update += OnPrerequisitePickerUpdate;
        }

        private void OnPrerequisitePickerUpdate()
        {
            if (Event.current != null && Event.current.commandName == "ObjectSelectorClosed")
            {
                EditorApplication.update -= OnPrerequisitePickerUpdate;

                var selected = EditorGUIUtility.GetObjectPickerObject() as QuestLine_SO;
                if (selected != null && selected != this)
                {
                    prerequisiteLine = selected;
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[QuestLine_SO] Set prerequisite: {selected.DevName}");
                }
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == 0)
            {
                EditorApplication.update -= OnPrerequisitePickerUpdate;
            }
        }

        private void ShowRewardTypePicker()
        {
            EditorGUIUtility.ShowObjectPicker<QuestRewardType_SO>(null, false, "", GUIUtility.GetControlID(FocusType.Passive));
            EditorApplication.update += OnRewardTypePickerUpdate;
        }

        private void OnRewardTypePickerUpdate()
        {
            if (Event.current != null && Event.current.commandName == "ObjectSelectorClosed")
            {
                EditorApplication.update -= OnRewardTypePickerUpdate;

                var selected = EditorGUIUtility.GetObjectPickerObject() as QuestRewardType_SO;
                if (selected != null)
                {
                    if (completionRewards == null)
                    {
                        completionRewards = new List<RewardInstance>();
                    }

                    completionRewards.Add(new RewardInstance { RewardType = selected, Amount = 100 });
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[QuestLine_SO] Added reward: {selected.name}");
                }
            }

            if (EditorGUIUtility.GetObjectPickerControlID() == 0)
            {
                EditorApplication.update -= OnRewardTypePickerUpdate;
            }
        }

        #endregion

        #region Validation

        private void DrawValidationSection()
        {
            var errors = GetValidationErrors();
            var warnings = GetValidationWarnings();
            var localizationIssues = GetLocalizationIssues();
            var circularIssues = GetCircularDependencyIssues();

            errors.AddRange(circularIssues);

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

            // Errors
            if (errors.Count > 0)
            {
                DrawSectionHeader("Errors", new Color(0.9f, 0.4f, 0.4f));
                foreach (var error in errors)
                {
                    DrawValidationItem(error, new Color(0.9f, 0.35f, 0.35f));
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

            // Localization
            if (localizationIssues.Count > 0)
            {
                DrawSectionHeader("Localization", new Color(0.6f, 0.7f, 0.9f));
                foreach (var issue in localizationIssues)
                {
                    DrawValidationItem(issue, new Color(0.6f, 0.7f, 0.9f));
                }
            }
        }

        private List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(devName))
                errors.Add("Dev Name is empty");

            if (string.IsNullOrEmpty(questLineId))
                errors.Add("QuestLine ID is missing");

            if (quests == null || quests.Count == 0)
                errors.Add("No quests in this questline");
            else
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    if (quests[i] == null)
                        errors.Add($"Quest at index {i} is null");
                }

                // Check for duplicates
                var seen = new HashSet<Quest_SO>();
                foreach (var quest in quests)
                {
                    if (quest != null && !seen.Add(quest))
                        errors.Add($"Duplicate quest: {quest.DevName}");
                }
            }

            // Check rewards
            if (completionRewards != null)
            {
                for (int i = 0; i < completionRewards.Count; i++)
                {
                    if (completionRewards[i].RewardType == null)
                        errors.Add($"Reward at index {i} has no type");
                    else if (completionRewards[i].Amount <= 0)
                        errors.Add($"Reward '{completionRewards[i].RewardType.name}' has invalid amount");
                }
            }

            return errors;
        }

        private List<string> GetValidationWarnings()
        {
            var warnings = new List<string>();

            if (icon == null)
                warnings.Add("QuestLine icon not assigned");

            if (completionRewards == null || completionRewards.Count == 0)
                warnings.Add("No completion rewards configured");

            return warnings;
        }

        private List<string> GetLocalizationIssues()
        {
            var issues = new List<string>();

            if (displayName != null && displayName.IsEmpty)
                issues.Add("Display Name localization not configured");

            if (description != null && description.IsEmpty)
                issues.Add("Description localization not configured");

            return issues;
        }

        private List<string> GetCircularDependencyIssues()
        {
            var issues = new List<string>();

            if (prerequisiteLine == null) return issues;

            var visited = new HashSet<QuestLine_SO> { this };
            var current = prerequisiteLine;
            var chain = new List<string> { devName };

            while (current != null)
            {
                chain.Add(current.DevName);

                if (visited.Contains(current))
                {
                    issues.Add($"Circular dependency: {string.Join(" → ", chain)}");
                    break;
                }

                visited.Add(current);
                current = current.PrerequisiteLine;
            }

            return issues;
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

        #region Helper Methods

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

        #endregion
#endif
    }
}
#endif
