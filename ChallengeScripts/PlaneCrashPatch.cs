using System;
using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace Challenges.ChallengeScripts
{
    internal static class PlaneCrashPatch
    {
        private const string PlaneCrashChallengeId = "planecrash";
        private const float InjuryAmount = 0.9f;

        private static readonly HashSet<int> AppliedCharacters = new HashSet<int>();
        private static bool _initialized;

        private static bool IsPlaneCrashEnabled()
        {
            return ChallengesAPI.challenges != null
                   && ChallengesAPI.challenges.ContainsKey(PlaneCrashChallengeId)
                   && ChallengesAPI.IsChallengeEnabled(PlaneCrashChallengeId);
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            ChallengesAPI.SceneLoaded += HandleSceneLoaded;
            _initialized = true;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AppliedCharacters.Clear();
        }

        [HarmonyPatch(typeof(CharacterAfflictions))]
        [HarmonyPatch("Update")]
        private static class CharacterAfflictionsUpdatePlaneCrash
        {
            [HarmonyPostfix]
            private static void Postfix(CharacterAfflictions __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                EnsureInitialized();

                if (!IsPlaneCrashEnabled())
                {
                    AppliedCharacters.Clear();
                    return;
                }

                Character character = __instance.character;
                if (character == null || character.isBot || !character.IsLocal)
                {
                    return;
                }

                PhotonView photonView = character.photonView;
                if (photonView == null || !photonView.IsMine)
                {
                    return;
                }

                var scene = SceneManager.GetActiveScene();
                if (scene.IsValid())
                {
                    var sceneName = scene.name ?? string.Empty;
                    if (string.Equals(sceneName, "Airport", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                int key = character.GetInstanceID();
                if (AppliedCharacters.Contains(key))
                {
                    return;
                }

                __instance.SetStatus(CharacterAfflictions.STATUSTYPE.Injury, InjuryAmount);

                AppliedCharacters.Add(key);
            }
        }
    }
}
