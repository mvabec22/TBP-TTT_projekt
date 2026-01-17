using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class PersistentItems : MonoBehaviour
{

    public static PersistentItems Instance;
    public static string baseUrl = "http://10.62.45.214:8000";

    public static bool gameTypeChosen = false; //false = local, true = comp
    public static bool RegisterLog = false; //false = register, true = LogIn
    public static string currentPlayer = "";
    public static string playerstat = "";
    public static int currentGameId = 0;
    public string myTurn = "X";
    public static bool isAdmin = false;

    private List<string> sceneHistory = new List<string>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // ---------------------------------------Organizacija Scena-----------------------
    public static void LoadScene(string sceneName)
    {
        Instance.sceneHistory.Add(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(sceneName);
    }

    public static void Back()
    {
        if (Instance.sceneHistory.Count > 0)
        {
            string previousScene =
            Instance.sceneHistory[Instance.sceneHistory.Count - 1];
            Instance.sceneHistory.RemoveAt(Instance.sceneHistory.Count - 1);
            if (previousScene == "MainMenu")
            {
                playerstat = currentPlayer;
            }
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.Log("No previous scene recorded!");
        }
    }

    public static void Quit()
    {
        Application.Quit();
    }

    //-----------------------------FastAPI povezivanje -------------------------
    public void Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest(endpoint, onSuccess, onError));
    }

    public void Post<T>(string endpoint, object data, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostRequest(endpoint, data, onSuccess, onError));
    }

    private IEnumerator GetRequest<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
    {
        string url = baseUrl + endpoint;
        using UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.downloadHandler.text);
        }
        else
        {
            T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
            onSuccess?.Invoke(result);
        }
    }

    private IEnumerator PostRequest<T>(string endpoint, object data, Action<T> onSuccess, Action<string> onError)
    {
        string url = baseUrl + endpoint;
        string json = JsonUtility.ToJson(data);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.downloadHandler.text);
        }
        else
        {
            if (typeof(T) == typeof(VoidResponse))
            {
                onSuccess?.Invoke(default);
            }
            else
            {
                T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
                onSuccess?.Invoke(result);
            }
        }
    }

}
