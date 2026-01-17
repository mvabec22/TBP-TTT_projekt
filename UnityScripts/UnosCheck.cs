using UnityEngine;
using TMPro;

public class UnosCheck : MonoBehaviour
{
    [SerializeField] private TMP_InputField kor_ime;
    [SerializeField] private TMP_InputField passw;

    private string ime;
    private string lozinka;

    [SerializeField] private GameObject unosUpozorenje;
    [SerializeField] private TextMeshProUGUI greska;

    public void PotvrdiUnos()
    {
        ime = kor_ime.text;
        lozinka = passw.text;

        if (string.IsNullOrEmpty(ime) || string.IsNullOrEmpty(lozinka))
        {
            PrikaziGresku("Ime i lozinka su obavezni");
            return;
        }

        AuthRequest data = new AuthRequest
        {
            kor_ime = ime,
            lozinka = lozinka
        };

        if (PersistentItems.RegisterLog == false)
        {
            Debug.Log("Obavlja se registracija");
            PersistentItems.Instance.Post<AuthResponse>(
                "/auth/register",
                data,
                response =>
                {
                    PersistentItems.currentPlayer = ime;
                    PersistentItems.playerstat = ime;
                    PersistentItems.isAdmin = false;
                    PersistentItems.LoadScene("MainMenu");
                },
                error =>
                {
                    PrikaziGresku("Registracija neuspjesna");
                }
            );
        }
        else
        {
            PersistentItems.Instance.Post<LoginResponse>(
                "/auth/login",
                data,
                response =>
                {
                    PersistentItems.currentPlayer = ime;
                    PersistentItems.playerstat = ime;
                    PersistentItems.isAdmin = response.isAdmin;
                    PersistentItems.LoadScene("MainMenu");
                },
                error =>
                {
                    PrikaziGresku("Neispravno korisnicko ime ili lozinka");
                }
            );
        }
    }

    private void PrikaziGresku(string poruka)
    {
        greska.text = poruka;
        unosUpozorenje.SetActive(true);
    }

    public void GasiUpozorenje()
    {
        unosUpozorenje.SetActive(false);
    }
}
