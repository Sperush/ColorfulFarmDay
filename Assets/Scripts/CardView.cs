using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class CardView : MonoBehaviour
{
    public Outline outline;
    public CardData cardData;
    public Image UpImage;
    public Image backImage;
    public Button button;
    public bool isHandCard;
    public List<CardView> cardsBlockedByMe = new List<CardView>();
    public int blockedCount = 0;
    [Header("Special Setup")]
    public bool isSpecial;
    public int type;
    public Sprite[] Sprite_Special;
    [HideInInspector]
    public bool isSelected;
    public Vector2 oldLocation;
    private Coroutine shakeRoutine;
    public Vector3 OldRotation;

    void Start()
    {
        if(cardData == null)
        {
            Debug.LogError("CardData is not assigned!");
            return;
        }
        if(UpImage == null || backImage == null || button == null)
        {
            Debug.LogError("UI components are not assigned!");
            return;
        }
        SetUpCard();
        if (!GameManager.Instance.isPK)
        {
            if (!isHandCard)
            {
                oldLocation = GetComponent<RectTransform>().anchoredPosition;
            }
            else
            {
                oldLocation = GameManager.Instance.oldHandCard.anchoredPosition;
            }
            OldRotation = transform.localEulerAngles;
        }
        button.onClick.AddListener(OnClick);
        GameManager.Instance.ListCardView.Add(this);
        if (GameManager.Instance.isPK)
        {
            PKController.ListCardView.Add(this);
            if (PKController.ListCardView.Count == 20)
            {
                PKController.Instance.SetUpCard();
            }
        }
    }
    void OnDestroy()
    {
        PKController.ListCardView.Clear();
    }

    public void SetUpCard()
    {
        if(isSpecial)
            UpImage.sprite = Sprite_Special[type];
        else if (cardData.color == CardColor.Red)
            UpImage.sprite = Resources.Load<Sprite>("Sprites/Red/" + cardData.number);
        else if (cardData.color == CardColor.Orange)
            UpImage.sprite = Resources.Load<Sprite>("Sprites/Orange/" + cardData.number);
        else if (cardData.color == CardColor.Yellow)
            UpImage.sprite = Resources.Load<Sprite>("Sprites/Yellow/" + cardData.number);
        else if (cardData.color == CardColor.Green)
            UpImage.sprite = Resources.Load<Sprite>("Sprites/Green/" + cardData.number);
        else if (cardData.color == CardColor.Blue)
            UpImage.sprite = Resources.Load<Sprite>("Sprites/Blue/" + cardData.number);
        else if (cardData.color == CardColor.Purple)
            UpImage.sprite = Resources.Load<Sprite>("Sprites/Purple/" + cardData.number);
    }

    public void ShowFace()
    {
        backImage.gameObject.SetActive(false);
        cardData.isFaceUp = true;
        button.interactable = true;
    }

    void OnClick()
    {
        if (GameManager.Instance.isPK)
        {
            if (transform.parent != PKController.Instance.MyHandArea.transform) return;
            isSelected = !isSelected;
            PKController.Instance.OnCardSelected(this);
        }
        else
        {
            if (isHandCard && transform.parent == GameManager.Instance.HandArea.transform)
            {
                GameManager.Instance.OnDrawCard(this);
            }
            else
            {
                GameManager.Instance.OnCardSelected(this);
            }
        }
    }
    public void Shake()
    {
        if (shakeRoutine != null)
        {
            // Đang rung -> dừng trước khi rung mới
            StopCoroutine(shakeRoutine);
            // Reset về đúng góc ban đầu
            transform.localEulerAngles = Vector3.zero;
        }
        shakeRoutine = StartCoroutine(ShakeRotationRoutine());
    }
    private IEnumerator ShakeRotationRoutine()
    {
        Vector3 originalEuler = transform.localEulerAngles;

        float duration = 0.4f;
        float elapsed = 0f;
        float magnitude = 15f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float angle = Mathf.Sin(elapsed * 50f) * magnitude * (1f - elapsed / duration);

            transform.localEulerAngles = new Vector3(
                originalEuler.x,
                originalEuler.y,
                originalEuler.z + angle
            );

            yield return null;
        }

        // Reset về đúng góc ban đầu
        transform.localEulerAngles = originalEuler;

        shakeRoutine = null; // Mark coroutine đã xong
    }
    public void OnTaken()
    {
        // Ẩn thẻ
        if (cardData.isWild) return;
        if (!isHandCard)
        {
            GameManager.Instance.count_card--;
        } else if (GameManager.Instance.HandCards.Count == 5)
        {
            GameManager.Instance.starCount--;
            GameManager.Instance.ShowStarRating(true);
        }
        // Mở các thẻ bị che
        foreach (var c in cardsBlockedByMe)
        {
            c.blockedCount--;
            if (c.blockedCount == 0)
                c.ShowFace();
        }
    }
}
