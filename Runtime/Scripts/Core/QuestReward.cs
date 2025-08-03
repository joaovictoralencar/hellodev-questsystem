using System;

namespace HelloDev.QuestSystem
{
    [Serializable]
    public class QuestReward
    {
        public string RewardName;
        public int RewardAmount;

        public QuestReward(string rewardName, int rewardAmount)
        {
            RewardName = rewardName;
            RewardAmount = rewardAmount;
        }

        public void GrantReward()
        {
            // Logic to grant the reward to the player
            Console.WriteLine($"Granted {RewardAmount} of {RewardName}.");
        }
    }
}