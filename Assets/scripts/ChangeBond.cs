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

    public void reloadTextFieldsBT()
    {
        angleOrDistLabel.GetComponent<TextMeshProUGUI>().text = angleOrDistLabel.GetComponent<TextMeshProUGUI>().text.TrimEnd(new char[] { ' ', '(', ')', '\u00C5', 'p', 'm' });
        initTextFieldsBT();
    }

    private void initTextFieldsBT()
    {
        angleOrDistLabel.GetComponent<TextMeshProUGUI>().text += SettingsData.useAngstrom ? " (\u00C5)" : " (pm)";
        var text = SettingsData.useAngstrom ? (bt.eqDist * 0.01f).ToString() : bt.eqDist.ToString();
        distInputField.GetComponent<MRTKTMPInputField>().text = text;
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

    /// <summary>
    /// Changes the bond parameters of a single bond 
    /// according to the text input.
    /// </summary>
    public void changeBondParametersBT()
    {
        bt_.eqDist = SettingsData.useAngstrom ? float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text)*100 : float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        bt_.kBond = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

    /// <summary>
    /// Changes the bond parameters of an angle bond 
    /// according to the text input.
    /// </summary>
    public void changeBondParametersAT()
    {
        at_.eqAngle = float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        at_.kAngle = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

    /// <summary>
    /// Changes the bond parameters of a torsion bond 
    /// according to the text input.
    /// </summary>
    public void changeBondParametersTT()
    {
        tt_.eqAngle = float.Parse(distInputField.GetComponent<MRTKTMPInputField>().text);
        tt_.vk = float.Parse(kInputField.GetComponent<MRTKTMPInputField>().text);
    }

    // Keyboard interactions
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
