using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureFormula : Draggable
{
    public SVGImage image;
    public Button collapse_button;
    public TextMeshProUGUI collapse_button_label;
    public TextMeshProUGUI label;

    private void Start()
    {
        collapse_button.onClick.AddListener(delegate { toggleImage(); });
    }

    public void newImageResize()
    {
        var rect = transform as RectTransform;
        var image_rect = image.transform as RectTransform;
        var button_rect = collapse_button.transform as RectTransform;
        var label_rect = label.transform as RectTransform;
        var vert_layout = GetComponent<VerticalLayoutGroup>();

        float new_width = image_rect.sizeDelta.x + vert_layout.padding.left + vert_layout.padding.right;
        float new_height = image_rect.sizeDelta.y + vert_layout.padding.top + vert_layout.padding.top + button_rect.sizeDelta.y + vert_layout.spacing + label_rect.sizeDelta.y + vert_layout.spacing;

        rect.sizeDelta = new Vector2(new_width, new_height);

        if (!image.gameObject.activeSelf)
        {
            resizeCollapse(true);
        }
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




}
