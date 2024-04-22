namespace kibotu
{
    internal static class Config
    {
        internal static string TrackUrl = "https://api.kibotu.ai/track";
        internal static string EngageUrl = "https://api.kibotu.ai/engage";
        internal static string GetBannerUrl = "https://api.kibotu.ai/ab/getPersonalizedBanner";
        internal static string GetQuestsEligibleUrl = "";
        internal static string GetQuestsFinalizedUrl = "";
        internal static string GetQuestsActiveUrl = "";
        internal static string GetQuestActionPrefUrl = "";

        internal static string GetQuestStartUrl = "";
        internal static string GetQuestProgressUrl = "";
        internal static string GetQuestFinishUrl = "";
        internal static string GetQuestFinalizeUrl = "";
    
        internal static bool ShowDebug = false;
        internal static bool ManualInitialization = true;
        internal static float FlushInterval = 60f;

        internal static int BatchSize = 50;

        internal const int PoolFillFrames = 50;
        internal const int PoolFillEachFrame = 20;
    }
}