using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicQuestExample.Rewards
{
    [CreateAssetMenu(fileName = "ExperienceQuestReward", menuName = "HelloDev/Quest System/Rewards/Experience Quest RewardType")]
    public class ExperienceQuestRewardType_SO : QuestRewardType_SO
    {
        public override void GiveReward(int amount)
        {
            Debug.Log($"Added {amount} experience to the player!");
        }
    }
    
}
