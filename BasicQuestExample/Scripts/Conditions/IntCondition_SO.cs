using UnityEngine;
using HelloDev.Events;

namespace HelloDev.QuestSystem.Conditions.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Int Condition", menuName = "HelloDev/Conditions/Int Condition")]
    public class IntCondition_SO : EventDrivenCondition_SO<int>
    {
        [Header("Event Reference")]
        [SerializeField] private IntGameEvent IntGameEvent;

        protected override void SubscribeToSpecificEvent()
        {
            if (IntGameEvent != null)
            {
                IntGameEvent.AddListener(OnEventTriggered);
            }
        }

        protected override void UnsubscribeFromSpecificEvent()
        {
            if (IntGameEvent != null)
            {
                IntGameEvent.RemoveListener(OnEventTriggered);
            }
        }

        protected override bool CompareValues(int eventValue, int targetValue, ComparisonType comparisonType)
        {
            return comparisonType switch
            {
                ComparisonType.Equals => eventValue == targetValue,
                ComparisonType.NotEquals => eventValue != targetValue,
                ComparisonType.GreaterThan => eventValue > targetValue,
                ComparisonType.GreaterThanOrEqual => eventValue >= targetValue,
                ComparisonType.LessThan => eventValue < targetValue,
                ComparisonType.LessThanOrEqual => eventValue <= targetValue,
                _ => false
            };
        }
    }
}