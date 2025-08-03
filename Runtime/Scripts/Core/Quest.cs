using System;

namespace HelloDev.QuestSystem
{
    public class Quest
    {
        public string QuestName { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; private set; }

        public Quest(string questName, string description)
        {
            QuestName = questName;
            Description = description;
            IsCompleted = false;
        }

        public void CompleteQuest()
        {
            IsCompleted = true;
            // Additional logic for completing the quest can be added here
        }

        public void ResetQuest()
        {
            IsCompleted = false;
            // Additional logic for resetting the quest can be added here
        }
    }
}