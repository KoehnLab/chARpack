using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This script provides keyboard interactions for the save dialog:
/// 'tab' switches between input fields, 'enter' confirms the input.
/// </summary>
public class saveDialogHelper : MonoBehaviour
{
    public Button okButton;
    public myInputField inputField;
    public ButtonConfigHelper closeButton;

    private void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent("return")))
        {
            okButton.onClick.Invoke();
        }
        if (Event.current.Equals(Event.KeyboardEvent("tab")))
        {
            if (EventSystem.current.currentSelectedGameObject != inputField)
            {
                inputField.Select();
            }
        }
    }
}
