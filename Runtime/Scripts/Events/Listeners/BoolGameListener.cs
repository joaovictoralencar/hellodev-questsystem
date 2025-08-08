using System;
using UnityEngine;
using UnityEngine.Events;

namespace HelloDev.Events
{
    public class BoolGameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent<bool> _gameEvent;
        [SerializeField] private UnityEvent<bool> _response;

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

        private void OnEventRaised(bool parameter)
        {
            _response?.Invoke(parameter);
        }
    }
}