using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeBond : MonoBehaviour
{

    public GameObject distInputField;
    public GameObject kInputField;
    public GameObject okButton;

    public ForceField.BondTerm bt;

    public void initTextFields()
    {
        distInputField.GetComponent<MRTKTMPInputField>().text = bt.eqDist.ToString();
        kInputField.GetComponent<MRTKTMPInputField>().text = bt.kBond.ToString();
    }


    public void changeBondParameters()
    {
        bt.eqDist = float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        bt.kBond = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

}
