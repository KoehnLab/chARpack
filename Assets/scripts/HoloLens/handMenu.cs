using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class handMenu : MonoBehaviour
{

    [HideInInspector] public GameObject handMenuPrefab;
    public GameObject buttonCollection;
    private string[] atomNames = new string[] { "C", "O", "F", "N", "Cl" };

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

        for (int i = 0; i < buttonCollection.transform.childCount; i++)
        {
            PressableButtonHoloLens2 button = buttonCollection.transform.GetChild(i).gameObject.GetComponent<PressableButtonHoloLens2>();
            var current_name = atomNames[i];
            button.GetComponent<ButtonConfigHelper>().MainLabelText = $"{current_name}";
            button.GetComponent<ButtonConfigHelper>().IconStyle = ButtonIconStyle.None;
            button.ButtonPressed.AddListener(delegate { GlobalCtrl.Singleton.createAtomUI(current_name); });

        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
