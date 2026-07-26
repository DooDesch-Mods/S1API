# Stations

This page documents station-related APIs (things that are not items themselves, but interact with item definitions and station UI).

## Chemistry Station Recipes

Register **Chemistry Station** recipes (i.e., `StationRecipe`) via a builder API.

Important notes:
- Use `WithRecipeId(...)` with a stable, mod-namespaced ID such as `my-mod:alternate-route` when more than one recipe produces the same product and quantity.
- Explicit IDs must use exactly one `:` and may contain ASCII letters, numbers, `.`, `_`, or `-` in each segment. Each segment must contain at least one letter or number. Validation occurs during `Build()`.
- If `WithRecipeId(...)` is omitted, the existing `"{qty}x{productId}"` ID is preserved exactly.
- Recipe IDs are matched case-insensitively. If another recipe with the same ID is already registered, S1API will **warn + skip** (first registration wins).
- Ingredient items must exist and have a valid `StationItem` (the builder throws if not).
- Recipes are discovered and unlocked by default for compatibility. Use
  `WithInitialAvailability(false, false)` for progression-controlled recipes,
  then call `SetAvailability(...)` on the returned recipe after loading the
  mod's progression state on each peer.
- Recommended timing: register recipes during `GameLifecycle.OnPreLoad` (late registration is supported; it will appear the next time the Chemistry Station UI is opened).

### Example (recommended timing)

```csharp
using MelonLoader;
using S1API.Lifecycle;
using S1API.Stations;
using UnityEngine;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        GameLifecycle.OnPreLoad += RegisterChemistryRecipes;
    }

    private static void RegisterChemistryRecipes()
    {
        ChemistryStationRecipes.CreateAndRegister(b => b
            .WithRecipeId("my-mod:custom-product")
            .WithTitle("My Custom Recipe")
            .WithCookTimeMinutes(10)
            .WithFinalLiquidColor(new Color(0.2f, 0.8f, 0.4f, 1f))
            // Product item must already exist in the registry (base-game or custom item)
            .WithProduct(itemId: "mymod_custom_product_item", quantity: 5)
            // Ingredient item(s) must have a StationItem (station-usable items)
            .WithIngredient(itemId: "ingredient_item_id", quantity: 1)
            .WithIngredientOptions(new[] { "ingredient_variant_a", "ingredient_variant_b" }, quantity: 1)
        );
    }
}
```

### Progression-controlled recipes

Keep the returned wrapper when a recipe should appear after a rank, quest, or
other saved milestone:

```csharp
ChemistryStationRecipe recipe =
    ChemistryStationRecipes.CreateAndRegister(b => b
        .WithRecipeId("my-mod:late-game-reaction")
        .WithInitialAvailability(
            isDiscovered: false,
            isUnlocked: false)
        .WithTitle("Late-game Reaction")
        .WithProduct("mymod_output", quantity: 1)
        .WithIngredient("mymod_precursor", quantity: 1));

// Reapply this on every peer after the relevant progression state loads.
recipe.SetAvailability(
    isDiscovered: progression.ReactionKnown,
    isUnlocked: progression.ReactionKnown);
```

`IsDiscovered` and `IsUnlocked` report the live local state. S1API does not
invent persistence or networking for a mod's progression rule; the owning mod
reapplies its saved decision on each peer.

### Alternate recipes for the same product

Explicit IDs let two ingredient routes produce the same output and quantity without colliding:

```csharp
ChemistryStationRecipes.CreateAndRegister(b => b
    .WithRecipeId("my-mod:coolant-standard-route")
    .WithTitle("Standard Coolant")
    .WithProduct("mymod_coolant", quantity: 2)
    .WithIngredient("mymod_base_fluid", quantity: 1)
    .WithIngredient("mymod_standard_stabilizer", quantity: 1));

ChemistryStationRecipes.CreateAndRegister(b => b
    .WithRecipeId("my-mod:coolant-reclaimed-route")
    .WithTitle("Reclaimed Coolant")
    .WithProduct("mymod_coolant", quantity: 2)
    .WithIngredient("mymod_reclaimed_fluid", quantity: 1)
    .WithIngredient("mymod_recycling_agent", quantity: 1));
```

### Intermediate products

Recipe IDs identify recipes, not products. A mod can register one recipe for an intermediate item and another recipe that consumes it:

```csharp
ChemistryStationRecipes.CreateAndRegister(b => b
    .WithRecipeId("my-mod:refined-catalyst")
    .WithTitle("Refined Catalyst")
    .WithProduct("mymod_refined_catalyst", quantity: 1)
    .WithIngredient("mymod_raw_catalyst", quantity: 2));

ChemistryStationRecipes.CreateAndRegister(b => b
    .WithRecipeId("my-mod:finished-coolant")
    .WithTitle("Finished Coolant")
    .WithProduct("mymod_finished_coolant", quantity: 1)
    .WithIngredient("mymod_refined_catalyst", quantity: 1)
    .WithIngredient("mymod_base_fluid", quantity: 1));
```

## Station Items for Custom Ingredients

Some station/minigame tasks (like Chemistry) spawn ingredient props by instantiating `StorableItemDefinition.StationItem`.
If an ingredient item has no StationItem, the game will log errors and may skip that ingredient.

For runtime/custom items, set a StationItem prefab when you build the item:

```csharp
using S1API.Items;

// A prefab GameObject that has a StationItem component (typically loaded from an AssetBundle)
GameObject myIngredientStationItemPrefab = ...;

var ingredient = ItemCreator.CreateBuilder()
    .WithBasicInfo("mymod_custom_ingredient", "Custom Ingredient", "Used in stations.", ItemCategory.Consumable)
    .WithStationItem(myIngredientStationItemPrefab)
    .Build();
```

## See Also

- <xref:S1API.Stations.ChemistryStationRecipeBuilder> - Chemistry Station recipe builder API
- <xref:S1API.Stations.ChemistryStationRecipes> - Chemistry Station recipe registry API

