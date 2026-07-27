using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Describes product content or a complete filled visual rendered for one packaging type.
    /// </summary>
    /// <remarks>
    /// Repeated-content profiles clone the provider's object for every configured placement.
    /// Complete-filled profiles clone one mod-owned object or one runtime-resolved game-owned
    /// visual template. Shared game-owned prefabs remain unchanged and assets are not
    /// transmitted to other peers.
    /// </remarks>
    public sealed class ProductPackagingContentProfile
    {
        internal ProductPackagingContentProfile(
            ProductPackagingContentSource source,
            Func<GameObject?>? contentProvider,
            IReadOnlyList<ProductPresentationTransform> placements,
            ProductPresentationTransform? completeVisualTransform,
            ProductPackagingVisualTemplate? nativeVisualTemplate,
            Action<GameObject>? nativeVisualCustomizer)
        {
            Source = source;
            ContentProvider = contentProvider;
            Placements = placements;
            CompleteVisualTransform = completeVisualTransform;
            NativeVisualTemplate = nativeVisualTemplate;
            NativeVisualCustomizer = nativeVisualCustomizer;
        }

        internal ProductPackagingContentSource Source { get; }

        internal Func<GameObject?>? ContentProvider { get; }

        internal IReadOnlyList<ProductPresentationTransform> Placements { get; }

        internal ProductPresentationTransform? CompleteVisualTransform { get; }

        internal ProductPackagingVisualTemplate? NativeVisualTemplate { get; }

        internal Action<GameObject>? NativeVisualCustomizer { get; }
    }

    internal enum ProductPackagingContentSource
    {
        RepeatedContent = 0,
        CompleteFilledVisual = 1,
        NativeFilledVisualScaffold = 2
    }
}
