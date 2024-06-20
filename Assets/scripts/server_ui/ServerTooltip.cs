using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ServerTooltip : MonoBehaviour
{
    public TMP_Text ToolTipText;
    public Button closeButton;
    public RectTransform rect;
    public Vector3 localPosition = new Vector3(0, 0, 0);
    public GameObject userbox;
    public int focus_id = -1;


    public bool isSmall = false;
    public GameObject title;
    public GameObject infobox;
    // Start is called before the first frame update
    public Canvas UI;
    public void Start()
    {
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        transform.SetParent(UI.transform);
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

    public void assignColor(int focus_id)
    {
        userbox.GetComponent<RawImage>().color = FocusColors.getColor(focus_id);
    }

}