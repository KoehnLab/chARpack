using chARpackColorPalette;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Microsoft.MixedReality.Toolkit;

public class ServerTorsionTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text ToolTipText;
    public Button closeButton;
    public Button modifyButton;
    public RectTransform rect;
    public Vector3 localPosition = new Vector3 (0,0,0);
    

    public Boolean isSmall = false;
    public GameObject title;
    public GameObject infobox;
    // Start is called before the first frame update
    public Canvas UI;
    void Start()
    {
        var UIthing = GameObject.Find("UICanvas");
        UI = UIthing.GetComponent<Canvas>();
        this.transform.SetParent(UI.transform);
        collapse_button.onClick.AddListener(delegate { resize();});
        var drag = title.gameObject.AddComponent<Draggable>();
        drag.target = transform;
        rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        rect.anchorMin= new Vector2(1,0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(0, 0);
        this.transform.localScale = new Vector2(1, 1);
        UnityEngine.Debug.Log(localPosition.x);
        UnityEngine.Debug.Log(localPosition.y);
        if(localPosition != new Vector3 (0,0,0))
        {
            rect.localPosition = localPosition;
        }        
    }

    // Update is called once per frame
    public void resize() 
    {
        if (isSmall) 
        {
        isSmall = false;
        modifyButton.gameObject.SetActive(true);
        infobox.SetActive(true);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y -130);
        }
        else 
        {
        isSmall = true;
        modifyButton.gameObject.SetActive(false);
        infobox.SetActive(false);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y +130);
        }
    }
        public void assignColour(int focus_id)
    {
        title.GetComponent<Image>().color = FocusColors.getColor(focus_id);
    }
    
}