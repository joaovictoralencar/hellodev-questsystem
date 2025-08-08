using UnityEngine;
using UnityEngine.Events;

namespace HelloDev.Events
{
    [System.Serializable]
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _gameEvent;
        [SerializeField] private UnityEvent _response;

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

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}