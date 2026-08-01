# Native Weed Variants

S1API can create marijuana-family weed variants through Schedule One's native
product creator. This deliberately narrow API keeps the game's own registration,
pricing, discovery, icon, save, RPC, and late-join behavior.

## Create a variant

Create the variant on the host after the save has loaded:

```csharp
using S1API.Lifecycle;
using S1API.Products;
using S1API.Properties;
using UnityEngine;

GameLifecycle.OnLoadComplete += () =>
{
    WeedDefinition calmKush = WeedItemCreator
        .CreateBuilder("example.mod:calm-kush")
        .WithName("Calm Kush")
        .WithProperties(Property.Calming, Property.Munchies)
        .WithAppearance(new WeedAppearanceSettings(
            new Color32(104, 156, 82, 255),
            new Color32(153, 108, 189, 255),
            new Color32(63, 124, 54, 255),
            new Color32(96, 74, 48, 255)))
        .Build();
};
```

`WithAppearance(...)` is optional. When it is omitted, the native creator
generates the weed colors from its properties.

## Builder contract

- The ID must be stable and namespaced, such as `example.mod:calm-kush`.
- IDs are compared by the native registry without regard to case. Do not publish
  two variants whose IDs differ only by casing.
- A non-empty name and between one and eight distinct, resolvable properties are
  required.
- The family is always marijuana. There is intentionally no `WithDrugType(...)`.
- `Build()` requires the native `ProductManager` and item registry. Use
  `GameLifecycle.OnLoadComplete` or a later host-side lifecycle point.
- Rebuilding an existing weed ID returns a typed `WeedDefinition` around the
  existing native definition. The first successfully created configuration wins.
- An ID already owned by a non-weed item fails with an
  `InvalidOperationException`.

The return value is the same `S1API.Products.WeedDefinition` wrapper used by
`ProductDefinitionWrapper` and existing product APIs.

## Native lifecycle behavior

The builder calls the game's `CreateWeed_Server` path. It does not inject product
lists or maintain a parallel registry. The native creator therefore remains
responsible for:

- the item registry and `ProductManager.AllProducts`;
- product names and the native, property-derived market price;
- discovery and generated product icons;
- the created-product save representation;
- observer RPC creation for connected clients; and
- replaying created products to clients that join later.

In multiplayer, create the variant once on the authoritative host. A lobby alone
does not prove replication: test an observing client after the host loads, and
test a client that joins after creation.

## Current scope

This milestone creates native-family weed variants only. It does not add generic
drug families, MDMA or heroin creation, custom models or icons, package contents,
custom persistence or network protocols, custom mixing behavior, shops, or
stations.

## Compatibility

This API is additive. It does not replace the existing product wrappers,
`ProductManager`, `ProductDefinitionWrapper`, or item/ingredient builders.
