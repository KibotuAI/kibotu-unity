using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestTriggers
    {
        [JsonProperty("state")] public KibotuQuestTriggerEvents State;
        [JsonProperty("ui")] public KibotuQuestTriggerEvents UI;
    }
}