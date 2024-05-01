using System;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestProgress
    {
        public KibotuQuestProgress()
        {
            
        }
        public KibotuQuestProgress(KibotuQuestProgress other)
        {
            CurrentState = other.CurrentState;
            CurrentStep = other.CurrentStep;
        }
        
        [JsonProperty("status")] public string CurrentState;
        
        public EnumQuestStates Status
        {
            get
            {
                switch (CurrentState)
                {
                    case "started":
                        return EnumQuestStates.Progress;
                    // case "finished":
                        // return EnumQuestStates.Finished;
                    case "lost":
                        return EnumQuestStates.Lost;
                    case "won":
                        return EnumQuestStates.Won;
                    case "welcome":
                    default:
                        return EnumQuestStates.Welcome;
                }
            }
            set
            {
                switch (value)
                {
                    case EnumQuestStates.Welcome:
                        CurrentState = "welcome";
                        break;
                    case EnumQuestStates.Progress:
                        CurrentState = "started";
                        break;
                    case EnumQuestStates.Won:
                        CurrentState = "won";
                        break;
                    case EnumQuestStates.Lost:
                        CurrentState = "lost";
                        break;
                }
            }
        }

        [JsonProperty("questProgress")] public int CurrentStep = 0;
        
        public string ProgressKey => $"{CurrentState}_{CurrentStep}";
    }
}