using HelloDev.Events;
using HelloDev.IDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicQuestExample.GameEvents
{
    [CreateAssetMenu(fileName = "GameEventID", menuName = "HelloDev/Events/ID Game Event")]
    public class GameEventID_SO : GameEvent_SO<ID_SO>
    {
    }
}