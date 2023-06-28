using System;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace kibotu
{
    public class KibotuSettings : ScriptableObject
    {
        private const string TrackUrlTemplate = "{0}track";
        private const string EngageUrlTemplate = "{0}engage";
        
        //TODO: Convert to log level
        [Tooltip("If true will print helpful debugging messages")] 
        public bool ShowDebug;
        [Tooltip("If true, you need to manually initialize the library")]
        public bool ManualInitialization;
        [Tooltip("The api host of where to send the requests to. Useful when you need to proxy all the request to somewhere else.'")]
        public string APIHostAddress = "https://api.kibotu.ai/";
        [Tooltip("The token of the Kibotu project.")]
        public string RuntimeToken = "";
        [Tooltip("Used when the DEBUG compile flag is set or when in the editor. Useful if you want to use different tokens for test builds.")]
        public string DebugToken = "";
        [Tooltip("Seconds (in realtime) between sending data to the API Host.")]
        public float FlushInterval = 60f;

        internal string Token {
            get {
                #if UNITY_EDITOR || DEBUG
                return DebugToken;
                #else
                return RuntimeToken;
                #endif
            }
        }

        public void ApplyToConfig()
        {
            string host = APIHostAddress.EndsWith("/") ? APIHostAddress : $"{APIHostAddress}/";
            Config.TrackUrl = string.Format(TrackUrlTemplate, host);
            Config.EngageUrl = string.Format(EngageUrlTemplate, host);
            Config.ShowDebug = ShowDebug;
            Config.ManualInitialization = ManualInitialization;
            Config.FlushInterval = FlushInterval;
        }

        #region static
        private static KibotuSettings _instance;

        public static void LoadSettings()
        {
            if (!_instance)
            {
                _instance = FindOrCreateInstance();
                _instance.ApplyToConfig();
            }
        }

        public static KibotuSettings Instance {
            get {
                LoadSettings();
                return _instance;
            }
        }

        private static KibotuSettings FindOrCreateInstance()
        {
            KibotuSettings instance = null;
            instance = instance ? null : Resources.Load<KibotuSettings>("Kibotu");
            instance = instance ? instance : Resources.LoadAll<KibotuSettings>(string.Empty).FirstOrDefault();
            instance = instance ? instance : CreateAndSave<KibotuSettings>();
            if (instance == null) throw new Exception("Could not find or create settings for Kibotu");
            return instance;
        }

        private static T CreateAndSave<T>() where T : ScriptableObject
        {
            T instance = CreateInstance<T>();
#if UNITY_EDITOR
            //Saving during Awake() will crash Unity, delay saving until next editor frame
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += () => SaveAsset(instance);
            }
            else
            {
                SaveAsset(instance);
            }
#endif
            return instance;
        }

#if UNITY_EDITOR
        private static void SaveAsset<T>(T obj) where T : ScriptableObject
        {

            string dirName = "Assets/Resources";
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            AssetDatabase.CreateAsset(obj, "Assets/Resources/Kibotu.asset");
            AssetDatabase.SaveAssets();
        }
#endif
        #endregion
    }
}
