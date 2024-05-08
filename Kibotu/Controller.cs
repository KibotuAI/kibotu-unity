using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using System.Threading;
using Unity.Jobs;
using Unity.Collections;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Newtonsoft.Json;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace kibotu
{
    internal class Controller : MonoBehaviour
    {
        private static Value _autoTrackProperties;
        private static Value _autoEngageProperties;
        private static int _retryCount = 0;
        private static DateTime _retryTime;

        #region Singleton

        private static Controller _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            KibotuSettings.LoadSettings();
            if (Config.ManualInitialization) return;
            Initialize();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            if (Config.ManualInitialization) return;
            GetEngageDefaultProperties();
            GetEventsDefaultProperties();
        }

        internal static void Initialize()
        {
            // Copy over any runtime changes that happened before initialization from settings instance to the config.
            KibotuSettings.Instance.ApplyToConfig();
            GetInstance();
        }

        internal static bool IsInitialized()
        {
            return _instance != null;
        }

        internal static void Disable()
        {
            if (_instance != null)
            {
                Destroy(_instance);
            }
        }

        internal static Controller GetInstance()
        {
            if (_instance == null)
            {
                GameObject g = new GameObject("Kibotu");
                _instance = g.AddComponent<Controller>();
                DontDestroyOnLoad(g);
            }

            return _instance;
        }

        #endregion

        void OnDestroy()
        {
            Kibotu.Log($"Kibotu Component Destroyed");
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                Metadata.InitSession();
            }
        }

        private void Start()
        {
            MigrateFrom1To2();
            KibotuTracking();
            CheckForKibotuImplemented();
            Kibotu.Log($"Kibotu Component Started");
            StartCoroutine(WaitAndFlush());
        }

        [CanBeNull] public string LastShownProgressKey = null;
        public bool SyncedQuests;
        [CanBeNull] public string PlayerId;
        [CanBeNull] public Dictionary<string, object> UserPropsOnInit;
        [CanBeNull] public KibotuQuest ActiveQuest;
        public List<KibotuQuest> EligibleQuests;

        private void DoQuestStart(string questId, string eventName, Dictionary<string, object> eventProperties)
        {
            StartCoroutine(StartQuestRequest(questId, eventName, eventProperties,
                (activeQuest) => { Kibotu.Log("Quest started on backend"); }));
        }

        private void DoQuestProgress(string questId, string eventName, Dictionary<string, object> eventProperties, Action<KibotuQuest> callback)
        {
            StartCoroutine(ProgressQuestRequest(questId, eventName, eventProperties,
                (activeQuest) =>
                {
                    Kibotu.Log("Quest triggered on backend");
                    callback(activeQuest);
                }));
        }

        private void DoQuestFinish(string questId, string eventName, Dictionary<string, object> eventProperties)
        {
            StartCoroutine(FinishQuestRequest(questId, eventName, eventProperties,
                (activeQuest) => { Kibotu.Log("Quest finished on backend"); }));
        }

        private void DoQuestFinalize(string questId)
        {
            StartCoroutine(FinalizeQuestRequest(questId, (activeQuest) => { Kibotu.Log("Quest finished on backend"); }));
        }

        private void DoInitQuests(Dictionary<string, object> requestData)
        {
            UserPropsOnInit = requestData;
            if (requestData.TryGetValue("playerId", out var playerId))
            {
                PlayerId = playerId.ToString();
            }

            if (SyncedQuests == true)
            {
                Kibotu.Log("InitQuests skip processing - already synced");
                return;
            }
            
            StartCoroutine(InitQuestsRequest(requestData, (activeQuest, quests, finalizedQuestIds) =>
            {
                try {

                    if (activeQuest != null && !String.IsNullOrEmpty(activeQuest.Id))
                    {
                        Kibotu.Log("Quests initialization: Active quest found: " + activeQuest.Id);
                        ActiveQuest = activeQuest;
                    }
                }
                catch (Exception ex)
                {
                    Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                }                
                // filter out all finalizedQuestIds from quests by object id
                EligibleQuests = quests.List.Where(x => !finalizedQuestIds.List.Contains(x.Id)).ToList();
                SyncedQuests = true;

                Kibotu.Log("Quests initialized; 00 Total quests: " + quests.List.Count + ", Eligible quests: " +
                           EligibleQuests.Count);

                var strEligibleQuests = "";
                try {
                    foreach (var q in EligibleQuests)
                    {
                        strEligibleQuests += q.Id + "; ";
                        strEligibleQuests += q.Title + "; ";
                        strEligibleQuests += q.from + "; ";
                        strEligibleQuests += q.to + "; ";
                        try
                        {
                            strEligibleQuests += "Triggers.State.Welcome: ";
                            foreach (var ev in q.Triggers.State.Welcome)
                            {
                                strEligibleQuests += "eventName: " + ev + "; ";
                            }
                        }
                        catch (Exception ex)
                        {
                            Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                        }
                        
                        try {
                            strEligibleQuests += "Milestones: ";
                            foreach (var m in q.Milestones) {
                                strEligibleQuests += "order: " + m.Order + "; prize: " + m.PrizeTitle + " - " + m.PrizeSku + "   ";
                            }
                        }
                        catch (Exception ex)
                        {
                            Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                        }
                        
                        try {
                            strEligibleQuests += "Graphics.Welcome.Background: " + q.Graphics.Welcome.Background + "; ";
                            strEligibleQuests += "Graphics.Welcome.TitleImage: " + q.Graphics.Progress.TitleImage + "; ";
                        }
                        catch (Exception ex)
                        {
                            Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                        }
                        
                        try 
                        {
                            if (q.Progress != null) {
                                strEligibleQuests += "Progress.CurrentState: " + q.Progress.CurrentState;
                            } else {
                                strEligibleQuests += "No Progress";
                            }
                        }
                        catch (Exception ex)
                        {
                            Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                        }
                        try {
                            foreach (var fi in q.TargetFilter)
                            {
                                strEligibleQuests += "TargetFilter: " + fi.Key + ":" + fi.Value + ";";
                            }
                        }
                        catch (Exception ex)
                        {
                            Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Kibotu.LogError("Error: " + ex.Message + "; StackTrace: " + ex.StackTrace);
                }
                
                
                Kibotu.Log("Quests initialized; Total quests: " + quests.List.Count + ", Eligible quests: " +
                           EligibleQuests.Count);
                Kibotu.Log("Quests initialized; EligibleQuests:" + strEligibleQuests);
            }));
        }

        private void DoGetPersonal(Dictionary<string, object> requestData, Action<Asset> callback)
        {
            StartCoroutine(GetPersonalRequest(requestData, callback));
        }

        // General method for performing the POST request
        private IEnumerator PerformPostRequest<TRequest, TResponse>(string url, TRequest requestData,
            Action<TResponse> callback)
        {
            string jsonRequest =
                JsonConvert.SerializeObject(
                    requestData); //System.Text.Json;JsonSerializer.Serialize .ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", KibotuSettings.Instance.Token);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Handle error
                    Kibotu.LogError("Error: " + request.error);
                    callback(default);
                }
                else
                {
                    // Handle successful response
                    string jsonResponse = request.downloadHandler.text;
                    
                    Kibotu.Log("DBG Raw Response: " + jsonResponse);
                    
                    // TResponse response = JsonUtility.FromJson<TResponse>(jsonResponse);
                    TResponse response = JsonConvert.DeserializeObject<TResponse>(jsonResponse);
                    callback(response);
                }
            }
        }

        private IEnumerator StartQuestRequest(
            string questId, string eventName, Dictionary<string, object> eventProperties,
            Action<KibotuQuest> callback)
        {
            var url = Config.GetQuestActionPrefUrl + "/" + questId + "/start";
            var requestData = new Dictionary<string, object>
                { { "playerId", PlayerId }, { "eventName", eventName }, { "eventProperties", eventProperties } };
            yield return PerformPostRequest(url, requestData, callback);
        }

        private IEnumerator ProgressQuestRequest(
            string questId, string eventName, Dictionary<string, object> eventProperties,
            Action<KibotuQuest> callback)
        {
            var url = Config.GetQuestActionPrefUrl + "/" + questId + "/trigger";
            var requestData = new Dictionary<string, object>
                { { "playerId", PlayerId }, { "eventName", eventName }, { "eventProperties", eventProperties } };
            yield return PerformPostRequest(url, requestData, callback);
        }

        private IEnumerator FinishQuestRequest(
            string questId, string eventName, Dictionary<string, object> eventProperties,
            Action<KibotuQuest> callback)
        {
            var url = Config.GetQuestActionPrefUrl + "/" + questId + "/finish";
            var requestData = new Dictionary<string, object>
                { { "playerId", PlayerId }, { "eventName", eventName }, { "eventProperties", eventProperties } };
            yield return PerformPostRequest(url, requestData, callback);
        }

        private IEnumerator FinalizeQuestRequest(
            string questId,
            Action<KibotuQuest> callback)
        {
            var url = Config.GetQuestActionPrefUrl + "/" + questId + "/finalize";
            var requestData = new Dictionary<string, object> { { "playerId", PlayerId } };
            yield return PerformPostRequest(url, requestData, callback);
        }

        private IEnumerator InitQuestsRequest(Dictionary<string, object> requestData,
            Action<KibotuQuest, KibotuListResult<KibotuQuest>, KibotuListResult<string>> callback)
        {
            KibotuQuest response1 = null;
            KibotuListResult<KibotuQuest> response2 = null;
            KibotuListResult<string> response3 = null;
            
            // Start the first request
            yield return StartCoroutine(PerformPostRequest(Config.GetQuestsActiveUrl, requestData,
                (KibotuQuest response) => { response1 = response; }));
            yield return StartCoroutine(PerformPostRequest(Config.GetQuestsEligibleUrl, requestData,
                (KibotuListResult<KibotuQuest> response) => { response2 = response; }));
            yield return StartCoroutine(PerformPostRequest(Config.GetQuestsFinalizedUrl, requestData,
                (KibotuListResult<string> response) => { response3 = response; }));
            
            // Wait until both requests are completed
            yield return new WaitUntil(() => response1 != null && response2 != null && response3 != null);

            callback(response1, response2, response3);
        }

        private IEnumerator GetPersonalRequest(Dictionary<string, object> requestData, Action<Asset> callback)
        {
            yield return PerformPostRequest(Config.GetBannerUrl, requestData, callback);
        }

        private void KibotuTracking()
        {
            if (!KibotuStorage.HasIntegratedLibrary)
            {
                StartCoroutine(SendHttpEvent("Integration", "85053bf24bba75239b16a601d9387e17",
                    KibotuSettings.Instance.Token, "", false));
                KibotuStorage.HasIntegratedLibrary = true;
            }

            if (Debug.isDebugBuild)
            {
                StartCoroutine(SendHttpEvent("SDK Debug Launch", "metrics-1", KibotuSettings.Instance.Token,
                    $",\"Debug Launch Count\":{KibotuStorage.MPDebugInitCount}", true));
            }
        }

        private void CheckForKibotuImplemented()
        {
            if (KibotuStorage.HasImplemented)
            {
                return;
            }

            int implementedScore = 0;
            implementedScore += KibotuStorage.HasTracked ? 1 : 0;
            implementedScore += KibotuStorage.HasIdendified ? 1 : 0;
            implementedScore += KibotuStorage.HasAliased ? 1 : 0;
            implementedScore += KibotuStorage.HasUsedPeople ? 1 : 0;

            if (implementedScore >= 3)
            {
                KibotuStorage.HasImplemented = true;

                StartCoroutine(SendHttpEvent("SDK Implemented", "metrics-1", KibotuSettings.Instance.Token,
                    $",\"Tracked\":{KibotuStorage.HasTracked.ToString().ToLower()}" +
                    $",\"Identified\":{KibotuStorage.HasIdendified.ToString().ToLower()}" +
                    $",\"Aliased\":{KibotuStorage.HasAliased.ToString().ToLower()}" +
                    $",\"Used People\":{KibotuStorage.HasUsedPeople.ToString().ToLower()}",
                    true
                ));
            }
        }

        private IEnumerator WaitAndFlush()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Config.FlushInterval);
                DoFlush();
            }
        }

        internal void DoFlush()
        {
            StartCoroutine(SendData(KibotuStorage.FlushType.EVENTS));
            StartCoroutine(SendData(KibotuStorage.FlushType.PEOPLE));
        }

        private IEnumerator SendData(KibotuStorage.FlushType flushType)
        {
            if (_retryTime > DateTime.Now && _retryCount > 0)
            {
                yield break;
            }

            string url = (flushType == KibotuStorage.FlushType.EVENTS) ? Config.TrackUrl : Config.EngageUrl;
            Value batch = KibotuStorage.DequeueBatchTrackingData(flushType, Config.BatchSize);
            while (batch.Count > 0)
            {
                String payload = batch.ToString();
                Kibotu.Log("Sending batch of data: " + payload);
                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    var bytes = Encoding.UTF8.GetBytes(payload);
                    request.uploadHandler = new UploadHandlerRaw(bytes);
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", KibotuSettings.Instance.Token);
                    yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                    if (request.result != UnityWebRequest.Result.Success)
#else
                    if (request.isHttpError || request.isNetworkError)
#endif
                    {
                        Kibotu.Log("API request to " + url + "has failed with reason " + request.error);
                        _retryCount += 1;
                        double retryIn = Math.Pow(2, _retryCount - 1) * 60;
                        retryIn = Math.Min(retryIn, 10 * 60); // limit 10 min
                        _retryTime = DateTime.Now;
                        _retryTime = _retryTime.AddSeconds(retryIn);
                        Kibotu.Log("Retrying request in " + retryIn + " seconds (retryCount=" + _retryCount + ")");
                        yield break;
                    }
                    else
                    {
                        _retryCount = 0;
                        KibotuStorage.DeleteBatchTrackingData(batch);
                        batch = KibotuStorage.DequeueBatchTrackingData(flushType, Config.BatchSize);
                        Kibotu.Log("Successfully posted to " + url);
                    }
                }
            }
        }

        private IEnumerator SendHttpEvent(string eventName, string apiToken, string distinctId, string properties,
            bool updatePeople)
        {
            // TODO for when we have more customers
            yield break;

            string json = "{\"event\":\"" + eventName + "\",\"properties\":{\"token\":\"" +
                          apiToken + "\",\"DevX\":true,\"mp_lib\":\"unity\"," +
                          "\"$lib_version\":\"" + Kibotu.KibotuUnityVersion + "\"," +
                          "\"Project Token\":\"" + distinctId + "\",\"distinct_id\":\"" + distinctId + "\"" +
                          properties + "}}";

            using (UnityWebRequest request = new UnityWebRequest(Config.TrackUrl, UnityWebRequest.kHttpVerbPOST))
            {
                if (!string.IsNullOrEmpty(json))
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    request.uploadHandler = new UploadHandlerRaw(bytes);
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", distinctId);
                    yield return request.SendWebRequest();
                }
            }

            if (updatePeople)
            {
                json = "{\"$add\":" + "{\"" + eventName +
                       "\":1}," +
                       "\"$token\":\"" + apiToken + "\"," +
                       "\"$distinct_id\":\"" + distinctId + "\"}";

                using (UnityWebRequest request = new UnityWebRequest(Config.EngageUrl, UnityWebRequest.kHttpVerbPOST))
                {
                    if (!string.IsNullOrEmpty(json))
                    {
                        var bytes = Encoding.UTF8.GetBytes(json);
                        request.uploadHandler = new UploadHandlerRaw(bytes);
                        request.SetRequestHeader("Content-Type", "application/json");
                        request.SetRequestHeader("Authorization", distinctId);
                        yield return request.SendWebRequest();
                    }
                }
            }
        }

        #region InternalSDK

        private void MigrateFrom1To2()
        {
            if (!KibotuStorage.HasMigratedFrom1To2)
            {
                string stateFile = Application.persistentDataPath + "/mp_state.json";
                try
                {
                    if (System.IO.File.Exists(stateFile))
                    {
                        string state = System.IO.File.ReadAllText(stateFile);
                        Value stateValue = Value.Deserialize(state);
                        string distinctIdKey = "distinct_id";
                        if (stateValue.ContainsKey(distinctIdKey) && !stateValue[distinctIdKey].IsNull)
                        {
                            string distinctId = stateValue[distinctIdKey];
                            KibotuStorage.DistinctId = distinctId;
                        }

                        string optedOutKey = "opted_out";
                        if (stateValue.ContainsKey(optedOutKey) && !stateValue[optedOutKey].IsNull)
                        {
                            bool optedOut = stateValue[optedOutKey];
                            KibotuStorage.IsTracking = !optedOut;
                        }

                        string trackedIntegrationKey = "tracked_integration";
                        if (stateValue.ContainsKey(trackedIntegrationKey) && !stateValue[trackedIntegrationKey].IsNull)
                        {
                            bool trackedIntegration = stateValue[trackedIntegrationKey];
                            KibotuStorage.HasIntegratedLibrary = trackedIntegration;
                        }
                    }
                }
                catch (Exception)
                {
                    Kibotu.LogError("Error migrating state from v1 to v2");
                }
                finally
                {
                    System.IO.File.Delete(stateFile);
                }

                string superPropertiesFile = Application.persistentDataPath + "/mp_super_properties.json";
                try
                {
                    if (System.IO.File.Exists(superPropertiesFile))
                    {
                        string superProperties = System.IO.File.ReadAllText(superPropertiesFile);
                        Value superPropertiesValue = Value.Deserialize(superProperties);
                        foreach (KeyValuePair<string, Value> kvp in superPropertiesValue)
                        {
                            if (!kvp.Key.StartsWith("$"))
                            {
                                Kibotu.Register(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Kibotu.LogError("Error migrating super properties from v1 to v2");
                }
                finally
                {
                    System.IO.File.Delete(superPropertiesFile);
                }

                KibotuStorage.HasMigratedFrom1To2 = true;
            }
        }

        internal static Value GetEngageDefaultProperties()
        {
            if (_autoEngageProperties == null)
            {
                Value properties = new Value();
#if UNITY_IOS
                        properties["_kb_ios_lib_version"] = Kibotu.KibotuUnityVersion;
                        properties["_kb_ios_version"] = Device.systemVersion;
                        properties["_kb_ios_app_release"] = Application.version;
                        properties["_kb_ios_device_model"] = SystemInfo.deviceModel;
#elif UNITY_ANDROID
                properties["_kb_android_lib_version"] = Kibotu.KibotuUnityVersion;
                properties["_kb_android_os"] = "Android";
                properties["_kb_android_os_version"] = SystemInfo.operatingSystem;
                properties["_kb_android_model"] = SystemInfo.deviceModel;
                properties["_kb_android_app_version"] = Application.version;
#else
                        properties["_kb_lib_version"] = Kibotu.KibotuUnityVersion;
#endif
                _autoEngageProperties = properties;
            }

            return _autoEngageProperties;
        }

        [CanBeNull]
        internal static void InitQuests(Dictionary<string, object> properties)
        {
            Kibotu.Log("Kibotu InitQuests 0.15");
            
            var strUserProps = "";
            foreach (var pair in properties)
            {
                strUserProps += pair.Key + " = " + pair.Value + "; ";
            }
            Kibotu.Log("InitQuests userProps: " + strUserProps);
            
            GetInstance().DoInitQuests(properties);
        }

        internal static void QuestStart(string questId, string eventName, Dictionary<string, object> eventProperties)
        {
            GetInstance().DoQuestStart(questId, eventName, eventProperties);
        }

        internal static void QuestProgress(string questId, string eventName, Dictionary<string, object> eventProperties, Action<KibotuQuest> callback)
        {
            GetInstance().DoQuestProgress(questId, eventName, eventProperties, callback);
        }
        
        internal static void QuestFinish(string questId, string eventName, Dictionary<string, object> eventProperties)
        {
            GetInstance().DoQuestFinish(questId, eventName, eventProperties);
        }
        
        internal static void QuestFinalize(string questId)
        {
            GetInstance().DoQuestFinalize(questId);
            
            // Remove immediatelly from eligible quests list
            GetInstance().ActiveQuest = null; 
            GetInstance().EligibleQuests = GetInstance().EligibleQuests.Where(x => x.Id != questId).ToList();
        }

        [CanBeNull]
        internal static void GetPersonalizedBanner(Dictionary<string, object> properties, Action<Asset> callback)
        {
            GetInstance().DoGetPersonal(properties, callback);
        }

        private static Value GetEventsDefaultProperties()
        {
            if (_autoTrackProperties == null)
            {
                Value properties = new Value
                {
                    { "_kb_lib", "unity" },
                    { "_kb_lib_version", Kibotu.KibotuUnityVersion },
                    { "_kb_os", SystemInfo.operatingSystemFamily.ToString() },
                    { "_kb_os_version", SystemInfo.operatingSystem },
                    { "_kb_model", SystemInfo.deviceModel },
                    { "_kb_app_version_string", Application.version },
                    {
                        "_kb_wifi", Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork
                    },
                    { "_kb_radio", Util.GetRadio() },
                    { "_kb_device", Application.platform.ToString() },
                    { "_kb_screen_dpi", Screen.dpi },
                };
#if UNITY_IOS
                    properties["_kb_os"] = "Apple";
                    properties["_kb_os_version"] = Device.systemVersion;
                    properties["_kb_manufacturer"] = "Apple";
#endif
#if UNITY_ANDROID
                properties["_kb_os"] = "Android";
#endif
                _autoTrackProperties = properties;
            }

            return _autoTrackProperties;
        }

        // TODO get this config from Kibotu server per game
        private static Dictionary<string, string> _predictionTriggeringEvents = new()
        {
            { "MatchFinished", "" }
        };

        internal static void DoTrack(string eventName, Value properties)
        {
            if (!KibotuStorage.IsTracking) return;
            if (properties == null) properties = new Value();
            properties.Merge(GetEventsDefaultProperties());
            // These auto properties can change in runtime so we don't bake them into AutoProperties
            properties["_kb_screen_width"] = Screen.width;
            properties["_kb_screen_height"] = Screen.height;
            properties.Merge(KibotuStorage.OnceProperties);
            KibotuStorage.ResetOnceProperties();
            properties.Merge(KibotuStorage.SuperProperties);
            Value startTime;
            if (KibotuStorage.TimedEvents.TryGetValue(eventName, out startTime))
            {
                properties["_kb_duration"] = Util.CurrentTimeInSeconds() - (double)startTime;
                KibotuStorage.TimedEvents.Remove(eventName);
            }

            // properties["token"] = KibotuSettings.Instance.Token;
            properties["distinct_id"] = KibotuStorage.DistinctId;
            properties["time"] = Util.CurrentTimeInMilliseconds();

            Value data = new Value();

            data["event"] = eventName;
            data["properties"] = properties;
            data["distinct_id"] = KibotuStorage.DistinctId;
            data["_kb_metadata"] = Metadata.GetEventMetadata();

            if (Debug.isDebugBuild && !eventName.StartsWith("$"))
            {
                KibotuStorage.HasTracked = true;
            }

            var isPredictionTriggeringEvent = _predictionTriggeringEvents.ContainsKey(eventName);
            if (isPredictionTriggeringEvent)
            {
                var predictId = Guid.NewGuid().ToString();
                data["predictId"] = predictId;
                _predictionTriggeringEvents[eventName] = predictId;
            }

            KibotuStorage.EnqueueTrackingData(data, KibotuStorage.FlushType.EVENTS);

            if (isPredictionTriggeringEvent)
            {
                GetInstance().DoFlush();
            }
        }

        internal static string ConsumePredictIdOfEvent(string eventName)
        {
            if (!_predictionTriggeringEvents.TryGetValue(eventName, out var predictId)) return "";

            _predictionTriggeringEvents[eventName] = "";
            return predictId;
        }

        internal static void DoEngage(Value properties)
        {
            if (!KibotuStorage.IsTracking) return;
            // properties["$token"] = KibotuSettings.Instance.Token;
            properties["distinct_id"] = KibotuStorage.DistinctId;
            properties["time"] = Util.CurrentTimeInMilliseconds();
            properties["_kb_metadata"] = Metadata.GetPeopleMetadata();

            KibotuStorage.EnqueueTrackingData(properties, KibotuStorage.FlushType.PEOPLE);
            if (Debug.isDebugBuild)
            {
                KibotuStorage.HasUsedPeople = true;
            }
        }

        internal static void DoClear()
        {
            KibotuStorage.DeleteAllTrackingData(KibotuStorage.FlushType.EVENTS);
            KibotuStorage.DeleteAllTrackingData(KibotuStorage.FlushType.PEOPLE);
        }

        #endregion

        internal static class Metadata
        {
            private static Int32 _eventCounter = 0, _peopleCounter = 0, _sessionStartEpoch;
            private static String _sessionID;
            private static System.Random _random = new System.Random(Guid.NewGuid().GetHashCode());

            internal static void InitSession()
            {
                _eventCounter = 0;
                _peopleCounter = 0;
                _sessionID = Convert.ToString(_random.Next(0, Int32.MaxValue), 16);
                _sessionStartEpoch = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }

            internal static Value GetEventMetadata()
            {
                Value eventMetadata = new Value
                {
                    { "_kb_event_id", Convert.ToString(_random.Next(0, Int32.MaxValue), 16) },
                    { "_kb_session_id", _sessionID },
                    { "_kb_session_seq_id", _eventCounter },
                    { "_kb_session_start_sec", _sessionStartEpoch }
                };
                _eventCounter++;
                return eventMetadata;
            }

            internal static Value GetPeopleMetadata()
            {
                Value peopleMetadata = new Value
                {
                    { "_kb_event_id", Convert.ToString(_random.Next(0, Int32.MaxValue), 16) },
                    { "_kb_session_id", _sessionID },
                    { "_kb_session_seq_id", _peopleCounter },
                    { "_kb_session_start_sec", _sessionStartEpoch }
                };
                _peopleCounter++;
                return peopleMetadata;
            }
        }
    }
}