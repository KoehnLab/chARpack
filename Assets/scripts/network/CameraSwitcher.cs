using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{

    private static CameraSwitcher _singleton;

    public static CameraSwitcher Singleton
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
                Debug.Log($"[{nameof(CameraSwitcher)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public Dictionary<ushort, GameObject> pannel = new Dictionary<ushort, GameObject>();
    public Canvas canvas;
    private Dictionary<ushort, Camera> cameras = new Dictionary<ushort, Camera>();
    private int current_id;

    public GameObject mainCamGO;

    private Camera currentCam_;

    public  Camera currentCam {
        get { return currentCam_; }
        private set
        {
            currentCam_ = value;
            canvas.worldCamera = currentCam_;
        }
    }
    
    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        currentCam = mainCamGO.GetComponent<Camera>();
        addCamera(0, currentCam);
    }

    public void addCamera(ushort id, Camera cam)
    {

        if (cam.enabled)
        {
            cam.enabled = false;
            currentCam.enabled = true;
        }
        if (cameras.Count == 0)
        {
            currentCam.enabled = false;
            currentCam = cam;
            currentCam.enabled = true;
        }
        if (!cameras.ContainsKey(id))
        {
            // create UI entry
            if (id == 0)
            {
                var userPannelEntryPrefab = (GameObject)Resources.Load("prefabs/UserPannelEntryPrefab");
                var userPannelEntryInstace = Instantiate(userPannelEntryPrefab, UserPannel.Singleton.transform);
                userPannelEntryInstace.GetComponentInChildren<TextMeshProUGUI>().text = "ServerCamera";
                pannel.Add(id, userPannelEntryInstace);
            }
            else
            {
                var userPannelEntryPrefab = (GameObject)Resources.Load("prefabs/UserPannelEntryPrefab");
                var userPannelEntryInstace = Instantiate(userPannelEntryPrefab, UserPannel.Singleton.transform);
                userPannelEntryInstace.GetComponentInChildren<TextMeshProUGUI>().text = UserServer.list[id].deviceName;
                pannel.Add(id, userPannelEntryInstace);
            }


            cameras[id] = cam;
            current_id = id;
            if (currentCam == cam)
            {
                pannel[id].transform.Find("Background").gameObject.SetActive(true);
            }
        }
    }

    public void removeCamera(ushort id)
    {
        if (cameras.ContainsKey(id))
        {
            var to_destroy = pannel[id];
            pannel.Remove(id);
            Destroy(to_destroy);

            cameras.Remove(id);
            if (cameras.Count == 0)
            {
                currentCam = mainCamGO.GetComponent<Camera>();
                currentCam.enabled = true;
            }
            else
            {
                foreach (var cam in cameras)
                {
                    currentCam = cam.Value;
                    currentCam.enabled = true;
                    pannel[cam.Key].transform.Find("Background").gameObject.SetActive(true);
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("[CameraSwitcher] Cannot remove unregistered camera.");
        }
    }

    public void nextCam()
    {
        if (cameras.Count < 1)
        {
            // Do nothing if no client camera is registered
            return;
        }
        currentCam.enabled = false;
        pannel[(ushort)current_id].transform.Find("Background").gameObject.SetActive(false);

        current_id = (current_id + 1) % cameras.Count;
        if (current_id == 0) current_id = cameras.Count;
        int count = 1;
        foreach (var cam in cameras.Values) {
            if (current_id == count)
            {
                currentCam = cam;
            }
            count++;
        }

        currentCam.enabled = true;
        pannel[(ushort)current_id].transform.Find("Background").gameObject.SetActive(true);
    }

    public void previousCam()
    {
        if (cameras.Count < 1)
        {
            // Do nothing if no client camera is registered
            return;
        }
        currentCam.enabled = false;
        pannel[(ushort)current_id].transform.Find("Background").gameObject.SetActive(false);

        current_id = (current_id - 1)%cameras.Count;
        if (current_id == 0) current_id = cameras.Count;
        int count = 1;
        foreach (var cam in cameras.Values)
        {
            if (current_id == count)
            {
                currentCam = cam;
            }
            count++;
        }

        currentCam.enabled = true;
        pannel[(ushort)current_id].transform.Find("Background").gameObject.SetActive(true);
    }

}
