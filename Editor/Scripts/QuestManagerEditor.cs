// filepath: com.hellodev.questsystem/Editor/Scripts/QuestManagerEditor.cs

using HelloDev.QuestSystem;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestManager))]
public class QuestManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        QuestManager questManager = (QuestManager)target;

        DrawDefaultInspector();

        // if (GUILayout.Button("Add Quest"))
        // {
        //     questManager.AddQuest();
        // }
        //
        // if (GUILayout.Button("Remove Quest"))
        // {
        //     questManager.RemoveQuest();
        // }
        //
        // if (GUILayout.Button("Save Quests"))
        // {
        //     questManager.SaveQuests();
        // }
        //
        // if (GUILayout.Button("Load Quests"))
        // {
        //     questManager.LoadQuests();
        // }
    }
}