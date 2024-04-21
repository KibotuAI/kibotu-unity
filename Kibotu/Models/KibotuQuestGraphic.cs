using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuQuestGraphic
    {
        [JsonProperty("background")] public string Background;
        [JsonProperty("titleImage")] public string TitleImage;
        // [JsonProperty("base")] public string Base;
        // [JsonProperty("preview")] public string Preview;
    }
}