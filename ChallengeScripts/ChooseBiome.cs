using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Challenges.ChallengeScripts
{
    public static class ChooseBiome
    {
        private const string FrostbiteId = "permasnow";
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;

            _initialized = true;
            ChallengesAPI.SceneLoaded += HandleSceneLoaded;
        }

        public static void SetMesa()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            ToggleSceneObject(scene, "Map/Biome_3/Desert", true);
            ToggleSceneObject(scene, "Map/Biome_3/Snow", false);
        }

        public static void SetAlpines()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            ToggleSceneObject(scene, "Map/Biome_3/Desert", false);
            ToggleSceneObject(scene, "Map/Biome_3/Snow", true);
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsGameplayScene(scene)) return;

            if (IsFrostbiteEnabled())
            {
                SetAlpines();
            }
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

        private static void ToggleSceneObject(Scene scene, string hierarchyPath, bool enable)
        {
            var target = FindSceneObject(scene, hierarchyPath);
            if (target == null)
            {
                Debug.LogWarning($"[ChooseBiome] Could not find object '{hierarchyPath}' in scene '{scene.name}'.");
                return;
            }

            if (target.activeSelf != enable)
            {
                target.SetActive(enable);
            }
        }

        private static GameObject FindSceneObject(Scene scene, string hierarchyPath)
        {
            if (!scene.isLoaded || string.IsNullOrEmpty(hierarchyPath)) return null;

            var segments = hierarchyPath.Split('/');
            if (segments.Length == 0) return null;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var found = FindByPathRecursive(roots[i].transform, segments, 0);
                if (found != null) return found.gameObject;
            }

            return null;
        }

        private static Transform FindByPathRecursive(Transform current, string[] segments, int index)
        {
            if (current == null || segments == null || index >= segments.Length) return null;

            if (!string.Equals(current.name, segments[index], StringComparison.Ordinal))
            {
                if (index == 0)
                {
                    for (int i = 0; i < current.childCount; i++)
                    {
                        var fallback = FindByPathRecursive(current.GetChild(i), segments, index);
                        if (fallback != null) return fallback;
                    }
                }
                return null;
            }

            if (index == segments.Length - 1)
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                var match = FindByPathRecursive(current.GetChild(i), segments, index + 1);
                if (match != null) return match;
            }

            return null;
        }
    }
}
