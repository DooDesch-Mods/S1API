# Item Icons

S1API supports loading item icons from embedded resources or AssetBundles and
capturing icons from runtime models.

## From Embedded Resources

Load sprites from embedded resources in your mod assembly with `ImageUtils`:

```csharp
using S1API.Utils;
using System.Reflection;
using UnityEngine;

var assembly = Assembly.GetExecutingAssembly();
using (var stream = assembly.GetManifestResourceStream("YourMod.Resources.my_icon.png"))
{
    if (stream != null)
    {
        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);

        var icon = ImageUtils.LoadImageRaw(data);

        if (icon != null)
        {
            var item = ItemCreator.CreateBuilder()
                .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Tools)
                .WithIcon(icon)
                .Build();
        }
    }
}
```

## From AssetBundle

```csharp
var bundle = AssetBundle.LoadFromFile("path/to/bundle");
var icon = bundle.LoadAsset<Sprite>("my_icon");

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Tools)
    .WithIcon(icon)
    .Build();
```

## From a Runtime Model

Use `IconFactory.GenerateIconSprite` when your item model is already loaded:

```csharp
using S1API.Rendering;

var icon = IconFactory.GenerateIconSprite(itemModel.transform);
if (icon != null)
{
    itemDefinition.Icon = icon;
}
```

The sprite overloads return a durable UI sprite. S1API normalizes the native
capture before returning it, so mod code does not need to encode and reload the
captured texture.

Assigning `ItemDefinition.Icon` also refreshes inventory slots and shop listings
that are already displaying that item. This covers icons generated after a save
has restored existing stacks. Mod code does not need to force an inventory move
or manually refresh those UIs.

The lower-level `IconFactory.GenerateIcon` texture overloads remain available
when direct texture ownership is required. Callers own textures returned by
those overloads.

## Tips

- Prefer square icons at 128x128 or larger
- Load and validate sprites before building the item definition
- Prefer `GenerateIconSprite` over manually converting a generated texture
- Use embedded resources for small self-contained mods
- Use AssetBundles when the icon ships alongside other art assets

## See Also

- [Item Registration & Basics](item-registration-basics.md)
- [Equippable Items](equippable-items.md)
- <xref:S1API.Items.StorableItemDefinitionBuilder>
