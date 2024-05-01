using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestTriggerEvents
    {
        public KibotuQuestTriggerEvents()
        {
        }
        
        public KibotuQuestTriggerEvents(KibotuQuestTriggerEvents other)
        {
            other.Welcome.CopyTo(Welcome);
            other.Progress.CopyTo(Progress);
            other.Finish.CopyTo(Finish);
        }

        [JsonProperty("welcome")] public List<KibotuQuestEvent> Welcome;
        [JsonProperty("progress")] public List<KibotuQuestEvent> Progress;
        [JsonProperty("finish")] public List<KibotuQuestEvent> Finish;
    }
}