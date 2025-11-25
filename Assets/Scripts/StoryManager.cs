using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class Story
{
    public Sprite Andy;
    public Sprite BaDi;
    public Sprite Background;
    public string text;
    public bool isAndyTalking; // True nếu Andy đang nói, false nếu BaDi đang nói
}

public class StoryManager : MonoBehaviour
{
    public PlayerData playerData;
    public GameObject HoiThoai;
    public GameObject Andy;
    public GameObject BaDi;
    public int index;
    public TextMeshProUGUI storyText;
    public List<Story> stories;
    public Image background;
    void Start()
    {
        Time.timeScale = 1f;
        BaDi.SetActive(false);
        Andy.SetActive(false);
        if (stories.Count > 0)
        {
            if (stories[index].text == "")
            {
                HoiThoai.SetActive(false);
            }
            else
            {
                HoiThoai.SetActive(true);
                storyText.SetText(stories[index].text);
            }
            background.sprite = stories[index].Background;
        }
    }
    public void NextStory()
    {
        index++;
        if(index >= stories.Count)
        {
            SceneManager.LoadScene("Lobby");
            return;
        }
        if (stories[index].text == "")
        {
            HoiThoai.SetActive(false);
        }
        else
        {
            HoiThoai.SetActive(true);
            storyText.SetText(stories[index].text);
        }
        Image img = BaDi.GetComponent<Image>();
        Image img2 = Andy.GetComponent<Image>();
        if (index >= 3)
        {
            BaDi.SetActive(true);
            Andy.SetActive(true);
            Color color = new Color(0.5566038f, 0.5566038f, 0.5566038f, 0.7215686f);
            if (stories[index].isAndyTalking)
            {
                img.color = color;
                img2.color = Color.white; // Andy đang nói, BaDi sẽ mờ
            } else
            {
                img2.color = color;
                img.color = Color.white; // BaDi đang nói, Andy sẽ mờ
            }
            if (stories[index].Andy == null)
            {
                Andy.SetActive(false);
            }
            else
            {
                img2.sprite = stories[index].Andy;
            }
            if (stories[index].BaDi == null){
                BaDi.SetActive(false);
            }
            else
            {
                img.sprite = stories[index].BaDi;
            }
        }
        background.sprite = stories[index].Background;
    }
}
