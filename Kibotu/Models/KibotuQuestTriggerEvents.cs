using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    public class KibotuQuestTriggerEvents
    {
        [JsonProperty("welcome")] public List<KibotuQuestEvent> Welcome;
        [JsonProperty("progress")] public List<KibotuQuestEvent> Progress;
        [JsonProperty("finish")] public List<KibotuQuestEvent> Finish;
    }
}