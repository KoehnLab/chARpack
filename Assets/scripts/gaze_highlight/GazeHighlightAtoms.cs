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
                        var focus_id = FocusManager.getMyFocusID();
                        if (focus_id > 0)
                        {
                            lastAtom.focused[focus_id] = false;
                            atom.focused[focus_id] = true;
                            lastAtom = atom;
                        }
                    }
                }
            }
        }
    }
}

