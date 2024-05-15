using System;
using System.Collections.Generic;
using UnityEngine;

namespace kibotu
{
    public static partial class Kibotu
    {
        public static List<Action<string>> _onLogListeners = new List<Action<string>>();
        public static List<Action<string>> _onErrorLogListeners = new List<Action<string>>();

        public static void Log(string s)
        {
            if (Config.ShowDebug)
            {
                Debug.Log("[Kibotu] " + s);
            }

            if (_onLogListeners.Count > 0)
            {
                foreach (var listener in _onLogListeners)
                {
                    try
                    {
                        listener.Invoke(s);
                    }
                    catch (Exception e)
                    {
                        LogError("Error invoking log listener: " + e.Message);
                    }
                }
            }
        }

        public static void LogWarning(string s)
        {
            if (Config.ShowDebug)
            {
                Debug.LogWarning("[Kibotu] " + s);
            }
            
            if (_onLogListeners.Count > 0)
            {
                foreach (var listener in _onLogListeners)
                {
                    try
                    {
                        listener.Invoke(s);
                    }
                    catch (Exception e)
                    {
                        LogError("Error invoking warning (log) listener: " + e.Message);
                    }
                }
            }
        }

        public static void LogError(string s)
        {
            if (Config.ShowDebug)
            {
                Debug.LogError("[Kibotu] " + s);
            }
            
            if (_onErrorLogListeners.Count > 0)
            {
                foreach (var listener in _onErrorLogListeners)
                {
                    try
                    {
                        listener.Invoke(s);
                    }
                    catch (Exception e)
                    {
                        LogError("Error invoking error listener: " + e.Message);
                    }
                }
            }
        }
    }
}