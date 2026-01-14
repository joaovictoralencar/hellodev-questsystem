using System;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Ports
{
    /// <summary>
    /// Represents flow between quests in a QuestLine.
    /// </summary>
    [Serializable]
    public class QuestFlow { }

    /// <summary>
    /// Represents flow between stages in a Quest.
    /// </summary>
    [Serializable]
    public class StageFlow { }

    /// <summary>
    /// Represents flow within a TaskGroup.
    /// </summary>
    [Serializable]
    public class TaskFlow { }

    /// <summary>
    /// Represents a condition evaluation output.
    /// </summary>
    [Serializable]
    public class ConditionResult { }

    /// <summary>
    /// Represents a player choice branch.
    /// </summary>
    [Serializable]
    public class ChoiceFlow { }

    /// <summary>
    /// Represents flow from ConditionContextNode to QuestNode.
    /// Used for trigger conditions and failure conditions.
    /// </summary>
    [Serializable]
    public class ConditionFlow { }

    /// <summary>
    /// Represents flow from RewardContextNode to QuestNode.
    /// Used for quest rewards.
    /// </summary>
    [Serializable]
    public class RewardFlow { }
}