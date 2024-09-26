using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class provides functionalities to load or save molecules from/to files.
/// </summary>
public class loadSaveWindow : myScrollObject
{

    private GameObject saveDialogPrefab;
    private GameObject loadEntryPrefab;
    private GameObject dialogPrefab;
    private myInputField dialogInputField;
    private ButtonConfigHelper dialogCloseButton;
    private GameObject saveDialogInstance;


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
                Destroy(value.gameObject);
            }

        }
    }

    void Awake()
    {
        Singleton = this;
    }

    public void Start()
    {
        // load prefabs
        saveDialogPrefab = (GameObject)Resources.Load("prefabs/saveDialog");
        loadEntryPrefab = (GameObject)Resources.Load("prefabs/LoadEntry");
        dialogPrefab = (GameObject)Resources.Load("prefabs/confirmDialog");

        //check for files
        initSavedFiles();

        // put window in vision
        //Vector3 in_vision_position = GlobalCtrl.Singleton.mainCamera.transform.position + 0.5f * GlobalCtrl.Singleton.mainCamera.transform.forward;
        //gameObject.transform.position = in_vision_position;

        // somehow the renderer list for clipping gets emptied after disable
        ////updateClipping();
    }

    /// <summary>
    /// This method initialises the saved files and refreshes them.
    /// </summary>
    public void initSavedFiles()
    {
        clearEntries();
        // get old scale
        var oldScale = scrollingObjectCollection.transform.parent.localScale;
        //reset scale 
        scrollingObjectCollection.transform.parent.localScale = Vector3.one;

#if UNITY_EDITOR || UNITY_STANDALONE || WINDOWS_UWP
        string path = Application.streamingAssetsPath + "/SavedMolecules/";
#else
        string path = Application.persistentDataPath + "/SavedMolecules/";
#endif
        if (Directory.Exists(path))
        {
            DirectoryInfo info = new DirectoryInfo(path);
            FileInfo[] fileInfo = info.GetFiles();
            foreach (FileInfo file in fileInfo)
            {

                if (file.Extension.Equals(".xml"))
                {
                    string name = Path.GetFileNameWithoutExtension(file.Name);
                        //file.Name.Substring(0, file.Name.Length - 4);

                    bool skip = false;
                    var currentLogEntries = gridObjectCollection.GetComponentsInChildren<showLoadConfirm>();
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
                        newLoadEntry.AddComponent<buttonMouseClick>();
                        newLoadEntry.transform.parent = gridObjectCollection.transform;
                    }
                }
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
        else
        {
            Debug.LogWarning($"[initSavedFiles] Could not find {path}");
        }
    }

    /// <summary>
    /// Opens a dialog where the user can enter save details and confirm the save process.
    /// </summary>
    public void openSaveDialog()
    {
        saveDialogInstance = Instantiate(saveDialogPrefab);
        var okButton = saveDialogInstance.GetComponent<saveDialogHelper>().okButton;
        okButton.onClick.AddListener(delegate{ performSave(); });
        dialogInputField = saveDialogInstance.GetComponent<saveDialogHelper>().inputField;
        dialogCloseButton = saveDialogInstance.GetComponent<saveDialogHelper>().closeButton;
        dialogCloseButton.OnClick.AddListener(delegate { onCloseButtonPressed(); });
        // put the dialog a bit further away
        saveDialogInstance.transform.position += 0.5f * GlobalCtrl.Singleton.mainCamera.transform.forward;
        gameObject.SetActive(false);
    }

    public void onCloseButtonPressed()
    {
        Destroy(saveDialogInstance);
        gameObject.SetActive(true);
    }


    /// <summary>
    /// Attempts to save the molecule to the given file name.
    /// If the file name was invalid, displays an error message.
    /// </summary>
    public void performSave()
    {
        var name = dialogInputField.text;
        if (name == "" || name == null)
        {
            saveDialogInstance.SetActive(false);
            var myDialog = Dialog.Open(dialogPrefab, DialogButtonType.OK, "Error", "Please enter file name", true);
            //make sure the dialog is rotated to the camera
            myDialog.transform.forward = -GlobalCtrl.Singleton.mainCamera.transform.forward;

            if (myDialog != null)
            {
                myDialog.OnClosed += OnClosedDialogEvent;
            }
            return;
        }
        GlobalCtrl.Singleton.SaveMolecule(false, name);
        Destroy(saveDialogInstance);
        gameObject.SetActive(true);
        initSavedFiles();
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.OK)
        {
            saveDialogInstance.SetActive(true);
        }
    }

}