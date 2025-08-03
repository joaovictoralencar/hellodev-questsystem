// filepath: com.hellodev.questsystem/Editor/Scripts/QuestEditor.cs

using HelloDev.QuestSystem;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Quest))]
public class QuestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Quest quest = (Quest)target;

        // Draw default inspector
        DrawDefaultInspector();

        // Custom GUI elements for quest management
        if (GUILayout.Button("Add Objective"))
        {
            // Logic to add a new objective to the quest
        }

        if (GUILayout.Button("Remove Objective"))
        {
            // Logic to remove an objective from the quest
        }

        // Additional custom editor functionality can be added here
    }
}