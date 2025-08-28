using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.QuestSystem
{
    public abstract class RuntimeScriptableObject : ScriptableObject
    {
        [SerializeField, TextArea] private string _description = "Describe what this scriptable object represents";
        static readonly List<RuntimeScriptableObject> Instances = new();

        void OnEnable()
        {
            Instances.Add(this);
        }

        void OnDisable()
        {
            Instances.Remove(this);
        }

        protected abstract void Reset();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ResetInstances()
        {
            foreach (var instance in Instances)
            {
                instance.Reset();
            }
        }
    }
}