using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuListResult<T>
    {
        public KibotuListResult<T>() {
        }
        
        public KibotuListResult<T>(List<T> other) {
            other.CopyTo(List);
        }

        [JsonProperty("list")]
        public List<T> List;
    }
    
    /*public static T GetRandomItem<T>(this IEnumerable<T> source)*/
}