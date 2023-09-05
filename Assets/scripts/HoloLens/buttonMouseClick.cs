using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class buttonMouseClick : MonoBehaviour
{
#if !WINDOWS_UWP
    private void OnMouseDown()
    {
        GetComponent<PressableButtonHoloLens2>().ButtonPressed.Invoke();
    }
#endif
}

