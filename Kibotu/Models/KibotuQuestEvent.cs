using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestEvent
    {
        public KibotuQuestEvent()
        {
        }

        public KibotuQuestEvent(KibotuQuestEvent other)
        {
            EventName = other.EventName;
            EventValue = other.EventValue;
        }
        
        [JsonProperty("eventName")]
        public string EventName { get; set; }
        
        [JsonProperty("eventValue")]
        public string EventValue { get; set; }
    }
}