using System;
using UnityEngine;
using UnityEngine.Events;

namespace HelloDev.Events
{
    public class FloatGameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent<float> _gameEvent;
        [SerializeField] private UnityEvent<float> _response;

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

        private void OnEventRaised(float parameter)
        {
            _response?.Invoke(parameter);
        }
    }
}