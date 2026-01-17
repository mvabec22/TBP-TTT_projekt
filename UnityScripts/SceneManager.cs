using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class MySceneManager : MonoBehaviour
{
    [SerializeField] private GameObject adminBtn;
    [SerializeField] private GameObject obrisiPotvrda;
    [SerializeField] private TMP_InputField kor_ime;
    [SerializeField] private TMP_InputField ip_adresa;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            if (PersistentItems.isAdmin)
            {
                adminBtn.SetActive(true);
            }
        }
    }

    public void potvrdiIP()
    {
        PersistentItems.baseUrl = ip_adresa.text;
    }


    public void AdminBtn()
    {
        PersistentItems.LoadScene("AdminScene");
    }
    public void ObrisiBtn(bool brisi)
    {
        if (brisi && kor_ime.text != null)
        {
            obrisiPotvrda.SetActive(true);
        }
        else
        {
            obrisiPotvrda.SetActive(false);
        }
    }

    public void ObrisiIgraca()
    {
        obrisiPotvrda.SetActive(false);
        StartCoroutine(ObrisiPlayer());
    }
    IEnumerator ObrisiPlayer()
    {
        VoidResponse voidrez = new VoidResponse { };

        string json = JsonUtility.ToJson(voidrez);
        using (UnityWebRequest req = new UnityWebRequest(
            $"{PersistentItems.baseUrl}/auth/delete/{kor_ime.text}",
            "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Greška pri slanju predaje: " + req.error);
            }
        }
    }

    public void LoadRegister(bool prijava)
    {
        PersistentItems.RegisterLog = prijava; //false = register, true = LogIn       
        PersistentItems.LoadScene("UnosKorisnik");
    }
    public void LoadLocalGame()
    {
        PersistentItems.gameTypeChosen = false;
        PersistentItems.LoadScene("Gameplay");
    }

    public void LoadMenu()
    {
        PersistentItems.playerstat = PersistentItems.currentPlayer;
        PersistentItems.LoadScene("MainMenu");
    }
    public void LoadHistory()
    {
        PersistentItems.LoadScene("PovijestIgara");
    }
    public void LoadStats()
    {
        PersistentItems.LoadScene("Statistika");
    }
    public void LoadLadder()
    {
        PersistentItems.LoadScene("Ljestvica");
    }
    public void LogOut()
    {
        PersistentItems.currentPlayer = "";
        PersistentItems.LoadScene("Welcome");
    }

    public void Back()
    {
        PersistentItems.Back();
    }

    public void Quit()
    {
        PersistentItems.Quit();
    }

    public void CreateGame(bool gameType)
    {
        PersistentItems.gameTypeChosen= gameType;
        PersistentItems.LoadScene("Gameplay");
    }

}
