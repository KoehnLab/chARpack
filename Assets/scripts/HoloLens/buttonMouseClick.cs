using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.EventSystems;

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

