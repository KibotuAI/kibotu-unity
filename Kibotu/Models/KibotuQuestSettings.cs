using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestSettings
    {
        public KibotuQuestSettings()
        {
        }

        public KibotuQuestSettings(KibotuQuestSettings other)
        {
        }

        // [JsonProperty("allowCollectingMilestones")] public bool allowCollectingMilestones;
        [JsonProperty("requiredOptInToQuest")] public bool requiredOptInToQuest;
    }
}