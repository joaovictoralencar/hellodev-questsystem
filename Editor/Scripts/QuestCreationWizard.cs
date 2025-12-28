#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEditor;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.Events;
using HelloDev.Conditions;
using System.Collections.Generic;
using System.IO;
using System;
using HelloDev.Conditions.Types;

namespace HelloDev.QuestSystem.Editor
{
    /// <summary>
    /// A wizard window that guides designers through creating a new quest with all related assets.
    /// </summary>
    public class QuestCreationWizard : OdinEditorWindow
    {
        #region Menu

        [MenuItem("Tools/HelloDev/Quest System/Quest Creation Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<QuestCreationWizard>();
            window.titleContent = new GUIContent("Quest Wizard");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        #endregion

        #region Step 1: Basic Info

        [TitleGroup("Step 1: Basic Information")]
        [LabelText("Quest Name")]
        [Required("Quest name is required")]
        [PropertyOrder(0)]
        public string questName = "NewQuest";

        [TitleGroup("Step 1: Basic Information")]
        [LabelText("Quest Type")]
        [PropertyOrder(1)]
        [InlineButton("FindQuestTypes", "Refresh")]
        [ValueDropdown("GetAvailableQuestTypes")]
        public QuestType_SO questType;

        [TitleGroup("Step 1: Basic Information")]
        [LabelText("Recommended Level")]
        [PropertyOrder(2)]
        [MinValue(1)]
        public int recommendedLevel = 1;

        [TitleGroup("Step 1: Basic Information")]
        [LabelText("Output Folder")]
        [FolderPath(RequireExistingPath = true)]
        [PropertyOrder(3)]
        public string outputFolder = "Assets/Quests";

        #endregion

        #region Step 2: Template Selection

        [TitleGroup("Step 2: Quest Template")]
        [EnumToggleButtons]
        [PropertyOrder(10)]
        public QuestTemplate selectedTemplate = QuestTemplate.Custom;

        [TitleGroup("Step 2: Quest Template")]
        [ShowIf("@selectedTemplate != QuestTemplate.Custom")]
        [InfoBox("$GetTemplateDescription")]
        [PropertyOrder(11)]
        [ReadOnly]
        [HideLabel]
        public string templateInfo = "";

        #endregion

        #region Step 3: Template Configuration

        // Kill X Template
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.KillX")]
        [LabelText("Target Name")]
        [PropertyOrder(20)]
        public string killTargetName = "Goblin";

        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.KillX")]
        [LabelText("Kill Count")]
        [MinValue(1)]
        [PropertyOrder(21)]
        public int killCount = 5;

        // Collect Items Template
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.CollectItems")]
        [LabelText("Item Name")]
        [PropertyOrder(20)]
        public string collectItemName = "Gold Coin";

        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.CollectItems")]
        [LabelText("Collect Count")]
        [MinValue(1)]
        [PropertyOrder(21)]
        public int collectCount = 10;

        // Talk To NPC Template
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.TalkToNPC")]
        [LabelText("NPC Name")]
        [PropertyOrder(20)]
        public string npcName = "Village Elder";

        // Reach Location Template
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.ReachLocation")]
        [LabelText("Location Name")]
        [PropertyOrder(20)]
        public string locationName = "Ancient Ruins";

        // Discovery Template
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.Discovery")]
        [LabelText("Discovery Items")]
        [PropertyOrder(20)]
        public List<string> discoveryItems = new() { "Clue 1", "Clue 2", "Clue 3" };

        // Escort Template
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.Escort")]
        [LabelText("Escort Target")]
        [PropertyOrder(20)]
        public string escortTarget = "Merchant";

        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.Escort")]
        [LabelText("Destination")]
        [PropertyOrder(21)]
        public string escortDestination = "Town Gate";

        // Custom Template - Task count
        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.Custom")]
        [LabelText("Number of Tasks")]
        [MinValue(1)]
        [MaxValue(10)]
        [PropertyOrder(20)]
        public int customTaskCount = 3;

        [TitleGroup("Step 3: Template Configuration")]
        [ShowIf("@selectedTemplate == QuestTemplate.Custom")]
        [LabelText("Task Types")]
        [PropertyOrder(21)]
        public List<TaskTypeChoice> customTaskTypes = new() { TaskTypeChoice.Int, TaskTypeChoice.Bool, TaskTypeChoice.Location };

        #endregion

        #region Step 4: Options

        [TitleGroup("Step 4: Options")]
        [LabelText("Create Events")]
        [ToggleLeft]
        [PropertyOrder(30)]
        public bool createEvents = true;

        [TitleGroup("Step 4: Options")]
        [LabelText("Create Conditions")]
        [ToggleLeft]
        [PropertyOrder(31)]
        public bool createConditions = true;

        [TitleGroup("Step 4: Options")]
        [LabelText("Wire Events to Conditions")]
        [ToggleLeft]
        [ShowIf("@createEvents && createConditions")]
        [PropertyOrder(32)]
        public bool wireEventsToCnditions = true;

        [TitleGroup("Step 4: Options")]
        [LabelText("Add to QuestLine")]
        [PropertyOrder(33)]
        [ValueDropdown("GetAvailableQuestLines")]
        public QuestLine_SO targetQuestLine;

        #endregion

        #region Preview & Create

        [TitleGroup("Preview")]
        [PropertyOrder(40)]
        [DisplayAsString]
        [HideLabel]
        [OnInspectorGUI("DrawPreview")]
        public string previewPlaceholder;

        [TitleGroup("Create Quest")]
        [PropertyOrder(50)]
        [Button("Create Quest", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        public void CreateQuest()
        {
            if (string.IsNullOrEmpty(questName))
            {
                EditorUtility.DisplayDialog("Error", "Quest name is required.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid output folder.", "OK");
                return;
            }

            try
            {
                CreateQuestAssets();
                EditorUtility.DisplayDialog("Success", $"Quest '{questName}' created successfully!", "OK");
                ResetWizard();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create quest: {ex.Message}", "OK");
                Debug.LogError($"[QuestCreationWizard] {ex}");
            }
        }

        #endregion

        #region Enums

        public enum QuestTemplate
        {
            [LabelText("Custom")]
            Custom,
            [LabelText("Kill X Enemies")]
            KillX,
            [LabelText("Collect Items")]
            CollectItems,
            [LabelText("Talk to NPC")]
            TalkToNPC,
            [LabelText("Reach Location")]
            ReachLocation,
            [LabelText("Discovery")]
            Discovery,
            [LabelText("Escort")]
            Escort
        }

        public enum TaskTypeChoice
        {
            Int,
            Bool,
            Location,
            Discovery,
            Timed,
            String
        }

        #endregion

        #region Helper Methods

        private IEnumerable<QuestType_SO> GetAvailableQuestTypes()
        {
            var guids = AssetDatabase.FindAssets("t:QuestType_SO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.LoadAssetAtPath<QuestType_SO>(path);
                if (type != null) yield return type;
            }
        }

        private IEnumerable<QuestLine_SO> GetAvailableQuestLines()
        {
            yield return null; // None option
            var guids = AssetDatabase.FindAssets("t:QuestLine_SO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var line = AssetDatabase.LoadAssetAtPath<QuestLine_SO>(path);
                if (line != null) yield return line;
            }
        }

        private void FindQuestTypes()
        {
            // Force refresh
            AssetDatabase.Refresh();
        }

        private string GetTemplateDescription()
        {
            return selectedTemplate switch
            {
                QuestTemplate.KillX => "Creates an IntTask that tracks kills. Generates: Event (OnKill), Condition, Task.",
                QuestTemplate.CollectItems => "Creates an IntTask for item collection. Generates: Event (OnCollect), Condition, Task.",
                QuestTemplate.TalkToNPC => "Creates a BoolTask triggered by NPC dialogue. Generates: Event (OnTalk), Condition, Task.",
                QuestTemplate.ReachLocation => "Creates a LocationTask for reaching a destination. Generates: ID_SO for location, Task.",
                QuestTemplate.Discovery => "Creates a DiscoveryTask for finding items. Generates: ID_SOs for each item, Task.",
                QuestTemplate.Escort => "Creates tasks for escort quest: Talk → Escort → Arrive. Generates: Multiple tasks and events.",
                QuestTemplate.Custom => "Create a custom quest with the task types you choose.",
                _ => ""
            };
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(8);

            var bgColor = new Color(0.18f, 0.18f, 0.18f);
            var rect = EditorGUILayout.GetControlRect(false, 120);
            EditorGUI.DrawRect(rect, bgColor);

            var style = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8)
            };

            var previewText = GeneratePreviewText();
            GUI.Label(rect, previewText, style);
        }

        private string GeneratePreviewText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>Quest:</b> {questName}");
            sb.AppendLine($"<b>Folder:</b> {outputFolder}/{questName}/");
            sb.AppendLine();
            sb.AppendLine("<b>Will create:</b>");

            switch (selectedTemplate)
            {
                case QuestTemplate.KillX:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    sb.AppendLine($"  - Tasks/SO_Task_Kill{killTargetName}s.asset (Int, {killCount} required)");
                    if (createEvents) sb.AppendLine($"  - Events/SO_Event_On{killTargetName}Killed.asset");
                    if (createConditions) sb.AppendLine($"  - Conditions/SO_Condition_{killTargetName}sKilled.asset");
                    break;

                case QuestTemplate.CollectItems:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    sb.AppendLine($"  - Tasks/SO_Task_Collect{collectItemName}s.asset (Int, {collectCount} required)");
                    if (createEvents) sb.AppendLine($"  - Events/SO_Event_On{collectItemName}Collected.asset");
                    if (createConditions) sb.AppendLine($"  - Conditions/SO_Condition_{collectItemName}sCollected.asset");
                    break;

                case QuestTemplate.TalkToNPC:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    sb.AppendLine($"  - Tasks/SO_Task_TalkTo{npcName.Replace(" ", "")}.asset (Bool)");
                    if (createEvents) sb.AppendLine($"  - Events/SO_Event_On{npcName.Replace(" ", "")}Talked.asset");
                    if (createConditions) sb.AppendLine($"  - Conditions/SO_Condition_{npcName.Replace(" ", "")}Talked.asset");
                    break;

                case QuestTemplate.ReachLocation:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    sb.AppendLine($"  - Tasks/SO_Task_Reach{locationName.Replace(" ", "")}.asset (Location)");
                    sb.AppendLine($"  - IDs/SO_ID_{locationName.Replace(" ", "")}.asset");
                    break;

                case QuestTemplate.Discovery:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    sb.AppendLine($"  - Tasks/SO_Task_Discover.asset (Discovery, {discoveryItems.Count} items)");
                    foreach (var item in discoveryItems)
                    {
                        sb.AppendLine($"  - IDs/SO_ID_{item.Replace(" ", "")}.asset");
                    }
                    break;

                case QuestTemplate.Escort:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    sb.AppendLine($"  - Tasks/SO_Task_TalkTo{escortTarget.Replace(" ", "")}.asset (Bool)");
                    sb.AppendLine($"  - Tasks/SO_Task_Escort{escortTarget.Replace(" ", "")}.asset (Location)");
                    sb.AppendLine($"  - IDs/SO_ID_{escortDestination.Replace(" ", "")}.asset");
                    break;

                case QuestTemplate.Custom:
                    sb.AppendLine($"  - SO_Quest_{questName}.asset");
                    for (int i = 0; i < customTaskCount && i < customTaskTypes.Count; i++)
                    {
                        sb.AppendLine($"  - Tasks/SO_Task_{questName}_{i + 1}.asset ({customTaskTypes[i]})");
                    }
                    break;
            }

            if (targetQuestLine != null)
            {
                sb.AppendLine();
                sb.AppendLine($"<b>Add to:</b> {targetQuestLine.DevName}");
            }

            return sb.ToString();
        }

        private void ResetWizard()
        {
            questName = "NewQuest";
            questType = null;
            recommendedLevel = 1;
            selectedTemplate = QuestTemplate.Custom;
            killTargetName = "Goblin";
            killCount = 5;
            collectItemName = "Gold Coin";
            collectCount = 10;
            npcName = "Village Elder";
            locationName = "Ancient Ruins";
            discoveryItems = new List<string> { "Clue 1", "Clue 2", "Clue 3" };
            escortTarget = "Merchant";
            escortDestination = "Town Gate";
            customTaskCount = 3;
            customTaskTypes = new List<TaskTypeChoice> { TaskTypeChoice.Int, TaskTypeChoice.Bool, TaskTypeChoice.Location };
            targetQuestLine = null;
        }

        #endregion

        #region Quest Creation

        private void CreateQuestAssets()
        {
            // Create quest folder
            var questFolder = Path.Combine(outputFolder, questName);
            if (!AssetDatabase.IsValidFolder(questFolder))
            {
                AssetDatabase.CreateFolder(outputFolder, questName);
            }

            // Create subfolders
            CreateSubfolder(questFolder, "Tasks");
            if (createEvents) CreateSubfolder(questFolder, "Events");
            if (createConditions) CreateSubfolder(questFolder, "Conditions");

            // Create quest asset
            var quest = ScriptableObject.CreateInstance<Quest_SO>();
            SetPrivateField(quest, "devName", questName);
            SetPrivateField(quest, "questId", Guid.NewGuid().ToString());
            SetPrivateField(quest, "questType", questType);
            SetPrivateField(quest, "recommendedLevel", recommendedLevel);

            var taskGroups = new List<TaskGroups.TaskGroup>
            {
                new TaskGroups.TaskGroup { GroupName = "Main Tasks" }
            };
            SetPrivateField(quest, "taskGroups", taskGroups);
            SetPrivateField(quest, "usesTaskGroups", true);

            // Create tasks based on template
            var tasks = CreateTasksForTemplate(questFolder);
            taskGroups[0].Tasks.AddRange(tasks);

            // Save quest
            var questPath = Path.Combine(questFolder, $"SO_Quest_{questName}.asset");
            AssetDatabase.CreateAsset(quest, questPath);

            // Add to questline if specified
            if (targetQuestLine != null)
            {
                var questsField = typeof(QuestLine_SO).GetField("quests",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (questsField != null)
                {
                    var quests = questsField.GetValue(targetQuestLine) as List<Quest_SO>;
                    if (quests == null)
                    {
                        quests = new List<Quest_SO>();
                        questsField.SetValue(targetQuestLine, quests);
                    }
                    quests.Add(quest);
                    EditorUtility.SetDirty(targetQuestLine);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the created quest
            Selection.activeObject = quest;
            EditorGUIUtility.PingObject(quest);
        }

        private List<Task_SO> CreateTasksForTemplate(string questFolder)
        {
            var tasks = new List<Task_SO>();
            var tasksFolder = Path.Combine(questFolder, "Tasks");
            var eventsFolder = Path.Combine(questFolder, "Events");
            var conditionsFolder = Path.Combine(questFolder, "Conditions");

            switch (selectedTemplate)
            {
                case QuestTemplate.KillX:
                    tasks.Add(CreateIntTask(tasksFolder, $"Kill{killTargetName}s", killCount,
                        eventsFolder, conditionsFolder, $"On{killTargetName}Killed"));
                    break;

                case QuestTemplate.CollectItems:
                    tasks.Add(CreateIntTask(tasksFolder, $"Collect{collectItemName.Replace(" ", "")}s", collectCount,
                        eventsFolder, conditionsFolder, $"On{collectItemName.Replace(" ", "")}Collected"));
                    break;

                case QuestTemplate.TalkToNPC:
                    tasks.Add(CreateBoolTask(tasksFolder, $"TalkTo{npcName.Replace(" ", "")}",
                        eventsFolder, conditionsFolder, $"On{npcName.Replace(" ", "")}Talked"));
                    break;

                case QuestTemplate.ReachLocation:
                    var idFolder = Path.Combine(questFolder, "IDs");
                    CreateSubfolder(questFolder, "IDs");
                    tasks.Add(CreateLocationTask(tasksFolder, $"Reach{locationName.Replace(" ", "")}", idFolder, locationName));
                    break;

                case QuestTemplate.Discovery:
                    var idsFolder = Path.Combine(questFolder, "IDs");
                    CreateSubfolder(questFolder, "IDs");
                    tasks.Add(CreateDiscoveryTask(tasksFolder, "Discover", idsFolder, discoveryItems));
                    break;

                case QuestTemplate.Escort:
                    tasks.Add(CreateBoolTask(tasksFolder, $"TalkTo{escortTarget.Replace(" ", "")}",
                        eventsFolder, conditionsFolder, $"On{escortTarget.Replace(" ", "")}Talked"));
                    var escortIdFolder = Path.Combine(questFolder, "IDs");
                    CreateSubfolder(questFolder, "IDs");
                    tasks.Add(CreateLocationTask(tasksFolder, $"Escort{escortTarget.Replace(" ", "")}", escortIdFolder, escortDestination));
                    break;

                case QuestTemplate.Custom:
                    for (int i = 0; i < customTaskCount && i < customTaskTypes.Count; i++)
                    {
                        var taskType = customTaskTypes[i];
                        var taskName = $"{questName}_{i + 1}";
                        tasks.Add(CreateTaskOfType(tasksFolder, taskName, taskType, eventsFolder, conditionsFolder));
                    }
                    break;
            }

            return tasks;
        }

        private Task_SO CreateIntTask(string folder, string name, int required, string eventsFolder, string conditionsFolder, string eventName)
        {
            var task = ScriptableObject.CreateInstance<TaskInt_SO>();
            SetPrivateField(task, "devName", name);
            SetPrivateField(task, "taskId", Guid.NewGuid().ToString());
            SetPrivateField(task, "requiredCount", required);

            var taskPath = Path.Combine(folder, $"SO_Task_{name}.asset");
            AssetDatabase.CreateAsset(task, taskPath);

            if (createEvents && createConditions)
            {
                var condition = CreateEventAndCondition<GameEventInt_SO, ConditionInt_SO>(
                    eventsFolder, conditionsFolder, eventName);
                if (condition != null)
                {
                    var conditions = new List<Condition_SO> { condition };
                    SetPrivateField(task, "conditions", conditions);
                }
            }

            return task;
        }

        private Task_SO CreateBoolTask(string folder, string name, string eventsFolder, string conditionsFolder, string eventName)
        {
            var task = ScriptableObject.CreateInstance<TaskBool_SO>();
            SetPrivateField(task, "devName", name);
            SetPrivateField(task, "taskId", Guid.NewGuid().ToString());

            var taskPath = Path.Combine(folder, $"SO_Task_{name}.asset");
            AssetDatabase.CreateAsset(task, taskPath);

            if (createEvents && createConditions)
            {
                var condition = CreateEventAndCondition<GameEventBool_SO, ConditionBool_SO>(
                    eventsFolder, conditionsFolder, eventName);
                if (condition != null)
                {
                    var conditions = new List<Condition_SO> { condition };
                    SetPrivateField(task, "conditions", conditions);
                }
            }

            return task;
        }

        private Task_SO CreateLocationTask(string folder, string name, string idsFolder, string locName)
        {
            // Create ID for location
            var locationId = ScriptableObject.CreateInstance<HelloDev.IDs.ID_SO>();
            SetPrivateField(locationId, "devName", locName);
            var idPath = Path.Combine(idsFolder, $"SO_ID_{locName.Replace(" ", "")}.asset");
            AssetDatabase.CreateAsset(locationId, idPath);

            var task = ScriptableObject.CreateInstance<TaskLocation_SO>();
            SetPrivateField(task, "devName", name);
            SetPrivateField(task, "taskId", Guid.NewGuid().ToString());
            SetPrivateField(task, "targetLocation", locationId);

            var taskPath = Path.Combine(folder, $"SO_Task_{name}.asset");
            AssetDatabase.CreateAsset(task, taskPath);

            return task;
        }

        private Task_SO CreateDiscoveryTask(string folder, string name, string idsFolder, List<string> items)
        {
            var requiredItems = new List<HelloDev.IDs.ID_SO>();

            foreach (var item in items)
            {
                var itemId = ScriptableObject.CreateInstance<HelloDev.IDs.ID_SO>();
                SetPrivateField(itemId, "devName", item);
                var idPath = Path.Combine(idsFolder, $"SO_ID_{item.Replace(" ", "")}.asset");
                AssetDatabase.CreateAsset(itemId, idPath);
                requiredItems.Add(itemId);
            }

            var task = ScriptableObject.CreateInstance<TaskDiscovery_SO>();
            SetPrivateField(task, "devName", name);
            SetPrivateField(task, "taskId", Guid.NewGuid().ToString());
            SetPrivateField(task, "requiredDiscoveries", requiredItems);

            var taskPath = Path.Combine(folder, $"SO_Task_{name}.asset");
            AssetDatabase.CreateAsset(task, taskPath);

            return task;
        }

        private Task_SO CreateTaskOfType(string folder, string name, TaskTypeChoice type, string eventsFolder, string conditionsFolder)
        {
            return type switch
            {
                TaskTypeChoice.Int => CreateIntTask(folder, name, 5, eventsFolder, conditionsFolder, $"On{name}"),
                TaskTypeChoice.Bool => CreateBoolTask(folder, name, eventsFolder, conditionsFolder, $"On{name}"),
                TaskTypeChoice.Location => CreateLocationTask(folder, name, folder.Replace("Tasks", "IDs"), name),
                TaskTypeChoice.Discovery => CreateDiscoveryTask(folder, name, folder.Replace("Tasks", "IDs"), new List<string> { $"{name}_Item1", $"{name}_Item2" }),
                TaskTypeChoice.Timed => CreateTimedTask(folder, name),
                TaskTypeChoice.String => CreateStringTask(folder, name),
                _ => null
            };
        }

        private Task_SO CreateTimedTask(string folder, string name)
        {
            var task = ScriptableObject.CreateInstance<TaskTimed_SO>();
            SetPrivateField(task, "devName", name);
            SetPrivateField(task, "taskId", Guid.NewGuid().ToString());
            SetPrivateField(task, "duration", 60f);

            var taskPath = Path.Combine(folder, $"SO_Task_{name}.asset");
            AssetDatabase.CreateAsset(task, taskPath);

            return task;
        }

        private Task_SO CreateStringTask(string folder, string name)
        {
            var task = ScriptableObject.CreateInstance<TaskString_SO>();
            SetPrivateField(task, "devName", name);
            SetPrivateField(task, "taskId", Guid.NewGuid().ToString());

            var taskPath = Path.Combine(folder, $"SO_Task_{name}.asset");
            AssetDatabase.CreateAsset(task, taskPath);

            return task;
        }

        private TCondition CreateEventAndCondition<TEvent, TCondition>(string eventsFolder, string conditionsFolder, string eventName)
            where TEvent : ScriptableObject
            where TCondition : Condition_SO
        {
            // Create event
            var gameEvent = ScriptableObject.CreateInstance<TEvent>();
            var eventPath = Path.Combine(eventsFolder, $"SO_Event_{eventName}.asset");
            AssetDatabase.CreateAsset(gameEvent, eventPath);

            // Create condition
            var condition = ScriptableObject.CreateInstance<TCondition>();
            var conditionPath = Path.Combine(conditionsFolder, $"SO_Condition_{eventName}.asset");

            // Wire event to condition
            var eventField = typeof(TCondition).GetField("gameEvent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eventField?.SetValue(condition, gameEvent);

            AssetDatabase.CreateAsset(condition, conditionPath);

            return condition;
        }

        private void CreateSubfolder(string parent, string name)
        {
            var path = Path.Combine(parent, name);
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                // Try base type
                field = obj.GetType().BaseType?.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            field?.SetValue(obj, value);
        }

        #endregion
    }
}
#endif
