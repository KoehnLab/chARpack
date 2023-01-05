using System.Collections;
using System.Collections.Generic;
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

    public Canvas canvas;
    private Dictionary<ushort, Camera> cameras = new Dictionary<ushort, Camera>();
    private int current_id;
    private Camera mainCam;

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
        mainCam = Camera.main;
    }

    private void Start()
    {
        currentCam = mainCam;
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
            cameras[id] = cam;
        }
    }

    public void removeCamera(ushort id)
    {
        if (cameras.ContainsKey(id))
        {
            if (cameras.Count == 1)
            {
                cameras[id].enabled = false;
                currentCam = mainCam;
                currentCam.enabled = true;
            }
            else
            {
                nextCam();
            }
            cameras.Remove(id);
        }
        else
        {
            Debug.LogError("[CameraSwitcher] Cannot remove unregistered camera.");
        }
    }

    public void nextCam()
    {
        currentCam.enabled = false;

        current_id = (current_id + 1) % cameras.Count;
        int count = 0;
        foreach (var cam in cameras.Values) {
            if (current_id == count)
            {
                currentCam = cam;
            }
            count++;
        }

        currentCam.enabled = true;
    }

    public void previousCam()
    {
        currentCam.enabled = false;

        current_id = (cameras.Count+ current_id - 1)%cameras.Count;
        int count = 0;
        foreach (var cam in cameras.Values)
        {
            if (current_id == count)
            {
                currentCam = cam;
            }
            count++;
        }

        currentCam.enabled = true;
    }
}
