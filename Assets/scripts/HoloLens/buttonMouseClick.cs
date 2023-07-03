using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class buttonMouseClick : MonoBehaviour
{
#if !WINDOWS_UWP
    private void OnMouseDown()
    {
        GetComponent<PressableButtonHoloLens2>().ButtonPressed.Invoke();
    }
#endif
}

