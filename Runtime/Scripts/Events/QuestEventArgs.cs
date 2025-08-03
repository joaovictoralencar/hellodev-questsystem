using System;

namespace HelloDev.QuestSystem.Events
{
    public class QuestEventArgs : EventArgs
    {
        public string QuestId { get; }
        public string QuestName { get; }
        public string Status { get; }

        public QuestEventArgs(string questId, string questName, string status)
        {
            QuestId = questId;
            QuestName = questName;
            Status = status;
        }
    }
}