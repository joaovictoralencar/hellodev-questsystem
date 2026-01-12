using System;
using HelloDev.Conditions;
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
    /// Follows the TaskTypedNode pattern:
    /// - Boolean toggle "Use Quest Asset" controls which ports are shown
    /// - Asset Mode: Shows Quest Asset input port only
    /// - Define Mode: Shows inline data ports for all Quest_SO fields
    ///
    /// In Define Mode, stages can be connected via StageGraph subgraph references
    /// which will be converted to stages during export.
    /// </remarks>
    [Serializable]
    public class QuestNode : QuestBaseNode
    {
        #region Option Names

        private const string OPT_USE_QUEST_ASSET = "UseQuestAsset";

        // Count options - must remain options for dynamic port regeneration
        private const string OPT_STAGE_COUNT = "StageCount";
        private const string OPT_START_CONDITION_COUNT = "StartConditionCount";
        private const string OPT_FAILURE_CONDITION_COUNT = "FailureConditionCount";
        private const string OPT_REWARD_COUNT = "RewardCount";

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

        // Define Mode - Dynamic ports (prefix + index)
        private const string PORT_STAGE = "StageInput";
        private const string PORT_START_CONDITION = "StartConditionInput";
        private const string PORT_FAILURE_CONDITION = "FailureConditionInput";
        private const string PORT_REWARD = "RewardInput";

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
        /// Number of stage ports to show (Define mode).
        /// </summary>
        public int StageCount => GetOptionValue<int>(OPT_STAGE_COUNT);

        /// <summary>
        /// Number of start condition ports to show (Define mode).
        /// </summary>
        public int StartConditionCount => GetOptionValue<int>(OPT_START_CONDITION_COUNT);

        /// <summary>
        /// Number of failure condition ports to show (Define mode).
        /// </summary>
        public int FailureConditionCount => GetOptionValue<int>(OPT_FAILURE_CONDITION_COUNT);

        /// <summary>
        /// Number of reward ports to show (Define mode).
        /// </summary>
        public int RewardCount => GetOptionValue<int>(OPT_REWARD_COUNT);

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

            // Only show count options in Define mode
            if (!UseQuestAsset)
            {
                // Count options - control dynamic port generation
                context.AddOption<int>(OPT_STAGE_COUNT)
                    .WithDisplayName("Stage Count")
                    .WithDefaultValue(1)
                    .WithTooltip("Number of stage ports to show");

                context.AddOption<int>(OPT_START_CONDITION_COUNT)
                    .WithDisplayName("Start Cond Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of start condition ports to show");

                context.AddOption<int>(OPT_FAILURE_CONDITION_COUNT)
                    .WithDisplayName("Fail Cond Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of failure condition ports to show");

                context.AddOption<int>(OPT_REWARD_COUNT)
                    .WithDisplayName("Reward Count")
                    .WithDefaultValue(0)
                    .WithTooltip("Number of reward ports to show");
            }
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
                // Define Mode: Show data ports and dynamic ports

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

                // Dynamic stage ports - accept StageFlow from StageNodes
                for (int i = 0; i < StageCount; i++)
                {
                    context.AddInputPort<StageFlow>(PORT_STAGE + i)
                        .WithDisplayName($"Stage {i + 1}")
                        .WithConnectorUI(PortConnectorUI.Arrowhead)
                        .Build();
                }

                // Dynamic start condition ports
                for (int i = 0; i < StartConditionCount; i++)
                {
                    context.AddInputPort<Condition_SO>(PORT_START_CONDITION + i)
                        .WithDisplayName($"Start Cond {i + 1}")
                        .Build();
                }

                // Dynamic failure condition ports
                for (int i = 0; i < FailureConditionCount; i++)
                {
                    context.AddInputPort<Condition_SO>(PORT_FAILURE_CONDITION + i)
                        .WithDisplayName($"Fail Cond {i + 1}")
                        .Build();
                }

                // Dynamic reward ports
                for (int i = 0; i < RewardCount; i++)
                {
                    context.AddInputPort<RewardInstance>(PORT_REWARD + i)
                        .WithDisplayName($"Reward {i + 1}")
                        .Build();
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets a stage from the dynamic port by index.
        /// </summary>
        public StageGraph GetStageGraph(int index)
        {
            if (index < 0 || index >= StageCount)
                return null;
            return GraphTraversalUtility.ResolveDataPort<StageGraph>(this, PORT_STAGE + index, null);
        }

        /// <summary>
        /// Gets a start condition from the dynamic port by index.
        /// </summary>
        public Condition_SO GetStartCondition(int index)
        {
            if (index < 0 || index >= StartConditionCount)
                return null;
            return GraphTraversalUtility.ResolveDataPort<Condition_SO>(this, PORT_START_CONDITION + index, null);
        }

        /// <summary>
        /// Gets a failure condition from the dynamic port by index.
        /// </summary>
        public Condition_SO GetFailureCondition(int index)
        {
            if (index < 0 || index >= FailureConditionCount)
                return null;
            return GraphTraversalUtility.ResolveDataPort<Condition_SO>(this, PORT_FAILURE_CONDITION + index, null);
        }

        /// <summary>
        /// Gets a reward from the dynamic port by index.
        /// </summary>
        public RewardInstance? GetReward(int index)
        {
            if (index < 0 || index >= RewardCount)
                return null;
            return GraphTraversalUtility.ResolveDataPort<RewardInstance>(this, PORT_REWARD + index, default);
        }

        #endregion
    }
}
