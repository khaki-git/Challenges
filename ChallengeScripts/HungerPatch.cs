using System;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace Challenges.ChallengeScripts
{
    internal static class HungerPatch
    {
        private const string HungerChallengeId = "hunger";

        private static bool IsHungerEnabled()
        {
            return ChallengesAPI.challenges != null
                   && ChallengesAPI.challenges.ContainsKey(HungerChallengeId)
                   && ChallengesAPI.IsChallengeEnabled(HungerChallengeId)
                   && IsInGameplayScene();
        }

        private static bool IsInGameplayScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return false;
            }

            var name = scene.name ?? string.Empty;
            if (string.Equals(name, "WilIsland", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return name.StartsWith("level_", StringComparison.OrdinalIgnoreCase);
        }

        [HarmonyPatch(typeof(Character), "CanRegenStamina")]
        private static class DisableNaturalStaminaRegen
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance, ref bool __result)
            {
                if (!__result)
                {
                    return;
                }

                if (!IsHungerEnabled())
                {
                    return;
                }

                if (__instance != null && __instance.infiniteStam)
                {
                    return;
                }

                __result = false;
            }
        }

        [HarmonyPatch(typeof(Action_RestoreHunger), nameof(Action_RestoreHunger.RunAction))]
        private static class RestoreStaminaWhenFed
        {
            [HarmonyPostfix]
            private static void Postfix(Action_RestoreHunger __instance)
            {
                if (!IsHungerEnabled())
                {
                    return;
                }

                Character character = GetCharacter(__instance);
                if (character == null)
                {
                    return;
                }

                var data = character.data;
                if (data == null)
                {
                    return;
                }

                float maxStamina = character.GetMaxStamina();
                if (maxStamina < 0f)
                {
                    maxStamina = 0f;
                }

                float missing = maxStamina - data.currentStamina;
                if (missing > 0f)
                {
                    character.AddStamina(missing);
                }

                data.currentStamina = maxStamina;
                character.ClampStamina();
            }
        }

        private static Character GetCharacter(Action_RestoreHunger instance)
        {
            if (instance == null)
            {
                return null;
            }

            var traverse = Traverse.Create(instance);
            Character character = traverse.Field("character").GetValue<Character>();
            if (character != null)
            {
                return character;
            }

            return traverse.Property("character").GetValue<Character>();
        }
    }
}
