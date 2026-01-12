using System;
using Unity.GraphToolkit.Editor;
using HelloDev.Events;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// A node that fires a GameEvent when reached in the quest flow.
    /// </summary>
    /// <remarks>
    /// Use this for:
    /// - Triggering world events when reaching a stage
    /// - Notifying other systems of quest progression
    /// - Firing achievement or analytics events
    /// - Integrating with dialogue or cutscene systems
    ///
    /// The event is raised when the flow reaches this node, then continues
    /// to the next node via the "Then" port.
    ///
    /// Note: This node fires GameEventVoid_SO (parameterless events).
    /// For typed events, use the appropriate condition system or
    /// create a custom node for your specific event type.
    /// </remarks>
    [Serializable]
    public class EventTriggerNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_EVENT = "Event";
        private const string OPT_TRIGGER_NAME = "TriggerName";
        private const string OPT_DELAY_FRAMES = "DelayFrames";

        #endregion

        #region Properties

        /// <summary>
        /// The event to fire when this node is reached.
        /// </summary>
        public GameEventVoid_SO Event => GetOptionValue<GameEventVoid_SO>(OPT_EVENT);

        /// <summary>
        /// Developer-friendly name for this trigger (for graph readability).
        /// </summary>
        public string TriggerName => GetOptionValue<string>(OPT_TRIGGER_NAME);

        /// <summary>
        /// Number of frames to delay before firing the event (0 = immediate).
        /// </summary>
        public int DelayFrames => GetOptionValue<int>(OPT_DELAY_FRAMES);

        /// <summary>
        /// Display name shown on the node.
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(TriggerName)
            ? $"[Event] {TriggerName}"
            : Event != null
                ? $"[Event] {Event.name}"
                : "[Event] No Event";

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<GameEventVoid_SO>(OPT_EVENT)
                .WithDisplayName("Event")
                .WithTooltip("The GameEvent to fire when this node is reached");

            context.AddOption<string>(OPT_TRIGGER_NAME)
                .WithDisplayName("Trigger Name")
                .WithDefaultValue("")
                .WithTooltip("Optional developer-friendly name for this trigger")
                .Delayed();

            context.AddOption<int>(OPT_DELAY_FRAMES)
                .WithDisplayName("Delay Frames")
                .WithDefaultValue(0)
                .WithTooltip("Number of frames to wait before firing (0 = immediate)")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Input: From previous node in the flow
            context.AddInputPort<StageFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            // Continue flow after firing the event
            context.AddOutputPort<StageFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
