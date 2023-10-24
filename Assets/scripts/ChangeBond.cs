using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChangeBond : MonoBehaviour
{

    public GameObject distInputField;
    public GameObject kInputField;
    public GameObject okButton;
    public GameObject angleOrDistLabel;
    public GameObject kLabel;

    private ForceField.BondTerm bt_;
    private ForceField.AngleTerm at_;
    private ForceField.TorsionTerm tt_;

    public ForceField.BondTerm bt { get => bt_; set { bt_ = value; initTextFieldsBT(); } }
    public ForceField.AngleTerm at { get => at_; set { at_ = value; initTextFieldsAT(); } }
    public ForceField.TorsionTerm tt { get => tt_; set { tt_ = value; initTextFieldsTT(); } }

    private void initTextFieldsBT()
    {
        distInputField.GetComponent<MRTKTMPInputField>().text = bt.eqDist.ToString();
        kInputField.GetComponent<MRTKTMPInputField>().text = bt.kBond.ToString();
    }

    private void initTextFieldsAT()
    {
        angleOrDistLabel.GetComponent<TextMeshProUGUI>().text = "Equilibrium Angle";
        kLabel.GetComponent<TextMeshProUGUI>().text = "kAngle";
        distInputField.GetComponent<MRTKTMPInputField>().text = at.eqAngle.ToString();
        kInputField.GetComponent<MRTKTMPInputField>().text = at.kAngle.ToString();
    }

    private void initTextFieldsTT()
    {
        angleOrDistLabel.GetComponent<TextMeshProUGUI>().text = "Equilibrium Angle";
        kLabel.GetComponent<TextMeshProUGUI>().text = "vk";
        distInputField.GetComponent<MRTKTMPInputField>().text = tt.eqAngle.ToString();
        kInputField.GetComponent<MRTKTMPInputField>().text = tt.vk.ToString();
    }


    public void changeBondParametersBT()
    {
        bt_.eqDist = float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        bt_.kBond = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

    public void changeBondParametersAT()
    {
        at_.eqAngle = float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        at_.kAngle = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

    public void changeBondParametersTT()
    {
        tt_.eqAngle = float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        tt_.vk = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

    void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent("return")))
        {
            okButton.GetComponent<Button>().onClick.Invoke();
        }
        if (Event.current.Equals(Event.KeyboardEvent("tab")))
        {
            if(EventSystem.current.currentSelectedGameObject == distInputField)
            {
                kInputField.GetComponent<myInputField>().Select();
                // Deactivate other input field so there aren't two blinking carets at the same time
                distInputField.GetComponent<myInputField>().DeactivateInputField();
            } else
            {
                distInputField.GetComponent<myInputField>().Select();
                kInputField.GetComponent<myInputField>().DeactivateInputField();
            }
        }
    }
}
