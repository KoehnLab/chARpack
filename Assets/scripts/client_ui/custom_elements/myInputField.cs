using UnityEngine;
using UnityEngine.EventSystems;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine.UI;
using System;

public class myInputField : MRTKTMPInputField
{
    TouchScreenKeyboard keyboard;

#if WINDOWS_UWP
    public override void OnDeselect(BaseEventData eventData)
    {
        return;
        //base.OnDeselect(eventData);
    }
#endif

#if !WINDOWS_UWP
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        SendOnFocus();

        ActivateInputField();

        keyboard = TouchScreenKeyboard.Open(text);
    }
#endif

}
