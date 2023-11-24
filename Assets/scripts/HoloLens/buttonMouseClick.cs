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

        // Avoid double pressing with mouse and finger click; special case for canvas buttons that don't have buttonConfigHelper and OnClick
        if (GameObject.FindObjectOfType(typeof(Microsoft.MixedReality.Toolkit.Input.RiggedHandVisualizer)) != null || gameObject.GetComponent<CanvasRenderer>())
        {
            GetComponent<PressableButtonHoloLens2>().ButtonPressed.Invoke();
        }
    }
#endif
}

