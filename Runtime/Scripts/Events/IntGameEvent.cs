using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.Events
{
    [CreateAssetMenu(fileName = "IntGameEvent", menuName = "HelloDev/Events/Int Game Event")]
    public class IntGameEvent : GameEvent<int>
    {
#if UNITY_EDITOR
        [Button]
        void RaiseEvent(int parameter)
        {
            Raise(parameter);
        }
#endif
    }
}