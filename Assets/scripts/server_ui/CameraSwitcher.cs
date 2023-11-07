using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

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

    public GameObject mainCamGO;

    private Camera currentCam_;

    public  Camera currentCam {
        get { return currentCam_; }
        private set
        {
            currentCam_ = value;
            if (canvas != null) canvas.worldCamera = currentCam_;
            foreach (var cam in cameras)
            {
                cam.Value.tag = "Untagged";
                cam.Value.enabled = false;
            }
            currentCam.tag = "MainCamera";
            currentCam.enabled = true;
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

    /// <summary>
    /// Registers a camera from a new user to the available cameras.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cam"></param>
    public void addCamera(ushort id, Camera cam)
    {

        if (cam.enabled)
        {
            cam.enabled = false;
            currentCam.enabled = true;
        }
        if (cameras.Count == 0)
        {
            currentCam = cam;
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
            if (currentCam == cam)
            {
                pannel[id].transform.Find("Background").gameObject.SetActive(true);
            }
        }
        GlobalCtrl.Singleton.currentCamera = currentCam;
    }

    /// <summary>
    /// Removes a camera from the lsit of registered cameras.
    /// </summary>
    /// <param name="id"></param>
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
                    if (cam.Value == null) continue;
                    currentCam = cam.Value;
                    pannel[cam.Key]?.transform.Find("Background").gameObject.SetActive(true);
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("[CameraSwitcher] Cannot remove unregistered camera.");
        }
        GlobalCtrl.Singleton.currentCamera = currentCam;
    }

    /// <summary>
    /// Switches to the view from the next registered user camera.
    /// </summary>
    public void nextCam()
    {
        if (cameras.Count < 1)
        {
            // Do nothing if no client camera is registered
            return;
        }
        var user_id = cameras.FirstOrDefault(x => x.Value == currentCam).Key;
        pannel[user_id].transform.Find("Background").gameObject.SetActive(false);

        var cams_as_list = cameras.Values.ToList();
        var id_in_list = cams_as_list.IndexOf(currentCam);
        currentCam = cameras.Values.ToList().getWrapElement(id_in_list+1);

        var new_user_id = cameras.FirstOrDefault(x => x.Value == currentCam).Key;
        pannel[new_user_id].transform.Find("Background").gameObject.SetActive(true);
        GlobalCtrl.Singleton.currentCamera = currentCam;
    }

    /// <summary>
    /// Switches to the view from the previous registered user camera.
    /// </summary>
    public void previousCam()
    {
        if (cameras.Count < 1)
        {
            // Do nothing if no client camera is registered
            return;
        }
        var user_id = cameras.FirstOrDefault(x => x.Value == currentCam).Key;
        pannel[user_id].transform.Find("Background").gameObject.SetActive(false);

        var cams_as_list = cameras.Values.ToList();
        var id_in_list = cams_as_list.IndexOf(currentCam);
        currentCam = cameras.Values.ToList().getWrapElement(id_in_list - 1);

        var new_user_id = cameras.FirstOrDefault(x => x.Value == currentCam).Key;
        pannel[new_user_id].transform.Find("Background").gameObject.SetActive(true);
        GlobalCtrl.Singleton.currentCamera = currentCam;
    }

}
