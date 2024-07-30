using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Diagnostics;

public class StructureFormula : MonoBehaviour
{
    public SVGImage image;
    public Vector2 originalSize;
    public Button collapse_button;
    public Button close_button;
    public TextMeshProUGUI collapse_button_label;
    public TextMeshProUGUI label;
    public HeatMap2D heatMap;
    public TMP_Dropdown highlight_choice_dropdown;
    [HideInInspector]
    public Atom2D[] interactables;
    public float scaleFactor = 1.0f;
    public int current_highlight_choice = 0;
    private static int _numFocusRegions = 1;
    public int onlyUser = -1;

    public Vector3 localPosition = new Vector3(0, 0, 0);

    public static int numFocusRegions { get => _numFocusRegions; 
        set
        {
            _numFocusRegions = value;
        }
    }

    private void Start()
    {
        collapse_button.onClick.AddListener(delegate { toggleImage(); });
        var drag = label.gameObject.AddComponent<Draggable>();
        drag.target = transform;

        heatMap.Intensity = 1.0f;
        heatMap.Radius = 0.0025f * scaleFactor;
        heatMap.CanvasWidth = originalSize.x;
        heatMap.CanvasHeight = originalSize.y;

        highlight_choice_dropdown.onValueChanged.AddListener(setHighlightOption);
        close_button.onClick.AddListener(close);

        RectTransform rect = transform as RectTransform;
        if (localPosition != new Vector3(0, 0, 0))
        {
            rect.localPosition = localPosition;
        }
        else
        {
            Vector2 save = SpawnManager.Singleton.GetSpawnLocalPosition(rect);
            rect.position = save;
        }
    }

    public void setHighlightOption(Int32 choice)
    {
        current_highlight_choice = choice;
        if (choice == 0)
        {
            heatMap.gameObject.SetActive(false);
            if (highlight_choice_dropdown.value != choice) highlight_choice_dropdown.value = choice;

            foreach (var inter in interactables)
            {
                inter.gameObject.SetActive(true);
            }
        }
        else
        {
            heatMap.gameObject.SetActive(true);
            if (highlight_choice_dropdown.value != choice) highlight_choice_dropdown.value = choice;

            foreach (var inter in interactables)
            {
                inter.gameObject.SetActive(false);
            }
        }
    }


    public void newImageResize()
    {
        var rect = transform as RectTransform;
        var image_rect = image.transform as RectTransform;
        var button_rect = collapse_button.transform as RectTransform;
        var label_rect = label.transform as RectTransform;
        var vert_layout = GetComponent<VerticalLayoutGroup>();
        var highlight_dropdown = highlight_choice_dropdown.transform as RectTransform;

        float new_width = image_rect.sizeDelta.x + vert_layout.padding.left + vert_layout.padding.right;
        float new_height = image_rect.sizeDelta.y + vert_layout.padding.top + vert_layout.padding.bottom + button_rect.sizeDelta.y + vert_layout.spacing + label_rect.sizeDelta.y + vert_layout.spacing + highlight_dropdown.sizeDelta.y + vert_layout.spacing;

        rect.sizeDelta = new Vector2(new_width, new_height);

        if (!image.gameObject.activeSelf)
        {
            resizeCollapse(true);
        }
        heatMap.CanvasWidth = originalSize.x;
        heatMap.CanvasHeight = originalSize.y;
        heatMap.ResetCurrentHeatMap();
    }

    public void resizeCollapse(bool value)
    {
        var image_rect = image.transform as RectTransform;
        var image_hight = image_rect.sizeDelta.y;
        var rect = transform as RectTransform;
        if (value)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y - image_hight);
            rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y + 0.5f * image_hight, rect.localPosition.z);
        }
        else
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + image_hight);
            rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y - 0.5f * image_hight, rect.localPosition.z);
        }
    }

    private void toggleImage()
    {
        image.gameObject.SetActive(!image.gameObject.activeSelf);
        highlight_choice_dropdown.gameObject.SetActive(!highlight_choice_dropdown.gameObject.activeSelf);
        resizeCollapse(!image.gameObject.activeSelf);
        if (image.gameObject.activeSelf)
        {
            collapse_button_label.text = "\u25BC";
        }
        else
        {
            collapse_button_label.text = "\u25BA";
        }
    }

    private void close()
    {
        StructureFormulaManager.Singleton.requestRemove(this);
    }
}
