using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script shows a prompt asking the user to confirm the requested load operation.
/// </summary>
public class showLoadConfirm : MonoBehaviour
{
    private string m_mol_name;
    public string mol_name
    {
        get { return m_mol_name; }
        set
        {
            m_mol_name = value;
            GetComponent<ButtonConfigHelper>().MainLabelText = m_mol_name;
        }
    }

    private GameObject dialogPrefab;

    private void Start()
    {
        dialogPrefab = (GameObject)Resources.Load("prefabs/confirmDialog");
    }

    public void triggered()
    {
        loadSaveWindow.Singleton.gameObject.SetActive(false);
        var myDialog = Dialog.Open(dialogPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm File Load ", $"Are you sure you want to load: \n{m_mol_name}", true);
        //make sure the dialog is rotated to the camera
        myDialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;
        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            EventManager.Singleton.DeviceLoadMolecule(m_mol_name);
            GlobalCtrl.Singleton.LoadMolecule(m_mol_name);
        }
        loadSaveWindow.Singleton.gameObject.SetActive(true);
    }

}
