using System;
using UnityEngine;
using UnityEngine.Events;

namespace HelloDev.Events
{
    public class IntGameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent<int> _gameEvent;
        [SerializeField] private UnityEvent<int> _response;

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

        private void OnEventRaised(int parameter)
        {
            _response?.Invoke(parameter);
        }
    }
}