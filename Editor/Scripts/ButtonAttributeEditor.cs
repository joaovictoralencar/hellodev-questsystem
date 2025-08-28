using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HelloDev.QuestSystem.Utils;
using UnityEditor;
using UnityEngine;

namespace HelloDev.QuestSystem.Editor
{
    /// <summary>
    /// Custom editor that finds methods marked with [ButtonAttribute] in any MonoBehaviour or ScriptableObject,
    /// displays them with foldout parameters and an Invoke button, handling null-safe drawing of various types.
    /// Parameter values are now persistent across inspector refreshes and object switches.
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true)]
    [CanEditMultipleObjects]
    public class ButtonAttributeEditor : UnityEditor.Editor
    {
        [System.Serializable]
        private class ParameterValue
        {
            public string typeName;
            public string serializedValue;

            public ParameterValue(Type type, object value)
            {
                typeName = type.AssemblyQualifiedName;
                serializedValue = SerializeValue(type, value);
            }

            public object GetValue()
            {
                var type = Type.GetType(typeName);
                return DeserializeValue(type, serializedValue);
            }

            private static string SerializeValue(Type type, object value)
            {
                if (value == null) return "null";

                try
                {
                    if (type == typeof(string)) return value.ToString();
                    if (type.IsPrimitive || type == typeof(decimal)) return value.ToString();
                    if (type.IsEnum) return value.ToString();
                    if (type == typeof(Vector2)) return JsonUtility.ToJson(value);
                    if (type == typeof(Vector3)) return JsonUtility.ToJson(value);
                    if (type == typeof(Color)) return JsonUtility.ToJson(value);
                    if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    {
                        var unityObj = value as UnityEngine.Object;
                        return unityObj != null ? unityObj.GetInstanceID().ToString() : "null";
                    }

                    return JsonUtility.ToJson(value);
                }
                catch
                {
                    return "null";
                }
            }

            private static object DeserializeValue(Type type, string serializedValue)
            {
                if (serializedValue == "null")
                    return type.IsValueType ? Activator.CreateInstance(type) : null;

                try
                {
                    if (type == typeof(string)) return serializedValue;
                    if (type == typeof(int)) return int.Parse(serializedValue);
                    if (type == typeof(float)) return float.Parse(serializedValue);
                    if (type == typeof(double)) return double.Parse(serializedValue);
                    if (type == typeof(bool)) return bool.Parse(serializedValue);
                    if (type.IsEnum) return Enum.Parse(type, serializedValue);
                    if (type == typeof(Vector2)) return JsonUtility.FromJson<Vector2>(serializedValue);
                    if (type == typeof(Vector3)) return JsonUtility.FromJson<Vector3>(serializedValue);
                    if (type == typeof(Color)) return JsonUtility.FromJson<Color>(serializedValue);
                    if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    {
                        if (int.TryParse(serializedValue, out int instanceId))
                            return EditorUtility.InstanceIDToObject(instanceId);
                        return null;
                    }

                    return JsonUtility.FromJson(serializedValue, type);
                }
                catch
                {
                    return type.IsValueType ? Activator.CreateInstance(type) : null;
                }
            }
        }

        [System.Serializable]
        private class MethodUIData
        {
            public string methodName;
            public string typeName;
            public List<ParameterValue> parameterValues;
            public bool isExpanded;
            public string displayName;
            public string description;

            [System.NonSerialized] public MethodInfo methodInfo;
        }

        // Static dictionary to persist data across different editor instances and object switches
        private static Dictionary<string, List<MethodUIData>> _persistentMethodData = new Dictionary<string, List<MethodUIData>>();

        private List<MethodUIData> _methods;
        private GUIStyle _boxStyle;
        private string _targetKey;
        private static readonly Color InvokeButtonColor = new Color(0.6f, 0.95f, 0.8f, 1f);

        private void OnEnable()
        {
            if (target == null) return;

            // Create a unique key using both type and instance ID for better persistence
            int id = target.GetInstanceID();
            _targetKey = $"{target.GetType().AssemblyQualifiedName}:{id}";

            // Try to get existing data first
            if (_persistentMethodData.TryGetValue(_targetKey, out _methods))
            {
                // Refresh method info references since they're not serialized
                RefreshMethodInfos();
            }
            else
            {
                // Gather all methods with [ButtonAttribute] for the first time
                GatherMethods();
                _persistentMethodData[_targetKey] = _methods;
            }
        }

        private void GatherMethods()
        {
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
                    methodInfo = mi,
                    methodName = mi.Name,
                    typeName = targetType.AssemblyQualifiedName,
                    description = attr.Description,
                    displayName = ToTitleCase(mi.Name),
                    isExpanded = false, // Start collapsed by default
                    parameterValues = parameters.Select(p =>
                    {
                        var defaultValue = p.HasDefaultValue ? p.DefaultValue : (p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null);
                        return new ParameterValue(p.ParameterType, defaultValue);
                    }).ToList()
                };

                _methods.Add(data);
            }
        }

        private void RefreshMethodInfos()
        {
            if (target == null) return;
            
            var targetType = target.GetType();
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var data in _methods)
            {
                data.methodInfo = methods.FirstOrDefault(m => m.Name == data.methodName &&
                                                              m.GetCustomAttribute<ButtonAttribute>() != null);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            if (_methods == null || !_methods.Any()) 
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Initialize styles within GUI context
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(0, 0, 6, 6)
                };
            }

            EditorGUILayout.Space(8);

            foreach (var method in _methods)
            {
                if (method.methodInfo != null) // Only draw if method info is available
                {
                    EditorGUILayout.BeginVertical(_boxStyle);
                    DrawMethodUI(method);
                    EditorGUILayout.EndVertical();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMethodUI(MethodUIData data)
        {
            var parameters = data.methodInfo.GetParameters();

            // Create a larger font style for method name
            var largeLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = EditorStyles.boldLabel.fontSize + 1
            };

            // Header with method name and invoke button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(data.displayName, largeLabelStyle);
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = InvokeButtonColor;
            if (GUILayout.Button("Invoke", GUILayout.Width(100), GUILayout.Height(22)))
            {
                InvokeMethod(data);
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();

            // Description if available
            if (!string.IsNullOrEmpty(data.description))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(data.description, MessageType.Info);
            }

            // Parameters section
            if (parameters.Length > 0)
            {
                EditorGUI.indentLevel++; // Indent the "Parameters" label
                data.isExpanded = EditorGUILayout.Foldout(data.isExpanded, "Parameters", true);
                EditorGUI.indentLevel--; // Reset for parameters

                if (data.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(1);

                    for (int i = 0; i < parameters.Length && i < data.parameterValues.Count; i++)
                    {
                        var newValue = DrawParameterField(parameters[i], data.parameterValues[i].GetValue());
                        data.parameterValues[i] = new ParameterValue(parameters[i].ParameterType, newValue);
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

            try
            {
                if (type == typeof(int))
                    result = EditorGUILayout.IntField(label, currentValue is int iv ? iv : 0);
                else if (type == typeof(float))
                    result = EditorGUILayout.FloatField(label, currentValue is float fv ? fv : 0f);
                else if (type == typeof(double))
                    result = EditorGUILayout.DoubleField(label, currentValue is double dv ? dv : 0d);
                else if (type == typeof(bool))
                    result = EditorGUILayout.Toggle(label, currentValue is bool bv && bv);
                else if (type == typeof(string))
                    result = EditorGUILayout.TextField(label, currentValue as string ?? string.Empty);
                else if (type.IsEnum)
                    result = EditorGUILayout.EnumPopup(label, currentValue as Enum ?? (Enum)Activator.CreateInstance(type));
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    result = EditorGUILayout.ObjectField(label, currentValue as UnityEngine.Object, type, true);
                else if (type == typeof(Vector2))
                    result = EditorGUILayout.Vector2Field(label, currentValue is Vector2 v2 ? v2 : Vector2.zero);
                else if (type == typeof(Vector3))
                    result = EditorGUILayout.Vector3Field(label, currentValue is Vector3 v3 ? v3 : Vector3.zero);
                else if (type == typeof(Color))
                    result = EditorGUILayout.ColorField(label, currentValue is Color c ? c : Color.white);
                else
                {
                    EditorGUILayout.LabelField(label, "Unsupported type");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error drawing '{label}': {ex.Message}");
            }

            return result;
        }

        private void InvokeMethod(MethodUIData data)
        {
            try
            {
                var parameterValues = data.parameterValues.Select(pv => pv.GetValue()).ToArray();
                data.methodInfo.Invoke(data.methodInfo.IsStatic ? null : target, parameterValues);
                Debug.Log($"Invoked method '{data.displayName}' on {target.name}.");
            }
            catch (TargetParameterCountException)
            {
                Debug.LogError($"Parameter count mismatch for '{data.displayName}'");
            }
            catch (TargetInvocationException tie)
            {
                Debug.LogError($"Error during method invocation '{data.displayName}': {tie.InnerException?.Message ?? tie.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error invoking '{data.displayName}': {ex.Message}");
            }
        }

        private static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Add spaces before capital letters
            var spaced = Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
            
            // Convert to title case
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spaced.ToLower());
        }
    }
}