using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hideLoadWindowOnConfirm : MonoBehaviour
{
    private GameObject loadConfirmGO;

    // become active
    void OnEnable()
    {
        loadConfirmGO = FindObjectOfType<loadSaveWindow>().gameObject;
        loadConfirmGO.SetActive(false);
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
