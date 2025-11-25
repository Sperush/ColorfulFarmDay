using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BagManager : MonoBehaviour
{
    public HorizontalLayoutGroup group;
    public TMP_Text txtSize;
    public TMP_Text txtPriceUpgrade;
    public TMP_Text txtPriceSell;
    public Slider selectQuantity;
    public TMP_Text quantityText;
    public Button CongQuantity;
    public Button TruQuantity;
    public Button SellItemBtn;
    public GameObject rowPrefab;
    public GameObject bagPrefab;
    public Transform rowParent;
    public Sprite[] statusBtn;
    private int priceUpgrade;
    private Item itemSelect;
    private Image oldSelect;

    public static BagManager Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        createBag();
    }
    public void Upgrade()
    {
        if (!PlayerController.Instance.SubCoins(priceUpgrade)) return;
        PlayerController.Instance.playerData.BagSize += 10;
        txtSize.SetText("Sức chứa: " + PlayerController.Instance.playerData.BagSize);
        txtPriceUpgrade.SetText((PlayerController.Instance.playerData.BagSize*10).ToString());
        txtPriceUpgrade.color = PlayerController.Instance.coin < priceUpgrade ? Color.red : Color.white;
    }
    public void LoadBag()
    {
        priceUpgrade = PlayerController.Instance.playerData.BagSize * 10;
        txtSize.SetText("Sức chứa: " + PlayerController.Instance.playerData.BagSize);
        txtPriceUpgrade.SetText(priceUpgrade.ToString());
        txtPriceUpgrade.color = PlayerController.Instance.coin < priceUpgrade ? Color.red : Color.white;
        itemSelect = Item.Null;
        selectQuantity.value = selectQuantity.minValue;
        updateSlider();
        selectQuantity.interactable = false;
        CongQuantity.interactable = false;
        TruQuantity.interactable = false;
        SellItemBtn.interactable = false;
        foreach (Transform child in rowParent)
        {
            foreach(Transform c in child)
            {
                Image img = c.gameObject.GetComponent<Image>(); 
                img.sprite = statusBtn[0];
                img.color = new Color(1, 0, 1, 1f);
                ItemBag ib = c.gameObject.GetComponent<ItemBag>();
                TMP_Text txt = c.Find("quantity").GetComponent<TMP_Text>();
                txt.color = Color.white;
                txt.text = "x" + PlayerController.Instance.GetCurrentItemBag(ib.item);
            }
        }
    }
    public void createBag()
    {
        Item currentItem = Item.Trung;
        for (int i = 0; i < 4; i++)
        {
            GameObject obj = Instantiate(rowPrefab, rowParent);
            for (int j = 0; j < 4; j++)
            {
                if (currentItem > Item.Butter) break;
                GameObject itemObj = Instantiate(bagPrefab, obj.transform);
                ItemBag ib = itemObj.GetComponent<ItemBag>();
                ib.item = currentItem;
                Image img2 = itemObj.transform.Find("Icon").GetComponent<Image>();
                img2.sprite = getIconItem(currentItem);
                itemObj.transform.Find("quantity").GetComponent<TMP_Text>().text = "x" + PlayerController.Instance.GetCurrentItemBag(currentItem);
                currentItem++;
            }
        }
    }
    public Sprite getIconItem(Item item)
    {
        return Resources.Load<Sprite>("Sprites/Map/" + item.ToString());
    }
    public void SellItemBag()
    {
        int quantity = (int)selectQuantity.value;
        if (quantity <= 0) return;
        txtPriceSell.SetText("Giá: " + (quantity * 50));
        PlayerController.Instance.SubItemBag(itemSelect, quantity);
        PlayerController.Instance.AddCoins((int)selectQuantity.value * 50, PlayerController.Instance.GetClickPositionInCanvas(), null, 0);
        selectQuantity.maxValue = PlayerController.Instance.GetCurrentItemBag(itemSelect);
        oldSelect.gameObject.transform.Find("quantity").GetComponent<TMP_Text>().text = "x"+(int)selectQuantity.maxValue;
        TaskManager.DoneActionTask(TypeActionTask.SubItem, -1, itemSelect, quantity, true);
        if (LobbyController.Instance.PanelTask.activeSelf) TaskManager.instance.LoadTask();
    }
    public void SelectItem(Item item, Image newSelect, TMP_Text txtnew)
    {
        itemSelect = item;
        if (oldSelect != null && oldSelect != newSelect)
        {
            oldSelect.sprite = statusBtn[0];
            oldSelect.color = new Color(1, 0, 1, 1f);
            oldSelect.gameObject.transform.Find("quantity").GetComponent<TMP_Text>().color = Color.white;
        }
        oldSelect = newSelect;
        oldSelect.sprite = statusBtn[1];
        oldSelect.color = new Color(1, 1, 1, 1f);
        txtnew.color = Color.black;
        int quantity = -1;
        if (item != Item.Null)
        {
            quantity = PlayerController.Instance.GetCurrentItemBag(item);
            selectQuantity.maxValue = quantity;
        } else
        {
            selectQuantity.value = 0;
        }
        SellItemBtn.interactable = TruQuantity.interactable = CongQuantity.interactable = selectQuantity.interactable = quantity > 0;
    }
    public void changeQuantity(bool isUp)
    {
        if ((selectQuantity.value == selectQuantity.minValue && !isUp) || (selectQuantity.value == selectQuantity.maxValue && isUp)) return;
        selectQuantity.value += isUp ? 1 : -1;
        updateSlider();
    }
    public void updateSlider()
    {
        int quantity = (int)selectQuantity.value;
        quantityText.SetText("x"+quantity.ToString());
        txtPriceSell.SetText("Giá: " + (quantity * 50));
    }
}
