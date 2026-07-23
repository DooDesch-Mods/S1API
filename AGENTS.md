# Repository Guidelines

## Project Structure & Module Organization
S1API.sln orchestrates two primary projects: `S1API/` for the modding API and `S1APILoader/` for the loader shim. API modules are grouped by gameplay domain (e.g., `S1API/Quests`, `S1API/Entities`, `S1API/Internal/Utils`). Public-facing docs and branding assets live under `Public/`, while DocFX sources reside in `S1API/docs/` and publish to `_site/`. Use `example.build.props` as a template to configure engine paths in a local `local.build.props`.

## Research Workflow
When implementing or extending S1API features that mirror or hook into the base game, inspect `../` first to confirm the upstream type, member, and behavior you are targeting. Search the game codebase for the relevant symbols before making API changes. Also use the `../.agents/skills/schedule-one-modding` skill when working with upstream game types so you follow the repository's modding-specific guidance and references. Example: if asked to add `onHourPass` support to `TimeManager`, look for `onHourPass` and `TimeManager` in `../` to verify naming, signatures, and call flow before editing `S1API`, and use the `schedule-one-modding` skill to guide the upstream research and integration approach.

## Build, Test, and Development Commands
- `dotnet restore S1API.sln -p:Configuration=MonoMelon` — restores the Mono graph.
- `dotnet build S1API.sln -c MonoMelon --no-restore -p:AutomateLocalDeployment=false` — builds Mono without deploying into a live game.
- `dotnet test S1API.Tests/S1API.Tests.csproj -c MonoMelon --no-restore --no-build` — runs the Mono contract and compatibility tests.
- Repeat the restore, build, and test commands with `Il2CppMelon` for the Il2Cpp graph.
- `docfx docfx.json` (run inside `S1API/`) — regenerates API documentation locally.

## Coding Style & Naming Conventions
Follow `CODING_STANDARDS.md`: namespaces mirror folders and internal frameworks live under `S1API.Internal.*`. Use PascalCase for types, methods, and public members; camelCase with a leading `_` for private fields (e.g., `_spawnDelay`). Keep arrow-bodied members concise and mark immutable data as `readonly` or `const`. All modder-facing APIs require XML `<summary>` docs, and conditional code should use the shared `#if (MONOMELON || MONOBEPINEX)` pattern.

## Testing Guidelines
`S1API.Tests/` contains xUnit contract and compatibility tests. Before opening a PR, restore, build, and test both `MonoMelon` and `Il2CppMelon` with matching configurations. Exercise affected gameplay flows in both runtimes when behavior depends on native lifecycle, networking, save/load, or rendered state.

## Commit & Pull Request Guidelines
Write imperative, single-purpose commits; lightweight prefixes such as `fix:` or `feat:` appear in history and are encouraged. Target PRs at `bleeding-edge`, include a short change narrative, reproduction or validation notes, and link any external issue. Screenshots or logs are helpful for UI or networking work. Never modify CI workflows without prior discussion.

## Release & Versioning Workflow
Always follow [`VERSIONING.md`](VERSIONING.md) for any release, hotfix, tagging, branch-planning, or version-bump work. Treat it as the authoritative release policy.

- Do not improvise an alternative release flow when `VERSIONING.md` already covers the task.
- Every shipped version must get its own `releases/x.y.z` maintenance branch exactly as described in `VERSIONING.md`.
- When publishing `x.y.z`, create `releases/x.y.z` from the exact tagged release commit immediately after the release.
- Do not assume tagging `stable` is sufficient for a release; the matching `releases/x.y.z` branch is required for the documented maintenance and NuGet publishing flow.
- For hotfixes, work from and merge back into the relevant `releases/x.y.z` branch, then backport to `stable` as `VERSIONING.md` describes.
