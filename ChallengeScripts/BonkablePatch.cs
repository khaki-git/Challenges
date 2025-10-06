using HarmonyLib;

namespace Challenges.ChallengeScripts
{
    public static class BonkablePatch
    {
        [HarmonyPatch(typeof(Bonkable))]
        [HarmonyPatch("Awake")]
        private static class BonkableFriendslopPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Bonkable __instance)
            {
                if (ChallengesAPI.IsChallengeEnabled("friendslop"))
                {
                    __instance.bonkForce *= 5;
                    __instance.ragdollTime *= 5;
                }
            }
        }
        
        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("Awake")]
        private static class ItemFriendslopPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Item __instance)
            {
                if (__instance.gameObject.GetComponent<Bonkable>() == null && ChallengesAPI.IsChallengeEnabled("friendslop"))
                {
                    __instance.gameObject.AddComponent<Bonkable>();
                }
            }
        }
    }
}