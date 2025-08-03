using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    public Text questTitleText;
    public Text questDescriptionText;
    public GameObject questObjectiveList;
    
    public void UpdateQuestUI(string title, string description)
    {
        questTitleText.text = title;
        questDescriptionText.text = description;
        // Additional logic to update the objective list can be added here
    }

    public void ClearQuestUI()
    {
        questTitleText.text = string.Empty;
        questDescriptionText.text = string.Empty;
        // Logic to clear the objective list can be added here
    }
}