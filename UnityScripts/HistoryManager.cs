using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class HistoryManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI imeIgraca;
    [SerializeField] private TextMeshProUGUI[] povijest = new TextMeshProUGUI[10];

    void Start()
    {
        imeIgraca.text = PersistentItems.playerstat;
        StartCoroutine(DohvatiPovijest(PersistentItems.playerstat));
    }

    IEnumerator DohvatiPovijest(string igrac)
    {
        string url = PersistentItems.baseUrl + "/igra/history/" + UnityWebRequest.EscapeURL(igrac);

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Greska pri dohvacanju povijesti: " + req.error);
                OcistiPovijest();
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("Primljeni JSON: " + json);

            try
            {
                GameHistory[] history = JsonHelper.FromJson<GameHistory>(json);

                if (history == null || history.Length == 0)
                {
                    Debug.Log("Nema povijesti igara za ovog igraca");
                    OcistiPovijest();
                    yield break;
                }

                PopuniPovijest(history);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Greska pri parsiranju JSON-a: " + e.Message);
                OcistiPovijest();
            }
        }
    }

    void PopuniPovijest(GameHistory[] history)
    {
        if (povijest == null)
        {
            Debug.LogError("Povijest polje nije inicijalizirano!");
            return;
        }

        int brojZaPopuniti = Mathf.Min(povijest.Length, history.Length);

        for (int i = 0; i < brojZaPopuniti; i++)
        {
            if (povijest[i] == null || history[i] == null) continue;

            var h = history[i];

            string rezultat;
            if (h.pobjednik == "nerijeseno")
                rezultat = "Nerijeseno";
            else if (h.pobjednik == "u tijeku")
                rezultat = "U tijeku";
            else
                rezultat = h.pobjednik;

            string tipIgre = h.tip_igre.ToUpper();
            povijest[i].text = $"{tipIgre}-{h.igracx}-{h.igraco}-{rezultat}-{h.trajanje}";

        }

        for (int i = brojZaPopuniti; i < povijest.Length; i++)
        {
            if (povijest[i] != null)
            {
                povijest[i].text = "";
            }
        }
    }

    void OcistiPovijest()
    {
        if (povijest == null) return;

        for (int i = 0; i < povijest.Length; i++)
        {
            if (povijest[i] != null)
            {
                povijest[i].text = "Nema dostupnih podataka";
            }
        }
    }
}