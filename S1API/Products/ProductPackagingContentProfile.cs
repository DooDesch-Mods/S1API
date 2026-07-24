using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Describes mod-owned product content rendered inside one packaging type.
    /// </summary>
    /// <remarks>
    /// S1API clones the provider's object for every configured placement. The game-owned
    /// packaging shell remains unchanged and assets are not transmitted to other peers.
    /// </remarks>
    public sealed class ProductPackagingContentProfile
    {
        internal ProductPackagingContentProfile(
            Func<GameObject?> contentProvider,
            IReadOnlyList<ProductPresentationTransform> placements)
        {
            ContentProvider = contentProvider;
            Placements = placements;
        }

        internal Func<GameObject?> ContentProvider { get; }

        internal IReadOnlyList<ProductPresentationTransform> Placements { get; }
    }
}
