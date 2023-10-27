using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class saveDialog : MonoBehaviour
{
    public GameObject okButton;
    public GameObject inputField;

    private void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent("return")))
        {
            okButton.GetComponent<Button>().onClick.Invoke();
        }
        if (Event.current.Equals(Event.KeyboardEvent("tab")))
        {
            if (EventSystem.current.currentSelectedGameObject != inputField)
            {
                inputField.GetComponent<myInputField>().Select();
            }
        }
    }
}
