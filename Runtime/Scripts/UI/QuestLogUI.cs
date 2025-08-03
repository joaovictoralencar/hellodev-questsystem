using UnityEngine;
using UnityEngine.UI;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private GameObject questLogPanel;
    [SerializeField] private Text questLogText;

    private void Start()
    {
        HideQuestLog();
    }

    public void ShowQuestLog(string questDetails)
    {
        questLogText.text = questDetails;
        questLogPanel.SetActive(true);
    }

    public void HideQuestLog()
    {
        questLogPanel.SetActive(false);
    }
}