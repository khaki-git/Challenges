using System;
using System.Reflection;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Challenges.ChallengeScripts
{
    public static class SnowPatch
    {
        private const string FrostbiteId = "permasnow";
        private const float ForcedStormDuration = 999999f;
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;

            _initialized = true;
            ChallengesAPI.SceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsFrostbiteEnabled() || !IsGameplayScene(scene)) return;

            ApplyStorm(scene);
        }

        private static bool IsGameplayScene(Scene scene)
        {
            if (!scene.IsValid()) return false;

            var name = scene.name ?? string.Empty;
            return name == "WilIsland" || name.StartsWith("level_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFrostbiteEnabled()
        {
            return ChallengesAPI.challenges != null
                   && ChallengesAPI.challenges.ContainsKey(FrostbiteId)
                   && ChallengesAPI.IsChallengeEnabled(FrostbiteId);
        }

        private static void ApplyStorm(Scene scene)
        {
            var zones = FindWindChillZones();
            if (zones == null || zones.Length == 0) return;

            for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null || zone.gameObject.scene != scene) continue;

                AttachController(zone);
            }
        }

        private static WindChillZone[] FindWindChillZones()
        {
            try
            {
#if UNITY_2023_1_OR_NEWER
                return UnityEngine.Object.FindObjectsByType<WindChillZone>(FindObjectsSortMode.None);
#else
                WindChillZone[] zones;
#pragma warning disable CS0618
                zones = UnityEngine.Object.FindObjectsOfType<WindChillZone>(includeInactive: true);
#pragma warning restore CS0618
                return zones;
#endif
            }
            catch
            {
#if UNITY_2023_1_OR_NEWER
                return UnityEngine.Object.FindObjectsByType<WindChillZone>(FindObjectsSortMode.None);
#else
                WindChillZone[] zones;
#pragma warning disable CS0618
                zones = UnityEngine.Object.FindObjectsOfType<WindChillZone>();
#pragma warning restore CS0618
                return zones;
#endif
            }
        }

        private static void AttachController(WindChillZone zone)
        {
            if (zone.gameObject.GetComponent<FrostbiteWindController>() != null) return;
            zone.gameObject.AddComponent<FrostbiteWindController>();
        }

        private class FrostbiteWindController : MonoBehaviour
        {
            private static readonly Vector2 ForcedOnRange = new Vector2(ForcedStormDuration, ForcedStormDuration);
            private static readonly Vector2 ForcedOffRange = Vector2.zero;
            private static readonly MethodInfo ToggleWind = typeof(WindChillZone)
                .GetMethod("RPCA_ToggleWind", BindingFlags.Instance | BindingFlags.NonPublic);
            private static readonly MethodInfo RandomWind = typeof(WindChillZone)
                .GetMethod("RandomWindDirection", BindingFlags.Instance | BindingFlags.NonPublic);

            private WindChillZone _zone;
            private PhotonView _view;
            private float _retryTimer;

            private void Awake()
            {
                _zone = GetComponent<WindChillZone>();
                _view = GetComponent<PhotonView>();
            }

            private void OnEnable()
            {
                EnsureStorm(forceRpc: true);
            }

            private void Update()
            {
                EnsureStorm();
            }

            private void EnsureStorm(bool forceRpc = false)
            {
                if (_zone == null)
                {
                    enabled = false;
                    return;
                }

                _zone.windTimeRangeOn = ForcedOnRange;
                _zone.windTimeRangeOff = ForcedOffRange;

                if (_zone.windActive && !forceRpc)
                {
                    // Keep timers topped up so the base behaviour never flips us off.
                    MaintainSwitchTimer();
                    return;
                }

                TryActivateStorm();
            }

            private void MaintainSwitchTimer()
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    // Reset timer occasionally to avoid float imprecision over very long sessions.
                    _retryTimer -= Time.deltaTime;
                    if (_retryTimer > 0f) return;
                    _retryTimer = 30f;
                    TryActivateStorm();
                }
            }

            private void TryActivateStorm()
            {
                Vector3 dir = Vector3.forward;
                if (RandomWind != null)
                {
                    try
                    {
                        dir = (Vector3)RandomWind.Invoke(_zone, null);
                    }
                    catch
                    {
                        // ignore and fall back to Vector3.forward
                    }
                }

                if (PhotonNetwork.IsMasterClient && _view != null)
                {
                    _view.RPC("RPCA_ToggleWind", RpcTarget.All, true, dir);
                }
                else
                {
                    ToggleWind?.Invoke(_zone, new object[] { true, dir });
                }
            }
        }
    }
}
