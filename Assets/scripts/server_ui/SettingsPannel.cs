using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class provides the functionality for the server-side settings panel.
/// </summary>
public class SettingsPannel : MonoBehaviour
{

    public GameObject bondStiffnessSlider;
    public GameObject repulsionScaleSlider;
    public GameObject forceFieldToggle;
    public GameObject spatialMeshToggle;
    public GameObject handMeshToggle;
    public GameObject handJointsToggle;
    public GameObject handRayToggle;
    public GameObject handMenuToggle;
    public GameObject languageDropdown;
    public GameObject gazeHighlightingToggle;
    public GameObject pointerHighlightingToggle;
    public GameObject integrationMethodDropdown;
    public GameObject interactionModeDropdown;
    public GameObject eulerSlider;
    public GameObject svSlider;
    public GameObject rkSlider;
    public GameObject mpSlider;
    public GameObject userBoxToggle;
    public GameObject userRayToggle;

    private void Start()
    {
        updateElements();
    }

    private void OnEnable()
    {
        updateElements();
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

        gazeHighlightingToggle.GetComponent<Toggle>().isOn = SettingsData.gazeHighlighting;

        pointerHighlightingToggle.GetComponent<Toggle>().isOn = SettingsData.pointerHighlighting;

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

        eulerSlider.GetComponent<Slider>().value = SettingsData.timeFactors[0];
        eulerSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        svSlider.GetComponent<Slider>().value = SettingsData.timeFactors[1];
        svSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        rkSlider.GetComponent<Slider>().value = SettingsData.timeFactors[2];
        rkSlider.GetComponent<UpdateSliderLabel>().updateLabel();

        mpSlider.GetComponent<Slider>().value = SettingsData.timeFactors[3];
        mpSlider.GetComponent<UpdateSliderLabel>().updateLabel();
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
        SettingsData.gazeHighlighting = gazeHighlightingToggle.GetComponent<Toggle>().isOn;
        SettingsData.pointerHighlighting = pointerHighlightingToggle.GetComponent<Toggle>().isOn;
        var options = languageDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var lang = options[languageDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        SettingsData.language = lang;
        options = integrationMethodDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var methodString = options[integrationMethodDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        Enum.TryParse(methodString, ignoreCase:true, out ForceField.Method method);
        SettingsData.integrationMethod = method;
        options = interactionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var interactionString = options[interactionModeDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        Enum.TryParse(interactionString, ignoreCase:true, out GlobalCtrl.InteractionModes mode);
        SettingsData.interactionMode = mode;
        SettingsData.timeFactors = new float[]{ eulerSlider.GetComponent<Slider>().value, 
                                                svSlider.GetComponent<Slider>().value, 
                                                rkSlider.GetComponent<Slider>().value, 
                                                mpSlider.GetComponent<Slider>().value };
        SettingsData.coop = new bool[] { userBoxToggle.GetComponent<Toggle>().isOn, userRayToggle.GetComponent<Toggle>().isOn };

        settingsControl.Singleton.updateSettings();
        EventManager.Singleton.UpdateSettings();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Toggles visibility of the settings panel.
    /// </summary>
    public void togglePannel()
    {
        var active = gameObject.activeSelf;
        gameObject.SetActive(!active);
    }

}
