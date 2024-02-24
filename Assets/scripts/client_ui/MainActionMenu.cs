using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainActionMenu : MonoBehaviour
{
    private static MainActionMenu _singleton;

    public static MainActionMenu Singleton
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
                Debug.Log($"[{nameof(MainActionMenu)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    private void Awake()
    {
        Singleton = this;
    }
}

