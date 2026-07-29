#if IL2CPPMELON
using NativeProductEntryList =
    Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Product.ProductEntry>;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Product = Il2CppScheduleOne.Product;
using S1ProductManagerUi = Il2CppScheduleOne.UI.Phone.ProductManagerApp;
using S1Root = Il2CppScheduleOne;
#elif MONOMELON
using NativeProductEntryList =
    System.Collections.Generic.List<ScheduleOne.Product.ProductEntry>;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Product = ScheduleOne.Product;
using S1ProductManagerUi = ScheduleOne.UI.Phone.ProductManagerApp;
using S1Root = ScheduleOne;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Products;
using UnityEngine;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;

namespace S1API.Internal.Products
{
    internal static class ProductManagerUiRuntime
    {
        internal readonly struct SectionIconState
        {
            internal SectionIconState(Sprite? sprite, Color tint)
            {
                Sprite = sprite;
                Tint = tint;
            }

            internal Sprite? Sprite { get; }

            internal Color Tint { get; }
        }

        private const string ManagedNamePrefix = "S1API_ProductKind_";
        private static readonly Log Logger = new Log("ProductManagerUiRuntime");

        internal static void ApplyCurrent(IReadOnlyList<ProductKindMetadata> metadata)
        {
            S1ProductManagerUi.ProductManagerApp app =
                S1DevUtilities.PlayerSingleton<
                    S1ProductManagerUi.ProductManagerApp>.Instance;
            if (app != null)
                Apply(app, metadata);
        }

        internal static void Apply(
            S1ProductManagerUi.ProductManagerApp app,
            IReadOnlyList<ProductKindMetadata> metadata)
        {
            if (app == null || app.ProductTypeContainers == null)
                return;

            EnsureContainers(app, metadata);
            ReconcileEntries(app);
        }

        internal static bool BeforeCreateEntry(
            S1ProductManagerUi.ProductManagerApp app,
            S1Product.ProductDefinition definition)
        {
            if (!TryResolveMetadata(definition, out ProductKindMetadata metadata))
                return true;

            if (!metadata.IsVisibleInProductManager)
                return false;

            bool hasContainer =
                FindManagedContainer(app, metadata.ProductKind.Id) != null;
            if (!RequiresContainerReconciliation(
                    metadata.IsVisibleInProductManager,
                    hasContainer))
            {
                return true;
            }

            EnsureContainers(app, SnapshotMetadata());
            if (FindManagedContainer(app, metadata.ProductKind.Id) != null)
                return true;

            Logger.Error(
                $"Skipped Product Manager entry '{definition.ID}' because the "
                + $"section for product kind '{metadata.ProductKind.Id}' could not be created.");
            return false;
        }

        internal static void AfterCreateEntry(
            S1ProductManagerUi.ProductManagerApp app,
            S1Product.ProductDefinition definition)
        {
            if (TryResolveMetadata(definition, out ProductKindMetadata metadata)
                && metadata.IsVisibleInProductManager)
            {
                RouteEntry(app, definition, metadata);
            }
        }

        internal static bool AllowFavouriteEntry(
            S1Product.ProductDefinition definition)
        {
            return !TryResolveMetadata(definition, out ProductKindMetadata metadata)
                   || metadata.IsVisibleInProductManager;
        }

        internal static bool AllowFavouriteRemoval()
        {
            return true;
        }

        internal static bool RequiresContainerReconciliation(
            bool isVisible,
            bool hasContainer)
        {
            return isVisible && !hasContainer;
        }

        internal static bool ShouldPurgeFavouriteEntry(
            bool hasEntry,
            bool hasDefinition)
        {
            return !hasEntry || !hasDefinition;
        }

        internal static SectionIconState CreateSectionIconState(Sprite? sprite)
        {
            Sprite? liveSprite =
                ProductKindIconLifetime.IsNullOrDestroyed(sprite)
                    ? null
                    : sprite;
            Color neutralTint = default;
            neutralTint.r = 1f;
            neutralTint.g = 1f;
            neutralTint.b = 1f;
            neutralTint.a = 1f;
            return new SectionIconState(liveSprite, neutralTint);
        }

        private static IReadOnlyList<ProductKindMetadata> SnapshotMetadata()
        {
            var result = new List<ProductKindMetadata>(
                ProductKindMetadataRegistry.All);
            return result.AsReadOnly();
        }

        private static void EnsureContainers(
            S1ProductManagerUi.ProductManagerApp app,
            IReadOnlyList<ProductKindMetadata> metadata)
        {
            var desired = new List<ProductKindMetadata>();
            var desiredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < metadata.Count; i++)
            {
                ProductKindMetadata item = metadata[i];
                if (item.IsVisibleInProductManager
                    && item.ProductKind.CompatibilityDrugType.HasValue)
                {
                    desired.Add(item);
                    desiredIds.Add(item.ProductKind.Id);
                }
            }

            desired.Sort(CompareMetadata);
            var existing = new Dictionary<
                string,
                S1ProductManagerUi.ProductTypeContainer>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = app.ProductTypeContainers.Count - 1; i >= 0; i--)
            {
                S1ProductManagerUi.ProductTypeContainer container =
                    app.ProductTypeContainers[i];
                if (!TryGetManagedId(container, out string? productKindId))
                    continue;

                if (!desiredIds.Contains(productKindId)
                    || existing.ContainsKey(productKindId))
                {
                    app.ProductTypeContainers.RemoveAt(i);
                    DestroyManagedContainer(app, container);
                    continue;
                }

                existing.Add(productKindId, container);
            }

            for (int i = 0; i < desired.Count; i++)
            {
                ProductKindMetadata item = desired[i];
                if (!existing.TryGetValue(
                        item.ProductKind.Id,
                        out S1ProductManagerUi.ProductTypeContainer? container))
                {
                    container = CreateContainer(app, item);
                    if (container == null)
                        continue;
                    existing.Add(item.ProductKind.Id, container);
                }

                ConfigureContainer(container, item);
            }

            for (int i = app.ProductTypeContainers.Count - 1; i >= 0; i--)
            {
                if (TryGetManagedId(app.ProductTypeContainers[i], out _))
                    app.ProductTypeContainers.RemoveAt(i);
            }

            for (int i = 0; i < desired.Count; i++)
            {
                if (!existing.TryGetValue(
                        desired[i].ProductKind.Id,
                        out S1ProductManagerUi.ProductTypeContainer? container))
                {
                    continue;
                }

                app.ProductTypeContainers.Add(container);
                container.transform.SetAsLastSibling();
            }
        }

        private static S1ProductManagerUi.ProductTypeContainer? CreateContainer(
            S1ProductManagerUi.ProductManagerApp app,
            ProductKindMetadata metadata)
        {
            S1ProductManagerUi.ProductTypeContainer? template = null;
            for (int i = 0; i < app.ProductTypeContainers.Count; i++)
            {
                S1ProductManagerUi.ProductTypeContainer candidate =
                    app.ProductTypeContainers[i];
                if (!TryGetManagedId(candidate, out _))
                {
                    template = candidate;
                    break;
                }
            }

            if (template == null)
            {
                Logger.Error(
                    $"Could not create Product Manager section '{metadata.ProductKind.Id}': "
                    + "no vanilla product-type container is available as a template.");
                return null;
            }

            GameObject clone = UnityObject.Instantiate(
                template.gameObject,
                template.transform.parent);
            clone.SetActive(false);
            clone.name = ManagedNamePrefix + metadata.ProductKind.Id;

            S1ProductManagerUi.ProductTypeContainer? container =
                clone.GetComponent<S1ProductManagerUi.ProductTypeContainer>();
            if (container == null)
            {
                UnityObject.DestroyImmediate(clone);
                return null;
            }

            ClearChildren(container.Enteries);
            ConfigureContainer(container, metadata);

            S1Root.UIScreen? screen = GetScreen(app);
            S1Root.UIPanel? panel = clone.GetComponent<S1Root.UIPanel>();
            if (screen == null || panel == null)
            {
                Logger.Error(
                    $"Could not create Product Manager section '{metadata.ProductKind.Id}': "
                    + "the cloned UI panel or owning screen is unavailable.");
                UnityObject.DestroyImmediate(clone);
                return null;
            }

            screen.AddPanel(panel);
            clone.SetActive(true);
            container.RefreshNoneDisplay();
            return container;
        }

        private static void ConfigureContainer(
            S1ProductManagerUi.ProductTypeContainer container,
            ProductKindMetadata metadata)
        {
            if (metadata.ProductKind.CompatibilityDrugType.HasValue)
            {
                ReflectionUtils.TrySetFieldOrProperty(
                    container,
                    "_drugType",
                    metadata.ProductKind.CompatibilityDrugType.Value.ToInternal());
            }

            Text? title = FindText(container, "Title");
            if (title != null)
            {
                title.text = metadata.DisplayName;
                title.color = metadata.Color;
            }

            Image? arrow = FindImage(container, "Arrow");
            if (arrow != null)
                arrow.color = metadata.Color;

            Image? icon = FindImage(container, "Image");
            if (icon != null)
            {
                SectionIconState iconState =
                    CreateSectionIconState(metadata.Icon);
                icon.sprite = iconState.Sprite;
                icon.color = iconState.Tint;
            }
        }

        private static void ReconcileEntries(
            S1ProductManagerUi.ProductManagerApp app)
        {
            NativeProductEntryList? entries =
                ReflectionUtils.TryGetFieldOrProperty(app, "entries")
                    as NativeProductEntryList;
            if (entries != null)
            {
                var definitions = new List<S1Product.ProductDefinition>();
                for (int i = 0; i < entries.Count; i++)
                {
                    S1Product.ProductEntry entry = entries[i];
                    if (entry != null && entry.Definition != null)
                        definitions.Add(entry.Definition);
                }

                for (int i = 0; i < definitions.Count; i++)
                {
                    if (!TryResolveMetadata(
                            definitions[i],
                            out ProductKindMetadata metadata))
                    {
                        continue;
                    }

                    if (metadata.IsVisibleInProductManager)
                        RouteEntry(app, definitions[i], metadata);
                    else
                        RemoveEntries(entries, definitions[i]);
                }
            }

            NativeProductEntryList? favourites =
                ReflectionUtils.TryGetFieldOrProperty(app, "favouriteEntries")
                    as NativeProductEntryList;
            if (favourites == null)
                return;

            for (int i = favourites.Count - 1; i >= 0; i--)
            {
                S1Product.ProductEntry entry = favourites[i];
                bool hasEntry = entry != null;
                bool hasDefinition =
                    hasEntry && entry!.Definition != null;
                if (ShouldPurgeFavouriteEntry(hasEntry, hasDefinition))
                {
                    favourites.RemoveAt(i);
                    if (hasEntry)
                        entry!.Destroy();
                    continue;
                }

                S1Product.ProductDefinition definition =
                    entry!.Definition!;
                if (!TryResolveMetadata(
                        definition,
                        out ProductKindMetadata metadata))
                {
                    continue;
                }

                if (metadata.IsVisibleInProductManager)
                {
                    RefreshEntryIcon(entry);
                    continue;
                }

                favourites.RemoveAt(i);
                entry.Destroy();
            }

            app.FavouritesContainer?.RefreshNoneDisplay();
        }

        private static void RouteEntry(
            S1ProductManagerUi.ProductManagerApp app,
            S1Product.ProductDefinition definition,
            ProductKindMetadata metadata)
        {
            S1ProductManagerUi.ProductTypeContainer? target =
                FindManagedContainer(app, metadata.ProductKind.Id);
            if (target == null)
                return;

            NativeProductEntryList? entries =
                ReflectionUtils.TryGetFieldOrProperty(app, "entries")
                    as NativeProductEntryList;
            if (entries == null)
                return;

            S1Product.ProductEntry? retained = null;
            for (int i = 0; i < entries.Count; i++)
            {
                S1Product.ProductEntry entry = entries[i];
                if (entry == null
                    || entry.Definition == null
                    || !string.Equals(
                        entry.Definition.ID,
                        definition.ID,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (retained == null)
                {
                    retained = entry;
                    continue;
                }

                entries.RemoveAt(i--);
                entry.Destroy();
            }

            if (retained != null && retained.transform.parent != target.Enteries)
                retained.transform.SetParent(target.Enteries, false);
            if (retained != null)
                RefreshEntryIcon(retained);

            RefreshContainers(app);
        }

        private static void RefreshEntryIcon(
            S1Product.ProductEntry entry)
        {
            if (entry.Icon != null
                && entry.Definition != null
                && entry.Definition.Icon != null
                && !ReferenceEquals(entry.Icon.sprite, entry.Definition.Icon))
            {
                entry.Icon.sprite = entry.Definition.Icon;
            }
        }

        private static void RemoveEntries(
            NativeProductEntryList entries,
            S1Product.ProductDefinition definition)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                S1Product.ProductEntry entry = entries[i];
                if (entry == null
                    || entry.Definition == null
                    || !string.Equals(
                        entry.Definition.ID,
                        definition.ID,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                entries.RemoveAt(i);
                entry.Destroy();
            }
        }

        private static void RefreshContainers(
            S1ProductManagerUi.ProductManagerApp app)
        {
            for (int i = 0; i < app.ProductTypeContainers.Count; i++)
                app.ProductTypeContainers[i]?.RefreshNoneDisplay();
        }

        private static bool TryResolveMetadata(
            S1Product.ProductDefinition definition,
            out ProductKindMetadata metadata)
        {
            metadata = null!;
            if (!CustomProductDefinitionRegistry.TryGetMetadata(
                    definition,
                    out CustomProductDefinitionMetadata? definitionMetadata)
                || definitionMetadata == null
                || !ProductKindMetadataRegistry.TryGet(
                    definitionMetadata.ProductKind,
                    out ProductKindMetadata? resolved)
                || resolved == null)
            {
                return false;
            }

            metadata = resolved;
            return true;
        }

        private static S1ProductManagerUi.ProductTypeContainer?
            FindManagedContainer(
                S1ProductManagerUi.ProductManagerApp app,
                string productKindId)
        {
            for (int i = 0; i < app.ProductTypeContainers.Count; i++)
            {
                S1ProductManagerUi.ProductTypeContainer candidate =
                    app.ProductTypeContainers[i];
                if (TryGetManagedId(candidate, out string? candidateId)
                    && string.Equals(
                        candidateId,
                        productKindId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool TryGetManagedId(
            S1ProductManagerUi.ProductTypeContainer? container,
            out string productKindId)
        {
            productKindId = string.Empty;
            if (container == null)
                return false;

            string name = container.gameObject.name;
            if (name == null
                || !name.StartsWith(ManagedNamePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            productKindId = name.Substring(ManagedNamePrefix.Length);
            return productKindId.Length > 0;
        }

        private static void DestroyManagedContainer(
            S1ProductManagerUi.ProductManagerApp app,
            S1ProductManagerUi.ProductTypeContainer container)
        {
            NativeProductEntryList? entries =
                ReflectionUtils.TryGetFieldOrProperty(app, "entries")
                    as NativeProductEntryList;
            if (entries != null)
            {
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    S1Product.ProductEntry entry = entries[i];
                    if (entry == null
                        || !entry.transform.IsChildOf(container.transform))
                    {
                        continue;
                    }

                    entries.RemoveAt(i);
                    entry.Destroy();
                }
            }

            S1Root.UIScreen? screen = GetScreen(app);
            S1Root.UIPanel? panel = container.GetComponent<S1Root.UIPanel>();
            if (screen != null && panel != null)
                screen.RemovePanel(panel);

            container.gameObject.SetActive(false);
            UnityObject.DestroyImmediate(container.gameObject);
        }

        private static S1Root.UIScreen? GetScreen(
            S1ProductManagerUi.ProductManagerApp app)
        {
            return ReflectionUtils.TryGetFieldOrProperty(app, "_screen")
                as S1Root.UIScreen;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityObject.DestroyImmediate(child);
            }
        }

        private static Text? FindText(
            S1ProductManagerUi.ProductTypeContainer container,
            string name)
        {
            var components = container.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (string.Equals(
                        components[i].gameObject.name,
                        name,
                        StringComparison.Ordinal))
                {
                    return components[i];
                }
            }

            return null;
        }

        private static Image? FindImage(
            S1ProductManagerUi.ProductTypeContainer container,
            string name)
        {
            var components = container.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (string.Equals(
                        components[i].gameObject.name,
                        name,
                        StringComparison.Ordinal))
                {
                    return components[i];
                }
            }

            return null;
        }

        private static int CompareMetadata(
            ProductKindMetadata left,
            ProductKindMetadata right)
        {
            int order = left.SortOrder.CompareTo(right.SortOrder);
            if (order != 0)
                return order;
            order = StringComparer.OrdinalIgnoreCase.Compare(
                left.DisplayName,
                right.DisplayName);
            return order != 0
                ? order
                : StringComparer.OrdinalIgnoreCase.Compare(
                    left.ProductKind.Id,
                    right.ProductKind.Id);
        }
    }
}
