using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanCornerInstructions : MonoBehaviour
{

    private static ScanCornerInstructions _singleton;

    public static ScanCornerInstructions Singleton
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
                Debug.Log($"[{nameof(ScanCornerInstructions)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public void Awake()
    {
        Singleton = this;
    }

    public delegate void ScreenScanCancelAction();
    public event ScreenScanCancelAction OnScreenScanCancel;

    public delegate void ScreenScanStartAction();
    public event ScreenScanStartAction OnScreenScanStart;

    public void cancel()
    {
        OnScreenScanCancel?.Invoke();
        gameObject.SetActive(false);
    }

    public void startScan()
    {
        OnScreenScanStart?.Invoke();
        gameObject.SetActive(false);
    }
}
