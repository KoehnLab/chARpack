using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// This class contains the implementation of all functions of the settings menu.
/// </summary>
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
    public GameObject integrationMethodGO;
    // Indicators
    public GameObject ForceFieldIndicator;
    public GameObject HandJointIndicator;
    public GameObject HandMenuIndicator;
    public GameObject HandMeshIndicator;
    public GameObject HandRayIndicator;
    public GameObject SpatialMeshIndicator;
    public GameObject DebugWindowIndicator;
    public GameObject GazeHighlightingIndicator;
    public GameObject PointerHighlightingIndicator;
    public GameObject RightHandMenuIndicator;
    public GameObject UserBoxIndicator;
    public GameObject UserRayIndicator;

    private Color orange = new Color(1.0f, 0.5f, 0.0f);

    private void Start()
    {
        updateVisuals();
        var userBoxes = GameObject.FindGameObjectsWithTag("User Box");
        // Connected to server (not local)
        if (LoginData.ip!=null && LoginData.ip!="127.0.0.1")
        {
            setVisual(UserBoxIndicator, true);
            setVisual(UserRayIndicator, true);
        } else
        {
            setVisual(UserBoxIndicator, false);
            setVisual(UserRayIndicator, false);
        }
    }

    /// <summary>
    /// Toggles the MRTK spatial mesh on/off.
    /// The default state is off.
    /// </summary>
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

    /// <summary>
    /// Toggles the force field on/off.
    /// When the force field is off, atoms will not move toward an equilibrium by themselves
    /// and instead stay in potentially physically unreasonable positions.
    /// By default, the force field is on.
    /// </summary>
    public void toggleForceField()
    {
        ForceField.Singleton.toggleForceFieldUI();
        updateVisuals();
    }

    /// <summary>
    /// Toggles visibility of the debug log window.
    /// By default, it is not visible.
    /// </summary>
    public void toggleDebugWindow()
    {
        GlobalCtrl.Singleton.toggleDebugWindow();
        updateVisuals();
    }

    /// <summary>
    /// Increases the stiffness of atom bonds.
    /// The stiffness is an integer value between 0 and 4.
    /// The default value is 1.
    /// </summary>
    public void increaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness < 4)
        {
            ForceField.Singleton.stiffness += 1;
            SettingsData.bondStiffness = ForceField.Singleton.stiffness;
            updateVisuals();
        }
    }

    /// <summary>
    /// Decreases the stiffness of atom bonds.
    /// The stiffness is an integer value between 0 and 4.
    /// The default value is 1.
    /// </summary>
    public void decreaseBondStiffness()
    {
        if (ForceField.Singleton.stiffness > 0)
        {
            ForceField.Singleton.stiffness -= 1;
            SettingsData.bondStiffness = ForceField.Singleton.stiffness;
            updateVisuals();
        }
    }

    /// <summary>
    /// Increases the repulsion scale for force field computations by 0.1.
    /// The repulsion scale is a floating point value between 0.1 and 0.9.
    /// The default value is 0.5.
    /// </summary>
    public void increaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale < 0.9f)
        {
            ForceField.Singleton.repulsionScale += 0.1f;
            SettingsData.repulsionScale = ForceField.Singleton.repulsionScale;
            updateVisuals();
        }
    }

    /// <summary>
    /// Increases the repulsion scale for force field computations by 0.1.
    /// The repulsion scale is a floating point value between 0.1 and 0.9.
    /// The default value is 0.5.
    /// </summary>
    public void decreaseRepusionScale()
    {
        if (ForceField.Singleton.repulsionScale > 0.1f)
        {
            ForceField.Singleton.repulsionScale -= 0.1f;
            SettingsData.repulsionScale = ForceField.Singleton.repulsionScale;
            updateVisuals();
        }
    }

    /// <summary>
    /// Toggles a pointer's "enabled" behavior. If a pointer's is Default or AlwaysOn,
    /// set it to AlwaysOff. Otherwise, set the pointer's behavior to AlwaysOn.
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
            newBehavior = PointerBehavior.AlwaysOn;
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
    /// Toggles the selected locale between German and English.
    /// English is the default language.
    /// </summary>
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

    /// <summary>
    /// Toggles gaze highlighting of atoms (atoms are marked with a white outline 
    /// when a user looks at them).
    /// By default, this behaviour is active.
    /// </summary>
    public void toggleGazeHighlighting()
    {
        SettingsData.gazeHighlighting = !SettingsData.gazeHighlighting;
        updateVisuals();
    }

    /// <summary>
    /// Toggles pointer/finger highlighting of atoms (atoms are marked with a white outline 
    /// when a user moves their index finger close to it).
    /// By default, this behaviour is active.
    /// </summary>
    public void togglePointerHighlighting()
    {
        SettingsData.pointerHighlighting = !SettingsData.pointerHighlighting;
        updateVisuals();
    }

    #region Integration method
    /// <summary>
    /// Toggles visibility of the child menu containing the integration method.
    /// </summary>
    public void toggleIntegrationMethod()
    {
        GameObject integrationMethodMenu = gameObject.transform.Find("IntegrationMethod").gameObject;
        integrationMethodMenu.SetActive(!integrationMethodMenu.activeSelf);
    }

    /// <summary>
    /// Switches to the next available force field integration method.
    /// </summary>
    public void switchIntegrationMethodForward()
    {
        ForceField.Singleton.switchIntegrationMethodForward();
        updateVisuals();
    }

    /// <summary>
    /// Switches to the last available force field integration method.
    /// </summary>
    public void switchIntegrationMethodBackward()
    {
        ForceField.Singleton.switchIntegrationMethodBackward();
        updateVisuals();
    }
    #endregion

    #region Hand settings
    /// <summary>
    /// Toggles visibility of the child menu containing hand settings.
    /// </summary>
    public void toggleHandSettingsMenu()
    {
        GameObject handSettings = gameObject.transform.Find("HandSettings").gameObject;
        handSettings.SetActive(!handSettings.activeSelf);
    }

    /// <summary>
    /// Toggles hand mesh visualization.
    /// The hand mesh is active by default.
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

    /// <summary>
    /// Toggles hand joint visualization.
    /// This visualization is not active by default.
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

    /// <summary>
    /// If hand ray is AlwaysOn or Default, set it to off.
    /// Otherwise, set behavior to AlwaysOn.
    /// The hand ray is active by default.
    /// </summary>
    public void toggleHandRay()
    {
        TogglePointerEnabled<ShellHandRayPointer>(InputSourceType.Hand);
        updateVisuals();
    }

    /// <summary>
    /// Toggles activity of the hand menu.
    /// When toggled off, the hand menu cannot be accessed by the user.
    /// By default, the hand menu is active.
    /// </summary>
    public void toggleHandMenu()
    {
        GlobalCtrl.Singleton.toggleHandMenu();
        updateVisuals();
    }

    /// <summary>
    /// Toggles handedness of the hand menu.
    /// The default value corresponds to a hand menu on the left hand
    /// which is more intuitive for right-handed users.
    /// </summary>
    public void toggleMenuHandedness()
    {
        if (handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness == Handedness.Left)
        {
            handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness = Handedness.Right;
            handMenu.Singleton.setButtonPosition(Handedness.Right);
            SettingsData.rightHandMenu = true;
        }
        else if (handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness == Handedness.Right)
        {
            handMenu.Singleton.GetComponent<SolverHandler>().TrackedHandedness = Handedness.Left;
            handMenu.Singleton.setButtonPosition(Handedness.Left);
            SettingsData.rightHandMenu = false;
        }
        updateVisuals();
    }

    #endregion

    #region Cooperation settings

    /// <summary>
    /// Toggles visibility of the child menu containing cooperation settings.
    /// </summary>
    public void toggleCoopSettings()
    {
        GameObject coopSettings = gameObject.transform.Find("CoopSettings").gameObject;
        coopSettings.SetActive(!coopSettings.activeSelf);
    }

    /// <summary>
    /// Toggles the box shown around a user's head in cooperation mode.
    /// It is active by default.
    /// </summary>
    // TODO: does this have to be broadcast?
    public void toggleUserBox()
    {
        try
        {
            var userBoxes = GameObject.FindGameObjectsWithTag("User Box");
            bool active = userBoxes[0].GetComponent<MeshRenderer>().enabled;
            foreach (GameObject userBox in userBoxes)
            {
                userBox.GetComponent<MeshRenderer>().enabled = !active;
            }
            setVisual(UserBoxIndicator, !active);
        }
        catch 
        {
            setVisual(UserBoxIndicator, false);
        } // No need to do something, we are simply not in coop mode
    }

    /// <summary>
    /// Toggles the ray shown in a user's gaze direction in cooperation mode.
    /// It is active by default.
    /// </summary>
    public void toggleUserRay()
    {
        try
        {
            var userRays = GameObject.FindGameObjectsWithTag("User Box");
            bool active = userRays[0].GetComponent<LineRenderer>().enabled;
            foreach (GameObject userRay in userRays)
            {
                userRay.GetComponent<LineRenderer>().enabled = !active;
            }
            setVisual(UserRayIndicator, !active);
        } catch
        {
            setVisual(UserRayIndicator, false);
        }
    }

    #endregion

    #region Visuals
    /// <summary>
    /// Updates the numbers for bond stiffness and repulsion scale
    /// as well as the colors of the indicator fields on each toggle button.
    /// </summary>
    public void updateVisuals()
    {
        setBondStiffnessVisual(SettingsData.bondStiffness);
        setRepulsionScaleVisual(SettingsData.repulsionScale);
        setIntegrationMethodVisual(ForceField.Singleton.currentMethod);

        setVisual(HandJointIndicator, SettingsData.handJoints);
        setVisual(HandMenuIndicator, SettingsData.handMenu);
        setVisual(HandMeshIndicator, SettingsData.handMesh);
        setVisual(HandRayIndicator, SettingsData.handRay);
        setVisual(ForceFieldIndicator, SettingsData.forceField);
        setVisual(SpatialMeshIndicator, SettingsData.spatialMesh);

        if (DebugWindow.Singleton == null)
        {
            setVisual(DebugWindowIndicator, false);
        }
        else
        {
            setVisual(DebugWindowIndicator, DebugWindow.Singleton.gameObject.activeSelf);
        }

        setVisual(GazeHighlightingIndicator, SettingsData.gazeHighlighting);
        setVisual(PointerHighlightingIndicator, SettingsData.pointerHighlighting);
        setVisual(RightHandMenuIndicator, SettingsData.rightHandMenu);
    }

    /// <summary>
    /// Sets an indicator field's color corresponding to on/off.
    /// </summary>
    /// <param name="indicator">the GameObject of the indicator</param>
    /// <param name="value">whether to set the indicator to the color corresponding to on or off</param>
    public void setVisual(GameObject indicator, bool value)
    {
        if (value)
        {
            indicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            indicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    /// <summary>
    /// Sets the text of the bond stiffness field to a new number.
    /// </summary>
    /// <param name="value">the new bond stiffness</param>
    public void setBondStiffnessVisual(ushort value)
    {
        bondStiffnessValueGO.GetComponent<TextMeshPro>().text = value.ToString();
    }

    /// <summary>
    /// Sets the text of the repulsion scale field to a new number.
    /// </summary>
    /// <param name="value">the new repulsion scale</param>
    public void setRepulsionScaleVisual(float value)
    {
        repuslionScaleValueGO.GetComponent<TextMeshPro>().text = value.ToString();
    }

    public void setIntegrationMethodVisual(ForceField.Method method)
    {
        integrationMethodGO.GetComponent<TextMeshPro>().text = GlobalCtrl.Singleton.GetLocalizedString(method.ToString());
    }
    #endregion
}
