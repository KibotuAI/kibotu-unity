using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestTriggers
    {
        public KibotuQuestTriggers()
        {
        }
        public KibotuQuestTriggers(KibotuQuestTriggers other)
        {
            State = new KibotuQuestTriggerEvents(other.State);
            UI = new KibotuQuestTriggerEvents(other.UI);
        }
        [JsonProperty("state")] public KibotuQuestTriggerEvents State;
        [JsonProperty("ui")] public KibotuQuestTriggerEvents UI;
    }
}