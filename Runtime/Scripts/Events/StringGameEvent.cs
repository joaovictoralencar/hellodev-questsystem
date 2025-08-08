using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.Events
{
    [CreateAssetMenu(fileName = "StringGameEvent", menuName = "HelloDev/Events/String Game Event")]
    public class StringGameEvent : GameEvent<string>
    {
#if UNITY_EDITOR
        [Button]
        void RaiseEvent(string parameter)
        {
            Raise(parameter);
        }
#endif
    }
}