using System;
using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal static class ProductMixingProfileRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ProductMixingProfile> Profiles = new Dictionary<string, ProductMixingProfile>(StringComparer.OrdinalIgnoreCase);

        internal static ProductMixingProfile Register(ProductMixingProfile profile)
        {
            lock (Gate)
            {
                if (!Profiles.TryGetValue(profile.ProductKind.Id, out ProductMixingProfile? existing))
                {
                    Profiles.Add(profile.ProductKind.Id, profile);
                    return profile;
                }
                if (existing.MixerMap == profile.MixerMap && ReferenceEquals(existing.OutputFactory, profile.OutputFactory))
                    return existing;
                throw new InvalidOperationException("A conflicting mixing profile is already registered for product kind '" + profile.ProductKind.Id + "'.");
            }
        }

        internal static bool TryGet(string productKindId, out ProductMixingProfile? profile)
        {
            string normalized = ProductKindId.Normalize(productKindId, nameof(productKindId));
            lock (Gate)
                return Profiles.TryGetValue(normalized, out profile);
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
                Profiles.Clear();
        }
    }
}
