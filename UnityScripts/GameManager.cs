using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Red;
    [SerializeField] private GameObject predajaUpit;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private TextMeshProUGUI gameOverText;

    public Sprite xSprite;
    public Sprite ySprite;
    public Sprite nullSprite;

    [SerializeField] private GameObject[] polja = new GameObject[9];

    private bool isLocalGame;
    private bool isMyTurn;
    private bool isGameOver;
    private string currentPlayerSymbol; // 'X' ili 'O'
    private bool isPollingActive = false;
    [SerializeField] private float pollingInterval = 1.5f;
    private int lastProcessedMove = 0;


    void Awake()
    {
        isLocalGame = !PersistentItems.gameTypeChosen;
        Debug.Log("lokalna: " + isLocalGame);

        Debug.Log("Current player: " + PersistentItems.currentPlayer);
        if (!isLocalGame)
        {
            StartCoroutine(KreirajIgru(
                "comp",
                PersistentItems.currentPlayer,
                PersistentItems.currentPlayer
            ));
        }
        else
        {
            StartCoroutine(KreirajIgru(
                "local",
                PersistentItems.currentPlayer,
                PersistentItems.currentPlayer
            ));
        }

        isGameOver = false;
        isPollingActive = false;

        if (isLocalGame)
        {
            currentPlayerSymbol = "X";
            isMyTurn = true;
            SetBoardInteractable(true);
        }
        else
        {
            currentPlayerSymbol = null;
            isMyTurn = false;
            SetBoardInteractable(false);
            StartPolling();
        }

        UpdateTurnText();
    }


    //--------------------------------UI Updates
    private void UpdateTurnText()
    {
        if (isGameOver)
        {
            Red.text = "Igra zavrsena";
            return;
        }

        Red.text = isMyTurn ? "Na redu si!" : "Protivnik je na redu";
    }
    private Sprite GetSpriteForTurn(string turn)
    {
        return turn == "X" ? xSprite : ySprite;
    }
    private void SetBoardInteractable(bool state)
    {
        foreach (GameObject polje in polja)
        {
            polje.GetComponent<UnityEngine.UI.Button>().interactable = state;
        }
    }
    private void DisableField(GameObject polje)
    {
        polje.GetComponent<UnityEngine.UI.Button>().interactable = false;
    }

    private void EnableOnlyEmptyFields()
    {
        foreach (GameObject polje in polja)
        {
            var img = polje.GetComponent<UnityEngine.UI.Image>();
            var btn = polje.GetComponent<UnityEngine.UI.Button>();

            btn.interactable = !isGameOver && img.sprite.name == "Prazno_0";
        }
    }

    //------------------------------------------------Potezi
    public void Potez(int index)
    {
        Debug.Log("klik na polje "+index);
        if (isGameOver || !isMyTurn)
            return;

        GameObject polje = polja[index];
        if (!polje.GetComponent<UnityEngine.UI.Button>().interactable)
            return;

        polje.GetComponent<UnityEngine.UI.Image>().sprite =
            GetSpriteForTurn(currentPlayerSymbol);

        DisableField(polje);

        if (!isLocalGame)
        {
            isMyTurn = false;
            SetBoardInteractable(false);
        }

        UpdateTurnText();
        StartCoroutine(SendMoveToBackend(index));
    }

    IEnumerator SendMoveToBackend(int index)
    {
        int cx = index % 3;
        int cy = index / 3;

        Potez request = new Potez
        {
            igra_id = PersistentItems.currentGameId,
            igrac = PersistentItems.currentPlayer,
            x = cx,
            y = cy
        };
        string json = JsonUtility.ToJson(request);
        using (UnityWebRequest req = new UnityWebRequest(
            $"{PersistentItems.baseUrl}/igra/move/{PersistentItems.currentGameId}",
            "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Greška pri slanju poteza: " + req.error);
                isMyTurn = true;
                SetBoardInteractable(true);
            }
        }
        if (isLocalGame)
        {
            yield return StartCoroutine(FetchGameStateOnce());
        }
    }



    //------------------------------------Polling
    IEnumerator FetchGameStateOnce()
    {
        string url = PersistentItems.baseUrl +
                     "/igra/" + PersistentItems.currentGameId;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Local state fetch error: " + req.error);
                yield break;
            }

            stanjeIgreOdg gameState =
                JsonUtility.FromJson<stanjeIgreOdg>(req.downloadHandler.text);

            CheckGameOver(gameState.pobjednik);

            if (isGameOver)
                yield break;

            currentPlayerSymbol = gameState.red;
            isMyTurn = true;
            EnableOnlyEmptyFields();
            UpdateTurnText();
        }
    }


    public void StartPolling()
    {
        if (!isPollingActive)
        {
            isPollingActive = true;
            StartCoroutine(PollGameState());
        }
    }
    public void StopPolling()
    {
        isPollingActive = false;
    }

    IEnumerator PollGameState()
    {
        while (isPollingActive && !isGameOver)
        {
            yield return new WaitForSeconds(pollingInterval);

            string stateUrl = PersistentItems.baseUrl +
                              "/igra/" + PersistentItems.currentGameId;

            using (UnityWebRequest stateReq = UnityWebRequest.Get(stateUrl))
            {
                yield return stateReq.SendWebRequest();

                if (stateReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Polling state error: " + stateReq.error);
                    continue;
                }

                stanjeIgreOdg gameState =
                    JsonUtility.FromJson<stanjeIgreOdg>(stateReq.downloadHandler.text);

                Debug.Log("pobjednik: "+gameState.pobjednik);
                Debug.Log("Red: "+gameState.red);
                if (gameState.pobjednik != "TRAJE" && gameState.pobjednik != "traje")
                {
                    CheckGameOver(gameState.pobjednik);
                    yield break;
                }

                HandleGameStateResponse(gameState);

                if (gameState.potezi == null)
                    continue;
                isMyTurn = gameState.red == currentPlayerSymbol && !isGameOver;
                foreach (var move in gameState.potezi)
                {
                    if (move.rb <= lastProcessedMove)
                        continue;

                    int index = move.y * 3 + move.x;

                    if (index < 0 || index >= polja.Length)
                        continue;

                    string symbol = move.igrac == gameState.igracx ? "X" : "O";

                    polja[index].GetComponent<UnityEngine.UI.Image>().sprite = GetSpriteForTurn(symbol);


                    lastProcessedMove = move.rb;
                }
            }
        }
    }





    //--------------------------------Game state
    void HandleGameStateResponse(stanjeIgreOdg gameState)
    {
        if ((gameState.pobjednik != "TRAJE" && gameState.pobjednik != "traje") || gameState.red == "DONE")
        {
            CheckGameOver(gameState.pobjednik);
            return;
        }


        CheckIfPlayerJoined(gameState);
        if (currentPlayerSymbol == null)
            return;

        isMyTurn = gameState.red == currentPlayerSymbol && !isGameOver;
        UpdateTurnText();
        EnableOnlyEmptyFields();
    }

    void CheckIfPlayerJoined(stanjeIgreOdg gameState)
    {
        if (currentPlayerSymbol != null) return;
        if (PersistentItems.gameTypeChosen == false)
        {
            currentPlayerSymbol = "X";
            isMyTurn = (gameState.red == "X");
            OnGameStarted();
            return;
        }

        if (PersistentItems.currentPlayer == gameState.igracx)
        {
            currentPlayerSymbol = "X";
            if (!string.IsNullOrEmpty(gameState.igraco))
            {
                OnGameStarted(); EnableOnlyEmptyFields();
            }
        }
        else if (PersistentItems.currentPlayer == gameState.igraco)
        {
            Debug.Log("Pridružio si se igri kao O");

            currentPlayerSymbol = "O";
            OnGameStarted(); EnableOnlyEmptyFields();
        }

    }

    public void Gotovo()
    {
        PersistentItems.LoadScene("MainMenu");
    }

    public void PredajaPotvrda(bool predaja)
    {
        if (predaja)
        {
            StartCoroutine(GiveUp());
            PersistentItems.LoadScene("MainMenu");
        }
        predajaUpit.SetActive(false);
    }

    IEnumerator GiveUp()
    {
        Predaja request = new Predaja
        {
            igra_id = PersistentItems.currentGameId,
            igrac = PersistentItems.currentPlayer,
        };

        string json = JsonUtility.ToJson(request);
        using (UnityWebRequest req = new UnityWebRequest(
            $"{PersistentItems.baseUrl}/igra/predaja/{PersistentItems.currentGameId}/{PersistentItems.currentPlayer}",
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

    public void PredajaUpit()
    {
        predajaUpit.SetActive(true);
    }



    //----------------------------------Game Over
    void HandleGameOver(string rezultat)
    {
        Debug.Log("rezultat: "+ rezultat);
        isGameOver = true;
        gameOver.SetActive(true);

        if (rezultat == PersistentItems.currentPlayer)
            gameOverText.text = "Pobjeda!";
        else if (rezultat == "Nerijeseno")
            gameOverText.text = "Nerijeseno";
        else
            gameOverText.text = "Poraz";
    }

    public IEnumerator KreirajIgru(string tip, string igracX, string host)
    {
        NovaIgraRequest request = new NovaIgraRequest
        {
            tip = tip,
            igracX = igracX,
            igracO = null,
            host = host
        };

        string json = JsonUtility.ToJson(request);
        string url = PersistentItems.baseUrl + "/igra/nova";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            req.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Greška pri kreiranju igre: " + req.error);
                yield break;
            }

            NovaIgraResponse response = JsonUtility.FromJson<NovaIgraResponse>(req.downloadHandler.text);

            if (response.success)
            {
                Debug.Log("Igra kreirana s ID: " + response.id_igre);
                PersistentItems.currentGameId = response.id_igre;
            }
        }
    }


    //---------------------------------------------UI POP ups
    void CheckGameOver(string winner)
    {
        Debug.Log("Winner: "+winner);
        if (winner != "TRAJE" && winner != "traje")
        {
            isGameOver = true;
            StopPolling();

            if (winner == currentPlayerSymbol)
            {
                Debug.Log("Pobjeda!");
            }
            else if (winner == "tie" || winner == "TIE")
            {
                Debug.Log("Neriješeno!");
            }
            else
            {
                Debug.Log("Poraz!");
            }

            HandleGameOver(winner);
        }
    }


  
    void OnGameStarted()
    {
        Debug.Log($"Igra je počela! Ti si: {currentPlayerSymbol}");
        UpdateTurnText();
        ResetBoard();
    }

    void ResetBoard()
    {
        foreach (GameObject polje in polja)
        {
            polje.GetComponent<UnityEngine.UI.Image>().sprite = nullSprite;
            polje.GetComponent<UnityEngine.UI.Button>().interactable =
                !isGameOver && isMyTurn;
        }
    }
}