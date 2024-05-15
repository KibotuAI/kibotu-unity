using System;
using System.Collections.Generic;

namespace kibotu
{
    [Serializable]
    public class KibotuEvent
    {
        public string EventName;
        public Dictionary<string, object> EventData;

        public KibotuEvent(string name, Dictionary<string, object> data)
        {
            EventName = name;
            EventData = data;
        }

        public KibotuEvent(KibotuEvent other)
        {
            EventName = other.EventName;
            EventData = new Dictionary<string, object>(other.EventData);
        }
    }
}