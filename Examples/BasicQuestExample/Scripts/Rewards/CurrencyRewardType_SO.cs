using HelloDev.QuestSystem.ScriptableObjects;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicQuestExample.Rewards
{
    /// <summary>
    /// Reward type for granting currency (gold, gems, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyReward", menuName = "HelloDev/Quest System/Rewards/Currency Reward")]
    public class CurrencyRewardType_SO : QuestRewardType_SO
    {
        /// <summary>
        /// Grants currency to the player.
        /// </summary>
        /// <param name="amount">Amount of currency to grant.</param>
        public override void GiveReward(int amount)
        {
            // TODO: Integrate with your actual currency/wallet system
            // Example: PlayerWallet.Instance.AddGold(amount);
            Debug.Log($"[CurrencyReward] Granted {amount} gold to player!");
        }
    }
}
