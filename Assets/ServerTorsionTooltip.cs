using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
public class ServerTorsionTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text ToolTipText;
    public Button closeButton;
    public Button modifyButton;
    

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
        var rect = transform as RectTransform;
        RectTransform canvasRectTransform = UI.GetComponent<RectTransform>();
        rect.anchorMin= new Vector2(1,0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(0, 0);
        this.transform.localScale = new Vector2(1, 1);
    }

    // Update is called once per frame
        public void resize() 
    {
        if (isSmall) 
        {
        
        }
        else 
        {
        
        }
    }
}
