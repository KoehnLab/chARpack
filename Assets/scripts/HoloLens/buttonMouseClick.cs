using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This script is used to make MRTK buttons clickable with the left mouse button.
/// </summary>
public class buttonMouseClick : MonoBehaviour
{
#if !WINDOWS_UWP
    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        GetComponent<PressableButtonHoloLens2>().ButtonPressed.Invoke();
    }
#endif
}

