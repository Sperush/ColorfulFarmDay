using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public int coin;
    public int energy;
    public int energyVip;
    public int index_level;
    public TextMeshProUGUI txtCoins;
    public TextMeshProUGUI txtEnergy;
    public TextMeshProUGUI txtEnergyVip;
    public PlayerData playerData;
    public static PlayerController Instance;
    public Sprite[] MusicSprite;
    public Image Music;
    public Canvas canvas;
    public GameObject PanelTutorial;
    public RectTransform Tutorial;
    [Header("Coin Effect")]
    public GameObject coinIconPrefab;
    public Vector3 startPos;
    public Vector3 targetPos;
    [Header("CardEnergy Effect")]
    public GameObject EnergyIconPrefab;
    public Vector3 startPos2;
    public Vector3 targetPos2;
    [Header("CardEnergyVip Effect")]
    public GameObject EnergyVipIconPrefab;
    public Vector3 startPos3;
    public Vector3 targetPos3;

    public int bagSize
    {
        get { return playerData.BagSize; }
    }

    public List<DataBag> dataBags
    {
        get { return playerData.dataBag; }
    }

    void Awake()
    {
        PanelTutorial.SetActive(false);
        Music.sprite = MusicSprite[playerData.isMuteMusic ? 1 : 0];
        Instance = this;
        coin = playerData.coins;
        energy = playerData.energy;
        if (!SceneManager.GetActiveScene().name.Contains("Level"))
        {
            energyVip = playerData.energyVIP;
            UpdateEnergyVip();
        }
        UpdateCoins();
        UpdateEnergy();
        if (SceneManager.GetActiveScene().name.Contains("Level")) index_level = int.Parse(SceneManager.GetActiveScene().name.Replace("Level", ""));
    }
    public void OpenPanelTutorial()
    {
        PanelTutorial.SetActive(true);
        Tutorial.anchoredPosition = new Vector2(Tutorial.anchoredPosition.x, 0f);
        TouchManager.IsPanelOpen = true;
    }
    public void ClosePanelTutorial()
    {
        PanelTutorial.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    public Vector2 GetClickPositionInCanvas()
    {
        Vector2 screenPos;
        #if UNITY_EDITOR || UNITY_STANDALONE
            screenPos = Input.mousePosition;
        #elif UNITY_ANDROID || UNITY_IOS
            screenPos = Input.GetTouch(0).position;
        #endif
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (screenPos != Vector2.negativeInfinity && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvasRect.GetComponentInParent<Canvas>().worldCamera, out Vector2 localPos))
        {
            return localPos;
        }

        return Vector2.negativeInfinity; // Không có click
    }
    public int getAllQuantityBag()
    {
        int total = 0;
        foreach (var bag in dataBags)
        {
            total += bag.current;
        }
        return total;
    }
    public bool AddItemBag(Item item, int amount)
    {
        if(getAllQuantityBag() + amount > playerData.BagSize)
        {
            NotiButton.Instance.ShowNotice("Kho chứa đã đầy!");
            return false;
        }
        DataBag bag = dataBags.FirstOrDefault(b => b.itemName == item);
        if (bag != null)
        {
            bag.current += amount;
        }
        else
        {
            bag = new DataBag { itemName = item, current = amount};
            dataBags.Add(bag);
        }
        TaskManager.DoneOderTasks();
        GameObject obj = new GameObject("DynamicImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.GetComponent<Image>().sprite = TaskManager.instance.getIconItem(item);
        PlayObjFlyEffect(obj, GetClickPositionInCanvas(), ConvertAnchorPos(new Vector3(486f, 107f, 0f), new Vector2(0f, 0f)), amount);
        return true;
    }
    public void SubItemBag(Item item, int amount)
    {
        DataBag bag = dataBags.FirstOrDefault(b => b.itemName == item);
        if (bag != null)
        {
            bag.current -= amount;
            if (bag.current <= 0)
            {
                dataBags.Remove(bag);
            }
        }
    }
    public bool HasItemBag(Item item, int amount = 1)
    {
        DataBag bag = dataBags.FirstOrDefault(b => b.itemName == item);
        if (bag != null && bag.current >= amount)
        {
            return true;
        }
        return false;
    }
    public int FindIndexItemBag(Item item)
    {
        return dataBags.FindIndex(b => b.itemName == item);
    }
    public int GetCurrentItemBag(Item item)
    {
        DataBag bag = dataBags.FirstOrDefault(b => b.itemName == item);
        if (bag != null)
        {
            return bag.current;
        }
        return 0;
    }
    public void MuteMusic()
    {
        BackgroundMusic.Instance.audioMusic.mute = !BackgroundMusic.Instance.audioMusic.mute;
        playerData.isMuteMusic = BackgroundMusic.Instance.audioMusic.mute;
        Music.sprite = BackgroundMusic.Instance.MusicSprite[BackgroundMusic.Instance.audioMusic.mute ? 1 : 0];
    }
    public bool SubCoins(int a)
    {
        if(coin < a)
        {
            NotiButton.Instance.ShowNotice("Bạn không có đủ tiền!");
            return false;
        }
        coin -= a;
        coin = Mathf.Max(coin, 0);
        playerData.coins = coin;
        UpdateCoins();
        return true;
    }
    public bool SubEnergy(int a)
    {
        if (energy < a)
        {
            NotiButton.Instance.ShowNotice("Bạn không có đủ vé năng lượng thường!");
            return false;
        }
        energy -= a;
        energy = Mathf.Max(energy, 0);
        playerData.energy = energy;
        UpdateEnergy();
        return true;
    }
    public bool SubEnergyVIP(int a)
    {
        if (energyVip < a)
        {
            NotiButton.Instance.ShowNotice("Bạn không có đủ vé năng lượng VIP!");
            return false;
        }
        energyVip -= a;
        energyVip = Mathf.Max(energyVip, 0);
        playerData.energyVIP = energyVip;
        UpdateEnergyVip();
        return true;
    }
    public void UpdateCoins()
    {
        if (coin >= 10_000_000)
        {
            txtCoins.SetText((coin / 1000000f).ToString("F1") + "M");
        }
        else
        {
            txtCoins.SetText(coin.ToString());
        }
    }
    public void AddCoins(int amount, Vector3? start = null, Vector3? target = null, int typeScene = 1) // 0: lobby ; 1: thuong ; 2: pk
    {
        if(typeScene != 0 && GameManager.Instance.isPK && !GameManager.Instance.SceneWin.activeSelf)
        {
            GameManager.Instance.coinsCombo += amount;
            GameManager.Instance.starCount = GameManager.Instance.coinsCombo >= 16 ? 3 : GameManager.Instance.coinsCombo >= 4 ? 2 : 1;
            GameManager.Instance.ShowStarRating(true);
        }
        int oldValue = coin;
        coin += amount;
        coin = Mathf.Min(coin, int.MaxValue);
        playerData.coins = coin;
        AnimateCount(txtCoins, oldValue, coin);
        Vector3 startPosFinal = start.HasValue ? start.Value : startPos;
        Vector3 targetPosFinal = target.HasValue ? target.Value : targetPos;
        Vector2 oldAnchor = (typeScene != 0 && GameManager.Instance.isPK) ? new Vector2(0f, 1f): new Vector2(1f, 1f);
        PlayObjFlyEffect(coinIconPrefab, startPosFinal, ConvertAnchorPos(targetPosFinal, oldAnchor));
    }
    public Tween AnimateCardFly(RectTransform rect, Vector2 targetPos, float duration = 0.2f)
    {
        // Nếu không có targetPos => dùng anchoredPosition hiện tại
        Vector2 finalTargetPos = targetPos;
        // Tạo sequence
        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(finalTargetPos, duration).SetEase(Ease.OutSine));
        // Scale tăng khi gần cuối (ví dụ: 80% → 100% thời gian)
        float scaleStartDelay = duration * 0.2f;
        float scaleDuration = duration * 0.8f;

        seq.AppendInterval(scaleStartDelay);
        seq.Append(rect.DOScale(1.1f, scaleDuration).SetEase(Ease.OutQuad));
        return seq;
    }
    public static Vector2 GetAnchoredPositionInCanvas(Canvas canvas, Vector3 screenPosition)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, canvas.worldCamera, out Vector2 localPos);
        return localPos;
    }
    public Vector2 ConvertAnchorPos(Vector3 anchoredPos, Vector2 oldAnchor, Vector2? newAnchor = null)
    {
        newAnchor = newAnchor.HasValue ? newAnchor:new Vector2(0.5f, 0.5f);
        Vector2 parentSize = (canvas.transform as RectTransform).rect.size;
        Vector2 deltaAnchor = oldAnchor - newAnchor.Value;
        Vector2 offset = new Vector2(deltaAnchor.x * parentSize.x, deltaAnchor.y * parentSize.y);
        return anchoredPos + (Vector3)offset;
    }

    public void PlayObjFlyEffect(GameObject obj,Vector3 startPos, Vector3 targetPos, int count=0)
    {
        int leg = count == 0 ? 8: count;
        for (int i = 0; i < leg; i++)
        {
            GameObject coin = Instantiate(obj, startPos, Quaternion.identity, canvas.transform);
            RectTransform rect = coin.GetComponent<RectTransform>();
            rect.localScale *= 0.8f;
            rect.anchoredPosition = new Vector2(startPos.x, startPos.y);
            Sequence seqMoveToHand = DOTween.Sequence();
            seqMoveToHand.SetUpdate(true);
            // Play âm thanh khi coin bắt đầu bay
            seqMoveToHand.AppendCallback(() =>
            {
                AudioManager.Instance.Play(GameSound.CoinFly,0.1f);
            });
            float delay = i * 0.05f + UnityEngine.Random.Range(0f, 0.03f);
            seqMoveToHand.AppendInterval(delay);
            seqMoveToHand.Append(AnimateCardFly(rect, targetPos, 0.5f));
            seqMoveToHand.OnComplete(() =>
            {
                Destroy(coin);
            });
        }
    }

    public void AddEnergy(int amount, Vector3? start = null, Vector3? target = null)
    {
        int oldValue = energy;
        energy += amount;
        energy = Mathf.Min(energy, int.MaxValue);
        playerData.energy = energy;
        AnimateCount(txtEnergy, oldValue, energy);
        Vector3 startPosFinal = start.HasValue ? start.Value : startPos2;
        Vector3 targetPosFinal = target.HasValue ? target.Value : targetPos2;
        PlayObjFlyEffect(EnergyIconPrefab, startPosFinal, ConvertAnchorPos(targetPosFinal, new Vector2(1f, 1f)), amount);
    }
    public void AddEnergyVip(int amount, Vector3? start = null, Vector3? target = null)
    {
        int oldValue = energyVip;
        energyVip += amount;
        energyVip = Mathf.Min(energyVip, int.MaxValue);
        playerData.energyVIP = energyVip;
        AnimateCount(txtEnergyVip, oldValue, energyVip);
        Vector3 startPosFinal = start.HasValue ? start.Value : startPos3;
        Vector3 targetPosFinal = target.HasValue ? target.Value : targetPos3;
        PlayObjFlyEffect(EnergyVipIconPrefab, startPosFinal, ConvertAnchorPos(targetPosFinal, new Vector2(1f, 1f)), amount);
    }
    private void AnimateCount(TextMeshProUGUI textTarget, int from, int to)
    {
        DOTween.To(() => from, x =>
        {
            if (x >= 10_000_000)
            {
                textTarget.SetText((x / 1_000_000f).ToString("F1") + "M");
            }
            else
            {
                textTarget.SetText(x.ToString());
            }
        }, to, 0.6f).SetEase(Ease.OutQuad).SetUpdate(true);
    }
    public void UpdateEnergy()
    {
        if (energy >= 10_000_000)
        {
            txtEnergy.SetText((energy / 1000000f).ToString("F1") + "M");
        }
        else
        {
            txtEnergy.SetText(energy.ToString());
        }
    }
    public void UpdateEnergyVip()
    {
        if (energyVip >= 10_000_000)
        {
            txtEnergyVip.SetText((energyVip / 1000000f).ToString("F1") + "M");
        }
        else
        {
            txtEnergyVip.SetText(energyVip.ToString());
        }
    }
    public void SaveData(int star)
    {
        playerData.coins = coin;
        playerData.energy = energy;
        playerData.energyVIP = energyVip;
        if (playerData.index_level < index_level) playerData.index_level = index_level;
        int levelIndex = index_level - 1;
        long finishTime = (long)CountdownTimer.Instance.currentTime;
        var levelData = playerData.dataLevel.Find(data => data.Level == levelIndex);
        if (levelData != null)
        {
            if (star > levelData.Star || (star == levelData.Star && finishTime < levelData.Time_Finished))
            {
                levelData.Star = star;
                levelData.Time_Finished = finishTime;
            }
        }
        else
        {
            playerData.dataLevel.Add(new DataLevel { Level = levelIndex, Star = star, Time_Finished = finishTime });
        }
        int totalStars = 0;
        long totalTime = 0;
        foreach (var data in playerData.dataLevel)
        {
            totalStars += data.Star;
            totalTime += data.Time_Finished;
        }
        PlayerDataSyncManager.SaveGameToPlayFab(playerData, null, totalStars, totalTime);
    }
}
