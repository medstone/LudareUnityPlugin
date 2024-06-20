using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

#if LUDARE_USE_STEAM_LOGIN
using Steamworks;
#endif

#if LUDARE_USE_EOS_LOGIN
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using PlayEveryWare.EpicOnlineServices;
#endif

#if LUDARE_USE_ANDROID_LOGIN
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class LoginEvent : UnityEvent<bool> { };

public class LoginCloseEvent : UnityEvent { };

struct LoginResponse
{
    public bool success;
    public string message;
    public string LudareID;
}

public class LudareManager : MonoBehaviour
{
    static string appdataDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
    static string appDirectory = "/Ludare";
    static string backupFileName = appDirectory + "/LocalEvents.csv";
    static string fullDirectory = appdataDirectory + backupFileName;

    public string ludareUserID;

    public string platformUserID;

    public string ludareGameID;

    public string ludareGameSecret;

    public bool debugOutput;

    public float maxTickTimer = 300;

    public float currTimer = 0;

    static public LudareManager single;

    public static LoginEvent onLogin;

    bool trackingStarted;

    private IEnumerator loginCoroutine;

    private StreamReader localBackupReader;

    private StreamWriter localBackupWriter;

    public NetworkReachability currNetworkState = NetworkReachability.NotReachable;

    public GameObject loadingPrefab;

    public GameObject loadingInst;

#if LUDARE_USE_EOS_LOGIN
    private EOSManager eosManager;

    private GameObject eosManagerObject;

    public GameObject eosManagerPrefab;
#endif

    void Awake()
    {

        onLogin = new LoginEvent();

#if LUDARE_USE_ANDROID_LOGIN
        PlayGamesPlatform.Activate();
#endif

#if LUDARE_USE_EOS_LOGIN
        eosManager = (EOSManager)FindObjectOfType(typeof(EOSManager));

        if(eosManager == null)
        {
            eosManagerObject = Instantiate(eosManagerPrefab, new Vector3(0,0,0), Quaternion.identity);
            eosManager = eosManagerObject.GetComponent<EOSManager>();
        }
#endif
    }

    // Start is called before the first frame update
    void Start()
    {

        if(single != null)
        {
            Destroy(this);
            return;
        }

        single = this;
        currTimer = maxTickTimer;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        currTimer -= Time.deltaTime;

        if (currTimer <= 0)
        {

            StartCoroutine(UpdateTracking());

            currTimer = maxTickTimer;
        }
    }

    void GameStart()
    {

    }

    void OnApplicationQuit()
    {
        StartCoroutine(TryWriteEvent("/RegisterGameShutdown"));
    }

    public bool TryLoginPlatform()
    {

        StartLoading();
#if LUDARE_USE_STEAM_LOGIN
        platformUserID = SteamUser.GetSteamID().ToString();

        StartCoroutine(TryLoginPlatform("Steam", platformUserID));
#endif

#if LUDARE_USE_EOS_LOGIN
        EOSManager.Instance.StartLoginWithLoginTypeAndToken(LoginCredentialType.AccountPortal, "", "", StartLoginWithLoginTypeAndTokenCallback);
#endif

#if LUDARE_USE_ANDROID_LOGIN
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if(success == SignInStatus.Success)
            {
                if(debugOutput == true) Debug.Log("Login Success");

            }
        });
#endif

        return false;
    }

    public void TryLoginLudare(string Username, string Password)
    {
        StartLoading();
        StartCoroutine(TryLogin(Username, Password));

    }

#if LUDARE_USE_EOS_LOGIN
    public void StartLoginWithLoginTypeAndTokenCallback(Epic.OnlineServices.Auth.LoginCallbackInfo loginCallbackInfo)
    {
        if(loginCallbackInfo.ResultCode == Result.Success)
        {
            platformUserID = loginCallbackInfo.LocalUserId.ToString();
            if(debugOutput == true) Debug.Log("Login Success");

            StartCoroutine(TryLoginPlatform("Epic", platformUserID));
        }
        else
        {
            if(debugOutput == true) Debug.Log("Login failed");
        }
    }
#endif

    IEnumerator CreateAndAccessBackupFile(bool read)
    {
        if (Directory.Exists(appdataDirectory + appDirectory) == false)
        {
            Directory.CreateDirectory(appdataDirectory + appDirectory);
        }

        if (File.Exists(fullDirectory) == false)
        {
            File.Create(fullDirectory);
        }

        while (localBackupWriter != null || localBackupReader != null)
        {
            yield return new WaitForSeconds(1);
        }

        if (read == true)
        {
            localBackupReader = File.OpenText(fullDirectory);
        }
        else
        {
            localBackupWriter = File.AppendText(fullDirectory);
        }
    }

    public void StartLoading()
    {
        loadingInst = Instantiate(loadingPrefab, Vector3.zero, Quaternion.identity);
    }

    public void EndLoading()
    {
        Destroy(loadingInst);
    }

    IEnumerator TryWriteEvent(string eventType)
    {
        if (currNetworkState == NetworkReachability.NotReachable && Application.internetReachability != NetworkReachability.NotReachable)
        {
            StartCoroutine(UploadBacklog());
        }

        currNetworkState = Application.internetReachability;

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield return CreateAndAccessBackupFile(false);

            string Store = eventType + ",";
            Store += ludareGameID + ",";
            Store += ludareUserID + ",";
            Store += ludareGameSecret + ",";
            Store += System.DateTime.Now;
            localBackupWriter.WriteLine(Store);
            localBackupWriter.Close();
            localBackupWriter = null;
        }
        else
        {

            UnityWebRequest www = UnityWebRequest.Post("https://www.devpowered.com:8000" + eventType,
                "{ \"userid\": \"" + ludareUserID + "\", \"gameid\": \"" + ludareGameID + "\", \"secret\": \"" + ludareGameSecret + "\", \"timestamp\": \"" + System.DateTime.Now + "\" }",
                "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                yield return CreateAndAccessBackupFile(false);

                string Store = eventType + ",";
                Store += ludareGameID + ",";
                Store += ludareUserID + ",";
                Store += ludareGameSecret + ",";
                Store += System.DateTime.Now;
                localBackupWriter.WriteLine(Store);
                localBackupWriter.Close();
                localBackupWriter = null;
            }
        }

        yield return null;
    }

    IEnumerator TryLogin(string Username, string Password)
    {
        UnityWebRequest www = UnityWebRequest.Post("https://www.devpowered.com:8000/GameLogin",
            "{ \"password\": \"" + Password + "\", \"username\": \"" + Username + "\" }",
            "application/json");

        yield return www.SendWebRequest();

        if(www.result != UnityWebRequest.Result.Success)
        {
            if (debugOutput == true)
            {
                Debug.Log("Login failed");
                Debug.Log(www.error);
            }
        }
        else
        {

            LoginResponse res = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);

            onLogin.Invoke(res.success);

            if (res.success == true)
            {
                ludareUserID = res.message;

                StartCoroutine(StartTracking());
            }
        }

        EndLoading();
    }

    IEnumerator TryLoginPlatform(string Platform, string PlatformId)
    {
        UnityWebRequest www = UnityWebRequest.Get("https://www.devpowered.com:8000/GetUserID?platform=" + Platform + "&userid=" + PlatformId);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            if (debugOutput == true)
            {
                Debug.Log("Login failed");
                Debug.Log(www.error);
            }
        }
        else
        {

            LoginResponse res = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);

            onLogin.Invoke(res.success);

            if (res.success == true)
            {
                ludareUserID = res.LudareID;

                StartCoroutine(StartTracking());
            }
        }

        EndLoading();
    }

    IEnumerator StartTracking()
    {

        StartCoroutine(TryWriteEvent("/RegisterGameStart"));
        trackingStarted = true;

        yield return null;

    }

    IEnumerator UpdateTracking()
    {
        StartCoroutine(TryWriteEvent("/RegisterGameContinue"));

        yield return null;
    }

    IEnumerator UploadBacklog()
    {
        string nextLine = "";

        yield return CreateAndAccessBackupFile(true);

        while ((nextLine = localBackupReader.ReadLine()) != null)
        {
            string[] vars = nextLine.Split(',');

            UnityWebRequest www = UnityWebRequest.Post("https://www.devpowered.com:8000" + vars[0],
                "{ \"userid\": \"" + vars[1] + "\", \"gameid\": \"" + vars[2] + "\", \"timestamp\": \"" + vars[3] + "\" }",
                "application/json");

            yield return www.SendWebRequest();
        }

        localBackupReader.Close();
        localBackupReader = null;

        if(File.Exists(fullDirectory) == true)
        {
            File.Delete(fullDirectory);
        }

        yield return null;
    }
}
