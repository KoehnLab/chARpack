using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class shows the full text of a debug log message including the stack trace in a separate window.
/// </summary>
public class showStackTrace : MonoBehaviour
{
    private string m_log_message;
    public string log_message {
        get { return m_log_message; }
        set 
        { 
            m_log_message = value;
            gameObject.GetComponent<ButtonConfigHelper>().MainLabelText = m_log_message;
        }
    }

    public string stack_trace;
    public LogType log_type;

    public GameObject DialogPrefabSmall;

    public void triggered()
    {
        var dialog = Dialog.Open(DialogPrefabSmall, DialogButtonType.OK, "Message: " + m_log_message, "Stack Trace: " + stack_trace, true);
        //make sure the dialog is rotated to the camera
        dialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;
    }



}
