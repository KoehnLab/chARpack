using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;

public class NetworkManagerLogin : MonoBehaviour
{
    private static NetworkManagerLogin _singleton;
    public static NetworkManagerLogin Singleton
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
                Debug.Log($"{nameof(NetworkManagerLogin)} instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }


    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        // Check for open servers

        
    }

    public void refresh()
    {

    }

    public void close()
    {
        Login.Singleton.gameObject.SetActive(true);
        Destroy(gameObject);
    }

}
