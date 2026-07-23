# Weed Variant Smoke Probe

This opt-in probe verifies the narrow native weed-variant lifecycle:

- the host sees another lobby member before loading;
- `Build()` returns a typed `WeedDefinition`;
- registry, `AllProducts`, discovery, price, icon, properties, appearance, and
  duplicate behavior are present;
- a client already connected observes native creation;
- a client joining after creation observes the replay; and
- the saved product restores in a fresh process that never calls `Build()`.

The PowerShell runner creates separate isolated installs for the host, observer,
and late-join roles. It requires explicit local paths to a Goldberg/GSE
`steam_api64.dll` and the matching LocalLobby DLL. It does not download either
dependency.

```powershell
pwsh tests/Smoke/Run-WeedVariantSmoke.ps1 `
  -Runtime MonoMelon `
  -GoldbergSteamApiPath C:\test-deps\goldberg\steam_api64.dll `
  -LocalLobbyDllPath C:\test-deps\LocalLobby-Mono.dll
```

Use `Il2CppMelon` with `LocalLobby-IL2CPP.dll` for the IL2CPP lane. Results,
role-specific logs, the disposable save, and the run token are written under a
unique `S1API.WeedVariantSmoke` directory on the source game's volume unless
`-OutputRoot` is provided. A custom output root must remain on that volume so
large immutable game files can be hard-linked into each isolated role.

Do not add the generated installations, emulator, game files, logs, saves, or
test outputs to the repository.
