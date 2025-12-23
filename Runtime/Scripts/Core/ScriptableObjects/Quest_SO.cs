using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Conditions;
using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.TaskGroups;
using HelloDev.Utils;
using UnityEngine;
using UnityEngine.Localization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HelloDev.QuestSystem.ScriptableObjects
{
    /// <summary>
    /// A ScriptableObject that represents the static data for a quest.
    /// This is the primary asset for configuring quests in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "HelloDev/Quest System/Scriptable Objects/Quest")]
    public class Quest_SO : RuntimeScriptableObject
    {
        #region Quest Overview (Designer View)

#if ODIN_INSPECTOR && UNITY_EDITOR
        private static readonly Color HeaderColor = new Color(0.18f, 0.18f, 0.22f);
        private static readonly Color RowEvenColor = new Color(0.22f, 0.22f, 0.26f);
        private static readonly Color RowOddColor = new Color(0.25f, 0.25f, 0.29f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f);

        [FoldoutGroup("Quest Overview", expanded: true, Order = -100)]
        [OnInspectorGUI("DrawQuestOverview", append: false)]
        [ShowInInspector]
        [HideLabel]
        [PropertyOrder(-100)]
        private int _overviewDummy; // Dummy field to attach the drawer

        private void DrawQuestOverview()
        {
            if (Event.current == null) return;

            // Create styles
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            var statStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            var tableHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.6f, 0.6f, 0.65f) },
                padding = new RectOffset(6, 6, 4, 4)
            };

            var tableCellStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 3, 3),
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            var indexStyle = new GUIStyle(tableCellStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            // Main container
            EditorGUILayout.BeginVertical();
            GUILayout.Space(4);

            // Quest Header
            string typeName = questType != null ? questType.name.Replace("SO_QuestType_", "").ToUpper() : "UNASSIGNED";
            string questTitle = !string.IsNullOrEmpty(devName) ? devName : "Unnamed Quest";

            EditorGUILayout.LabelField(questTitle, headerStyle);
            GUILayout.Space(2);

            // Stats Row
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DrawStatBadge(typeName, GetQuestTypeColor());
            GUILayout.Space(8);
            DrawStatBadge(recommendedLevel > 0 ? $"LVL {recommendedLevel}" : "ANY LVL", new Color(0.5f, 0.5f, 0.55f));
            GUILayout.Space(8);
            DrawStatBadge($"{AllTasks?.Count ?? 0} TASKS", new Color(0.4f, 0.5f, 0.6f));
            GUILayout.Space(8);
            DrawStatBadge($"{rewards?.Count ?? 0} REWARDS", new Color(0.5f, 0.45f, 0.3f));

            int startCond = startConditions?.Count(c => c != null) ?? 0;
            int failCond = failureConditions?.Count(c => c != null) ?? 0;
            if (startCond > 0 || failCond > 0)
            {
                GUILayout.Space(8);
                DrawStatBadge($"{startCond + failCond} COND", new Color(0.45f, 0.4f, 0.5f));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12);

            // Task Table
            var allTasksList = AllTasks;
            if (allTasksList != null && allTasksList.Count > 0)
            {
                // Table Header
                var headerRect = EditorGUILayout.BeginHorizontal();
                DrawTableBackground(headerRect, HeaderColor);

                GUILayout.Space(2);
                EditorGUILayout.LabelField("#", tableHeaderStyle, GUILayout.Width(28));
                EditorGUILayout.LabelField("TYPE", tableHeaderStyle, GUILayout.Width(90));
                EditorGUILayout.LabelField("TASK NAME", tableHeaderStyle, GUILayout.MinWidth(100));
                EditorGUILayout.LabelField("CONDITIONS", tableHeaderStyle, GUILayout.Width(80));
                GUILayout.Space(2);

                EditorGUILayout.EndHorizontal();

                // Draw separator
                var separatorRect = GUILayoutUtility.GetRect(1, 1);
                EditorGUI.DrawRect(separatorRect, BorderColor);

                // Table Rows
                for (int i = 0; i < allTasksList.Count; i++)
                {
                    var task = allTasksList[i];
                    bool isEven = i % 2 == 0;

                    var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
                    DrawTableBackground(rowRect, isEven ? RowEvenColor : RowOddColor);

                    GUILayout.Space(2);

                    if (task == null)
                    {
                        // Null task - show error
                        EditorGUILayout.LabelField((i + 1).ToString(), indexStyle, GUILayout.Width(28));

                        var errorStyle = new GUIStyle(tableCellStyle) { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
                        EditorGUILayout.LabelField("ERROR", errorStyle, GUILayout.Width(90));
                        EditorGUILayout.LabelField("Missing Task Reference!", errorStyle, GUILayout.MinWidth(100));
                        EditorGUILayout.LabelField("-", tableCellStyle, GUILayout.Width(80));
                    }
                    else
                    {
                        var (taskTypeName, typeColor) = GetTaskTypeInfo(task);
                        int condCount = task.Conditions?.Count(c => c != null) ?? 0;

                        // Index
                        EditorGUILayout.LabelField((i + 1).ToString(), indexStyle, GUILayout.Width(28));

                        // Type with color
                        var typeStyle = new GUIStyle(tableCellStyle)
                        {
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = typeColor }
                        };
                        EditorGUILayout.LabelField(taskTypeName, typeStyle, GUILayout.Width(90));

                        // Task Name
                        EditorGUILayout.LabelField(task.DevName, tableCellStyle, GUILayout.MinWidth(100));

                        // Conditions
                        string condText = condCount > 0 ? condCount.ToString() : "-";
                        var condStyle = new GUIStyle(tableCellStyle)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = condCount > 0 ? new Color(0.6f, 0.8f, 0.6f) : new Color(0.5f, 0.5f, 0.5f) }
                        };
                        EditorGUILayout.LabelField(condText, condStyle, GUILayout.Width(80));
                    }

                    GUILayout.Space(2);
                    EditorGUILayout.EndHorizontal();
                }

                // Bottom border
                var bottomRect = GUILayoutUtility.GetRect(1, 1);
                EditorGUI.DrawRect(bottomRect, BorderColor);
            }
            else
            {
                // No tasks message
                var warningStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.8f, 0.6f, 0.4f) }
                };
                EditorGUILayout.LabelField("No task groups configured - quest will complete immediately", warningStyle);
            }

            GUILayout.Space(8);
            EditorGUILayout.EndVertical();
        }

        private void DrawStatBadge(string text, Color bgColor)
        {
            var content = new GUIContent(text);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                padding = new RectOffset(6, 6, 2, 2)
            };

            var size = style.CalcSize(content);
            var rect = GUILayoutUtility.GetRect(size.x + 4, size.y + 4);

            // Draw rounded background
            EditorGUI.DrawRect(rect, bgColor);
            GUI.Label(rect, content, style);
        }

        private void DrawTableBackground(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, color);
            }
        }

        private Color GetQuestTypeColor()
        {
            if (questType == null) return new Color(0.5f, 0.5f, 0.5f);

            string typeName = questType.name.ToLower();
            if (typeName.Contains("main")) return new Color(0.8f, 0.65f, 0.2f);      // Gold
            if (typeName.Contains("secondary")) return new Color(0.5f, 0.65f, 0.8f); // Blue
            if (typeName.Contains("special")) return new Color(0.7f, 0.5f, 0.8f);    // Purple
            if (typeName.Contains("failed")) return new Color(0.7f, 0.35f, 0.35f);   // Red
            if (typeName.Contains("completed")) return new Color(0.4f, 0.7f, 0.4f);  // Green

            return new Color(0.5f, 0.5f, 0.55f);
        }

        private (string typeName, Color color) GetTaskTypeInfo(Task_SO task)
        {
            string fullTypeName = task.GetType().Name;
            string cleanName = fullTypeName.Replace("Task", "").Replace("_SO", "");

            return cleanName switch
            {
                "Int" => ("COUNTER", new Color(0.5f, 0.75f, 1f)),
                "Bool" => ("TOGGLE", new Color(0.5f, 0.9f, 0.5f)),
                "String" => ("TEXT", new Color(0.95f, 0.75f, 0.45f)),
                "Location" => ("LOCATION", new Color(0.9f, 0.55f, 0.9f)),
                "Timed" => ("TIMED", new Color(1f, 0.55f, 0.55f)),
                "Discovery" => ("DISCOVERY", new Color(0.95f, 0.9f, 0.45f)),
                _ => (cleanName.ToUpper(), new Color(0.7f, 0.7f, 0.7f))
            };
        }
#endif

        #endregion

        #region Identity

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(0)]
        [Required("Dev Name is required for identification.")]
#else
        [Header("Identity")]
#endif
        [Tooltip("Internal name for developers, used for identification in code.")]
        [SerializeField]
        private string devName;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(1)]
        [ReadOnly]
        [DisplayAsString]
#endif
        [Tooltip("A unique, permanent identifier for this quest. Auto-generated.")]
        [SerializeField]
        private string questId;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(2)]
#endif
        [Tooltip("The type of the quest. Use this to group quests together.")]
        [SerializeField]
        private QuestType_SO questType;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [PropertyOrder(3)]
#endif
        [Tooltip("The recommended level for the player to start this quest.")]
        [SerializeField]
        private int recommendedLevel = -1;

        #endregion

        #region Display

#if ODIN_INSPECTOR
        [FoldoutGroup("Display", expanded: true)]
        [PropertyOrder(10)]
#else
        [Header("Display")]
#endif
        [Tooltip("The localized display name of the quest.")]
        [SerializeField]
        private LocalizedString displayName;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(11)]
#endif
        [Tooltip("The localized description of the quest.")]
        [SerializeField]
        private LocalizedString questDescription;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(12)]
#endif
        [Tooltip("The localized quest location. Optional.")]
        [SerializeField]
        private LocalizedString questLocation;

#if ODIN_INSPECTOR
        [FoldoutGroup("Display")]
        [PropertyOrder(13)]
        [PreviewField(50, ObjectFieldAlignment.Left)]
#endif
        [Tooltip("An icon to display in the UI, representing the quest.")]
        [SerializeField]
        private Sprite questSprite;

        #endregion

        #region Task Groups

#if ODIN_INSPECTOR
        [FoldoutGroup("Task Groups", expanded: true)]
        [PropertyOrder(18)]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowFoldout = true)]
#else
        [Header("Task Groups")]
#endif
        [Tooltip("Task groups allow parallel, optional, and any-order task execution. Groups execute sequentially.")]
        [SerializeField]
        private List<TaskGroup> taskGroups = new();

        #endregion

        #region Conditions

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(30)]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("Start conditions should be event-driven for automatic quest activation.", InfoMessageType.Info)]
#else
        [Header("Conditions")]
#endif
        [Tooltip("The list of conditions that must be met to start this quest.")]
        [SerializeField]
        private List<Condition_SO> startConditions;

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(31)]
        [ListDrawerSettings(ShowFoldout = true)]
#endif
        [Tooltip("The list of conditions that, when all met, will cause this quest to fail.")]
        [SerializeField]
        private List<Condition_SO> failureConditions;

#if ODIN_INSPECTOR
        [FoldoutGroup("Conditions")]
        [PropertyOrder(32)]
        [ListDrawerSettings(ShowFoldout = true)]
        [InfoBox("When any of these conditions is met, the current in-progress task will fail.", InfoMessageType.Info)]
#endif
        [Tooltip("The list of conditions that, if any is met, will cause the current task to fail.")]
        [SerializeField]
        private List<Condition_SO> globalTaskFailureConditions;

        #endregion

        #region Rewards

#if ODIN_INSPECTOR
        [FoldoutGroup("Rewards")]
        [PropertyOrder(40)]
        [ListDrawerSettings(ShowFoldout = true)]
        [ValidateInput(nameof(ValidateRewards), "One or more rewards are invalid. Check for null types or zero amounts.")]
        [InfoBox("Rewards are automatically distributed when the quest is completed.", InfoMessageType.Info)]
#else
        [Header("Rewards")]
#endif
        [Tooltip("The list of rewards for completing this quest.")]
        [SerializeField]
        private List<RewardInstance> rewards;

        #endregion

        #region Properties

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
        /// Gets the list of task groups for this quest.
        /// </summary>
        public List<TaskGroup> TaskGroups => taskGroups ?? new List<TaskGroup>();

        /// <summary>
        /// Gets all tasks across all groups (flattened list).
        /// Use this when you need all tasks regardless of grouping.
        /// </summary>
        public List<Task_SO> AllTasks => taskGroups?.SelectMany(g => g.Tasks).Where(t => t != null).ToList() ?? new List<Task_SO>();

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and returns a new runtime instance of this quest.
        /// </summary>
        public QuestRuntime GetRuntimeQuest()
        {
            return new QuestRuntime(this);
        }

        #endregion

        #region Validation

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

            ValidateConfiguration();
            ValidateTaskGroups();
        }

        private void ValidateTaskGroups()
        {
            if (taskGroups == null) return;

            foreach (var group in taskGroups)
            {
                var warnings = group.Validate();
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"[Quest_SO] '{devName}': {warning}", this);
                }
            }
        }

        private void ValidateConfiguration()
        {
            // Validate task groups
            if (taskGroups == null || taskGroups.Count == 0)
            {
                Debug.LogWarning($"[Quest_SO] '{devName}': No task groups configured. Quest will complete immediately.", this);
            }

            // Validate start conditions
            if (startConditions != null)
            {
                for (int i = 0; i < startConditions.Count; i++)
                {
                    if (startConditions[i] == null)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': StartCondition at index {i} is null.", this);
                    }
                    else if (startConditions[i] is not IConditionEventDriven)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': StartCondition '{startConditions[i].name}' is not event-driven. Quest may not auto-start.", this);
                    }
                }
            }

            // Validate rewards
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].RewardType == null)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': Reward at index {i} has null RewardType.", this);
                    }
                    else if (rewards[i].Amount <= 0)
                    {
                        Debug.LogWarning($"[Quest_SO] '{devName}': Reward '{rewards[i].RewardType.name}' has invalid amount ({rewards[i].Amount}).", this);
                    }
                }
            }
        }

#if ODIN_INSPECTOR
        private bool ValidateRewards(List<RewardInstance> rewardList)
        {
            if (rewardList == null) return true;
            foreach (var reward in rewardList)
            {
                if (reward.RewardType == null) return false;
                if (reward.Amount <= 0) return false;
            }
            return true;
        }
#endif

        #endregion

        #region Editor Buttons

#if ODIN_INSPECTOR
        [ButtonGroup("Actions")]
        [Button("Generate New ID", ButtonSizes.Medium)]
        [PropertyOrder(100)]
#endif
        private void GenerateNewGuid()
        {
            questId = Guid.NewGuid().ToString();
        }

#if ODIN_INSPECTOR
        [ButtonGroup("Actions")]
        [Button("Validate Configuration", ButtonSizes.Medium)]
        [PropertyOrder(100)]
        private void ValidateButton()
        {
            ValidateConfiguration();
            ValidateTaskGroups();
            Debug.Log($"[Quest_SO] '{devName}': Validation complete. Check console for warnings.", this);
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

        #endregion
    }
}
