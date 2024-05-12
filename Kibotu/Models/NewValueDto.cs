using System;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class NewValueDto
    {
        [JsonProperty("newValue")] public int NewValue = 0;

        public NewValueDto()
        {
            
        }
        public NewValueDto(NewValueDto other)
        {
            NewValue = other.NewValue;
        }
    }
}