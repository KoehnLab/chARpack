using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        }
    }
}
