using System;
using System.Collections.Generic;
using System.Linq;
using HelloDev.Events;
using HelloDev.QuestSystem.Conditions.ScriptableObjects;
using HelloDev.QuestSystem.Utils;
using UnityEngine;

namespace HelloDev.QuestSystem.Conditions
{
    /// <summary>
    /// The base interface for all quest conditions.
    /// Any class that implements this interface can be used to define a quest's requirements.
    /// </summary>
    public interface ICondition
    {
        bool IsInverted { get; set; }

        /// <summary>
        /// Evaluates the condition. This version is for passive conditions.
        /// </summary>
        bool Evaluate();
    }

    /// <summary>
    ///  The base interface for all event-driven conditions.
    /// Any class that implements this interface can be used to define a quest's requirements.
    /// </summary>
    public interface IEventDrivenCondition : ICondition
    {
        void SubscribeToEvent(System.Action onConditionMet);
        void UnsubscribeFromEvent();
    }
    
    /// <summary>
    /// Enum for comparison types used in conditions
    /// </summary>
    public enum ComparisonType
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }
    /// <summary>
    /// An enum representing the logical operators for composite conditions.
    /// </summary>
    public enum CompositeOperator
    {
        And,
        Or
    }
}