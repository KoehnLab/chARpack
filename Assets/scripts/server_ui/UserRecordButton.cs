using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UserRecordButton : MonoBehaviour
{
    private bool recording = false;
    public ushort client_id;
    TextMeshProUGUI button_label;

    private void Start()
    {
        var rec_button = transform.Find("RecButton");
        rec_button.GetComponent<Button>().onClick.AddListener(delegate { recPressed(); });
        button_label = rec_button.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void recPressed()
    {
        if (!recording)
        {
            button_label.color = Color.red;
            // networking
            EventManager.Singleton.MRCapture(client_id, true);
            recording = true;
        }
        else
        {
            button_label.color = Color.black;
            // networking
            EventManager.Singleton.MRCapture(client_id, false);
            recording = false;
        }


    }
}
