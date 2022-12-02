using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doTheSave : MonoBehaviour
{

    public GameObject inputField;
    public GameObject errorDialogPrefab;
    public GameObject saveDialog;

    public void performSave()
    {
        var name = inputField.GetComponent<MRTKTMPInputField>().text;
        if (name == "" || name == null)
        {
            saveDialog.SetActive(false);
            var myDialog = Dialog.Open(errorDialogPrefab, DialogButtonType.OK, "Error", "Please enter file name", true);
            if (myDialog != null)
            {
                myDialog.OnClosed += OnClosedDialogEvent;
            }
            return;
        }
        GlobalCtrl.Instance.SaveMolecule(0,name);
        Destroy(saveDialog);
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.OK)
        {
            saveDialog.SetActive(true);
        }
    }

}
