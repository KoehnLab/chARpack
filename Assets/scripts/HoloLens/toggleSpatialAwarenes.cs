using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class toggleSpatialAwarenes : MonoBehaviour
{
    public void toggle()
    {
        // Get the first Mesh Observer available, generally we have only one registered
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        if (observer.DisplayOption == SpatialAwarenessMeshDisplayOptions.None)
        {
            // Set to visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;

        } else
        {
            // Set to not visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
        }

    }
}
