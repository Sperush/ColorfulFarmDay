using UnityEngine;

public enum CardColor
{
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple,
    Wild // Màu đặc biệt cho Wild Card
}

[System.Serializable]
public class CardData
{
    public bool isWild;
    public CardColor color;
    public int number;
    public bool isFaceUp; // true = mặt trên, false = mặt dưới
}
