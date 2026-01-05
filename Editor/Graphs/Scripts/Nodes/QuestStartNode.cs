using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point node for a Quest. Every QuestGraph should have exactly one.
    /// </summary>
    [Serializable]
    public class QuestStartNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_START_CONDITIONS = "StartConditions";

        #endregion

        #region Properties

        public List<Condition_SO> StartConditions => GetOptionValue<List<Condition_SO>>(OPT_START_CONDITIONS) ?? new List<Condition_SO>();

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption<List<Condition_SO>>(OPT_START_CONDITIONS)
                .WithDisplayName("Start Conditions")
                .WithTooltip("Conditions that must be met before this quest can start")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Start nodes only have output - no input
            context.AddOutputPort<StageFlow>("FirstStage")
                .WithDisplayName("First Stage")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
