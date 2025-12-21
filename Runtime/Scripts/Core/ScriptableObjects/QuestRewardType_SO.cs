using System;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.ScriptableObjects
{
    public abstract class QuestRewardType_SO : ScriptableObject
    {
        [SerializeField] Sprite Icon;
        [SerializeField] LocalizedString Name;

        public Sprite RewardIcon => Icon;
        public LocalizedString RewardName => Name;
        public abstract void GiveReward(int amount);
    }
    
    [Serializable]
    public struct RewardInstance
    {
        public QuestRewardType_SO RewardType;
        public int Amount;
    }
}