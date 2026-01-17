using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class StatsManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI imeIgraca;
    [SerializeField] private TextMeshProUGUI statsIgraca;

    void Start()
    {
        imeIgraca.text = PersistentItems.playerstat;
        statsIgraca.text = "Ucitavanje...";

        StartCoroutine(DohvatiStatistiku(PersistentItems.playerstat));
    }

    IEnumerator DohvatiStatistiku(string igrac)
    {
        string url = PersistentItems.baseUrl + "/ljestvica/stats/" + igrac;

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Greska pri dohvacanju statistike: " + req.error);
                Debug.LogError("Status code: " + req.responseCode);
                statsIgraca.text = "Greska pri ucitavanju statistike.";
                yield break;
            }

            string jsonResponse = req.downloadHandler.text;
            Debug.Log("Primljeni JSON: " + jsonResponse);

            try
            {
                StatsIgraca stats = JsonUtility.FromJson<StatsIgraca>(jsonResponse);

                if (stats == null)
                {
                    Debug.LogError("Deserializacija je vratila null!");
                    statsIgraca.text = "Greška: Prazan odgovor";
                    yield break;
                }

                // Formatiraj ispis: pobjede-gubitci-izjednaceno
                statsIgraca.text = $"{stats.pobjede}-{stats.gubitci}-{stats.izjednaceno}";

                Debug.Log($"Statistika uspješno učitana za {stats.kor_ime}: {stats.pobjede}-{stats.gubitci}-{stats.izjednaceno}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Greška pri parsiranju JSON-a: " + e.Message);
                statsIgraca.text = "Greška pri obradi podataka";
            }
        }
    }
}