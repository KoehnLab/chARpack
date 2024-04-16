using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SecondaryStructureFormula : MonoBehaviour
{
    [HideInInspector]
    public int focus_id;
    public TMP_Dropdown mol_dropdown;
    public Button okButton;
    public Button cancelButton;

    private void Start()
    {
        cancelButton.onClick.AddListener(onCancel);
        okButton.onClick.AddListener(onOK);

        // fill dropdown
        List<string> mol_options = new List<string>();
        foreach (var mol_id in  StructureFormulaManager.Singleton.getMolIDs())
        {
            mol_options.Add($"{mol_id}");
        }
        mol_dropdown.AddOptions(mol_options);
    }

    private void onOK()
    {
        var mol_id = (ushort)int.Parse(mol_dropdown.options[mol_dropdown.value].text);
        StructureFormulaManager.Singleton.pushSecondaryContent(mol_id, focus_id);
        Destroy(gameObject);
    }

    private void onCancel()
    {
        Destroy(gameObject);
    }
}
