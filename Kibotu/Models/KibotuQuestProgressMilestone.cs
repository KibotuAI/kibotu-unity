using System;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestProgressMilestone
    {
        public KibotuQuestProgressMilestone()
        {
            
        }

        public KibotuQuestProgressMilestone(KibotuQuestProgressMilestone other)
        {
            Order = other.Order;
            PrizeTitle = other.PrizeTitle;
            PrizeImage = other.PrizeImage;
            PrizeSku = other.PrizeSku;
            Goal = other.Goal;
            GoalImage = other.GoalImage;
        }

        [JsonProperty("order")] public int Order;
        [JsonProperty("prizeTitle")] public string PrizeTitle;
        [JsonProperty("prizeImage")] public string PrizeImage;
        [JsonProperty("prizeSku")] public string PrizeSku;
        [JsonProperty("prizeValue")] public string PrizeValue; // String for all non-numeric use-cases
        [JsonProperty("goal")] public int Goal;
        [JsonProperty("goalImage")] public string GoalImage;
    }
}