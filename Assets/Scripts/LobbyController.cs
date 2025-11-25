using Google;
using Photon.Pun;
using PlayFab;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PlayFabLogin;

public class LobbyController : MonoBehaviour
{
    public TMP_Text txtName;
    public PlayerData playerData;
    public GameObject PanelRank;
    public GameObject PanelTask;
    public GameObject PanelBag;
    public GameObject PanelBuyCoins;
    public GameObject PanelBuyEnergy;
    public GameObject PanelBuyEnergyVip;
    public GameObject PanelThuHoach;
    public GameObject noticePanel;
    public Button btnDangXuat;
    public TextMeshProUGUI noticeText;
    public Item ItemPanelSelect;
    public TMP_Text txtTimeGiveCard;
    public List<ProDuceManager> listProduceManagers;

    public static LobbyController Instance;

    void Awake()
    {
        Time.timeScale = 1f;
        Instance = this;
        txtName.text = playerData.playerName;
        if(playerData.isGiveCardFree) txtTimeGiveCard.SetText("00:00:00");
        PanelThuHoach.SetActive(false);
        PanelBuyCoins.SetActive(false);
        PanelBuyEnergy.SetActive(false);
        PanelRank.SetActive(false);
        PanelBag.SetActive(false);
        BackgroundMusic.Instance.ChangeMusic(1);
    }
    public void DangXuatAccount()
    {
        btnDangXuat.interactable = false; // Vô hiệu hóa nút để tránh nhấn nhiều lần
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            NotiButton.Instance.ShowNotice("Vui lòng kết nối mạng để đăng xuất!");
            return;
        }
        PlayerDataSyncManager.SaveGameToPlayFab(playerData, CheckBothSaved);
    }
    void CheckBothSaved()
    {
        btnDangXuat.interactable = true;
        TouchManager.IsPanelOpen = false;
        // Reset các thông tin đăng nhập
        playerData.ResetAll();
        GoogleSignIn.DefaultInstance.SignOut();
        PhotonNetwork.Disconnect();
        PlayFabLogin.ResetInstance();
        PlayFabClientAPI.ForgetAllCredentials();
        // Chuyển scene
        BackgroundMusic.Instance.ChangeMusic(0);
        SceneManager.LoadScene("SignIn");
    }

    public void Update()
    {
        if (!playerData.isGiveCardFree)
        {
            DateTime now = DateTime.Now;
            long secondsElapsed = (now.Ticks - playerData.TimeStartGiveCardFree) / TimeSpan.TicksPerSecond;
            long secondsRemaining = 7200 - secondsElapsed;
            long hours = secondsRemaining / 3600;
            long minutes = (secondsRemaining % 3600) / 60;
            long seconds = secondsRemaining % 60;
            txtTimeGiveCard.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            if (secondsRemaining <= 0)
            {
                playerData.isGiveCardFree = true;
            }
        }
    }

    public void GiveFreeCard()
    {
        if (playerData.isGiveCardFree)
        {
            PlayerController.Instance.AddEnergyVip(1, PlayerController.Instance.GetClickPositionInCanvas());
            playerData.TimeStartGiveCardFree = DateTime.Now.Ticks;
            playerData.isGiveCardFree = false;
        }
    }
    public void OpenListLV()
    {
        BackgroundMusic.Instance.ChangeMusic(2);
        SceneManager.LoadScene("ListLevel");
    }
    public void OpenTask()
    {
        PanelTask.SetActive(true);
        TaskManager.instance.LoadTask();
        TouchManager.IsPanelOpen = true;
    }
    public void CloseTask()
    {
        PanelTask.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void OpenRank()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            NotiButton.Instance.ShowNotice("Vui lòng kết nối mạng để xem bảng xếp hạng!");
            return;
        }
        RankingManager.Instance.LoadBXH();
        PanelRank.SetActive(true);
        TouchManager.IsPanelOpen = true;
    }
    public void CloseRank()
    {
        PanelRank.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void OpenBag()
    {
        PanelBag.SetActive(true);
        BagManager.Instance.LoadBag();
        TouchManager.IsPanelOpen = true;
    }
    public void CloseBag()
    {
        PanelBag.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void ClosePanelThuHoach()
    {
        PanelThuHoach.SetActive(false);
        ItemPanelSelect = Item.Null;
        TouchManager.IsPanelOpen = false;
    }
    public void OpenBuyCoins()
    {
        PanelBuyCoins.SetActive(true);
        TouchManager.IsPanelOpen = true;
    }
    public void CloseBuyCoins()
    {
        PanelBuyCoins.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void OpenBuyEnergy()
    {
        PanelBuyEnergy.SetActive(true);
        TouchManager.IsPanelOpen = true;
    }
    public void CloseBuyEnergy()
    {
        PanelBuyEnergy.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void OpenBuyEnergyVip()
    {
        PanelBuyEnergyVip.SetActive(true);
        TouchManager.IsPanelOpen = true;
    }
    public void CloseBuyEnergyVip()
    {
        PanelBuyEnergyVip.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
}
