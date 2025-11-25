using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public Sprite[] states;
    public Image image;
    public Button button;
    public TMP_Text nameLV;
    public PlayerData playerData;
    public int level;
    [Header("Star Level")]
    public Sprite starFull;
    public Sprite starEmpty;
    public Image[] stars;
    void Start()
    {
        nameLV.SetText(level.ToString());
        bool isHasStar = false;
        foreach(var data in playerData.dataLevel)
        {
            if (data.Level == level)
            {
                ShowStarRating(data.Star);
                isHasStar = true;
                button.interactable = true;
            }
        }
        if(!isHasStar) ShowStarRating(0);
        if (playerData.index_level == level) image.sprite = states[1];
        button.onClick.AddListener(OnClick);
        if (playerData.index_level < level)
        {
            nameLV.color = new Color(1f, 1f, 1f, 0.5f); // Mờ chữ nếu chưa mở khóa
            image.color = new Color(0.6320754f, 0.6320754f, 0.6320754f, 0.8666667f); // Màu xám nếu chưa mở khóa
        }
    }
    void OnClick()
    {
        if(playerData.index_level < level)
        {
            NotiButton.Instance.ShowNotice("Chưa mở khóa cấp độ này");
            return;
        } else if (level > 4)
        {
            NotiButton.Instance.ShowNotice("Hiện tại game chỉ có 4 màn");
            return;
        }
        SceneManager.LoadScene("Level" + level);
    }
    public void ShowStarRating(int count)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = i < count ? starFull : starEmpty;
        }
        image.sprite = states[0];
    }
}
