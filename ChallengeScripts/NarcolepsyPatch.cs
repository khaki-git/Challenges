using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Challenges.ChallengeScripts
{
    internal static class NarcolepsyPatch
    {
        private const string NarcolepsyChallengeId = "narcolepsy";
        private const float MinIntervalSeconds = 60f;
        private const float MaxIntervalSeconds = 360f;
        private const float InstagibGracePeriod = 1f;

        private static readonly Dictionary<int, NarcolepsyState> States = new Dictionary<int, NarcolepsyState>();

        private static bool IsNarcolepsyEnabled()
        {
            return ChallengesAPI.challenges != null
                   && ChallengesAPI.challenges.ContainsKey(NarcolepsyChallengeId)
                   && ChallengesAPI.IsChallengeEnabled(NarcolepsyChallengeId);
        }

        private static float GetNextIntervalSeconds()
        {
            return Random.Range(MinIntervalSeconds, MaxIntervalSeconds);
        }

        private static NarcolepsyState GetOrCreateState(int key, bool isConscious)
        {
            if (!States.TryGetValue(key, out var state))
            {
                state = new NarcolepsyState
                {
                    NextTrigger = Time.time + GetNextIntervalSeconds(),
                    WasConscious = isConscious
                };
                States[key] = state;
            }

            return state;
        }

        [HarmonyPatch(typeof(CharacterAfflictions))]
        [HarmonyPatch("Update")]
        private static class CharacterAfflictionsUpdateNarcolepsy
        {
            [HarmonyPostfix]
            private static void Postfix(CharacterAfflictions __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                if (!IsNarcolepsyEnabled())
                {
                    States.Clear();
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

                var data = character.data;
                bool isConscious = data != null && data.fullyConscious;

                int key = character.GetInstanceID();
                var state = GetOrCreateState(key, isConscious);

                if (!isConscious)
                {
                    if (state.WasConscious)
                    {
                        state.NextTrigger = Time.time + GetNextIntervalSeconds();
                    }

                    state.WasConscious = false;
                    return;
                }

                state.WasConscious = true;

                float now = Time.time;
                if (now < state.NextTrigger)
                {
                    return;
                }
                
                float remainderOfHealth = Mathf.Max(0f, 1f - (
                    __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury)+
                    __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger)+
                    __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Thorns)+
                    __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Cold)+
                    __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hot)+
                    __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Poison)
                    ));
                float drowsyDamage = remainderOfHealth + 0.1f;
                if (drowsyDamage > 0f && __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Drowsy) < 0.05f)
                {
                    __instance.AddStatus(CharacterAfflictions.STATUSTYPE.Drowsy, drowsyDamage);
                }

                photonView.RPC("RPCA_PassOut", RpcTarget.All);

                state.InstagibGraceUntil = now + InstagibGracePeriod;
                state.NextTrigger = now + GetNextIntervalSeconds();
            }
        }

        internal static bool ShouldPreventInstagib(Character character)
        {
            if (character == null)
            {
                return false;
            }

            if (!IsNarcolepsyEnabled())
            {
                return false;
            }

            int key = character.GetInstanceID();
            if (!States.TryGetValue(key, out var state))
            {
                return false;
            }

            return Time.time <= state.InstagibGraceUntil;
        }

        private sealed class NarcolepsyState
        {
            public float NextTrigger;
            public bool WasConscious;
            public float InstagibGraceUntil;
        }
    }
}
