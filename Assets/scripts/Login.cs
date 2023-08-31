using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

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

    [HideInInspector] public GameObject anchorPrefab;

    private void Awake()
    {
        Singleton = this;
        serverListPrefab = (GameObject)Resources.Load("prefabs/ServerList");
        qrManagerPrefab = (GameObject)Resources.Load("prefabs/QR/QRCodesManager");
        stopScanButtonPrefab = (GameObject)Resources.Load("prefabs/QR/StopScanButton");
        anchorPrefab = (GameObject)Resources.Load("prefabs/QR/QRAnchor");

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
            //make sure the window is rotated to the camera
            serverListInstance.transform.forward = Camera.main.transform.forward;
            gameObject.SetActive(false);
        }
    }

    public void startScanQR()
    {
        // initializes singleton
        Debug.Log("[Login:QR] Starting scan.");
        qrManagerInstance = Instantiate(qrManagerPrefab);
        if (qrManagerInstance == null)
        {
            qrManagerInstance = QRTracking.QRCodesManager.Singleton.gameObject;
        }
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
        //var codesList = qrManagerInstance.GetComponent<QRTracking.QRCodesManager>().qrCodesList;
        var objectList = qrManagerInstance.GetComponent<QRTracking.QRCodesVisualizer>().qrCodesObjectsList;
        if (objectList.Count > 0)
        {
            // make it simple if code list is just 1
            if (objectList.Count == 1)
            {
                var e = objectList.FirstOrDefault().Value;
                LoginData.offsetPos = e.transform.position;
                LoginData.offsetRot = e.transform.rotation;
                Debug.Log("[Login:QR] Only one QR Code in list.");
            }
            else
            {
                Debug.Log("[Login:QR] Checking for last updated QR code.");
                // lets check for the last updated QR code
                var now = System.DateTimeOffset.Now;
                GameObject lastObj = null;
                var minDiff = System.TimeSpan.MaxValue;
                foreach (var obj in objectList.Values)
                {
                    var code = obj.GetComponent<QRTracking.QRCode>().qrCode;
                    // calc offset
                    var diff = now.Subtract(code.LastDetectedTime);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        lastObj = obj;
                    }
                }
                // position and rotation of QR code object
                if (lastObj != null)
                {
                    LoginData.offsetPos = lastObj.transform.position;
                    LoginData.offsetRot = lastObj.transform.rotation;
                    Debug.Log("[Login:QR] Taking last updated QR code.");
                }
                else
                {
                    var e = objectList.LastOrDefault().Value;
                    LoginData.offsetPos = e.transform.position;
                    LoginData.offsetRot = e.transform.rotation;
                    Debug.Log("[Login:QR] Last updated QR code not available. Taking last in list.");
                }
            }
            if (QRAnchor.Singleton == null)
            {
                Instantiate(anchorPrefab);
            }
            QRAnchor.Singleton.transform.position = LoginData.offsetPos;
            QRAnchor.Singleton.transform.rotation = LoginData.offsetRot;
        }
        else
        {
            Debug.LogError("[Login:QR] Scan unsuccessful. Please try again.");
        }

        // destroy qr code manager
        Destroy(QRTracking.QRCodesManager.Singleton.gameObject);
    }

}
