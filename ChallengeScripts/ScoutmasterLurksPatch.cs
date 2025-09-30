using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Challenges.ChallengeScripts
{
    internal static class ScoutmasterLurksPatch
    {
        private const string ChallengeId = "scoutmasterlurks";

        private static readonly FieldInfo SinceLookForTargetField = AccessTools.Field(typeof(Scoutmaster), "sinceLookForTarget");
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(Scoutmaster), "character");
        private static readonly MethodInfo SetCurrentTargetMethod = AccessTools.Method(typeof(Scoutmaster), "SetCurrentTarget");
        private static readonly FieldInfo ChillForSecondsField = AccessTools.Field(typeof(Scoutmaster), "chillForSeconds");
        private static readonly FieldInfo TpCounterField = AccessTools.Field(typeof(Scoutmaster), "tpCounter");
        private static readonly FieldInfo TargetHasSeenCounterField = AccessTools.Field(typeof(Scoutmaster), "targetHasSeenMeCounter");

        private static readonly System.Collections.Generic.HashSet<int> LeftForDay = new System.Collections.Generic.HashSet<int>();

        private static bool IsChallengeEnabled()
        {
            return ChallengesAPI.challenges != null
                   && ChallengesAPI.challenges.ContainsKey(ChallengeId)
                   && ChallengesAPI.IsChallengeEnabled(ChallengeId);
        }

        [HarmonyPatch(typeof(Scoutmaster), "LookForTarget")]
        private static class ScoutmasterLookForTarget
        {
            [HarmonyPrefix]
            private static bool Prefix(Scoutmaster __instance)
            {
                if (!IsChallengeEnabled() || __instance == null)
                {
                    return true;
                }

                if (SinceLookForTargetField == null || CharacterField == null)
                {
                    return true;
                }

                if (!(SinceLookForTargetField.GetValue(__instance) is float sinceLook))
                {
                    return true;
                }

                bool night = IsNight();
                if (!night && sinceLook < 30f)
                {
                    return false;
                }

                SinceLookForTargetField.SetValue(__instance, 0f);

                if (!night)
                {
                    SetCurrentTarget(__instance, null, 0f);
                    return false;
                }

                Character scoutmasterCharacter = GetScoutmasterCharacter(__instance);
                if (scoutmasterCharacter == null)
                {
                    SetCurrentTarget(__instance, null, 0f);
                    return false;
                }

                Character closest = GetClosestLivingPlayer(scoutmasterCharacter);
                if (!IsValidTarget(closest))
                {
                    SetCurrentTarget(__instance, null, 0f);
                    return false;
                }

                SetCurrentTarget(__instance, closest, 0f);
                return false;
            }
        }

        [HarmonyPatch(typeof(Scoutmaster), "VerifyTarget")]
        private static class ScoutmasterVerifyTarget
        {
            [HarmonyPrefix]
            private static bool Prefix(Scoutmaster __instance)
            {
                if (!IsChallengeEnabled() || __instance == null)
                {
                    return true;
                }

                Character scoutmasterCharacter = GetScoutmasterCharacter(__instance);
                if (scoutmasterCharacter == null)
                {
                    return true;
                }

                Character current = __instance.currentTarget;
                if (!IsValidTarget(current))
                {
                    current = null;
                }

                Character closest = GetClosestLivingPlayer(scoutmasterCharacter);
                if (!IsValidTarget(closest))
                {
                    if (current != null)
                    {
                        SetCurrentTarget(__instance, null, 0f);
                    }

                    return false;
                }

                if (current != closest)
                {
                    SetCurrentTarget(__instance, closest, 0f);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Scoutmaster), "Update")]
        private static class ScoutmasterUpdate
        {
            [HarmonyPostfix]
            private static void Postfix(Scoutmaster __instance)
            {
                if (!IsChallengeEnabled() || __instance == null)
                {
                    return;
                }

                PhotonView view = __instance.GetComponent<PhotonView>();
                if (view != null && !view.IsMine)
                {
                    return;
                }

                int id = __instance.GetInstanceID();
                if (IsNight())
                {
                    LeftForDay.Remove(id);
                    return;
                }

                if (LeftForDay.Add(id))
                {
                    SetCurrentTarget(__instance, null, 0f);
                    ForceTeleportAway(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Scoutmaster), "Chase")]
        private static class ScoutmasterChase
        {
            [HarmonyPostfix]
            private static void Postfix(Scoutmaster __instance)
            {
                if (!IsChallengeEnabled() || __instance == null)
                {
                    return;
                }

                PhotonView view = __instance.GetComponent<PhotonView>();
                if (view != null && !view.IsMine)
                {
                    return;
                }

                Character scoutmaster = GetScoutmasterCharacter(__instance);
                Character target = __instance.currentTarget;
                if (!IsValidTarget(target) || scoutmaster == null)
                {
                    return;
                }

                float distance = Vector3.Distance(scoutmaster.Center, target.Center);
                float awareness = 0f;
                if (TargetHasSeenCounterField != null)
                {
                    awareness = (float)TargetHasSeenCounterField.GetValue(__instance);
                }

                bool targetAlerted = awareness >= 0.5f || distance < 18f;

                if (targetAlerted)
                {
                    return;
                }

                float desiredForward = Mathf.Clamp((distance - 4f) / 12f, 0f, 1f);
                if (distance < 12f)
                {
                    desiredForward = Mathf.Clamp((distance - 10f) / 8f, -0.2f, 0.25f);
                }

                scoutmaster.input.movementInput = new Vector2(0f, desiredForward);

                bool inGrabRange = distance < 3.5f && awareness > 0.2f;
                scoutmaster.input.useSecondaryIsPressed = inGrabRange;

                if (distance < 8f && ChillForSecondsField != null)
                {
                    float chill = (float)ChillForSecondsField.GetValue(__instance);
                    if (chill < 0.75f)
                    {
                        ChillForSecondsField.SetValue(__instance, 0.75f);
                    }
                }
            }
        }

        private static Character GetScoutmasterCharacter(Scoutmaster scoutmaster)
        {
            if (scoutmaster == null)
            {
                return null;
            }

            Character character = CharacterField?.GetValue(scoutmaster) as Character;
            if (character != null)
            {
                return character;
            }

            return scoutmaster.GetComponent<Character>();
        }

        private static bool IsNight()
        {
            return DayNightManager.instance != null && DayNightManager.instance.isDay < 0.5f;
        }

        private static bool IsValidTarget(Character character)
        {
            if (character == null || character.isBot)
            {
                return false;
            }

            var data = character.data;
            if (data == null || data.dead || data.fullyPassedOut)
            {
                return false;
            }

            return true;
        }

        private static Character GetClosestLivingPlayer(Character scoutmasterCharacter)
        {
            if (scoutmasterCharacter == null)
            {
                return null;
            }

            Character closest = null;
            float bestDistance = float.MaxValue;
            Vector3 origin = scoutmasterCharacter.Center;

            foreach (Character candidate in Character.AllCharacters)
            {
                if (!IsValidTarget(candidate) || candidate == scoutmasterCharacter)
                {
                    continue;
                }

                float distance = Vector3.Distance(origin, candidate.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        private static void SetCurrentTarget(Scoutmaster scoutmaster, Character target, float force)
        {
            if (scoutmaster == null || SetCurrentTargetMethod == null)
            {
                return;
            }

            SetCurrentTargetMethod.Invoke(scoutmaster, new object[] { target, force });
        }

        private static void ForceTeleportAway(Scoutmaster scoutmaster)
        {
            if (scoutmaster == null)
            {
                return;
            }

            Character character = GetScoutmasterCharacter(scoutmaster);
            if (character != null)
            {
                Vector3 pos = character.transform.position;
                if (pos.z > 4900f && Mathf.Abs(pos.x) < 20f && Mathf.Abs(pos.y) < 20f)
                {
                    return;
                }
            }

            if (TpCounterField != null)
            {
                TpCounterField.SetValue(scoutmaster, 0f);
            }

            if (ChillForSecondsField != null)
            {
                ChillForSecondsField.SetValue(scoutmaster, 5f);
            }

            scoutmaster.TeleportFarAway();
        }
    }
}
