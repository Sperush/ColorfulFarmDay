using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WildCardPickerUI : MonoBehaviour
{
    public static WildCardPickerUI Instance;

    [Header("UI")]
    public GameObject panel;
    public List<Button> colorButtons;
    public List<Button> numberButtons;

    [Header("References")]
    private CardView currentWildCard;

    private CardColor selectedColor = CardColor.Red;
    private int selectedNumber = 0;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(CardView card)
    {
        currentWildCard = card;
        panel.SetActive(true);

        // Reset màu các nút màu
        for (int i = 0; i < colorButtons.Count; i++)
        {
            Color c = colorButtons[i].image.color;
            c.a = 1f; // Mặc định tất cả đều hiện rõ
            colorButtons[i].image.color = c;
        }

        // Reset màu các nút số
        for (int i = 0; i < numberButtons.Count; i++)
        {
            numberButtons[i].image.color = Color.white; // Hoặc màu mặc định bạn thích
        }

        // Reset giá trị mặc định
        selectedColor = CardColor.Wild;
        selectedNumber = -1;
    }


    public void OnSelectColor(int colorIndex)
    {
        for(int i = 0; i < colorButtons.Count; i++)
        {
            Color color = colorButtons[i].image.color;
            if (i != colorIndex)
            {
                color.a = 0.35f;
                colorButtons[i].image.color = color;
            } else
            {
                color.a = 1f;
                colorButtons[i].image.color = color;
            }
        }
        selectedColor = (CardColor)colorIndex;
        Debug.Log("Chọn màu: " + selectedColor);
    }

    public void OnSelectNumber(int num)
    {
        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (i != num)
            {
                numberButtons[i].image.color = new Color(0.35f, 0.35f, 0.35f);
            }
            else
            {
                numberButtons[i].image.color = Color.white;
            }
        }
        selectedNumber = num;
        Debug.Log("Chọn số: " + selectedNumber);
    }

    public void OnConfirm()
    {
        if(selectedColor == CardColor.Wild || selectedNumber < 0)
        {
            Debug.LogError("Vui lòng chọn màu và số hợp lệ trước khi xác nhận!");
            return;
        }
        // Gán dữ liệu cho Wild Card
        currentWildCard.cardData.color = selectedColor;
        currentWildCard.cardData.number = selectedNumber;
        currentWildCard.cardData.isWild = false;
        currentWildCard.SetUpCard();
        GameManager.Instance.cardData = currentWildCard.cardData;
        panel.SetActive(false);

        Debug.Log("Xác nhận Wild Card: " + selectedColor + ", " + selectedNumber);
    }
}
