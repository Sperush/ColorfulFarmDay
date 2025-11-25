using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public enum GameSound
{
    ButtonClick,
    ButtonWhoosh,
    ButtonPop,
    CoinFly,
    CoinArrive,
    Victory,
    Lose,
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clips")]
    public AudioClip buttonClickClip;
    public AudioClip coinFlyClip;
    public AudioClip victoryClip;
    public AudioClip loseClip;

    [Header("Audio Source Pool")]
    public AudioSource audioSourcePrefab;
    private List<AudioSource> audioSources = new List<AudioSource>();
    private Dictionary<GameSound, AudioClip> soundMap = new Dictionary<GameSound, AudioClip>();
    [Header("Global Volume & Mute")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    public bool isMuted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tạo pool AudioSource
            for (int i = 0; i < 10; i++)
            {
                AudioSource src = Instantiate(audioSourcePrefab, transform);
                audioSources.Add(src);
            }

            // Map enum -> clip
            soundMap[GameSound.ButtonClick] = buttonClickClip;
            soundMap[GameSound.CoinFly] = coinFlyClip;
            soundMap[GameSound.Victory] = victoryClip;
            soundMap[GameSound.Lose] = loseClip;
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void Play(GameSound sound, float volum=-1f)
    {
        if (isMuted) return;

        if (soundMap.ContainsKey(sound))
        {
            AudioClip clip = soundMap[sound];
            AudioSource src = audioSources.Find(s => !s.isPlaying);
            if (src == null) src = audioSources[0];

            src.volume = volum == -1 ? sfxVolume:volum;
            src.pitch = 1f;
            src.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("AudioManager: Sound not found " + sound);
        }
    }

    /// <summary>
    /// Mute toàn bộ SFX
    /// </summary>
    public void MuteAll()
    {
        isMuted = true;
        SaveSettings();
    }

    /// <summary>
    /// Bật lại âm thanh SFX
    /// </summary>
    public void UnmuteAll()
    {
        isMuted = false;
        SaveSettings();
    }

    /// <summary>
    /// Toggle mute
    /// </summary>
    public void ToggleMute()
    {
        isMuted = !isMuted;
        SaveSettings();
    }

    /// <summary>
    /// Fade âm lượng SFX từ hiện tại về targetVolume trong duration giây
    /// </summary>
    public void FadeVolume(float targetVolume, float duration)
    {
        DOTween.To(() => sfxVolume, v => sfxVolume = v, targetVolume, duration)
            .SetEase(Ease.Linear)
            .OnComplete(SaveSettings);
    }

    /// <summary>
    /// Save âm lượng + mute
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("SFXMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load âm lượng + mute
    /// </summary>
    private void LoadSettings()
    {
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        isMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
    }
}
