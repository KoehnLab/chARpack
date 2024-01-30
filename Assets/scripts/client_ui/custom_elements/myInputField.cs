using UnityEngine;
using UnityEngine.EventSystems;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class myInputField : MRTKTMPInputField
{
    public override void OnDeselect(BaseEventData eventData)
    {
        return;
        //base.OnDeselect(eventData);
    }

}
