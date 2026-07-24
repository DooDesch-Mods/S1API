using System;
using System.Collections.Generic;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Registers process-lifetime presentation profiles for generic custom products.
    /// </summary>
    /// <remarks>
    /// Product IDs and product-kind IDs are case-insensitive. A product-specific profile takes
    /// deterministic precedence over a kind profile. Registration does not transmit assets;
    /// every participating peer must register matching product definitions and profiles locally.
    /// </remarks>
    public static class ProductPresentationProfileRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ProductPresentationProfileRegistration>
            ProductProfiles =
                new Dictionary<string, ProductPresentationProfileRegistration>(
                    StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ProductPresentationProfileRegistration>
            KindProfiles =
                new Dictionary<string, ProductPresentationProfileRegistration>(
                    StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a profile for one stable custom product ID.
        /// </summary>
        /// <param name="ownerId">The stable ID of the owning mod.</param>
        /// <param name="productId">The stable product ID.</param>
        /// <param name="profile">The immutable presentation profile.</param>
        /// <returns>The registered profile, or the same retained profile on an idempotent call.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an ID is empty or <paramref name="productId"/> is not namespaced.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the key has a conflicting owner or profile, or a required context cannot
        /// be applied to an existing product.
        /// </exception>
        public static ProductPresentationProfile RegisterForProduct(
            string ownerId,
            string productId,
            ProductPresentationProfile profile)
        {
            return Register(
                ProductProfiles,
                "product ID",
                ownerId,
                ProductKindId.Normalize(productId, nameof(productId)),
                profile);
        }

        /// <summary>
        /// Registers a profile for every generic custom product of one product kind.
        /// </summary>
        /// <param name="ownerId">The stable ID of the owning mod.</param>
        /// <param name="productKind">The immutable product kind.</param>
        /// <param name="profile">The immutable presentation profile.</param>
        /// <returns>The registered profile, or the same retained profile on an idempotent call.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="productKind"/> or <paramref name="profile"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the key has a conflicting owner or profile, or a required context cannot
        /// be applied to an existing product.
        /// </exception>
        public static ProductPresentationProfile RegisterForProductKind(
            string ownerId,
            ProductKind productKind,
            ProductPresentationProfile profile)
        {
            if (productKind == null)
                throw new ArgumentNullException(nameof(productKind));

            return Register(
                KindProfiles,
                "product-kind ID",
                ownerId,
                productKind.Id,
                profile);
        }

        private static ProductPresentationProfile Register(
            Dictionary<string, ProductPresentationProfileRegistration> registrations,
            string keyDescription,
            string ownerId,
            string key,
            ProductPresentationProfile profile)
        {
            string normalizedOwnerId = NormalizeRequired(ownerId, nameof(ownerId));
            string normalizedKey = NormalizeRequired(key, nameof(key));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            ProductPresentationProfileRegistration registration;
            bool added = false;
            lock (Gate)
            {
                if (registrations.TryGetValue(
                        normalizedKey,
                        out ProductPresentationProfileRegistration? existing))
                {
                    if (!string.Equals(
                            existing.OwnerId,
                            normalizedOwnerId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Presentation profile {keyDescription} '{normalizedKey}' is already " +
                            $"owned by '{existing.OwnerId}' and cannot be registered by " +
                            $"'{normalizedOwnerId}'. Registration keys are case-insensitive.");
                    }

                    if (!ReferenceEquals(existing.Profile, profile))
                    {
                        throw new InvalidOperationException(
                            $"Presentation profile {keyDescription} '{normalizedKey}' is already " +
                            "registered by this owner with another profile. Reuse the original " +
                            "profile or choose another stable key.");
                    }

                    return existing.Profile;
                }

                registration =
                    new ProductPresentationProfileRegistration(
                        normalizedOwnerId,
                        normalizedKey,
                        profile);
                registrations.Add(normalizedKey, registration);
                added = true;
            }

            try
            {
                CustomProductDefinitionRegistry.ApplyPresentationProfiles();
                return registration.Profile;
            }
            catch
            {
                if (added)
                {
                    lock (Gate)
                    {
                        if (registrations.TryGetValue(
                                normalizedKey,
                                out ProductPresentationProfileRegistration? current) &&
                            ReferenceEquals(current, registration))
                        {
                            registrations.Remove(normalizedKey);
                        }
                    }

                    try
                    {
                        CustomProductDefinitionRegistry.ApplyPresentationProfiles();
                    }
                    catch (Exception rollbackException)
                    {
                        MelonLoader.MelonLogger.Error(
                            "[ProductPresentationProfileRegistry] Failed to restore " +
                            $"presentation fallbacks after rejecting '{normalizedKey}': " +
                            rollbackException);
                    }
                }

                throw;
            }
        }

        internal static bool TryResolve(
            string productId,
            string productKindId,
            out ProductPresentationProfileRegistration? registration)
        {
            lock (Gate)
            {
                if (ProductProfiles.TryGetValue(productId, out registration))
                    return true;

                return KindProfiles.TryGetValue(productKindId, out registration);
            }
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
            {
                ProductProfiles.Clear();
                KindProfiles.Clear();
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
    }
}
