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

        public KibotuListResult(KibotuListResult<T> other) {
            List = new List<T>(other.List);
        }

        [JsonProperty("list")]
        public List<T> List;
    }
}