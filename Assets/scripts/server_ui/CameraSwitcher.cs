using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

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

    public Dictionary<ushort, GameObject> panel = new Dictionary<ushort, GameObject>();
    public Canvas canvas;
    private Dictionary<ushort, Camera> cameras = new Dictionary<ushort, Camera>();

    public GameObject mainCamGO;
    private GameObject userPanelEntryPrefab;

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
        userPanelEntryPrefab = (GameObject)Resources.Load("prefabs/UserPanelEntryPrefab");
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
            var userPanelEntryInstace = Instantiate(userPanelEntryPrefab, UserPanel.Singleton.transform);
            var user_panel_entry = userPanelEntryInstace.GetComponent<UserPanelEntry>();
            user_panel_entry.client_id = id;

            user_panel_entry.user_name_label.text = id == 0 ? "ServerCamera" : UserServer.list[id].deviceName;

            var device_type = id == 0 ? myDeviceType.PC : UserServer.list[id].deviceType;

            if (device_type == myDeviceType.PC)
            {
                user_panel_entry.canRecord(false);
                user_panel_entry.hasEyeTracking(false);
                user_panel_entry.hasBattery(false);
            }
            else if (device_type == myDeviceType.AR)
            {
                user_panel_entry.canRecord(true);
                user_panel_entry.hasEyeTracking(true);
                user_panel_entry.hasBattery(true);
            }
            else if (device_type == myDeviceType.XR)
            {
                user_panel_entry.canRecord(false);
                user_panel_entry.hasEyeTracking(false);
                user_panel_entry.hasBattery(true);
            }

            panel.Add(id, userPanelEntryInstace);

            cameras[id] = cam;
            if (currentCam == cam)
            {
                panel[id].transform.Find("Background").gameObject.SetActive(true);
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
            var to_destroy = panel[id];
            panel.Remove(id);
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
                    panel[cam.Key]?.transform.Find("Background").gameObject.SetActive(true);
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
        panel[user_id].transform.Find("Background").gameObject.SetActive(false);

        var cams_as_list = cameras.Values.ToList();
        var id_in_list = cams_as_list.IndexOf(currentCam);
        currentCam = cameras.Values.ToList().getWrapElement(id_in_list+1);

        var new_user_id = cameras.FirstOrDefault(x => x.Value == currentCam).Key;
        panel[new_user_id].transform.Find("Background").gameObject.SetActive(true);
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
        panel[user_id].transform.Find("Background").gameObject.SetActive(false);

        var cams_as_list = cameras.Values.ToList();
        var id_in_list = cams_as_list.IndexOf(currentCam);
        currentCam = cameras.Values.ToList().getWrapElement(id_in_list - 1);

        var new_user_id = cameras.FirstOrDefault(x => x.Value == currentCam).Key;
        panel[new_user_id].transform.Find("Background").gameObject.SetActive(true);
        GlobalCtrl.Singleton.currentCamera = currentCam;
    }

}
