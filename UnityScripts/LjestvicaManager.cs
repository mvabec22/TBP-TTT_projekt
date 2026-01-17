using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System;
using UnityEngine.SceneManagement;

public class LjestvicaManager : MonoBehaviour
{
    public TextMeshProUGUI[] ljestvica = new TextMeshProUGUI[11];
    [SerializeField] private TextMeshProUGUI popisIgraca; 

    void Start()
    {
        StartCoroutine(FetchLeaderboard());

    }
    public IEnumerator FetchLeaderboard()
    {
        string url = PersistentItems.baseUrl + "/ljestvica/" + PersistentItems.currentPlayer;
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Greska pri dohvacanju ljestvice: " + req.error);
            yield break;
        }

        string json = req.downloadHandler.text;
        Debug.Log("JSON odgovor: " + json);

        try
        {
            LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(json);

            if (response != null && response.ljestvica != null)
            {
                if (SceneManager.GetActiveScene().name == "AdminScene")
                {
                    PopuniPopis(response.ljestvica);
                }
                else
                {
                    PopuniLJestvicu(response.ljestvica);

                }
            }
            else
            {
                Debug.LogError("Nema podataka u odgovoru ili je format neispravan");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Greska pri parsiranju JSON-a: " + e.Message);
            Debug.Log("Primljeni JSON: " + json);
        }
    }
    void PopuniLJestvicu(StavkaLjestvice[] entries)
    {
        if (ljestvica == null)
        {
            Debug.LogError("Ljestvica polje nije inicijalizirano! Provjeri Inspector.");
            return;
        }

        if (entries == null)
        {
            Debug.LogError("Entries niz je null!");
            return;
        }

        Debug.Log($"Popunjavam {ljestvica.Length} polja sa {entries.Length} unosa");

        int brojZaPopuniti = Mathf.Min(ljestvica.Length, entries.Length);

        for (int i = 0; i < brojZaPopuniti; i++)
        {
            if (ljestvica[i] == null)
            {
                Debug.LogError($"Element ljestvice[{i}] je null!");
                continue;
            }

            if (entries[i] != null)
            {
                ljestvica[i].text = $"{entries[i].rang}-{entries[i].kor_ime}-{entries[i].pobjede}-{entries[i].gubitci}-{entries[i].izjednaceno}";
            }
        }
        for (int i = brojZaPopuniti; i < ljestvica.Length; i++)
        {
            if (ljestvica[i] != null)
            {
                ljestvica[i].text = "";
            }
        }
    }

    void PopuniPopis(StavkaLjestvice[] entries)
    {
        if (entries == null)
        {
            Debug.LogError("Entries niz je null!");
            popisIgraca.text = "";
            return;
        }
        popisIgraca.text = "";
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null)
            {
                popisIgraca.text += $"{entries[i].rang}-{entries[i].kor_ime}-{entries[i].pobjede}-{entries[i].gubitci}-{entries[i].izjednaceno}\n";
            }
        }
    }


    public void Stats(int brIgraca)
    {
        try
        {
            if (brIgraca < 0 || brIgraca >= ljestvica.Length)
            {
                Debug.LogWarning("Neispravan indeks igraca za statistiku");
                return;
            }

            if (ljestvica[brIgraca] == null)
            {
                Debug.LogWarning($"Element ljestvice na indeksu {brIgraca} je null");
                return;
            }

            string text = ljestvica[brIgraca].text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning("Odabrano polje je prazno");
                return;
            }

            string[] parts = text.Split('-');

            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                string imeIgraca = parts[1].Trim();
                PersistentItems.playerstat = imeIgraca;
                PersistentItems.LoadScene("Statistika");

                Debug.Log($"Odabran igrač za statistiku: {imeIgraca}");
            }
            else
            {
                Debug.LogWarning($"Neispravan format teksta za ljestvicu: '{text}'");
                Debug.Log($"Dijelovi: {string.Join("|", parts)}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Greška u Stats metodi: {e.Message}");
        }
    }
}