using System;
using System.Collections.Generic;
using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.Events
{
    [CreateAssetMenu(fileName = "GameEvent", menuName = "HelloDev/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new List<Action>();
        
        [SerializeField, TextArea] 
        private string _description = "Describe what this event represents";
        
#if UNITY_EDITOR
        [SerializeField] private bool _logRaises = false;
        private readonly Stack<string> _callStack = new Stack<string>();
        [Button]
        void RaiseEvent()
        {
            Raise();
        }
#endif

        public void AddListener(Action listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void RemoveListener(Action listener)
        {
            if (listener != null)
            {
                _listeners.Remove(listener);
            }
        }

        public virtual void Raise()
        {
#if UNITY_EDITOR
            if (_logRaises)
            {
                Debug.Log($"GameEvent '{name}' raised with {_listeners.Count} listeners", this);
                _callStack.Push(UnityEngine.StackTraceUtility.ExtractStackTrace());
            }
#endif
            
            // Iterate backwards to handle listeners that might remove themselves during the event
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _listeners[i]?.Invoke();
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
    }
}