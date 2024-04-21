using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    public class KibotuQuestTriggers
    {
        [JsonProperty("state")] public KibotuQuestTriggerEvents State;
        [JsonProperty("ui")] public KibotuQuestTriggerEvents UI;
    }
}