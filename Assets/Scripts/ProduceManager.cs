using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProDuceManager : MonoBehaviour
{
    public PlayerData playerData;
    public Transform fillBar; // Gán thanh màu cần tô
    public float maxFillHeight = 1f;
    public int level = 1; // Cấp độ sản xuất
    public Item itemName; // Tên sản phẩm đang sản xuất
    public long currentTimeStart;
    public long timeThuHoach;
    public int quantityProduce; // Số lượng sản phẩm
    public int[] PriceUpLevel; // Giá nâng cấp sản phẩm
    public byte typeSanXuat = 0;
    public SpriteRenderer btnThuHoach;
    public SpriteRenderer[] spritesObj = { }; // Mảng các sprite để hiển thị trạng thái sản xuất
    public Sprite[] status = { }; // Mảng các sprite trạng thái sản xuất
    public Sprite[] spritesItems;
    private long currentTime;
    private int index = -1;

    private void Awake()
    {
        startPosition = fillBar.localPosition;
        index = playerData.dataSanXuat.FindIndex(d => d.itemName == itemName);
        if (index != -1)
        {
            LoadFromData(playerData.dataSanXuat[index]);
        }
    }

    public void LoadFromData(DataSanXuat data)
    {
        level = data.level;
        itemName = data.itemName;
        currentTimeStart = data.currentTimeStart;
        timeThuHoach = data.timeThuHoach;
        quantityProduce = data.quantityProduce;
        typeSanXuat = data.typeSanXuat;
        if(typeSanXuat == 2) OnReadyToHarvest(true);
    }
    public void Save()
    {
        DataSanXuat newData = SaveToData();
        if (index != -1) playerData.dataSanXuat[index] = newData;
        else playerData.dataSanXuat.Add(newData);
        PlayerDataSyncManager.SaveGameToPlayFab(playerData);
    }

    public DataSanXuat SaveToData()
    {
        return new DataSanXuat
        {
            level = level,
            itemName = itemName,
            currentTimeStart = currentTimeStart,
            timeThuHoach = timeThuHoach,
            quantityProduce = quantityProduce,
            typeSanXuat = typeSanXuat
        };
    }
    private long oldCurrentTime = 0L;
    private Vector3 startPosition;

    public void Update()
    {
        if (typeSanXuat == 0 || typeSanXuat == 2) return;
        // Tính thời gian còn lại
        DateTime now = DateTime.Now;
        long secondsElapsed = (now.Ticks - currentTimeStart) / TimeSpan.TicksPerSecond;
        currentTime = Math.Max(0L, timeThuHoach - secondsElapsed);
        // === Tô màu fill bar theo % ===
        float percent = Mathf.Clamp01((float)secondsElapsed / timeThuHoach);
        Vector3 pos = fillBar.localPosition;
        pos.y = Mathf.Lerp(startPosition.y, 0f, percent); // Di chuyển dần về y = 0
        fillBar.localPosition = pos;
        // Nếu đang là vật phẩm được chọn, cập nhật UI nếu cần
        if (LobbyController.Instance != null && LobbyController.Instance.ItemPanelSelect == itemName)
        {
            bool shouldUpdate = false;
            if (currentTime < 3600) shouldUpdate = currentTime != oldCurrentTime;
            else shouldUpdate = oldCurrentTime == 0 || currentTime <= oldCurrentTime - 60;
            if (shouldUpdate)
            {
                oldCurrentTime = currentTime;
                var timeText = LobbyController.Instance.PanelThuHoach.transform.Find("Panel/Time")?.GetComponent<TMPro.TMP_Text>();
                if (timeText != null) timeText.text = "Còn: " + FormatTime(currentTime);
            }
        }
        // Kiểm tra đã chín chưa
        if (secondsElapsed >= timeThuHoach) OnReadyToHarvest(false);
    }

    private void OnReadyToHarvest(bool isLoad)
    {
        typeSanXuat = 2;
        fillBar.localPosition = new Vector3(0f, 0f, 0f);
        foreach (var item in spritesObj) item.sprite = status[1];
        if(!isLoad) Save();
    }

    private void SetSpritesUnripe()
    {
        typeSanXuat = 0;
        fillBar.localPosition = startPosition;
        foreach (var item in spritesObj) item.sprite = status[0];
        Save();
    }

    public void NangCap()
    {
        if (typeSanXuat > 0)
        {
            NotiButton.Instance.ShowNotice("Đang sản xuất không thể nâng cấp!");
            return;
        }
        if (PlayerController.Instance.SubCoins(PriceUpLevel[level - 1]))
        {
            level++; // Tăng cấp độ sản xuất
            timeThuHoach -= 600; // Giảm thời gian thu hoạch 10p
            quantityProduce += 5; // Tăng số lượng sản phẩm thu hoạch
            NotiButton.Instance.ShowNotice("Nâng cấp thành công! Cấp độ hiện tại: " + level);
            Save();
        }
    }
    public void OpenPanel()
    {
        if (typeSanXuat == 2)
        {
            ThuHoach();
            return; // Nếu đang sản xuất VIP thì không mở panel thu hoạch
        }
        LobbyController.Instance.ItemPanelSelect = itemName;
        LobbyController.Instance.PanelThuHoach.transform.Find("Panel/ImageItem").GetComponent<Image>().sprite = spritesItems[(int)itemName];
        string txt = typeSanXuat == 0 ? "Cần " + timeThuHoach / 3600 + " giờ" : "Còn: " + FormatTime(currentTime);
        LobbyController.Instance.PanelThuHoach.transform.Find("Panel/Time").GetComponent<TMP_Text>().text = txt;
        Transform sub1 = LobbyController.Instance.PanelThuHoach.transform.Find("Panel/SubCardNormal");
        sub1.gameObject.SetActive(typeSanXuat == 1 ? false : true);
        Button btn1 = sub1.GetComponent<Button>();
        btn1.onClick.RemoveAllListeners();
        btn1.onClick.AddListener(() => SanXuat(false));
        Transform sub2 = LobbyController.Instance.PanelThuHoach.transform.Find("Panel/SubCardVip");
        Button btn2 = sub2.GetComponent<Button>();
        btn2.onClick.RemoveAllListeners();
        btn2.onClick.AddListener(() => SanXuat(true));
        RectTransform subCardVipRectTransform = sub2.GetComponent<RectTransform>();
        Vector3 localPosition = subCardVipRectTransform.localPosition;
        localPosition.x = typeSanXuat == 1 ? 0f : 133f;
        subCardVipRectTransform.localPosition = localPosition;
        LobbyController.Instance.PanelThuHoach.SetActive(true);
        TouchManager.IsPanelOpen = true;
    }
    public string FormatTime(long seconds)
    {
        long days = seconds / (3600 * 24);
        long hours = (seconds % (3600 * 24)) / 3600;
        long minutes = (seconds % 3600) / 60;
        long secs = seconds % 60;

        if (days > 0)
        {
            return $"{days}d{hours}h";
        }
        else if (hours > 0)
        {
            return $"{hours}h{minutes}p";
        }
        else if (minutes > 0)
        {
            return $"{minutes}p{secs}s";
        }
        else
        {
            return $"{secs}s";
        }
    }
    public void ClosePanel()
    {
        LobbyController.Instance.PanelThuHoach.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void SanXuat(bool isVip)
    {
        if (isVip)
        {
            if (!PlayerController.Instance.SubEnergyVIP(1)) return;
            OnReadyToHarvest(false);
        }
        else
        {
            if (!PlayerController.Instance.SubEnergy(1)) return;
            DateTime now = DateTime.Now;
            currentTimeStart = now.Ticks; // Lưu thời gian bắt đầu sản xuất
            typeSanXuat = 1;
            Save();
        }
        LobbyController.Instance.PanelThuHoach.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public void ThuHoach()
    {
        if (!PlayerController.Instance.AddItemBag(itemName, quantityProduce)) return;
        SetSpritesUnripe();
    }
}
