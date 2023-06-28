namespace kibotu
{
    internal static class Config
    {
        // Can be overriden by MixpanelSettings
        internal static string TrackUrl = "https://api.kibotu.ai/track";
        internal static string EngageUrl = "https://api.kibotu.ai/engage";
        internal static bool ShowDebug = false;
        internal static bool ManualInitialization = false;
        internal static float FlushInterval = 60f;

        internal static int BatchSize = 50;

        internal const int PoolFillFrames = 50;
        internal const int PoolFillEachFrame = 20;
    }
}