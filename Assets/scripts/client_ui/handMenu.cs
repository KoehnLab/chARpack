using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using chARpackColorPalette;

/// <summary>
/// This class provides the functionality of a small scrollable atom menu
/// attached to the user's hand as well.
/// The menu also contains buttons to switch between interaction modes.
/// </summary>
public class handMenu : myScrollObject
{

    [HideInInspector] public GameObject handMenuPrefab;
    [HideInInspector] public GameObject atomEntryPrefab;
    private string[] atomNames = new string[] {
        "C", "O", "F",
        "N", "Cl", "S",
        "Si", "P", "I",
        "Br", "Ca", "B",
        "Na", "Fe", "Zn",
        "Ag", "Au", "Mg",
        "Ti"
    };

    public GameObject ChainModeIndicator;
    public GameObject MeasurementModeIndicator;

    private static handMenu _singleton;

    public static handMenu Singleton
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
                Debug.Log($"[{nameof(handMenu)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
        handMenuPrefab = (GameObject)Resources.Load("prefabs/HandMenu");

        // add atom buttons
        atomEntryPrefab = (GameObject)Resources.Load("prefabs/AtomButton");
        generateAtomEntries();
    }

    private void Start()
    {
        // Set starting colors for indicators
        setVisuals();
    }

    /// <summary>
    /// Generates buttons ordered in a GridObjectCollection for the different atom types.
    /// </summary>
    public void generateAtomEntries()
    {
        clearEntries();
        // get old scale
        var oldScale = scrollingObjectCollection.transform.parent.localScale;
        //reset scale 
        scrollingObjectCollection.transform.parent.localScale = Vector3.one;

        foreach (var atom in atomNames)
        {
            var entry = Instantiate(atomEntryPrefab);
            var button = entry.GetComponent<PressableButtonHoloLens2>();
            button.GetComponent<ButtonConfigHelper>().MainLabelText = $"{atom}";
            button.ButtonPressed.AddListener(delegate { GlobalCtrl.Singleton.createAtomUI(atom); });
            button.transform.parent = gridObjectCollection.transform;

        }
        // update on collection places all items in order
        gridObjectCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        // update on scoll content makes the list scrollable
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
        // update clipping for out of sight entries
        updateClipping();
        // scale after setting everything up
        scrollingObjectCollection.transform.parent.localScale = oldScale;
        // reset rotation
        resetRotation();
    }

    /// <summary>
    /// Toggles activity of the hand menu.
    /// </summary>
    public void toggleVisible()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        SettingsData.handMenu = gameObject.activeSelf;
    }

    public void OnEnable()
    {
        scrollUpdate();
    }

    /// <summary>
    /// Sets the colors of the indicators on the interaction mode buttons
    /// according to which one is currently active.
    /// </summary>
    public void setVisuals()
    {
        if(GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.FRAGMENT_ROTATION)
        {
            ChainModeIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.orange;
            MeasurementModeIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.gray;
        }
        else if(GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.MEASUREMENT)
        {
            ChainModeIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.gray;
            MeasurementModeIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.orange;
        }
        else
        {
            ChainModeIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.gray;
            MeasurementModeIndicator.GetComponent<MeshRenderer>().material.color = chARpackColors.gray;
        }
    }

    /// <summary>
    /// Sets the position of interaction mode and pagination buttons depending on which hand
    /// the menu attaches to.
    /// </summary>
    /// <param name="hand">the hand the menu attaches to</param>
    public void setButtonPosition(Handedness hand)
    {
        if(hand == Handedness.Right)
        {
            gameObject.transform.Find("MenuContent/Grid/ToggleChainModeButton").transform.localPosition = new Vector3(0.205f, 0.025f, 0);
            gameObject.transform.Find("MenuContent/Grid/ToggleMeasurmentModeButton").transform.localPosition = new Vector3(0.205f, -0.025f, 0);

            gameObject.transform.Find("MenuContent/ScrollParent/ScrollPaginationButtons").transform.localPosition = new Vector3(-0.0327f, 0, 0);
        }
        else if(hand == Handedness.Left)
        {
            gameObject.transform.Find("MenuContent/Grid/ToggleChainModeButton").transform.localPosition = new Vector3(0, 0.025f, 0);
            gameObject.transform.Find("MenuContent/Grid/ToggleMeasurmentModeButton").transform.localPosition = new Vector3(0, -0.025f, 0);

            gameObject.transform.Find("MenuContent/ScrollParent/ScrollPaginationButtons").transform.localPosition = new Vector3(0.1f, 0, 0);
        }
    }
}
