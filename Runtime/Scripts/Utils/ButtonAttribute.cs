using System;
using UnityEngine;

namespace HelloDev.QuestSystem.Utils
{
    /// <summary>
    /// An attribute that creates a button in the Unity Inspector to call the decorated method.
    /// It must be placed on a parameterless method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string Description { get; }

        public ButtonAttribute(string description = null)
        {
            Description = description;
        }
    }
}