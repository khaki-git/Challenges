# Challenge API Guide

This will be a brief guide on how to create challenges with the Challenges mod.  
This is not a beginner's guide to modding, [this is a beginner's guide to modding](https://peakmodding.github.io/getting-started/overview/).  
I would assume you already have a BepInEx plugin set up with this mod as a dependency & with all the PEAK dlls as a dependency too.

## Registering a Challenge
To register a challenge, you just need to call `ChallengesAPI.RegisterChallenge` with the challenge properties needed.  
Here's a challenge example below:
```csharp
ChallengesAPI.RegisterChallenge(
  new Challenge(
    "MyChallengesId", // this is the id of your mod, it does not get shown to the player
    "My really cool challenge that's unique and cool!", // this is the display name, this is what appears in the 
    " - Dying kills you", // description of the challenge
    ChallengeDifficulty.LIGHT, // difficulty
    ascentOverride: -1 // the ascent (-1 == tenderfoot, 0 == PEAK)
  )
);
```
Here's the list of all acceptable challenge difficulties:
```
ChallengeDifficulty.LIGHT
ChallengeDifficulty.EASY
ChallengeDifficulty.MEDIUM
ChallengeDifficulty.MEDIUMCORE
ChallengeDifficulty.HARD
ChallengeDifficulty.HARDCORE
```
There is currently no way to add, remove, or change difficulties.

## Reading a Challenge
You can test if a challenge is active by querying `ChallengesAPI.IsChallengeEnabled` with the Id of the challenge you wish to check for.  
If it is currently in hell mode, then it will always return true. However, you can check for hell mode with `ChallengesAPI.IsHell` for special behaviours.