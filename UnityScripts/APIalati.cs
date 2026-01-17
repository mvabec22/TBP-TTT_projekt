using System;
using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        if (json.StartsWith("["))
        {
            string newJson = "{\"array\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }
        else
        {
            return JsonUtility.FromJson<T[]>(json);
        }
    }

    public static T FromJsonObject<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}

[Serializable]
public class VoidResponse { }


[Serializable]
public class deleteResponse
{
    public string obrisi;
}

public class AuthRequest
{
    public string kor_ime;
    public string lozinka;
}

public class AuthResponse
{
    public string status;
    public string message;
}

[Serializable]
public class NovaIgraRequest
{
    public string tip;
    public string igracX;
    public string igracO;
    public string host;
}

[Serializable]
public class NovaIgraResponse
{
    public bool success;
    public string message;
    public int id_igre;
    public string tip;
    public string igracX;
    public string igracO;
    public string host;
}

[Serializable]
public class Potez
{
    public int rb;
    public int igra_id;
    public string igrac;
    public int x;
    public int y;
}

public class Predaja
{
    public int igra_id;
    public string igrac;
}

[Serializable]
public class stanjeIgreOdg
{
    public int id;
    public string red;
    public string pobjednik; 
    public string igracx;
    public string igraco;
    public string host;
    public Potez[] potezi;
}

[System.Serializable]
public class GameHistory
{
    public string tip_igre;
    public string igracx;
    public string igraco;
    public string pobjednik;
    public string trajanje;
}
public class HistoryResponse
{
    public GameHistory[] array;
}

[System.Serializable]
public class StavkaLjestvice
{
    public string kor_ime;
    public int pobjede;
    public int gubitci;
    public int izjednaceno;
    public int score;
    public int rang;
}
public class LeaderboardResponse
{
    public string igrac;
    public StavkaLjestvice[] ljestvica;
}

public class StatsIgraca
{
    public string kor_ime;
    public int pobjede;
    public int gubitci;
    public int izjednaceno;
}

public class LoginResponse
{
    public bool success;
    public string kor_ime;
    public bool isAdmin;
    public int admin_level;
    public string permissions;
}