using System;
using HelloDev.Events;
using UnityEngine;
using UnityEngine.Events;

namespace HelloDev.QuestSystem.Conditions.ScriptableObjects
{
    public abstract class Condition_SO : ScriptableObject, ICondition
    {
        [SerializeField] private bool _isInverted;

        public bool IsInverted
        {
            get => _isInverted;
            set => _isInverted = value;
        }

        public abstract bool Evaluate();
    }

    // Generic base class for event-driven conditions
    public abstract class EventDrivenCondition_SO<T> : Condition_SO, IEventDrivenCondition
    {
        [SerializeField] protected T _targetValue;
        [SerializeField] protected ComparisonType _comparisonType = ComparisonType.Equals;
        
        private System.Action _onConditionMet;
        private bool _isSubscribed = false;

        public T TargetValue
        {
            get => _targetValue;
            set => _targetValue = value;
        }

        public ComparisonType ComparisonType
        {
            get => _comparisonType;
            set => _comparisonType = value;
        }

        public override bool Evaluate()
        {
            return false;
        }

        // This is the non-generic method that can be called polymorphically
        public virtual void SubscribeToEvent(System.Action onConditionMet)
        {
            if (_isSubscribed) return;
            
            _onConditionMet = onConditionMet;
            SubscribeToSpecificEvent();
            _isSubscribed = true;
        }

        public virtual void UnsubscribeFromEvent()
        {
            if (!_isSubscribed) return;
            
            UnsubscribeFromSpecificEvent();
            _isSubscribed = false;
            _onConditionMet = null;
        }

        protected abstract void SubscribeToSpecificEvent();
        protected abstract void UnsubscribeFromSpecificEvent();

        protected void OnEventTriggered(T eventParameter)
        {
            if (EvaluateCondition(eventParameter))
            {
                _onConditionMet?.Invoke();
            }
        }

        protected virtual bool EvaluateCondition(T eventParameter)
        {
            bool result = CompareValues(eventParameter, _targetValue, _comparisonType);
            return IsInverted ? !result : result;
        }

        protected abstract bool CompareValues(T eventValue, T targetValue, ComparisonType comparisonType);

        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvent();
        }
    }
}