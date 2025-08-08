using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.Events
{
    [CreateAssetMenu(fileName = "FloatGameEvent", menuName = "HelloDev/Events/Float Game Event")]
    public class FloatGameEvent : GameEvent<float>
    {
#if UNITY_EDITOR
        [Button]
        void RaiseEvent(float parameter)
        {
            Raise(parameter);
        }
#endif
    }
}