using HarmonyLib;
using Photon.Pun;

namespace Challenges.ChallengeScripts
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPCA_PassOut))]
    internal static class InstagibPassOutPatch
    {
        private const string InstagibChallengeId = "instagib";
        private const string DieInstantlyMethodName = "DieInstantly";

        private static void Postfix(Character __instance)
        {
            if (__instance == null || !ShouldInstagib())
            {
                return;
            }

            if (NarcolepsyPatch.ShouldPreventInstagib(__instance))
            {
                return;
            }

            PhotonView photonView = __instance.photonView;
            if (photonView == null || !photonView.IsMine)
            {
                return;
            }

            __instance.Invoke(DieInstantlyMethodName, 0.02f);
        }

        private static bool ShouldInstagib()
        {
            var registeredChallenges = ChallengesAPI.challenges;
            if (registeredChallenges == null)
            {
                return false;
            }

            if (!registeredChallenges.ContainsKey(InstagibChallengeId))
            {
                return false;
            }

            return ChallengesAPI.IsChallengeEnabled(InstagibChallengeId);
        }
    }
}
