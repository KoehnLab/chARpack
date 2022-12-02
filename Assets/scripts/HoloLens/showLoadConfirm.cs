using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public GameObject loadConfirmPrefab;

    public void triggered()
    {
        var myDialog = Dialog.Open(loadConfirmPrefab, DialogButtonType.Yes | DialogButtonType.No, "Confirm File Load ", $"Are you sure you want to load: \n{m_mol_name}", true);
        if (myDialog != null)
        {
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            GlobalCtrl.Instance.LoadMolecule(m_mol_name);
        }
    }

}
