# Challenge API Guide

This document teaches you how to recreate one of the built-in challenges that ships with the `Challenges` plugin. By rebuilding the **Frostbite** scenario from scratch you will learn every API surface needed to mirror an existing challenge, verify that its helper scripts still run, and prepare the ground for your own variations later.

## 1. What You'll Rebuild
- Target challenge: **Frostbite** (`id: permasnow`).
- Behaviour: forces The Alpines, keeps storms active, bumps the ascent to 5, and warns players about the required snowstorm mod.
- Supporting helpers: `ChooseBiome` switches the biome, `SnowPatch` keeps storms alive. Reusing the original ID means those helpers keep working automatically.

```csharp
// Built-in reference (excerpt from BuiltinChallenges.cs)
new Challenge(
    "permasnow",
    "Frostbite",
    " - Ascent 5\n" +
    " - It is always storming in The Alpines\n" +
    " - It is always raining in The Tropics\n" +
    " - The Alpines is forced\n" +
    "PLEASE USE THE FAIR SNOWSTORMS MOD.",
    ChallengeDifficulty.MEDIUMCORE,
    ascentOverride: 5
);
```

We will rebuild that definition inside a separate plugin, confirm the kiosk shows our copy, and watch the helper scripts fire when a climb begins.

## 2. Prepare Your Environment
1. **Install PEAK and BepInEx 5.** Launch the game once so BepInEx generates the folders.
2. **Fetch the Challenges source or DLL.** You need the compiled `Challenges.dll` in your mod project so C# can see `ChallengesAPI` and the helper scripts.
3. **Reference the assembly.** In your `.csproj` add:
   ```xml
   <ItemGroup>
     <Reference Include="Challenges">
       <HintPath>../Challenges/bin/Release/Challenges.dll</HintPath>
     </Reference>
   </ItemGroup>
   ```
   Adjust the path to wherever the DLL lives on your machine.
4. **Verify the reference.** Build your mod once. The compiler should resolve `Challenge`, `ChallengesAPI`, and `Challenges.ChallengeScripts.*`.

## 3. Understand the Core Types

### 3.1 `Challenge`
```csharp
public class Challenge
{
    public string id { get; }
    public string title;
    public string description;
    public ChallengeDifficulty difficulty;
    public bool enabled;
    public int? ascentOverride { get; }
}
```

- `id` must stay stable. Reusing `permasnow` makes the built-in helpers treat your clone exactly like the original Frostbite entry.
- `title` and `description` appear in the kiosk. Use `\n` for manual line breaks.
- `difficulty` is the kiosk badge (`LIGHT`, `EASY`, `MEDIUM`, `MEDIUMCORE`, `HARD`, `HARDCORE`).
- `enabled` is toggled by the kiosk—do not set it yourself.
- `ascentOverride` changes the current ascent immediately after selection when it is not `null`.

### 3.2 `ChallengesAPI`
Key members you will use while cloning Frostbite:
- `RegisterChallenge(Challenge challenge)` — add or replace a challenge definition.
- `SetSingularChallengeEnabled(string id)` — enable exactly one challenge for testing.
- `IsChallengeEnabled(string id)` — check whether your clone is active.
- `ChallengeListChanged` — event fired after registration or selection changes.
- `SceneLoaded` — event fired after a Unity scene loads while any challenge is active.

## 4. Mirror the Built-in Data
Create a plugin that constructs a new `Challenge` instance using the same ID that the built-in version uses. When `RegisterChallenge` sees a duplicate ID it replaces the existing definition but preserves the `enabled` flag so the kiosk state is not lost.

```csharp
using BepInEx;
using Challenges;

[BepInPlugin("com.example.frostbiteclone", "Frostbite Clone", "1.0.0")]
public class FrostbiteClonePlugin : BaseUnityPlugin
{
    private Challenge _frostbite;

    private void Awake()
    {
        _frostbite = BuildFrostbiteClone();
        ChallengesAPI.RegisterChallenge(_frostbite);
    }

    private static Challenge BuildFrostbiteClone()
    {
        return new Challenge(
            id: "permasnow", // match the built-in ID so helper scripts still recognise it
            title: "Frostbite",
            description: string.Join("\n", new[]
            {
                " - Ascent 5",
                " - It is always storming in The Alpines",
                " - It is always raining in The Tropics",
                " - The Alpines is forced",
                "PLEASE USE THE FAIR SNOWSTORMS MOD."
            }),
            difficulty: ChallengeDifficulty.MEDIUMCORE,
            ascentOverride: 5
        );
    }
}
```

> **Tip:** keeping the `id` identical to the built-in entry means you do not have to recreate `ChooseBiome` or `SnowPatch`; they key off the ID and will now affect your clone.

## 5. Confirm the Clone Works In-Game
1. Build your mod and drop the DLL in `<PEAK>/BepInEx/plugins/` alongside `Challenges.dll`.
2. Launch PEAK, interact with the challenge kiosk, and find **Frostbite** in the list.
3. Open the details panel; the description and difficulty should match the built-in entry.
4. Select the challenge and press **Go** to start a climb. The ascent should change to 5 immediately.
5. Enter the level and observe the forced storm and biome swap. Because the ID matches, `SnowPatch` and `ChooseBiome` continue to run.

## 6. Trace Helper Scripts and Scene Hooks
If you want to confirm the helper scripts are firing, subscribe to `SceneLoaded`. The event runs after any gameplay scene loads while at least one challenge is enabled.

```csharp
private void OnEnable()
{
    ChallengesAPI.SceneLoaded += HandleSceneLoaded;
}

private void OnDisable()
{
    ChallengesAPI.SceneLoaded -= HandleSceneLoaded;
}

private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
{
    if (ChallengesAPI.IsChallengeEnabled("permasnow"))
    {
        Logger.LogInfo($"Frostbite active in scene {scene.name}; helper patches should already be running.");
    }
}
```

Because you reused the same ID, `Challenges.ChallengeScripts.SnowPatch` and `ChooseBiome` continue to listen for `permasnow` and apply the weather and biome changes without extra work.

## 7. Extending the Clone
Once the clone behaves identically to the built-in challenge you can customise it safely.
- **Change the description or difficulty** by mutating `_frostbite.description` or `_frostbite.difficulty` before registering it.
- **Add extra effects** by listening to `SceneLoaded` and applying your own patches when `permasnow` is enabled.
- **Replace helper behaviour** by creating your own script that checks `ChallengesAPI.IsChallengeEnabled("permasnow")` and performs new logic; the built-in helpers will coexist unless you disable them in your code.

Example: add a log line when storms start, while keeping existing helpers alive.

```csharp
private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene,
    UnityEngine.SceneManagement.LoadSceneMode mode)
{
    if (!ChallengesAPI.IsChallengeEnabled("permasnow")) return;

    Logger.LogInfo("Custom Frostbite tweak: adjusting loot tables.");
    // TODO: implement your own gameplay adjustments here.
}
```

## 8. Quick API Reference

| Member | Purpose | When to Use |
| --- | --- | --- |
| `ChallengesAPI.RegisterChallenge(Challenge challenge)` | Adds or replaces a challenge definition. | As soon as your plugin initialises. |
| `ChallengesAPI.SetSingularChallengeEnabled(string id)` | Enables exactly one challenge (disables the rest). | For automated testing or custom UI. |
| `ChallengesAPI.DisableAllChallenges()` | Clears the enabled flag on every challenge. | To reset state outside the kiosk flow. |
| `ChallengesAPI.IsChallengeEnabled(string id)` | Checks if a challenge is active. | Inside patches to gate behaviour. |
| `ChallengesAPI.IsOneOfTheseChallengesEnabled(string[] ids)` | Tests a set of IDs quickly. | When multiple challenges share logic. |
| `ChallengesAPI.ChallengeListChanged` | Event fired after registrations or selections change. | To refresh cached state or UI. |
| `ChallengesAPI.SceneLoaded` | Event fired after a scene loads while any challenge is active. | To apply scene-specific tweaks. |
| `ChallengesAPI.HellChallengeId` / `ChallengesAPI.IsHell` | Utilities for the Hell mode catch-all challenge. | When adding logic that should treat Hell as special. |

## 9. Troubleshooting
- **Frostbite disappears from the kiosk.** Ensure your plugin builds against the same `Challenges.dll` version that the game is running and that `RegisterChallenge` executes without exceptions.
- **Helpers stop working.** Double-check the clone keeps the ID `permasnow`. If you change it, you must either update the helper scripts to look for the new ID or create your own equivalents.
- **SceneLoaded never fires.** The event only hooks when at least one challenge is enabled. Use `ChallengesAPI.SetSingularChallengeEnabled("permasnow")` in a debug build to force activation before the kiosk runs.
- **Description changes do not appear.** After modifying the definition, restart the game or trigger a challenge refresh by toggling the selection in the kiosk so the UI reloads the data.

By mirroring a built-in challenge first, you can verify that your integration is solid before branching out into brand-new gameplay ideas. Once you are comfortable overriding and extending Frostbite, repeat the same pattern for the other built-ins (Instagib, Baggage Allowance, etc.)—the only differences are the IDs and the helper scripts each one uses.
