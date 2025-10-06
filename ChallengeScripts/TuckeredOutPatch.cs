using HarmonyLib;
using UnityEngine;

namespace Challenges.ChallengeScripts
{
    public static class TuckeredOut
    {
        [HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
        private static class TuckeredOutPatch
        {
            [HarmonyPostfix]
            private static void Postfix(CharacterAfflictions __instance)
            {
                if (!ChallengesAPI.IsChallengeEnabled("tuckered_out")) return;
                if (__instance.character.IsLocal && __instance.character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight) > 0)
                {
                    __instance.character.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, 
                        __instance.character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Weight) * Time.deltaTime / 60);
                }
            }
        }
    }
}