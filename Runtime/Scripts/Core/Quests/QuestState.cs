using UnityEngine;

namespace HelloDev.QuestSystem
{
    /// <summary>
    /// Represents the current state of a quest.
    /// </summary>
    public enum QuestState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed,
    }
    
    /// <summary>
    /// Represents the current state of a task within a quest.
    /// </summary>
    public enum TaskState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }
}