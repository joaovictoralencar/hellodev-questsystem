using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.Events
{
    [CreateAssetMenu(fileName = "BoolGameEvent", menuName = "HelloDev/Events/Bool Game Event")]
    public class BoolGameEvent : GameEvent<bool>
    {
#if UNITY_EDITOR
        [Button]
        void RaiseEvent(bool parameter)
        {
            Raise(parameter);
        }
#endif
    }
}