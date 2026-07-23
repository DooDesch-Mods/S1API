#if IL2CPPMELON
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using S1API.Lifecycle;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Retains mod-owned product definitions for the process lifetime
    /// and restores their scene-scoped native registrations around save loading.
    /// </summary>
    internal static class CustomProductDefinitionRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, CustomProductDefinitionRegistration>
            Registrations =
                new Dictionary<string, CustomProductDefinitionRegistration>(
                    StringComparer.OrdinalIgnoreCase);

        private static ICustomProductDefinitionRuntimeAdapter _runtimeAdapter =
            CustomProductDefinitionRuntimeAdapter.Instance;
        private static bool _hooked;

        /// <summary>
        /// Records ownership of a stable product ID and applies the definition to
        /// the active native registries when they are available.
        /// </summary>
        internal static S1Product.ProductDefinition Register(
            string ownerId,
            string productId,
            string productName,
            float initialPrice,
            S1Product.ProductDefinition definition)
        {
            string normalizedOwnerId = NormalizeRequired(ownerId, nameof(ownerId));
            string normalizedProductId = NormalizeRequired(productId, nameof(productId));
            string normalizedProductName = NormalizeRequired(productName, nameof(productName));

            if (ReferenceEquals(definition, null))
                throw new ArgumentNullException(nameof(definition));

            if (float.IsNaN(initialPrice) || float.IsInfinity(initialPrice))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialPrice),
                    initialPrice,
                    "Initial product price must be a finite value.");
            }

            CustomProductDefinitionRegistration registration;
            lock (Gate)
            {
                if (Registrations.TryGetValue(
                        normalizedProductId,
                        out CustomProductDefinitionRegistration? existing))
                {
                    if (!string.Equals(
                            existing.OwnerId,
                            normalizedOwnerId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Custom product ID '{normalizedProductId}' is already owned by " +
                            $"'{existing.OwnerId}' and cannot be registered by " +
                            $"'{normalizedOwnerId}'. Product ownership is case-insensitive.");
                    }

                    registration = existing;
                }
                else
                {
                    registration = new CustomProductDefinitionRegistration(
                        normalizedOwnerId,
                        normalizedProductId,
                        normalizedProductName,
                        initialPrice,
                        definition);
                    Registrations.Add(normalizedProductId, registration);
                }

                EnsureHooked();
            }

            _runtimeAdapter.Apply(registration);
            return registration.Definition;
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);

            string normalized = value.Trim();
            if (normalized.Length == 0)
            {
                throw new ArgumentException(
                    "Value cannot be empty or whitespace.",
                    parameterName);
            }

            return normalized;
        }

        private static void EnsureHooked()
        {
            if (_hooked)
                return;

            GameLifecycle.OnPreLoad += OnPreLoad;
            GameLifecycle.OnLoadComplete += OnLoadComplete;
            _hooked = true;
        }

        private static void OnPreLoad()
        {
            ApplyAll("pre-load");
        }

        private static void OnLoadComplete()
        {
            ApplyAll("load-complete");
        }

        private static void ApplyAll(string phase)
        {
            foreach (CustomProductDefinitionRegistration registration in Snapshot())
            {
                try
                {
                    _runtimeAdapter.Apply(registration);
                }
                catch (Exception exception)
                {
                    MelonLoader.MelonLogger.Error(
                        $"[CustomProductDefinitionRegistry] Failed to restore product " +
                        $"'{registration.ProductId}' owned by '{registration.OwnerId}' during " +
                        $"{phase}: {exception}");
                }
            }
        }

        private static CustomProductDefinitionRegistration[] Snapshot()
        {
            lock (Gate)
            {
                var snapshot =
                    new CustomProductDefinitionRegistration[Registrations.Count];
                Registrations.Values.CopyTo(snapshot, 0);
                return snapshot;
            }
        }

        internal static void ResetForTesting(
            ICustomProductDefinitionRuntimeAdapter runtimeAdapter)
        {
            if (runtimeAdapter == null)
                throw new ArgumentNullException(nameof(runtimeAdapter));

            lock (Gate)
            {
                if (_hooked)
                {
                    GameLifecycle.OnPreLoad -= OnPreLoad;
                    GameLifecycle.OnLoadComplete -= OnLoadComplete;
                }

                Registrations.Clear();
                _runtimeAdapter = runtimeAdapter;
                _hooked = false;
            }
        }

        internal static void RestoreRuntimeAdapterForTesting()
        {
            ResetForTesting(CustomProductDefinitionRuntimeAdapter.Instance);
        }

        internal static void InvokePreLoadForTesting()
        {
            OnPreLoad();
        }

        internal static void InvokeLoadCompleteForTesting()
        {
            OnLoadComplete();
        }
    }
}
