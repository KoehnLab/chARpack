using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class GazeHighlightAtoms : MonoBehaviour
{
    Atom lastAtom = null;
    private void Update()
    {
        var eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
        if (eyeGazeProvider != null)
        {
            EyeTrackingTarget lookedAtEyeTarget = EyeTrackingTarget.LookedAtEyeTarget;

            // Update GameObject to the current eye gaze position at a given distance
            if (lookedAtEyeTarget != null)
            {
                Debug.Log("[GazeHighlightAtoms] OBJECT HIT");
                // check if object is atom
                Atom atom = lookedAtEyeTarget.GetComponent<Atom>();
                if (atom != null)
                {
                    if (atom != lastAtom)
                    {
                        lastAtom.focusHighlight(false);
                        atom.focusHighlight(true);
                        lastAtom = atom;
                    }
                }
            }
        }
    }
}

