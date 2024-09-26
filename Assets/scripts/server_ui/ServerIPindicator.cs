using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ServerIPindicator : MonoBehaviour
{

    private static ServerIPindicator _singleton;

    public static ServerIPindicator Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(ServerIPindicator)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
    }


    public void setIP(string ip_string)
    {
        GetComponent<TMP_Text>().text = ip_string;
    }

}
