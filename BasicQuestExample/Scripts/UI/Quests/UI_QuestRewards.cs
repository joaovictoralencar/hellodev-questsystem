using HelloDev.QuestSystem.Quests;
using HelloDev.QuestSystem.ScriptableObjects;
using HelloDev.Utils;
using UnityEngine;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    public class UI_QuestRewards : MonoBehaviour
    {
        [SerializeField] private UI_QuestRewardItem RewardItemPrefab;
        [SerializeField] private Transform RewardsContainer;
        [SerializeField] private GameObject NoRewardsText;

        public void Setup(Quest quest)
        {
            RewardsContainer.DestroyAllChildren();
            if (quest.QuestData.Rewards.Count == 0)
            {
                NoRewardsText.gameObject.SetActive(true);
                return;
            }

            NoRewardsText.gameObject.SetActive(false);

            foreach (RewardInstance reward in quest.QuestData.Rewards)
            {
                UI_QuestRewardItem rewardItem = Instantiate(RewardItemPrefab, RewardsContainer);
                rewardItem.Setup(reward);
            }
        }
    }
}