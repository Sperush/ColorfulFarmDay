using UnityEngine;
using System.Collections.Generic;
using static PlayFabLogin;
[System.Serializable]
public enum Item
{
    Null=-1,
    Trung,
    Lua,
    Ngo,
    Tao,
    BanhMi,
    Cherry,
    Kem,
    Sua,
    Raspberry,
    BongNgo,
    AppleJuice,
    AppleJam,
    Pie,
    Butter,
    CardEnergy,
    CardEnergyVip,
    Coins
}
[System.Serializable]
public class DataLevel
{
    public int Level;
    public long Time_Finished;
    public int Star;
}
[System.Serializable]
public class DataBag
{
    public Item itemName;
    public int current;
}
[System.Serializable]
public class DataSanXuat
{
    public int level = 1; // Cấp độ sản xuất
    public Item itemName; // Tên sản phẩm đang sản xuất
    public long currentTimeStart;
    public long timeThuHoach;
    public byte typeSanXuat = 0;
    public int quantityProduce; // Số lượng sản phẩm
}
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    public string playerName;
    public string username;
    public string password;
    public LoginMethod typeLogin;
    public bool typeNotSave;
    public bool isGoogleLoggedIn;
    public int coins;
    public int energy;
    public int energyVIP;
    public int index_level;
    public bool isMuteMusic;
    public int BagSize = 20; // Kích thước túi đồ
    public List<DataLevel> dataLevel;
    public List<DataBag> dataBag;
    public List<DataSanXuat> dataSanXuat; // Danh sách sản xuất
    public List<TaskData> tasks;
    public List<MilestoneData> milestones;
    public float energyValue;
    public string lastDailyResetDate;
    public long TimeStartGiveCardFree;
    public bool isGiveCardFree;
    public string sessionTokenKey;

    public void ResetAll()
    {
        playerName = "";
        username = "";
        password = "";
        typeLogin = LoginMethod.Username;
        typeNotSave = false;
        isGoogleLoggedIn = false;
        coins = 0;
        energy = 0;
        energyVIP = 0;
        index_level = 0;
        isMuteMusic = false;
        BagSize = 0;
        dataLevel = new List<DataLevel>();
        dataBag = new List<DataBag>();
        dataSanXuat = new List<DataSanXuat>();
        tasks = new List<TaskData>();
        milestones = new List<MilestoneData>();
        energyValue = 0;
        lastDailyResetDate = "";
        TimeStartGiveCardFree = 0;
    }
}
