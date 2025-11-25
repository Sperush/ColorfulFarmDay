using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using System.Linq;
using System;

[System.Serializable]
public class RankEntryData
{
    public int stt = -1; // Thứ tự trong BXH
    public string playerName;
    public int StarCount;
    public long timeSumFinish;
}

public class RankingManager : MonoBehaviour
{
    public GameObject myRanker;
    [Header("Prefab BXH")]
    public GameObject rankItemPrefab;
    public Transform rankParent;

    [Header("Dữ liệu BXH")]
    public static List<RankEntryData> rankingEntries = new List<RankEntryData>();

    [Header("Icon Hạng")]
    public Sprite rankSprite1;
    public Sprite rankSprite2;
    public Sprite rankSprite3;
    public static RankingManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void PopulateRanking()
    {
        // Xóa danh sách cũ
        foreach (Transform child in rankParent)
            Destroy(child.gameObject);

        // Tạo mới danh sách
        for (int i = 0; i < rankingEntries.Count; i++)
        {
            CreateRankItem(rankingEntries[i], i);
        }
        LoadMyRanker();
    }
    public void GetMyRanker(Action<RankEntryData> onCompleted)
    {
        PlayFabClientAPI.GetLeaderboardAroundPlayer(new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "StarCount",
            MaxResultsCount = 1
        },
        result =>
        {
            var item = result.Leaderboard.FirstOrDefault();
            if (item != null)
            {
                var stt = item.Position + 1;
                var myStar = item.StatValue;
                GetUserTime(item.PlayFabId, (myTime) =>
                {
                    var myRank = new RankEntryData
                    {
                        stt = stt,
                        playerName = PlayFabLogin.Instance.playerData.playerName,
                        StarCount = myStar,
                        timeSumFinish = myTime
                    };
                    onCompleted?.Invoke(myRank); // Gọi callback khi xong
                });
            }
            else
            {
                Debug.LogWarning("Không tìm thấy dữ liệu BXH của bản thân.");
                onCompleted?.Invoke(null); // Gọi callback với null nếu không có
            }
        },
        error =>
        {
            Debug.LogError("Lỗi khi lấy dữ liệu của bản thân: " + error.GenerateErrorReport());
            onCompleted?.Invoke(null);
        });
    }

    public static void GetUserTime(string playFabId, Action<long> onDone)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = playFabId
        },
        result =>
        {
            long time = 0;
            if (result.Data != null && result.Data.TryGetValue("timeSumFinish", out var val))
            {
                long.TryParse(val.Value, out time);
            }
            onDone?.Invoke(time);
        },
        error =>
        {
            Debug.LogWarning("⚠️ Không thể lấy UserData: " + error.GenerateErrorReport());
            onDone?.Invoke(0);
        });
    }
    public void LoadMyRanker()
    {
        GetMyRanker((myRank) =>
        {
            if (myRank != null)
            {
                CreateRankItem(myRank, myRank.stt - 1, true);
            }
        });
    }
    void CreateRankItem(RankEntryData entry, int index, bool isMe = false)
    {
        GameObject go;
        if (isMe) go = myRanker;
        else go = Instantiate(rankItemPrefab, rankParent);
        Image icon = go.transform.Find("Icon").GetComponent<Image>();
        GameObject txtRankObj = go.transform.Find("txtRank").gameObject;
        TMP_Text txtRank = txtRankObj.GetComponent<TMP_Text>();
        txtRank.text = (index + 1).ToString();
        switch (index)
        {
            case 0:
                icon.sprite = rankSprite1;
                icon.gameObject.SetActive(true);
                txtRankObj.SetActive(false);
                break;
            case 1:
                icon.sprite = rankSprite2;
                icon.gameObject.SetActive(true);
                txtRankObj.SetActive(false);
                break;
            case 2:
                icon.sprite = rankSprite3;
                icon.gameObject.SetActive(true);
                txtRankObj.SetActive(false);
                break;
        }
        // Tên người chơi
        TMP_Text nameText = go.transform.Find("Name").GetComponent<TMP_Text>();
        nameText.text = entry.playerName;
        // Số sao
        TMP_Text scoreText = go.transform.Find("Star/TextCountStar").GetComponent<TMP_Text>();
        scoreText.text = $"{entry.StarCount}";

        // Thời gian hoàn thành
        TMP_Text timeText = go.transform.Find("Star/TextSumTime").GetComponent<TMP_Text>();
        timeText.text = FormatTime(entry.timeSumFinish);
    }

    public string FormatTime(long seconds)
    {
        long days = seconds / (3600 * 24);
        long hours = (seconds % (3600 * 24)) / 3600;
        long minutes = (seconds % 3600) / 60;
        long secs = seconds % 60;

        if (days > 0)
            return $"{days}d{hours}h";
        else if (hours > 0)
            return $"{hours}h{minutes}p";
        else if (minutes > 0)
            return $"{minutes}p{secs}s";
        else
            return $"{secs}s";
    }

    public void LoadBXH()
    {
        rankingEntries.Clear();

        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = "StarCount",
            MaxResultsCount = 100
        },
        result =>
        {
            int totalEntries = result.Leaderboard.Count;
            int completedEntries = 0;

            foreach (var item in result.Leaderboard)
            {
                var entry = new RankEntryData
                {
                    playerName = item.DisplayName ?? item.PlayFabId,
                    StarCount = item.StatValue,
                    timeSumFinish = 0 // Mặc định là 0 nếu không có dữ liệu
                };
                rankingEntries.Add(entry);
                GetUserTime(item.PlayFabId, entry, () =>
                {
                    completedEntries++;
                    if (completedEntries >= totalEntries)
                    {
                        // Đủ dữ liệu => sắp xếp rồi hiển thị
                        SortAndShowRanking();
                    }
                });
            }
        },
        error =>
        {
            Debug.LogError("Lỗi khi load BXH: " + error.GenerateErrorReport());
        });
    }

    void GetUserTime(string playFabId, RankEntryData entry, System.Action onComplete)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { "timeSumFinish" }
        },
        result =>
        {
            if (result.Data != null && result.Data.ContainsKey("timeSumFinish"))
            {
                if (long.TryParse(result.Data["timeSumFinish"].Value, out long time))
                {
                    entry.timeSumFinish = time;
                }
            }
            onComplete?.Invoke();
        },
        error =>
        {
            Debug.LogWarning($"Không thể lấy thời gian của {playFabId}: " + error.GenerateErrorReport());
            onComplete?.Invoke(); // vẫn gọi callback để không bị treo
        });
    }

    void SortAndShowRanking()
    {
        rankingEntries.Sort((x, y) =>
        {
            int starCompare = y.StarCount.CompareTo(x.StarCount);
            if (starCompare != 0) return starCompare;

            return y.timeSumFinish.CompareTo(x.timeSumFinish); // time giảm dần
        });

        PopulateRanking();
    }
}