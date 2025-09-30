# Challenge API Guide

This document explains how to integrate external mods with the `Challenges` plugin that ships with the PEAK challenge kiosk. The goal is to walk through every step in detail: understanding the types, registering your own challenges, responding to player selections, and reacting to gameplay events. Follow each section sequentially if you are integrating for the first time.

## 1. Set Up Your Development Environment
1. **Install PEAK and BepInEx.** Your development machine must have PEAK installed and BepInEx 5 configured so that the game loads managed plugins. The challenge kiosk relies on the same setup.
2. **Obtain the Challenges source.** Clone or download the Challenges repository. Place it next to your own mod source so you can reference the public API when writing code.
3. **Reference the compiled DLL.** After building or downloading `Challenges.dll`, copy it to your mod project and add it as a reference. In a `.csproj` file this typically looks like:

   ```xml
   <ItemGroup>
     <Reference Include="Challenges">
       <HintPath>../Challenges/bin/Release/Challenges.dll</HintPath>
     </Reference>
   </ItemGroup>
   ```

4. **Verify the reference.** Build your project once. If the compiler finds `ChallengesAPI`, the reference is correctly configured. Fix any missing dependency errors before continuing.

## 2. Understand the Core Types

### 2.1 `Challenge`
`Challenge` is a simple data container. The fields and properties are defined in [`ChallengesAPI.cs`](../ChallengesAPI.cs) and reproduced below for clarity:

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

- `id` (read-only) is the unique identifier. Use lowercase strings with a prefix tied to your plugin (e.g. `myplugin_frozenpeak`). Once published, keep the ID stable so saved games and other mods can reference it reliably.
- `title` appears as the primary label in the kiosk list and in the details panel.
- `description` supports explicit line breaks (`\n`). Aim to list every gameplay rule so players know what will happen.
- `difficulty` uses the `ChallengeDifficulty` enum: `LIGHT`, `EASY`, `MEDIUM`, `MEDIUMCORE`, `HARD`, `HARDCORE`.
- `enabled` tracks whether players have activated the challenge for the current session. Do not set it manually—`ChallengesAPI` manages it.
- `ascentOverride` is optional. When present, the UI sets `Ascents.currentAscent` to this value immediately after the challenge is selected.

### 2.2 `ChallengesAPI`
`ChallengesAPI` is a static helper class that exposes registration, selection, query, and event hooks. You will interact with it through the methods and events described in the following sections.

## 3. Register a Challenge

Perform the following steps during your plugin startup (commonly inside `BaseUnityPlugin.Awake`).

1. **Construct the challenge instance.** Create a `Challenge` object and fill in every field you care about.
2. **Call `ChallengesAPI.RegisterChallenge`.** Pass the instance created in step 1.
3. **Store the reference.** Keep a field pointing to the challenge object so you can read its `enabled` flag later without looking it up in the dictionary.
4. **Test in-game.** Launch PEAK, open the challenge kiosk, and confirm that your challenge appears in the list.

Example implementation:

```csharp
using BepInEx;
using Challenges;

[BepInPlugin("com.yourname.climbvariants", "Climb Variants", "1.0.0")]
public class ClimbVariantsPlugin : BaseUnityPlugin
{
    private Challenge _soloSprint;

    private void Awake()
    {
        _soloSprint = new Challenge(
            id: "climbvariants_solosprint",
            title: "Solo Sprint",
            description: "- Ascent 4\n- Double movement speed\n- Enemies deal extra damage",
            difficulty: ChallengeDifficulty.MEDIUM,
            ascentOverride: 4
        );

        ChallengesAPI.RegisterChallenge(_soloSprint);
    }
}
```

**Verification checklist:**
- The BepInEx console should log that the `Challenges` plugin refreshed its UI when your challenge registers.
- Opening the kiosk should show your challenge with the correct title and description.
- Selecting the challenge should toggle the `enabled` field to `true` inside your stored reference after `SetSingularChallengeEnabled` runs.

## 4. Respond to Player Selection Changes

Use the `ChallengesAPI.ChallengeListChanged` event when you need to update state as soon as the active challenge changes.

1. **Subscribe during initialisation (e.g. in `OnEnable`).**
2. **Unsubscribe in `OnDisable` or `OnDestroy`.** Prevent duplicate handlers when the plugin reloads.
3. **Inspect the dictionary inside your handler.** Either use your cached `Challenge` reference or consult `ChallengesAPI.challenges` to see which challenges are currently enabled.

```csharp
private void OnEnable()
{
    ChallengesAPI.ChallengeListChanged += HandleChallengeListChanged;
}

private void OnDisable()
{
    ChallengesAPI.ChallengeListChanged -= HandleChallengeListChanged;
}

private void HandleChallengeListChanged()
{
    if (_soloSprint != null && _soloSprint.enabled)
    {
        Logger.LogInfo("Solo Sprint is active.");
    }
}
```

## 5. Query Challenge State on Demand

When you are inside gameplay logic (Harmony patches, MonoBehaviours, or gameplay systems), use these methods:

- `ChallengesAPI.IsChallengeEnabled("challenge_id")` → returns `true` if the given challenge is currently active.
- `ChallengesAPI.IsOneOfTheseChallengesEnabled(new[] { "id_a", "id_b" })` → handy for shared logic.
- `ChallengesAPI.IsHell` → `true` when the Hell challenge is active, meaning every registered challenge is forced on.

Example Harmony postfix:

```csharp
[HarmonyPatch(typeof(Character), nameof(Character.ApplyFallDamage))]
public static class FallDamagePatch
{
    public static void Prefix(Character __instance, ref float damage)
    {
        if (!ChallengesAPI.IsChallengeEnabled("climbvariants_solosprint"))
        {
            return;
        }

        damage *= 1.5f; // make falls harsher during Solo Sprint
    }
}
```

**Diagnostic tip:** When behaviour does not match expectations, print the result of `ChallengesAPI.IsChallengeEnabled` to the BepInEx console to confirm whether the challenge flag is set.

## 6. React to Scene Loads for Challenge-Specific Logic

`ChallengesAPI.SceneLoaded` mirrors `SceneManager.sceneLoaded` but only fires while any challenge is active (including Hell mode). This reduces unnecessary work when players opt out of challenges.

1. **Subscribe/unsubscribe** exactly like in Section 4.
2. **Check the scene name and the challenge state.** Apply your challenge-specific logic inside the handler.

```csharp
using UnityEngine.SceneManagement;

private void OnEnable()
{
    ChallengesAPI.SceneLoaded += OnChallengeSceneLoaded;
}

private void OnDisable()
{
    ChallengesAPI.SceneLoaded -= OnChallengeSceneLoaded;
}

private void OnChallengeSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (!ChallengesAPI.IsChallengeEnabled("climbvariants_solosprint"))
    {
        return;
    }

    if (scene.name == "WilIsland")
    {
        Logger.LogInfo("Solo Sprint active on WilIsland – applying modifiers.");
        // Add your gameplay changes here (spawn items, adjust weather, etc.).
    }
}
```

**Testing workflow:**
- Launch a private lobby so you can reload quickly.
- Activate your challenge via the kiosk, start a run, and watch the BepInEx console. Your handler should log the message when the scene loads.
- Deactivate the challenge (or disable all challenges) and repeat. The handler should not log anything, confirming the guard clause works.

## 7. Resetting Challenge State

When your mod needs to clear the active selection—for example, after a custom flow completes—call `ChallengesAPI.DisableAllChallenges()`. This sets every challenge’s `enabled` flag to `false` and fires `ChallengeListChanged` once. Use sparingly: you normally rely on the kiosk to manage selection.

## 8. Handling Hell Mode

- The string constant `ChallengesAPI.HellChallengeId` equals `"hell"`.
- When this challenge is selected, `ChallengesAPI.IsHell` becomes `true`, any query for an existing challenge returns `true`, and your change handlers run once per challenge because every `enabled` flag is set to `true`.
- If your challenge should behave differently when Hell is active, check `ChallengesAPI.IsHell` first.

Example:

```csharp
if (ChallengesAPI.IsHell)
{
    Logger.LogInfo("Hell mode active: enabling extreme tuning.");
}
else if (ChallengesAPI.IsChallengeEnabled("climbvariants_solosprint"))
{
    Logger.LogInfo("Solo Sprint only: apply moderate tuning.");
}
```

## 9. Inspecting the Challenge Dictionary Safely

`ChallengesAPI.challenges` is a `Dictionary<string, Challenge>` that contains every registered challenge. The dictionary is populated lazily from `_registeredChallenges`, so always ensure it is initialised before reading it:

1. Call `ChallengesAPI.EnsureChallengesRegistered()` if you depend on the dictionary immediately after startup.
2. Check for `null` and `Count > 0` before iterating.

```csharp
ChallengesAPI.EnsureChallengesRegistered();

if (ChallengesAPI.challenges != null && ChallengesAPI.challenges.Count > 0)
{
    foreach (var pair in ChallengesAPI.challenges)
    {
        Logger.LogDebug($"Challenge {pair.Key}: enabled={pair.Value.enabled}");
    }
}
```

## 10. Troubleshooting Integration Issues

- **Challenge never appears.** Ensure `ChallengesAPI.RegisterChallenge` runs (add a `Logger.LogInfo` right before the call). Verify that no exception occurs during plugin startup.
- **Handlers fire multiple times.** Check that you unsubscribe in `OnDisable`. Each re-subscription without cleanup adds another callback.
- **Scene handler never triggers.** Remember that `SceneLoaded` only fires when at least one challenge is active. Activate one through the kiosk or via `ChallengesAPI.SetSingularChallengeEnabled` in code before expecting callbacks.
- **Enabled flag not persisting.** If you re-register the same challenge object every frame, the `enabled` flag might reset. Register once during initialisation and update the existing instance instead of constructing a new one repeatedly.

## 11. Summary of Public Members

| Member | Purpose | When to Use |
| --- | --- | --- |
| `ChallengesAPI.RegisterChallenge(Challenge challenge)` | Adds or updates a challenge definition and notifies listeners. | During plugin initialisation. |
| `ChallengesAPI.DisableAllChallenges()` | Clears the enabled flag on every challenge. | When forcing a reset outside the kiosk flow. |
| `ChallengesAPI.SetSingularChallengeEnabled(string id)` | Enables one challenge (or Hell) and disables the rest. | To synchronise selection programmatically (e.g. from custom UI). |
| `ChallengesAPI.IsChallengeEnabled(string id)` | Checks if a specific challenge is active. | Inside Harmony patches or gameplay scripts. |
| `ChallengesAPI.IsOneOfTheseChallengesEnabled(string[] ids)` | Convenience method to test multiple IDs. | When several challenges share logic. |
| `ChallengesAPI.ChallengeListChanged` | Event fired after registration or selection changes. | To refresh cached data or UI when the list updates. |
| `ChallengesAPI.SceneLoaded` | Event fired after a scene loads while any challenge is active. | To apply scene-specific modifications. |
| `ChallengesAPI.EnsureChallengesRegistered()` | Populates the challenge dictionary from cached registrations. | When you need to inspect `ChallengesAPI.challenges` immediately after startup. |
| `ChallengesAPI.HellChallengeId` | Constant ID string for the Hell challenge. | To compare IDs safely. |
| `ChallengesAPI.IsHell` | Global flag indicating Hell mode. | When tuning behaviour for the all-challenge scenario. |
| `ChallengesAPI.challenges` | Public dictionary of registered challenges. | For diagnostic tools or advanced integrations. |

By following the sequential steps above, you can safely extend the challenge ecosystem, ensure your gameplay modifiers react to player selections, and keep your codebase in sync with the official kiosk workflow.
