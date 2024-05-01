using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kibotu
{
    [Serializable]
    public class KibotuListResult<T>
    {
        public KibotuListResult() {
        }

        public KibotuListResult(List<T> other) {
            other.CopyTo(List);
        }
        
        [JsonProperty("list")]
        public List<T> List;
    }
}