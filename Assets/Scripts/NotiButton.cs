using TMPro;
using Unity.Collections;
using UnityEngine;

public class NotiButton : MonoBehaviour
{
    public GameObject noticePanel;
    public TextMeshProUGUI noticeText;
    public static NotiButton Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Đã có một bản tồn tại rồi
        }
    }

    public void ShowNotice(string message, float duration = 3f)
    {
        noticeText.text = message;
        noticePanel.SetActive(true);
        StopAllCoroutines(); // Dừng nếu có Coroutine cũ đang chạy
        StartCoroutine(HideAfterSeconds(duration));
    }
    private System.Collections.IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        noticePanel.SetActive(false);
    }
}