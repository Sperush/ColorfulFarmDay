using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Google;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

public class PlayFabLogin : MonoBehaviour
{
    public PlayerData playerData;
    public Image Music;
    [Header("UI Button")]
    public Button registerButton;
    public Button loginButton;
    public Button btnLoginGoogle;
    public Button btnSwitchLogin;
    public Button btnSwitchRegister;
    public Button btnCreateName;
    [Header("GameObject")]
    public GameObject panelLogin;
    public GameObject panelRegister;
    public GameObject panelCreateName;
    [Header("TMP_InputField")]
    public TMP_InputField[] playerName;
    public TMP_InputField[] username;
    public TMP_InputField[] password;
    public static PlayFabLogin Instance;
    public enum LoginMethod { Google, Username }
    private bool isLoggedIn = false;
    private bool isLoggingIn = false;
    private GoogleSignInConfiguration configuration;
    private LoginMethod selectedLoginMethod;
    private float checkTimer = -1;
    private const float checkInterval = 5f;
    private string sessionTokenKey = "SessionToken";
    public float minInterval = 30f; // thời gian tối thiểu giữa 2 lần update
    public float maxInterval = 60f; // thời gian tối đa giữa 2 lần update
    // ⚠️ Nhớ thay bằng WebClientId từ Google Console (Web OAuth 2.0 client ID)
    public string webClientId = "994907455913-bevq37sordv5ng27suolnr2jmscur5il.apps.googleusercontent.com";

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Hủy object mới (không thay thế cái cũ)
            return;
        }
        Music.sprite = BackgroundMusic.Instance.MusicSprite[playerData.isMuteMusic ? 1 : 0];
        Instance = this;
        DontDestroyOnLoad(gameObject);
        selectedLoginMethod = playerData.typeLogin;
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true,
            RequestAuthCode = false
        };
    }
    void Start()
    {
        StartCoroutine(AutoSaveLoop());
    }
    private IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            float waitTime = UnityEngine.Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
            Debug.Log($"⏳ Auto save after {waitTime} seconds");
            PlayerDataSyncManager.SaveGameToPlayFab(
                playerData,
                onComplete: () => Debug.Log("✅ Auto save complete")
            );
        }
    }
    public static void ResetInstance()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject); // Huỷ gameObject chứ không chỉ set null
            Instance = null;
        }
    }
    public void MuteMusic()
    {
        BackgroundMusic.Instance.audioMusic.mute = !BackgroundMusic.Instance.audioMusic.mute;
        playerData.isMuteMusic = BackgroundMusic.Instance.audioMusic.mute;
        Music.sprite = BackgroundMusic.Instance.MusicSprite[BackgroundMusic.Instance.audioMusic.mute ? 1 : 0];
    }
    public void OnApplicationQuit()
    {
        PlayerDataSyncManager.SaveGameToPlayFab(playerData);
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            // App sắp bị ẩn → lưu dữ liệu lại
            PlayerDataSyncManager.SaveGameToPlayFab(playerData);
        }
    }
    public void OnInputChanged(TMP_InputField value)
    {
        string filtered = Regex.Replace(value.text, @"[^\u0020-\u007E]", ""); // Giữ ký tự ASCII printable (space -> ~)
        filtered = Regex.Replace(filtered, "[^a-zA-Z0-9]", ""); // Giữ lại chỉ chữ + số
        if (filtered != value.text)
        {
            value.text = filtered;
            NotiButton.Instance.ShowNotice("Chỉ được nhập chữ cái và số!");
        }
        if (value.text.Length > 14)
        {
            value.text = value.text.Remove(14);
            NotiButton.Instance.ShowNotice("Tên nhân vật phải từ 3–14 ký tự");
        }
    }
    public bool CheckInput(int i)
    {
        if (string.IsNullOrEmpty(username[i].text))
        {
            username[i].Select();
            NotiButton.Instance.ShowNotice("Vui lòng nhập tên đăng nhập");
            return false;
        }
        if (string.IsNullOrEmpty(password[i].text))
        {
            password[i].Select();
            NotiButton.Instance.ShowNotice("Vui lòng nhập mật khẩu");
            return false;
        }
        if (panelRegister.activeSelf)
        {
            if (string.IsNullOrEmpty(playerName[0].text))
            {
                playerName[0].Select();
                NotiButton.Instance.ShowNotice("Vui lòng nhập tên nhân vật");
                return false;
            }
            if (playerName[0].text.Length < 3 || playerName[0].text.Length > 14)
            {
                playerName[0].Select();
                NotiButton.Instance.ShowNotice("Tên nhân vật phải từ 3–14 ký tự");
                return false;
            }
        }
        if (panelCreateName.activeSelf)
        {
            if (string.IsNullOrEmpty(playerName[1].text))
            {
                playerName[1].Select();
                NotiButton.Instance.ShowNotice("Vui lòng nhập tên nhân vật");
                return false;
            }
            if (playerName[1].text.Length < 3 || playerName[1].text.Length > 14)
            {
                playerName[1].Select();
                NotiButton.Instance.ShowNotice("Tên nhân vật phải từ 3–14 ký tự");
                return false;
            }
        }
        if (username[i].text.Length < 3 || username[i].text.Length > 20)
        {
            username[i].Select();
            NotiButton.Instance.ShowNotice("Tên tài khoản phải từ 3–20 ký tự");
            return false;
        }
        if (password[i].text.Length < 6 || password[i].text.Length > 100)
        {
            password[i].Select();
            NotiButton.Instance.ShowNotice("Mật khẩu phải từ 6–100 ký tự");
            return false;
        }
        return true;
    }
    public void OpenPanel(GameObject gameObject, LoginResult result = null, string sessionToken = null)
    {
        gameObject.SetActive(true);
        if (gameObject == panelLogin)
        {
            panelRegister.SetActive(false);
            username[0].text = playerData.username;
            password[0].text = playerData.password;
            Debug.Log($"🔐 Đã lấy thông tin đăng nhập từ PlayerPrefs: {playerData.username}");
        }
        else if (gameObject == panelRegister)
        {
            panelLogin.SetActive(false);
            playerName[0].text = "";
            username[1].text = "";
            password[1].text = "";
        }
        else if (gameObject == panelCreateName)
        {
            btnLoginGoogle.gameObject.SetActive(false);
            btnCreateName.interactable = true;
            btnCreateName.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(playerName[1].text))
                {
                    playerName[1].Select();
                    NotiButton.Instance.ShowNotice("Vui lòng nhập tên nhân vật");
                    return;
                }
                if (playerName[1].text.Length < 3 || playerName[1].text.Length > 14)
                {
                    playerName[1].Select();
                    NotiButton.Instance.ShowNotice("Tên nhân vật phải từ 3–14 ký tự");
                    return;
                }
                btnCreateName.interactable = false;
                SetDisplayName(playerName[1].text, result, sessionToken);
            });
            panelLogin.SetActive(false);
            panelRegister.SetActive(false);
        }
    }
    private void SetDisplayName(string newName, LoginResult result1, string sessionToken)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newName
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result =>
            {
                ProceedAfterLogin(result1, false, sessionToken);
            },
            error =>
            {
                if (error.Error == PlayFabErrorCode.NameNotAvailable)
                {
                    NotiButton.Instance.ShowNotice("Tên nhân vật đã được sử dụng");
                }
                btnCreateName.interactable = true;
            }
        );
    }
    // Khi người dùng chọn, gọi hàm này
    public void SelectLoginMethod(string method)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            NotiButton.Instance.ShowNotice("Vui lòng kết nối mạng để đăng nhập!");
            return;
        }
        selectedLoginMethod = (LoginMethod)System.Enum.Parse(typeof(LoginMethod), method);
        Debug.Log($"🔐 Đã chọn phương thức đăng nhập: {selectedLoginMethod}");
        isLoggingIn = true;
        switch (selectedLoginMethod)
        {
            case LoginMethod.Google:
                SignInWithGoogle(false);
                break;
            case LoginMethod.Username:
                if (!CheckInput(0)) return;
                LoginWithUsername();
                break;
        }
    }
    public void Update()
    {
        checkTimer += Time.deltaTime;
        if ((int)checkTimer == 0 || checkTimer >= checkInterval)
        {
            CheckInternetAndLogin();
            checkTimer = 0;
        }
    }
    public void CheckInternetAndLogin()
    {
        // Kiểm tra không có mạng
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (isLoggingIn) isLoggingIn = false;
            if (isLoggedIn) isLoggedIn = false;
            return;
        }
        // Chỉ thực hiện auto-reconnect nếu KHÔNG ở Login scene
        if (SceneManager.GetActiveScene().name == "SignIn")
        {
            if (selectedLoginMethod == LoginMethod.Username && string.IsNullOrEmpty(playerData.username) && string.IsNullOrEmpty(playerData.password)) return;
            if (selectedLoginMethod == LoginMethod.Google && !playerData.isGoogleLoggedIn) return;
        }

        if (isLoggedIn || isLoggingIn) return;
        isLoggingIn = true;
        if (selectedLoginMethod == LoginMethod.Username)
        {
            Debug.Log("🌐 Có mạng, thử đăng nhập bằng tài khoản...");
            LoginWithUsername();
        }
        else if (selectedLoginMethod == LoginMethod.Google)
        {
            Debug.Log("🌐 Có mạng, thử đăng nhập Google...");
            SignInWithGoogle(true);
        }
    }
    public void RegisterWithUsername()
    {
        if (!CheckInput(1)) return;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            NotiButton.Instance.ShowNotice("Vui lòng kết nối mạng để đăng ký!");
            return;
        }
        registerButton.interactable = false;
        btnLoginGoogle.interactable = false;
        btnSwitchLogin.interactable = false;
        var request = new RegisterPlayFabUserRequest
        {
            DisplayName = playerName[0].text,
            Username = username[1].text,
            Password = password[1].text,
            RequireBothUsernameAndEmail = false // không cần email
        };
        PlayFabClientAPI.RegisterPlayFabUser(request,
        result =>
        {
            playerData.playerName = playerName[0].text;
            playerData.username = username[1].text;
            playerData.password = password[1].text;
            Debug.Log("✅ Đăng ký thành công!");
            LoginWithUsername(true); // tự login sau khi đăng ký
        },
        error =>
        {
            switch (error.Error)
            {
                case PlayFabErrorCode.UsernameNotAvailable:
                    NotiButton.Instance.ShowNotice("Tên đăng nhập đã được sử dụng");
                    username[1].Select();
                    break;
                case PlayFabErrorCode.NameNotAvailable:
                    NotiButton.Instance.ShowNotice("Tên nhân vật đã được sử dụng");
                    playerName[0].Select();
                    break;
                default:
                    NotiButton.Instance.ShowNotice("Đăng ký thất bại. Vui lòng thử lại sau");
                    break;
            }
            registerButton.interactable = true;
            btnLoginGoogle.interactable = true;
            btnSwitchLogin.interactable = true;
        });
    }

    public void LoginWithUsername(bool isNewAccount = false)
    {
        loginButton.interactable = false;
        btnLoginGoogle.interactable = false;
        btnSwitchRegister.interactable = false;
        var request = new LoginWithPlayFabRequest
        {
            Username = string.IsNullOrEmpty(username[0].text) ? playerData.username : username[0].text,
            Password = string.IsNullOrEmpty(password[0].text) ? playerData.password : password[0].text,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithPlayFab(request, result => OnPlayFabLoginSuccess(result, isNewAccount), OnPlayFabLoginError);
    }
    public void SignInWithGoogle(bool silent = false)
    {
        GoogleSignIn.Configuration = configuration;
        if (silent)
        {
            // Không đăng xuất, không popup — thử đăng nhập lại bằng session cũ
            GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith(task => OnGoogleSignInFinished(task, true));
        }
        else
        {// Bắt buộc popup chọn tài khoản
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            GoogleSignIn.Configuration.RequestEmail = true;
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task => OnGoogleSignInFinished(task));
        }
    }
    private void OnGoogleSignInFinished(Task<GoogleSignInUser> task, bool silent = false)
    {
        if (task.IsCanceled || task.IsFaulted)
        {
            Debug.LogError("❌ Google Sign-In thất bại.");
            playerData.isGoogleLoggedIn = false;
            if (!silent) NotiButton.Instance.ShowNotice("Đăng nhập Google thất bại. Vui lòng đăng nhập lại");
            OpenPanel(panelLogin);
            isLoggingIn = false;
            return;
        }

        playerData.isGoogleLoggedIn = true;
        isLoggingIn = true;
        var request = new LoginWithCustomIDRequest
        {
            CustomId = task.Result.UserId,
            CreateAccount = true // nếu chưa tồn tại thì tạo mới
        };
        PlayFabClientAPI.LoginWithCustomID(request, result => OnPlayFabLoginSuccess(result, false), OnPlayFabLoginError);
        //var request = new LoginWithGoogleAccountRequest
        //{
        //    TitleId = PlayFabSettings.TitleId,
        //    ServerAuthCode = task.Result.AuthCode,
        //    CreateAccount = true,
        //    InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
        //    {
        //        GetPlayerProfile = true
        //    }
        //};
        //PlayFabClientAPI.LoginWithGoogleAccount(request, result => OnPlayFabLoginSuccess(result, false), OnPlayFabLoginError);
    }

    public void OnPlayFabLoginSuccess(LoginResult result, bool isNewAccount)
    {
        Debug.Log("✅ Đăng nhập PlayFab thành công!");
        if(SceneManager.GetActiveScene().name != "SignIn") return;
        // Tạo session token mới
        string newSessionToken = Guid.NewGuid().ToString();
        playerData.sessionTokenKey = newSessionToken;

        // Cập nhật token lên PlayFab
        var updateReq = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { sessionTokenKey, newSessionToken } }
        };

        PlayFabClientAPI.UpdateUserData(updateReq,
        updateResult =>
        {
            Debug.Log("SessionToken cập nhật thành công trên PlayFab.");
            if (selectedLoginMethod == LoginMethod.Google && result.NewlyCreated)
            {
                // Nếu là tài khoản mới đăng nhập bằng Google, mở panel tạo tên
                OpenPanel(panelCreateName, result, newSessionToken);
                return;
            }
            ProceedAfterLogin(result, isNewAccount, newSessionToken);
        },
        error =>
        {
            Debug.LogWarning($"Cập nhật SessionToken lỗi: {error.GenerateErrorReport()} - Vẫn tiếp tục kết nối Photon.");
            if (selectedLoginMethod == LoginMethod.Google && result.NewlyCreated)
            {
                // Nếu là tài khoản mới đăng nhập bằng Google, mở panel tạo tên
                OpenPanel(panelCreateName, result, newSessionToken);
                return;
            }
            ProceedAfterLogin(result, isNewAccount, newSessionToken);
        });
    }

    private void ProceedAfterLogin(LoginResult result, bool isNewAccount, string sessionToken)
    {
        string displayName = result.InfoResultPayload?.PlayerProfile?.DisplayName;
        PhotonSession.Instance.ConnectToPhoton(result.PlayFabId, sessionToken);
        // Load hoặc save dữ liệu tùy trạng thái
        Action doLoad = () => PlayerDataSyncManager.LoadGameFromPlayFab(playerData, result, isNewAccount);

        if (!isNewAccount && playerData.typeNotSave && playerData.playerName == displayName)
        {
            PlayerDataSyncManager.SaveGameToPlayFab(playerData, doLoad);
        }
        else
        {
            doLoad();
        }
    }

    public void Login(LoginResult result, bool isNewAccount)
    {
        isLoggedIn = true;
        isLoggingIn = false;
        loginButton.interactable = true;
        registerButton.interactable = true;
        btnLoginGoogle.interactable = true;
        btnSwitchLogin.interactable = true;
        btnSwitchRegister.interactable = true;
        playerData.typeLogin = selectedLoginMethod;
        if (selectedLoginMethod == LoginMethod.Username)
        {
            playerData.username = string.IsNullOrEmpty(username[0].text) ? playerData.username : username[0].text;
            playerData.password = string.IsNullOrEmpty(password[0].text) ? playerData.password : password[0].text;
        }
        string displayName = result.InfoResultPayload?.PlayerProfile?.DisplayName;
        if (!string.IsNullOrEmpty(displayName))
        {
            playerData.playerName = displayName;
            Debug.Log("🎉 DisplayName: " + displayName);
        }

        if (SceneManager.GetActiveScene().name == "SignIn")
        {
            if (!isNewAccount && !result.NewlyCreated) SceneManager.LoadScene("Lobby");
            else SceneManager.LoadScene("Story");
        }
    }

    public void OnPlayFabLoginError(PlayFabError error)
    {
        NotiButton.Instance.ShowNotice("Đăng nhập thất bại. Vui lòng thử lại sau");
        if (!panelLogin.activeSelf) OpenPanel(panelLogin);
        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidUsernameOrPassword:
                NotiButton.Instance.ShowNotice("Tên đăng nhập hoặc mật khẩu không đúng");
                break;
            case PlayFabErrorCode.AccountNotFound:
                NotiButton.Instance.ShowNotice("Tài khoản không tồn tại. Bạn cần đăng ký");
                break;
            case PlayFabErrorCode.NameNotAvailable:
                NotiButton.Instance.ShowNotice("Tên nhân vật đã được sử dụng");
                break;
            case PlayFabErrorCode.UsernameNotAvailable:
                NotiButton.Instance.ShowNotice("Tên đăng nhập đã được sử dụng");
                break;
            case PlayFabErrorCode.InvalidEmailAddress:
                NotiButton.Instance.ShowNotice("Địa chỉ email không hợp lệ");
                break;
            case PlayFabErrorCode.AccountBanned:
                NotiButton.Instance.ShowNotice("Tài khoản đã bị khóa");
                break;
            default:
                NotiButton.Instance.ShowNotice("Đăng nhập thất bại. Vui lòng thử lại sau");
                break;
        }
        isLoggingIn = false;
        loginButton.interactable = true;
        registerButton.interactable = true;
        btnLoginGoogle.interactable = true;
        btnSwitchLogin.interactable = true;
        btnSwitchRegister.interactable = true;
    }
}
