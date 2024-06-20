using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using chARpackColorPalette;
using UnityEngine.XR.ARFoundation;

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
        try
        {
            DebugWindow.Singleton.debugIndicator = DebugWindowIndicator;
        } catch { }
    }

    public GameObject bondStiffnessValueGO;
    public GameObject repuslionScaleValueGO;
    public GameObject integrationMethodGO;
    public GameObject colorPaletteGO;
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
    public GameObject NetworkMeasurementIndicator;
    public GameObject ColorInterpolationIndicator;
    public GameObject LicoriceRenderingIndicator;
    public GameObject VideoPassThroughIndicator;
    // Time factor sliders
    public GameObject EulerTimeFactorSlider;
    public GameObject SVTimeFactorSlider;
    public GameObject RKTimeFactorSlider;
    public GameObject MPTimeFactorSlider;

    public GameObject LengthUnitLabel;

    private void Start()
    {
        initTimeFactors();
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

    #region General
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
        if (ForceField.Singleton != null)
        {
            ForceField.Singleton.toggleForceFieldUI();
        }
        else
        {
            SettingsData.forceField = !SettingsData.forceField;
        }
        updateVisuals();
    }

    /// <summary>
    /// Toggles visibility of the debug log window.
    /// By default, it is not visible.
    /// </summary>
    public void toggleDebugWindow()
    {
        DebugWindow.Singleton.toggleVisible();
        updateVisuals();
    }

    /// <summary>
    /// Increases the stiffness of atom bonds.
    /// The stiffness is an integer value between 0 and 4.
    /// The default value is 1.
    /// </summary>
    public void increaseBondStiffness()
    {
        if (SettingsData.bondStiffness < 4)
        {
            SettingsData.bondStiffness += 1;
            if (ForceField.Singleton != null)
            {
                ForceField.Singleton.stiffness = SettingsData.bondStiffness;
            }
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
        if (SettingsData.bondStiffness > 0)
        {
            SettingsData.bondStiffness -= 1;
            if (ForceField.Singleton != null)
            {
                ForceField.Singleton.stiffness = SettingsData.bondStiffness;
            }
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
        if (SettingsData.repulsionScale < 0.9f)
        {
            SettingsData.repulsionScale += 0.1f;
            if (ForceField.Singleton != null)
            {
                ForceField.Singleton.repulsionScale = SettingsData.repulsionScale;
            }
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
        if (SettingsData.repulsionScale > 0.1f)
        {
            SettingsData.repulsionScale -= 0.1f;
            if (ForceField.Singleton != null)
            {
                ForceField.Singleton.repulsionScale = SettingsData.repulsionScale;
            }
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
    /// Toggles the unit of length used for displaying and entering distances.
    /// The button switches between Angstrom and picometers, Angstrom being the default.
    /// </summary>
    public void toggleLengthUnit()
    {
        SettingsData.useAngstrom = !SettingsData.useAngstrom;
        updateVisuals();

    }
    #endregion

    #region Visual Settings
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

    /// <summary>
    /// Toggles whether to interpolate the colors on bonds.
    /// Interpolation results in a smoother look, no interpolation results in the classic
    /// image of a bond in two solid colors, separated in the middle.
    /// </summary>
    public void toggleColorInterpolation()
    {
        SettingsData.interpolateColors = !SettingsData.interpolateColors;
        GlobalCtrl.Singleton?.reloadShaders();
        updateVisuals();
    }

    public void toggleVisualSettingsMenu()
    {
        GameObject visualSettingsMenu = gameObject.transform.Find("VisualSettings").gameObject;
        visualSettingsMenu.SetActive(!visualSettingsMenu.activeSelf);
    }

    public void toggleLicoriceRendering()
    {
        SettingsData.licoriceRendering = !SettingsData.licoriceRendering;
        GlobalCtrl.Singleton?.setLicoriceRendering(SettingsData.licoriceRendering);
        updateVisuals();
    }

    public void switchColorPalette(int howfar)
    {
        int currentScheme = Array.IndexOf(Enum.GetValues(typeof(ColorScheme)), SettingsData.colorScheme);
        int newIndex = (currentScheme + howfar) % chARpackColorSchemes.numberOfColorSchemes;
        while (newIndex < 0) newIndex = chARpackColorSchemes.numberOfColorSchemes + newIndex;
        colorSchemeManager.Singleton.setColorPalette((ColorScheme)(Enum.GetValues(typeof(ColorScheme))).GetValue(newIndex));
        updateVisuals();
    }

    public void toggleVideoPassThrough()
    {
        SettingsData.videoPassThrough = !SettingsData.videoPassThrough;
        var ar_cam_man = Camera.main.GetComponent<ARCameraManager>();
        if (ar_cam_man != null) 
        {
            ar_cam_man.enabled = SettingsData.videoPassThrough;
        }
        updateVisuals();
    }
    #endregion

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
        SettingsData.switchIntegrationMethodForward();
        if (ForceField.Singleton != null) ForceField.Singleton.currentMethod = SettingsData.integrationMethod;
        updateVisuals();
    }

    /// <summary>
    /// Switches to the last available force field integration method.
    /// </summary>
    public void switchIntegrationMethodBackward()
    {
        SettingsData.switchIntegrationMethodBackward();
        if (ForceField.Singleton != null) ForceField.Singleton.currentMethod = SettingsData.integrationMethod;
        updateVisuals();
    }

    private void initTimeFactors()
    {
        GameObject[] sliders = new GameObject[] { EulerTimeFactorSlider, SVTimeFactorSlider, RKTimeFactorSlider, MPTimeFactorSlider };
        float[] defaultVals = new float[] { /*Euler*/0.6f, /*SV*/0.75f, /*RK*/0.25f, /*MP*/0.2f };
        // TODO: use more sensible max and min vals
        float[] maxVals = new float[] { 1f, 1f, 1f, 1f };
        float[] minVals = new float[] { 0f, 0f, 0f, 0f };
        for (int i = 0; i < sliders.Length; i++)
        {
            GameObject slider = sliders[i];
            slider.GetComponent<mySlider>().maxVal = maxVals[i];
            slider.GetComponent<mySlider>().minVal = minVals[i];

            slider.GetComponent<mySlider>().defaultVal = defaultVals[i];
        }

        EulerTimeFactorSlider.GetComponent<mySlider>().OnValueUpdated.AddListener(OnEulerUpdated);
        SVTimeFactorSlider.GetComponent<mySlider>().OnValueUpdated.AddListener(OnSVUpdated);
        RKTimeFactorSlider.GetComponent<mySlider>().OnValueUpdated.AddListener(OnRKUpdated);
        MPTimeFactorSlider.GetComponent<mySlider>().OnValueUpdated.AddListener(OnMPUpdated);
    }

    public void OnEulerUpdated(mySliderEventData eventData)
    {
        SettingsData.timeFactors[0] = eventData.NewValue;
        if (ForceField.Singleton != null)
        {
            ForceField.Singleton.EulerTimeFactor = eventData.NewValue;
        }
    }

    public void OnSVUpdated(mySliderEventData eventData)
    {
        SettingsData.timeFactors[1] = eventData.NewValue;
        if (ForceField.Singleton != null)
        {
            ForceField.Singleton.SVtimeFactor = eventData.NewValue;
        }
    }

    public void OnRKUpdated(mySliderEventData eventData)
    {
        SettingsData.timeFactors[2] = eventData.NewValue;
        if (ForceField.Singleton != null)
        {
            ForceField.Singleton.RKtimeFactor = eventData.NewValue;
        }
    }

    public void OnMPUpdated(mySliderEventData eventData)
    {
        SettingsData.timeFactors[3] = eventData.NewValue;
        if (ForceField.Singleton != null)
        {
            ForceField.Singleton.MPtimeFactor = eventData.NewValue;
        }
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
        if (GlobalCtrl.Singleton != null)
        {
            GlobalCtrl.Singleton.toggleHandMenu();
        }
        else
        {
            SettingsData.handMenu = !SettingsData.handMenu;
        }

        updateVisuals();
    }

    /// <summary>
    /// Toggles handedness of the hand menu.
    /// The default value corresponds to a hand menu on the left hand
    /// which is more intuitive for right-handed users.
    /// </summary>
    public void toggleMenuHandedness()
    {
        if (handMenu.Singleton != null)
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
        }
        else
        {
            SettingsData.rightHandMenu = !SettingsData.rightHandMenu;
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
            if (userBoxes.Length > 0)
            {
                bool active = userBoxes[0].GetComponent<MeshRenderer>().enabled;
                foreach (GameObject userBox in userBoxes)
                {
                    userBox.GetComponent<MeshRenderer>().enabled = !active;
                }
                SettingsData.coop[0] = !active;
                setVisual(UserBoxIndicator, !active);
            }
            else
            {
                SettingsData.coop[0] = !SettingsData.coop[0];
                setVisual(UserBoxIndicator, SettingsData.coop[0]);
            }
        }
        catch 
        {
            setVisual(UserBoxIndicator, false);
            SettingsData.coop[0] = false;
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
            if (userRays.Length > 0)
            {
                bool active = userRays[0].GetComponent<LineRenderer>().enabled;
                foreach (GameObject userRay in userRays)
                {
                    userRay.GetComponent<LineRenderer>().enabled = !active;
                }
                SettingsData.coop[1] = !active;
                setVisual(UserRayIndicator, !active);
            }
            else
            {
                SettingsData.coop[1] = !SettingsData.coop[1];
                setVisual(UserRayIndicator, !SettingsData.coop[1]);
            }
        } catch
        {
            setVisual(UserRayIndicator, false);
            SettingsData.coop[1] = false;
        }
    }

    public void toggleMeasurementNetworking()
    {
        SettingsData.networkMeasurements = !SettingsData.networkMeasurements;
        updateVisuals();
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
        setIntegrationMethodVisual(SettingsData.integrationMethod);
        setColorPaletteVisual(SettingsData.colorScheme);

        setVisual(HandJointIndicator, SettingsData.handJoints);
        setVisual(HandMenuIndicator, SettingsData.handMenu);
        setVisual(HandMeshIndicator, SettingsData.handMesh);
        setVisual(HandRayIndicator, SettingsData.handRay);
        setVisual(ForceFieldIndicator, SettingsData.forceField);
        setVisual(SpatialMeshIndicator, SettingsData.spatialMesh);
        setVisual(ColorInterpolationIndicator, SettingsData.interpolateColors);
        setVisual(VideoPassThroughIndicator, SettingsData.videoPassThrough);

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
        setTimeFactorVisuals(SettingsData.timeFactors);
        // Connected to server (not local)
        if (LoginData.ip != null && LoginData.ip != "127.0.0.1")
        {
            setVisual(UserBoxIndicator, SettingsData.coop[0]);
            setVisual(UserRayIndicator, SettingsData.coop[1]);
        }
        setVisual(NetworkMeasurementIndicator, SettingsData.networkMeasurements);
        setVisual(LicoriceRenderingIndicator, SettingsData.licoriceRendering);

        setLengthUnitVisuals(SettingsData.useAngstrom);
    }

    public void setLengthUnitVisuals(bool useAngstrom)
    {
        LengthUnitLabel.GetComponent<TextMeshPro>().text = useAngstrom ? "\u00C5" : "pm";
        if (GlobalCtrl.Singleton != null)
        {
            GlobalCtrl.Singleton.regenerateSingleBondTooltips();
            GlobalCtrl.Singleton.regenerateChangeBondWindows();
            GlobalCtrl.Singleton.regenerateAtomTooltips();
        }
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
            indicator.GetComponent<MeshRenderer>().material.color = ColorPalette.activeIndicatorColor;
        }
        else
        {
            indicator.GetComponent<MeshRenderer>().material.color = ColorPalette.inactiveIndicatorColor;
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
        var text = method.ToString();
        if (localizationManager.Singleton != null)
        {
            text = localizationManager.Singleton.GetLocalizedString(method.ToString());
        }
        integrationMethodGO.GetComponent<TextMeshPro>().text = text;
    }

    public void setColorPaletteVisual(ColorScheme colorScheme)
    {
        var text = colorScheme.ToString();
        if (localizationManager.Singleton != null)
        {
            text = localizationManager.Singleton.GetLocalizedString(colorScheme.ToString());
        }
        colorPaletteGO.GetComponent<TextMeshPro>().text = text;
    }

    public void setTimeFactorVisuals(float[] timeFactors)
    {
        GameObject[] sliders = new GameObject[] { EulerTimeFactorSlider, SVTimeFactorSlider, RKTimeFactorSlider, MPTimeFactorSlider };

        for(int i=0; i<sliders.Length; i++)
        {
            GameObject slider = sliders[i];
            slider.GetComponent<mySlider>().SliderValue = timeFactors[i];
        }
    }
    #endregion
}
