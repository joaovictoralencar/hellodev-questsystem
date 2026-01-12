using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using HelloDev.QuestSystem;

namespace HelloDev.QuestSystem.QuestGraph.Editor
{
    /// <summary>
    /// CustomPropertyDrawer for ConditionList wrapper.
    /// Graph Toolkit's CustomPropertyDrawerAdapter discovers this via
    /// ScriptAttributeUtility.GetDrawerTypeForType().
    /// </summary>
    /// <remarks>
    /// This drawer enables List&lt;Condition_SO&gt; fields to appear in Graph Toolkit
    /// node inspectors by wrapping them in the ConditionList type.
    /// </remarks>
    [CustomPropertyDrawer(typeof(ConditionList))]
    public class ConditionListDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            // Get the _conditions field inside ConditionList
            var conditionsProp = property.FindPropertyRelative("_conditions");

            if (conditionsProp != null)
            {
                // Use preferredLabel if available, otherwise use property's nice name
                // Graph Toolkit's CustomPropertyDrawerAdapter sets the label via PropertyField
                var labelText = !string.IsNullOrEmpty(preferredLabel)
                    ? preferredLabel
                    : property.displayName;

                var listField = new PropertyField(conditionsProp, labelText);
                listField.BindProperty(conditionsProp);
                container.Add(listField);
            }
            else
            {
                container.Add(new Label($"Could not find _conditions in {property.propertyPath}"));
            }

            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Fallback for IMGUI rendering
            var conditionsProp = property.FindPropertyRelative("_conditions");

            if (conditionsProp != null)
            {
                EditorGUI.PropertyField(position, conditionsProp, label, true);
            }
            else
            {
                EditorGUI.LabelField(position, label, new GUIContent("Could not find _conditions"));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var conditionsProp = property.FindPropertyRelative("_conditions");

            if (conditionsProp != null)
            {
                return EditorGUI.GetPropertyHeight(conditionsProp, label, true);
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}
