using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QRAnchor : MonoBehaviour
{
    private static QRAnchor _singleton;

    public static QRAnchor Singleton
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
                Debug.Log($"[{nameof(QRAnchor)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }
}
