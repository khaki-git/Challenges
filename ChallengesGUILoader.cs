using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Challenges;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zorro.Core;

public static class ChallengesGUILoader
{
    private static GameObject _menuPrefab;
    private static GameObject _fallbackButtonPrefab;

    private static GameObject _menuInstance;
    private static Transform _listRoot;
    private static GameObject _buttonTemplate;
    private static GameObject _selectedPanel;
    private static Button _backButton;
    private static Button _goButton;
    private static MenuWindow _menuWindow;
    private static Challenge _selectedChallenge;
    private static readonly List<ChallengeKiosk> _linkedKiosks = new List<ChallengeKiosk>();

    private static int _page = 0;
    private static int _perPage = 5;

    private static LoaderHost _host;
    private static bool _pendingRefresh;

    public static void Load(GameObject challengesGUINew, GameObject challengeButtonNew)
    {
        _menuPrefab = challengesGUINew;
        _fallbackButtonPrefab = challengeButtonNew;

        ChallengesAPI.ChallengeListChanged -= OnChallengeListChanged;
        ChallengesAPI.ChallengeListChanged += OnChallengeListChanged;

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "Airport")
            {
                ChallengesAPI.DisableAllChallenges();
                CreateKiosk();
            }
        };

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded && activeScene.name == "Airport")
        {
            ChallengesAPI.DisableAllChallenges();
            CreateKiosk();
        }
    }

    public static int AssignMasterClientViewID(GameObject go)
    {
        int viewID = PhotonNetwork.AllocateViewID(false);
        var pv = go.GetComponent<PhotonView>();
        pv.ViewID = viewID;
        return viewID;
    }

    private static void Log(string msg) => Debug.Log($"[ChallengesGUI] {msg}");

    private static void CreateKiosk()
    {
        if (_menuInstance == null)
        {
            ResetCachedReferences();
        }

        if (_menuInstance != null)
        {
            if (!EnsureMenuStructure())
            {
                Log("Cached menu references missing; rebuilding GUI instance.");
                Object.Destroy(_menuInstance);
                ResetCachedReferences();
            }
            else
            {
                EnsureHost();
                LinkKiosksToMenu();

                if (_pendingRefresh && _listRoot != null)
                {
                    _pendingRefresh = false;
                    FillButtons();
                }
                else if (_listRoot != null)
                {
                    ChallengesAPI.EnsureChallengesRegistered();
                    FillButtons();
                }
                return;
            }
        }

        if (_menuPrefab == null)
        {
            Log("Menu prefab is null.");
            return;
        }

        _menuInstance = Object.Instantiate(_menuPrefab);
        _menuInstance.name = "ChallengesGUI";
        _menuInstance.SetActive(false);
        _menuWindow = _menuInstance.AddComponent<ChallengeMenuWindow>();

        EnsureHost();
        EnsureMenuStructure();

        LinkKiosksToMenu();

        _host.Boot(() =>
        {
            TryFillOrRetry();
            if (_pendingRefresh && _listRoot != null)
            {
                _pendingRefresh = false;
                FillButtons();
            }
        });
    }

    internal static void EnsureMenuReady()
    {
        if (_menuInstance == null)
        {
            CreateKiosk();
            return;
        }

        if (!EnsureMenuStructure())
        {
            Log("Menu structure invalid when ensuring readiness; rebuilding.");
            Object.Destroy(_menuInstance);
            ResetCachedReferences();
            CreateKiosk();
            return;
        }

        if (_listRoot == null)
        {
            Log("EnsureMenuReady aborted: list root missing after rebuild.");
            return;
        }

        ChallengesAPI.EnsureChallengesRegistered();

        if (_pendingRefresh)
        {
            _pendingRefresh = false;
            FillButtons();
        }
        else
        {
            FillButtons();
        }
    }

    private static void EnsureHost()
    {
        if (_menuInstance == null) return;
        if (_host != null) return;
        _host = _menuInstance.GetComponent<LoaderHost>() ?? _menuInstance.AddComponent<LoaderHost>();
    }

    private static bool EnsureMenuStructure()
    {
        if (_menuInstance == null) return false;

        if (_listRoot == null)
        {
            var content = _menuInstance.transform.FindChildRecursive("Content");
            var main = content ? content.Find("Main") : null;
            var list = main ? main.Find("ChallengeList") : null;
            _listRoot = list ? list.Find("Contents") : null;
            if (_listRoot == null) _listRoot = _menuInstance.transform.FindChildRecursive("Contents");
            Log($"List root: {(_listRoot ? _listRoot.GetHierarchyPath() : "null")}");
        }

        if (_listRoot != null && _buttonTemplate == null)
        {
            _buttonTemplate = _listRoot.Find("Challenge")?.gameObject;
            if (_buttonTemplate != null)
            {
                Log($"Button template: {_buttonTemplate.transform.GetHierarchyPath()}");
                _buttonTemplate.SetActive(false);
            }
            else
            {
                Log("Button template not found. Using fallback prefab.");
            }
        }

        if (_selectedPanel == null)
        {
            _selectedPanel = _menuInstance.transform.FindChildRecursive("SelectedChallenge")?.gameObject;
            Log($"Selected panel: {(_selectedPanel ? _selectedPanel.transform.GetHierarchyPath() : "null")}");
            if (_selectedPanel && _selectedChallenge == null)
                _selectedPanel.SetActive(false);
        }

        WireNavigationButtons();
        UpdateGoButtonState();

        return _listRoot != null;
    }

    private static void ResetCachedReferences()
    {
        _menuInstance = null;
        _listRoot = null;
        _buttonTemplate = null;
        _selectedPanel = null;
        _backButton = null;
        _goButton = null;
        _menuWindow = null;
        _host = null;
        _selectedChallenge = null;
        _pendingRefresh = false;
    }

    private static void LinkKiosksToMenu()
    {
        if (_menuInstance == null) return;

        PruneKioskList();

        var existingKiosks = Object.FindObjectsByType<ChallengeKiosk>(FindObjectsSortMode.None);
        if (existingKiosks != null && existingKiosks.Length > 0)
        {
            for (int i = 0; i < existingKiosks.Length; i++)
            {
                existingKiosks[i].challengesGUI = _menuInstance;
                TrackKiosk(existingKiosks[i]);
            }
            Log($"Linked GUI to {existingKiosks.Length} existing ChallengeKiosk instance(s).");
        }
        else
        {
            var baseKiosk = Object.FindFirstObjectByType<AirportCheckInKiosk>()?.gameObject;
            if (baseKiosk != null)
            {
                bool wasActive = baseKiosk.activeSelf;
                baseKiosk.SetActive(false);

                var clone = Object.Instantiate(baseKiosk, baseKiosk.transform.position, baseKiosk.transform.rotation, baseKiosk.transform.parent);
                clone.transform.position -= new Vector3(0, 0, .7f);
                clone.name = "ChallengesKiosk";

                var pv = clone.GetComponent<PhotonView>();
                if (pv != null) PhotonNetwork.AllocateViewID(pv);

                var old = clone.GetComponent<AirportCheckInKiosk>();
                if (old != null) Object.Destroy(old);

                var kiosk = clone.AddComponent<ChallengeKiosk>();
                kiosk.challengesGUI = _menuInstance;
                TrackKiosk(kiosk);

                clone.SetActive(true);
                baseKiosk.SetActive(wasActive);

                Log("Spawned ChallengesKiosk clone and linked GUI.");
            }
            else
            {
                Log("Base AirportCheckInKiosk not found; could not spawn kiosk.");
            }
        }

        if (_host == null)
            _host = _menuInstance.AddComponent<LoaderHost>();

        if (_pendingRefresh && _listRoot != null)
        {
            _pendingRefresh = false;
            FillButtons();
        }
    }

    private static void TryFillOrRetry()
    {
        ChallengesAPI.EnsureChallengesRegistered();

        Log($"Initial challenges count: {ChallengesAPI.challenges.Count}");
        FillButtons();

        if (ChallengesAPI.challenges.Count == 0)
            _host.StartCoroutine(_host.RetryUntil(() => ChallengesAPI.challenges.Count > 0, 8, 0.5f, FillButtons));
    }

    private static void OnChallengeListChanged()
    {
        if (_menuInstance == null || _listRoot == null)
        {
            _pendingRefresh = true;
            return;
        }

        _pendingRefresh = false;
        FillButtons();
    }

    private static void FillButtons()
    {
        if (_listRoot == null)
        {
            Log("FillButtons aborted: list root is null.");
            return;
        }

        ClearListKeepTemplate();

        int total = ChallengesAPI.challenges != null ? ChallengesAPI.challenges.Count : 0;
        Log($"Render page={_page} perPage={_perPage} total={total}");

        if (total == 0)
        {
            RebuildLayout();
            return;
        }

        var items = ChallengesAPI.challenges.Values.ToList();

        int start = _page * _perPage;
        if (start >= total)
        {
            _page = (total - 1) / _perPage;
            start = _page * _perPage;
        }

        int end = Mathf.Min(start + _perPage, total);
        Log($"Slice [{start}..{end})");

        for (int i = start; i < end; i++)
        {
            var c = items[i];
            var bt = SpawnButton(c.title);
            if (bt == null) continue;
            WireChallengeButton(bt, c);
            Log($"Spawned button: {c.title}");
        }

        bool hasPrev = _page > 0;
        bool hasNext = end < total;

        if (hasPrev)
        {
            var prev = SpawnButton("Previous");
            if (prev != null) prev.onClick.AddListener(PrevPage);
        }

        if (hasNext)
        {
            var next = SpawnButton("Next");
            if (next != null) next.onClick.AddListener(NextPage);
        }

        RebuildLayout();

        if (_selectedPanel != null && _selectedChallenge == null)
        {
            _selectedPanel.SetActive(false);
        }
    }

    private static void ClearListKeepTemplate()
    {
        for (int i = _listRoot.childCount - 1; i >= 0; i--)
        {
            var g = _listRoot.GetChild(i).gameObject;
            if (_buttonTemplate != null && ReferenceEquals(g, _buttonTemplate)) continue;
            Object.Destroy(g);
        }
    }

    private static Button SpawnButton(string label)
    {
        GameObject prefab = _buttonTemplate ?? _fallbackButtonPrefab;
        if (prefab == null)
        {
            Log("No button prefab available.");
            return null;
        }

        var go = Object.Instantiate(prefab, _listRoot, false);
        go.name = $"{prefab.name}_Clone";
        go.SetActive(true);

        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        TMP_Text txt = null;
        var t = go.transform.Find("Text");
        if (t) txt = t.GetComponent<TMP_Text>();
        if (txt == null) txt = go.GetComponentInChildren<TMP_Text>(true);
        if (txt != null) txt.text = label;

        btn.onClick.RemoveAllListeners();
        return btn;
    }

    private static void WireChallengeButton(Button btn, Challenge c)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SelectChallenge(c));

        if (c.enabled && (_selectedChallenge == null || _selectedChallenge.id != c.id))
        {
            SelectChallenge(c);
        }
    }

    private static void SelectChallenge(Challenge challenge)
    {
        if (_selectedPanel == null || challenge == null) return;

        _selectedChallenge = challenge;

        if (challenge.ascentOverride.HasValue)
        {
            Ascents.currentAscent = challenge.ascentOverride.Value;
        }

        _selectedPanel.SetActive(true);

        var tTitle = _selectedPanel.transform.Find("Title");
        var tSub = _selectedPanel.transform.Find("Subtext");
        var tBody = _selectedPanel.transform.Find("Description")?.Find("Body");

        var title = tTitle ? tTitle.GetComponent<TMP_Text>() : null;
        var sub = tSub ? tSub.GetComponent<TMP_Text>() : null;
        var body = tBody ? tBody.GetComponent<TMP_Text>() : null;

        if (title != null) title.text = challenge.title;
        if (sub != null)
        {
            sub.color = DifficultyColour(challenge.difficulty);
            sub.text = challenge.difficulty.ToString();
        }
        if (body != null) body.text = challenge.description;

        UpdateGoButtonState();
        Log($"Selected: {challenge.title} ({challenge.difficulty})");
    }

    private static void WireNavigationButtons()
    {
        if (_menuInstance == null) return;

        var backTransform = _menuInstance.transform.FindChildRecursive("BACK");
        if (backTransform != null)
        {
            _backButton = backTransform.GetComponent<Button>() ?? backTransform.gameObject.AddComponent<Button>();
            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(HandleBackPressed);
        }

        var goTransform = _menuInstance.transform.FindChildRecursive("Go");
        if (goTransform == null && _selectedPanel != null)
        {
            goTransform = _selectedPanel.transform.FindChildRecursive("Go");
        }

        if (goTransform != null)
        {
            _goButton = goTransform.GetComponent<Button>() ?? goTransform.gameObject.AddComponent<Button>();
            _goButton.onClick.RemoveAllListeners();
            _goButton.onClick.AddListener(HandleGoPressed);
        }
    }

    private static void HandleBackPressed()
    {
        MenuWindow.CloseAllWindows();

        if (_menuInstance != null)
            _menuInstance.SetActive(false);

        _selectedChallenge = null;
        if (_selectedPanel != null)
            _selectedPanel.SetActive(false);

        UpdateGoButtonState();
    }

    private static void HandleGoPressed()
    {
        if (_selectedChallenge == null) return;

        ChallengesAPI.SetSingularChallengeEnabled(_selectedChallenge.id);

        var kiosk = GetAnyKiosk();
        if (kiosk != null)
        {
            int ascent = Ascents.currentAscent;
            kiosk.StartGame(ascent);
        }
        else
        {
            Log("No ChallengeKiosk available to start the run.");
        }
    }

    private static void UpdateGoButtonState()
    {
        if (_goButton == null) return;
        _goButton.interactable = _selectedChallenge != null;
    }

    private static void TrackKiosk(ChallengeKiosk kiosk)
    {
        if (kiosk == null) return;
        if (!_linkedKiosks.Contains(kiosk))
            _linkedKiosks.Add(kiosk);
    }

    private static void PruneKioskList()
    {
        for (int i = _linkedKiosks.Count - 1; i >= 0; i--)
        {
            if (_linkedKiosks[i] == null)
                _linkedKiosks.RemoveAt(i);
        }
    }

    private static ChallengeKiosk GetAnyKiosk()
    {
        PruneKioskList();
        if (_linkedKiosks.Count > 0)
            return _linkedKiosks[0];

        var kiosk = Object.FindFirstObjectByType<ChallengeKiosk>();
        TrackKiosk(kiosk);
        return kiosk;
    }

    public static void NextPage()
    {
        int total = ChallengesAPI.challenges != null ? ChallengesAPI.challenges.Count : 0;
        if (((_page + 1) * _perPage) >= total) return;
        _page++;
        FillButtons();
    }

    public static void PrevPage()
    {
        if (_page == 0) return;
        _page--;
        FillButtons();
    }

    private static void RebuildLayout()
    {
        var rt = _listRoot as RectTransform;
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private static Color DifficultyColour(ChallengeDifficulty d)
    {
        switch (d)
        {
            case ChallengeDifficulty.LIGHT: return new Color(0.6f, 1.0f, 0.6f);
            case ChallengeDifficulty.EASY: return new Color(0.1f, 0.9f, 0.1f);
            case ChallengeDifficulty.MEDIUM: return new Color(1.0f, 0.85f, 0.1f);
            case ChallengeDifficulty.MEDIUMCORE: return new Color(1.0f, 0.55f, 0.0f);
            case ChallengeDifficulty.HARD: return new Color(0.9f, 0.1f, 0.1f);
            case ChallengeDifficulty.HARDCORE: return new Color(0.5f, 0.0f, 0.0f);
            default: return Color.white;
        }
    }

    private class LoaderHost : MonoBehaviour
    {
        public void Boot(System.Action onReady)
        {
            StartCoroutine(BootNextFrame(onReady));
        }

        private IEnumerator BootNextFrame(System.Action onReady)
        {
            yield return null;
            onReady?.Invoke();
        }

        public IEnumerator RetryUntil(System.Func<bool> condition, int attempts, float delaySeconds, System.Action onTick)
        {
            for (int i = 1; i <= attempts; i++)
            {
                yield return new WaitForSeconds(delaySeconds);
                Log($"Retry {i}/{attempts} – challenges={ (ChallengesAPI.challenges != null ? ChallengesAPI.challenges.Count : 0) }");
                onTick?.Invoke();
                if (condition()) yield break;
            }
            Log("Retries exhausted.");
        }
    }
}

public static class TransformFindExtensions
{
    public static Transform FindChildRecursive(this Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
            var r = c.FindChildRecursive(name);
            if (r != null) return r;
        }
        return null;
    }

    public static string GetHierarchyPath(this Transform t)
    {
        if (t == null) return "null";
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack);
    }
}
