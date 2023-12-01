using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class settingsControl : MonoBehaviour
{
    private static settingsControl _singleton;
    public static settingsControl Singleton
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
                Debug.Log($"[{nameof(settingsControl)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }
    private void Awake()
    {
        Singleton = this;
    }

    public void updateSettings()
    {
        setBondStiffness(SettingsData.bondStiffness);
        setForceField(SettingsData.forceField);
        setHandJoint(SettingsData.handJoints);
        setHandMenu(SettingsData.handMenu);
        setHandMesh(SettingsData.handMesh);
        setHandRay(SettingsData.handRay);
        setRepulsionScale(SettingsData.repulsionScale);
        setSpatialMesh(SettingsData.spatialMesh);
        setLanguage(SettingsData.language);
        setIntegrationMethod(SettingsData.integrationMethod);
        setInteractionMode(SettingsData.interactionMode);
        // gaze and pointer highlighting are handled by checking the value in SettingsData directly in the script
    }

    public void setForceField(bool value)
    {
        ForceField.Singleton.enableForceFieldMethod(value);
    }

    private void setBondStiffness(ushort value)
    {
        ForceField.Singleton.stiffness = value;
    }

    private void setRepulsionScale(float value)
    {
        ForceField.Singleton.repulsionScale = value;
    }

    private void setSpatialMesh(bool value)
    {
#if WINDOWS_UWP
        // Get the first Mesh Observer available, generally we have only one registered
        var observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        if (value)
        {
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.Visible;
        }
        else
        {
            observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
        }
#endif
    }

    private void setHandMesh(bool value)
    {
#if WINDOWS_UWP
        MixedRealityInputSystemProfile inputSystemProfile = CoreServices.InputSystem?.InputSystemProfile;
        if (inputSystemProfile == null)
        {
            return;
        }

        MixedRealityHandTrackingProfile handTrackingProfile = inputSystemProfile.HandTrackingProfile;
        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandMeshVisualization = value;
        }
#endif
    }

    private void setHandJoint(bool value)
    {
#if WINDOWS_UWP
        MixedRealityHandTrackingProfile handTrackingProfile = null;

        if (CoreServices.InputSystem?.InputSystemProfile != null)
        {
            handTrackingProfile = CoreServices.InputSystem.InputSystemProfile.HandTrackingProfile;
        }

        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandJointVisualization = value;
        }
#endif
    }

    private void setHandRay(bool value)
    {
#if WINDOWS_UWP
        if (value)
        {
            PointerUtils.SetPointerBehavior<ShellHandRayPointer>(PointerBehavior.Default, InputSourceType.Hand);
        }
        else
        {
            PointerUtils.SetPointerBehavior<ShellHandRayPointer>(PointerBehavior.AlwaysOff, InputSourceType.Hand);
        }
#endif
    }

    private void setHandMenu(bool value)
    {
#if WINDOWS_UWP
        handMenu.Singleton.gameObject.SetActive(value);
#endif
    }

    private void setLanguage(string lang)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(lang);
    }

    private void setIntegrationMethod(ForceField.Method method)
    {
        ForceField.Singleton.currentMethod = method;
    }

    private void setInteractionMode(GlobalCtrl.InteractionModes mode)
    {
        GlobalCtrl.Singleton.currentInteractionMode = mode;
    }
}
