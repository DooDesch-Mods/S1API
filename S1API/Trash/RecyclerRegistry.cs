#if (MONOMELON || MONOBEPINEX)
using S1Recycler = ScheduleOne.ObjectScripts.Recycler;
#else
using S1Recycler = Il2CppScheduleOne.ObjectScripts.Recycler;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Lifecycle;
using S1API.Logging;

namespace S1API.Trash
{
    /// <summary>
    /// Discovers active native world recyclers and forwards their local processing signals.
    /// </summary>
    /// <remarks>
    /// This API observes the native flow without changing authority, replication, or persistence.
    /// Events forward each native signal; they do not deduplicate signals or replay past processing.
    /// </remarks>
    public static class RecyclerRegistry
    {
        private static readonly Log Logger = new Log("RecyclerRegistry");
        private static readonly Dictionary<int, Recycler> Recyclers = new Dictionary<int, Recycler>();
        private static bool _lifecycleHooked;

        /// <summary>Raised when a recycler's native processing-start signal fires on this peer.</summary>
        public static event Action<Recycler>? ProcessingStarted;

        /// <summary>
        /// Raised when a recycler's native processing-stop signal fires on this peer.
        /// This signal does not imply a state change or that cash is ready for collection.
        /// </summary>
        /// <remarks>Destroying a recycler or leaving the scene does not synthesize a stop signal.</remarks>
        public static event Action<Recycler>? ProcessingStopped;

        /// <summary>
        /// Gets an immutable snapshot of active recyclers in the loaded scene, reusing wrappers for live native objects.
        /// </summary>
        /// <returns>A read-only list of recyclers. The list does not change when scene membership changes.</returns>
        public static IReadOnlyList<Recycler> GetAll()
        {
            EnsureLifecycleHook();
            PruneDestroyedRecyclers();
            var nativeRecyclers = UnityEngine.Object.FindObjectsOfType<S1Recycler>();
            var recyclers = new List<Recycler>(nativeRecyclers.Length);
            for (int index = 0; index < nativeRecyclers.Length; index++)
            {
                S1Recycler native = nativeRecyclers[index];
                if (native != null)
                    recyclers.Add(Wrap(native));
            }

            return new ReadOnlyCollection<Recycler>(recyclers);
        }

        internal static Recycler Wrap(S1Recycler native)
        {
            EnsureLifecycleHook();
            int instanceId = native.GetInstanceID();
            if (Recyclers.TryGetValue(instanceId, out Recycler? recycler))
            {
                if (recycler.S1Recycler != null)
                    return recycler;

                recycler.Cleanup();
                Recyclers.Remove(instanceId);
            }

            recycler = new Recycler(native);
            Recyclers.Add(instanceId, recycler);
            return recycler;
        }

        internal static void Remove(S1Recycler native)
        {
            int instanceId = native.GetInstanceID();
            if (!Recyclers.TryGetValue(instanceId, out Recycler? recycler))
                return;

            recycler.Cleanup();
            Recyclers.Remove(instanceId);
        }

        internal static void NotifyProcessingStarted(Recycler recycler) =>
            Invoke(ProcessingStarted, recycler, nameof(ProcessingStarted));

        internal static void NotifyProcessingStopped(Recycler recycler) =>
            Invoke(ProcessingStopped, recycler, nameof(ProcessingStopped));

        private static void Invoke(Action<Recycler>? handlers, Recycler recycler, string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<Recycler> handler in handlers.GetInvocationList())
            {
                try { handler(recycler); }
                catch (Exception exception)
                {
                    Logger.Warning($"A RecyclerRegistry.{eventName} subscriber failed: {exception.Message}");
                }
            }
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;

            GameLifecycle.OnPreSceneChange += ClearSceneState;
            _lifecycleHooked = true;
        }

        private static void PruneDestroyedRecyclers()
        {
            List<int>? destroyedInstanceIds = null;
            foreach (KeyValuePair<int, Recycler> pair in Recyclers)
            {
                if (pair.Value.S1Recycler != null)
                    continue;

                pair.Value.Cleanup();
                destroyedInstanceIds ??= new List<int>();
                destroyedInstanceIds.Add(pair.Key);
            }

            if (destroyedInstanceIds == null)
                return;

            foreach (int instanceId in destroyedInstanceIds)
                Recyclers.Remove(instanceId);
        }

        private static void ClearSceneState()
        {
            foreach (Recycler recycler in Recyclers.Values)
                recycler.Cleanup();

            Recyclers.Clear();
        }
    }
}
