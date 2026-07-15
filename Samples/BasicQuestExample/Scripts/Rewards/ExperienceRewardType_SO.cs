using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicQuestExample.Rewards
{
    /// <summary>
    /// Reward type for granting experience points.
    /// </summary>
    [CreateAssetMenu(fileName = "ExperienceReward", menuName = "HelloDev/Quest System/Rewards/Experience Reward")]
    public class ExperienceRewardType_SO : QuestRewardType_SO
    {
        /// <summary>
        /// Grants experience points to the player.
        /// </summary>
        /// <param name="amount">Amount of XP to grant.</param>
        public override void GiveReward(int amount)
        {
            // TODO: Integrate with your actual XP/leveling system
            // Example: PlayerStats.Instance.AddExperience(amount);
            Debug.Log($"[ExperienceReward] Granted {amount} XP to player!");
        }
    }
}
