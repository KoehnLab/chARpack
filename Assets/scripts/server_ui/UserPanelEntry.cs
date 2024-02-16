using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserPanelEntry : MonoBehaviour
{

    private bool recording = false;
    [HideInInspector] public ushort client_id;

    // set in editor
    public TextMeshProUGUI user_name_label;

    public GameObject rec_button_go;
    public Button rec_button;
    public TextMeshProUGUI rec_button_label;

    public GameObject eye_calib_visual_go;
    public Image eye_calib_visual;

    public Sprite eye_calib_icon;
    public Sprite no_eye_calib_icon;

    public GameObject battery_status_visual_go;
    public Image battery_status_visual;

    public Sprite battery_status_charging;
    public Sprite battery_status_normal;

    public GameObject battery_level_visual_go;
    public TextMeshProUGUI battery_level_label;

    public GameObject device_type_visual_go;
    public TextMeshProUGUI device_type_label;


    public void hasEyeTracking(bool value)
    {
        if (!value)
        {
            Destroy(eye_calib_visual_go);
        }
        else
        {
            eye_calib_visual_go.GetComponent<Button>().onClick.AddListener(delegate { eyeCalibPressed(); });
        }
    }

    public void hasBattery(bool value)
    {
        if (!value)
        {
            Destroy(battery_level_visual_go);
            Destroy(battery_status_visual_go);
        }
    }

    public void canRecord(bool value)
    {
        if (!value)
        {
            Destroy(rec_button_go);
        }
        else
        {
            rec_button.onClick.AddListener(delegate { recPressed(); });
        }
    }

    private void recPressed()
    {
        if (!recording)
        {
            rec_button_label.color = Color.red;
            // networking
            EventManager.Singleton.MRCapture(client_id, true);
            recording = true;
        }
        else
        {
            rec_button_label.color = Color.black;
            // networking
            EventManager.Singleton.MRCapture(client_id, false);
            recording = false;
        }
    }

    private void eyeCalibPressed()
    {
        UserServer.list[client_id].requestEyeCalibrationState();
    }

    public void updateDeviceType(myDeviceType type)
    {
        switch (type)
        {
            case myDeviceType.AR:
                device_type_label.text = "AR";
                break;
            case myDeviceType.PC:
                device_type_label.text = "PC";
                break;
            case myDeviceType.Mobile:
                device_type_label.text = "MO";
                break;
            case myDeviceType.VR:
                device_type_label.text = "VR";
                break;
            case myDeviceType.XR:
                device_type_label.text = "XR";
                break;
        }
    }

    public void updateEyeCalibrationState(bool state)
    {
        if (state)
        {
            eye_calib_visual.sprite = eye_calib_icon;
        }
        else
        {
            eye_calib_visual.sprite = no_eye_calib_icon;
        }
    }

    public void updateBatteryStaus(BatteryStatus status)
    {

        if (status == BatteryStatus.Charging)
        {
            battery_status_visual.sprite = battery_status_charging;
        }
        else
        {
            battery_status_visual.sprite = battery_status_normal;
        }
    }

    public void updateBatteryLevel(float level)
    {
        battery_level_label.text = $"{(level*100):0}%";
    }

}
