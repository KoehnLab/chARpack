using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script hides the Load/Save window upon opening the load confirm dialog.
/// </summary>
public class hideLoadWindowOnConfirm : MonoBehaviour
{
    private GameObject loadConfirmGO;

    // become active
    void OnEnable()
    {
        if (loadConfirmGO != null)
        {
            loadConfirmGO = FindObjectOfType<loadSaveWindow>().gameObject;
            loadConfirmGO.SetActive(false);
        }
    }

    // get deactivated
    void OnDisable()
    {
        if (loadConfirmGO != null)
        {
            loadConfirmGO.SetActive(true);
            loadConfirmGO.GetComponent<loadSaveWindow>().updateClipping();
        }
    }
}
