using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

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
        setExclusiveFullscreen(SettingsData.exclusiveFullscreen); // In Awake to hopefully avoid strange blinking behavior
    }

    public void Start()
    {
        updateSettings();
    }

    public void updateSettings()
    {
        try
        {
            // These settings currently depend on the presence of hand-related game objects in the scene
            setHandJoint(SettingsData.handJoints);
            setHandMenu(SettingsData.handMenu);
            setHandMesh(SettingsData.handMesh);
            setHandRay(SettingsData.handRay);
            setSpatialMesh(SettingsData.spatialMesh);
        }
        catch
        {
            Debug.Log("Couldn't set hand settings locally; probably in Server Scene");
        }
        setBondStiffness(SettingsData.bondStiffness);
        setForceField(SettingsData.forceField);
        setRepulsionScale(SettingsData.repulsionScale);
        setLanguage(SettingsData.language);
        setIntegrationMethod(SettingsData.integrationMethod);
        setTimeFactors(SettingsData.timeFactors);
        setCoopSettings(SettingsData.coop);
        setInteractionMode(SettingsData.interactionMode);
        setAutoGenerateStructureFormulas(SettingsData.autogenerateStructureFormulas);
        setExclusiveFullscreen(SettingsData.exclusiveFullscreen);
        setSyncMode(SettingsData.syncMode);
        setRandomSeed(SettingsData.randomSeed);
        GlobalCtrl.Singleton.setLicoriceRendering(SettingsData.licoriceRendering);
        GlobalCtrl.Singleton.reloadShaders();
        GlobalCtrl.Singleton.regenerateSingleBondTooltips(); // Regenerate in case length unit was changed
        // gaze and pointer highlighting and color interpolation are handled by checking the value in SettingsData directly in the script
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
    }

    private void setHandMesh(bool value)
    {
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
    }

    private void setHandJoint(bool value)
    {
        MixedRealityHandTrackingProfile handTrackingProfile = null;

        if (CoreServices.InputSystem?.InputSystemProfile != null)
        {
            handTrackingProfile = CoreServices.InputSystem.InputSystemProfile.HandTrackingProfile;
        }

        if (handTrackingProfile != null)
        {
            handTrackingProfile.EnableHandJointVisualization = value;
        }
    }

    private void setHandRay(bool value)
    {
        if (value)
        {
            PointerUtils.SetPointerBehavior<ShellHandRayPointer>(PointerBehavior.Default, InputSourceType.Hand);
        }
        else
        {
            PointerUtils.SetPointerBehavior<ShellHandRayPointer>(PointerBehavior.AlwaysOff, InputSourceType.Hand);
        }
    }

    private void setHandMenu(bool value)
    {
        handMenu.Singleton.gameObject.SetActive(value);
    }

    private void setAutoGenerateStructureFormulas(bool value)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (StructureFormulaGenerator.Singleton)
        {
            if (value) EventManager.Singleton.OnMoleculeLoaded += StructureFormulaGenerator.Singleton.immediateRequestStructureFormula;
            else EventManager.Singleton.OnMoleculeLoaded -= StructureFormulaGenerator.Singleton.immediateRequestStructureFormula;
        }
#endif
    }

    private int windowWidth = 0;
    private int windowHeight = 0;

    private void setExclusiveFullscreen(bool value)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (windowWidth == 0) windowWidth = Screen.currentResolution.width / 3; //Initialization
        if (windowHeight == 0) windowHeight = Screen.currentResolution.height * 3 / 5;

        if (!value) Screen.SetResolution(windowWidth, windowHeight, false);
        else
        {
            windowWidth = Screen.width;
            windowHeight = Screen.height;
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        }
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

    private void setTimeFactors(float[] timeFactors)
    {
        ForceField.Singleton.EulerTimeFactor = timeFactors[0];
        ForceField.Singleton.SVtimeFactor = timeFactors[1];
        ForceField.Singleton.RKtimeFactor = timeFactors[2];
        ForceField.Singleton.MPtimeFactor = timeFactors[3];

    }

    private void setInteractionMode(GlobalCtrl.InteractionModes mode)
    {
        GlobalCtrl.Singleton.setInteractionMode(mode);
    }

    private void setCoopSettings(bool[] coop)
    {
        bool userBox = coop[0];
        bool userRay = coop[1];
        // We want to keep seeing the boxes in the server scene
        if (!SceneManager.GetActiveScene().name.Equals("ServerScene"))
        {
            var userBoxes = GameObject.FindGameObjectsWithTag("User Box");
            foreach (GameObject box in userBoxes)
            {
                box.GetComponent<MeshRenderer>().enabled = userBox;
                box.GetComponent<LineRenderer>().enabled = userRay;
            }
        }
    }
    
    private void setSyncMode(TransitionManager.SyncMode mode)
    {
        if (NetworkManagerClient.Singleton != null)
        {
            NetworkManagerClient.Singleton.changeSyncMode(mode);
        }
        if (NetworkManagerServer.Singleton != null)
        {
            NetworkManagerServer.Singleton.changeSyncMode(mode);
        }
    }

    private void setRandomSeed(int seed)
    {
        UnityEngine.Random.InitState(seed);
    }
}
