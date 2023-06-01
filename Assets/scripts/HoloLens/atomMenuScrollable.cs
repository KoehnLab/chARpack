using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class atomMenuScrollable : MonoBehaviour
{

    [HideInInspector] public GameObject atomMenuScrollablePrefab;
    [HideInInspector] public GameObject atomEntryPrefab;
    public GameObject clippingBox;
    public GameObject gridObjectCollection;
    public GameObject scrollingObjectCollection;
    private string[] atomNames = new string[] { "C", "O", "F", "N", "Cl" };

    private static atomMenuScrollable _singleton;

    public static atomMenuScrollable Singleton
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
                Debug.Log($"[{nameof(atomMenu)}] Instance already exists, destroying duplicate!");
                Destroy(value.gameObject);
            }

        }
    }

    private void Awake()
    {
        Singleton = this;
        atomMenuScrollablePrefab = (GameObject)Resources.Load("prefabs/AtomMenuScrollable");
        atomEntryPrefab = (GameObject)Resources.Load("prefabs/AtomButton");
        generateAtomEntries();

    }

    public void close()
    {
        Destroy(gameObject);
    }
    public void refresh()
    {
        generateAtomEntries();
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

    private void resetRotation()
    {
        foreach (Transform child in gridObjectCollection.transform)
        {
            child.localRotation = Quaternion.identity;
        }
    }
    public void generateAtomEntries()
    {
        // get old scale
        var oldScale = scrollingObjectCollection.transform.parent.localScale;
        //reset scale 
        scrollingObjectCollection.transform.parent.localScale = Vector3.one;

        foreach(var atom in atomNames)
        {
            var entry = Instantiate(atomEntryPrefab);
            var button = entry.GetComponent<PressableButtonHoloLens2>();
            button.GetComponent<ButtonConfigHelper>().MainLabelText = $"{atom}";
            button.GetComponent<ButtonConfigHelper>().IconStyle = ButtonIconStyle.None;
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
}
