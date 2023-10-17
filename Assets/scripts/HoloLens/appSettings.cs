using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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
    // Indicators
    public GameObject ForceFieldIndicator;
    public GameObject HandJointIndicator;
    public GameObject HandMenuIndicator;
    public GameObject HandMeshIndicator;
    public GameObject HandRayIndicator;
    public GameObject SpatialMeshIndicator;
    public GameObject DebugWindowIndicator;
    public GameObject GazeHighlightingIndicator;

    private Color orange = new Color(1.0f, 0.5f, 0.0f);

    private void Start()
    {
        updateVisuals();
    }

    public void updateVisuals()
    {
        setBondStiffnessVisual(SettingsData.bondStiffness);
        setForceFieldVisual(SettingsData.forceField);
        setHandJointVisual(SettingsData.handJoints);
        setHandMenuVisual(SettingsData.handMenu);
        setHandMeshVisual(SettingsData.handMesh);
        setHandRayVisual(SettingsData.handRay);
        setRepulsionScaleVisual(SettingsData.repulsionScale);
        setSpatialMeshVisual(SettingsData.spatialMesh);
        if (DebugWindow.Singleton == null)
        {
            setDebugWindowVisual(false);
        }
        else
        {
            setDebugWindowVisual(DebugWindow.Singleton.gameObject.activeSelf);
        }
        setGazeHighlightingVisual(SettingsData.gazeHighlighting);
    }

    public void toggleSpatialMesh()
    {
        // Get the first Mesh Observer available, generally we have only one registered
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        if (observer.DisplayOption == SpatialAwarenessMeshDisplayOptions.None)
        {
            // Set to visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;
            SettingsData.spatialMesh = true;
        }
        else
        {
            // Set to not visible
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
            SettingsData.spatialMesh = false;
        }
        updateVisuals();
    }

    public void setSpatialMeshVisual(bool value)
    {
        if (value)
        {
            SpatialMeshIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            SpatialMeshIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void toggleForceField()
    {
        ForceField.Singleton.toggleForceFieldUI();
        updateVisuals();
    }

    public void setForceFieldVisual(bool value)
    {
        if (value)
        {
            ForceFieldIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            ForceFieldIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void toggleDebugWindow()
    {
        GlobalCtrl.Singleton.toggleDebugWindow();
        updateVisuals();
    }

    private void setDebugWindowVisual(bool value)
    {
        if (value)
        {
            DebugWindowIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            DebugWindowIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void increaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness < 4)
        {
            ForceField.Singleton.stiffness += 1;
            SettingsData.bondStiffness = ForceField.Singleton.stiffness;
            updateVisuals();
        }
    }

    public void decreaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness > 0)
        {
            ForceField.Singleton.stiffness -= 1;
            SettingsData.bondStiffness = ForceField.Singleton.stiffness;
            updateVisuals();
        }
    }

    public void setBondStiffnessVisual(ushort value)
    {
        bondStiffnessValueGO.GetComponent<TextMeshPro>().text = value.ToString();
    }

    public void increaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale < 0.9f)
        {
            ForceField.Singleton.repulsionScale += 0.1f;
            SettingsData.repulsionScale = ForceField.Singleton.repulsionScale;
            updateVisuals();
        }
    }

    public void decreaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale > 0.1f)
        {
            ForceField.Singleton.repulsionScale -= 0.1f;
            SettingsData.repulsionScale = ForceField.Singleton.repulsionScale;
            updateVisuals();
        }
    }

    public void setRepulsionScaleVisual(float value)
    {
        repuslionScaleValueGO.GetComponent<TextMeshPro>().text = value.ToString();
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
            SettingsData.handMesh = handTrackingProfile.EnableHandMeshVisualization;
            updateVisuals();
        }
    }

    public void setHandMeshVisual(bool value)
    {
        if (value)
        {
            HandMeshIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            HandMeshIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
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
            SettingsData.handJoints = handTrackingProfile.EnableHandJointVisualization;
            updateVisuals();
        }
    }

    public void setHandJointVisual(bool value)
    {
        if (value)
        {
            HandJointIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            HandJointIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
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
            SettingsData.handRay = true;
        }
        else
        {
            newBehavior = PointerBehavior.AlwaysOff;
            SettingsData.handRay = false;
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
        updateVisuals();
    }

    public void setHandRayVisual(bool value)
    {
        if (value)
        {
            HandRayIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            HandRayIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void toggleHandMenu()
    {
        GlobalCtrl.Singleton.toggleHandMenu();
        updateVisuals();
    }

    public void setHandMenuVisual(bool value)
    {
        if (value)
        {
            HandMenuIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            HandMenuIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    // Switch languages between German and English
    public void switchLanguage()
    {
        LocaleIdentifier current = LocalizationSettings.SelectedLocale.Identifier;
        if(current == "en")
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("de");
        }
        else
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
        }
    }

    public void toggleGazeHighlighting()
    {
        GlobalCtrl.Singleton.toggleGazeHighlighting();
        updateVisuals();
    }

    public void setGazeHighlightingVisual(bool value)
    {
        if (value)
        {
            GazeHighlightingIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            GazeHighlightingIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void toggleUserBox()
    {
        var userBoxes = GameObject.FindGameObjectsWithTag("User Box");
        bool active = userBoxes[0].GetComponent<MeshRenderer>().enabled;
        foreach(GameObject userBox in userBoxes)
        {
            userBox.GetComponent<MeshRenderer>().enabled = !active;
        }
    }

    public void toggleUserRay()
    {
        var userRays = GameObject.FindGameObjectsWithTag("User Box");
        bool active = userRays[0].GetComponent<LineRenderer>().enabled;
        foreach (GameObject userRay in userRays)
        {
            userRay.GetComponent<LineRenderer>().enabled = !active;
        }
    }
}
