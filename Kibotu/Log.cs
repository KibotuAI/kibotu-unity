using UnityEngine;

namespace kibotu
{
    public static partial class Kibotu
    {
        public static void Log(string s)
        {
            if (Config.ShowDebug)
            {
                Debug.Log("[Mixpanel] " + s);
            }
        }

        public static void LogError(string s)
        {
            if (Config.ShowDebug)
            {
                Debug.LogError("[Mixpanel] " + s);
            }
        }
    }
}
