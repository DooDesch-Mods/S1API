#if IL2CPPMELON
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using S1API.Lifecycle;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Retains mod-owned product definitions for the process lifetime
    /// and restores their scene-scoped native registrations around save loading.
    /// </summary>
    internal static class CustomProductDefinitionRegistry
    {
        private static readonly object Gate = new object();
        private static readonly object ApplyGate = new object();
        private static readonly Dictionary<string, CustomProductDefinitionRegistration>
            Registrations =
                new Dictionary<string, CustomProductDefinitionRegistration>(
                    StringComparer.OrdinalIgnoreCase);

        private static ICustomProductDefinitionRuntimeAdapter _runtimeAdapter =
            CustomProductDefinitionRuntimeAdapter.Instance;
        private static ICustomProductPresentationRuntime _presentationRuntime =
            CustomProductPresentationRuntime.Instance;
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
            return Register(
                ownerId,
                productId,
                productName,
                initialPrice,
                definition,
                null);
        }

        /// <summary>
        /// Records ownership and S1API wrapper metadata for a generic custom product.
        /// </summary>
        internal static S1Product.ProductDefinition Register(
            string ownerId,
            string productId,
            string productName,
            float initialPrice,
            S1Product.ProductDefinition definition,
            CustomProductDefinitionMetadata? metadata)
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

            lock (ApplyGate)
            {
                CustomProductDefinitionRegistration registration;
                bool added = false;
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
                                $"Custom product ID '{normalizedProductId}' is already owned " +
                                $"by '{existing.OwnerId}' and cannot be registered by " +
                                $"'{normalizedOwnerId}'. Product ownership is " +
                                "case-insensitive.");
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
                            definition,
                            metadata);
                        Registrations.Add(normalizedProductId, registration);
                        added = true;
                    }

                    EnsureHooked();
                }

                try
                {
                    ProductPresentationProfileRegistration? profile =
                        ResolvePresentationProfile(registration);
                    if (profile != null)
                        _presentationRuntime.Apply(registration, profile);

                    _runtimeAdapter.Apply(registration);

                    if (profile == null)
                        _presentationRuntime.Apply(registration, null);
                }
                catch
                {
                    if (added)
                    {
                        if (registration.PresentationState != null)
                        {
                            try
                            {
                                _presentationRuntime.Apply(registration, null);
                            }
                            catch (Exception rollbackException)
                            {
                                MelonLoader.MelonLogger.Error(
                                    "[CustomProductDefinitionRegistry] Failed to restore " +
                                    $"presentation fallbacks after rejecting " +
                                    $"'{normalizedProductId}': {rollbackException}");
                            }
                        }

                        lock (Gate)
                        {
                            if (Registrations.TryGetValue(
                                    normalizedProductId,
                                    out CustomProductDefinitionRegistration? current) &&
                                ReferenceEquals(current, registration))
                            {
                                Registrations.Remove(normalizedProductId);
                            }
                        }
                    }

                    throw;
                }

                return registration.Definition;
            }
        }

        internal static bool TryGetMetadata(
            S1Product.ProductDefinition definition,
            out CustomProductDefinitionMetadata? metadata)
        {
            metadata = null;
            if (ReferenceEquals(definition, null))
                return false;

            string productId;
            try
            {
                productId = definition.ID;
            }
            catch (Exception)
            {
                return false;
            }

            return TryGetMetadata(productId, definition, out metadata);
        }

        internal static bool TryGetMetadata(
            string productId,
            S1Product.ProductDefinition definition,
            out CustomProductDefinitionMetadata? metadata)
        {
            metadata = null;
            if (string.IsNullOrEmpty(productId) ||
                ReferenceEquals(definition, null))
            {
                return false;
            }

            lock (Gate)
            {
                if (!Registrations.TryGetValue(
                        productId,
                        out CustomProductDefinitionRegistration? registration) ||
                    registration.Metadata == null ||
                    !AreSameDefinition(registration.Definition, definition))
                {
                    return false;
                }

                metadata = registration.Metadata;
                return true;
            }
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
            lock (ApplyGate)
            {
                foreach (CustomProductDefinitionRegistration registration in Snapshot())
                {
                    try
                    {
                        _runtimeAdapter.Apply(registration);
                        ApplyPresentationProfile(registration);
                    }
                    catch (Exception exception)
                    {
                        MelonLoader.MelonLogger.Error(
                            $"[CustomProductDefinitionRegistry] Failed to restore product " +
                            $"'{registration.ProductId}' owned by " +
                            $"'{registration.OwnerId}' during {phase}: {exception}");
                    }
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
            ICustomProductDefinitionRuntimeAdapter runtimeAdapter,
            ICustomProductPresentationRuntime? presentationRuntime = null)
        {
            if (runtimeAdapter == null)
                throw new ArgumentNullException(nameof(runtimeAdapter));

            lock (ApplyGate)
            {
                lock (Gate)
                {
                    if (_hooked)
                    {
                        GameLifecycle.OnPreLoad -= OnPreLoad;
                        GameLifecycle.OnLoadComplete -= OnLoadComplete;
                    }

                    Registrations.Clear();
                    _runtimeAdapter = runtimeAdapter;
                    _presentationRuntime =
                        presentationRuntime ?? CustomProductPresentationRuntime.Instance;
                    _hooked = false;
                }
            }

            ProductPresentationProfileRegistry.ResetForTesting();
        }

        internal static void RestoreRuntimeAdapterForTesting()
        {
            ResetForTesting(
                CustomProductDefinitionRuntimeAdapter.Instance,
                CustomProductPresentationRuntime.Instance);
        }

        internal static void ApplyPresentationProfiles()
        {
            lock (ApplyGate)
            {
                foreach (CustomProductDefinitionRegistration registration in Snapshot())
                    ApplyPresentationProfile(registration);
            }
        }

        internal static void InvokePreLoadForTesting()
        {
            OnPreLoad();
        }

        internal static void InvokeLoadCompleteForTesting()
        {
            OnLoadComplete();
        }

        private static bool AreSameDefinition(
            S1Product.ProductDefinition left,
            S1Product.ProductDefinition right)
        {
            return ReferenceEquals(left, right) ||
                   (left is UnityEngine.Object leftObject &&
                    right is UnityEngine.Object rightObject &&
                    leftObject == rightObject);
        }

        private static void ApplyPresentationProfile(
            CustomProductDefinitionRegistration registration)
        {
            _presentationRuntime.Apply(
                registration,
                ResolvePresentationProfile(registration));
        }

        private static ProductPresentationProfileRegistration?
            ResolvePresentationProfile(
                CustomProductDefinitionRegistration registration)
        {
            if (registration.Metadata == null)
                return null;

            ProductPresentationProfileRegistry.TryResolve(
                registration.ProductId,
                registration.Metadata.ProductKind.Id,
                out ProductPresentationProfileRegistration? profile);
            return profile;
        }
    }
}
