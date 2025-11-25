using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Linq;

[System.Serializable]
public class GameManager : MonoBehaviour
{
    public bool isPK;
    public RectTransform cardLast;
    public RectTransform oldHandCard;
    public static GameManager Instance;
    public GameObject cardPrefab;
    public GameObject PriceUndo;
    public GameObject CanvasArea;
    public GameObject HandArea;
    public GameObject BoardArea;
    public GameObject Deck;
    public GameObject BuyCard;
    public Button UndoBtn;
    public int countUndoFree;
    public int count_card;
    public TextMeshProUGUI txtTimeDown;
    public TextMeshProUGUI txtcountUndo;
    public TextMeshProUGUI txtPriceUndo;
    public TextMeshProUGUI txtPriceBuyCard;
    public Sprite starFull;
    public Sprite starEmpty;
    public TMP_Text textRewardcoins;
    public Image[] stars;
    public int[] RewardCoinsLevels;
    public Slider starSlider; // Set từ Unity
    public Image[] starsBar;
    public Sprite starFull1;
    public Sprite starEmpty1;
    [Header("Scene")]
    public GameObject SceneWin;
    public GameObject SceneLose;
    public List<CardView> currentChain = new List<CardView>();
    public CardData cardData;
    public int starCount = 3;

    public List<CardView> HandCards = new List<CardView>();
    public List<CardView> BoardCards = new List<CardView>();
    public List<CardView> ListCardView = new List<CardView>();

    private bool isEndHand = false;
    private bool isEndBoard = false;
    private bool isStart = false;
    private int priceUndo = 100;
    private int priceBuyCard = 500;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        if (!isPK)
        {
            BuyCard.SetActive(false);
            priceBuyCard = 500 + (PlayerController.Instance.index_level - 1) * 20;
            txtPriceBuyCard.SetText(priceBuyCard.ToString());
            priceUndo = 100 + (PlayerController.Instance.index_level - 1) * 20;
            txtPriceUndo.SetText(priceUndo.ToString());
            SceneWin.SetActive(false);
            SceneLose.SetActive(false);
            cardData = currentChain[0].cardData;
            UpdateUndo();
            HandArea.GetComponent<GridLayoutGroup>().enabled = false;
            // Sequence tổng
            Sequence seqHand = DOTween.Sequence();
            Sequence seqBoard = DOTween.Sequence();

            // HandCards
            foreach (var card in HandCards)
            {
                var rect = card.GetComponent<RectTransform>();
                Vector2 target = rect.anchoredPosition;
                seqHand.Append(AnimateCardFly(rect, false));
                seqHand.AppendInterval(0.05f);
            }

            // BoardCards
            foreach (var card in BoardCards)
            {
                var rect = card.GetComponent<RectTransform>();
                Vector2 target = rect.anchoredPosition;
                seqBoard.Append(AnimateCardFly(rect, true, 0.1f));
                seqBoard.AppendInterval(0.01f);
            }
            // Kết thúc sequence
            seqHand.OnComplete(() => { 
                HandCards.RemoveAt(HandCards.Count - 1); // Loại bỏ thẻ cuối cùng (thẻ đã rút)
                isEndHand = true;
            });
            seqBoard.OnComplete(() => { isEndBoard = true; });
        }
    }
    public void PutToMiddle(RectTransform rect, Vector2 target, CardView last=null)
    {
        Sequence seqMoveToHand = DOTween.Sequence();
        seqMoveToHand.Append(last != null ? AnimateCardFly2(rect, target, 0.5f, last.OldRotation.z):AnimateCardFly2(rect, target, 0.5f));
        seqMoveToHand.OnComplete(() =>
        {
            if (last != null)
            {
                if (last.isHandCard)
                {
                    last.transform.SetParent(HandArea.transform);
                }
                else { last.transform.SetParent(BoardArea.transform); }
            }
        });
    }
    public Tween AnimateCardFly2(RectTransform rect, Vector2 targetPos, float duration = 0.2f, float z1 = 0f)
    {
        // Nếu không có targetPos => dùng anchoredPosition hiện tại
        Vector2 finalTargetPos = targetPos;
        // Tạo sequence
        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(finalTargetPos, duration).SetEase(Ease.OutBack));
        Vector3 currentRot = rect.localEulerAngles;
        if (currentRot.z != z1)
        {
            seq.Join(rect.DOLocalRotate(new Vector3(currentRot.x, currentRot.y, z1), duration).SetEase(Ease.OutBack));
        }
        return seq;
    }

    public Tween AnimateCardFly(RectTransform rect, bool isTOP, float duration = 0.2f)
    {
        // Lưu vị trí đích đang sẵn có
        Vector2 targetPos = rect.anchoredPosition;

        // Tính vị trí bắt đầu
        Vector2 startPos = targetPos + new Vector2(0, isTOP ? 1000 : -1000);

        // Gán vị trí bắt đầu
        rect.anchoredPosition = startPos;

        // Scale bắt đầu
        rect.localScale = Vector3.one * 0.4f;

        // Tạo sequence
        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutBack));
        seq.Join(rect.DOScale(isTOP ? 1.182166f : 1f, duration).SetEase(Ease.OutBack));

        return seq;
    }
    [HideInInspector]
    public int coinsCombo = 0;
    public int EvaluateAndShowStar(int level)
    {
        int coin = RewardCoinsLevels[starCount-1];
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = i < starCount ? starFull : starEmpty;
        }
        textRewardcoins.SetText("+"+coin.ToString());
        // Cập nhật dữ liệu người chơi
        PlayerController.Instance.AddCoins(coin);
        PlayerController.Instance.AddEnergy(1);
        AudioManager.Instance.Play(GameSound.Victory,1f);
        TaskManager.DoneActionTask(TypeActionTask.Play_Level, level);
        if (starCount == 3) TaskManager.DoneActionTask(TypeActionTask.Play_Star_For_Level, level);
        return starCount;
    }
    private int lastShownStars = -1;
    public void ShowStarRating(bool isBar)
    {
        Image[] st = isBar ? starsBar:stars;
        int[] sliderTargets = { 0, 50, 100 };
        float targetValue = sliderTargets[Mathf.Clamp(starCount - 1, 0, 2)];
        DOTween.To(() => starSlider.value, x =>
        {
            starSlider.value = x;
            int starsToShow = (x >= 100f) ? 3 : (x >= 50f ? 2 : 1);
            if (lastShownStars != starsToShow)
            {
                for (int i = 0; i < st.Length; i++)
                {
                    st[i].sprite = i < starCount ? (isBar ? starFull1 : starFull) : (isBar ? starEmpty1 : starEmpty);
                }
                lastShownStars = starsToShow;
            }
        }, targetValue, 0.8f).SetEase(Ease.OutQuad).SetUpdate(true);
    }
    public void OpenWinScene()
    {
        SceneWin.SetActive(true);
        PlayerController.Instance.SaveData(EvaluateAndShowStar(PlayerController.Instance.index_level - 1));
    }
    public void ReplayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void CloseWinScene()
    {
        Time.timeScale = 1f;
        SceneWin.SetActive(false);
    }
    public void WinGame()
    {
        Time.timeScale = 0f;
        PlayerController.Instance.index_level++;
        OpenWinScene();
    }
    public void NextLevel()
    {
        SceneManager.LoadScene("Level" + PlayerController.Instance.index_level);
    }
    public void BackToHome()
    {
        SceneManager.LoadScene("Lobby");
    }
    public void EndGame()
    {
        Time.timeScale = 0f;
        SceneLose.SetActive(true);
    }

    public bool IsValidMove(CardData last, CardData next, bool isPk, bool checkcombo = false)
    {
        // Wild card được đánh mọi lúc
        if (last.isWild && last.number == -1) return true;
        if (next.isWild && next.number == -1) return true;

        if (isPk)
        {
            int numberDiff = next.number - last.number;
            int colorDiff = (int)next.color - (int)last.color;

            if (PKController.Instance.isPkUP)
            {
                if (!checkcombo)
                {
                    // Tăng màu hoặc số (không cần liền kề)
                    if (numberDiff > 0 || colorDiff > 0)
                        return true;
                }

                // Liền kề cả số và màu theo chiều tăng
                bool numberNear = numberDiff == 1 || (last.number == 9 && next.number == 0);
                bool colorNear = colorDiff == 1 || (last.color == CardColor.Purple && next.color == CardColor.Red);

                if (numberNear && colorNear)
                    return true;
            }
            else
            {
                if (!checkcombo)
                {
                    // Giảm màu hoặc số (không cần liền kề)
                    if (numberDiff < 0 || colorDiff < 0)
                        return true;
                }

                // Liền kề cả số và màu theo chiều giảm
                bool numberNear = numberDiff == -1 || (last.number == 0 && next.number == 9);
                bool colorNear = colorDiff == -1 || (last.color == CardColor.Red && next.color == CardColor.Purple);

                if (numberNear && colorNear)
                    return true;
            }

            return false;
        }

        // Không phải PK mode (giữ luật cũ: chỉ cần liền kề)
        bool sameColor = last.color == next.color;
        bool sameNumber = last.number == next.number;
        bool colorAdjacent = IsColorAdjacent(last.color, next.color, false, checkcombo);
        bool numberAdjacent = IsNumberAdjacent(last.number, next.number, false, checkcombo);

        if (sameColor && numberAdjacent) return true;
        if (sameNumber && colorAdjacent) return true;
        if (colorAdjacent && numberAdjacent) return true;

        return false;
    }


    public bool IsNumberAdjacent(int a, int b, bool isPk, bool checkcombo)
    {
        if (a == 0 && b == 9) return true;
        if (a == 9 && b == 0) return true;
        if (!isPk)
        {
            return Mathf.Abs(a - b) == 1;
        }
        else
        {
            return PKController.Instance.isPkUP ? (checkcombo ? (a - b == -1) : (a - b <= -1)) : (checkcombo ? (a - b == 1) : (a - b >= 1));
        }
    }
    private const int ColorCount = 6; // Chỉ 6 màu

    public bool IsColorAdjacent(CardColor a, CardColor b, bool isPk, bool checkcombo)
    {
        // Nếu 1 trong 2 là Wild -> không liền kề
        if (a == CardColor.Wild || b == CardColor.Wild)
            return false;
        int ai = (int)a; //index card
        int bi = (int)b; //card next
        if (!isPk)
        {
            return (Mathf.Abs(ai - bi) == 1) || (Mathf.Abs(ai - bi) == ColorCount - 1);
        }
        else
        {
            return PKController.Instance.isPkUP ? (checkcombo ? (ai - bi == -1) : (ai - bi <= -1)) : (checkcombo ? (ai - bi == 1) : (ai - bi >= 1));
        }
    }

    public void UpdateUndo()
    {
        PriceUndo.SetActive(countUndoFree <= 0);
        txtcountUndo.SetText(countUndoFree.ToString());
    }
    public void Update()
    {
        if (isPK) return;
        if (isEndHand && isEndBoard)
        {
            HandArea.GetComponent<GridLayoutGroup>().enabled = true; // Bật lại GridLayoutGroup sau khi hoàn thành
            Debug.Log("Tất cả thẻ đã xếp xong!");
            foreach (var c in ListCardView)
            {   
                c.backImage.gameObject.SetActive(!c.cardData.isFaceUp);
            }
            cardLast.GetComponent<CardView>().cardData.isFaceUp = false;
            StartCoroutine(CountdownTimer.Instance.StartCountUp());
            isEndHand = false;
            isEndBoard = false;
            isStart = true;
        }
        if (currentChain.Count == 1)
        {
            if (UndoBtn.interactable)
            {
                UndoBtn.interactable = false;
            }
        }
        else if (!UndoBtn.interactable)
        {
            UndoBtn.interactable = true;
        }
    }
    public void OnCardSelected(CardView selected)
    {
        if (!isStart || (!selected.cardData.isFaceUp && !selected.cardData.isWild) || (selected.cardData.isWild && !PlayerController.Instance.SubCoins(300))) return;
        var last = currentChain[currentChain.Count - 1];
        if (IsValidMove(last.cardData, selected.cardData, false))
        {
            // Nếu là Wild chưa định danh
            if (selected.cardData.isWild)
            {
                foreach (var c in currentChain.Skip(1))
                    c.gameObject.SetActive(false);
                currentChain.RemoveRange(1, currentChain.Count - 1);
                WildCardPickerUI.Instance.Show(currentChain[0]);
                return;
            }
            else
            {
                currentChain.Add(selected);
                selected.OnTaken();
                selected.cardData.isFaceUp = false;
                selected.transform.SetParent(CanvasArea.transform);
                PutToMiddle(selected.GetComponent<RectTransform>(), PlayerController.Instance.ConvertAnchorPos(cardLast.anchoredPosition, new Vector2(0.5f, 0f)));
            }
            if (count_card == 0)
            {
                WinGame();
            }
        }
        else
        {
            if (last.cardData != selected.cardData)
            {
                CountdownTimer.Instance.currentTime += 10; // Tăng thời gian nếu chọn sai
                Debug.Log("Không hợp lệ");
            }
            else
            {
                Debug.Log("Đang chọn thẻ này!");
            }
        }
    }
    public void UndoLastMove()
    {
        if (currentChain.Count <= 1) return;
        // Lấy thẻ cuối cùng
        CardView last = currentChain[currentChain.Count - 1];
        // Trừ lượt Undo
        if (countUndoFree > 0)
        {
            countUndoFree--;
            UpdateUndo();
        }
        else if (!PlayerController.Instance.SubCoins(priceUndo))
        {
            return;
        }
        // Loại bỏ khỏi chuỗi hiện tại
        currentChain.Remove(last);
        // Khôi phục lại thẻ đã rút
        if (!last.isHandCard)
        {
            last.ShowFace();
            count_card++;
            last.cardData.isFaceUp = true; // Đặt lại trạng thái mặt thẻ
        }
        else
        {
            HandCards.Add(last);
            if (HandCards.Count == 5)
            {
                starCount++;
                ShowStarRating(true);
            }
            if (BuyCard.activeSelf) BuyCard.SetActive(false);
            last.backImage.gameObject.SetActive(true);
        }
        // Nếu thẻ này đã mở các thẻ khác, ta phải khóa lại
        foreach (var c in last.cardsBlockedByMe)
        {
            c.blockedCount++;
            if (c.blockedCount > 0)
            {
                c.cardData.isFaceUp = false; // Đặt lại trạng thái mặt thẻ
                c.backImage.gameObject.SetActive(true);
            }
        }
        // Khôi phục vị trí và anchor để tránh lỗi hiển thị
        RectTransform rect = last.GetComponent<RectTransform>();
        //rect.anchorMin = new Vector2(0.5f, 0f);
        //rect.anchorMax = new Vector2(0.5f, 0f);
        //rect.pivot = new Vector2(0.5f, 0f);
        // Đặt lại vị trí giữa Deck
        last.transform.position = cardLast.position;
        PutToMiddle(rect, last.oldLocation, last);
    }
    private int countBuy=0;
    public void BuyCardHand()
    {
        if(countBuy >= 3)   
        {
            NotiButton.Instance.ShowNotice("Tối đa mua được 3 lượt!");
            return;
        }
        if (!PlayerController.Instance.SubCoins(priceBuyCard)) return;
        int ran = Random.Range(0, 4);
        for (int i = 0; i < 5; i++)
        {
            CardView newCard = Instantiate(cardPrefab, HandArea.transform).GetComponent<CardView>();
            newCard.isHandCard = true;
            newCard.blockedCount = i == 4 ? 1:2;
            int num = currentChain[currentChain.Count - 1].cardData.number+1;
            newCard.cardData.number = ran == i ? (num > 9 ? 0:num):Random.Range(0, 9);
            newCard.cardData.color = ran == i ? currentChain[currentChain.Count - 1].cardData.color : (CardColor)Random.Range(0, 6);
            newCard.SetUpCard();
            if (HandCards.Count > 0) newCard.cardsBlockedByMe.Add(HandCards[HandCards.Count - 1]);
            HandCards.Add(newCard);
        }
        countBuy++;
        BuyCard.SetActive(false);
        if (HandCards.Count == 5)
        {
            starCount++;
            ShowStarRating(true);
        }
    }
    public void OnDrawCard(CardView selected)
    {
        if (!isStart) return;
        if (selected.blockedCount == 1)
        {
            currentChain.Add(selected);
            selected.OnTaken();
            Vector3 worldPos = selected.transform.position;
            selected.transform.SetParent(CanvasArea.transform, false);
            RectTransform transform = selected.GetComponent<RectTransform>();
            transform.anchorMin = new Vector2(0.5f, 0f);
            transform.anchorMax = new Vector2(0.5f, 0f);
            transform.pivot = new Vector2(0.5f, 0f);
            selected.transform.position = worldPos;
            selected.backImage.gameObject.SetActive(false);
            HandCards.Remove(selected);
            PutToMiddle(transform, cardLast.anchoredPosition);
            FindCardAccept();
            if(HandCards.Count == 0)
            {
                BuyCard.SetActive(true);
            }
        }
    }
    public void FindCardAccept()
    {
        if (currentChain.Count <= 1) return;
        foreach (var c in ListCardView)
        {
            // Chỉ xét các lá đang còn hiện diện
            if (!c.gameObject.activeSelf || c.cardData.isWild || c.isHandCard || !c.cardData.isFaceUp) continue;
            // Nếu có thể nối tiếp selected
            if (IsValidMove(currentChain[currentChain.Count - 2].cardData, c.cardData, false))
            {
                c.Shake();
            }
        }
    }
}
