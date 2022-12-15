using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.IO;
using TMPro;
using UnityEngine;

public class loadSaveWindow : MonoBehaviour
{
    public GameObject gridObjCollection;
    public GameObject loadEntryPrefab;
    public GameObject scrollingObjectCollection;
    public GameObject clippingBox;
    public GameObject saveDialogPrefab;

    private enum Color { red, green, blue, black, white, yellow, orange };

    private static loadSaveWindow _singleton;

    public static loadSaveWindow Singleton
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
                Debug.Log($"[{nameof(loadSaveWindow)}] Instance already exists, destroying duplicate!");
                Destroy(value);
            }

        }
    }

    void Awake()
    {
        Singleton = this;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// this method initialises the saved files and refreshes them
    /// </summary>
    public void initSavedFiles()
    {
        string path = Application.streamingAssetsPath + "/SavedMolecules/";
        DirectoryInfo info = new DirectoryInfo(path);
        FileInfo[] fileInfo = info.GetFiles();
        foreach (FileInfo file in fileInfo)
        {

            if (file.Extension.Equals(".xml"))
            {
                string name = file.Name.Substring(0, file.Name.Length - 4);

                bool skip = false;
                var currentLogEntries = gridObjCollection.GetComponentsInChildren<showLoadConfirm>();
                foreach (var entry in currentLogEntries)
                {
                    if (name == entry.mol_name)
                    {
                        skip = true;
                    }
                }

                if (!skip)
                {
                    GameObject newLoadEntry = Instantiate(loadEntryPrefab, Vector3.zero, Quaternion.identity);
                    newLoadEntry.GetComponent<showLoadConfirm>().mol_name = name;
                    newLoadEntry.transform.SetParent(gridObjCollection.transform, false);

                    // update on collection places all items in order
                    gridObjCollection.GetComponent<GridObjectCollection>().UpdateCollection();
                    // update on scoll content makes the list scrollable
                    scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
                    // add all renderers of a log entry to the clipping box renderer list to make the buttons disappear when out of bounds
                    var cb = clippingBox.GetComponent<ClippingBox>();
                    var renderers = newLoadEntry.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        cb.AddRenderer(renderer);
                    }
                }
            }
        }
    }
    public void show()
    {
        initSavedFiles();
        gameObject.SetActive(true);
        // put window in vision
        //Vector3 in_vision_position = Camera.main.transform.position + 0.5f * Camera.main.transform.forward;
        //gameObject.transform.position = in_vision_position;

        // somehow the renderer list for clipping gets emptied after disable
        ////updateClipping();
    }

    public void updateClipping()
    {
        if (gameObject.activeSelf)
        {
            var cb = clippingBox.GetComponent<ClippingBox>();
            foreach (Transform child in gridObjCollection.transform)
            {
                var renderers = child.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    cb.AddRenderer(renderer);
                }
            }
        }
    }

    public void openSaveDialog()
    {
        var saveDialogInstance = Instantiate(saveDialogPrefab);
        // put the dialog a bit further away
        saveDialogInstance.transform.position += 0.5f * Camera.main.transform.forward;
        gameObject.SetActive(false);
    }

}