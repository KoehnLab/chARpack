using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour
{

    private static Login _singleton;

    public static Login Singleton
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
                Debug.Log($"[{nameof(Login)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    [HideInInspector] public GameObject serverListInstance;
    [HideInInspector] public GameObject serverListPrefab;

    [HideInInspector] public GameObject qrManagerPrefab;
    [HideInInspector] public GameObject qrManagerInstance;

    [HideInInspector] public GameObject stopScanButtonPrefab;
    [HideInInspector] public GameObject stopScanButtonInstance;

    private void Awake()
    {
        Singleton = this;
        serverListPrefab = (GameObject)Resources.Load("prefabs/ServerList");
        qrManagerPrefab = (GameObject)Resources.Load("prefabs/QR/QRCodesManager");
        stopScanButtonPrefab = (GameObject)Resources.Load("prefabs/QR/StopScanButton");

    }


    public void normal()
    {
        LoginData.normal_mode = true;
        SceneManager.LoadScene("MainScene");
    }

    public void host()
    {
        Debug.Log("[Login] Starting Server.");
        SceneManager.LoadScene("ServerScene");
    }

    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void showServerList()
    {
        if (serverListInstance == null)
        {
            Vector3 spawnPos = transform.position - new Vector3(1, 0, 0) * 0.1f + new Vector3(0, 1, 0) * 0.1f;
            serverListInstance = Instantiate(serverListPrefab, spawnPos, Quaternion.identity);
            gameObject.SetActive(false);
        }
    }

    public void startScanQR()
    {
        // initializes singleton
        qrManagerInstance = Instantiate(qrManagerPrefab);
        qrManagerInstance.GetComponent<QRTracking.QRCodesManager>().StartQRTracking();
        gameObject.SetActive(false);
        stopScanButtonInstance = Instantiate(stopScanButtonPrefab);
        stopScanButtonInstance.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { stopScanQR(); });

    }

    public void stopScanQR()
    {
        var qrManager = qrManagerInstance.GetComponent<QRTracking.QRCodesManager>();
        qrManager.StopQRTracking();
        Destroy(stopScanButtonInstance);
        gameObject.SetActive(true);
        // get the code list
        var objectList = qrManagerInstance.GetComponent<QRTracking.QRCodesVisualizer>().qrCodesObjectsList;
        if (objectList.Count > 0)
        {
            // we are expecting only one qr code
            foreach (var qrCodeObject in objectList.Values)
            {
                // position and rotation of QR code object is automatically updated
                LoginData.offsetPos = qrCodeObject.transform.position;
                LoginData.offsetRot = qrCodeObject.transform.rotation;
                // get first object
                break;
            }
        }

        // destroy qr code manager
        Destroy(QRTracking.QRCodesManager.Singleton.gameObject);
    }

}
