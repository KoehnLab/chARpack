using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ServerSnapTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text ToolTipText;
    public Button closeButton;
    public Button snapButton;
    public Vector3 localPosition = new Vector3(0, 0, 0);
    public Molecule mol1;
    public Molecule mol2;
    public GameObject userbox;

    public bool isSmall = false;
    public GameObject title;
    public GameObject infobox;
    public RectTransform rect;
    public int focus_id = -1;

    public Canvas UI;
    // Start is called before the first frame update
    void Start()
    {
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize(); });
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        transform.localScale = new Vector2(1, 1);
        if (localPosition != new Vector3(0, 0, 0))
        {
            rect.localPosition = localPosition;
        }
        else
        {
            Vector2 save = SpawnManager.Singleton.GetSpawnLocalPosition(rect);
            rect.position = save;
        }
        assignColor(focus_id);
    }


    public void resize()
    {
        if (isSmall)
        {
            isSmall = false;
            snapButton.gameObject.SetActive(true);
            infobox.SetActive(true);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y - 160);
        }
        else
        {
            isSmall = true;
            snapButton.gameObject.SetActive(false);
            infobox.SetActive(false);
            rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y + 160);
        }
    }
    public void assignColor(int focus_id)
    {
        userbox.GetComponent<RawImage>().color = FocusColors.getColor(focus_id);
    }
}
