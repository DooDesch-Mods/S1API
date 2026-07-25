using System;
using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Stores immutable custom-product consumption profile registrations.</summary>
    internal static class ProductConsumptionProfileRegistrationRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ProductConsumptionProfileRegistration>
            ProductProfiles = new Dictionary<string, ProductConsumptionProfileRegistration>(
                StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ProductConsumptionProfileRegistration>
            KindProfiles = new Dictionary<string, ProductConsumptionProfileRegistration>(
                StringComparer.OrdinalIgnoreCase);

        internal static ProductConsumptionProfile RegisterForProduct(
            string productId,
            ProductConsumptionProfile profile)
        {
            return Register(
                ProductProfiles,
                "product ID",
                ProductKindId.Normalize(productId, nameof(productId)),
                profile);
        }

        internal static ProductConsumptionProfile RegisterForProductKind(
            ProductKind productKind,
            ProductConsumptionProfile profile)
        {
            return Register(KindProfiles, "product-kind ID", productKind.Id, profile);
        }

        internal static bool TryResolve(
            string productId,
            string productKindId,
            out ProductConsumptionProfileRegistration? registration)
        {
            lock (Gate)
            {
                if (ProductProfiles.TryGetValue(productId, out registration))
                    return true;

                return KindProfiles.TryGetValue(productKindId, out registration);
            }
        }

        internal static bool TryGetManifestIdentity(
            string productId,
            string productKindId,
            out string providerId,
            out int providerVersion)
        {
            if (TryResolve(productId, productKindId, out ProductConsumptionProfileRegistration? registration) &&
                registration != null)
            {
                providerId = registration.Profile.ProviderId;
                providerVersion = registration.Profile.ProviderVersion;
                return true;
            }

            providerId = string.Empty;
            providerVersion = 0;
            return false;
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
            {
                ProductProfiles.Clear();
                KindProfiles.Clear();
            }
        }

        private static ProductConsumptionProfile Register(
            Dictionary<string, ProductConsumptionProfileRegistration> registrations,
            string keyDescription,
            string key,
            ProductConsumptionProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            ProductConsumptionProfileRegistration registration;
            lock (Gate)
            {
                if (registrations.TryGetValue(key, out ProductConsumptionProfileRegistration? existing))
                {
                    if (ReferenceEquals(existing.Profile, profile))
                        return existing.Profile;

                    throw new InvalidOperationException(
                        "Consumption profile " + keyDescription + " '" + key +
                        "' is already registered by provider '" + existing.Profile.ProviderId +
                        "'. Reuse the existing profile or choose another stable key.");
                }

                registration = new ProductConsumptionProfileRegistration(key, profile);
                registrations.Add(key, registration);
            }

            CustomProductManifestRuntime.RefreshHostManifestIfReady();
            return registration.Profile;
        }
    }
}
