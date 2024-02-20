using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    [HideInInspector] public GameObject labelPrefab;

    private Transform cam;

    public GameObject toggleDebugButton;

    private void Awake()
    {
        Singleton = this;
        serverListPrefab = (GameObject)Resources.Load("prefabs/ServerList");
        qrManagerPrefab = (GameObject)Resources.Load("prefabs/QR/QRCodesManager");
        stopScanButtonPrefab = (GameObject)Resources.Load("prefabs/QR/StopScanButton");
        anchorPrefab = (GameObject)Resources.Load("prefabs/QR/QRAnchor");
        labelPrefab = (GameObject)Resources.Load("prefabs/3DLabelPrefab");

        cam = Camera.main.transform;

        GameObject debugWindow = Instantiate((GameObject)Resources.Load("prefabs/DebugWindow"));
        debugWindow.SetActive(false);
        toggleDebugButton.GetComponent<Button>().onClick.AddListener(delegate { DebugWindow.Singleton.toggleVisible(); });
        DebugWindow.Singleton.debugIndicator = toggleDebugButton.transform.Find("IconAndText/Indicator").gameObject;

    }

    /// <summary>
    /// Loads normal mode.
    /// </summary>
    public void normal()
    {
        LoginData.normal_mode = true;
        SceneManager.LoadScene("MainScene");
    }

    /// <summary>
    /// Loads the server scene.
    /// </summary>
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

    /// <summary>
    /// Spawns an instance of the server list facing the camera.
    /// </summary>
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

    public void startQRScanTimer()
    {
        var num_reads = 1000;

        Debug.Log("[Login:QR] Starting scan with timer.");
        qrManagerInstance = Instantiate(qrManagerPrefab);
        if (qrManagerInstance == null)
        {
            qrManagerInstance = QRTracking.QRCodesManager.Singleton.gameObject;
        }
        qrManagerInstance.GetComponent<QRTracking.QRCodesManager>().StartQRTracking();
        gameObject.SetActive(false);
        var scanProgressLabel_go = Instantiate(labelPrefab);
        var scanProgressLabel = scanProgressLabel_go.GetComponent<TextMeshPro>();
        scanProgressLabel.text = $"0/{num_reads}";

        var pos_list = new List<Vector3>();
        var quat_list = new List<Quaternion>();

        while (pos_list.Count < num_reads)
        {
            var objectList = qrManagerInstance.GetComponent<QRTracking.QRCodesVisualizer>().qrCodesObjectsList;
            if (objectList == null) continue;
            if (objectList.Count > 0)
            {
                // make it simple if code list is just 1
                if (objectList.Count == 1)
                {
                    var e = objectList.FirstOrDefault().Value;
                    pos_list.Add(e.transform.position);
                    quat_list.Add(e.transform.rotation);
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
                        pos_list.Add(lastObj.transform.position);
                        quat_list.Add(lastObj.transform.rotation);
                        Debug.Log("[Login:QR] Taking last updated QR code.");
                    }
                    else
                    {
                        var e = objectList.LastOrDefault().Value;
                        pos_list.Add(e.transform.position);
                        quat_list.Add(e.transform.rotation);
                        Debug.Log("[Login:QR] Last updated QR code not available. Taking last in list.");
                    }
                }
            }

            // set label
            if (pos_list.Count > 0)
            {
                scanProgressLabel_go.transform.position = pos_list.Last();
            }
            scanProgressLabel_go.transform.forward = cam.forward;
            scanProgressLabel.text = $"{pos_list.Count}/{num_reads}";
        }

        qrManagerInstance.GetComponent<QRTracking.QRCodesManager>().StopQRTracking();


        int num_values = pos_list.Count / 2;

        var final_pos = pos_list[num_values];
        var final_quat = quat_list[num_values];

        for (int j = num_values + 1; j < pos_list.Count; j++)
        {
            final_pos += pos_list[j];
            final_quat *= quat_list[j];
        }

        final_pos /= num_values;

        LoginData.offsetPos = final_pos;
        LoginData.offsetRot = final_quat;

        if (QRAnchor.Singleton == null)
        {
            Instantiate(anchorPrefab);
        }
        QRAnchor.Singleton.transform.position = LoginData.offsetPos;
        QRAnchor.Singleton.transform.rotation = LoginData.offsetRot;


        // destroy qr code manager
        Destroy(QRTracking.QRCodesManager.Singleton.gameObject);
        // destroy progess label
        Destroy(scanProgressLabel_go);
        // show login menu
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Starts scanning of a QR code, spawns stop scan button.
    /// </summary>
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

    /// <summary>
    /// Ends scanning the QR code; sets appropriate offsets
    /// for commmon coordinate system if the scan was successful.
    /// </summary>
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
