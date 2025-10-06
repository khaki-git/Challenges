using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public static class ChallengeMultiplayerSync
{
    // Photon reserves event codes >= 200 for internal use; keep ours well below that range.
    private const byte SelectionEventCode = 101;
    private const byte StartRequestEventCode = 102;
    private const byte StartConfirmedEventCode = 103;

    private const string SelectedChallengeProp = "challenge.selected";
    private const string SelectedAscentProp = "challenge.ascent";

    private static bool _initialized;
    private static CallbackTarget _callbackTarget;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _callbackTarget = new CallbackTarget();
        PhotonNetwork.AddCallbackTarget(_callbackTarget);
        _initialized = true;

        if (PhotonNetwork.CurrentRoom != null)
        {
            _callbackTarget.ApplySelectionFromProperties(PhotonNetwork.CurrentRoom.CustomProperties);
        }
    }

    public static void BroadcastSelection(Challenge challenge)
    {
        Initialize();

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        var challengeId = challenge != null ? challenge.id : string.Empty;
        object ascentPayload = challenge != null ? (object)Ascents.currentAscent : null;

        var payload = new object[] { challengeId, ascentPayload };
        PhotonNetwork.RaiseEvent(
            SelectionEventCode,
            payload,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );

        var props = new Hashtable
        {
            [SelectedChallengeProp] = string.IsNullOrEmpty(challengeId) ? null : challengeId,
            [SelectedAscentProp] = ascentPayload
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void NotifyAirportLoaded()
    {
        Initialize();

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || !PhotonNetwork.IsMasterClient)
            return;

        var props = new Hashtable
        {
            [SelectedChallengeProp] = null,
            [SelectedAscentProp] = null
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void RequestChallengeStart(int ascent)
    {
        Initialize();

        if (!PhotonNetwork.InRoom)
        {
            ChallengeKiosk.BeginIslandLoad(ChallengeKiosk.DetermineSceneName(), ascent);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            HandleMasterStartRequest(ascent);
            return;
        }

        PhotonNetwork.RaiseEvent(
            StartRequestEventCode,
            ascent,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    internal static void HandleMasterStartRequest(int ascent)
    {
        var sceneName = ChallengeKiosk.DetermineSceneName();
        var payload = new object[] { sceneName, ascent };

        PhotonNetwork.RaiseEvent(
            StartConfirmedEventCode,
            payload,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    private sealed class CallbackTarget : IOnEventCallback, IInRoomCallbacks, IMatchmakingCallbacks
    {
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent == null)
                return;

            switch (photonEvent.Code)
            {
                case SelectionEventCode:
                    HandleSelectionEvent(photonEvent.CustomData);
                    break;
                case StartRequestEventCode:
                    HandleStartRequestEvent(photonEvent.CustomData, photonEvent.Sender);
                    break;
                case StartConfirmedEventCode:
                    HandleStartConfirmedEvent(photonEvent.CustomData);
                    break;
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            ApplySelectionFromProperties(propertiesThatChanged);
        }

        public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps) { }

        public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) { }

        public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) { }

        public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) { }

        public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> friendList) { }

        public void OnCreatedRoom() { }

        public void OnCreateRoomFailed(short returnCode, string message) { }

        public void OnJoinRoomFailed(short returnCode, string message) { }

        public void OnJoinRandomFailed(short returnCode, string message) { }

        public void OnLeftRoom() { }

        public void OnJoinedRoom()
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                ApplySelectionFromProperties(PhotonNetwork.CurrentRoom.CustomProperties);
            }
        }

        public void ApplySelectionFromProperties(Hashtable props)
        {
            if (props == null || props.Count == 0)
                return;

            if (!props.ContainsKey(SelectedChallengeProp) && !props.ContainsKey(SelectedAscentProp))
                return;

            props.TryGetValue(SelectedChallengeProp, out var challengeObj);
            props.TryGetValue(SelectedAscentProp, out var ascentObj);

            var challengeId = challengeObj as string;
            int? ascent = null;
            if (ascentObj != null)
            {
                try
                {
                    ascent = Convert.ToInt32(ascentObj);
                }
                catch
                {
                    ascent = null;
                }
            }

            ChallengesGUILoader.ApplyNetworkSelection(string.IsNullOrEmpty(challengeId) ? null : challengeId, ascent);
        }

        private void HandleSelectionEvent(object payload)
        {
            if (!(payload is object[] array) || array.Length == 0)
                return;

            var selectionId = array[0] as string;
            int? ascent = null;
            if (array.Length > 1 && array[1] != null)
            {
                try
                {
                    ascent = Convert.ToInt32(array[1]);
                }
                catch
                {
                    ascent = null;
                }
            }

            ChallengesGUILoader.ApplyNetworkSelection(string.IsNullOrEmpty(selectionId) ? null : selectionId, ascent);
        }

        private void HandleStartRequestEvent(object payload, int sender)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            int ascent;
            try
            {
                ascent = Convert.ToInt32(payload);
            }
            catch
            {
                ascent = Ascents.currentAscent;
            }

            HandleMasterStartRequest(ascent);
        }

        private void HandleStartConfirmedEvent(object payload)
        {
            if (!(payload is object[] array) || array.Length < 2)
                return;

            var sceneName = array[0] as string;
            int ascent;
            try
            {
                ascent = Convert.ToInt32(array[1]);
            }
            catch
            {
                ascent = Ascents.currentAscent;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                sceneName = ChallengeKiosk.DetermineSceneName();
            }

            ChallengeKiosk.BeginIslandLoad(sceneName, ascent);
        }
    }
}
