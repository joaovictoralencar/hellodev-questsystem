using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point node for a Quest or Questline graph.
    /// Supports two output modes: Stage (for quest graphs) or Quest (for questline graphs).
    /// </summary>
    [Serializable]
    public class QuestStartNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_OUTPUT_MODE = "OutputMode";
        private const string OPT_START_CONDITIONS = "StartConditions";

        #endregion

        #region Enums

        /// <summary>
        /// Determines what type of flow this start node outputs.
        /// </summary>
        public enum StartNodeOutputMode
        {
            /// <summary>Outputs StageFlow - use in quest graphs to connect to stages.</summary>
            Stage = 0,
            /// <summary>Outputs QuestFlow - use in questline graphs to connect to quests.</summary>
            Quest = 1
        }

        #endregion

        #region Properties

        /// <summary>
        /// The output mode for this start node.
        /// </summary>
        public StartNodeOutputMode OutputMode => GetOptionValue<StartNodeOutputMode>(OPT_OUTPUT_MODE);

        /// <summary>
        /// Whether this node outputs StageFlow (true) or QuestFlow (false).
        /// </summary>
        public bool IsStageMode => OutputMode == StartNodeOutputMode.Stage;

        /// <summary>
        /// Whether this node outputs QuestFlow (true) or StageFlow (false).
        /// </summary>
        public bool IsQuestMode => OutputMode == StartNodeOutputMode.Quest;

        public List<Condition_SO> StartConditions => GetOptionValue<List<Condition_SO>>(OPT_START_CONDITIONS) ?? new List<Condition_SO>();

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<StartNodeOutputMode>(OPT_OUTPUT_MODE)
                .WithDisplayName("Output Mode")
                .WithDefaultValue(StartNodeOutputMode.Stage)
                .WithTooltip("Stage: Connect to stages (quest graph)\nQuest: Connect to quests (questline graph)");

            context.AddOption<List<Condition_SO>>(OPT_START_CONDITIONS)
                .WithDisplayName("Start Conditions")
                .WithTooltip("Conditions that must be met before this quest/questline can start")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Start nodes only have output - no input
            if (IsStageMode)
            {
                context.AddOutputPort<StageFlow>("FirstStage")
                    .WithDisplayName("First Stage")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
            else
            {
                context.AddOutputPort<QuestFlow>("FirstQuest")
                    .WithDisplayName("First Quest")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion
    }
}
