using Microsoft.MixedReality.Toolkit.Utilities;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class provides the functionality for the server-side settings panel.
/// </summary>
public class SettingsPanel : MonoBehaviour
{

    public GameObject bondStiffnessSlider;
    public GameObject repulsionScaleSlider;
    public GameObject forceFieldToggle;
    public GameObject spatialMeshToggle;
    public GameObject handMeshToggle;
    public GameObject handJointsToggle;
    public GameObject handRayToggle;
    public GameObject handMenuToggle;
    public GameObject autoGenerateStructureFormulasToggle;
    public GameObject exclusiveFullscreenToggle;
    public GameObject languageDropdown;
    public GameObject gazeHighlightingToggle;
    public GameObject pointerHighlightingToggle;
    public GameObject bondColorInterpolationToggle;
    public GameObject licoriceRenderingToggle;
    public GameObject integrationMethodDropdown;
    public GameObject interactionModeDropdown;
    public GameObject lengthUnitDropdown;
    public GameObject eulerSlider;
    public GameObject svSlider;
    public GameObject rkSlider;
    public GameObject mpSlider;
    public GameObject userBoxToggle;
    public GameObject userRayToggle;
    public GameObject networkMeasurementsToggle;
    public GameObject highlightColorMapDropdown;
    public GameObject showAllHighlightsToggle;
    public GameObject useAsyncModeToggle;
    public GameObject transitionModeDropdown;
    public GameObject immersiveTargetDropdown;
    public GameObject requireGrabHoldToggle;
    public GameObject handednessDropdown;
    public GameObject transitionAnimationDropdown;
    public GameObject transitionAnimationDurationSlider;
    public GameObject desktopTargetDropdown;
    public GameObject randomSeedInputField;
    public GameObject allowedTransitionInteractionsDropdown;
    public GameObject allowThrowingToggle;
    public GameObject hoverGazeAsSelectionToggle;

    // save load buttons
    public GameObject saveSettingsButton;
    public GameObject loadSettingsButton;

    private void Start()
    {
        updateElements();
        saveSettingsButton.GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(ShowSaveDialogCoroutine()); });
        loadSettingsButton.GetComponent<Button>().onClick.AddListener(delegate { StartCoroutine(ShowLoadDialogCoroutine()); });
    }

    private void OnEnable()
    {
        updateElements();
    }

    IEnumerator ShowSaveDialogCoroutine()
    {
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files);


        if (FileBrowser.Success)
        {
            if (FileBrowser.Result.Length != 1)
            {
                Debug.LogError("[SettingsPanel] Path from FileBrowser is empty. Abort.");
                yield break;
            }
            FileInfo fi = new FileInfo(FileBrowser.Result[0]);
            SettingsData.dumpSettingsToJSON(fi.FullName);
        }
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files);


        if (FileBrowser.Success)
        {
            if (FileBrowser.Result.Length != 1)
            {
                Debug.LogError("[SettingsPanel] Path from FileBrowser is empty. Abort.");
                yield break;
            }
            FileInfo fi = new FileInfo(FileBrowser.Result[0]);
            SettingsData.readSettingsFromJSON(fi.FullName);
            updateElements();
        }
    }


    /// <summary>
    /// Checks the current state of all settings data from the network.
    /// </summary>
    void updateElements()
    {
        bondStiffnessSlider.GetComponent<Slider>().value = SettingsData.bondStiffness;
        bondStiffnessSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        repulsionScaleSlider.GetComponent<Slider>().value = SettingsData.repulsionScale;
        repulsionScaleSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        forceFieldToggle.GetComponent<Toggle>().isOn = SettingsData.forceField;

        spatialMeshToggle.GetComponent<Toggle>().isOn = SettingsData.spatialMesh;

        handMeshToggle.GetComponent<Toggle>().isOn = SettingsData.handMesh;

        handJointsToggle.GetComponent<Toggle>().isOn = SettingsData.handJoints;

        handRayToggle.GetComponent<Toggle>().isOn = SettingsData.handRay;

        handMenuToggle.GetComponent<Toggle>().isOn = SettingsData.handMenu;

        autoGenerateStructureFormulasToggle.GetComponent<Toggle>().isOn = SettingsData.autogenerateStructureFormulas;

        exclusiveFullscreenToggle.GetComponent<Toggle>().isOn = SettingsData.exclusiveFullscreen;

        gazeHighlightingToggle.GetComponent<Toggle>().isOn = SettingsData.gazeHighlighting;

        pointerHighlightingToggle.GetComponent<Toggle>().isOn = SettingsData.pointerHighlighting;

        showAllHighlightsToggle.GetComponent<Toggle>().isOn = SettingsData.showAllHighlightsOnClients;

        int highlightColorMapValue = (int)SettingsData.highlightColorMap;
        highlightColorMapDropdown.GetComponent<TMPro.TMP_Dropdown>().value = highlightColorMapValue;

        bondColorInterpolationToggle.GetComponent<Toggle>().isOn = SettingsData.interpolateColors;

        licoriceRenderingToggle.GetComponent<Toggle>().isOn = SettingsData.licoriceRendering;

        int langValue = 0;
        if (SettingsData.language == "de")
        {
            langValue = 1;
        }
        languageDropdown.GetComponent<TMPro.TMP_Dropdown>().value = langValue;

        int integrationMethodValue = (int)SettingsData.integrationMethod;
        integrationMethodDropdown.GetComponent<TMPro.TMP_Dropdown>().value = integrationMethodValue;

        int interactionModeValue = (int)SettingsData.interactionMode;
        interactionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().value = interactionModeValue;

        lengthUnitDropdown.GetComponent<TMPro.TMP_Dropdown>().value = SettingsData.useAngstrom ? 0 : 1;

        eulerSlider.GetComponent<Slider>().value = SettingsData.timeFactors[0];
        eulerSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        svSlider.GetComponent<Slider>().value = SettingsData.timeFactors[1];
        svSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        rkSlider.GetComponent<Slider>().value = SettingsData.timeFactors[2];
        rkSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        mpSlider.GetComponent<Slider>().value = SettingsData.timeFactors[3];
        mpSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        networkMeasurementsToggle.GetComponent<Toggle>().isOn = SettingsData.networkMeasurements;

        useAsyncModeToggle.GetComponent<Toggle>().isOn = SettingsData.syncMode == TransitionManager.SyncMode.Async;
        transitionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().value = (int)SettingsData.transitionMode;
        allowedTransitionInteractionsDropdown.GetComponent<TransitionInteractionTypeFlagEnumDropdown>().selectedOptions = SettingsData.allowedTransitionInteractions;
        immersiveTargetDropdown.GetComponent<TMPro.TMP_Dropdown>().value = (int)SettingsData.immersiveTarget;
        desktopTargetDropdown.GetComponent<TMPro.TMP_Dropdown>().value = (int)SettingsData.desktopTarget;
        requireGrabHoldToggle.GetComponent<Toggle>().isOn = SettingsData.requireGrabHold;
        var handednessValue = 2; // Both
        if (SettingsData.handedness == Handedness.Right)
        {
            handednessValue = 0;
        }
        else if (SettingsData.handedness == Handedness.Left)
        {
            handednessValue = 1;
        }
        handednessDropdown.GetComponent<TMPro.TMP_Dropdown>().value =  handednessValue;
        var transitionAniValue = 0;
        if (SettingsData.transitionAnimation == TransitionManager.TransitionAnimation.BOTH)
        {
            transitionAniValue = 3;
        }
        else if (SettingsData.transitionAnimation == TransitionManager.TransitionAnimation.ROTATION)
        {
            transitionAniValue = 2;
        }
        else if (SettingsData.transitionAnimation == TransitionManager.TransitionAnimation.SCALE)
        {
            transitionAniValue = 1;
        }
        transitionAnimationDropdown.GetComponent<TMPro.TMP_Dropdown>().value = transitionAniValue;
        transitionAnimationDurationSlider.GetComponent<Slider>().value = SettingsData.transitionAnimationDuration;
        transitionAnimationDurationSlider.GetComponent<UpdateSliderLabel>().updateLabel();
        randomSeedInputField.GetComponent<TMP_InputField>().text = $"{SettingsData.randomSeed}";
        allowThrowingToggle.GetComponent<Toggle>().isOn = SettingsData.allowThrowing;
        hoverGazeAsSelectionToggle.GetComponent<Toggle>().isOn = SettingsData.hoverGazeAsSelection;
    }

    /// <summary>
    /// Saves the (changed) settings and triggers an update settings event.
    /// </summary>
    public void saveSettings()
    {
        SettingsData.bondStiffness = (ushort)bondStiffnessSlider.GetComponent<Slider>().value;
        SettingsData.repulsionScale = repulsionScaleSlider.GetComponent<Slider>().value;
        SettingsData.forceField = forceFieldToggle.GetComponent<Toggle>().isOn;
        SettingsData.spatialMesh = spatialMeshToggle.GetComponent<Toggle>().isOn;
        SettingsData.handMesh = handMeshToggle.GetComponent<Toggle>().isOn;
        SettingsData.handJoints = handJointsToggle.GetComponent<Toggle>().isOn;
        SettingsData.handRay = handRayToggle.GetComponent<Toggle>().isOn;
        SettingsData.handMenu = handMenuToggle.GetComponent<Toggle>().isOn;
        SettingsData.autogenerateStructureFormulas = autoGenerateStructureFormulasToggle.GetComponent<Toggle>().isOn;
        SettingsData.exclusiveFullscreen = exclusiveFullscreenToggle.GetComponent<Toggle>().isOn;
        SettingsData.gazeHighlighting = gazeHighlightingToggle.GetComponent<Toggle>().isOn;
        SettingsData.pointerHighlighting = pointerHighlightingToggle.GetComponent<Toggle>().isOn;
        SettingsData.showAllHighlightsOnClients = showAllHighlightsToggle.GetComponent<Toggle>().isOn;
        SettingsData.highlightColorMap = highlightColorMapDropdown.GetComponent<TMPro.TMP_Dropdown>().value;
        var options = languageDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var lang = options[languageDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        SettingsData.language = lang;
        options = integrationMethodDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var methodString = options[integrationMethodDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        Enum.TryParse(methodString, ignoreCase:true, out ForceField.Method method);
        SettingsData.integrationMethod = method;
        options = interactionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var interactionString = options[interactionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        interactionString = interactionString.Replace(" ", "_");
        Enum.TryParse(interactionString, ignoreCase:true, out GlobalCtrl.InteractionModes mode);
        SettingsData.interactionMode = mode;
        SettingsData.useAngstrom = lengthUnitDropdown.GetComponent<TMPro.TMP_Dropdown>().value == 0;
        SettingsData.timeFactors = new float[]{ eulerSlider.GetComponent<Slider>().value, 
                                                svSlider.GetComponent<Slider>().value, 
                                                rkSlider.GetComponent<Slider>().value, 
                                                mpSlider.GetComponent<Slider>().value };
        SettingsData.coop = new bool[] { userBoxToggle.GetComponent<Toggle>().isOn, userRayToggle.GetComponent<Toggle>().isOn };
        SettingsData.networkMeasurements = networkMeasurementsToggle.GetComponent<Toggle>().isOn;
        SettingsData.interpolateColors = bondColorInterpolationToggle.GetComponent<Toggle>().isOn;
        SettingsData.licoriceRendering = licoriceRenderingToggle.GetComponent<Toggle>().isOn;

        SettingsData.syncMode = useAsyncModeToggle.GetComponent<Toggle>().isOn ? TransitionManager.SyncMode.Async : TransitionManager.SyncMode.Sync;
        SettingsData.transitionMode = (TransitionManager.TransitionMode)transitionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().value;

        SettingsData.allowedTransitionInteractions = allowedTransitionInteractionsDropdown.GetComponent<TransitionInteractionTypeFlagEnumDropdown>().selectedOptions;
        Debug.Log($"[FlagEnum] Current Options {SettingsData.allowedTransitionInteractions}");

        SettingsData.immersiveTarget = (TransitionManager.ImmersiveTarget)immersiveTargetDropdown.GetComponent<TMPro.TMP_Dropdown>().value;
        SettingsData.desktopTarget = (TransitionManager.DesktopTarget)desktopTargetDropdown.GetComponent<TMPro.TMP_Dropdown>().value;
        SettingsData.requireGrabHold = requireGrabHoldToggle.GetComponent<Toggle>().isOn;
        options = handednessDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var handedness = Handedness.Both;
        if (options[handednessDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text == "Right")
        {
            handedness = Handedness.Right;
        }
        else if (options[handednessDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text == "Left")
        {
            handedness = Handedness.Left;
        }
        SettingsData.handedness = handedness;
        options = transitionAnimationDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        if (options[transitionAnimationDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text == "Both")
        {
            SettingsData.transitionAnimation = TransitionManager.TransitionAnimation.BOTH;
        }
        else if (options[transitionAnimationDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text == "Scale")
        {
            SettingsData.transitionAnimation = TransitionManager.TransitionAnimation.SCALE;
        }
        else if (options[transitionAnimationDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text == "Rotation")
        {
            SettingsData.transitionAnimation = TransitionManager.TransitionAnimation.ROTATION;
        }
        else
        {
            SettingsData.transitionAnimation = TransitionManager.TransitionAnimation.NONE;
        }
        SettingsData.transitionAnimationDuration = transitionAnimationDurationSlider.GetComponent<Slider>().value;
        SettingsData.randomSeed = int.Parse(randomSeedInputField.GetComponent<TMP_InputField>().text);
        SettingsData.allowThrowing = allowThrowingToggle.GetComponent<Toggle>().isOn;
        SettingsData.hoverGazeAsSelection = hoverGazeAsSelectionToggle.GetComponent<Toggle>().isOn;


        settingsControl.Singleton.updateSettings();
        EventManager.Singleton.UpdateSettings();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Toggles visibility of the settings panel.
    /// </summary>
    public void togglePanel()
    {
        var active = gameObject.activeSelf;
        gameObject.SetActive(!active);
        HoverMarker.Singleton.setSettingsActive(gameObject.activeSelf);
    }


    public void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

}
