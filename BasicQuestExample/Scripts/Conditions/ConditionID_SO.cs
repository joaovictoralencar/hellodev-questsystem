using HelloDev.Conditions;
using HelloDev.QuestSystem.BasicQuestExample.GameEvents;
using HelloDev.Utils;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicQuestExample.Conditions
{
    [CreateAssetMenu(fileName = "ID Condition", menuName = "HelloDev/Conditions/ID Condition")]
    public class ConditionID_SO : ConditionEventDriven_SO<ID_SO>
    {
        [Header("Event Reference")]
        [SerializeField] private GameEventID_SO GameEventID;

        protected override void SubscribeToSpecificEvent()
        {
            if (GameEventID != null)
            {
                GameEventID.AddListener(OnEventTriggered);
            }
        }

        protected override void UnsubscribeFromSpecificEvent()
        {
            if (GameEventID != null)
            {
                GameEventID.RemoveListener(OnEventTriggered);
            }
        }

        protected override bool CompareValues(ID_SO eventValue, ID_SO targetValue, ComparisonType comparisonType)
        {
            return comparisonType switch
            {
                ComparisonType.Equals => eventValue == targetValue,
                _ => false
            };
        }
    }
}