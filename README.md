# Challenges

Challenges is a BepInEx plugin for **PEAK**, the cooperative climbing and survival game. It adds an in-game challenge kiosk, ships several curated scenarios, and exposes an API that other mods can call to plug their own challenges into the same UI flow.

## Feature Overview
- Challenge kiosk spawns in the Airport hub and connects to the normal boarding flow so teams can pick a challenge before every climb.
- Dynamic UI pulls data from `ChallengesAPI` at runtime, supports pagination, and shows custom descriptions, difficulties, and ascent overrides.
- Built-in scenarios: **Frostbite**, **Instagib**, **Baggage Allowance**, **The Hunger**, and **Hell** (enables everything at once).
- Helper scripts (`ChooseBiome`, `SnowPatch`, `HungerPatch`, etc.) demonstrate how to react to challenge state when gameplay scenes load.
- Public API lets community plugins register challenges, subscribe to change events, and query which modifiers are active.

## Prerequisites
- A working copy of **PEAK** installed through Steam (or an equivalent distribution).
- [BepInEx 5](https://github.com/BepInEx/BepInEx) installed in the PEAK directory. The game should launch once with BepInEx so that the folder structure is initialised.
- Optional but recommended: [dotnet SDK 7+](https://dotnet.microsoft.com/en-us/download) for building the plugin from source.

## Installation (Step-by-Step)
1. **Confirm the BepInEx layout.** Verify that `<PEAK>/BepInEx/plugins/` exists. If it does not, install BepInEx 5 into the game directory and launch PEAK once to let BepInEx generate folders.
2. **Prepare the plugin folder.** Create `<PEAK>/BepInEx/plugins/Challenges/` (capitalisation is not important) to keep the mod isolated from other plugins.
3. **Download or build `Challenges.dll`.** If you want the precompiled release, copy the DLL into the folder created above. If you prefer to build locally, follow the steps in the next section first.
4. **Copy the UI asset bundle.** Place `peakchallenges.peakbundle` next to `Challenges.dll` inside the same plugins directory. The loader will instantiate the kiosk UI from this bundle when the game starts.
5. **Launch PEAK.** Load into the Airport hub. A kiosk labelled `CHALLENGE KIOSK` should appear near the check-in area. Interact with it to open the challenge selection window.
6. **Pick a challenge.** Select a challenge in the UI, press **Go**, and start the climb. To switch challenges later, interact with the kiosk again and choose a different option.

## Building From Source
1. **Clone or extract the repository.** Place it anywhere on your machine. In the remainder of this section the repository root is referred to as `<ChallengesRepo>`.
2. **Review configurable paths.** Open [`Challenges.csproj`](Challenges.csproj) and note the default property values:
   - `GameDir` → `/var/home/bazzite/.local/share/Steam/steamapps/common/PEAK`
   - `PeakLibDir` → `/home/bazzite/Documents/peaklib`
   Adjust these paths at build time if your installation differs.
3. **Run the build.** From the repository root, execute:

   ```bash
   cd <ChallengesRepo>
   dotnet build -c Release \
       /p:GameDir="/path/to/Steam/steamapps/common/PEAK" \
       /p:PeakLibDir="/path/to/peaklib"
   ```

   Replace both placeholders with actual paths on your system. The SDK compiles against the assemblies inside the PEAK installation and the PEAKLib dependency tree.

4. **Locate the output.** The compiled DLL appears in `bin/Release/Challenges.dll`. Development builds will target `bin/Debug/` instead.
5. **Deploy the build.** Copy the DLL to `<PEAK>/BepInEx/plugins/Challenges/` (replacing any existing copy), keep the `peakchallenges.peakbundle` in the same folder, and restart the game.
6. **Verify in-game.** Open the challenge kiosk after loading into the Airport. Ensure the list contains both the built-in challenges and any third-party challenges you expect to be registered.

## Using the Challenge API
The public API lives in [`ChallengesAPI.cs`](ChallengesAPI.cs). It enables mod authors to register new challenges, listen for selection or scene events, and perform runtime queries. A comprehensive integration guide, including complete code samples and event diagrams, is available in [`docs/challenge-api.md`](docs/challenge-api.md).

## Development Notes
- The challenge UI, button template, and supporting prefabs are bundled inside `peakchallenges.peakbundle`. When editing UI assets, rebuild the bundle with the same name so the loader continues to find it.
- `ChallengesGUILoader` controls kiosk instantiation, pagination, and selection persistence. Start there when changing UI behaviour or the kiosk workflow.
- Built-in helper scripts under [`ChallengeScripts/`](ChallengeScripts) illustrate how to subscribe to `ChallengesAPI.SceneLoaded` and apply gameplay tweaks when specific challenges are active.

## Troubleshooting
- **Kiosk does not appear.** Check the BepInEx console/log for asset bundle load failures. Confirm `peakchallenges.peakbundle` is present next to the DLL.
- **Challenge selection does nothing.** Make sure no other mod is calling `ChallengesAPI.DisableAllChallenges()` on load. You can also verify that `ChallengesAPI.challenges` is populated via the BepInEx console.
- **Build fails with missing references.** Ensure the `GameDir` and `PeakLibDir` properties point to valid directories. The project depends on assemblies shipped with PEAK and PEAKLib.

## License
No explicit license is provided. Assume all rights reserved unless the author specifies otherwise.
