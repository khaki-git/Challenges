// AirportCheckInKiosk.cs
using System;
using System.Collections;
using System.Reflection;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
using HarmonyLib;

// If you prefer Harmony, you can replace the reflection helper with AccessTools.MethodDelegate.
// using HarmonyLib;

public class ChallengeKiosk : MonoBehaviourPun, IInteractibleConstant, IInteractible
{
    public float interactTime = 0.5f;
    public GameObject challengesGUI;

    private MaterialPropertyBlock mpb;

    private MeshRenderer[] _mr;

    private MeshRenderer[] meshRenderers
    {
        get
        {
            if (_mr == null)
            {
                _mr = GetComponentsInChildren<MeshRenderer>();
                MonoBehaviour.print(_mr.Length);
            }
            return _mr;
        }
        set => _mr = value;
    }

    public bool holdOnFinish { get; }

    public bool IsInteractible(Character interactor) => true;

    public void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (GameHandler.GetService<NextLevelService>().Data.IsSome)
        {
            Debug.Log($"seconds left until next map... {GameHandler.GetService<NextLevelService>().Data.Value.SecondsLeft}");
        }
        GameHandler.GetService<RichPresenceService>().SetState(RichPresenceState.Status_Airport);
    }

    public void Interact(Character interactor) { }

    public void HoverEnter()
    {
        if (mpb == null) return;

        mpb.SetFloat(Item.PROPERTY_INTERACTABLE, 1f);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null)
                meshRenderers[i].SetPropertyBlock(mpb);
        }
    }

    public void HoverExit()
    {
        if (mpb == null) return;

        mpb.SetFloat(Item.PROPERTY_INTERACTABLE, 0f);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].SetPropertyBlock(mpb);
        }
    }

    public Vector3 Center() => transform.position;

    public Transform GetTransform() => transform;

    public string GetInteractionText() => LocalizedText.GetText("BOARDFLIGHT");

    public string GetName() => "CHALLENGE KIOSK";

    public bool IsConstantlyInteractable(Character interactor) => IsInteractible(interactor);

    public float GetInteractTime(Character interactor) => interactTime;

    private static readonly Action<MenuWindow> OpenMenu =
        AccessTools.MethodDelegate<Action<MenuWindow>>(AccessTools.Method(typeof(MenuWindow), "Open"));

    public void Interact_CastFinished(Character interactor)
    {
        ChallengesGUILoader.EnsureMenuReady();

        if (challengesGUI == null)
        {
            Debug.LogWarning("ChallengeKiosk.Interact_CastFinished called without a linked challenges GUI instance.");
            return;
        }

        var mw = challengesGUI.GetComponent<MenuWindow>();
        if (mw == null)
        {
            Debug.LogWarning("ChallengeKiosk.Interact_CastFinished could not find MenuWindow on challenges GUI instance.");
            return;
        }
        OpenMenu(mw);
    }

    public void StartGame(int ascent)
    {
        if (photonView == null)
        {
            Debug.LogWarning("ChallengeKiosk.StartGame called without a PhotonView instance.");
            return;
        }

        photonView.RPC("LoadIslandMaster", RpcTarget.MasterClient, ascent);
    }

    public void CancelCast(Character interactor) {}

    public void ReleaseInteract(Character interactor) { }

    // -----------------------
    // Scene loading RPC flow
    // -----------------------

    [PunRPC]
    public void LoadIslandMaster(int ascent)
    {
        MenuWindow.CloseAllWindows();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Loading scene as master.");
            NextLevelService service = GameHandler.GetService<NextLevelService>();

            string sceneName = "WilIsland";
            if (service.Data.IsSome)
            {
                sceneName = SingletonAsset<MapBaker>.Instance.GetLevel(service.Data.Value.CurrentLevelIndex);
            }
            else if (PhotonNetwork.OfflineMode)
            {
                sceneName = SingletonAsset<MapBaker>.Instance.GetLevel(0);
            }

            if (string.IsNullOrEmpty(sceneName))
                sceneName = "WilIsland";

            photonView.RPC("BeginIslandLoadRPC", RpcTarget.All, sceneName, ascent);
        }
    }

    [PunRPC]
    public void BeginIslandLoadRPC(string sceneName, int ascent)
    {
        GameHandler.AddStatus<SceneSwitchingStatus>(new SceneSwitchingStatus());
        Debug.Log("Begin scene load RPC: " + sceneName);

        Ascents.currentAscent = ascent;

        var loader = RetrievableResourceSingleton<LoadingScreenHandler>.Instance;

        // Call the internal method via reflection and pass the IEnumerator to Load(...)
        IEnumerator loadProcess = LoadSceneProcess_Internal(loader, sceneName, networked: true, yieldForCharacterSpawn: true, delay: 0f);

        loader.Load(LoadingScreen.LoadingScreenType.Plane, null, loadProcess);
    }

    // -----------------------
    // Reflection helper
    // -----------------------

    private static MethodInfo _miLoadSceneProcess;

    /// <summary>
    /// Invokes internal IEnumerator LoadSceneProcess(string, bool, bool, float) on LoadingScreenHandler.
    /// </summary>
    private static IEnumerator LoadSceneProcess_Internal(object handler, string sceneName, bool networked, bool yieldForCharacterSpawn, float delay)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        if (_miLoadSceneProcess == null)
        {
            // Internal instance method in the game's assembly:
            // IEnumerator LoadSceneProcess(string sceneName, bool networked, bool yieldForCharacterSpawn, float delay)
            _miLoadSceneProcess = handler.GetType().GetMethod(
                "LoadSceneProcess",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(string), typeof(bool), typeof(bool), typeof(float) },
                modifiers: null
            );

            if (_miLoadSceneProcess == null)
                throw new MissingMethodException(handler.GetType().FullName, "LoadSceneProcess(string, bool, bool, float)");
        }

        var enumerator = _miLoadSceneProcess.Invoke(handler, new object[] { sceneName, networked, yieldForCharacterSpawn, delay }) as IEnumerator;
        if (enumerator == null)
            throw new InvalidOperationException("LoadSceneProcess did not return an IEnumerator.");

        return enumerator;
    }

    // -----------------------
    // Harmony alternative
    // -----------------------
    /*
    private static Func<LoadingScreenHandler, string, bool, bool, float, IEnumerator> _loadSceneProcessDelegate;

    private static IEnumerator LoadSceneProcess_Internal(LoadingScreenHandler handler, string sceneName, bool networked, bool yieldForCharacterSpawn, float delay)
    {
        if (_loadSceneProcessDelegate == null)
        {
            var mi = AccessTools.Method(typeof(LoadingScreenHandler), "LoadSceneProcess",
                new[] { typeof(string), typeof(bool), typeof(bool), typeof(float) });

            _loadSceneProcessDelegate =
                AccessTools.MethodDelegate<Func<LoadingScreenHandler, string, bool, bool, float, IEnumerator>>(mi);
        }

        return _loadSceneProcessDelegate(handler, sceneName, networked, yieldForCharacterSpawn, delay);
    }
    */
}
