using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Unity.VisualScripting;
[System.Serializable]
public enum TypeEvent
{
    Null = -1,
    Daily,
    Weekly,
    Monthly
}
[System.Serializable]
public enum TypeActionTask
{
    Null = -1,
    Complate_Event,
    Play_Level,
    Play_Star_For_Level,
    SubItem
}
[System.Serializable]
public class ItemData
{
    public Sprite icon;
    public Item itemName;
    public int max;
}
[System.Serializable]
public class DataActionTask
{
    public TypeActionTask type; // 0 = hoàn thành event , 1 = Chơi level, 2 = đạt 3 sao ở level, 3 = Tiêu vật Phẩm
    public TypeEvent typeEvent; // Chỉ sử dụng khi type = 0, để xác định event nào
    public int level = -1; // Cấp độ cần thực hiện
    public Item name;
    public int current = 0; // Số lượng hiện tại đã thực hiện
    public int countMax = 0; // Số lượng cần thực hiện
}
[System.Serializable]
public class TaskData
{
    public string description;
    public int coinReward;
    public int energyReward;
    public DataActionTask ActionTask;
    public List<ItemData> itemDatas = new List<ItemData>(); // Danh sách item để hoàn thành nhiệm vụ
    public bool isCompleted;
    public bool isPickUp;
    public Sprite icon;      // icon nhiệm vụ
}
[System.Serializable]
public class MilestoneData
{
    public float threshold;  // Ví dụ: 0.2 = 20%
    public int rewardCoins;
    public int rewardCardEnergy;
    public bool claimed = false;
}
[System.Serializable]
public class DailyTaskState
{
    public List<TaskData> tasks;
    public List<MilestoneData> milestone;
    public float energyValue;
}
public class TaskManager : MonoBehaviour
{
    [Header("Nhiệm vụ")]
    public int countTask = 5; // Số lượng nhiệm vụ mỗi ngày
    public Transform taskParent;
    public GameObject taskPrefab;
    public GameObject itemPrefab;
    public Sprite[] Donebtn;
    [Header("Tích lũy năng lượng")]
    public GameObject panelInfoGift;
    public Slider energySlider;
    public int energyMax = 100;
    public Sprite[] defaultSprite; // Icon mặc định cho nhiệm vụ=
    [Header("Các mốc quà")]
    public List<Button> milestoneButtons; // Gán lần lượt các nút nhận quà=
    public static TaskManager instance;
    private bool isSetup;

    public void Awake()
    {
        if(instance == null) instance = this;
        LobbyController.Instance.PanelTask.SetActive(false);
        SetUp();
    }
    public void SetUp()
    {
        CheckDailyReset();
        // Nếu chưa có dữ liệu, load từ PlayerPrefs
        if (TaskDataStore.tasks == null || TaskDataStore.tasks.Count == 0)
            LoadDailyTaskData();
        UpdateEnergySlider();
    }
    List<MilestoneData> GenerateNewMilestone()
    {
        List<MilestoneData> milestone = new List<MilestoneData>();
        for (int i = 1; i < 6; i++)
        {
            int[] coins = {100, 150};
            MilestoneData milestoneData = new MilestoneData();
            milestoneData.threshold = 20*i;
            milestoneData.rewardCoins = coins[Random.Range(0,2)] * i;
            milestoneData.rewardCardEnergy = Random.Range(1, 3) * i;
            milestone.Add(milestoneData);
        }
        return milestone;
    }

    List<TaskData> GenerateNewTasks()
    {
        List<TaskData> tasks = new List<TaskData>();
        int count = countTask; // Tổng số nhiệm vụ
        int remainingEnergy = energyMax;

        for (int i = 0; i < count; i++)
        {
            TaskData task = new TaskData();

            bool isOrderTask = Random.value < 0.3f; // Ví dụ 30% là giao hàng

            if (isOrderTask)
            {
                // GIAO HÀNG
                task.ActionTask = new DataActionTask
                {
                    type = TypeActionTask.Null,
                    typeEvent = TypeEvent.Null,
                    level = -1,
                    name = Item.Null,
                    current = 0,
                    countMax = 0
                };

                // Random danh sách vật phẩm cần giao
                task.itemDatas = GenerateRandomItems();

                // Mô tả
                task.description = "Thu thập các vật phẩm cần thiết";

                // Icon mặc định
                task.icon = DefaultIcon();
            }
            else
            {
                // NHIỆM VỤ HÀNH ĐỘNG
                TypeActionTask actionType = (TypeActionTask)Random.Range(1 , 4);
                task.ActionTask = new DataActionTask
                {
                    type = actionType,
                    typeEvent = actionType == TypeActionTask.Complate_Event ? TypeEvent.Daily : TypeEvent.Null,
                    level = actionType == TypeActionTask.Play_Level || actionType == TypeActionTask.Play_Star_For_Level ? Random.Range(1, PlayFabLogin.Instance.playerData.index_level+1) : -1,
                    name = actionType == TypeActionTask.SubItem ? RandomItem() : Item.Null,
                    current = 0,
                    countMax = actionType == TypeActionTask.Play_Level || actionType == TypeActionTask.Play_Star_For_Level ? 1 : Random.Range(1, 10)
                };

                // Mô tả
                task.description = GenerateDescription(task.ActionTask);

                // Icon
                if (actionType == TypeActionTask.SubItem)
                    task.icon = getIconItem(task.ActionTask.name);
                else
                    task.icon = DefaultIcon();
            }

            // Phần thưởng coin
            int[] coinsOptions = { 100, 150, 200, 250, 300, 350, 400, 450, 500 };
            task.coinReward = coinsOptions[Random.Range(0, coinsOptions.Length)];

            // Phân phối năng lượng
            if (i == count - 1)
            {
                task.energyReward = remainingEnergy;
            }
            else
            {
                int average = remainingEnergy / (count - i);
                int min = Mathf.Max(1, average - 10);
                int max = Mathf.Min(remainingEnergy - (count - i - 1), average + 10);
                if (min > max) min = max;
                task.energyReward = Random.Range(min, max + 1);
            }
            remainingEnergy -= task.energyReward;

            task.isCompleted = false;

            tasks.Add(task);
        }

        return tasks;
    }

    Item RandomItem()
    {
        Item[] items = { Item.Tao, Item.Ngo, Item.Trung, Item.Lua, Item.BanhMi, Item.Cherry, Item.Kem, Item.Sua, Item.Raspberry, Item.BongNgo, Item.AppleJuice, Item.AppleJam, Item.Pie, Item.Butter };
        return items[Random.Range(0, items.Length-1)];
    }
    string GenerateDescription(DataActionTask action)
    {
        switch (action.type)
        {
            case TypeActionTask.Complate_Event:
                return "Hoàn thành sự kiện";
            case TypeActionTask.Play_Level:
                return $"Chơi win Level {action.level}";
            case TypeActionTask.Play_Star_For_Level:
                return $"Đạt 3 sao Level {action.level}";
            case TypeActionTask.SubItem:
                return $"Bán 0/{action.countMax} {action.name}";
            default:
                return "Thu thập vật phẩm giao hàng.";
        }
    }
    List<ItemData> GenerateRandomItems()
    {
        List<ItemData> items = new List<ItemData>();
        int itemCount = Random.Range(1, 6); // 1–3 loại item cần giao

        for (int i = 0; i < itemCount; i++)
        {
            Item item = RandomItem();
            while(items.Find(data => data.itemName == item) != null)
            {
                item = RandomItem();
            }
            ItemData data = new ItemData
            {
                itemName = item,
                max = Random.Range(2, 6),
                icon = getIconItem(item)
            };
            items.Add(data);
        }
        return items;
    }
    public Sprite getIconItem(Item item) {
        return Resources.Load<Sprite>("Sprites/Map/" + item.ToString());
    }
    Sprite DefaultIcon()
    {
        return defaultSprite[Random.Range(0,defaultSprite.Length-1)];
    }
    void CheckDailyReset()
    {
        string currentDateStr = System.DateTime.UtcNow.ToString("yyyyMMdd");
        if (TaskDataStore.lastDailyResetDate != currentDateStr)
        {
            TaskDataStore.lastDailyResetDate = currentDateStr;
            ResetDailyTasks();
        }
    }

    void LoadDailyTaskData()
    {
        TaskDataStore.tasks = LobbyController.Instance.playerData.tasks;
        TaskDataStore.milestones = LobbyController.Instance.playerData.milestones;
        TaskDataStore.energyValue = LobbyController.Instance.playerData.energyValue;
    }

    void ResetDailyTasks()
    {
        foreach (var task in TaskDataStore.tasks)
            task.isCompleted = false;

        foreach (var m in TaskDataStore.milestones)
            m.claimed = false;

        TaskDataStore.energyValue = 0f;

        // Random nhiệm vụ mỗi ngày
        TaskDataStore.tasks = GenerateNewTasks();
        TaskDataStore.milestones = GenerateNewMilestone();
        SaveDataTask(true);
    }

    public static void SaveDataTask(bool isSaveSever)
    {
        PlayFabLogin.Instance.playerData.tasks = TaskDataStore.tasks;
        PlayFabLogin.Instance.playerData.milestones = TaskDataStore.milestones;
        PlayFabLogin.Instance.playerData.energyValue = TaskDataStore.energyValue;
        PlayFabLogin.Instance.playerData.lastDailyResetDate = TaskDataStore.lastDailyResetDate;
        if(isSaveSever) PlayerDataSyncManager.SaveGameToPlayFab(PlayFabLogin.Instance.playerData);
    }

    public static void DoneActionTask(TypeActionTask type1, int level, Item name = Item.Null, int quantity = 0 , bool isSaveServer = false, TypeEvent typeEvent = TypeEvent.Null)// 0 = hoàn thành event , 1 = Chơi level, 2 = đạt 3 sao ở level, 3 = Tiêu vật Phẩm
    {
        bool isChanged = false;
        switch (type1)
        {
            case TypeActionTask.Complate_Event:
                // Hoàn thành event: Tìm tất cả nhiệm vụ type 0
                var eventTasks = TaskDataStore.tasks.FindAll(task => !task.isCompleted && task.ActionTask.typeEvent == typeEvent);
                foreach (var t in eventTasks)
                {
                    t.ActionTask.current++;
                    if (t.ActionTask.current >= t.ActionTask.countMax)
                    {
                        t.isCompleted = true;
                        t.ActionTask.current = t.ActionTask.countMax;
                    }
                    if (!isChanged) isChanged = true;
                }
                break;
            case TypeActionTask.Play_Level:
            case TypeActionTask.Play_Star_For_Level:
                // Chơi level hoặc Đạt 3 sao
                var levelTasks = TaskDataStore.tasks.FindAll(task => !task.isCompleted && task.ActionTask.type == type1 && task.ActionTask.level == level);
                foreach (var t in levelTasks)
                {
                    t.ActionTask.current++;
                    if (t.ActionTask.current >= t.ActionTask.countMax)
                    {
                        t.isCompleted = true;
                        t.ActionTask.current = t.ActionTask.countMax;
                    }
                    if (!isChanged) isChanged = true;
                }
                break;
            case TypeActionTask.SubItem:
                // Tiêu vật phẩm
                var itemTasks = TaskDataStore.tasks.FindAll(task =>
                    !task.isCompleted &&
                    task.ActionTask.type == type1 &&
                    task.ActionTask.name == name);

                foreach (var t in itemTasks)
                {
                    t.ActionTask.current += quantity;
                    if (t.ActionTask.current >= t.ActionTask.countMax)
                    {
                        t.isCompleted = true;
                        t.ActionTask.current = t.ActionTask.countMax;
                    }
                    if (!isChanged) isChanged = true;
                }
                break;
            default:
                Debug.LogWarning($"Không tìm thấy nhiệm vụ với type {type1} và level {level}");
                return;
        }
        if (isChanged) SaveDataTask(isSaveServer);
    }
    public void LoadTask()
    {
        if (!isSetup)
        {
            PopulateTasks();
            isSetup = true;
        }
        for(int i = 0; i < TaskDataStore.milestones.Count; i++)
        {
            if (TaskDataStore.milestones[i].claimed) milestoneButtons[i].interactable = false;
        }
        int k = 0;
        RectTransform rect = taskParent.GetComponent<RectTransform>();
        Vector2 rct = rect.anchoredPosition;
        rct.y = 0;
        rect.anchoredPosition = rct;

        foreach (Transform child in taskParent)
        {
            TaskItem task = child.gameObject.GetComponent<TaskItem>();
            if (task.isGiaoHang && !task.taskData.isPickUp)
            {
                Transform parent = child.Find("GiaoHang/ItemList");
                int i = 0, done = 0;
                foreach (Transform c in parent)
                {   
                    TMP_Text countText = c.Find("Text").GetComponent<TMP_Text>();
                    int current = PlayerController.Instance.GetCurrentItemBag(task.taskData.itemDatas[i].itemName);
                    if (current >= task.taskData.itemDatas[i].max)
                    {
                        countText.text = $"{task.taskData.itemDatas[i].max}/{task.taskData.itemDatas[i].max}";
                        done++;
                        countText.color = Color.green;
                    }
                    else
                    {
                        countText.color = Color.red;
                        countText.text = $"{current}/{task.taskData.itemDatas[i].max}";
                    }
                    i++;
                }
                task.taskData.isCompleted = done == task.taskData.itemDatas.Count;
            } else if (task.taskData.ActionTask.type == TypeActionTask.SubItem)
            {
                child.Find("ActionTask/NoiDung/Text").GetComponent<TMP_Text>().text = $"Bán {task.taskData.ActionTask.current}/{task.taskData.ActionTask.countMax} {task.taskData.ActionTask.name}";
            }
            Transform ac = child.Find("Accept");
            Image spriteBTN = ac.GetComponent<Image>();
            spriteBTN.sprite = Donebtn[task.taskData.isCompleted ? 1:0];
            if (task.taskData.isPickUp)
            {
                Button btn = ac.GetComponent<Button>();
                btn.interactable = false;
            }
            k++;
        }
        SortTaskToLoad();
    }
    public void PopulateTasks()
    {
        foreach (Transform child in taskParent)
            Destroy(child.gameObject);

        foreach (var task in TaskDataStore.tasks)
        {
            GameObject go = Instantiate(taskPrefab, taskParent);
            TaskItem t = go.GetComponent<TaskItem>();
            t.taskData = task;
            //Check action task
            if (task.ActionTask.type >= 0)
            {
                go.transform.Find("ActionTask").gameObject.SetActive(true);
                go.transform.Find("ActionTask/NoiDung/Text").GetComponent<TMP_Text>().text = task.description;
            }
            else
            {
                t.isGiaoHang = true;
                go.transform.Find("GiaoHang").gameObject.SetActive(true);
                Transform itemParent = go.transform.Find("GiaoHang/ItemList");
                //int done = 0;
                foreach (var item in task.itemDatas)
                {
                    GameObject itemGo = Instantiate(itemPrefab, itemParent);
                    itemGo.transform.SetParent(itemParent);
                    itemGo.GetComponent<Image>().sprite = item.icon;
                    TMP_Text countText = itemGo.transform.Find("Text").GetComponent<TMP_Text>();
                    int current = PlayerController.Instance.GetCurrentItemBag(item.itemName);
                    if (task.isPickUp || current >= item.max)
                    {
                        countText.text = $"{item.max}/{item.max}";
                        //done++;
                        countText.color = Color.green;
                    }
                    else
                    {
                        countText.text = $"{current}/{item.max}";
                        countText.color = Color.red;
                    }
                }
                //task.isCompleted = done == task.itemDatas.Count;
            }

            go.transform.Find("Reward/CoinsReward/Text").GetComponent<TMP_Text>().text = task.coinReward.ToString();
            go.transform.Find("Reward/EnergyReward/Text").GetComponent<TMP_Text>().text = task.energyReward.ToString();
            Image iconImg = go.transform.Find("Icon/Image").GetComponent<Image>();
            iconImg.sprite = task.icon;
            iconImg.color = Color.white;
            //go.transform.Find("Accept/Text").GetComponent<TMP_Text>().text = task.isCompleted ? "Done" : "Go";
            Image spriteBTN = go.transform.Find("Accept").GetComponent<Image>();
            //spriteBTN.sprite = Donebtn[task.isCompleted ? 1:0];
            Button btn = go.transform.Find("Accept").GetComponent<Button>();
            //if (task.isPickUp) btn.interactable = false;
            btn.onClick.AddListener(() =>
            {
                if (task.isCompleted)
                {
                    CompleteTask(task);
                    go.transform.SetAsLastSibling();
                    spriteBTN.sprite = Donebtn[1];
                    btn.interactable = false;
                    t.taskData = task;
                    SaveDataTask(true);
                    LoadTask();
                    return;
                }
                else
                {
                    // Di chuyển đến nhiệm vụ
                    if (task.ActionTask.type >= 0) // 0 = hoàn thành event , 1 = Chơi level, 2 = đạt 3 sao ở level, 3 = Tiêu vật Phẩm
                    {
                        switch(task.ActionTask.type)
                        {
                            case TypeActionTask.Complate_Event:
                                break;
                            case TypeActionTask.Play_Level:
                            case TypeActionTask.Play_Star_For_Level:
                                SceneManager.LoadScene("Level" + task.ActionTask.level);
                                break;
                            case TypeActionTask.SubItem:
                                // Tiêu vật Phẩm
                                if(task.ActionTask.name == Item.Coins)
                                {
                                    NotiButton.Instance.ShowNotice("Chức năng này chưa được hỗ trợ!");
                                }
                                else if (task.ActionTask.name == Item.CardEnergy || task.ActionTask.name == Item.CardEnergyVip)
                                {
                                    NotiButton.Instance.ShowNotice("Chức năng này chưa được hỗ trợ!");
                                }
                                else
                                {
                                    LobbyController.Instance.OpenBag();
                                }
                                break;
                        }
                    }
                    else
                    {
                        foreach (var item in task.itemDatas)
                        {
                            int current = PlayerController.Instance.GetCurrentItemBag(item.itemName);
                            if (current < item.max)
                            {
                                var produce = LobbyController.Instance.listProduceManagers.Find(x => x.itemName == item.itemName);
                                if (produce != null)
                                {
                                    LobbyController.Instance.CloseTask();
                                    if(produce.typeSanXuat != 2) produce.OpenPanel();
                                }
                                break;
                            }
                        }
                    }
                }
            });
        }
    }

    void CompleteTask(TaskData task)
    {
        task.isPickUp = true;
        if(task.ActionTask.type == TypeActionTask.Null)
        {
            foreach(var itemData in task.itemDatas) {
                PlayerController.Instance.SubItemBag(itemData.itemName, itemData.max);
            }
        }
        SortTaskToLoad();
        // Cộng coins
        PlayerController.Instance.AddCoins(task.coinReward, PlayerController.Instance.GetClickPositionInCanvas(), null, 0);
        // Cộng năng lượng
        TaskDataStore.energyValue += task.energyReward;
        UpdateEnergySlider();
    }

    public void SortTaskToLoad()
    {
        // 1. Lưu toàn bộ TaskItem kèm theo index gốc
        List<(Transform transform, TaskItem task, int originalIndex)> taskItems = new List<(Transform, TaskItem, int)>();

        int index = 0;
        foreach (Transform child in taskParent)
        {
            if (child.TryGetComponent<TaskItem>(out var task))
            {
                taskItems.Add((child, task, index));
                index++;
            }
        }

        // 2. Sort theo yêu cầu:
        // Ưu tiên: Hoàn thành lên đầu, chưa hoàn thành giữ nguyên thứ tự, nhận quà xuống cuối
        taskItems.Sort((a, b) =>
        {
            var aData = a.task.taskData;
            var bData = b.task.taskData;

            // Đã nhận quà xuống cuối
            if (aData.isPickUp && !bData.isPickUp) return 1;
            if (!aData.isPickUp && bData.isPickUp) return -1;

            // Hoàn thành lên đầu
            if (aData.isCompleted && !bData.isCompleted) return -1;
            if (!aData.isCompleted && bData.isCompleted) return 1;

            // Còn lại thì giữ nguyên thứ tự ban đầu
            return a.originalIndex.CompareTo(b.originalIndex);
        });

        // 3. Gán lại vị trí hiển thị theo thứ tự mới
        for (int i = 0; i < taskItems.Count; i++)
        {
            taskItems[i].transform.SetSiblingIndex(i);
        }

    }

    public static void DoneOderTasks()
    {
        for (int i = 0; i < TaskDataStore.tasks.Count; i++)
        {
            if (!TaskDataStore.tasks[i].isCompleted && TaskDataStore.tasks[i].ActionTask.type < 0)
            {
                bool isAllItemsEnough = true;
                TaskDataStore.tasks[i].itemDatas.ForEach(item =>
                {
                    if (PlayerController.Instance.GetCurrentItemBag(item.itemName) < item.max) isAllItemsEnough = false;
                });
                // Nếu tất cả item đã đủ
                TaskDataStore.tasks[i].isCompleted = isAllItemsEnough;
            }
        }
    }
    void UpdateEnergySlider()
    {
        energySlider.value = Mathf.Clamp(TaskDataStore.energyValue, 0, energyMax);
    }
    public void ClosePanelInfoGift()
    {
        panelInfoGift.SetActive(false);
        TouchManager.IsPanelOpen = false;
    }
    // Gọi từ UI Button của từng milestone
    public void ClaimMilestone(int index)
    {
        if (index < 0 || index >= TaskDataStore.milestones.Count) return;
        var m = TaskDataStore.milestones[index];

        if (m.claimed) return;
        if (TaskDataStore.energyValue < m.threshold)
        {
            panelInfoGift.SetActive(true);
            TouchManager.IsPanelOpen = true;
            return;
        }
        // Nhận quà
        PlayerController.Instance.AddCoins(m.rewardCoins, PlayerController.Instance.GetClickPositionInCanvas(), null, 0);
        PlayerController.Instance.AddEnergyVip(m.rewardCardEnergy, PlayerController.Instance.GetClickPositionInCanvas());

        m.claimed = true;
        milestoneButtons[index].interactable = false;
        Debug.Log($"Đã nhận quà milestone {m.threshold * 100}%");
    }
}
