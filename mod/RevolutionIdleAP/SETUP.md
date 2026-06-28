# Revolution Idle AP Mod — Build & BepInEx Setup

Game: Revolution Idle (Steam, appid 2763740) — Unity **2022.3.62f3**, IL2CPP **metadata v31**.
Install dir: `A:\SteamLibrary\steamapps\common\Revolution Idle`.

## Status

✅ **Smoke test passed.** The plugin loads under BepInEx (ACTk does not block it), Harmony-postfixes
`GameData.get_PrestigeUnlocked`, and reads live `GameData` (`achByte`, `unlockedAch`).

## The BepInEx toolchain problem (and the fix)

BepInEx IL2CPP must run Cpp2IL + Il2CppInterop to generate interop assemblies on first launch.
For this game's metadata v31:

- **Official `6.0.0-pre.2` (be.697)** — bundled Cpp2IL only supports metadata 23-29 → fails on v31.
- **Latest BE build (e.g. 784 from builds.bepinex.dev)** — reads v31, but ships an **internally
  incompatible** dependency set: Cpp2IL `pre-release.20` needs AsmResolver `beta.3`
  (`ModuleDefinition(String, AssemblyReference)`), while Il2CppInterop `1.5.3` needs `beta.5`
  (`ModuleDefinition(Utf8String, AssemblyReference)`). No single AsmResolver satisfies both.

**Fix:** keep BE 784 + Il2CppInterop 1.5.3 + AsmResolver beta.5, and upgrade **Cpp2IL to
`pre-release.21`** (built against beta.5, so everything agrees). Cpp2IL .21 isn't on NuGet, so build
it from source.

## Reproduce the BepInEx install

1. Download & extract `BepInEx-Unity.IL2CPP-win-x64` build **784** (builds.bepinex.dev) into the game dir.
2. Build Cpp2IL .21 from source:
   ```sh
   git clone --depth 1 --branch 2022.1.0-pre-release.21 https://github.com/SamboyCoding/Cpp2IL.git
   cd Cpp2IL && rm global.json            # it pins SDK 9.0.0 exactly
   dotnet publish Cpp2IL.Core/Cpp2IL.Core.csproj -c Release -f net6.0 -o publish_out
   ```
   (Requires .NET SDK 9 because the csproj multi-targets net9.0.)
3. Copy this **consistent closure** from `publish_out` into `<game>\BepInEx\core\`, overwriting:
   - `Cpp2IL.Core.dll`, `LibCpp2IL.dll`, `StableNameDotNet.dll`, `WasmDisassembler.dll`  (.21)
   - `AssetRipper.CIL.dll` (**1.2.2**), `AssetRipper.Primitives.dll` (3.2.0), `Gee.External.Capstone.dll` (2.3.2)
   - `AsmResolver*.dll` (**6.0.0-beta.5** — what BE 784 already ships; leave as-is)
4. Launch the game once. BepInEx regenerates interop (~2-4 min) → `BepInEx\interop\` (~127 dlls incl.
   `Assembly-CSharp.dll`). Log: `BepInEx\LogOutput.log` should end with `Il2CppInteropGen ... Done!`.

> When swapping `core` DLLs, the game must be **fully closed** — it locks them. `taskkill /F /T` and
> poll until no `Revolution*` process remains.

## Build the plugin

```sh
cd mod/RevolutionIdleAP
dotnet build -c Release      # globs BepInEx\core + interop refs; auto-deploys to BepInEx\plugins\
```
The `GameDir` property in `RevolutionIdleAP.csproj` points at the install. The MSB3246 "bad image"
warning (one metadata-less interop stub) is harmless. Launch the game and check `LogOutput.log` for
`[SMOKE]` lines.

## Notes / corrections discovered

- `GameData.achByte` is **~10055 bytes**, not 520 — a broad flags/save blob. Achievement *detection*
  for AP locations should hook `GameData.UnlockAchievement(int id)` and/or read `unlockedAch`
  (`List<int>`), **not** assume `achByte` is the achievement array.
- Unlock *gates* still live at `achByte[N]` (Prestige=3, Promotion=11, Infinity=29, Eternity=69,
  Unity=160, Minerals=239) OR a currency threshold — force unlocks by Harmony-postfixing the
  `get_XxxUnlocked` getters (proven approach), never by writing `achByte`.
