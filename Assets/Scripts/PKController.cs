using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PKController : MonoBehaviour
{
    public GameObject iconChieu;
    public TMP_Text txtChieu;
    public GameObject PanelCard;
    public GameObject[] cardLast;
    public GameObject player;
    public GameObject bot;
    public CardData lastPlayedCard = new CardData();
    public List<CardData> lastPlayedCombo = new List<CardData>();
    public GameObject btnAccept;
    public GameObject btnReject;
    [Header("Scene")]
    public GameObject MyHandArea;
    public GameObject BotHandArea;
    public List<CardView> MyHandCards = new List<CardView>();
    public List<CardView> BotHandCards = new List<CardView>();
    private bool isEndMyHand = false;
    private bool isEndBotHand = false;
    private bool isStart = false;
    [Header("Data")]
    public List<CardView> currentChain = new List<CardView>();
    public bool isMyRound;
    public bool isPkUP;

    public static PKController Instance;
    public static List<CardView> ListCardView = new List<CardView>();
    [HideInInspector]
    private bool isDoneCard1;
    private bool isDoneCard2;
    // Start is called before the first frame update
    void Start()
    {
        isMyRound = Random.Range(0, 2) == 1;
        isPkUP = Random.Range(0,2) == 0;
        txtChieu.text = isPkUP ? "Chiều tăng" : "Chiều giảm";
        btnAccept.SetActive(isMyRound);
        btnReject.SetActive(isMyRound);
        iconChieu.transform.rotation = Quaternion.Euler(0, 0, isPkUP ? 90 : -90);
        Instance = this;
    }
    bool IsCardBetter(CardData prev, CardData next)
    {
        if (prev.isWild) return true;
        if (next.isWild) return true;

        if (isPkUP)
        {
            // Tăng dần
            if (next.number > prev.number)
                return true;

            if (next.number == prev.number && next.color > prev.color)
                return true;
        }
        else
        {
            // Giảm dần
            if (next.number < prev.number)
                return true;

            if (next.number == prev.number && next.color < prev.color)
                return true;
        }

        return false;
    }

    private bool IsComboStronger(List<CardData> prev, List<CardData> next)
    {
        if (prev.Count != next.Count)
            return false;

        // Đảm bảo các lá trong next là một chuỗi hợp lệ
        for (int i = 0; i < next.Count - 1; i++)
        {
            if (!GameManager.Instance.IsValidMove(next[i], next[i + 1], true, true))
                return false;
        }

        // So sánh lá đầu tiên
        return IsCardBetter(prev[0], next[0]);
    }


    private List<List<CardView>> FindAllCombos(List<CardView> hand, CardData lastCardToMatch = null)
    {
        var allCombos = new List<List<CardView>>();

        if (hand.Count < 2)
            return allCombos;

        var sorted = new List<CardView>(hand);
        sorted.Sort(isPkUP ? CompareCardAsc : CompareCardDesc);

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (sorted[i].isSpecial) continue;

            var tempCombo = new List<CardView> { sorted[i] };

            if (lastCardToMatch != null && !GameManager.Instance.IsValidMove(lastCardToMatch, sorted[i].cardData, true, true))
                continue;

            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (sorted[j].isSpecial) break;

                if (GameManager.Instance.IsValidMove(tempCombo.Last().cardData, sorted[j].cardData, true, true))
                {
                    tempCombo.Add(sorted[j]);

                    if (tempCombo.Count >= 2)
                        allCombos.Add(new List<CardView>(tempCombo));
                }
                else
                {
                    break;
                }
            }
        }

        return allCombos;
    }
    public CardView FindBestSingleCard(CardData lastPlayed, List<CardView> hand, bool isPkUP)
    {
        CardView bestSingle = null;
        int bestDistance = int.MaxValue;

        foreach (var card in hand)
        {
            if (card.isSpecial)
                continue;

            if (GameManager.Instance.IsValidMove(lastPlayed, card.cardData, true))
            {
                int numberDiff = Mathf.Abs(card.cardData.number - lastPlayed.number);
                int colorDiff = Mathf.Abs((int)card.cardData.color - (int)lastPlayed.color);
                int totalDistance = numberDiff + colorDiff;

                if (bestSingle == null || totalDistance < bestDistance)
                {
                    bestSingle = card;
                    bestDistance = totalDistance;
                }
                else if (totalDistance == bestDistance)
                {
                    // Ưu tiên số nhỏ hơn khi tăng dần, lớn hơn khi giảm dần
                    if (isPkUP)
                    {
                        if (card.cardData.number < bestSingle.cardData.number)
                            bestSingle = card;
                        else if (card.cardData.number == bestSingle.cardData.number &&
                                 card.cardData.color < bestSingle.cardData.color)
                            bestSingle = card;
                    }
                    else
                    {
                        if (card.cardData.number > bestSingle.cardData.number)
                            bestSingle = card;
                        else if (card.cardData.number == bestSingle.cardData.number &&
                                 card.cardData.color > bestSingle.cardData.color)
                            bestSingle = card;
                    }
                }
            }
        }

        return bestSingle;
    }
    public CardView FindExtremeCard(List<CardView> hand, bool isPkUP)
    {
        CardView candidate = null;

        foreach (var card in hand)
        {
            if (card.isSpecial)
                continue;

            if (candidate == null)
            {
                candidate = card;
                continue;
            }

            if (isPkUP)
            {
                // Tìm nhỏ nhất
                if (card.cardData.number < candidate.cardData.number)
                {
                    candidate = card;
                }
                else if (card.cardData.number == candidate.cardData.number &&
                         card.cardData.color < candidate.cardData.color)
                {
                    candidate = card;
                }
            }
            else
            {
                // Tìm lớn nhất
                if (card.cardData.number > candidate.cardData.number)
                {
                    candidate = card;
                }
                else if (card.cardData.number == candidate.cardData.number &&
                         card.cardData.color < candidate.cardData.color)
                {
                    candidate = card;
                }
            }
        }

        return candidate;
    }

    private bool isSuyNghi;
    private IEnumerator BotPlayDelayed()
    {
        Debug.Log("Bot đang suy nghĩ...");
        yield return new WaitForSeconds(Random.Range(1.5f, 5f)); // Delay 1.5 giây
        BotPlay();
    }
    public void BotPlay()
    {
        Debug.Log("Bot đang chọn bài...");
        bool isNearEmpty = BotHandCards.Count <= 3;

        // 1) Nếu bàn có combo
        if (lastPlayedCombo != null && lastPlayedCombo.Count > 1)
        {
            int comboLength = lastPlayedCombo.Count;

            var candidateCombos = FindAllCombos(BotHandCards)
                .Where(c => c.Count == comboLength)
                .ToList();

            foreach (var combo in candidateCombos)
            {
                if (IsComboStronger(lastPlayedCombo, combo.Select(c => c.cardData).ToList()))
                {
                    Debug.Log($"Bot chặn combo {combo.Count} lá.");
                    PlayCombo(combo, false);
                    return;
                }
            }

            // Không tìm được combo chặn -> thử thẻ đặc biệt
            CardView specialCard = BotHandCards.FirstOrDefault(c => c.isSpecial);
            if (specialCard != null)
            {
                Debug.Log("Bot đánh thẻ đặc biệt để chặn combo.");
                PlayCard(specialCard, false);
                return;
            }

            Debug.Log("Bot không có combo hoặc thẻ đặc biệt để chặn combo, bỏ lượt.");
            EndTurn();
            lastPlayedCard.number = -1;
            lastPlayedCombo.Clear();
            return;
        }
        // 2) Nếu bàn có thẻ đơn
        if (lastPlayedCard.number >= 0)
        {
            CardView bestSingle = FindBestSingleCard(lastPlayedCard, BotHandCards, isPkUP);
            if (bestSingle != null)
            {
                Debug.Log($"Bot đánh lá hợp lệ: Số {bestSingle.cardData.number}, Màu {bestSingle.cardData.color}");
                PlayCard(bestSingle, false);
                return;
            }
            // TH đặc biệt: 9 Tím / 0 Đỏ
            if ((isPkUP && lastPlayedCard.number == 9 && lastPlayedCard.color == CardColor.Purple) || (!isPkUP && lastPlayedCard.number == 0 && lastPlayedCard.color == CardColor.Red))
            {
                var shield = BotHandCards.FirstOrDefault(c => c.isSpecial && c.type == 2);
                if (shield != null)
                {
                    Debug.Log("Bot dùng khiên vàng để chặn thẻ đặc biệt.");
                    PlayCard(shield, false);
                }
                else
                {
                    Debug.Log("Không có khiên vàng → Bỏ lượt.");
                    EndTurn();
                }
                lastPlayedCard.number = -1;
                lastPlayedCombo.Clear();
                return;
            }
            // TH bình thường → duyệt thẻ đặc biệt
            var specials = BotHandCards.Where(c => c.isSpecial).OrderBy(c => c.type).ToList();
            if (specials.Count > 0)
            {
                var first = specials[0];
                if (first.type == 2)
                {
                    // Check nếu toàn bộ là khiên vàng
                    bool onlyShields = BotHandCards.All(c => c.isSpecial && c.type == 2);
                    if (onlyShields)
                    {
                        Debug.Log("Chỉ còn khiên vàng → Đánh thẻ khiên.");
                        PlayCard(first, false);
                    }
                    else
                    {
                        Debug.Log("Không chỉ còn khiên vàng → Bỏ lượt.");
                        EndTurn();
                    }
                }
                else
                {
                    Debug.Log("Đánh thẻ đặc biệt type 0 hoặc 1.");
                    PlayCard(first, false);
                }
                lastPlayedCard.number = -1;
                lastPlayedCombo.Clear();
                return;
            }
            Debug.Log("Không có thẻ đặc biệt → Bỏ lượt.");
            EndTurn();
            lastPlayedCard.number = -1;
            lastPlayedCombo.Clear();
        }

        // 3) Nếu bàn KHÔNG có thẻ
        CardView extremeCard = FindExtremeCard(BotHandCards, isPkUP);
        if (extremeCard != null)
        {
            Debug.Log($"Bot đánh lá {(isPkUP ? "nhỏ nhất" : "lớn nhất")}: Số {extremeCard.cardData.number}, Màu {extremeCard.cardData.color}");
            PlayCard(extremeCard, false);
            return;
        }
        var allCombos = FindAllCombos(BotHandCards);
        if (allCombos.Any())
        {
            var smallestCombo = allCombos.OrderBy(c => c.Count).First();
            Debug.Log($"Bot đánh combo nhỏ nhất ({smallestCombo.Count} lá).");
            PlayCombo(smallestCombo, false);
            return;
        }

        CardView specialCardStart = BotHandCards.FirstOrDefault(c => c.isSpecial);
        if (specialCardStart != null)
        {
            Debug.Log("Bot đánh thẻ đặc biệt khởi đầu.");
            PlayCard(specialCardStart, false);
            return;
        }

        Debug.Log("Bot không còn bài để đánh, bỏ lượt.");
        EndTurn();
        lastPlayedCard.number = -1;
        lastPlayedCombo.Clear();
    }
    public void SetUpCard()
    {
        MyHandCards.Clear();
        BotHandCards.Clear();
        int totalCards = ListCardView.Count;
        int specialCardCount = Random.Range(2, 6); // đảm bảo ít nhất 2 thẻ đặc biệt để chia đều
        int normalCardCount = totalCards - specialCardCount;
        List<CardView> specialCards = new List<CardView>();
        List<CardView> normalCards = new List<CardView>();
        var allCards = new List<CardView>(ListCardView);
        foreach (var c in allCards)
        {
            c.isSpecial = false;
            c.type = -1;
        }
        // Chọn ngẫu nhiên thẻ đặc biệt
        for (int i = 0; i < specialCardCount; i++)
        {
            CardView c = allCards[i];
            c.isSpecial = true;
            c.type = Random.Range(0, 3);
            specialCards.Add(c);
        }
        // Các thẻ còn lại là thẻ thường
        for (int i = specialCardCount; i < totalCards; i++)
        {
            CardView c = allCards[i];
            c.cardData.number = Random.Range(0, 9);
            c.cardData.color = GetColor(Random.Range(0, 5));
            normalCards.Add(c);
        }
        List<CardView> allShuffled = new List<CardView>();
        allShuffled.AddRange(specialCards);
        allShuffled.AddRange(normalCards);
        Shuffle(allShuffled);
        // Chia làm 2 bộ
        int specialsInDeck1 = 0;
        int specialsInDeck2 = 0;
        foreach (var card in allShuffled)
        {
            if (MyHandCards.Count < totalCards / 2)
            {
                MyHandCards.Add(card);
                if (card.isSpecial)
                    specialsInDeck1++;
            }
            else
            {
                BotHandCards.Add(card);
                if (card.isSpecial)
                    specialsInDeck2++;
            }
            card.SetUpCard();
        }
        // Đảm bảo mỗi bộ có ít nhất 1 và tối đa 3 thẻ đặc biệt
        if (specialsInDeck1 == 0 || specialsInDeck1 > 3 || specialsInDeck2 == 0 || specialsInDeck2 > 3)
        {
            SetUpCard();
            return;
        }

        // Sequence tổng
        Sequence seqMyHand = DOTween.Sequence();
        Sequence seqBotHand = DOTween.Sequence();
        var rect2 = cardLast[0].GetComponent<RectTransform>();
        // MyHandCards
        foreach (var card in MyHandCards)
        {
            var rect = card.GetComponent<RectTransform>();
            seqMyHand.Append(AnimateCardFly(rect, false, 0.1f, rect2.anchoredPosition));
            seqMyHand.AppendInterval(0.05f);
        }
        // BotHandCards
        foreach (var card in BotHandCards)
        {
            var rect = card.GetComponent<RectTransform>();
            seqBotHand.Append(AnimateCardFly(rect, true, 0.1f, rect2.anchoredPosition));
            seqBotHand.AppendInterval(0.05f );
        }
        // Kết thúc sequence
        // Sau khi hoàn tất bay vào giữa
        seqMyHand.OnComplete(() =>
        {
            isDoneCard1 = true;
        });
        seqBotHand.OnComplete(() =>
        {
            isDoneCard2 = true;
        });
    }
    public Tween AnimateCardFly(RectTransform rect, bool isTOP, float duration = 0.2f, Vector2? targetPos = null)
    {
        // Nếu không có targetPos => dùng anchoredPosition hiện tại
        Vector2 finalTargetPos = targetPos ?? rect.anchoredPosition;
        // Tính vị trí bắt đầu
        Vector2 startPos = finalTargetPos + new Vector2(0, isTOP ? 1000 : -1000);
        // Gán vị trí bắt đầu
        rect.anchoredPosition = startPos;
        // Scale bắt đầu
        rect.localScale = Vector3.one * 0.4f;
        // Tạo sequence
        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(finalTargetPos, duration).SetEase(Ease.OutBack));
        seq.Join(rect.DOScale(1.182166f, duration).SetEase(Ease.OutBack));
        return seq;
    }
    public Tween AnimateCardFly2(RectTransform rect, Vector2 targetPos, float duration = 0.2f)
    {
        // Tạo sequence
        Sequence seq = DOTween.Sequence();
        seq.Join(rect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutBack));
        return seq;
    }
    public void PutToMiddle(RectTransform rect, int count)
    {
        count--;
        Sequence seqMoveToHand = DOTween.Sequence();
        var rect2 = cardLast[0].GetComponent<RectTransform>();
        seqMoveToHand.Append(AnimateCardFly2(rect, rect2.anchoredPosition, 0.5f));
        seqMoveToHand.AppendInterval(1f);
        seqMoveToHand.OnComplete(() => {
            if (count == 0)
            {
                EndTurn();
                if(rect.GetComponent<CardView>().type == 0) rect.gameObject.SetActive(false);
            }
        });
    }
    void Update()
    {
        if(isDoneCard1 && isDoneCard2){
            isDoneCard1 = false;
            isDoneCard2 = false;
            // Sequence tổng
            Sequence seqMoveToHand = DOTween.Sequence();
            Sequence seqMoveToHand2 = DOTween.Sequence();
            var rect2 = cardLast[1].GetComponent<RectTransform>();
            var rect3 = cardLast[2].GetComponent<RectTransform>();
            // MyHandCards
            foreach (var card in MyHandCards)
            {
                var rect = card.GetComponent<RectTransform>();
                seqMoveToHand.Append(AnimateCardFly2(rect, rect2.anchoredPosition, 0.1f));
                seqMoveToHand.AppendInterval(0.05f);
            }
            // BotHandCards
            foreach (var card in BotHandCards)
            {
                var rect = card.GetComponent<RectTransform>();
                seqMoveToHand2.Append(AnimateCardFly2(rect, rect3.anchoredPosition, 0.1f));
                seqMoveToHand2.AppendInterval(0.05f);
            }
            seqMoveToHand.OnComplete(() => { isEndMyHand = true; });
            seqMoveToHand2.OnComplete(() => { isEndBotHand = true; });
        }
        if (isEndMyHand && isEndBotHand)
        {
            MyHandCards.ForEach(card => card.transform.SetParent(MyHandArea.transform));
            BotHandCards.ForEach(card => card.transform.SetParent(BotHandArea.transform));
            MyHandArea.GetComponent<GridLayoutGroup>().enabled = true; // Bật lại GridLayoutGroup sau khi hoàn thành
            BotHandArea.GetComponent<GridLayoutGroup>().enabled = true; // Bật lại GridLayoutGroup sau khi hoàn thành
            Debug.Log("Tất cả thẻ đã xếp xong!");
            foreach (var c in MyHandCards)
            {
                c.GetComponent<RectTransform>().localScale = Vector3.one * 1.182166f;
                c.backImage.gameObject.SetActive(false);
            }
            CountdownTimer.Instance.countdownTime = 20;
            StartCoroutine(CountdownTimer.Instance.StartCountdown());
            isEndMyHand = false;
            isEndBotHand = false;
            isStart = true;
        }
        if (isStart)
        {
            if (!isSuyNghi)
            {
                isSuyNghi = true;
                if (!isMyRound)
                {
                    StartCoroutine(MoveUp(player.GetComponent<RectTransform>(), -1000, 0.5f)); //ẩn player, bot hiện
                    StartCoroutine(BotPlayDelayed());
                }
                else
                {
                    StartCoroutine(MoveUp(bot.GetComponent<RectTransform>(), 1000, 0.5f)); //ẩn bot, hiện player
                    MarkCombos(MyHandCards);
                }
            }
        }
    }
    public void RejectRound()
    {
        btnAccept.SetActive(false);
        btnReject.SetActive(false);
        EndTurn();
        lastPlayedCard.number = -1;
        lastPlayedCombo.Clear();
    }
    public void EndTurn()
    {
        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        isMyRound = !isMyRound;
        if (BotHandCards.Count == 0 || MyHandCards.Count == 0)
        {
            if (BotHandCards.Count == 0) GameManager.Instance.EndGame();
            else GameManager.Instance.WinGame();
            yield break;
        }

        if (!isUseSpecial)
        {
            int moveAmount = isMyRound ? 1000 : -1000;

            // Chạy song song cả 2 MoveUp và chờ chúng hoàn tất
            yield return StartCoroutine(MoveUp(player.GetComponent<RectTransform>(), moveAmount, 0.5f));
            yield return StartCoroutine(MoveUp(bot.GetComponent<RectTransform>(), moveAmount, 0.5f));
        }

        isUseSpecial = false;

        CountdownTimer.Instance.countdownText.color = Color.white;
        // Reset đồng hồ đếm ngược
        if (CountdownTimer.Instance.currentTime < 1)
        {
            CountdownTimer.Instance.countdownTime = 20;
            StartCoroutine(CountdownTimer.Instance.StartCountdown());
        }
        else
        {
            CountdownTimer.Instance.currentTime = 20;
        }

        if (isMyRound)
        {
            btnAccept.SetActive(true);
            btnReject.SetActive(true);
            MarkCombos(MyHandCards);
        }
        else
        {
            yield return StartCoroutine(BotPlayDelayed()); // nếu BotPlay có thời gian delay
        }
    }

    public bool OnCardSpecial(int type)
    {
        switch (type)
        {
            case 0: //Thẻ chuyển đổi chiều đánh(tăng/giảm)
                isPkUP = !isPkUP;
                txtChieu.text = isPkUP ? "Chiều tăng" : "Chiều giảm";
                iconChieu.transform.rotation = Quaternion.Euler(0, 0, isPkUP ? 90 : -90);
                NotiButton.Instance.ShowNotice("Đổi chiều đánh " + (isPkUP ? "giảm thành tăng!" : "tăng thành giảm!"),5f);
                break;
            case 1: //Thẻ loại bỏ(loại bỏ thẻ đối thủ và đến lượt mình đánh)
                if(isMyRound)
                {
                    NotiButton.Instance.ShowNotice("Bạn đã ngắt bài đối thủ!", 5f);
                }
                else
                {
                    NotiButton.Instance.ShowNotice("Đối thủ đã ngắt bài của bạn!", 5f);
                }
                lastPlayedCard.number = -1;
                lastPlayedCombo.Clear();
                break;
            case 2: //Thẻ khiên vàng
                int number = lastPlayedCard.number;
                CardColor color = lastPlayedCard.color;
                if (!(isPkUP ? (number == 9 && color == CardColor.Purple) : (number == 0 && color == CardColor.Red)) && (isMyRound ? MyHandCards : BotHandCards).Any(c => !c.isSpecial || c.type != 2))
                {
                    if (isMyRound) NotiButton.Instance.ShowNotice("Chỉ có thể chặn thẻ " + (isPkUP ? "9 Tím!" : "0 đỏ") + " hoặc bạn chỉ còn Thẻ khiên vàng!", 5f);
                    return false;
                }
                lastPlayedCard.number = -1;
                lastPlayedCombo.Clear();
                break;
            default:
                return false;
        }
        isUseSpecial = true;
        isMyRound = !isMyRound;
        return true;
    }
    IEnumerator MoveUp(RectTransform rect, float distance, float duration)
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, distance);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public CardColor GetColor(int number)
    {
        switch (number)
        {
            case 0: return CardColor.Red;
            case 1: return CardColor.Orange;
            case 2: return CardColor.Yellow;
            case 3: return CardColor.Green;
            case 4: return CardColor.Blue;
            case 5: return CardColor.Purple;
            default: return CardColor.Red;
        }
    }
    private bool isUseSpecial;
    public void AcceptSelect()
    {
        // Lấy danh sách các thẻ đang chọn
        if (currentChain.Count == 0)
        {
            NotiButton.Instance.ShowNotice("Chưa chọn bài!");
            return;
        }
        // Nếu chỉ chọn 1 lá
        if (currentChain.Count == 1)
        {
            var selected = currentChain[0];
            if (!selected.isSpecial && lastPlayedCard.number >= 0 && !GameManager.Instance.IsValidMove(lastPlayedCard, selected.cardData, true))
            {
                NotiButton.Instance.ShowNotice("Phải đánh lá "+(isPkUP?"lớn hơn!":"nhỏ hơn!"));
                return;
            }
            btnReject.SetActive(false);
            btnAccept.SetActive(false);
            PlayCard(selected, true);
        }
        else
        {
            if(currentChain.Exists(cardViews => cardViews.isSpecial))
            {
                NotiButton.Instance.ShowNotice("Không được đánh thẻ đặc biệt cùng với thẻ thường!");
                return;
            } else if (!CheckCombo(currentChain, true))
            {
                NotiButton.Instance.ShowNotice("Combo không hợp lệ!");
                return;
            }
            btnReject.SetActive(false);
            btnAccept.SetActive(false);
            PlayCombo(currentChain, true);
        }
        currentChain.Clear();
    }
    public void PlayCard(CardView card, bool isPlayer)
    {
        if (!card.isSpecial)
        {
            lastPlayedCard = card.cardData;
            lastPlayedCombo.Clear();
        }
        else if (card.isSpecial && !OnCardSpecial(card.type))
        {
            if (isPlayer)
            {
                btnReject.SetActive(true);
                btnAccept.SetActive(true);
            }
            else
            {
                Debug.Log("Bot không chặn được, bỏ lượt.");
                EndTurn();
            }
            return;
        }
        card.backImage.gameObject.SetActive(false);
        if (isPlayer) MyHandCards.Remove(card);
        else BotHandCards.Remove(card);
        card.transform.SetParent(PanelCard.transform);
        card.GetComponent<Outline>().enabled = false;
        Vector3 worldPos = card.transform.position;
        RectTransform transform = card.GetComponent<RectTransform>();
        transform.anchorMin = new Vector2(0.5f, 0.5f);
        transform.anchorMax = new Vector2(0.5f, 0.5f);
        transform.pivot = new Vector2(0.5f, 0.5f);
        card.transform.position = worldPos;
        PutToMiddle(card.GetComponent<RectTransform>(), 1);
        card.isSelected = false;
    }
    public void PlayCombo(List<CardView> combo, bool isPlayer)
    {
        Debug.Log($"Đánh combo {combo.Count} lá số {combo[0].cardData.number}");
        lastPlayedCard = combo[0].cardData;
        lastPlayedCombo = combo.Select(c => c.cardData).ToList();
        foreach (var c in combo)
        {
            c.backImage.gameObject.SetActive(false);
            if (isPlayer) MyHandCards.Remove(c);
            else BotHandCards.Remove(c);
            c.transform.SetParent(PanelCard.transform);
            c.GetComponent<Outline>().enabled = false;
            Vector3 worldPos = c.transform.position;
            RectTransform transform = c.GetComponent<RectTransform>();
            transform.anchorMin = new Vector2(0.5f, 0.5f);
            transform.anchorMax = new Vector2(0.5f, 0.5f);
            transform.pivot = new Vector2(0.5f, 0.5f);
            c.transform.position = worldPos;
            int cout = c == combo[combo.Count - 1] ? 1:0;
            PutToMiddle(c.GetComponent<RectTransform>(), cout);
            c.isSelected = false;
        }
        int amount = (int)Mathf.Pow(2, combo.Count);
        if (isPlayer) PlayerController.Instance.AddCoins(amount);
    }
    void AddToChain(CardView c)
    {
        currentChain.Add(c);
    }
    public void OnCardSelected(CardView selected)
    {
        if (!isStart) return;
        if (!selected.isSelected)
        {
            if (currentChain.Contains(selected)) currentChain.Remove(selected);
            selected.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 20);
        } else
        {
            selected.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 20);
            AddToChain(selected);
        }
    }
    public List<CardView> FindComboThatContinueChain(List<CardView> hand, CardData lastPlayed)
    {
        List<CardView> bestCombo = null;

        for (int i = 0; i < hand.Count; i++)
        {
            List<CardView> chain = new List<CardView>();
            chain.Add(hand[i]);

            for (int j = 0; j < hand.Count; j++)
            {
                if (i == j) continue;

                var last = chain[chain.Count - 1];
                var next = hand[j];

                if (GameManager.Instance.IsValidMove(last.cardData, next.cardData, true, true))
                {
                    chain.Add(next);
                }
            }

            // Nếu combo >= 2 lá và có thể nối với lá cuối cùng trên bàn
            if (chain.Count >= 2 && GameManager.Instance.IsValidMove(lastPlayed, chain[0].cardData, true))
            {
                if (bestCombo == null || chain.Count > bestCombo.Count)
                {
                    bestCombo = new List<CardView>(chain);
                }
            }
        }

        return bestCombo;
    }
    public bool CheckCombo(List<CardView> c, bool checkFullCombo)
    {
        if (c == null || c.Count < 2)
            return false;
        // Tạo copy để sort mà không làm thay đổi list gốc
        List<CardView> cards = new List<CardView>(c);
        cards.Sort(isPkUP ? CompareCardAsc : CompareCardDesc);
        bool anyValid = false;
        for (int i = 0; i < cards.Count - 1; i++)
        {
            bool valid = IsComboValid(
                cards[i].cardData,
                cards[i + 1].cardData,
                isPkUP // combo chiều tăng hoặc giảm
            );
            if (checkFullCombo)
            {
                if (!valid)
                {
                    // Nếu cần toàn bộ cặp phải hợp lệ, gặp cặp sai => Fail
                    return false;
                }
            }
            else
            {
                if (valid)
                {
                    // Nếu chỉ cần có 1 cặp đúng
                    anyValid = true;
                    break;
                }
            }
        }
        return checkFullCombo ? true : anyValid;
    }
    private int CompareCardAsc(CardView a, CardView b) //tăng dần
    {
        int numberCompare = a.cardData.number.CompareTo(b.cardData.number);
        if (numberCompare != 0)
            return numberCompare;
        return a.cardData.color.CompareTo(b.cardData.color);
    }
    private int CompareCardDesc(CardView a, CardView b) //giảm dần
    {
        int numberCompare = b.cardData.number.CompareTo(a.cardData.number);
        if (numberCompare != 0)
            return numberCompare;
        return b.cardData.color.CompareTo(a.cardData.color);
    }
    public void MarkCombos(List<CardView> cards)
    {
        if (cards.Count < 2) return;

        // Tắt outline trước
        foreach (var c in cards)
            if (c.outline != null) c.outline.enabled = false;

        var sortedCards = new List<CardView>(cards);
        sortedCards.Sort(isPkUP ? CompareCardAsc : CompareCardDesc);

        int comboIndex = 0;
        Color[] comboColors = new Color[]
        {
        Color.red, Color.green, Color.blue, Color.yellow, Color.cyan
        };

        List<CardView> currentCombo = new List<CardView>();

        for (int i = 0; i < sortedCards.Count; i++)
        {
            var current = sortedCards[i];

            if (current.isSpecial)
            {
                if (currentCombo.Count >= 2)
                {
                    ApplyComboOutline(currentCombo, comboColors[comboIndex % comboColors.Length]);
                    comboIndex++;
                }
                currentCombo.Clear();
                continue;
            }

            if (currentCombo.Count == 0)
            {
                currentCombo.Add(current);
                continue;
            }

            var last = currentCombo[currentCombo.Count - 1];
            if (IsComboValid(last.cardData, current.cardData, isPkUP))
            {
                currentCombo.Add(current);
            }
            else
            {
                if (currentCombo.Count >= 2)
                {
                    ApplyComboOutline(currentCombo, comboColors[comboIndex % comboColors.Length]);
                    comboIndex++;
                }

                currentCombo.Clear();
                currentCombo.Add(current);
            }
        }

        if (currentCombo.Count >= 2)
        {
            ApplyComboOutline(currentCombo, comboColors[comboIndex % comboColors.Length]);
        }
    }
    private bool IsComboValid(CardData a, CardData b, bool isAscending)
    {
        int colorDelta = (int)b.color - (int)a.color;
        int numberDelta = b.number - a.number;

        if (!isAscending)
        {
            colorDelta *= -1;
            numberDelta *= -1;
        }

        // Cùng màu
        if (a.color == b.color)
        {
            return numberDelta == 1;
        }

        // Cùng số
        if (a.number == b.number)
        {
            return colorDelta == 1;
        }

        // Khác màu và khác số
        if (numberDelta == 1 && colorDelta == 1)
        {
            return true;
        }

        return false;
    }

    // Helper function để đánh dấu combo
    private void ApplyComboOutline(List<CardView> combo, Color color)
    {
        foreach (var c in combo)
        {
            if (c.outline != null)
            {
                c.outline.effectColor = color;
                c.outline.enabled = true;
            }
        }
    }
}
