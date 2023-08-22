using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private void Start()
    {
        updateElements();
    }

    private void OnEnable()
    {
        updateElements();
    }

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

        int langValue = 0;
        if (SettingsData.language == "de")
        {
            langValue = 1;
        }
        languageDropdown.GetComponent<TMPro.TMP_Dropdown>().value = langValue;
    }

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
        var options = languageDropdown.GetComponent<TMPro.TMP_Dropdown>().options;
        var lang = options[languageDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        SettingsData.language = lang;

        settingsControl.Singleton.updateSettings();
        EventManager.Singleton.UpdateSettings();

        gameObject.SetActive(false);
    }

    public void togglePannel()
    {
        var active = gameObject.activeSelf;
        gameObject.SetActive(!active);
    }

}
