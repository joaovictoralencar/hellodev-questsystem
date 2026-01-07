using System;
using Unity.GraphToolkit.Editor;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Node that registers custom ScriptableObject types for use as Variables/Constants.
    /// </summary>
    /// <remarks>
    /// This node exists solely to register custom types with the Blackboard.
    /// Graph Toolkit discovers supported variable types by scanning port data types.
    /// Place this node once in your graph to enable custom ScriptableObject variables.
    ///
    /// Registered types include:
    /// - Quest System: Quest_SO, QuestLine_SO, Task_SO, QuestType_SO, QuestRewardType_SO
    /// - ID System: ID_SO
    /// - Condition System: Condition_SO, WorldFlagBase_SO
    /// - Event System: GameEventBase_SO
    /// - Unity: Sprite, LocalizedString
    /// </remarks>
    [Serializable]
    public class VariableTypesNode : QuestBaseNode
    {
        /// <inheritdoc/>
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Register all custom ScriptableObject types
            RegisterCustomVariableTypes(context);
        }
    }
}
