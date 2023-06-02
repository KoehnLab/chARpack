using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class appSettings : MonoBehaviour
{

    private static appSettings _singleton;
    public static appSettings Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"[{nameof(appSettings)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }
    private void Awake()
    {
        Singleton = this;
    }

    public GameObject bondStiffnessValueGO;
    public GameObject repuslionScaleValueGO;
    private void Start()
    {
        bondStiffnessValueGO.GetComponent<TextMeshPro>().text = ForceField.Singleton.stiffness.ToString();
        repuslionScaleValueGO.GetComponent<TextMeshPro>().text = ForceField.Singleton.repulsionScale.ToString();
    }


    public void toggleSpatialMesh()
    {
        // Get the first Mesh Observer available, generally we have only one registered
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        if (observer.DisplayOption == SpatialAwarenessMeshDisplayOptions.None)
        {
            // Set to visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;

        }
        else
        {
            // Set to not visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
        }

    }

    public void toggleForceField()
    {
        ForceField.Singleton.toggleForceFieldUI();
    }

    public void toggleDebugWindow()
    {
        GlobalCtrl.Singleton.toggleDebugWindow();
    }


    public void increaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness < 4)
        {
            ForceField.Singleton.stiffness += 1;
            bondStiffnessValueGO.GetComponent<TextMeshPro>().text = ForceField.Singleton.stiffness.ToString();
        }
    }

    public void decreaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness > 0)
        {
            ForceField.Singleton.stiffness -= 1;
            bondStiffnessValueGO.GetComponent<TextMeshPro>().text = ForceField.Singleton.stiffness.ToString();
        }
    }

    public void increaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale < 0.9f)
        {
            ForceField.Singleton.repulsionScale += 0.1f;
            repuslionScaleValueGO.GetComponent<TextMeshPro>().text = ForceField.Singleton.repulsionScale.ToString();
        }
    }

    public void decreaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale > 0.1f)
        {
            ForceField.Singleton.repulsionScale -= 0.1f;
            repuslionScaleValueGO.GetComponent<TextMeshPro>().text = ForceField.Singleton.repulsionScale.ToString();
        }
    }

    /// <summary>
    /// Toggles hand mesh visualization
    /// </summary>
    public void toggleHandMesh()
    {
        MixedRealityInputSystemProfile inputSystemProfile = CoreServices.InputSystem?.InputSystemProfile;
        if (inputSystemProfile == null)
        {
            return;
        }

        MixedRealityHandTrackingProfile handTrackingProfile = inputSystemProfile.HandTrackingProfile;
        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandMeshVisualization = !handTrackingProfile.EnableHandMeshVisualization;
        }
    }

    /// <summary>
    /// Toggles hand joint visualization
    /// </summary>
    public void toggleHandJoint()
    {
        MixedRealityHandTrackingProfile handTrackingProfile = null;

        if (CoreServices.InputSystem?.InputSystemProfile != null)
        {
            handTrackingProfile = CoreServices.InputSystem.InputSystemProfile.HandTrackingProfile;
        }

        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandJointVisualization = !handTrackingProfile.EnableHandJointVisualization;
        }
    }

    /// <summary>
    /// Toggles a pointer's "enabled" behavior. If a pointer's is Default or AlwaysOn,
    /// set it to AlwaysOff. Otherwise, set the pointer's behavior to Default.
    /// Will set this state for all matching pointers.
    /// </summary>
    /// <typeparam name="T">Type of pointer to set</typeparam>
    /// <param name="inputType">Input type of pointer to set</param>
    public void TogglePointerEnabled<T>(InputSourceType inputType) where T : class, IMixedRealityPointer
    {
        PointerBehavior oldBehavior = PointerUtils.GetPointerBehavior<T>(Handedness.Any, inputType);
        PointerBehavior newBehavior;
        if (oldBehavior == PointerBehavior.AlwaysOff)
        {
            newBehavior = PointerBehavior.Default;
        }
        else
        {
            newBehavior = PointerBehavior.AlwaysOff;
        }
        PointerUtils.SetPointerBehavior<T>(newBehavior, inputType);
    }

    /// <summary>
    /// If hand ray is AlwaysOn or Default, set it to off.
    /// Otherwise, set behavior to default
    /// </summary>
    public void toggleHandRay()
    {
        TogglePointerEnabled<ShellHandRayPointer>(InputSourceType.Hand);
    }
}
