using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace chARpack
{
    public class GazeHighlightAtoms : MonoBehaviour
    {
        Atom lastAtom = null;
        private void Update()
        {
            var eyeGazeProvider = CoreServices.InputSystem?.EyeGazeProvider;
            if (eyeGazeProvider != null && SettingsData.gazeHighlighting)
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
                            lastAtom.proccessFocusUI(false);
                            atom.proccessFocusUI(true);
                            lastAtom = atom;
                        }
                    }
                }
                else
                {
                    lastAtom.proccessFocusUI(false);
                }
            }
        }
    }
}
