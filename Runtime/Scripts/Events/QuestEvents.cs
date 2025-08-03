using System;

namespace HelloDev.QuestSystem.Events
{
    public delegate void QuestEventHandler(object sender, QuestEventArgs e);

    public static class QuestEvents
    {
        public static event QuestEventHandler QuestStarted;
        public static event QuestEventHandler QuestCompleted;
        public static event QuestEventHandler QuestFailed;

        public static void OnQuestStarted(QuestEventArgs e)
        {
            QuestStarted?.Invoke(null, e);
        }

        public static void OnQuestCompleted(QuestEventArgs e)
        {
            QuestCompleted?.Invoke(null, e);
        }

        public static void OnQuestFailed(QuestEventArgs e)
        {
            QuestFailed?.Invoke(null, e);
        }
    }
}