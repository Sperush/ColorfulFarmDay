using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveWrapper
{
    public int coins;
    public int energy;
    public int energyVIP;
    public int index_level;
    public int BagSize;
    public List<DataLevel> dataLevel;
    public List<DataBag> dataBag;
    public List<DataSanXuat> dataSanXuat;

    public List<TaskData> tasks;
    public List<MilestoneData> milestones;
    public float energyValue;
    public string lastDailyResetDate;
    public long TimeGiveCardFree;
    public bool isGiveCardFree;
    
    public SaveWrapper(PlayerData pd)
    {
        coins = pd.coins;
        energy = pd.energy;
        energyVIP = pd.energyVIP;
        index_level = pd.index_level;
        BagSize = pd.BagSize;
        dataLevel = pd.dataLevel;
        dataBag = pd.dataBag;
        dataSanXuat = pd.dataSanXuat;
        tasks = pd.tasks;
        milestones = pd.milestones;
        energyValue = pd.energyValue;
        lastDailyResetDate = pd.lastDailyResetDate;
        TimeGiveCardFree = pd.TimeStartGiveCardFree;
        isGiveCardFree = pd.isGiveCardFree;

    }

    public void ApplyTo(PlayerData pd)
    {
        pd.coins = coins;
        pd.energy = energy;
        pd.energyVIP = energyVIP;
        pd.index_level = index_level;
        pd.BagSize = BagSize;
        pd.dataLevel = dataLevel;
        pd.dataBag = dataBag;
        pd.dataSanXuat = dataSanXuat;
        pd.TimeStartGiveCardFree = TimeGiveCardFree;
        pd.isGiveCardFree = isGiveCardFree;

        TaskDataStore.tasks = tasks;
        TaskDataStore.milestones = milestones;
        TaskDataStore.energyValue = energyValue;
        TaskDataStore.lastDailyResetDate = lastDailyResetDate;
    }
}

public class PlayerDataSyncManager : MonoBehaviour
{
    public static void SaveGameToPlayFab(PlayerData playerData, Action onComplete = null, int? newStarCount = null, long? newTimeSumFinish = null)
    {
        if (SceneManager.GetActiveScene().name == "SignIn") return;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            playerData.typeNotSave = true;
            return;
        }
        int completed = 0;
        int total = 0;

        void CheckDone()
        {
            completed++;
            if (completed >= total)
            {
                Debug.Log("✅ SaveAllToPlayFab hoàn tất.");
                onComplete?.Invoke();
            }
        }

        // 1️⃣ Lưu toàn bộ dữ liệu game (GameSave)
        total++;
        SaveWrapper wrapper = new SaveWrapper(playerData);
        string json = JsonUtility.ToJson(wrapper);
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "GameSave", json } }
        }, _ => {
            playerData.typeNotSave = false;
            CheckDone();
        }, error => {
            playerData.typeNotSave = true;
            Debug.LogError("❌ Lỗi lưu GameSave: " + error.GenerateErrorReport());
            CheckDone();
        });

        // 2️⃣ Nếu có StarCount hoặc TimeSumFinish thì lưu luôn
        if (newStarCount.HasValue || newTimeSumFinish.HasValue)
        {
            PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), statResult =>
            {
                int oldStar = statResult.Statistics.FirstOrDefault(s => s.StatisticName == "StarCount")?.Value ?? 0;

                PlayFabClientAPI.GetUserData(new GetUserDataRequest(), dataResult =>
                {
                    long oldTime = 0;
                    if (dataResult.Data != null && dataResult.Data.TryGetValue("timeSumFinish", out var val))
                        long.TryParse(val.Value, out oldTime);

                    // StarCount
                    if (newStarCount.HasValue && newStarCount.Value != oldStar)
                    {
                        total++;
                        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
                        {
                            Statistics = new List<StatisticUpdate>
                            {
                                new StatisticUpdate { StatisticName = "StarCount", Value = newStarCount.Value }
                            }
                        },
                        _ => CheckDone(),
                        error => { 
                            Debug.LogError(error.GenerateErrorReport());
                            playerData.typeNotSave = true;
                            CheckDone();
                        });
                    }

                    // TimeSumFinish
                    if (newTimeSumFinish.HasValue && newTimeSumFinish.Value != oldTime)
                    {
                        total++;
                        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
                        {
                            Data = new Dictionary<string, string> { { "timeSumFinish", newTimeSumFinish.Value.ToString() } },
                            Permission = UserDataPermission.Public
                        },
                        _ => CheckDone(),
                        error => {
                            Debug.LogError(error.GenerateErrorReport());
                            playerData.typeNotSave = true;
                            CheckDone(); });
                    }

                    if (total == 1) CheckDone();

                }, error => {
                    Debug.LogError(error.GenerateErrorReport());
                    CheckDone();
                });

            }, error => {
                Debug.LogError(error.GenerateErrorReport());
                CheckDone();
            });
        }
    }

    public static void LoadGameFromPlayFab(PlayerData playerData, LoginResult result1, bool isNewAccount)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("GameSave"))
            {
                string json = result.Data["GameSave"].Value;
                SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
                wrapper.ApplyTo(playerData);
                Debug.Log("✅ Đã tải dữ liệu từ PlayFab!");
            }
            else
            {
                Debug.Log("ℹ️ Không có dữ liệu để tải.");
                playerData.index_level = 1;
                playerData.BagSize = 20;
                playerData.dataLevel = new List<DataLevel>();
                playerData.dataBag = new List<DataBag>();
                playerData.dataSanXuat = new List<DataSanXuat>();
                playerData.tasks = new List<TaskData>();
                playerData.milestones = new List<MilestoneData>();
                playerData.energyValue = 0f;
                playerData.lastDailyResetDate = "";
                playerData.TimeStartGiveCardFree = DateTime.Now.Ticks;
                playerData.isGiveCardFree = false;
            }
            PlayFabLogin.Instance.Login(result1, isNewAccount);
        },
        error =>
        {
            Debug.LogError("❌ Lỗi khi tải dữ liệu: " + error.GenerateErrorReport());
        });
    }
}
