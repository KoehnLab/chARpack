using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tis class provides the user interaction for saving a molecule to a file.
/// </summary>
public class doTheSave : MonoBehaviour
{

    public GameObject inputField;
    public GameObject errorDialogPrefab;
    public GameObject saveDialog;

    /// <summary>
    /// Attempts to save the molecule to the given file name.
    /// If the file name was invalid, displays an error message.
    /// </summary>
    public void performSave()
    {
        var name = inputField.GetComponent<MRTKTMPInputField>().text;
        if (name == "" || name == null)
        {
            saveDialog.SetActive(false);
            var myDialog = Dialog.Open(errorDialogPrefab, DialogButtonType.OK, "Error", "Please enter file name", true);
            //make sure the dialog is rotated to the camera
            myDialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;

            if (myDialog != null)
            {
                myDialog.OnClosed += OnClosedDialogEvent;
            }
            return;
        }
        GlobalCtrl.Singleton.SaveMolecule(false,name);
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
