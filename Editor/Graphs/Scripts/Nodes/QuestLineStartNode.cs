using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphToolkit.Editor;
using HelloDev.Conditions;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Entry point node for a QuestLine. Every QuestLineGraph should have exactly one.
    /// </summary>
    [Serializable]
    public class QuestLineStartNode : QuestBaseNode
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
                .WithTooltip("Conditions that must be met before this questline can start")
                .ShowInInspectorOnly();
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Start nodes only have output - no input
            context.AddOutputPort<QuestFlow>("FirstQuest")
                .WithDisplayName("First Quest")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();
        }

        #endregion
    }
}
