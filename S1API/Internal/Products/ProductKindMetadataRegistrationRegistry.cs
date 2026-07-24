using System;
using System.Collections.Generic;
using S1API.Lifecycle;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal static class ProductKindMetadataRegistrationRegistry
    {
        private static readonly object Gate = new object();
        private static readonly object ApplyGate = new object();
        private static readonly Dictionary<string, ProductKindMetadata> Metadata =
            new Dictionary<string, ProductKindMetadata>(StringComparer.OrdinalIgnoreCase);

        private static IProductKindMetadataRuntimeAdapter _runtimeAdapter =
            ProductKindMetadataRuntimeAdapter.Instance;
        private static bool _hooked;

        internal static IReadOnlyCollection<ProductKindMetadata> All
        {
            get
            {
                var snapshot = Snapshot();
                Array.Sort(snapshot, Compare);
                return Array.AsReadOnly(snapshot);
            }
        }

        internal static bool TryGet(
            string productKindId,
            out ProductKindMetadata? metadata)
        {
            string normalizedId = ProductKindId.Normalize(productKindId, nameof(productKindId));
            lock (Gate)
                return Metadata.TryGetValue(normalizedId, out metadata);
        }

        internal static ProductKindMetadata Register(ProductKindMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            ProductKindMetadata registered;
            lock (Gate)
            {
                if (Metadata.TryGetValue(metadata.ProductKind.Id, out ProductKindMetadata? existing))
                {
                    if (!existing.IsEquivalentTo(metadata))
                        throw CreateConflict(existing, metadata);
                    registered = existing;
                }
                else
                {
                    ValidateNativeMetadataCollision(metadata);
                    Metadata.Add(metadata.ProductKind.Id, metadata);
                    registered = metadata;
                }

                EnsureHooked();
            }

            ApplyAll("registration");
            return registered;
        }

        internal static void ApplyAll(string phase)
        {
            lock (ApplyGate)
            {
                try
                {
                    _runtimeAdapter.Apply(Snapshot());
                }
                catch (Exception exception)
                {
                    try
                    {
                        MelonLoader.MelonLogger.Error(
                            "[ProductKindMetadataRegistry] Failed to apply product-kind metadata "
                            + $"during {phase}: {exception}");
                    }
                    catch (Exception)
                    {
                        // MelonLogger is not initialized in contract-test hosts.
                    }
                }
            }
        }

        internal static void ResetForTesting(IProductKindMetadataRuntimeAdapter runtimeAdapter)
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

                    Metadata.Clear();
                    _runtimeAdapter = runtimeAdapter;
                    _hooked = false;
                }
            }
        }

        internal static void RestoreRuntimeAdapterForTesting()
        {
            ResetForTesting(ProductKindMetadataRuntimeAdapter.Instance);
        }

        internal static void InvokePreLoadForTesting()
        {
            OnPreLoad();
        }

        internal static void InvokeLoadCompleteForTesting()
        {
            OnLoadComplete();
        }

        private static ProductKindMetadata[] Snapshot()
        {
            lock (Gate)
            {
                var snapshot = new ProductKindMetadata[Metadata.Count];
                Metadata.Values.CopyTo(snapshot, 0);
                return snapshot;
            }
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

        private static int Compare(ProductKindMetadata left, ProductKindMetadata right)
        {
            int order = left.SortOrder.CompareTo(right.SortOrder);
            if (order != 0)
                return order;
            order = StringComparer.OrdinalIgnoreCase.Compare(left.DisplayName, right.DisplayName);
            return order != 0
                ? order
                : StringComparer.OrdinalIgnoreCase.Compare(
                    left.ProductKind.Id,
                    right.ProductKind.Id);
        }

        private static void ValidateNativeMetadataCollision(ProductKindMetadata candidate)
        {
            DrugType? candidateType = candidate.ProductKind.CompatibilityDrugType;
            if (candidateType != DrugType.MDMA && candidateType != DrugType.Heroin)
                return;

            foreach (ProductKindMetadata existing in Metadata.Values)
            {
                if (existing.ProductKind.CompatibilityDrugType != candidateType)
                    continue;

                if (!string.Equals(
                        existing.DisplayName,
                        candidate.DisplayName,
                        StringComparison.Ordinal)
                    || !existing.HasSameColor(candidate))
                {
                    throw new InvalidOperationException(
                        $"Product kind '{candidate.ProductKind.Id}' conflicts with "
                        + $"'{existing.ProductKind.Id}' for native compatibility drug type "
                        + $"'{candidateType}'. Shared native types must use the same display "
                        + "name and color so native-facing metadata remains deterministic.");
                }
            }
        }

        private static InvalidOperationException CreateConflict(
            ProductKindMetadata existing,
            ProductKindMetadata requested)
        {
            return new InvalidOperationException(
                $"Product-kind metadata for '{requested.ProductKind.Id}' is already "
                + $"registered as '{existing.DisplayName}' with different presentation "
                + "or Product Manager behavior. Product-kind IDs are case-insensitive.");
        }
    }
}
