using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackgroundMusic : MonoBehaviour
{
    public PlayerData playerData;
    public static BackgroundMusic Instance;
    [Header("Audio")]
    public AudioSource audioMusic;
    public AudioClip[] musicClip;
    public Sprite[] MusicSprite;

    void Awake()
    {
        audioMusic.mute = playerData.isMuteMusic;
        if (Instance != null)
        {
            Destroy(gameObject);
        } else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public void ChangeMusic(int typeScene)
    {
        audioMusic.clip = musicClip[typeScene];
        if (!audioMusic.isPlaying)
        {
            audioMusic.Play();
        }
    }
}
