using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    public float countdownTime = 90f;
    public float currentTime;
    public TMP_Text countdownText;
    public TMP_Text doubleScoreText;

    [HideInInspector]
    public bool isDoubleScore = false;
    public static CountdownTimer Instance;

    void Start()
    {
        Instance = this;
        if (!GameManager.Instance.isPK) doubleScoreText.gameObject.SetActive(false);
    }
    System.Collections.IEnumerator RandomDoubleScore()
    {
        while (currentTime < 900)
        {
            // Chờ 10 giây
            yield return new WaitForSeconds(10f);

            // Random xác suất (ví dụ 30%)
            float rand = Random.value;
            if (rand <= 0.3f)
            {
                ActivateDoubleScore(5f); // Kích hoạt x2 score trong 5 giây
            }
        }
    }
    public void ActivateDoubleScore(float duration)
    {
        if (isDoubleScore) return; // Nếu đang active thì bỏ qua

        StartCoroutine(DoubleScoreRoutine(duration));
    }
    System.Collections.IEnumerator DoubleScoreRoutine(float duration)
    {
        isDoubleScore = true;
        doubleScoreText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        isDoubleScore = false;
        doubleScoreText.gameObject.SetActive(false);
    }
    public System.Collections.IEnumerator StartCountdown()
    {
        currentTime = countdownTime;
        while (currentTime > 0)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            if (minutes == 0 && seconds == 5) countdownText.color = Color.red;
            countdownText.text = $"{minutes:D2}:{seconds:D2}";
            currentTime -= Time.deltaTime;
            yield return null;
        }
        if (PKController.Instance.isMyRound)
        {
            PKController.Instance.btnAccept.SetActive(false);
            PKController.Instance.btnReject.SetActive(false);
        }
        PKController.Instance.EndTurn();
    }
    public System.Collections.IEnumerator StartCountUp()
    {
        StartCoroutine(RandomDoubleScore());
        currentTime = 0;
        bool isSub = false;
        while (currentTime < 900)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            if (!isSub && seconds == 0 && minutes == 3)
            {
                GameManager.Instance.starCount--;
                GameManager.Instance.ShowStarRating(true);
                countdownText.color = Color.red;
                isSub = true;
            }
            countdownText.text = $"{minutes:D2}:{seconds:D2}";
            currentTime += Time.deltaTime;
            yield return null;
        }
        GameManager.Instance.EndGame();
    }
}
