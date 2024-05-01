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
            Welcome = new List<string>(other.Welcome);
            Progress = new List<string>(other.Progress);
            Finish = new List<string>(other.Finish);
        }

        [JsonProperty("welcome")] public List<string> Welcome;
        [JsonProperty("progress")] public List<string> Progress;
        [JsonProperty("finish")] public List<string> Finish;
    }
}