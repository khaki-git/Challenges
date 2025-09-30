using BepInEx;
using Challenges;
using HarmonyLib;
using PEAKLib.Core;
using UnityEngine;

[BepInPlugin("com.khakixd.challenges", "Challenges", "1.0.0")]
public class ChallengesPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(ChallengesPlugin).Assembly, "com.khakixd.challenges");
        this.LoadBundleAndContentsWithName("peakchallenges.peakbundle", (bundle) =>
        {
            var challenges = bundle.LoadAsset<GameObject>("Challenges");
            var challenge = bundle.LoadAsset<GameObject>("Challenge");
            
            ChallengesGUILoader.Load(challenges, challenge);
            BuiltinChallenges.RegisterAll();
        });
    }
}
