using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using HelloDev.QuestSystem.Utils;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.Editor
{
    /// <summary>
    /// Custom editor that finds methods marked with [ButtonAttribute] in any MonoBehaviour or ScriptableObject,
    /// displays them with foldout parameters and an Invoke button, handling null-safe drawing of various types.
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true)]
    [CanEditMultipleObjects]
    public class ButtonAttributeEditor : UnityEditor.Editor
    {
        private class MethodUIData
        {
            public MethodInfo MethodInfo;
            public object[] ParameterValues;
            public bool IsExpanded;
            public string DisplayName;
            public string Description;
        }

        private List<MethodUIData> _methods;
        private GUIStyle _boxStyle;
        private static readonly Color ButtonColor = new Color(0.7f, 0.85f, 1f, 1f);

        private void OnEnable()
        {
            // Gather all methods with [ButtonAttribute]
            _methods = new List<MethodUIData>();
            var targetType = target.GetType();
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var mi in methods)
            {
                var attr = mi.GetCustomAttribute<ButtonAttribute>();
                if (attr == null) continue;

                var parameters = mi.GetParameters();
                var data = new MethodUIData
                {
                    MethodInfo = mi,
                    Description = attr.Description,
                    DisplayName = ToTitleCase(mi.Name),
                    IsExpanded = parameters.Length > 0,
                    ParameterValues = parameters.Select(p => p.HasDefaultValue ? p.DefaultValue : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)).ToArray()
                };
                _methods.Add(data);
            }
        }

        public override void OnInspectorGUI()
        {
            // Initialize styles within GUI context
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 4, 4),
                    margin = new RectOffset(0, 0, 4, 4)
                };
            }

            DrawDefaultInspector();

            if (_methods == null || !_methods.Any()) return;

            EditorGUILayout.Space(4);
            // Header highlight
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Inspector Buttons");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            foreach (var method in _methods)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                DrawMethodUI(method);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawMethodUI(MethodUIData data)
        {
            var parameters = data.MethodInfo.GetParameters();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(data.DisplayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = ButtonColor;
            if (GUILayout.Button("Invoke", GUILayout.Width(70), GUILayout.Height(18)))
                InvokeMethod(data);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(data.Description))
            {
                EditorGUILayout.HelpBox(data.Description, MessageType.Info);
            }

            if (parameters.Length > 0)
            {
                data.IsExpanded = EditorGUILayout.Foldout(data.IsExpanded, "Parameters", true);
                if (data.IsExpanded)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        data.ParameterValues[i] = DrawParameterField(parameters[i], data.ParameterValues[i]);
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private object DrawParameterField(ParameterInfo param, object currentValue)
        {
            var type = param.ParameterType;
            var label = ToTitleCase(param.Name);
            object result = currentValue;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4));

            try
            {
                if (type == typeof(int))
                    result = EditorGUILayout.IntField(currentValue is int iv ? iv : 0);
                else if (type == typeof(float))
                    result = EditorGUILayout.FloatField(currentValue is float fv ? fv : 0f);
                else if (type == typeof(double))
                    result = EditorGUILayout.DoubleField(currentValue is double dv ? dv : 0d);
                else if (type == typeof(bool))
                    result = EditorGUILayout.Toggle(currentValue is bool bv && bv);
                else if (type == typeof(string))
                    result = EditorGUILayout.TextField(currentValue as string ?? string.Empty);
                else if (type.IsEnum)
                    result = EditorGUILayout.EnumPopup(currentValue as Enum ?? (Enum)Activator.CreateInstance(type));
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    result = EditorGUILayout.ObjectField(currentValue as UnityEngine.Object, type, true);
                else if (type == typeof(Vector2))
                    result = EditorGUILayout.Vector2Field(GUIContent.none, currentValue is Vector2 v2 ? v2 : Vector2.zero);
                else if (type == typeof(Vector3))
                    result = EditorGUILayout.Vector3Field(GUIContent.none, currentValue is Vector3 v3 ? v3 : Vector3.zero);
                else if (type == typeof(Color))
                    result = EditorGUILayout.ColorField(currentValue is Color c ? c : Color.white);
                else
                {
                    EditorGUILayout.LabelField("Unsupported type");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error drawing '{label}': {ex.Message}");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(1);
            return result;
        }

        private void InvokeMethod(MethodUIData data)
        {
            try
            {
                data.MethodInfo.Invoke(data.MethodInfo.IsStatic ? null : target, data.ParameterValues);
            }
            catch (TargetParameterCountException)
            {
                Debug.LogError($"Parameter count mismatch for '{data.DisplayName}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking '{data.DisplayName}': {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static string ToTitleCase(string text)
        {
            var spaced = Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced);
        }
    }
}
