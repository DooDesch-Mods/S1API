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
station representation is deliberately not copied.

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
  packaging policy, and borrowed consumption references;
- same-mod save/load and native item network serialization; and
- explicit discovery, Product Manager listing, and existing shop integration.

Not supported:

- mixing, generated variants, native family conversion, or production-station
  recipes;
- custom models, icons, loose/stored/held providers, consumption providers, or
  Product Manager UI categories;
- packaged contents, composite package icons, or packaging-content save data;
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
remains intact.
