#if IL2CPPMELON
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using System.Collections.Generic;
using S1API.Lifecycle;

namespace S1API.Internal.Items
{
    internal interface IRuntimeItemDefinitionRegistryAdapter
    {
        bool IsRegistered(string itemId);

        void Register(S1ItemFramework.ItemDefinition definition);
    }

    internal sealed class RuntimeItemDefinitionRegistryAdapter
        : IRuntimeItemDefinitionRegistryAdapter
    {
        internal static readonly RuntimeItemDefinitionRegistryAdapter Instance =
            new RuntimeItemDefinitionRegistryAdapter();

        private RuntimeItemDefinitionRegistryAdapter()
        {
        }

        public bool IsRegistered(string itemId)
        {
            return S1Registry.Instance != null && S1Registry.ItemExists(itemId);
        }

        public void Register(S1ItemFramework.ItemDefinition definition)
        {
            if (S1Registry.Instance == null)
                throw new InvalidOperationException("ScheduleOne.Registry is unavailable.");

            S1Registry.Instance.AddToRegistry(definition);
        }
    }

    /// <summary>
    /// Retains definitions created by S1API builders and reapplies them after the
    /// native registry removes runtime items during a scene transition.
    /// </summary>
    internal static class RuntimeItemDefinitionRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, S1ItemFramework.ItemDefinition> Definitions =
            new Dictionary<string, S1ItemFramework.ItemDefinition>(
                StringComparer.OrdinalIgnoreCase);

        private static IRuntimeItemDefinitionRegistryAdapter _adapter =
            RuntimeItemDefinitionRegistryAdapter.Instance;
        private static bool _hooked;

        internal static void Retain(
            string itemId,
            S1ItemFramework.ItemDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item ID cannot be empty or whitespace.", nameof(itemId));
            if (ReferenceEquals(definition, null))
                throw new ArgumentNullException(nameof(definition));

            lock (Gate)
            {
                Definitions[itemId] = definition;
                EnsureHooked();
            }
        }

        internal static void Forget(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            lock (Gate)
                Definitions.Remove(itemId);
        }

        private static void EnsureHooked()
        {
            if (_hooked)
                return;

            GameLifecycle.OnPreLoad += ReapplyAll;
            _hooked = true;
        }

        private static void ReapplyAll()
        {
            KeyValuePair<string, S1ItemFramework.ItemDefinition>[] snapshot;
            lock (Gate)
            {
                snapshot =
                    new KeyValuePair<string, S1ItemFramework.ItemDefinition>[Definitions.Count];
                ((ICollection<KeyValuePair<string, S1ItemFramework.ItemDefinition>>)Definitions)
                    .CopyTo(snapshot, 0);
            }

            foreach (KeyValuePair<string, S1ItemFramework.ItemDefinition> entry in snapshot)
            {
                try
                {
                    if (!_adapter.IsRegistered(entry.Key))
                        _adapter.Register(entry.Value);
                }
                catch (Exception exception)
                {
                    MelonLoader.MelonLogger.Error(
                        $"[RuntimeItemDefinitionRegistry] Failed to restore item " +
                        $"'{entry.Key}' during pre-load: {exception}");
                }
            }
        }

        internal static void ResetForTesting(
            IRuntimeItemDefinitionRegistryAdapter adapter)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            lock (Gate)
            {
                if (_hooked)
                    GameLifecycle.OnPreLoad -= ReapplyAll;

                Definitions.Clear();
                _adapter = adapter;
                _hooked = false;
            }
        }

        internal static void RestoreRuntimeAdapterForTesting()
        {
            ResetForTesting(RuntimeItemDefinitionRegistryAdapter.Instance);
        }

        internal static void InvokePreLoadForTesting()
        {
            ReapplyAll();
        }
    }
}
