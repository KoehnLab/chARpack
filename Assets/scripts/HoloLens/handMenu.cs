using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class handMenu : MonoBehaviour
{

    [HideInInspector] public GameObject handMenuPrefab;
    [HideInInspector] public GameObject atomEntryPrefab;
    public GameObject clippingBox;
    public GameObject gridObjectCollection;
    public GameObject scrollingObjectCollection;
    public GameObject scrollParent;
    private string[] atomNames = new string[] {
        "C", "O", "F",
        "N", "Cl", "S",
        "Si", "P", "I",
        "Br", "Ca", "B",
        "Na", "Fe", "Zn",
        "Ag", "Au", "Mg",
        "Ti"
    };

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
    private void resetRotation()
    {
        foreach (Transform child in gridObjectCollection.transform)
        {
            child.localRotation = Quaternion.identity;
        }
    }

    public void updateClipping()
    {
        if (gameObject.activeSelf)
        {
            var cb = clippingBox.GetComponent<ClippingBox>();
            cb.ClearRenderers();
            foreach (Transform child in gridObjectCollection.transform)
            {
                var renderers = child.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    cb.AddRenderer(renderer);
                }
            }
        }
    }

    public void generateAtomEntries()
    {
        // get old scale
        var oldScale = scrollingObjectCollection.transform.parent.localScale;
        //reset scale 
        scrollingObjectCollection.transform.parent.localScale = Vector3.one;

        foreach (var atom in atomNames)
        {
            var entry = Instantiate(atomEntryPrefab);
            var button = entry.GetComponent<PressableButtonHoloLens2>();
            button.GetComponent<ButtonConfigHelper>().MainLabelText = $"{atom}";
            button.GetComponent<ButtonConfigHelper>().IconStyle = ButtonIconStyle.None;
            //button.ButtonPressed.AddListener(delegate { GlobalCtrl.Singleton.createAtomUI(atom); });
            button.transform.parent = gridObjectCollection.transform;
            button.GetComponent<ButtonConfigHelper>().OnClick.AddListener(delegate { GlobalCtrl.Singleton.createAtomUI(atom); });

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
}
