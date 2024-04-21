using Newtonsoft.Json;

namespace kibotu
{
    public class KibotuQuestProgressMilestone
    {
        [JsonProperty("order")] public int Order;
        [JsonProperty("prizeTitle")] public string PrizeTitle;
        [JsonProperty("prizeImage")] public string PrizeImage;
        [JsonProperty("prizeSku")] public string PrizeSku;
        [JsonProperty("goal")] public int Goal;
        [JsonProperty("goalImage")] public string GoalImage;
    }
}