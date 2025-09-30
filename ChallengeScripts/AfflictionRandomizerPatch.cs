using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Challenges.ChallengeScripts
{
    internal static class AfflictionRandomizerPatch
    {
        private const string ChallengeId = "afflictionrandomizer";

        private static readonly CharacterAfflictions.STATUSTYPE[] StatusTypes =
            Enum.GetValues(typeof(CharacterAfflictions.STATUSTYPE))
                .Cast<CharacterAfflictions.STATUSTYPE>()
                .ToArray();

        private static bool IsRandomizerEnabled()
        {
            return ChallengesAPI.IsChallengeEnabled(ChallengeId);
        }

        [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.AddStatus))]
        private static class CharacterAfflictionsAddStatusRandomizer
        {
            [HarmonyPrefix]
            private static void Prefix(ref CharacterAfflictions.STATUSTYPE statusType)
            {
                if (!IsRandomizerEnabled())
                {
                    return;
                }

                if (statusType == CharacterAfflictions.STATUSTYPE.Drowsy)
                {
                    return;
                }

                if (StatusTypes == null || StatusTypes.Length == 0)
                {
                    return;
                }

                var originalType = statusType;
                int index = UnityEngine.Random.Range(0, StatusTypes.Length);
                var randomType = StatusTypes[index];

                if (StatusTypes.Length > 1 && randomType == originalType)
                {
                    index = (index + 1) % StatusTypes.Length;
                    randomType = StatusTypes[index];
                }

                statusType = randomType;
            }
        }
    }
}
