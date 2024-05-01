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
            Welcome = new List<KibotuQuestEvent>(other.Welcome);
            Progress = new List<KibotuQuestEvent>(other.Progress);
            Finish = new List<KibotuQuestEvent>(other.Finish);
        }

        [JsonProperty("welcome")] public List<KibotuQuestEvent> Welcome;
        [JsonProperty("progress")] public List<KibotuQuestEvent> Progress;
        [JsonProperty("finish")] public List<KibotuQuestEvent> Finish;
    }
}