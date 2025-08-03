using System;

namespace HelloDev.QuestSystem
{
    [Serializable]
    public class QuestObjective
    {
        public string ObjectiveDescription { get; set; }
        public bool IsCompleted { get; private set; }

        public QuestObjective(string description)
        {
            ObjectiveDescription = description;
            IsCompleted = false;
        }

        public void CompleteObjective()
        {
            IsCompleted = true;
            // Additional logic for when the objective is completed can be added here
        }
    }
}