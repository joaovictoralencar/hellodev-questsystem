using System.Collections.Generic;
using UnityEngine;

namespace HelloDev.QuestSystem
{
    public class QuestManager : MonoBehaviour
    {
        // private List<Quest> quests;
        //
        // public QuestManager()
        // {
        //     quests = new List<Quest>();
        // }
        //
        // public void AddQuest(Quest quest)
        // {
        //     quests.Add(quest);
        // }
        //
        // public void RemoveQuest(Quest quest)
        // {
        //     quests.Remove(quest);
        // }
        //
        // public void UpdateQuestProgress(string questId, float progress)
        // {
        //     Quest quest = quests.Find(q => q.Id == questId);
        //     if (quest != null)
        //     {
        //         quest.UpdateProgress(progress);
        //     }
        // }
        //
        // public List<Quest> GetActiveQuests()
        // {
        //     return quests.FindAll(q => !q.IsCompleted);
        // }
        //
        // public void CompleteQuest(string questId)
        // {
        //     Quest quest = quests.Find(q => q.Id == questId);
        //     if (quest != null)
        //     {
        //         quest.Complete();
        //     }
        // }
    }
}