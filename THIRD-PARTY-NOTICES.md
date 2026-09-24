# Third-party notices

The player release (`RevolutionIdleAP-vX.Y.Z.zip`) bundles third-party software so that the mod
works without a separate BepInEx install. Each component remains under its own licence and the
copyright of its own authors. Full licence texts are in [`licenses/`](licenses/), which is included
in the release.

Only original work in this repository is covered by [`LICENSE`](LICENSE) (MIT).

**No Revolution Idle code or assets are redistributed.** The mod is built against interop
assemblies generated from your own installed copy of the game, on your own machine, the first time
you launch it — which is why the first launch takes a few minutes. No part of `GameAssembly.dll`,
`global-metadata.dat`, or any game asset appears in this repository or in the release.

## Bundled components

| Component | Licence | Upstream |
|---|---|---|
| BepInEx (`BepInEx.*`) — **modified**, see below | LGPL-2.1 | https://github.com/BepInEx/BepInEx |
| UnityDoorstop (`winhttp.dll`, `doorstop_config.ini`) | LGPL-2.1 | https://github.com/NeighTools/UnityDoorstop |
| Il2CppInterop (`Il2CppInterop.*`) | LGPL-3.0 | https://github.com/BepInEx/Il2CppInterop |
| HarmonyX (`0Harmony.dll`) | MIT | https://github.com/BepInEx/HarmonyX |
| Cpp2IL (`Cpp2IL.Core`, `LibCpp2IL`, `StableNameDotNet`, `WasmDisassembler`) — **rebuilt**, see below | MIT | https://github.com/SamboyCoding/Cpp2IL |
| Disarm | MIT | https://github.com/SamboyCoding/Disarm |
| AsmResolver (`AsmResolver.*`) | MIT | https://github.com/Washi1337/AsmResolver |
| AssetRipper.CIL, AssetRipper.Primitives | MIT | https://github.com/AssetRipper |
| Mono.Cecil (`Mono.Cecil*`) | MIT | https://github.com/jbevain/cecil |
| MonoMod (`MonoMod.*`) | MIT | https://github.com/MonoMod/MonoMod |
| Iced | MIT | https://github.com/icedland/iced |
| Capstone.NET (`Gee.External.Capstone`) | MIT | https://github.com/9ee1/Capstone.NET |
| SemanticVersioning | MIT | https://github.com/adamreeve/semver.net |
| Dobby (`dobby.dll`) | Apache-2.0 | https://github.com/jmpews/Dobby |
| .NET 6 runtime (`dotnet/`) | MIT | https://github.com/dotnet/runtime |
| Archipelago.MultiClient.Net | MIT | https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net |
| Newtonsoft.Json | MIT | https://github.com/JamesNK/Newtonsoft.Json |

## Modified components

Stock BepInEx builds cannot load this game's Unity version (IL2CPP metadata v31), so the release
bundles a **patched BepInEx 6.0.0-be.784** whose Cpp2IL components were **rebuilt from source**
against AsmResolver 6.0.0-beta.5 and AssetRipper.CIL, so that interop generation succeeds on
metadata v31. No source changes were made to BepInEx or Cpp2IL themselves — the modification is to
which versions of those dependencies are compiled in. The exact steps are documented in
[`mod/RevolutionIdleAP/SETUP.md`](mod/RevolutionIdleAP/SETUP.md), and both projects' sources remain
available at the upstream URLs above under their own licences.

BepInEx and UnityDoorstop are LGPL-2.1 and Il2CppInterop is LGPL-3.0; they are distributed here
unmodified in source terms and dynamically loaded, and their licence texts are included so that
recipients have the same rights the upstream projects grant.

## The game

Revolution Idle © Oni Gaming. This is an unofficial, non-commercial fan project, not affiliated
with or endorsed by Oni Gaming.
