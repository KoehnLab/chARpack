using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ServerBondTooltip : MonoBehaviour
{
    public Button collapse_button;
    public TMP_Text ToolTipText;
    public Button closeButton;
    public Button deleteButton;    
    public Button modifyButton;
    

    public Boolean isSmall = false;
    public GameObject title;
    public GameObject infobox;
    public RectTransform rect;
  
    public Canvas UI;
    // Start is called before the first frame update
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
    }

    
    public void resize() 
    {
        if (isSmall) 
        {
        isSmall = false;
        deleteButton.gameObject.SetActive(true);
        modifyButton.gameObject.SetActive(true);
        infobox.SetActive(true);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y -230);
        }
        else 
        {
        isSmall = true;
        deleteButton.gameObject.SetActive(false);
        modifyButton.gameObject.SetActive(false);
        infobox.SetActive(false);
        rect.offsetMin = new Vector2(rect.offsetMin.x, rect.offsetMin.y +230);
        }
    }
}
