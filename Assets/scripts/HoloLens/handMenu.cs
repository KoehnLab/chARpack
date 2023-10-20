using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
    private Color orange = new Color(1.0f, 0.5f, 0.0f);

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

    public void toggleVisible()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        SettingsData.handMenu = gameObject.activeSelf;
    }

    public void OnEnable()
    {
        scrollUpdate();
    }

    public void setVisuals()
    {
        if(GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.CHAIN)
        {
            ChainModeIndicator.GetComponent<MeshRenderer>().material.color = orange;
            MeasurementModeIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
        else if(GlobalCtrl.Singleton.currentInteractionMode == GlobalCtrl.InteractionModes.MEASUREMENT)
        {
            ChainModeIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
            MeasurementModeIndicator.GetComponent<MeshRenderer>().material.color = orange;
        }
        else
        {
            ChainModeIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
            MeasurementModeIndicator.GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

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
