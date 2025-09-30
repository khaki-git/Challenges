using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum ChallengeDifficulty
{
    LIGHT,
    EASY,
    MEDIUM,
    MEDIUMCORE,
    HARD,
    HARDCORE,
}

public class Challenge
{
    public string id { get; }
    public string title;
    public string description;
    public ChallengeDifficulty difficulty;
    public bool enabled = false;

    public int? ascentOverride { get; }

    public Challenge(
        string id,
        string title = "My Challenge",
        string description = "Custom Challenge for PEAK",
        ChallengeDifficulty difficulty = ChallengeDifficulty.MEDIUM,
        int? ascentOverride = null
    )
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.difficulty = difficulty;
        this.ascentOverride = ascentOverride;
    }
}

public static class ChallengesAPI
{
    public static bool IsHell = false;
    public const string HellChallengeId = "hell";
    public static Dictionary<string, Challenge> challenges = new Dictionary<string, Challenge>();

    public static event Action ChallengeListChanged;

    private static readonly List<Challenge> _registeredChallenges = new List<Challenge>();

    private static event Action<Scene, LoadSceneMode> _sceneLoaded;
    private static bool _sceneHooked;

    public static event Action<Scene, LoadSceneMode> SceneLoaded
    {
        add
        {
            _sceneLoaded += value;
            UpdateSceneSubscription();
        }
        remove
        {
            _sceneLoaded -= value;
            UpdateSceneSubscription();
        }
    }

    public static void RegisterChallenge(Challenge challenge)
    {
        if (challenge == null || string.IsNullOrEmpty(challenge.id)) return;

        EnsureChallengeDictionary();

        Challenge existing;
        if (challenges != null && challenges.TryGetValue(challenge.id, out existing))
        {
            challenge.enabled = existing.enabled;
        }

        int storedIndex = _registeredChallenges.FindIndex(c => c != null && c.id == challenge.id);
        if (storedIndex >= 0)
        {
            _registeredChallenges[storedIndex] = challenge;
        }
        else
        {
            _registeredChallenges.Add(challenge);
        }

        challenges[challenge.id] = challenge;
        ChallengeListChanged?.Invoke();
        UpdateSceneSubscription();
    }

    public static bool IsChallengeEnabled(string id)
    {
        EnsureChallengeDictionary();

        if (IsHell) return true;
        if (challenges == null) return false;
        return challenges.TryGetValue(id, out var challenge) && challenge.enabled;
    }

    public static void SetSingularChallengeEnabled(string id)
    {
        EnsureChallengeDictionary();
        if (challenges == null || challenges.Count == 0) return;

        bool changed = false;
        bool enableHellMode = !string.IsNullOrEmpty(id) && id == HellChallengeId;

        if (IsHell != enableHellMode)
        {
            IsHell = enableHellMode;
            changed = true;
        }

        foreach (var challenge in challenges.Values)
        {
            bool enable = enableHellMode ? true : challenge.id == id;
            if (challenge.enabled == enable) continue;
            challenge.enabled = enable;
            changed = true;
        }

        if (changed)
        {
            ChallengeListChanged?.Invoke();
            UpdateSceneSubscription();
        }
    }

    public static bool IsOneOfTheseChallengesEnabled(string[] challengeList)
    {
        foreach (var challenge in challengeList)
        {
            if (IsChallengeEnabled(challenge)) return true;
        }
        return false;
    }

    public static void DisableAllChallenges()
    {
        EnsureChallengeDictionary();
        if (challenges == null || challenges.Count == 0) return;

        bool changed = false;
        foreach (var challenge in challenges.Values)
        {
            if (!challenge.enabled) continue;
            challenge.enabled = false;
            changed = true;
        }

        if (changed)
        {
            ChallengeListChanged?.Invoke();
            UpdateSceneSubscription();
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _sceneLoaded?.Invoke(scene, mode);

        if (!AnyChallengeActive())
        {
            UpdateSceneSubscription();
        }
    }

    private static bool AnyChallengeActive()
    {
        EnsureChallengeDictionary();

        if (IsHell) return true;
        if (challenges == null || challenges.Count == 0) return false;

        foreach (var challenge in challenges.Values)
        {
            if (challenge.enabled) return true;
        }

        return false;
    }

    private static void UpdateSceneSubscription()
    {
        bool shouldHook = _sceneLoaded != null && AnyChallengeActive();

        if (shouldHook && !_sceneHooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            _sceneHooked = true;
        }
        else if (!shouldHook && _sceneHooked)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _sceneHooked = false;
        }
    }

    internal static void EnsureChallengesRegistered()
    {
        EnsureChallengeDictionary();
    }

    private static void EnsureChallengeDictionary()
    {
        if (challenges == null)
            challenges = new Dictionary<string, Challenge>();

        if (challenges.Count > 0) return;
        if (_registeredChallenges.Count == 0) return;

        for (int i = 0; i < _registeredChallenges.Count; i++)
        {
            var stored = _registeredChallenges[i];
            if (stored == null || string.IsNullOrEmpty(stored.id)) continue;
            challenges[stored.id] = stored;
        }
    }
}
