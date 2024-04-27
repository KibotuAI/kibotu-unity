using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestEvent
    {
        [JsonProperty("eventName")]
        public string EventName { get; set; }
        
        [JsonProperty("eventValue")]
        public string EventValue { get; set; }
    }
}