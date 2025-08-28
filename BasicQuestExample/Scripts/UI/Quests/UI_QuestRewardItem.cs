using HelloDev.QuestSystem.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace HelloDev.QuestSystem.BasicQuestExample.UI
{
    public class UI_QuestRewardItem : MonoBehaviour
    {
        [SerializeField] private Image rewardImage;
        [SerializeField] private LocalizeStringEvent rewardNameText;
        [SerializeField] private TextMeshProUGUI rewardAmountText;

        public void Setup(RewardInstance reward)
        {
            QuestRewardType_SO questRewardType = reward.RewardType;
            rewardImage.sprite = questRewardType.RewardIcon;
            rewardNameText.StringReference = questRewardType.RewardName;
            rewardAmountText.gameObject.SetActive(reward.Amount > 1);
            rewardAmountText.text = reward.Amount.ToString();
        }
    }
}