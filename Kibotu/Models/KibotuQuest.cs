﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quantum;
using UnityEngine;

namespace kibotu
{
    [Serializable]
    public class KibotuQuest
    {
        [JsonProperty("_id")] public string Id;
        [JsonProperty("name")] public string Title;
        [JsonProperty("enabled")] public string Enabled;
        [JsonProperty("progress")] [CanBeNull] public KibotuQuestProgress Progress;
        [JsonProperty("milestones")] public KibotuQuestProgressMilestone[] Milestones;
        [JsonProperty("countryCodesArray")] public List<string> CountryCodes;
        [JsonProperty("graphics")] public KibotuQuestGraphics Graphics;
        [JsonProperty("triggers")] public KibotuQuestTriggers Triggers;
        [JsonProperty("collectibleIconImage")] public string CollectibleIconImage;
        
        public int TotalSteps
        {
            get
            {
                return Milestones[Milestones.Length - 1].Goal;
            }
        }
        
        public JObject TargetFilter;

        public DateTime from;
        public DateTime to;

        [CanBeNull]
        public string GetPrize()
        {
            if (Progress?.Status == EnumQuestStates.Won)
            {
                // Iterate over milestones in reverse order and check if we have reached the goal
                for (int i = Milestones.Length - 1; i >= 0; i--)
                {
                    var curMile = Milestones[i];
                    if (Progress.CurrentStep >= curMile.Goal)
                    {
                        // reached the goal
                        return curMile.PrizeSku;
                    }
                }
            }

            return null;
        }

        public bool TryPassingConditions(JObject properties)
        {
            var match = new ConditionEvaluationProvider().EvalCondition(properties, TargetFilter);
            return match;
        }

        /**
        * true indicates we need to start a new quest
        */
        public bool TryTriggersStateStarting(Dictionary<string, object> properties, string eventName, int? eventValue)
        {
            // Check if current event triggers the quest
            var isTriggerMatching = Triggers.State.Welcome.FindAll(x => x.EventName == eventName).Count > 0;
            if (isTriggerMatching)
            {
                // Check if properties matches conditions
                if (TryPassingConditions(JObject.FromObject(properties)))
                {
                    // Start the quest
                    return true;
                }
            }

            return false;
        }
        
        /**
         * true to increment the progress for an active state
         */
        public bool TryTriggersStateProgressing(Dictionary<string, object> properties, string eventName, int? eventValue)
        {
            // Check if current event triggers the quest
            if (Progress != null && (Progress.Status == EnumQuestStates.Welcome || Progress.Status == EnumQuestStates.Progress))
            {
                var isTriggerMatching = Triggers.State.Progress.FindAll(x => x.EventName == eventName).Count > 0;
                if (isTriggerMatching)
                {
                    // Check if properties matches conditions
                    if (TryPassingConditions(JObject.FromObject(properties)))
                    {
                        // Increment progress
                        return true;
                    }
                }
            }
            else
            {
                // Active quest not in relevant state
            }

            return false;
        }

        public bool TryTriggersUI(Dictionary<string, object> properties, string eventName, int? eventValue)
        {
            // Relevant only for active quest
            if (Progress == null)
            {
                return false;
            }
            
            // Check if current event can trigger the UI
            List<KibotuQuestEvent> triggersAllowList = new List<KibotuQuestEvent>();
            
            switch (Progress.Status)
            {
                case EnumQuestStates.Welcome:
                    triggersAllowList = Triggers.UI.Welcome;
                    break;
                case EnumQuestStates.Progress:
                    triggersAllowList = Triggers.UI.Progress;
                    break;
                case EnumQuestStates.Won:
                case EnumQuestStates.Lost:
                    triggersAllowList = Triggers.UI.Finish;
                    break;
            }
            
            var isTriggerMatching = triggersAllowList.FindAll(x => x.EventName == eventName).Count > 0;
            if (isTriggerMatching)
            {
                return true;
            }

            return false;
        }
    }
}