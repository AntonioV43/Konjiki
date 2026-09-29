using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();
        private static bool printLogs = false;

        public static void Subscribe<T>(Action<T> listener)
        {
            if (printLogs) Debug.Log($"Subscribe typeof(T)={typeof(T)}, hash={typeof(T).GetHashCode()}");

            if (!_events.ContainsKey(typeof(T))) _events[typeof(T)] = null;
            _events[typeof(T)] = (Action<T>)_events[typeof(T)] + listener;
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            if (printLogs) Debug.Log($"Unsubscribe typeof(T)={typeof(T)}, hash={typeof(T).GetHashCode()}");
            
            if (_events.ContainsKey(typeof(T)))
                _events[typeof(T)] = (Action<T>)_events[typeof(T)] - listener;
        }

        public static void Publish<T>(T eventData)
        {
            if (printLogs) Debug.Log($"Publish typeof(T)={typeof(T)}, hash={typeof(T).GetHashCode()}");

            if (_events.ContainsKey(typeof(T)) && _events[typeof(T)] != null)
                ((Action<T>)_events[typeof(T)])(eventData);
        }
    }
}