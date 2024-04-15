using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Atom2D : MonoBehaviour
{
    [HideInInspector]
    public Atom atom;
    private Material focus2Dmaterial;
    private static int numFoci = 1;

    public static int NumFoci
    {
        get => numFoci;
        set
        {
            if (value > 4 || value < 1)
            {
                Debug.LogError("[Atom2D:NumFoci] Minimum: 1, Maximum: 4.");
                return;
            }
            numFoci = value;
            needsGlobalUpdate = true;
        }
    }

    private bool needsUpdate;
    private static bool needsGlobalUpdate;

    public static void StopGlobalUpdate()
    {
        needsGlobalUpdate = false;
    }

    [SerializeField]
    private Color[] fociColors = new Color[4] { Color.white, Color.red, Color.blue, Color.green };
    public Color[] FociColors
    {
        get { return fociColors; }
        set
        {
            fociColors = value;
            needsUpdate = true;
        }
    }

    private void Start()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(delegate { selectOnClick(); });
        focus2Dmaterial = Instantiate(Resources.Load<Material>("materials/Focus2DMaterial"));
        GetComponent<Image>().material = focus2Dmaterial;
    }

    void Update()
    {
        if (needsUpdate || needsGlobalUpdate)
        {
            needsUpdate = false;

            UpdateMaterialProperties();
        }
    }

    private void selectOnClick()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            atom.markAtomUI(!atom.isMarked);
        }
        else
        {
            atom.serverFocusHighlightUI(!atom.serverFocus);
        }
    }

    private void UpdateMaterialProperties()
    {
        focus2Dmaterial.SetInt("NumFoci", numFoci);
        focus2Dmaterial.SetColorArray("_FociColors", fociColors);
    }
}

