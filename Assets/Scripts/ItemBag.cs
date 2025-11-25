using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBag : MonoBehaviour
{
    public Image image;
    public TMP_Text txtQuantity;
    [HideInInspector]
    public Item item;

    public void SelectItem()
    {
        BagManager.Instance.SelectItem(item, image, txtQuantity);
    }
}
