# Generic Custom Products

`CustomProductDefinitionBuilder` registers the first generic S1API product
definition that does not belong to a native weed, methamphetamine, cocaine, or
shroom family. Use it for same-mod products such as tablets, powders, or other
fixed definitions that should not enter native mix generation.

## Register before save restoration

Create the logical kind once, then build the definition during
`GameLifecycle.OnPreLoad`. Every host and client must run the same mod and
register the same stable IDs before native item save data is restored.

```csharp
using System;
using S1API.Items;
using S1API.Lifecycle;
using S1API.Products;
using S1API.Properties;

ProductKind focusTabletKind = new ProductKindBuilder(
        "example.mod:focus-tablet")
    .WithCompatibilityDrugType(DrugType.MDMA)
    .Build();

CustomProductDefinition? focusTablet = null;

GameLifecycle.OnPreLoad += () =>
{
    if (focusTablet != null)
        return;

    var representationTemplate =
        ItemManager.GetDefinition("weed") as ProductDefinition;
    var baggie = ProductPopulator.GetPackaging("baggie");

    if (representationTemplate == null)
    {
        throw new InvalidOperationException(
            "Cannot register example.mod:products/focus-tablet: " +
            "the representation template 'weed' is unavailable during OnPreLoad.");
    }

    if (baggie == null)
    {
        throw new InvalidOperationException(
            "Cannot register example.mod:products/focus-tablet: " +
            "the required packaging 'baggie' is unavailable during OnPreLoad.");
    }

    focusTablet = CustomProductItemCreator
        .CreateBuilder(
            "example.mod:products/focus-tablet",
            focusTabletKind)
        .WithName("Focus Tablet")
        .WithDescription("A fixed, same-mod generic product.")
        .WithProductPrice(175f)
        .WithProperties(Property.Focused)
        .WithLegalStatus(LegalStatus.Illegal)
        .WithBaseAddictiveness(0.2f)
        .WithDefaultQuality(Quality.Standard)
        .WithValidPackaging(baggie)
        .WithRepresentationsFrom(representationTemplate)
        .WithEffectDurations(
            playerSeconds: 120,
            npcSeconds: 180)
        .Build();
};
```

The compatibility drug type is required because the native generic
`ProductDefinition` save representation expects one. It does not turn the
logical kind into a native enum member, native family definition, or mixable
product.

## Builder contract and defaults

- Product and product-kind IDs are durable, case-insensitive, and namespaced.
  Do not change a published ID or reuse it with another definition.
- `WithName`, `WithProductPrice`, and `WithRepresentationsFrom` are required.
- Prices follow the native product-manager policy: finite values are clamped to
  1 through 999 and rounded to the nearest integer.
- Description defaults to empty, legal status to `Illegal`, base addictiveness
  to `0`, quality to `Standard`, properties to none, and valid packaging to none.
- Up to eight distinct properties may be supplied. Every property must resolve
  to a vanilla or registered custom native effect.
- Packaging is deduplicated case-insensitively and stored in ascending capacity
  order. `CustomProductDefinition.CreatePackagedInstance` returns `null` for
  packaging outside that policy.
- When effect durations are omitted, they are borrowed from the representation
  template.
- A successful builder is immutable. Repeated `Build()` calls on that builder
  return the same wrapper. Another builder claiming the same ID fails
  deterministically.

The repeated-`Build()` guarantee applies only to that builder instance. As with
existing typed product wrappers, registry and Product Manager lookups may return
a different wrapper for the same native definition. Compare the stable ID or
native-backed item equality; do not use `ReferenceEquals` across lookup calls.

The template's icon, stored/held representations, functional product,
consumption animation, and item UI references are shared rather than cloned.
The builder does not export, embed, or redistribute those game assets. Its
station representation is deliberately not copied unless a presentation
profile supplies a station visual.

## Custom presentation profiles

`ProductPresentationProfile` lets a generic custom product use mod-owned visuals
without subclassing a native drug family. Register the profile by stable product
ID before building the definition:

```csharp
using S1API.Products;
using UnityEngine;

// Load once through S1API.AssetBundles, MAPI's embedded GLB loader, or another
// local mod-owned asset path. Providers should return this reusable prefab source.
GameObject pillVisual = LoadPillVisual();

ProductPresentationProfile pillProfile =
    new ProductPresentationProfileBuilder()
        .WithLooseVisual(() => pillVisual)
        .WithGeneratedIconFromLooseVisual(size: 512)
        .Require(
            ProductPresentationContext.Stored,
            ProductPresentationContext.Held,
            ProductPresentationContext.Station,
            ProductPresentationContext.FunctionalProduct)
        .Build();

ProductPresentationProfileRegistry.RegisterForProduct(
    ownerId: "example.mod",
    productId: "example.mod:products/focus-tablet",
    profile: pillProfile);
```

The loose visual is the deterministic default for the stored, held, station,
and functional-product contexts. Providers may author the desired root
transform directly. For reusable source objects, pass a
`ProductPresentationTransform` to set an explicit local position, Euler
rotation, and scale on S1API's cloned visual root:

```csharp
var pillPose =
    new ProductPresentationTransform(
        localPosition: Vector3.zero,
        localEulerAngles:
            (Quaternion.Euler(78f, 0f, -8f) *
             Quaternion.Euler(0f, 90f, 0f)).eulerAngles,
        localScale: Vector3.one * 0.06f);

ProductPresentationProfile profile =
    new ProductPresentationProfileBuilder()
        .WithLooseVisual(() => pillVisual, pillPose)
        .WithHeldVisual(() => heldPillVisual, heldPose)
        .WithStationVisual(() => stationPillVisual, stationPose)
        .WithIcon(() => pillIcon)
        .WithConsumptionPrefab(() => pillConsumeAnimationPrefab)
        .Build();
```

Visual providers return a `GameObject` prefab source. S1API clones the source
and preserves its root local position, rotation, and scale unless an explicit
presentation transform overrides them. A loose transform follows the loose
provider into fallback contexts; a context-specific provider and transform
take precedence. S1API also clones the representation template's native
stored-item, equippable, station-item, and functional-product scaffolds,
replacing only their family-specific visual setter. This keeps native storage
footprints, station modules, draggable behavior, first-person equip behavior,
and consumption wiring intact.

The held context also creates a third-person `AvatarEquippable` under the
deterministic resource path
`S1API/ProductPresentation/{productId}/Held`. Every peer must register the same
product and profile locally before that path is received over the network.
S1API does not transmit the mesh, materials, textures, definition, or profile.

An explicit consumption provider must return a prefab containing the native
`ProductConsumeAnimation` component. Generated icons reuse the base game's
`IconGenerator` through `S1API.Rendering.IconFactory`; explicit sprites can be
supplied with `WithIcon`. S1API preserves the representation template's icon
while capture is queued, waits for the native `@IconGenerator` rig, yields
through a complete render frame, and rejects transparent cold-start captures.
The loading screen stays open until queued product icons complete or reach the
bounded retry timeout. `MugshotGenerator` remains reserved for avatar/accessory
previews. The generated icon is the loose inventory icon only.
`ProductIconManager` packaging combinations are intentionally unchanged.

Automatic icon fitting targets 72% of the native thumbnail camera by default.
Adjust the framing or preserve the authored scale with the additive overload:

```csharp
.WithGeneratedIconFromLooseVisual(
    size: 512,
    fitToCamera: true,
    cameraFill: 0.82f)
```

Set `fitToCamera: false` when the provider's scale is already authored for the
base-game thumbnail rig. The loose presentation transform controls icon
rotation; `cameraFill` controls only automatic scale fitting. Direct
`IconFactory.GenerateIcon` and `GenerateIconSprite` overloads expose the same
framing controls.

Profiles may instead be registered by `ProductKind` with
`RegisterForProductKind`. A product-ID profile always wins over a kind profile,
and both key types are case-insensitive. The first owner wins; an owner may
repeat the same profile registration idempotently but cannot silently replace
it with another profile.

Missing optional providers preserve the original template reference. Provider
exceptions, null results, incompatible native scaffolds, and icon-rendering
failures also preserve that reference and log a warning. `Require(...)` changes
those conditions into an actionable registration error once the relevant
native scene service is available. Profiles and generated prefabs are retained
for the process lifetime and reapplied during pre-load and load-complete
restoration.

Presentation profiles affect only generic custom products registered with
`CustomProductDefinitionBuilder`. Vanilla definitions, native-family builders,
and legacy custom registrations are not modified.

## Loose and packaged instances

```csharp
ProductInstance loose =
    focusTablet.CreateInstance(
        quantity: 2,
        quality: Quality.Premium);

ProductInstance? packaged =
    focusTablet.CreatePackagedInstance(
        quantity: 1,
        packaging: baggie,
        quality: Quality.Standard);
```

Loose and packaged native item data use the product ID, quality, and packaging
ID. That data survives ordinary save/load and network serialization when the
same definition is registered before restoration on every peer.

## Discovery, listing, and shops are opt-in

`Build()` registers the definition and initial price only. It does not discover
the product, list it in Product Manager, add it to shops, or put it in the
native created-products list.

On the authoritative host/server, discovery is explicit:

```csharp
GameLifecycle.OnLoadComplete += () =>
{
    CustomProductDefinition definition = focusTablet ??
        throw new InvalidOperationException(
            "The custom product was not registered during OnPreLoad.");

    definition.Discover();
    definition.SetListed();
};
```

Definitions created during `OnPreLoad` must defer discovery and listing until
`OnLoadComplete`, when `ProductManager.Instance` and the network session are
available. Subscribe to these lifecycle events once; `SetListed` throws when
called before Product Manager initialization.

Pass `listForSale: true` to `Discover` only when discovery should also list the
product. Shop inventory remains separate; call the existing
`S1API.Shops.ShopManager` APIs explicitly after registration.

## Supported and unsupported behavior

Supported in this milestone:

- process-lifetime definition ownership and scene re-registration through the
  S1API custom-product lifecycle registry;
- stable loose and packaged item instances;
- fixed name, description, price, effects, legality, addictiveness, quality,
  packaging policy, borrowed consumption references, and optional mod-owned
  loose presentation profiles;
- same-mod save/load and native item network serialization; and
- explicit discovery, Product Manager listing, and existing shop integration.

Not supported:

- mixing, generated variants, native family conversion, or production-station
  recipes;
- packaged contents, composite package icons, or packaging-content save data;
- custom packaging definitions or Product Manager UI categories;
- definition transfer to a peer without the defining mod; or
- recovery of a saved custom item after its mod or stable definition ID is
  removed.

Do not place a generic custom product in native mixing flows. The compatibility
drug type only satisfies native product-item and save assumptions; S1API does
not register mix recipes or generated outputs for this definition.

## Compatibility

This API is additive. Existing `ProductDefinition`, `ProductInstance`,
`ProductDefinitionWrapper`, native-family builders, defaults, exception
behavior, save IDs, and network payloads are unchanged. The wrapper factory
selects `CustomProductDefinition` only for definitions registered with the new
builder metadata; all previous generic and native-family fallback behavior
remains intact. Presentation-profile registration is opt-in, and definitions
without a resolved profile keep the exact representation references selected by
`WithRepresentationsFrom`.
