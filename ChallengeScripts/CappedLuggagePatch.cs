using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace Challenges.ChallengeScripts
{
    [HarmonyPatch(typeof(Luggage), "OpenLuggageRPC")]
    internal static class CappedLuggagePatch
    {
        private const string ChallengeId = "cappedluggage";

        private static void Prefix(ref bool spawnItems, ref bool __state)
        {
            bool shouldCap = spawnItems && ShouldCap();
            __state = shouldCap;
            if (!shouldCap)
            {
                return;
            }

            spawnItems = false;
        }

        private static void Postfix(Luggage __instance, bool __state)
        {
            if (!__state || __instance == null)
            {
                return;
            }

            __instance.StartCoroutine(SpawnSingleItem(__instance));
        }

        private static IEnumerator SpawnSingleItem(Luggage luggage)
        {
            if (luggage == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

            var traverse = Traverse.Create(luggage);
            var spawnSpots = traverse.Method("GetSpawnSpots").GetValue<IList>();
            if (!(spawnSpots?.Count > 0))
            {
                yield break;
            }

            IList singleSpotList;
            try
            {
                singleSpotList = Activator.CreateInstance(spawnSpots.GetType()) as IList;
            }
            catch
            {
                singleSpotList = null;
            }

            if (singleSpotList == null)
            {
                singleSpotList = spawnSpots;
                if (singleSpotList.IsFixedSize || singleSpotList.IsReadOnly)
                {
                    yield break;
                }

                for (int i = singleSpotList.Count - 1; i >= 1; i--)
                {
                    singleSpotList.RemoveAt(i);
                }
            }
            else
            {
                singleSpotList.Add(spawnSpots[0]);
            }

            traverse.Method("SpawnItems", singleSpotList).GetValue();
        }

        private static bool ShouldCap()
        {
            var registered = ChallengesAPI.challenges;
            if (registered == null)
            {
                return false;
            }

            if (!registered.ContainsKey(ChallengeId))
            {
                return false;
            }

            return ChallengesAPI.IsChallengeEnabled(ChallengeId);
        }
    }
}
