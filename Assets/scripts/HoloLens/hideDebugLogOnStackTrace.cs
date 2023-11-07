using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script hides the debug window upon showing the stack trace of a specific message.
/// </summary>
public class hideDebugLogOnStackTrace : MonoBehaviour
{
    private GameObject debugLogGO;

    // become active
    void OnEnable()
    {
        debugLogGO = FindObjectOfType<DebugWindow>().gameObject;
        debugLogGO.SetActive(false);
    }

    // get deactivated
    void OnDisable()
    {
        if (debugLogGO != null)
        {
            debugLogGO.SetActive(true);
            debugLogGO.GetComponent<DebugWindow>().updateClipping();
        }
    }
}
