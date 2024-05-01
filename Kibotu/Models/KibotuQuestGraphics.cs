using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestGraphics
    {
        public KibotuQuestGraphics()
        {
        }
        
        public KibotuQuestGraphics(KibotuQuestGraphics other)
        {
            Welcome = new KibotuQuestGraphic(other.Welcome);
            // Started = new KibotuQuestGraphic(other.Started);
            Progress = new KibotuQuestGraphic(other.Progress);
            Lost = new KibotuQuestGraphic(other.Lost);
            Won = new KibotuQuestGraphic(other.Won);
        }


        
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