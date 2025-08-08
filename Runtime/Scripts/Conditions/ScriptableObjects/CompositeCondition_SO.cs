using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HelloDev.QuestSystem.Conditions.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CompositeCondition", menuName = "HelloDev/Conditions/Composite Condition")]
    public class CompositeCondition_SO : Condition_SO
    {
        [SerializeField] private List<Condition_SO> _conditions = new List<Condition_SO>();
        [SerializeField] private CompositeOperator _operator = CompositeOperator.And;

        private readonly Dictionary<Condition_SO, bool> _conditionStates = new Dictionary<Condition_SO, bool>();
        private Action _onConditionMet;

        public void Initialize(Action onConditionMet)
        {
            _onConditionMet = onConditionMet;
            
            foreach (var condition in _conditions)
            {
                _conditionStates[condition] = false;
                
                if (condition is EventDrivenCondition_SO<int> intCondition)
                {
                    intCondition.SubscribeToEvent(() => OnChildConditionMet(condition));
                }
                else if (condition is EventDrivenCondition_SO<string> stringCondition)
                {
                    stringCondition.SubscribeToEvent(() => OnChildConditionMet(condition));
                }
                // Add more type checks as needed for other generic types
            }
        }

        private void OnChildConditionMet(Condition_SO condition)
        {
            _conditionStates[condition] = true;
            
            if (Evaluate())
            {
                _onConditionMet?.Invoke();
            }
        }

        public override bool Evaluate()
        {
            if (_conditions == null || _conditions.Count == 0)
            {
                return !IsInverted;
            }

            bool finalResult = _operator switch
            {
                CompositeOperator.And => _conditionStates.Values.All(state => state),
                CompositeOperator.Or => _conditionStates.Values.Any(state => state),
                _ => false
            };

            return IsInverted ? !finalResult : finalResult;
        }

        public void Cleanup()
        {
            foreach (var condition in _conditions)
            {
                if (condition is EventDrivenCondition_SO<int> intCondition)
                {
                    intCondition.UnsubscribeFromEvent();
                }
                else if (condition is EventDrivenCondition_SO<string> stringCondition)
                {
                    stringCondition.UnsubscribeFromEvent();
                }
            }
            
            _conditionStates.Clear();
            _onConditionMet = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}