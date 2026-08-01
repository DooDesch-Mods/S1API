using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace S1API.Internal.Abstraction
{
    /// <summary>
    /// Provides the internal implementation for cross-runtime Unity event subscriptions.
    /// </summary>
    [Obsolete("Use S1API.Utils.EventHelper instead. This compatibility type will be hidden from public usage in a future update.")]
    public static class EventHelper
    {
        private static readonly Dictionary<Action, Delegate> SubscribedActions =
            new Dictionary<Action, Delegate>();

        private static readonly Dictionary<Delegate, Delegate> SubscribedGenericActions =
            new Dictionary<Delegate, Delegate>();

        internal static void AddListener(Action listener, Action<Action> subscribe)
        {
            if (SubscribedActions.ContainsKey(listener))
                return;

            subscribe(listener);
            SubscribedActions.Add(listener, listener);
        }

        internal static void RemoveListener(Action listener, Action<Action> unsubscribe)
        {
            if (!SubscribedActions.ContainsKey(listener))
                return;

            unsubscribe(listener);
            SubscribedActions.Remove(listener);
        }

        /// <summary>
        /// Adds a listener to a Unity event in a Mono and IL2CPP compatible manner.
        /// </summary>
        /// <param name="listener">The callback to subscribe.</param>
        /// <param name="unityEvent">The Unity event to subscribe to.</param>
        public static void AddListener(Action listener, UnityEvent unityEvent)
        {
            if (listener == null || unityEvent == null || SubscribedActions.ContainsKey(listener))
                return;

#if IL2CPPMELON
            Action wrapped = new Action(listener);
            unityEvent.AddListener(wrapped);
#else
            UnityAction wrapped = new UnityAction(listener);
            unityEvent.AddListener(wrapped);
#endif
            SubscribedActions.Add(listener, wrapped);
        }

        /// <summary>
        /// Removes a listener previously added with <see cref="AddListener(Action, UnityEvent)"/>.
        /// </summary>
        /// <param name="listener">The callback to unsubscribe.</param>
        /// <param name="unityEvent">The Unity event to unsubscribe from.</param>
        public static void RemoveListener(Action listener, UnityEvent unityEvent)
        {
            if (listener == null || unityEvent == null)
                return;

            SubscribedActions.TryGetValue(listener, out Delegate? wrappedAction);
            SubscribedActions.Remove(listener);
            if (wrappedAction == null)
                return;

#if IL2CPPMELON
            if (wrappedAction is Action action)
                unityEvent.RemoveListener(action);
#else
            if (wrappedAction is UnityAction unityAction)
                unityEvent.RemoveListener(unityAction);
#endif
        }

        /// <summary>
        /// Adds a listener to a generic Unity event in a Mono and IL2CPP compatible manner.
        /// </summary>
        /// <typeparam name="T">The event argument type.</typeparam>
        /// <param name="listener">The callback to subscribe.</param>
        /// <param name="unityEvent">The Unity event to subscribe to.</param>
        public static void AddListener<T>(Action<T> listener, UnityEvent<T> unityEvent)
        {
            if (listener == null || unityEvent == null || SubscribedGenericActions.ContainsKey(listener))
                return;

#if IL2CPPMELON
            Action<T> wrapped = new Action<T>(listener);
            unityEvent.AddListener(wrapped);
#else
            UnityAction<T> wrapped = new UnityAction<T>(listener);
            unityEvent.AddListener(wrapped);
#endif
            SubscribedGenericActions.Add(listener, wrapped);
        }

        /// <summary>
        /// Removes a listener previously added with <see cref="AddListener{T}(Action{T}, UnityEvent{T})"/>.
        /// </summary>
        /// <typeparam name="T">The event argument type.</typeparam>
        /// <param name="listener">The callback to unsubscribe.</param>
        /// <param name="unityEvent">The Unity event to unsubscribe from.</param>
        public static void RemoveListener<T>(Action<T> listener, UnityEvent<T> unityEvent)
        {
            if (listener == null || unityEvent == null)
                return;

            if (!SubscribedGenericActions.TryGetValue(listener, out Delegate? wrappedAction)
                || wrappedAction == null)
                return;

#if IL2CPPMELON
            if (wrappedAction is Action<T> action)
                unityEvent.RemoveListener(action);
#else
            if (wrappedAction is UnityAction<T> unityAction)
                unityEvent.RemoveListener(unityAction);
#endif
            SubscribedGenericActions.Remove(listener);
        }

        /// <summary>
        /// Adds an event-trigger entry whose callback does not need the event data.
        /// </summary>
        /// <param name="trigger">The target event-trigger component.</param>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="listener">The callback invoked when the event fires.</param>
        public static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            Action listener)
        {
            if (trigger == null || listener == null)
                return;

            AddEventTrigger(trigger, eventType, _ => listener());
        }

        /// <summary>
        /// Adds an event-trigger entry whose callback receives the event data.
        /// </summary>
        /// <param name="trigger">The target event-trigger component.</param>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="listener">The callback invoked with the event data.</param>
        public static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            Action<BaseEventData> listener)
        {
            if (trigger == null || listener == null)
                return;

            var entry = new EventTrigger.Entry { eventID = eventType };
            AddListener(listener, entry.callback);
            trigger.triggers.Add(entry);
        }

        /// <summary>
        /// Removes a matching event-trigger entry and its callback.
        /// </summary>
        /// <param name="trigger">The target event-trigger component.</param>
        /// <param name="eventType">The event type to remove.</param>
        /// <param name="listener">The callback previously supplied when adding the entry.</param>
        public static void RemoveEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            Action<BaseEventData> listener)
        {
            if (trigger == null || listener == null || !SubscribedGenericActions.ContainsKey(listener))
                return;

            EventTrigger.Entry? entry = trigger.triggers.
#if IL2CPPMELON
                _items.
#endif
                FirstOrDefault(candidate => candidate.eventID == eventType);
            if (entry == null)
                return;

            RemoveListener(listener, entry.callback);
            trigger.triggers.Remove(entry);
        }
    }
}
