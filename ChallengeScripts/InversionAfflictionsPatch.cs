using HarmonyLib;
using UnityEngine;
using Zorro.Core;

namespace Challenges.ChallengeScripts
{
    internal static class InversionAfflictionsPatch
    {
        private const string InversionChallengeId = "inversion";
        private const string NarcolepsyChallengeId = "narcolepsy";

        private static bool IsInversionEnabled()
        {
            return ChallengesAPI.challenges != null
                   && ChallengesAPI.challenges.ContainsKey(InversionChallengeId)
                   && ChallengesAPI.IsChallengeEnabled(InversionChallengeId);
        }

        private static bool ShouldInvertDrowsy()
        {
            if (!ChallengesAPI.IsHell)
            {
                return true;
            }

            return ChallengesAPI.challenges == null
                   || !ChallengesAPI.challenges.ContainsKey(NarcolepsyChallengeId)
                   || !ChallengesAPI.IsChallengeEnabled(NarcolepsyChallengeId);
        }

        [HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
        private static class UpdateNormalStatusesInversion
        {
            [HarmonyPrefix]
            private static bool Prefix(CharacterAfflictions __instance)
            {
                if (__instance == null)
                {
                    return true;
                }

                if (!IsInversionEnabled())
                {
                    return true;
                }

                Character character = __instance.character;
                if (character == null)
                {
                    return true;
                }

                if (!character.IsLocal)
                {
                    return false;
                }

                if (Ascents.isNightCold
                    && Singleton<MountainProgressHandler>.Instance
                    && Singleton<MountainProgressHandler>.Instance.maxProgressPointReached < 3
                    && DayNightManager.instance != null
                    && DayNightManager.instance.isDay < 0.5f)
                {
                    __instance.AddStatus(
                        CharacterAfflictions.STATUSTYPE.Cold,
                        Time.deltaTime * (1f - DayNightManager.instance.isDay) * Ascents.nightColdRate);
                }

                if (character.data != null && character.data.fullyConscious)
                {
                    __instance.AddStatus(
                        CharacterAfflictions.STATUSTYPE.Hunger,
                        Time.deltaTime * __instance.hungerPerSecond * Ascents.hungerRateMultiplier);
                }

                if (__instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Poison) > 0f
                    && Time.time - __instance.LastAddedStatus(CharacterAfflictions.STATUSTYPE.Poison) > __instance.poisonReductionCooldown)
                {
                    __instance.AddStatus(
                        CharacterAfflictions.STATUSTYPE.Poison,
                        __instance.poisonReductionPerSecond * Time.deltaTime);
                }

                if (__instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > 0f
                    && Time.time - __instance.LastAddedStatus(CharacterAfflictions.STATUSTYPE.Drowsy) > __instance.drowsyReductionCooldown)
                {
                    float delta = __instance.drowsyReductionPerSecond * Time.deltaTime;
                    if (ShouldInvertDrowsy())
                    {
                        __instance.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, delta);
                    }
                    else
                    {
                        __instance.SubtractStatus(CharacterAfflictions.STATUSTYPE.Drowsy, delta);
                    }
                }

                if (__instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hot) > 0f
                    && Time.time - __instance.LastAddedStatus(CharacterAfflictions.STATUSTYPE.Hot) > __instance.hotReductionCooldown)
                {
                    __instance.AddStatus(
                        CharacterAfflictions.STATUSTYPE.Hot,
                        __instance.hotReductionPerSecond * Time.deltaTime);
                }

                return false;
            }
        }
    }
}
