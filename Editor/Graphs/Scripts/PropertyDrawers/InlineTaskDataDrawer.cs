using UnityEngine;
using UnityEditor;
using HelloDev.QuestSystem.QuestGraph.Editor.Nodes;

namespace HelloDev.QuestSystem.QuestGraph.Editor.PropertyDrawers
{
    /// <summary>
    /// Custom PropertyDrawer for InlineTaskData.
    /// Shows all task fields organized by category.
    /// Type-specific fields are labeled to indicate which task types use them.
    /// </summary>
    [CustomPropertyDrawer(typeof(InlineTaskData))]
    public class InlineTaskDataDrawer : PropertyDrawer
    {
        private const float SPACING = 2f;
        private const float HEADER_SPACING = 6f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float fullWidth = position.width;

            // Draw foldout header
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, y, fullWidth, lineHeight),
                property.isExpanded,
                label,
                true
            );
            y += lineHeight + SPACING;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            // Draw common fields
            y = DrawHeader(position.x, y, fullWidth, "Common Fields (All Task Types)");
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("devName"), "Dev Name");
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("displayName"), "Display Name");
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("taskDescription"), "Description");
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("conditions"), "Conditions");
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("failureConditions"), "Failure Conditions");

            // Draw type-specific fields section
            y += HEADER_SPACING;
            y = DrawHeader(position.x, y, fullWidth, "Type-Specific Fields");

            // Int task field
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("requiredCount"), "Required Count (Int Task)");

            // String task field
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("targetValue"), "Target Value (String Task)");

            // Discovery task field
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("requiredDiscoveries"), "Required Discoveries (Discovery Task)");

            // Timed task fields
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("timeLimit"), "Time Limit (Timed Task)");
            y = DrawField(position.x, y, fullWidth, property.FindPropertyRelative("failQuestOnExpire"), "Fail Quest On Expire (Timed Task)");

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + SPACING; // Foldout

            // Common fields header
            height += EditorGUIUtility.singleLineHeight + SPACING;

            // Common fields
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("devName"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("displayName"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("taskDescription"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("conditions"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("failureConditions"));

            // Type-specific header
            height += HEADER_SPACING;
            height += EditorGUIUtility.singleLineHeight + SPACING;

            // Type-specific fields (all shown)
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("requiredCount"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("targetValue"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("requiredDiscoveries"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("timeLimit"));
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("failQuestOnExpire"));

            return height;
        }

        private float DrawHeader(float x, float y, float width, string text)
        {
            EditorGUI.LabelField(
                new Rect(x, y, width, EditorGUIUtility.singleLineHeight),
                text,
                EditorStyles.boldLabel
            );
            return y + EditorGUIUtility.singleLineHeight + SPACING;
        }

        private float DrawField(float x, float y, float width, SerializedProperty property, string label)
        {
            if (property == null)
                return y;

            float height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(
                new Rect(x, y, width, height),
                property,
                new GUIContent(label),
                true
            );
            return y + height + SPACING;
        }

        private float GetPropertyHeightWithSpacing(SerializedProperty property)
        {
            if (property == null)
                return 0;
            return EditorGUI.GetPropertyHeight(property, true) + SPACING;
        }
    }
}
