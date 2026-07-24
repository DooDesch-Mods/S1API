using System;
using System.Collections.Generic;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Registers process-lifetime packaging-content profiles by product and packaging ID.
    /// </summary>
    /// <remarks>
    /// Keys are case-insensitive. Registration does not transmit assets; every participating
    /// peer must register matching product definitions and profiles locally.
    /// </remarks>
    public static class ProductPackagingContentProfileRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<ProductPackagingContentKey,
            ProductPackagingContentProfileRegistration> Registrations =
                new Dictionary<ProductPackagingContentKey,
                    ProductPackagingContentProfileRegistration>();

        /// <summary>
        /// Registers content for one stable product and packaging pair.
        /// </summary>
        /// <param name="ownerId">The stable ID of the owning mod.</param>
        /// <param name="productId">The stable, namespaced custom product ID.</param>
        /// <param name="packagingId">The packaging definition ID, such as <c>baggie</c>.</param>
        /// <param name="profile">The immutable packaging-content profile.</param>
        /// <returns>The retained profile.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an ID is empty or <paramref name="productId"/> is not namespaced.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the product/packaging pair has a conflicting owner or profile.
        /// </exception>
        public static ProductPackagingContentProfile Register(
            string ownerId,
            string productId,
            string packagingId,
            ProductPackagingContentProfile profile)
        {
            string normalizedOwnerId = NormalizeRequired(ownerId, nameof(ownerId));
            string normalizedProductId =
                ProductKindId.Normalize(productId, nameof(productId));
            string normalizedPackagingId =
                NormalizeRequired(packagingId, nameof(packagingId));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var key =
                new ProductPackagingContentKey(
                    normalizedProductId,
                    normalizedPackagingId);
            lock (Gate)
            {
                if (Registrations.TryGetValue(
                        key,
                        out ProductPackagingContentProfileRegistration? existing))
                {
                    if (!string.Equals(
                            existing.OwnerId,
                            normalizedOwnerId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Packaging content for product '{normalizedProductId}' and " +
                            $"packaging '{normalizedPackagingId}' is already owned by " +
                            $"'{existing.OwnerId}' and cannot be registered by " +
                            $"'{normalizedOwnerId}'. Registration keys are case-insensitive.");
                    }

                    if (!ReferenceEquals(existing.Profile, profile))
                    {
                        throw new InvalidOperationException(
                            $"Packaging content for product '{normalizedProductId}' and " +
                            $"packaging '{normalizedPackagingId}' is already registered by " +
                            "this owner with another profile. Reuse the original profile.");
                    }

                    return existing.Profile;
                }

                Registrations.Add(
                    key,
                    new ProductPackagingContentProfileRegistration(
                        normalizedOwnerId,
                        key,
                        profile));
                return profile;
            }
        }

        internal static bool TryResolve(
            string productId,
            string packagingId,
            out ProductPackagingContentProfileRegistration? registration)
        {
            var key = new ProductPackagingContentKey(productId, packagingId);
            lock (Gate)
                return Registrations.TryGetValue(key, out registration);
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
                Registrations.Clear();
            ProductPackagingContentRuntime.ResetForSceneChange();
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
