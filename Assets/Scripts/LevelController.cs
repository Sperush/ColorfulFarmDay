using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class LevelController : MonoBehaviour
{
    public PlayerData playerData;
    [HideInInspector]
    public static LevelController Instance;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
        Instance = this;
    }

    public void BackToHub()
    {
        SceneManager.LoadScene("Lobby");
    }
}   
