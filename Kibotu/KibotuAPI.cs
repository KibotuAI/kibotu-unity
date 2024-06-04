using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace kibotu
{
    /// <summary>
    /// Core class for interacting with %Kibotu Analytics.
    /// </summary>
    /// <description>
    /// <p>Open unity project settings and set the properties in the unity inspector (token, debug token, etc.)</p>
    /// <p>Once you have the Kibotu settings setup, you can track events by using <c>Kibotu.Track(string eventName)</c>.
    /// You can also update %People Analytics records with Kibotu.people. </p>
    /// </description>
    /// <code>
    ///        //Track an event in Kibotu Engagement<br/>
    ///        Kibotu.Track("Hello World");<br/>
    ///        Kibotu.Identify("CURRENT USER DISTINCT ID");<br/>
    ///        Kibotu.People.Set("Plan", "Premium");<br/>
    /// </code>
    public static partial class Kibotu
    {
        internal const string KibotuUnityVersion = "1.0.24";

        /// <summary>
        /// Creates an Kibotu instance. Use only if you have enabled "Manual Initialization" from your Project Settings.
        /// Do not forget to call Disable() when you want to dispose your object.
        /// </summary>
        public static void Init()
        {
            Controller.Initialize();
        }

        /// <summary>
        /// Checks whether Kibotu is initialized or not. If it is not, every API will be no-op.
        /// </summary>
        public static bool IsInitialized()
        {
            bool initialized = Controller.IsInitialized();
            if (!initialized)
            {
                Kibotu.Log("Kibotu is not initialized");
            }

            return initialized;
        }

        /// <summary>
        /// By default, Kibotu uses PlayerPreferences for data persistence. However you can call this method to
        /// set the data persistence of your choice as long as it follows IPeferences
        /// </summary>
        /// <param name="preferences">the new distinct_id that should represent original</param>
        public static void SetPreferencesSource(IPreferences preferences)
        {
            KibotuStorage.SetPreferencesSource(preferences);
        }

        /// <summary>
        /// Creates a distinct_id alias.
        /// </summary>
        /// <param name="alias">the new distinct_id that should represent original</param>
        public static void Alias(string alias)
        {
            if (!IsInitialized()) return;
            if (alias == KibotuStorage.DistinctId) return;
            Value properties = new Value();
            properties["alias"] = alias;
            Track("_kb_create_alias", properties);
            KibotuStorage.HasAliased = true;
            Flush();
        }

        /// <summary>
        /// Clears all current event timers.
        /// </summary>
        public static void ClearTimedEvents()
        {
            if (!IsInitialized()) return;
            KibotuStorage.ResetTimedEvents();
        }

        /// <summary>
        /// Clears the event timer for a single event.
        /// </summary>
        /// <param name="eventName">the name of event to clear event timer</param>
        public static void ClearTimedEvent(string eventName)
        {
            if (!IsInitialized()) return;
            Value properties = KibotuStorage.TimedEvents;
            properties.Remove(eventName);
            KibotuStorage.TimedEvents = properties;
        }

        /// <summary>
        /// Sets the distinct ID of the current user.
        /// </summary>
        /// <param name="uniqueId">a string uniquely identifying this user. Events sent to %Kibotu
        /// using the same distinct_id will be considered associated with the same visitor/customer for
        /// retention and funnel reporting, so be sure that the given value is globally unique for each
        /// individual user you intend to track.
        /// </param>
        public static void Identify(string uniqueId)
        {
            if (!IsInitialized()) return;
            if (KibotuStorage.DistinctId == uniqueId) return;
            string oldDistinctId = KibotuStorage.DistinctId;
            KibotuStorage.DistinctId = uniqueId;
            Track("_kb_identify", "_kb_anon_distinct_id", oldDistinctId);
            KibotuStorage.HasIdendified = true;
        }

        [Obsolete("Please use 'DistinctId' instead!")]
        public static string DistinctID
        {
            get => KibotuStorage.DistinctId;
        }

        public static string DistinctId
        {
            get => KibotuStorage.DistinctId;
        }

        /// <summary>
        /// Opt out tracking.
        /// </summary>
        public static void OptOutTracking()
        {
            if (!IsInitialized()) return;
            People.DeleteUser();
            Flush();
            Reset();
            KibotuStorage.IsTracking = false;
        }

        /// <summary>
        /// Opt in tracking.
        /// </summary>
        public static void OptInTracking()
        {
            if (!IsInitialized()) return;
            KibotuStorage.IsTracking = true;
            Controller.DoTrack("_kb_opt_in", null);
        }

        /// <summary>
        /// Opt in tracking.
        /// </summary>
        /// <param name="distinctId">the distinct id for events. Behind the scenes,
        /// <code>Identify</code> will be called by using this distinct id.</param>
        public static void OptInTracking(string distinctId)
        {
            if (!IsInitialized()) return;
            Identify(distinctId);
            OptInTracking();
        }

        /// <summary>
        /// Registers super properties, overwriting ones that have already been set.
        /// </summary>
        /// <param name="key">name of the property to register</param>
        /// <param name="value">value of the property to register</param>
        public static void Register(string key, Value value)
        {
            if (!IsInitialized()) return;
            Value properties = KibotuStorage.SuperProperties;
            properties[key] = value;
            KibotuStorage.SuperProperties = properties;
        }

        /// <summary>
        /// Registers super properties without overwriting ones that have already been set.
        /// </summary>
        /// <param name="key">name of the property to register</param>
        /// <param name="value">value of the property to register</param>
        public static void RegisterOnce(string key, Value value)
        {
            if (!IsInitialized()) return;
            Value properties = KibotuStorage.OnceProperties;
            properties[key] = value;
            KibotuStorage.OnceProperties = properties;
        }

        /// <summary>
        /// Clears all super properties, once properties, timed events from persistent KibotuStorage.
        /// </summary>
        public static void Reset()
        {
            _onLogListeners.Clear();
            _onErrorLogListeners.Clear();
            if (!IsInitialized()) return;
            Controller.Reset();
            KibotuStorage.DeleteAllTrackingData(KibotuStorage.FlushType.EVENTS);
            KibotuStorage.DeleteAllTrackingData(KibotuStorage.FlushType.PEOPLE);
            KibotuStorage.ResetSuperProperties();
            KibotuStorage.ResetOnceProperties();
            KibotuStorage.ResetTimedEvents();
            Flush();
            KibotuStorage.DistinctId = "";
        }

        /// <summary>
        /// Clears all items from the Track and Engage request queues, anything not already sent to the Kibotu
        /// API will no longer be sent
        /// </summary>
        public static void Clear()
        {
            if (!IsInitialized()) return;
            Controller.DoClear();
        }

        /// <summary>
        /// Clears all super properties
        /// </summary>
        public static void ClearSuperProperties()
        {
            KibotuStorage.ResetSuperProperties();
        }

        /// <summary>
        /// Start timing of an event. Calling Kibotu.StartTimedEvent(string eventName) will not send an event,
        /// but when you eventually call Kibotu.Track(string eventName), your tracked event will be sent with a "$duration" property,
        /// representing the number of seconds between your calls.
        /// </summary>
        /// <param name="eventName">the name of the event to track with timing</param>
        public static void StartTimedEvent(string eventName)
        {
            if (!IsInitialized()) return;
            Value properties = KibotuStorage.TimedEvents;
            properties[eventName] = Util.CurrentTimeInSeconds();
            KibotuStorage.TimedEvents = properties;
        }

        /// <summary>
        /// Begin timing of an event, but only if the event has not already been registered as a timed event.
        /// Useful if you want to know the duration from the point in time the event was first registered.
        /// </summary>
        /// <param name="eventName">the name of the event to track with timing</param>
        public static void StartTimedEventOnce(string eventName)
        {
            if (!IsInitialized()) return;
            if (!KibotuStorage.TimedEvents.ContainsKey(eventName))
            {
                Value properties = KibotuStorage.TimedEvents;
                properties[eventName] = Util.CurrentTimeInSeconds();
                KibotuStorage.TimedEvents = properties;
            }
        }

        /// <summary>
        /// Tracks an event.
        /// </summary>
        /// <param name="eventName">the name of the event to send</param>
        public static void Track(string eventName)
        {
            if (!IsInitialized()) return;
            Controller.DoTrack(eventName, null);
        }

        /// <summary>
        /// Tracks an event with properties of key=value.
        /// </summary>
        /// <param name="eventName">the name of the event to send</param>
        /// <param name="key">A Key value for the data</param>
        /// <param name="value">The value to use for the key</param>
        public static void Track(string eventName, string key, Value value)
        {
            if (!IsInitialized()) return;
            Value properties = new Value();
            properties[key] = value;
            Controller.DoTrack(eventName, properties);
        }

        /// <summary>
        /// Tracks an event with properties.
        /// </summary>
        /// <param name="eventName">the name of the event to send</param>
        /// <param name="properties">A Value containing the key value pairs of the properties
        /// to include in this event. Pass null if no extra properties exist.
        /// </param>
        public static void Track(string eventName, Value properties)
        {
            if (!IsInitialized()) return;
            Controller.DoTrack(eventName, properties);
        }

        public static void SetFakeQuest(KibotuQuest quest)
        {
            Controller.GetInstance().SetFakeQuest(quest);
        }
        
        // Init quests - fetch quests config from the backend
        public static void InitQuests(Dictionary<string, object> properties)
        {
            if (!IsInitialized()) return;
            Controller.InitQuests(properties);
        }

        public static void onQuestRewardAction(string questId)
        {
            Controller.QuestFinalize(questId);
        }

        public static void onClosedWelcomeUI(string questId)
        {
            var activeQuest = Controller.GetInstance().ActiveQuest;
            if (activeQuest != null &&
                activeQuest.Progress != null &&
                activeQuest.Progress.Status == EnumQuestStates.Welcome)
            {
                Controller.GetInstance().ActiveQuest.Progress.Status = EnumQuestStates.Progress;
            }
        }

        public static void onClosedProgressUI(string questId)
        {
            var activeQuest = Controller.GetInstance().ActiveQuest;
            if (activeQuest != null &&
                activeQuest.Progress != null &&
                activeQuest.Progress.Status == EnumQuestStates.Progress)
            {
                Controller.GetInstance().LastShownProgressKey = activeQuest?.Progress?.ProgressKey;
            }
        }

        public static Dictionary<string, object> TriggerQuestState(string eventName)
        {
            return TriggerQuestState(eventName, new Dictionary<string, object>());
        }

        /**
         * Handles triggered event, could be progressing an active quest or starting a new quest.
         * Async method, result of this is bool - false: do nothing, true: show ActiveQuest modal.
         */
        public static Dictionary<string, object> TriggerQuestState(string eventName,
            Dictionary<string, object> eventProperties)
        {
            Kibotu.Log("TriggerQuestState eventName: " + eventName);
            Dictionary<string, object> conditionsObj = new Dictionary<string, object>();

            // Logical sequence:
            // 1. Try activating a new quest
            // 2. Try processing an event for quest state 
            // 2.1. Try progressing the quest
            // 2.2. Try finishing the quest

            if (!IsInitialized())
            {
                Kibotu.Log("TriggerQuestState - !IsInitialized");
                conditionsObj.Add("IsInitialized", "False");
                return conditionsObj;
            }
           
            if (!Controller.GetInstance().SyncedQuests)
            {
                conditionsObj.Add("SyncedQuests", "False");
                Kibotu.Log("TriggerQuestState - no SyncedQuests yet; Skipping action...");
                return conditionsObj;
            }

            var userProps = Controller.GetInstance().UserPropsOnInit;

            var strUserProps = "";
            foreach (var pair in userProps)
            {
                strUserProps += pair.Key + " = " + pair.Value + "; ";
            }

            Kibotu.Log("TriggerQuestState userProps: " + strUserProps);

            conditionsObj.Add("strUserProps", strUserProps);
            var activeQuest = GetActiveQuest();

            // Process event to trigger new quest
            if (activeQuest == null)
            {
                conditionsObj.Add("no activeQuest", "null");
                Kibotu.Log("TriggerQuestState - no active quest found");
                var eligibleQuests = GetEligibleQuestsDefinitions();
                if (eligibleQuests == null)
                {
                    conditionsObj.Add("eligibleQuests", "null");
                    return conditionsObj;
                }
                
                foreach (var quest in eligibleQuests)
                {
                    // Get the first matching - 
                    if (quest.TryTriggersStateStarting(userProps, eventName, 0))
                    {
                        // start new quest
                        Kibotu.Log("TriggerQuestState - start new quest: " + quest.Id);

                        quest.Progress = new KibotuQuestProgress();
                        quest.Progress.Status = EnumQuestStates.Welcome;
                        quest.Progress.CurrentStep = 0;

                        Controller.GetInstance().ActiveQuest = quest;
                        activeQuest = quest;

                        conditionsObj.Add("Starting quest",
                            quest.Id + "; eventName: " + eventName + "; userProps: " + strUserProps);
                        Controller.QuestStart(quest.Id, eventName, eventProperties);
                        break;
                    }
                    else
                    {
                        // not starting this quest
                        Kibotu.Log("TriggerQuestState - not starting this quest: " + quest.Id + "; eventName: " +
                                   eventName + "; userProps: " + strUserProps);
                        conditionsObj.Add("Not starting quest"+ quest.Id,
                            "; eventName: " + eventName +
                            "; userProps: " + strUserProps);
                    }
                }
            }

            // activeQuest might be set in the previous block 
            if (activeQuest != null)
            {
                Kibotu.Log("TriggerQuestState - active quest found");

                conditionsObj.Add("activeQuest", activeQuest.ToString());
                conditionsObj.Add("DateTime.Now", DateTime.Now.ToString());

                // Process event for active event
                if (activeQuest.TryTriggersStateProgressing(userProps, eventName, 0))
                {
                    if ((activeQuest.Progress.Status == EnumQuestStates.Welcome ||
                         activeQuest.Progress.Status == EnumQuestStates.Progress) &&
                        activeQuest.to < DateTime.Now)
                    {
                        conditionsObj.Add("Finishing quest - activeQuest.Progress.Status", activeQuest.Progress.Status.ToString());

                        DoFinishQuest(eventName, eventProperties, activeQuest);
                    }
                    else
                    {
                        conditionsObj.Add("Progressing quest - activeQuest.Progress.Status", activeQuest.Progress.Status.ToString());

                        // Progressing the quest
                        activeQuest.Progress.Status = EnumQuestStates.Progress;
                        if (activeQuest.Progress != null)
                        {
                            activeQuest.Progress.CurrentStep++;
                        	conditionsObj.Add("Progressing quest - activeQuest.Progress.CurrentStep", activeQuest.Progress.CurrentStep);
                        } else {
                        	conditionsObj.Add("Not progressing quest; aq: ", activeQuest.ToString());
						}

                        Controller.QuestProgress(activeQuest.Id, eventName, eventProperties,
                            (cbquest) =>
                            {
                                // TODO Compare cbquest.newValue with CurrentStep;

                                if (activeQuest?.Progress.CurrentStep >=
                                    activeQuest.Milestones[activeQuest.Milestones.Length - 1].Goal)
                                {
		                        	conditionsObj.Add("Progress finish last goall qId: ", activeQuest.Id);

                                    activeQuest.Progress.Status = EnumQuestStates.Won;
                                    Controller.QuestFinish(activeQuest.Id, eventName, eventProperties);
                                } else {
									conditionsObj.Add("Progress didnt finish last goal", "");
								}
                            });
                    }
                } else {
	                conditionsObj.Add("TryTriggersStateProgressing else", "");
				}

                if (activeQuest.TryTriggersStateFinishing(userProps, eventName, 0))
                {
                    conditionsObj.Add("TryTriggersStateFinishing Finishing quest - activeQuest.Progress.Status", activeQuest.Progress.Status.ToString());
                    DoFinishQuest(eventName, eventProperties, activeQuest);
                }
                else
                {
                    conditionsObj.Add("TryTriggersStateFinishing else", "");
                }
            }

            return conditionsObj;
        }

        private static void DoFinishQuest(string eventName, Dictionary<string, object> eventProperties, KibotuQuest activeQuest)
        {
            // Time's up - won't count the new event
            if (activeQuest.Progress.CurrentStep < activeQuest.Milestones[0].Goal)
            {
                activeQuest.Progress.Status = EnumQuestStates.Lost;
            }
            else
            {
                activeQuest.Progress.Status = EnumQuestStates.Won;
            }

            Controller.QuestFinish(activeQuest.Id, eventName, eventProperties);
        }

        public static bool TriggerQuestUI(string eventName, Dictionary<string, object> eventProperties)
        {
            Kibotu.Log("TriggerQuestUI eventName: " + eventName);

            if (!IsInitialized()) return false;
            if (!Controller.GetInstance().SyncedQuests)
            {
                Kibotu.Log("TriggerQuestUI skip processing - quests are not synced");
                return false;
            }

            var userProps = Controller.GetInstance().UserPropsOnInit;

            // Show only if got active quest
            var activeQuest = GetActiveQuest();

            if (activeQuest != null)
            {
                var strUserProps = "";
                foreach (var pair in userProps)
                {
                    strUserProps += pair.Key + " = " + pair.Value + "; ";
                }

                Kibotu.Log(
                    $"TriggerQuestUI - active quest found  questId: {activeQuest.Id}; current status: {activeQuest.Progress?.Status}; userProps: {strUserProps}");

                // Process event for active event
                if (activeQuest.TryTriggersUI(userProps, eventName, 0))
                {
                    Kibotu.Log("TryTriggersUI - true, should show modal");

                    if (activeQuest?.Progress?.ProgressKey != null && activeQuest?.Progress?.ProgressKey ==
                        Controller.GetInstance().LastShownProgressKey)
                    {
                        Kibotu.Log("TryTriggersUI - false; Suppressing the same exact state; ProgressKey: " +
                                   activeQuest?.Progress?.ProgressKey);
                        return false;
                    }

                    // Controller.GetInstance().LastShownProgressKey = activeQuest?.Progress?.ProgressKey;
                    return true;
                }
            }

            return false;
        }

        public static KibotuQuest GetActiveQuest()
        {
            if (!IsInitialized()) return null;
            if (!Controller.GetInstance().SyncedQuests)
            {
                Kibotu.Log("GetActiveQuest - quests are not synced");
                return null;
            }

            var activeQuest = Controller.GetInstance().ActiveQuest;
            return activeQuest;
        }

        public static List<KibotuQuest> GetEligibleQuestsDefinitions()
        {
            if (!IsInitialized()) return null;
            if (!Controller.GetInstance().SyncedQuests) return null;
            return Controller.GetInstance().EligibleQuests;
        }

        /**
         * To be invoked when user got the finish state and collected the prize.
         */
        private static bool FinalizeQuest(KibotuQuest quest)
        {
            if (!IsInitialized()) return false;
            Controller.QuestFinalize(quest.Id);
            return false;
        }

        public static void SubscribeToQuestProgressEvent(Action<KibotuEvent> callback)
        {
            Controller.GetInstance().SubscribeToQuestProgressEvent(callback);
        }

        public static void SubscribeToLogs(Action<string> cb)
        {
            _onLogListeners.Clear(); // For now holding only one    
            Kibotu._onLogListeners.Add(cb);
        }

        public static void SubscribeToErrors(Action<string> cb)
        {
            Kibotu._onErrorLogListeners.Clear(); // For now holding only one
            Kibotu._onErrorLogListeners.Add(cb);
        }

        public static void GetPersonalizedBanner(Dictionary<string, object> value, Action<Asset> callback)
        {
            var vvalue = new kibotu.Value();
            if (value != null)
            {
                foreach (var pair in value)
                {
                    vvalue[pair.Key] = pair.Value.ToString();
                }
            }

            value["userId"] = DistinctId;
            value["prizeKey"] = "CurrencyPremium_CurrencySoft";
            value["offerId"] = "8622d2-c8c8-4c52-851c-d71af05";
            value["activeChamp"] = "Opac";
            value["debugForceImageUrl"] = "myimg";

            if (String.IsNullOrEmpty(DistinctId))
            {
                Kibotu.Log("Not identified");
                return;
            }

            if (!IsInitialized()) return;
            Controller.GetPersonalizedBanner(value, callback);

            return;
        }

        public static string ConsumePredictIdOfEvent(string eventName)
        {
            return !IsInitialized() ? null : Controller.ConsumePredictIdOfEvent(eventName);
        }

        /// <summary>
        /// Removes a single superProperty.
        /// </summary>
        /// <param name="key">name of the property to unregister</param>
        public static void Unregister(string key)
        {
            if (!IsInitialized()) return;
            Value properties = KibotuStorage.SuperProperties;
            properties.Remove(key);
            KibotuStorage.SuperProperties = properties;
        }

        /// <summary>
        /// Flushes the queued data to Kibotu
        /// </summary>
        public static void Flush()
        {
            if (!IsInitialized()) return;
            Controller.GetInstance().DoFlush();
        }

        /// <summary>
        /// Sets the project token to be used. This setting will override what it is set in the Unity Project Settings.
        /// </summary>
        public static void SetToken(string token)
        {
            if (!IsInitialized()) return;
//             KibotuSettings.Instance.DebugToken = token;
            KibotuSettings.Instance.RuntimeToken = token;
        }

        /// <summary>
        /// Disables Kibotu Component. Useful if you have "Manual Initialization" enabled under your Project Settings.
        /// </summary>
        public static void Disable()
        {
            if (!IsInitialized()) return;
            Controller.Disable();
        }

        public static class PreciseAB
        {
            /// <summary>
            /// Hashes a string to a float between 0 and 1 using the simple Fowler–Noll–Vo algorithm (fnv32a).
            /// </summary>
            /// <param name="value">The string to hash.</param>
            /// <returns>float between 0 and 1, null if an unsupported version.</returns>
            private static double? Hash(string seed, string value, int version)
            {
                // New hashing algorithm.

                if (version == 2)
                {
                    var n = FNV32A(FNV32A(seed + value).ToString());
                    return (n % 10000) / 10000d;
                }

                // Original hashing algorithm (with a bias flaw).

                if (version == 1)
                {
                    var n = FNV32A(value + seed);
                    return (n % 1000) / 1000d;
                }

                return null;
            }

            /// <summary>
            /// Implementation of the Fowler–Noll–Vo algorithm (fnv32a) algorithm.
            /// </summary>
            /// <param name="value">The value to hash.</param>
            /// <returns>The hashed value.</returns>
            private static uint FNV32A(string value)
            {
                uint hash = 0x811c9dc5;
                uint prime = 0x01000193;

                foreach (char c in value.ToCharArray())
                {
                    hash ^= c;
                    hash *= prime;
                }

                return hash;
            }

            /**
             * 50%/50%
             */
            public static bool IsInControlGroup(string experimentId, string attributionKey)
            {
                var assigned = -1;
                var variationHash = Hash(experimentId, attributionKey, 2);
                if (variationHash.Value >= 0 && variationHash.Value < 0.5)
                {
                    assigned = 0;
                }
                else
                {
                    assigned = 1;
                }

                return assigned != 1;
            }
        }

        /// <summary>
        /// Core interface for using %Kibotu %People Analytics features. You can get an instance by calling Kibotu.people
        /// </summary>
        public static class People
        {
            /// <summary>
            /// Append values to list properties.
            /// </summary>
            /// <param name="properties">mapping of list property names to values to append</param>
            public static void Append(Value properties)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value { { "_kb_append", properties } });
            }

            /// <summary>
            /// Appends a value to a list-valued property.
            /// </summary>
            /// <param name="property">the %People Analytics property that should have it's value appended to</param>
            /// <param name="values">the new value that will appear at the end of the property's list</param>
            public static void Append(string property, Value values)
            {
                Append(new Value { { property, values } });
            }

            /// <summary>
            /// Permanently clear the whole transaction history for the identified people profile.
            /// </summary>
            public static void ClearCharges()
            {
                Unset("_kb_transactions");
            }

            /// <summary>
            /// Permanently delete the identified people profile
            /// </summary>
            public static void DeleteUser()
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value { { "_kb_delete", "" } });
            }

            /// <summary>
            /// Change the existing values of multiple %People Analytics properties at once.
            /// </summary>
            /// <param name="properties"> A map of String properties names to Long amounts. Each property associated with a name in the map </param>
            public static void Increment(Value properties)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value { { "_kb_add", properties } });
            }

            /// <summary>
            /// Convenience method for incrementing a single numeric property by the specified amount.
            /// </summary>
            /// <param name="property">property name</param>
            /// <param name="by">amount to increment by</param>
            public static void Increment(string property, Value by)
            {
                Increment(new Value { { property, by } });
            }

            /// <summary>
            /// Set a collection of properties on the identified user all at once.
            /// </summary>
            /// <param name="properties">a JSONObject containing the collection of properties you wish to apply
            /// to the identified user. Each key in the JSONObject will be associated with a property name, and the value
            /// of that key will be assigned to the property.
            /// </param>
            public static void Set(Value properties)
            {
                if (!IsInitialized()) return;
                properties.Merge(Controller.GetEngageDefaultProperties());
                Controller.DoEngage(new Value { { "_kb_set", properties } });
            }

            /// <summary>
            /// Sets a single property with the given name and value for this user.
            /// </summary>
            /// <param name="property">property name</param>
            /// <param name="to">property value</param>
            public static void Set(string property, Value to)
            {
                Set(new Value { { property, to } });
            }

            /// <summary>
            /// Like Kibotu.Set(Value properties), but will not set properties that already exist on a record.
            /// </summary>
            /// <param name="properties">a JSONObject containing the collection of properties you wish to apply to the identified user. Each key in the JSONObject will be associated with a property name, and the value of that key will be assigned to the property.</param>
            public static void SetOnce(Value properties)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value { { "_kb_set_once", properties } });
            }

            /// <summary>
            /// Like Kibotu.Set(string property, Value to), but will not set properties that already exist on a record.
            /// </summary>
            /// <param name="property">property name</param>
            /// <param name="to">property value</param>
            public static void SetOnce(string property, Value to)
            {
                SetOnce(new Value { { property, to } });
            }

            /// <summary>
            /// Track a revenue transaction for the identified people profile.
            /// </summary>
            /// <param name="amount">amount of revenue received</param>
            /// <param name="properties">a JSONObject containing the collection of properties you wish to apply</param>
            public static void TrackCharge(double amount, Value properties)
            {
                properties["_kb_amount"] = amount;
                TrackCharge(properties);
            }

            /// <summary>
            /// Track a revenue transaction for the identified people profile.
            /// </summary>
            /// <param name="amount">amount of revenue received</param>
            public static void TrackCharge(double amount)
            {
                TrackCharge(new Value { { "_kb_amount", amount } });
            }

            /// <summary>
            /// Track a revenue transaction for the identified people profile.
            /// </summary>
            /// <param name="properties">a JSONObject containing the collection of properties you wish to apply</param>
            public static void TrackCharge(Value properties)
            {
                if (!IsInitialized()) return;
                properties["_kb_time"] = Util.CurrentDateTime();
                Controller.DoEngage(new Value { { "_kb_append", new Value { { "_kb_transactions", properties } } } });
            }

            /// <summary>
            /// Adds values to a list-valued property only if they are not already present in the list.
            /// If the property does not currently exist, it will be created with the given list as it's value.
            /// If the property exists and is not list-valued, the union will be ignored.
            /// </summary>
            /// <param name="properties">mapping of list property names to lists to union</param>
            public static void Union(Value properties)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value { { "_kb_union", properties } });
            }

            /// <summary>
            /// Adds values to a list-valued property only if they are not already present in the list.
            /// If the property does not currently exist, it will be created with the given list as it's value.
            /// If the property exists and is not list-valued, the union will be ignored.            /// </summary>
            /// <param name="property">name of the list-valued property to set or modify</param>
            /// <param name="values">an array of values to add to the property value if not already present</param>
            public static void Union(string property, Value values)
            {
                if (!values.IsArray)
                    throw new ArgumentException("Union with values property must be an array", nameof(values));
                Union(new Value { { property, values } });
            }

            /// <summary>
            /// Takes a string property name, and permanently removes the property and their values from a profile.
            /// </summary>
            /// <param name="property">property</param>
            public static void Unset(string property)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value { { "_kb_unset", new string[] { property } } });
            }

            /// <summary>
            /// Sets the email for this user.
            /// </summary>
            public static string Email
            {
                set => Set(new Value { { "_kb_email", value } });
            }

            /// <summary>
            /// Sets the first name for this user.
            /// </summary>
            public static string FirstName
            {
                set => Set(new Value { { "_kb_first_name", value } });
            }

            /// <summary>
            /// Sets the last name for this user.
            /// </summary>
            public static string LastName
            {
                set => Set(new Value { { "_kb_last_name", value } });
            }

            /// <summary>
            /// Sets the name for this user.
            /// </summary>
            public static string Name
            {
                set => Set(new Value { { "_kb_name", value } });
            }
        }
    }
}