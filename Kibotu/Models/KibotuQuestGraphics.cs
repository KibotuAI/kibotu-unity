using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestGraphics
    {
        [JsonProperty("welcome")] public KibotuQuestGraphic Welcome;
        // [JsonProperty("started")] public KibotuQuestGraphic Started;
        [JsonProperty("progress")] public KibotuQuestGraphic Progress;
        [JsonProperty("lost")] public KibotuQuestGraphic Lost;
        [JsonProperty("won")] public KibotuQuestGraphic Won;

        public KibotuQuestGraphic GetGraphic(EnumQuestStates? state)
        {
            switch (state)
            {
                case EnumQuestStates.Welcome:
                    return Welcome;
                case EnumQuestStates.Progress:
                    return Progress;
                case EnumQuestStates.Lost:
                    return Lost;
                case EnumQuestStates.Won:
                    return Won;
                default:
                    return null;
            }
        }
    }
}