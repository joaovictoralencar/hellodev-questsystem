using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.Events
{
    public abstract class GameEvent<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new List<Action<T>>();
        
        [SerializeField, TextArea] 
        private string _description = "Describe what this event represents";
        
#if UNITY_EDITOR
        [SerializeField] private bool _logRaises = false;
        private readonly Stack<string> _callStack = new Stack<string>();
#endif

        public void AddListener(Action<T> listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void RemoveListener(Action<T> listener)
        {
            if (listener != null)
            {
                _listeners.Remove(listener);
            }
        }

        public virtual void Raise(T parameter)
        {
#if UNITY_EDITOR
            if (_logRaises)
            {
                Debug.Log($"GameEvent '{name}' raised with parameter: {parameter} and {_listeners.Count} listeners", this);
                _callStack.Push(UnityEngine.StackTraceUtility.ExtractStackTrace());
            }
#endif
            
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _listeners[i]?.Invoke(parameter);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in GameEvent '{name}' listener: {e.Message}", this);
                }
            }
        }

        public void RemoveAllListeners()
        {
            _listeners.Clear();
        }
        
        public int ListenerCount => _listeners.Count;
        
#if UNITY_EDITOR
        public Stack<string> GetCallStack() => _callStack;
#endif
    }
}