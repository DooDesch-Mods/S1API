# Cutscenes

`S1API.Cutscenes` provides a small, cross-runtime API for local cutscenes on Mono and IL2CPP. The native game cutscene manages player state, input, camera overrides, and FOV restoration. Your mod sets the duration and updates S1API's managed camera control each frame.

This API controls presentation for the local player only. It does not synchronize multiplayer state or provide a timeline, keyframes, NPC animation, or a cutscene queue. Your mod still decides when to play a cutscene and how to move its NPCs, vehicles, or props.

## Fixed camera

Every builder requires a stable, mod-namespaced ID, a positive duration, and an initial camera pose. The display name and FOV override are optional.

```csharp
using S1API.Cutscenes;
using UnityEngine;

CutsceneHandle handle = new CutsceneBuilder("my-mod:warehouse-intro")
    .WithName("Warehouse intro")
    .WithDuration(3f)
    .WithFov(60f)
    .WithInitialCamera(cameraPosition, cameraRotation)
    .OnEnded(reason => MelonLogger.Msg($"Cutscene ended: {reason}"))
    .Build();

if (!handle.Play())
{
    MelonLogger.Warning("The cutscene could not start.");
}
```

`Play()` returns `false` when the required runtime objects are unavailable or another S1API cutscene is already playing. A failed start cleans up any native state it created and does not leave an active S1API handle behind.

## Moving the camera

`WithCameraUpdate` receives a `CutsceneFrame`. The callback advances with `Time.unscaledDeltaTime` and provides elapsed time, normalized progress, and a `CutsceneCamera` that does not expose Mono- or IL2CPP-specific game types.

```csharp
CutsceneHandle handle = new CutsceneBuilder("my-mod:crew-arrival")
    .WithName("Crew arrival")
    .WithDuration(10f)
    .WithFov(60f)
    .WithInitialCamera(startPosition, startRotation)
    .WithCameraUpdate(frame =>
    {
        Vector3 position = Vector3.Lerp(startPosition, endPosition, frame.Progress);
        Vector3 target = Vector3.Lerp(startTarget, endTarget, frame.Progress);
        frame.Camera.SetPositionAndRotation(
            position,
            Quaternion.LookRotation(target - position, Vector3.up));
    })
    .Build();
```

The last camera callback before normal completion receives `Progress == 1f`, so it can apply the exact end pose. If the started or camera callback throws, S1API restores the native camera state and ends the cutscene with `CutsceneEndReason.Failed`.

## Stopping, skipping, and cleanup

`Stop()` ends playback with `Stopped`. `Skip()` ends it with `Skipped`. Both methods are idempotent: only the call that ends active playback returns `true`.

```csharp
if (shouldCancel)
{
    handle.Stop();
}
else if (shouldSkip)
{
    handle.Skip();
}
```

Only one S1API-managed local cutscene can play at a time. Use `CutsceneManager.Active`, `CutsceneManager.IsPlaying`, or `CutsceneManager.StopActive()` to inspect or stop it.

An active cutscene ends with `SceneChanged` when its scene unloads. Deinitializing S1API or disposing a playing handle ends it with `Stopped`. Every path removes the native camera, input, and FOV overrides before clearing `CutsceneManager.Active` and invoking the end callback. Exceptions from the end callback are logged after cleanup and do not reactivate the handle.

## Optional presentation helpers

The builder includes a few small presentation helpers:

```csharp
CutsceneHandle handle = new CutsceneBuilder("my-mod:shop-reveal")
    .WithDuration(6f)
    .WithInitialCamera(startPosition, startRotation)
    .WithFadeIn(0.35f)
    .WithFadeOut(0.35f)
    .WithHoldToSkip(0.5f)
    .WithTitleCard("THE NEW SHOP", 2f)
    .Build();
```

- `WithFadeIn` fades in from black.
- `WithFadeIn(fadeSeconds, delaySeconds)` keeps the owned overlay black until the delay elapses, then starts the fade.
- `WithFadeOut` fades to black near normal completion, then releases the overlay.
- `WithHoldToSkip` displays a local skip prompt and requires the player to hold the primary-click input.
- `WithTitleCard` displays centered text near the start of playback.

S1API closes the black overlay only when it opened that overlay from a hidden state. If another game system already has the overlay open, the cutscene does not take ownership or close it when playback ends.
