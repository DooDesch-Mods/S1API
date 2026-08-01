using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace S1API.Utils
{
    /// <summary>
    /// Provides cross-runtime helpers for subscribing to and unsubscribing from Unity events.
    /// </summary>
    public static class EventHelper
    {
#pragma warning disable CS0618
        internal static void AddListener(Action listener, Action<Action> subscribe) =>
            Internal.Abstraction.EventHelper.AddListener(listener, subscribe);

        internal static void RemoveListener(Action listener, Action<Action> unsubscribe) =>
            Internal.Abstraction.EventHelper.RemoveListener(listener, unsubscribe);

        /// <summary>
        /// Adds a listener to a Unity event in a Mono and IL2CPP compatible manner.
        /// </summary>
        /// <param name="listener">The callback to subscribe.</param>
        /// <param name="unityEvent">The Unity event to subscribe to.</param>
        public static void AddListener(Action listener, UnityEvent unityEvent) =>
            Internal.Abstraction.EventHelper.AddListener(listener, unityEvent);

        /// <summary>
        /// Removes a listener previously added with <see cref="AddListener(Action, UnityEvent)"/>.
        /// </summary>
        /// <param name="listener">The callback to unsubscribe.</param>
        /// <param name="unityEvent">The Unity event to unsubscribe from.</param>
        public static void RemoveListener(Action listener, UnityEvent unityEvent) =>
            Internal.Abstraction.EventHelper.RemoveListener(listener, unityEvent);

        /// <summary>
        /// Adds a listener to a generic Unity event in a Mono and IL2CPP compatible manner.
        /// </summary>
        /// <typeparam name="T">The event argument type.</typeparam>
        /// <param name="listener">The callback to subscribe.</param>
        /// <param name="unityEvent">The Unity event to subscribe to.</param>
        public static void AddListener<T>(Action<T> listener, UnityEvent<T> unityEvent) =>
            Internal.Abstraction.EventHelper.AddListener(listener, unityEvent);

        /// <summary>
        /// Removes a listener previously added with <see cref="AddListener{T}(Action{T}, UnityEvent{T})"/>.
        /// </summary>
        /// <typeparam name="T">The event argument type.</typeparam>
        /// <param name="listener">The callback to unsubscribe.</param>
        /// <param name="unityEvent">The Unity event to unsubscribe from.</param>
        public static void RemoveListener<T>(Action<T> listener, UnityEvent<T> unityEvent) =>
            Internal.Abstraction.EventHelper.RemoveListener(listener, unityEvent);

        /// <summary>
        /// Adds an event-trigger entry whose callback does not need the event data.
        /// </summary>
        /// <param name="trigger">The target event-trigger component.</param>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="listener">The callback invoked when the event fires.</param>
        public static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            Action listener) =>
            Internal.Abstraction.EventHelper.AddEventTrigger(trigger, eventType, listener);

        /// <summary>
        /// Adds an event-trigger entry whose callback receives the event data.
        /// </summary>
        /// <param name="trigger">The target event-trigger component.</param>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="listener">The callback invoked with the event data.</param>
        public static void AddEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            Action<BaseEventData> listener) =>
            Internal.Abstraction.EventHelper.AddEventTrigger(trigger, eventType, listener);

        /// <summary>
        /// Removes a matching event-trigger entry and its callback.
        /// </summary>
        /// <param name="trigger">The target event-trigger component.</param>
        /// <param name="eventType">The event type to remove.</param>
        /// <param name="listener">The callback previously supplied when adding the entry.</param>
        public static void RemoveEventTrigger(
            EventTrigger trigger,
            EventTriggerType eventType,
            Action<BaseEventData> listener) =>
            Internal.Abstraction.EventHelper.RemoveEventTrigger(trigger, eventType, listener);
#pragma warning restore CS0618
    }
}
