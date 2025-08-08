using System;
using UnityEngine;
using UnityEngine.Events;

namespace HelloDev.Events
{
    public class StringGameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent<string> _gameEvent;
        [SerializeField] private UnityEvent<string> _response;

        private void OnEnable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.AddListener(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_gameEvent != null)
            {
                _gameEvent.RemoveListener(OnEventRaised);
            }
        }

        private void OnEventRaised(string parameter)
        {
            _response?.Invoke(parameter);
        }
    }
}