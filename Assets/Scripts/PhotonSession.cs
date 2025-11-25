using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using PlayFab;
using UnityEngine.SceneManagement;
using Google;

public class PhotonSession : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static PhotonSession Instance { get; private set; }
    private string currentPlayFabId;
    private string localSessionToken;
    private const byte EVENT_KICK = 1;

    void Awake()
    {
         Instance = this;
    }

    public void ConnectToPhoton(string playFabId, string sessionToken)
    {
        currentPlayFabId = playFabId;
        localSessionToken = sessionToken;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = sessionToken; // nickname = token
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            JoinUserRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon connected.");
        JoinUserRoom();
    }

    private void JoinUserRoom()
    {
        string roomName = $"USER_{currentPlayFabId}";
        var options = new RoomOptions { MaxPlayers = 2, IsVisible = false, IsOpen = true };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room USER_{currentPlayFabId} with {PhotonNetwork.CurrentRoom.PlayerCount} players.");

        // Gửi event kick cho thiết bị khác
        PhotonNetwork.RaiseEvent(
            EVENT_KICK,
            localSessionToken,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            new SendOptions { Reliability = true });
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != EVENT_KICK) return;

        string newToken = photonEvent.CustomData as string;
        string storedToken = PlayFabLogin.Instance.playerData.sessionTokenKey;

        if (string.IsNullOrEmpty(storedToken) || storedToken != newToken)
        {
            NotiButton.Instance.ShowNotice("Tài khoản đang đăng nhập trên thiết bị khác");
            StartCoroutine(LogoutRoutine());
        }
    }

    private IEnumerator LogoutRoutine()
    {
        GoogleSignIn.DefaultInstance.Disconnect();
        PhotonNetwork.Disconnect();
        PlayFabLogin.Instance.playerData.sessionTokenKey = "";
        yield return new WaitForSeconds(0.2f);
        PlayerDataSyncManager.SaveGameToPlayFab(PlayFabLogin.Instance.playerData, CheckBothSaved);
    }
    void CheckBothSaved()
    {
        TouchManager.IsPanelOpen = false;
        // Reset các thông tin đăng nhập
        PlayFabLogin.Instance.playerData.ResetAll();
        PlayFabLogin.ResetInstance();
        PlayFabClientAPI.ForgetAllCredentials();
        // Chuyển scene
        SceneManager.LoadScene("SignIn");
    }

    public override void OnEnable()
    {
        base.OnEnable(); // gọi code gốc trong MonoBehaviourPunCallbacks
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

}
