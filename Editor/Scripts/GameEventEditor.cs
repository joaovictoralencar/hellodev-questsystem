using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HelloDev.Events.Editor
{
    [CustomEditor(typeof(GameEvent<>), true, isFallback = false)]
    [CanEditMultipleObjects]
    public class GameEventEditor : UnityEditor.Editor
    {
        private bool _showListeners = true;

        // Persist payload values per-instance (key uses InstanceID)
        private static Dictionary<string, PayloadValue> _persistentPayloadValues = new Dictionary<string, PayloadValue>();

        private string _instanceKey;
        private Type _payloadType;
        private PayloadValue _payloadValue;
        private GUIStyle _boxStyle;
        private static readonly Color RaiseButtonColor = new Color(0.6f, 0.95f, 0.8f, 1f);

        private void OnEnable()
        {
            if (target == null) return;

            int id = target.GetInstanceID();
            _instanceKey = $"{target.GetType().AssemblyQualifiedName}:{id}";
            _payloadType = ResolvePayloadType(target.GetType());

            if (_payloadType != null)
            {
                if (!_persistentPayloadValues.TryGetValue(_instanceKey, out _payloadValue))
                {
                    var defaultObj = CreateDefaultForType(_payloadType);
                    _payloadValue = new PayloadValue(_payloadType, defaultObj);
                    _persistentPayloadValues[_instanceKey] = _payloadValue;
                }
            }
            else
            {
                // Ensure entry exists (nullable)
                if (!_persistentPayloadValues.TryGetValue(_instanceKey, out _payloadValue))
                {
                    _payloadValue = null;
                    _persistentPayloadValues[_instanceKey] = null;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            // Raise panel
            EditorGUILayout.Space(8);
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(0, 0, 6, 6)
                };
            }

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Raise Event", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_payloadType == null)
            {
                EditorGUILayout.HelpBox("This generic event has no payload type (unexpected). Press Raise to invoke parameterless Raise if available.", MessageType.Info);
                EditorGUILayout.Space(4);
                GUI.backgroundColor = RaiseButtonColor;
                if (GUILayout.Button("Raise", GUILayout.Height(22)))
                {
                    TryRaiseEvent(null);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                DrawPayloadEditor();
                EditorGUILayout.Space(4);
                GUI.backgroundColor = RaiseButtonColor;
                if (GUILayout.Button($"Raise", GUILayout.Height(22)))
                {
                    var valueToSend = _payloadValue?.GetValue();
                    TryRaiseEvent(valueToSend);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();

            // Listeners section below
            EditorGUILayout.Space(6);
            DrawListenersSection();

            serializedObject.ApplyModifiedProperties();
        }

        #region Payload UI

        private void DrawPayloadEditor()
        {
            if (_payloadValue == null)
            {
                _payloadValue = new PayloadValue(_payloadType, CreateDefaultForType(_payloadType));
                _persistentPayloadValues[_instanceKey] = _payloadValue;
            }

            var t = _payloadType;
            try
            {
                EditorGUI.BeginChangeCheck();
                object current = _payloadValue.GetValue();
                object newVal = current;

                if (t == typeof(int))
                    newVal = EditorGUILayout.IntField("Value", current is int iv ? iv : 0);
                else if (t == typeof(float))
                    newVal = EditorGUILayout.FloatField("Value", current is float fv ? fv : 0f);
                else if (t == typeof(double))
                {
                    double dv = current is double dd ? dd : 0d;
                    dv = EditorGUILayout.DoubleField("Value", dv);
                    newVal = dv;
                }
                else if (t == typeof(bool))
                    newVal = EditorGUILayout.Toggle("Value", current is bool bv && bv);
                else if (t == typeof(string))
                    newVal = EditorGUILayout.TextField("Value", current as string ?? string.Empty);
                else if (t.IsEnum)
                    newVal = EditorGUILayout.EnumPopup("Value", current as Enum ?? (Enum)Activator.CreateInstance(t));
                else if (t == typeof(Vector2))
                    newVal = EditorGUILayout.Vector2Field("Value", current is Vector2 v2 ? v2 : Vector2.zero);
                else if (t == typeof(Vector3))
                    newVal = EditorGUILayout.Vector3Field("Value", current is Vector3 v3 ? v3 : Vector3.zero);
                else if (t == typeof(Color))
                    newVal = EditorGUILayout.ColorField("Value", current is Color c ? c : Color.white);
                else if (typeof(UnityEngine.Object).IsAssignableFrom(t))
                    newVal = EditorGUILayout.ObjectField("Value", current as UnityEngine.Object, t, true);
                else
                {
                    // Complex type -> JSON editor
                    EditorGUILayout.LabelField("Payload JSON", EditorStyles.label);
                    string json = _payloadValue.serializedValue ?? "null";
                    json = EditorGUILayout.TextArea(json, GUILayout.MinHeight(80));
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Apply JSON"))
                    {
                        var des = _payloadValue.DeserializeFromJson(json);
                        if (des == null)
                        {
                            Debug.LogWarning($"Failed to parse JSON into {_payloadType.Name}. Keeping previous value.");
                        }
                        else
                        {
                            _payloadValue.serializedValue = json;
                            // update cached object
                            _payloadValue = new PayloadValue(_payloadType, des);
                            _persistentPayloadValues[_instanceKey] = _payloadValue;
                        }
                    }
                    if (GUILayout.Button("Reset"))
                    {
                        var def = CreateDefaultForType(t);
                        _payloadValue = new PayloadValue(t, def);
                        _persistentPayloadValues[_instanceKey] = _payloadValue;
                    }
                    EditorGUILayout.EndHorizontal();

                    // preview
                    try
                    {
                        var previewObj = _payloadValue.GetValue();
                        if (previewObj != null)
                        {
                            EditorGUILayout.LabelField("Preview (ToString):", EditorStyles.miniLabel);
                            EditorGUILayout.SelectableLabel(previewObj.ToString(), GUILayout.Height(20));
                        }
                    }
                    catch { }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (newVal != null && (t.IsPrimitive || t == typeof(string) || t.IsEnum ||
                                          t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Color) ||
                                          typeof(UnityEngine.Object).IsAssignableFrom(t)))
                    {
                        _payloadValue = new PayloadValue(t, newVal);
                        _persistentPayloadValues[_instanceKey] = _payloadValue;
                    }
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"Error rendering payload editor: {ex.Message}", MessageType.Warning);
            }
        }

        private object CreateDefaultForType(Type t)
        {
            if (t == null) return null;
            if (t == typeof(string)) return string.Empty;
            if (t.IsValueType) return Activator.CreateInstance(t);
            return null;
        }

        #endregion

        #region Raise logic (invokes per selected target)

        private void TryRaiseEvent(object payload)
        {
            // If multiple objects are selected, call raise on each one.
            foreach (var obj in targets)
            {
                try
                {
                    var targetType = obj.GetType();

                    MethodInfo raiseMethod = null;
                    object[] args = null;

                    // Prefer method with single parameter matching payload type
                    if (_payloadType != null)
                    {
                        var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                            .Where(m => string.Equals(m.Name, "Raise", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 1);

                        foreach (var m in methods)
                        {
                            var p = m.GetParameters()[0].ParameterType;
                            if (payload == null)
                            {
                                // if payload is null, accept any reference-type parameter
                                if (!p.IsValueType)
                                {
                                    raiseMethod = m;
                                    break;
                                }
                            }
                            else
                            {
                                var payloadType = payload.GetType();
                                if (p.IsAssignableFrom(payloadType) || payloadType.IsAssignableFrom(p))
                                {
                                    raiseMethod = m;
                                    break;
                                }
                            }
                        }

                        if (raiseMethod == null)
                        {
                            // fallback: first Raise with one parameter
                            raiseMethod = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                    .FirstOrDefault(m => string.Equals(m.Name, "Raise", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 1);
                        }

                        args = new object[] { payload };
                    }
                    else
                    {
                        // parameterless Raise()
                        raiseMethod = targetType.GetMethod("Raise", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                        args = new object[0];
                    }

                    if (raiseMethod == null)
                    {
                        Debug.LogError($"No suitable Raise method found on {obj.GetType().Name} for payload type {_payloadType?.Name ?? "none"}.");
                        continue;
                    }

                    raiseMethod.Invoke(obj, args);
                    Debug.Log($"Raised event '{(obj as UnityEngine.Object)?.name ?? obj.GetType().Name}' via {raiseMethod.Name}.");
                }
                catch (TargetInvocationException tie)
                {
                    Debug.LogError($"Error during Raise invocation on {((obj as UnityEngine.Object) != null ? ((UnityEngine.Object)obj).name : obj.GetType().Name)}: {tie.InnerException?.Message ?? tie.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error calling Raise on event: {ex.Message}");
                }
            }
        }

        #endregion

        #region Listeners (kept / slightly polished)

        private void DrawListenersSection()
        {
            var listenerCount = GetListenerCount();
            var listenersLabel = $"Listeners ({listenerCount})";

            _showListeners = EditorGUILayout.BeginFoldoutHeaderGroup(_showListeners, listenersLabel);

            if (_showListeners)
            {
                EditorGUI.indentLevel++;

                if (listenerCount == 0)
                {
                    EditorGUILayout.HelpBox("No listeners registered", MessageType.Info);
                }
                else
                {
                    DrawListenersList();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawListenersList()
        {
            var listeners = GetListeners();
            if (listeners == null)
            {
                EditorGUILayout.HelpBox("Could not access listeners list", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < listeners.Count; i++)
            {
                var listener = listeners[i];
                if (listener == null)
                {
                    EditorGUILayout.LabelField($"{i + 1}. <null listener>");
                    continue;
                }

                EditorGUILayout.BeginHorizontal();

                var delegateListener = listener as Delegate;
                if (delegateListener == null)
                {
                    EditorGUILayout.LabelField($"{i + 1}. <invalid delegate> {listener.GetType().Name}");
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                var methodInfo = delegateListener.Method;
                var targetObject = delegateListener.Target;

                string listenerName = GetListenerDisplayName(methodInfo, targetObject);
                string targetName = GetTargetDisplayName(targetObject);

                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));

                if (targetObject is Component component)
                {
                    if (GUILayout.Button($"{targetName} → {listenerName}", EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = component.gameObject;
                        EditorGUIUtility.PingObject(component.gameObject);
                    }
                }
                else if (targetObject is ScriptableObject so)
                {
                    if (GUILayout.Button($"{targetName} → {listenerName}", EditorStyles.linkLabel))
                    {
                        Selection.activeObject = so;
                        EditorGUIUtility.PingObject(so);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"{targetName} → {listenerName}");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private int GetListenerCount()
        {
            var property = target.GetType().GetProperty("ListenerCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                try { return (int)property.GetValue(target); }
                catch { /* ignore */ }
            }
            return 0;
        }

        private System.Collections.IList GetListeners()
        {
            var targetType = target.GetType();
            string[] possibleFieldNames = { "_listeners", "listeners", "m_listeners" };

            foreach (var fieldName in possibleFieldNames)
            {
                var field = targetType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    var value = field.GetValue(target);
                    if (value is System.Collections.IList list)
                        return list;
                }
            }

            var baseType = targetType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                foreach (var fieldName in possibleFieldNames)
                {
                    var field = baseType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    if (field != null)
                    {
                        var value = field.GetValue(target);
                        if (value is System.Collections.IList list)
                            return list;
                    }
                }
                baseType = baseType.BaseType;
            }

            return null;
        }

        private Stack<string> GetCallStack()
        {
            var method = target.GetType().GetMethod("GetCallStack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                return method.Invoke(target, null) as Stack<string>;
            }
            return null;
        }

        private Type[] GetGenericArguments(Type type)
        {
            while (type != null && !type.IsGenericType)
            {
                type = type.BaseType;
            }

            return type?.GetGenericArguments();
        }

        private string GetListenerDisplayName(MethodInfo methodInfo, object target)
        {
            if (methodInfo.IsStatic)
                return $"{methodInfo.DeclaringType?.Name}.{methodInfo.Name}";

            if (methodInfo.Name.Contains("<") || methodInfo.Name.Contains("lambda") || methodInfo.Name.Contains("Anonymous"))
                return "Lambda/Anonymous Method";

            if (methodInfo.Name.StartsWith("m_"))
                return "Unity Event Method";

            return methodInfo.Name;
        }

        private string GetTargetDisplayName(object target)
        {
            if (target is Component component)
                return $"{component.gameObject.name} ({component.GetType().Name})";
            else if (target is ScriptableObject so)
                return $"{so.name} ({so.GetType().Name})";
            else if (target != null)
                return target.GetType().Name;

            return "Unknown";
        }

        #endregion

        #region Helpers

        private Type ResolvePayloadType(Type eventType)
        {
            var t = eventType;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("GameEvent"))
                {
                    var args = t.GetGenericArguments();
                    if (args != null && args.Length > 0) return args[0];
                }
                t = t.BaseType;
            }
            return null;
        }

        #endregion

        #region PayloadValue (serialize/persist)

        [Serializable]
        private class PayloadValue
        {
            public string typeName;
            public string serializedValue;

            [NonSerialized] private object _cachedObject;

            public PayloadValue(Type type, object value)
            {
                typeName = type.AssemblyQualifiedName;
                serializedValue = SerializeValue(type, value);
                _cachedObject = value;
            }

            public object GetValue()
            {
                if (_cachedObject != null) return _cachedObject;
                var type = Type.GetType(typeName);
                _cachedObject = DeserializeValue(type, serializedValue);
                return _cachedObject;
            }

            public object DeserializeFromJson(string json)
            {
                var type = Type.GetType(typeName);
                try
                {
                    if (string.IsNullOrEmpty(json) || json == "null")
                        return type.IsValueType ? Activator.CreateInstance(type) : null;
                    return JsonUtility.FromJson(json, type);
                }
                catch
                {
                    return null;
                }
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

        #endregion
    }
}
