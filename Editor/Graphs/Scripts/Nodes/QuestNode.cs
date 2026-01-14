using System;
using HelloDev.QuestSystem.QuestGraph.Editor.Converters;
using HelloDev.QuestSystem.QuestGraph.Editor.Ports;
using HelloDev.QuestSystem.ScriptableObjects;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Localization;

namespace HelloDev.QuestSystem.QuestGraph.Editor.Nodes
{
    /// <summary>
    /// Node for referencing or defining a Quest in a QuestLine graph.
    /// Supports two modes: Asset Mode (reference existing Quest_SO) or Define Mode (create inline).
    /// </summary>
    /// <remarks>
    /// Follows the StageNode pattern for context inputs:
    /// - Boolean toggle "Use Quest Asset" controls which ports are shown
    /// - Asset Mode: Shows Quest Asset input port only
    /// - Define Mode: Shows inline data ports and context inputs
    ///
    /// Output ports:
    /// - Then (QuestFlow): Connects to the next quest in the questline chain
    /// - Stages (StageFlow): Connects to the first stage of this quest
    ///
    /// Input ports (Define mode):
    /// - Trigger Conditions (ConditionFlow): Receives from ConditionContextNode
    /// - Fail Conditions (ConditionFlow): Receives from ConditionContextNode
    /// - Rewards (RewardFlow): Receives from RewardContextNode
    ///
    /// This design allows quests to chain to other quests while also defining their stages,
    /// conditions, and rewards via context nodes with draggable blocks.
    /// </remarks>
    [Serializable]
    public class QuestNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_USE_QUEST_ASSET = "UseQuestAsset";

        #endregion

        #region Port Names

        // Asset Mode
        private const string PORT_QUEST_ASSET = "QuestAssetInput";

        // Define Mode - Identity ports (visible on node)
        private const string PORT_DEV_NAME = "DevNameInput";
        private const string PORT_IS_OPTIONAL = "IsOptionalInput";
        private const string PORT_RECOMMENDED_LEVEL = "RecommendedLevelInput";

        // Define Mode - Display (LocalizedStrings and assets)
        private const string PORT_QUEST_TYPE = "QuestTypeInput";
        private const string PORT_DISPLAY_NAME = "DisplayNameInput";
        private const string PORT_DESCRIPTION = "DescriptionInput";
        private const string PORT_LOCATION = "LocationInput";
        private const string PORT_SPRITE = "SpriteInput";

        // Define Mode - Flow outputs
        private const string PORT_STAGES_FLOW = "Stages";

        // Define Mode - Context inputs (receive from ConditionContextNode/RewardContextNode)
        private const string PORT_TRIGGER_CONDITIONS = "TriggerConditionsInput";
        private const string PORT_FAIL_CONDITIONS = "FailConditionsInput";
        private const string PORT_GLOBAL_TASK_FAILURE = "GlobalTaskFailureInput";
        private const string PORT_REWARDS = "RewardsInput";

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use an existing Quest Asset (true) or define inline (false).
        /// </summary>
        public bool UseQuestAsset => GetOptionValue<bool>(OPT_USE_QUEST_ASSET);

        /// <summary>
        /// Whether this quest is optional in the questline.
        /// </summary>
        public bool IsOptional => GraphTraversalUtility.ResolveDataPort<bool>(this, PORT_IS_OPTIONAL, false);

        /// <summary>
        /// The referenced Quest_SO asset (Asset mode only).
        /// </summary>
        public Quest_SO QuestAsset
        {
            get
            {
                if (!UseQuestAsset)
                    return null;
                return GraphTraversalUtility.ResolveDataPort<Quest_SO>(this, PORT_QUEST_ASSET, null);
            }
        }

        /// <summary>
        /// Dev name for this quest (Define mode only).
        /// </summary>
        public string DevName => GraphTraversalUtility.ResolveDataPort<string>(this, PORT_DEV_NAME, "New Quest");

        /// <summary>
        /// Quest type from port (Define mode only).
        /// </summary>
        public QuestType_SO QuestType => GraphTraversalUtility.ResolveDataPort<QuestType_SO>(this, PORT_QUEST_TYPE, null);

        /// <summary>
        /// Recommended player level for this quest (Define mode only).
        /// </summary>
        public int RecommendedLevel => GraphTraversalUtility.ResolveDataPort<int>(this, PORT_RECOMMENDED_LEVEL, -1);

        /// <summary>
        /// Display name from port (Define mode only).
        /// </summary>
        public LocalizedString DisplayNameLocalized => GraphTraversalUtility.ResolveDataPort<LocalizedString>(this, PORT_DISPLAY_NAME, default);

        /// <summary>
        /// Description from port (Define mode only).
        /// </summary>
        public LocalizedString Description => GraphTraversalUtility.ResolveDataPort<LocalizedString>(this, PORT_DESCRIPTION, default);

        /// <summary>
        /// Location from port (Define mode only).
        /// </summary>
        public LocalizedString Location => GraphTraversalUtility.ResolveDataPort<LocalizedString>(this, PORT_LOCATION, default);

        /// <summary>
        /// Sprite from port (Define mode only).
        /// </summary>
        public Sprite QuestSprite => GraphTraversalUtility.ResolveDataPort<Sprite>(this, PORT_SPRITE, null);

        /// <summary>
        /// Whether this node has a valid quest configuration.
        /// </summary>
        public bool HasValidQuest
        {
            get
            {
                if (UseQuestAsset)
                    return QuestAsset != null;

                // Define mode: at least need a dev name
                return !string.IsNullOrWhiteSpace(DevName);
            }
        }

        /// <summary>
        /// Display name for this node.
        /// </summary>
        public string DisplayName
        {
            get
            {
                string prefix = IsOptional ? "[Optional] " : "";
                if (UseQuestAsset && QuestAsset != null)
                    return prefix + QuestAsset.DevName;
                if (!UseQuestAsset && !string.IsNullOrEmpty(DevName))
                    return prefix + DevName;
                return "[Quest] " + (UseQuestAsset ? "No Asset" : "Unnamed");
            }
        }

        /// <summary>
        /// Quest ID (from asset or empty for define mode).
        /// </summary>
        public string QuestId
        {
            get
            {
                if (UseQuestAsset && QuestAsset != null)
                    return QuestAsset.QuestId.ToString();
                return string.Empty;
            }
        }

        #endregion

        #region Option Definition

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            // Mode toggle - this is the key option that switches between Asset/Define mode
            context.AddOption<bool>(OPT_USE_QUEST_ASSET)
                .WithDisplayName("Use Quest Asset")
                .WithDefaultValue(true)
                .WithTooltip("Check to use an existing Quest_SO asset.\nUncheck to define quest inline.");
        }

        #endregion

        #region Port Definition

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            // Flow ports (always shown) - QuestFlow for quest-to-quest connections
            context.AddInputPort<QuestFlow>("In")
                .WithDisplayName("In")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            context.AddOutputPort<QuestFlow>("Then")
                .WithDisplayName("Then")
                .WithConnectorUI(PortConnectorUI.Arrowhead)
                .Build();

            if (UseQuestAsset)
            {
                // Asset Mode: Show Quest Asset input port only
                context.AddInputPort<Quest_SO>(PORT_QUEST_ASSET)
                    .WithDisplayName("Quest Asset")
                    .Build();
            }
            else
            {
                // Define Mode: Show data ports and context inputs

                // Identity ports - visible on node and in Node Properties
                context.AddInputPort<string>(PORT_DEV_NAME)
                    .WithDisplayName("Dev Name")
                    .Build();

                context.AddInputPort<bool>(PORT_IS_OPTIONAL)
                    .WithDisplayName("Is Optional")
                    .Build();

                context.AddInputPort<int>(PORT_RECOMMENDED_LEVEL)
                    .WithDisplayName("Recommended Level")
                    .Build();

                // Display ports - editable in Node Properties inspector
                context.AddInputPort<QuestType_SO>(PORT_QUEST_TYPE)
                    .WithDisplayName("Quest Type")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DISPLAY_NAME)
                    .WithDisplayName("Display Name")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_DESCRIPTION)
                    .WithDisplayName("Description")
                    .Build();

                context.AddInputPort<LocalizedString>(PORT_LOCATION)
                    .WithDisplayName("Location")
                    .Build();

                context.AddInputPort<Sprite>(PORT_SPRITE)
                    .WithDisplayName("Sprite")
                    .Build();

                // StageFlow output - connect to StageNodes via flow
                context.AddOutputPort<StageFlow>(PORT_STAGES_FLOW)
                    .WithDisplayName("Stages")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                // Context inputs - receive from ConditionContextNode/RewardContextNode
                context.AddInputPort<ConditionFlow>(PORT_TRIGGER_CONDITIONS)
                    .WithDisplayName("Trigger Conditions")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                context.AddInputPort<ConditionFlow>(PORT_FAIL_CONDITIONS)
                    .WithDisplayName("Fail Conditions")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                context.AddInputPort<ConditionFlow>(PORT_GLOBAL_TASK_FAILURE)
                    .WithDisplayName("Global Task Failure")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();

                context.AddInputPort<RewardFlow>(PORT_REWARDS)
                    .WithDisplayName("Rewards")
                    .WithConnectorUI(PortConnectorUI.Arrowhead)
                    .Build();
            }
        }

        #endregion
    }
}
