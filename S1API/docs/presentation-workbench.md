# Presentation workbench

The presentation workbench is an in-game authoring tool for tuning visual
position, rotation, scale, and native icon framing without rebuilding the mod
after every value change. It provides three context-accurate previews:

- **First person** clones the visual into the local player's real viewmodel
  container and assigns the `Viewmodel` layer.
- **Avatar** aligns the visual to the selected hand on the local player's
  visible avatar and renders the result through a dedicated preview camera.
- **Icon** captures the visual through the base game's native `IconGenerator`
  rig. Position is intentionally omitted because `IconFactory` centers the
  renderer bounds before capture.

The workbench never equips an inventory slot, registers a preview object,
persists values, or sends an RPC. Closing it or unloading the scene destroys
all preview objects and restores movement, inventory, cursor, camera, avatar,
and previously visible equippable state.

## Register a definition

Create one immutable definition during mod initialization and register it under
a stable namespaced ID:

```csharp
using S1API.Items;
using S1API.Rendering;
using UnityEngine;

GameObject firstPersonSource = LoadFirstPersonSource();
GameObject avatarSource = LoadAvatarSource();
GameObject iconSource = LoadIconSource();

PresentationWorkbenchDefinition definition =
    new PresentationWorkbenchDefinitionBuilder(
            "example.mod:items/focus-tablet",
            "Focus tablet")
        .WithFirstPersonPreview(
            () => firstPersonSource,
            new PresentationWorkbenchTransform(
                Vector3.zero,
                new Vector3(0f, 180f, 0f),
                Vector3.one * 1.8f))
        .WithAvatarPreview(
            () => avatarSource,
            new PresentationWorkbenchTransform(
                new Vector3(0f, -0.16f, 0f),
                new Vector3(0f, 270f, 0f),
                Vector3.one * 2.25f),
            hand: AvatarHand.Right,
            animationTrigger: "RightArm_Hold_ClosedHand")
        .WithIconPreview(
            () => iconSource,
            initialEulerAngles: new Vector3(18f, -32f, 0f),
            fitToCamera: true,
            cameraFill: 0.8f)
        .Build();

PresentationWorkbenchRegistry.Register("example.mod", definition);
```

Providers return reusable prefab sources. The workbench clones each result and
does not mutate the provider-owned object. Registering the same definition
instance again under the same owner is idempotent. IDs and owner IDs are
case-insensitive; another owner cannot replace an existing ID.

The animation trigger is included as authoring metadata for the eventual
`AvatarEquippable`. The preview does not apply it because changing the local
avatar animator could leak state beyond the authoring session.

## Open and edit

Open the native developer console after the local player has spawned:

```text
presentation_workbench example.mod:items/focus-tablet
```

Other command forms are:

```text
presentation_workbench list
presentation_workbench close
```

`list` reports explicit workbench definitions. A product presentation profile
can still be opened directly by its stable product ID even though it does not
appear in that list.

Select a supported context, enter numeric values, and commit each field with
Enter or by moving focus. Icon changes are debounced before the native rig is
captured again. `Fit` enables bounds-based automatic scale fitting;
`cameraFill` controls how much of the native camera's vertical view the fitted
model occupies. Values greater than `1` intentionally crop the model.

Use **Copy C#** to copy an invariant-culture
`PresentationWorkbenchTransform`, `ProductPresentationTransform`, or
`WithIconPreview(...)` fragment to the clipboard. The workbench does not write
source files or modify the registered definition.

## Product presentation profiles

Profiles registered through `ProductPresentationProfileRegistry` are exposed
automatically:

```text
presentation_workbench example.mod:products/focus-tablet
```

The held visual supplies the first-person preview. The avatar preview uses the
avatar-held override when configured and otherwise preserves the legacy held
visual and transform fallback. Generated loose icons use the same rotation,
scale, fit, fill, and size settings as runtime icon generation.

Use a separate avatar pose when a shared source needs different first- and
third-person placement:

```csharp
ProductPresentationProfile profile =
    new ProductPresentationProfileBuilder()
        .WithHeldVisual(() => visual, firstPersonPose)
        .WithAvatarHeldTransform(avatarPose)
        .WithGeneratedIconFromLooseVisual(
            size: 512,
            fitToCamera: true,
            cameraFill: 0.8f)
        .Build();
```

Use `WithAvatarHeldVisual(provider, avatarPose)` when the avatar also needs a
different source. Omitting both avatar-specific methods keeps the existing
held visual and pose behavior.

## Runtime requirements and limits

The workbench requires a spawned local player and the render-ready services in
the Main or Tutorial scene. It is a local authoring aid, not a multiplayer
content transfer or runtime customization protocol.

The first-person preview uses the game's actual viewmodel container, so it is
the authoritative camera-space preview. The avatar panel isolates the player
layer against a neutral background; it does not simulate every locomotion or
animation state. Icon preview uses the authoritative native capture pipeline,
including automatic centering and optional bounds fitting.
