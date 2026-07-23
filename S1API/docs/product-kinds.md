# Logical Product Kinds

`ProductKind` gives mods a stable logical identity for product categories without allocating native enum values or waiting for `ProductManager`.

## Register a product kind

Use a namespaced ID so unrelated mods cannot collide:

```csharp
using S1API.Products;

ProductKind mdma = new ProductKindBuilder("examplemod:mdma")
    .WithCompatibilityDrugType(DrugType.MDMA)
    .Build();
```

IDs use `<namespace>:<name>` form. Comparisons are case-insensitive, and the name may contain `/` for optional grouping.

`CompatibilityDrugType` is optional metadata for consumers that still need a vanilla category. It is not the product kind's identity and does not create a native product definition or add support to native systems.

## Look up registered kinds

```csharp
ProductKind? mdma = ProductKindRegistry.Get("EXAMPLEMOD:MDMA");

if (ProductKindRegistry.TryGet("examplemod:mdma", out ProductKind? found))
{
    // Use the immutable logical product kind.
}

IReadOnlyCollection<ProductKind> allKinds = ProductKindRegistry.All;
```

Registry snapshots and product kinds are read-only. Registrations remain available across scene and save transitions for the lifetime of the process.

## Duplicate registrations

Building the same case-insensitive ID with the same compatibility metadata returns the original `ProductKind`. Building that ID with different metadata throws an `InvalidOperationException` that identifies the conflict.

This makes per-load setup calls safe when they repeat an equivalent registration while rejecting two mods that claim the same logical ID differently.
