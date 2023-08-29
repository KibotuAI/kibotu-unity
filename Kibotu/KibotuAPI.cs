using System;

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
        internal const string KibotuUnityVersion = "0.9.1";

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
            if (!initialized) {
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
        public static string DistinctID {
            get => KibotuStorage.DistinctId;
        }

        public static string DistinctId {
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
            if (!IsInitialized()) return;
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
        public static void Track(string eventName, Value properties) {
            if (!IsInitialized()) return;
            Controller.DoTrack(eventName, properties);
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
            KibotuSettings.Instance.DebugToken = token;
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
                Controller.DoEngage(new Value {{"_kb_append", properties}});
            }

            /// <summary>
            /// Appends a value to a list-valued property.
            /// </summary>
            /// <param name="property">the %People Analytics property that should have it's value appended to</param>
            /// <param name="values">the new value that will appear at the end of the property's list</param>
            public static void Append(string property, Value values)
            {
                Append(new Value {{property, values}});
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
                Controller.DoEngage(new Value {{"_kb_delete", ""}});
            }

            /// <summary>
            /// Change the existing values of multiple %People Analytics properties at once.
            /// </summary>
            /// <param name="properties"> A map of String properties names to Long amounts. Each property associated with a name in the map </param>
            public static void Increment(Value properties)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value {{"_kb_add", properties}});
            }

            /// <summary>
            /// Convenience method for incrementing a single numeric property by the specified amount.
            /// </summary>
            /// <param name="property">property name</param>
            /// <param name="by">amount to increment by</param>
            public static void Increment(string property, Value by)
            {
                Increment(new Value {{property, by}});
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
                Controller.DoEngage(new Value {{"_kb_set", properties}});
            }

            /// <summary>
            /// Sets a single property with the given name and value for this user.
            /// </summary>
            /// <param name="property">property name</param>
            /// <param name="to">property value</param>
            public static void Set(string property, Value to)
            {
                Set(new Value {{property, to}});
            }

            /// <summary>
            /// Like Kibotu.Set(Value properties), but will not set properties that already exist on a record.
            /// </summary>
            /// <param name="properties">a JSONObject containing the collection of properties you wish to apply to the identified user. Each key in the JSONObject will be associated with a property name, and the value of that key will be assigned to the property.</param>
            public static void SetOnce(Value properties)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value {{"_kb_set_once", properties}});
            }

            /// <summary>
            /// Like Kibotu.Set(string property, Value to), but will not set properties that already exist on a record.
            /// </summary>
            /// <param name="property">property name</param>
            /// <param name="to">property value</param>
            public static void SetOnce(string property, Value to)
            {
                SetOnce(new Value {{property, to}});
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
                TrackCharge(new Value {{"_kb_amount", amount}});
            }

            /// <summary>
            /// Track a revenue transaction for the identified people profile.
            /// </summary>
            /// <param name="properties">a JSONObject containing the collection of properties you wish to apply</param>
            public static void TrackCharge(Value properties)
            {
                if (!IsInitialized()) return;
                properties["_kb_time"] = Util.CurrentDateTime();
                Controller.DoEngage(new Value {{"_kb_append", new Value {{"_kb_transactions", properties}}}});
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
                Controller.DoEngage(new Value {{"_kb_union", properties}});
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
                Union(new Value {{property, values}});
            }

            /// <summary>
            /// Takes a string property name, and permanently removes the property and their values from a profile.
            /// </summary>
            /// <param name="property">property</param>
            public static void Unset(string property)
            {
                if (!IsInitialized()) return;
                Controller.DoEngage(new Value {{"_kb_unset", new string[]{property}}});
            }

            /// <summary>
            /// Sets the email for this user.
            /// </summary>
            public static string Email
            {
                set => Set(new Value {{"_kb_email", value}});
            }

            /// <summary>
            /// Sets the first name for this user.
            /// </summary>
            public static string FirstName
            {
                set => Set(new Value {{"_kb_first_name", value}});
            }

            /// <summary>
            /// Sets the last name for this user.
            /// </summary>
            public static string LastName
            {
                set => Set(new Value {{"_kb_last_name", value}});
            }

            /// <summary>
            /// Sets the name for this user.
            /// </summary>
            public static string Name
            {
                set => Set(new Value {{"_kb_name", value}});
            }

        }
    }
}
