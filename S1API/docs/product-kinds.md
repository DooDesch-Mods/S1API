# Logical Product Kinds

`ProductKindDescriptor` gives mods a stable logical identity for product categories without allocating native enum values or waiting for `ProductManager`.

## Register a product kind

Use a namespaced ID so unrelated mods cannot collide:

```csharp
using S1API.Products;

ProductKindDescriptor mdma = new ProductKindDescriptorBuilder("examplemod:mdma")
    .WithCompatibilityDrugType(DrugType.MDMA)
    .Build();
```

IDs use `<namespace>:<name>` form. Comparisons are case-insensitive, and the name may contain `/` for optional grouping.

`CompatibilityDrugType` is optional metadata for consumers that still need a vanilla category. It is not the descriptor's identity and does not create a native product definition or add support to native systems.

## Look up registered kinds

```csharp
ProductKindDescriptor? mdma = ProductKindRegistry.Get("EXAMPLEMOD:MDMA");

if (ProductKindRegistry.TryGet("examplemod:mdma", out ProductKindDescriptor? found))
{
    // Use the immutable logical descriptor.
}

IReadOnlyCollection<ProductKindDescriptor> allKinds = ProductKindRegistry.All;
```

Registry snapshots and descriptors are read-only. Registrations remain available across scene and save transitions for the lifetime of the process.

## Duplicate registrations

Building the same case-insensitive ID with the same compatibility metadata returns the original descriptor. Building that ID with different metadata throws an `InvalidOperationException` that identifies the conflict.

This makes per-load setup calls safe when they repeat an equivalent registration while rejecting two mods that claim the same logical ID differently.
